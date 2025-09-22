using System;
using System.Collections.Generic;
using System.Threading.Tasks;

using Avalonia;
using Avalonia.Media;

namespace SourceGit.Models
{
    public record CommitGraphLayout(double startY, double clipWidth, double rowHeight)
    {
        public double StartY { get; set; } = startY;
        public double ClipWidth { get; set; } = clipWidth;
        public double RowHeight { get; set; } = rowHeight;
    }

    public class CommitGraph
    {
        public static List<Pen> Pens { get; } = [];

        public static void SetDefaultPens(double thickness = 2)
        {
            SetPens(s_defaultPenColors, thickness);
        }

        public static void SetPens(List<Color> colors, double thickness)
        {
            Pens.Clear();

            foreach (var c in colors)
                Pens.Add(new Pen(c.ToUInt32(), thickness));

            s_penCount = colors.Count;
        }

        public class Path(int color, bool isMerged)
        {
            public List<Point> Points { get; } = [];
            public int Color { get; } = color;
            public bool IsMerged { get; } = isMerged;
        }

        public class Link
        {
            public Point Start;
            public Point Control;
            public Point End;
            public int Color;
            public bool IsMerged;
        }

        public enum DotType
        {
            Default,
            Head,
            Merge,
        }

        public class Dot
        {
            public DotType Type;
            public Point Center;
            public int Color;
            public bool IsMerged;
        }

        public List<Path> Paths { get; } = [];
        public List<Link> Links { get; } = [];
        public List<Dot> Dots { get; } = [];

        /// <summary>
        /// Async version of Parse with chunked processing for large repositories
        /// </summary>
        public static async Task<CommitGraph> ParseAsync(List<Commit> commits, bool firstParentOnlyEnabled, int chunkSize = 50, int yieldFrequency = 100)
        {
            // Check if this is a linear repository (optimization for performance)
            bool isLinear = IsLinearRepository(commits);
            System.Diagnostics.Debug.WriteLine($"CommitGraph: Processing {commits.Count} commits async, isLinear: {isLinear}");

            if (isLinear)
            {
                System.Diagnostics.Debug.WriteLine("CommitGraph: Using optimized linear algorithm (async)");
                return await ParseLinearRepositoryAsync(commits, yieldFrequency);
            }

            System.Diagnostics.Debug.WriteLine("CommitGraph: Using complex algorithm for branched repository (async)");
            return await ParseComplexRepositoryAsync(commits, firstParentOnlyEnabled, chunkSize, yieldFrequency);
        }

        public static CommitGraph Parse(List<Commit> commits, bool firstParentOnlyEnabled)
        {
            // Check if this is a linear repository (optimization for performance)
            bool isLinear = IsLinearRepository(commits);
            System.Diagnostics.Debug.WriteLine($"CommitGraph: Processing {commits.Count} commits, isLinear: {isLinear}");

            if (isLinear)
            {
                System.Diagnostics.Debug.WriteLine("CommitGraph: Using optimized linear algorithm");
                return ParseLinearRepository(commits);
            }

            System.Diagnostics.Debug.WriteLine("CommitGraph: Using complex algorithm for branched repository");

            const double unitWidth = 12;
            const double halfWidth = 6;
            const double unitHeight = 1;
            const double halfHeight = 0.5;

            var temp = new CommitGraph();
            var unsolved = new List<PathHelper>();
            var ended = new List<PathHelper>();
            var offsetY = -halfHeight;
            var colorPicker = new ColorPicker();

            foreach (var commit in commits)
            {
                PathHelper major = null;
                var isMerged = commit.IsMerged;

                // Update current y offset
                offsetY += unitHeight;

                // Find first curves that links to this commit and marks others that links to this commit ended.
                var offsetX = 4 - halfWidth;
                var maxOffsetOld = unsolved.Count > 0 ? unsolved[^1].LastX : offsetX + unitWidth;
                foreach (var l in unsolved)
                {
                    if (l.Next.Equals(commit.SHA, StringComparison.Ordinal))
                    {
                        if (major == null)
                        {
                            offsetX += unitWidth;
                            major = l;

                            if (commit.Parents.Count > 0)
                            {
                                major.Next = commit.Parents[0];
                                major.Goto(offsetX, offsetY, halfHeight);
                            }
                            else
                            {
                                major.End(offsetX, offsetY, halfHeight);
                                ended.Add(l);
                            }
                        }
                        else
                        {
                            l.End(major.LastX, offsetY, halfHeight);
                            ended.Add(l);
                        }

                        isMerged = isMerged || l.IsMerged;
                    }
                    else
                    {
                        offsetX += unitWidth;
                        l.Pass(offsetX, offsetY, halfHeight);
                    }
                }

                // Remove ended curves from unsolved
                foreach (var l in ended)
                {
                    colorPicker.Recycle(l.Path.Color);
                    unsolved.Remove(l);
                }
                ended.Clear();

                // If no path found, create new curve for branch head
                // Otherwise, create new curve for new merged commit
                if (major == null)
                {
                    offsetX += unitWidth;

                    if (commit.Parents.Count > 0)
                    {
                        major = new PathHelper(commit.Parents[0], isMerged, colorPicker.Next(), new Point(offsetX, offsetY));
                        unsolved.Add(major);
                        temp.Paths.Add(major.Path);
                    }
                }
                else if (isMerged && !major.IsMerged && commit.Parents.Count > 0)
                {
                    major.ReplaceMerged();
                    temp.Paths.Add(major.Path);
                }

                // Calculate link position of this commit.
                var position = new Point(major?.LastX ?? offsetX, offsetY);
                var dotColor = major?.Path.Color ?? 0;
                var anchor = new Dot() { Center = position, Color = dotColor, IsMerged = isMerged };
                if (commit.IsCurrentHead)
                    anchor.Type = DotType.Head;
                else if (commit.Parents.Count > 1)
                    anchor.Type = DotType.Merge;
                else
                    anchor.Type = DotType.Default;
                temp.Dots.Add(anchor);

                // Deal with other parents (the first parent has been processed)
                if (!firstParentOnlyEnabled)
                {
                    for (int j = 1; j < commit.Parents.Count; j++)
                    {
                        var parentHash = commit.Parents[j];
                        var parent = unsolved.Find(x => x.Next.Equals(parentHash, StringComparison.Ordinal));
                        if (parent != null)
                        {
                            if (isMerged && !parent.IsMerged)
                            {
                                parent.Goto(parent.LastX, offsetY + halfHeight, halfHeight);
                                parent.ReplaceMerged();
                                temp.Paths.Add(parent.Path);
                            }

                            temp.Links.Add(new Link
                            {
                                Start = position,
                                End = new Point(parent.LastX, offsetY + halfHeight),
                                Control = new Point(parent.LastX, position.Y),
                                Color = parent.Path.Color,
                                IsMerged = isMerged,
                            });
                        }
                        else
                        {
                            offsetX += unitWidth;

                            // Create new curve for parent commit that not includes before
                            var l = new PathHelper(parentHash, isMerged, colorPicker.Next(), position, new Point(offsetX, position.Y + halfHeight));
                            unsolved.Add(l);
                            temp.Paths.Add(l.Path);
                        }
                    }
                }

                // Margins & merge state (used by Views.Histories).
                commit.IsMerged = isMerged;
                commit.Margin = new Thickness(Math.Max(offsetX, maxOffsetOld) + halfWidth + 2, 0, 0, 0);
                commit.Color = dotColor;
            }

            // Deal with curves haven't ended yet.
            for (var i = 0; i < unsolved.Count; i++)
            {
                var path = unsolved[i];
                var endY = (commits.Count - 0.5) * unitHeight;

                if (path.Path.Points.Count == 1 && Math.Abs(path.Path.Points[0].Y - endY) < 0.0001)
                    continue;

                path.End((i + 0.5) * unitWidth + 4, endY + halfHeight, halfHeight);
            }
            unsolved.Clear();

            return temp;
        }

        private class ColorPicker
        {
            public int Next()
            {
                if (_colorsQueue.Count == 0)
                {
                    for (var i = 0; i < s_penCount; i++)
                        _colorsQueue.Enqueue(i);
                }

                return _colorsQueue.Dequeue();
            }

            public void Recycle(int idx)
            {
                if (!_colorsQueue.Contains(idx))
                    _colorsQueue.Enqueue(idx);
            }

            private Queue<int> _colorsQueue = new Queue<int>();
        }

        private class PathHelper
        {
            public Path Path { get; private set; }
            public string Next { get; set; }
            public double LastX { get; private set; }

            public bool IsMerged => Path.IsMerged;

            public PathHelper(string next, bool isMerged, int color, Point start)
            {
                Next = next;
                LastX = start.X;
                _lastY = start.Y;

                Path = new Path(color, isMerged);
                Path.Points.Add(start);
            }

            public PathHelper(string next, bool isMerged, int color, Point start, Point to)
            {
                Next = next;
                LastX = to.X;
                _lastY = to.Y;

                Path = new Path(color, isMerged);
                Path.Points.Add(start);
                Path.Points.Add(to);
            }

            /// <summary>
            ///     A path that just passed this row.
            /// </summary>
            /// <param name="x"></param>
            /// <param name="y"></param>
            /// <param name="halfHeight"></param>
            public void Pass(double x, double y, double halfHeight)
            {
                if (x > LastX)
                {
                    Add(LastX, _lastY);
                    Add(x, y - halfHeight);
                }
                else if (x < LastX)
                {
                    Add(LastX, y - halfHeight);
                    y += halfHeight;
                    Add(x, y);
                }

                LastX = x;
                _lastY = y;
            }

            /// <summary>
            ///     A path that has commit in this row but not ended
            /// </summary>
            /// <param name="x"></param>
            /// <param name="y"></param>
            /// <param name="halfHeight"></param>
            public void Goto(double x, double y, double halfHeight)
            {
                if (x > LastX)
                {
                    Add(LastX, _lastY);
                    Add(x, y - halfHeight);
                }
                else if (x < LastX)
                {
                    var minY = y - halfHeight;
                    if (minY > _lastY)
                        minY -= halfHeight;

                    Add(LastX, minY);
                    Add(x, y);
                }

                LastX = x;
                _lastY = y;
            }

            /// <summary>
            ///     A path that has commit in this row and end.
            /// </summary>
            /// <param name="x"></param>
            /// <param name="y"></param>
            /// <param name="halfHeight"></param>
            public void End(double x, double y, double halfHeight)
            {
                if (x > LastX)
                {
                    Add(LastX, _lastY);
                    Add(x, y - halfHeight);
                }
                else if (x < LastX)
                {
                    Add(LastX, y - halfHeight);
                }

                Add(x, y);

                LastX = x;
                _lastY = y;
            }

            /// <summary>
            ///     End the current path and create a new from the end.
            /// </summary>
            public void ReplaceMerged()
            {
                var color = Path.Color;
                Add(LastX, _lastY);

                Path = new Path(color, true);
                Path.Points.Add(new Point(LastX, _lastY));
                _endY = 0;
            }

            private void Add(double x, double y)
            {
                if (_endY < y)
                {
                    Path.Points.Add(new Point(x, y));
                    _endY = y;
                }
            }

            private double _lastY = 0;
            private double _endY = 0;
        }

        private static int s_penCount = 0;
        private static readonly List<Color> s_defaultPenColors = [
            Colors.Orange,
            Colors.ForestGreen,
            Colors.Turquoise,
            Colors.Olive,
            Colors.Magenta,
            Colors.Red,
            Colors.Khaki,
            Colors.Lime,
            Colors.RoyalBlue,
            Colors.Teal,
        ];

        /// <summary>
        /// Detects if a repository has a linear commit history (no branches/merges)
        /// This allows us to use a much faster O(n) algorithm instead of O(n²)
        /// </summary>
        private static bool IsLinearRepository(List<Commit> commits)
        {
            if (commits.Count == 0)
                return true;
            if (commits.Count == 1)
                return true;

            System.Diagnostics.Debug.WriteLine($"IsLinearRepository: Checking {commits.Count} commits");

            // Check first few commits for debugging
            for (int i = 0; i < Math.Min(5, commits.Count); i++)
            {
                var commit = commits[i];
                System.Diagnostics.Debug.WriteLine($"Commit {i}: SHA={commit.SHA[..8]}, Parents={commit.Parents.Count}");
                if (commit.Parents.Count > 0)
                    System.Diagnostics.Debug.WriteLine($"  First parent: {commit.Parents[0][..8]}");
            }

            // Simplified check: just verify no commit has more than 1 parent (no merges)
            for (int i = 0; i < commits.Count; i++)
            {
                var commit = commits[i];

                // If any commit has more than 1 parent, it's a merge
                if (commit.Parents.Count > 1)
                {
                    System.Diagnostics.Debug.WriteLine($"Found merge commit at index {i}: {commit.Parents.Count} parents");
                    return false;
                }

                // Root commit can have 0 parents, all others should have 1
                if (i < commits.Count - 1 && commit.Parents.Count == 0)
                {
                    System.Diagnostics.Debug.WriteLine($"Found non-root commit with 0 parents at index {i}");
                    return false;
                }
            }

            System.Diagnostics.Debug.WriteLine("Repository detected as linear");
            return true;
        }

        /// <summary>
        /// Async version of ParseLinearRepository with yield points for UI responsiveness
        /// </summary>
        private static async Task<CommitGraph> ParseLinearRepositoryAsync(List<Commit> commits, int yieldFrequency)
        {
            const double unitWidth = 12;
            const double unitHeight = 1;
            const double halfHeight = 0.5;

            var graph = new CommitGraph();
            if (commits.Count == 0)
                return graph;

            // Create a single path for the linear history
            var path = new Path(0, false); // Color 0, not merged (main branch)
            var offsetX = unitWidth; // Fixed X position for linear history
            var offsetY = -halfHeight;

            // Add dots and create the straight line path with yield points
            for (int i = 0; i < commits.Count; i++)
            {
                offsetY += unitHeight;

                // Add dot for this commit
                graph.Dots.Add(new Dot
                {
                    Center = new Point(offsetX, offsetY),
                    Color = 0,
                    IsMerged = commits[i].IsMerged,
                    Type = commits[i].IsCurrentHead ? DotType.Head :
                           commits[i].Parents.Count > 1 ? DotType.Merge : DotType.Default
                });

                // Add point to the path
                path.Points.Add(new Point(offsetX, offsetY));

                // Set commit properties
                commits[i].Margin = new Thickness(offsetX + 8, 0, 0, 0);
                commits[i].Color = 0;

                // Yield control periodically to keep UI responsive
                if (i % yieldFrequency == 0 && i > 0)
                {
                    await Task.Delay(1);
                }
            }

            // Add the single path to the graph
            graph.Paths.Add(path);

            return graph;
        }

        /// <summary>
        /// Async version of complex repository parsing with chunked processing
        /// </summary>
        private static async Task<CommitGraph> ParseComplexRepositoryAsync(List<Commit> commits, bool firstParentOnlyEnabled, int chunkSize, int yieldFrequency)
        {
            // For complex repositories, fall back to chunked synchronous processing
            // This prevents blocking while still providing yield points
            return await Task.Run(async () =>
            {
                const double unitWidth = 12;
                const double halfWidth = 6;
                const double unitHeight = 1;
                const double halfHeight = 0.5;

                var temp = new CommitGraph();
                var unsolved = new List<PathHelper>();
                var ended = new List<PathHelper>();
                var offsetY = -halfHeight;
                var colorPicker = new ColorPicker();

                for (int commitIndex = 0; commitIndex < commits.Count; commitIndex++)
                {
                    var commit = commits[commitIndex];
                    PathHelper major = null;
                    var isMerged = commit.IsMerged;

                    // Update current y offset
                    offsetY += unitHeight;

                    // Find first curves that links to this commit and marks others that links to this commit ended.
                    var offsetX = 4 - halfWidth;
                    var maxOffsetOld = unsolved.Count > 0 ? unsolved[^1].LastX : offsetX + unitWidth;
                    foreach (var l in unsolved)
                    {
                        if (l.Next.Equals(commit.SHA, StringComparison.Ordinal))
                        {
                            if (major == null)
                            {
                                offsetX += unitWidth;
                                major = l;

                                if (commit.Parents.Count > 0)
                                {
                                    major.Next = commit.Parents[0];
                                    major.Goto(offsetX, offsetY, halfHeight);
                                }
                                else
                                {
                                    major.End(offsetX, offsetY, halfHeight);
                                    ended.Add(l);
                                }
                            }
                            else
                            {
                                l.End(major.LastX, offsetY, halfHeight);
                                ended.Add(l);
                            }

                            isMerged = isMerged || l.IsMerged;
                        }
                        else
                        {
                            offsetX += unitWidth;
                            l.Pass(offsetX, offsetY, halfHeight);
                        }
                    }

                    // Remove ended curves from unsolved
                    foreach (var l in ended)
                    {
                        colorPicker.Recycle(l.Path.Color);
                        unsolved.Remove(l);
                    }
                    ended.Clear();

                    // If no path found, create new curve for branch head
                    // Otherwise, create new curve for new merged commit
                    if (major == null)
                    {
                        offsetX += unitWidth;

                        if (commit.Parents.Count > 0)
                        {
                            major = new PathHelper(commit.Parents[0], isMerged, colorPicker.Next(), new Point(offsetX, offsetY));
                            unsolved.Add(major);
                            temp.Paths.Add(major.Path);
                        }
                    }
                    else if (isMerged && !major.IsMerged && commit.Parents.Count > 0)
                    {
                        major.ReplaceMerged();
                        temp.Paths.Add(major.Path);
                    }

                    // Calculate link position of this commit.
                    var position = new Point(major?.LastX ?? offsetX, offsetY);
                    var dotColor = major?.Path.Color ?? 0;
                    var anchor = new Dot() { Center = position, Color = dotColor, IsMerged = isMerged };
                    if (commit.IsCurrentHead)
                        anchor.Type = DotType.Head;
                    else if (commit.Parents.Count > 1)
                        anchor.Type = DotType.Merge;
                    else
                        anchor.Type = DotType.Default;
                    temp.Dots.Add(anchor);

                    // Deal with other parents (the first parent has been processed)
                    if (!firstParentOnlyEnabled)
                    {
                        for (int j = 1; j < commit.Parents.Count; j++)
                        {
                            var parentHash = commit.Parents[j];
                            var parent = unsolved.Find(x => x.Next.Equals(parentHash, StringComparison.Ordinal));
                            if (parent != null)
                            {
                                if (isMerged && !parent.IsMerged)
                                {
                                    parent.Goto(parent.LastX, offsetY + halfHeight, halfHeight);
                                    parent.ReplaceMerged();
                                    temp.Paths.Add(parent.Path);
                                }

                                temp.Links.Add(new Link
                                {
                                    Start = position,
                                    End = new Point(parent.LastX, offsetY + halfHeight),
                                    Control = new Point(parent.LastX, position.Y),
                                    Color = parent.Path.Color,
                                    IsMerged = isMerged,
                                });
                            }
                            else
                            {
                                offsetX += unitWidth;

                                // Create new curve for parent commit that not includes before
                                var l = new PathHelper(parentHash, isMerged, colorPicker.Next(), position, new Point(offsetX, position.Y + halfHeight));
                                unsolved.Add(l);
                                temp.Paths.Add(l.Path);
                            }
                        }
                    }

                    // Margins & merge state (used by Views.Histories).
                    commit.IsMerged = isMerged;
                    commit.Margin = new Thickness(Math.Max(offsetX, maxOffsetOld) + halfWidth + 2, 0, 0, 0);
                    commit.Color = dotColor;

                    // Yield control periodically
                    if (commitIndex % yieldFrequency == 0 && commitIndex > 0)
                    {
                        await Task.Delay(1);
                    }
                }

                // Deal with curves haven't ended yet.
                for (var i = 0; i < unsolved.Count; i++)
                {
                    var path = unsolved[i];
                    var endY = (commits.Count - 0.5) * unitHeight;

                    if (path.Path.Points.Count == 1 && Math.Abs(path.Path.Points[0].Y - endY) < 0.0001)
                        continue;

                    path.End((i + 0.5) * unitWidth + 4, endY + halfHeight, halfHeight);
                }
                unsolved.Clear();

                return temp;
            });
        }

        /// <summary>
        /// Optimized parser for linear repositories - O(n) instead of O(n²)
        /// Creates a simple straight line down the commit list
        /// </summary>
        private static CommitGraph ParseLinearRepository(List<Commit> commits)
        {
            const double unitWidth = 12;
            const double unitHeight = 1;
            const double halfHeight = 0.5;

            var graph = new CommitGraph();
            if (commits.Count == 0)
                return graph;

            // Create a single path for the linear history
            var path = new Path(0, false); // Color 0, not merged (main branch)
            var offsetX = unitWidth; // Fixed X position for linear history
            var offsetY = -halfHeight;

            // Add dots and create the straight line path
            for (int i = 0; i < commits.Count; i++)
            {
                offsetY += unitHeight;

                // Set dot type based on commit properties
                var dotType = commits[i].IsCurrentHead ? DotType.Head :
                             commits[i].Parents.Count > 1 ? DotType.Merge : DotType.Default;

                // Add dot for this commit
                graph.Dots.Add(new Dot
                {
                    Center = new Point(offsetX, offsetY),
                    Color = 0,
                    IsMerged = commits[i].IsMerged,
                    Type = dotType
                });

                // Add point to the path
                path.Points.Add(new Point(offsetX, offsetY));

                // Set commit properties
                commits[i].Margin = new Thickness(offsetX + 8, 0, 0, 0);
                commits[i].Color = 0;
            }

            // Add the single path to the graph
            graph.Paths.Add(path);

            return graph;
        }
    }
}
