using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;

using Avalonia;
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Media;

namespace SourceGit.Views
{
    public class CommitRefsSummary : Control
    {
        public static readonly StyledProperty<FontFamily> FontFamilyProperty =
            TextBlock.FontFamilyProperty.AddOwner<CommitRefsSummary>();

        public FontFamily FontFamily
        {
            get => GetValue(FontFamilyProperty);
            set => SetValue(FontFamilyProperty, value);
        }

        public static readonly StyledProperty<double> FontSizeProperty =
           TextBlock.FontSizeProperty.AddOwner<CommitRefsSummary>();

        public double FontSize
        {
            get => GetValue(FontSizeProperty);
            set => SetValue(FontSizeProperty, value);
        }

        public static readonly StyledProperty<IBrush> BackgroundProperty =
            AvaloniaProperty.Register<CommitRefsSummary, IBrush>(nameof(Background), Brushes.Transparent);

        public IBrush Background
        {
            get => GetValue(BackgroundProperty);
            set => SetValue(BackgroundProperty, value);
        }

        public static readonly StyledProperty<IBrush> ForegroundProperty =
            AvaloniaProperty.Register<CommitRefsSummary, IBrush>(nameof(Foreground), Brushes.White);

        public IBrush Foreground
        {
            get => GetValue(ForegroundProperty);
            set => SetValue(ForegroundProperty, value);
        }

        public static readonly StyledProperty<bool> UseGraphColorProperty =
            AvaloniaProperty.Register<CommitRefsSummary, bool>(nameof(UseGraphColor));

        public bool UseGraphColor
        {
            get => GetValue(UseGraphColorProperty);
            set => SetValue(UseGraphColorProperty, value);
        }

        public static readonly StyledProperty<bool> ShowTagsProperty =
            AvaloniaProperty.Register<CommitRefsSummary, bool>(nameof(ShowTags), true);

        public bool ShowTags
        {
            get => GetValue(ShowTagsProperty);
            set => SetValue(ShowTagsProperty, value);
        }

        static CommitRefsSummary()
        {
            AffectsRender<CommitRefsSummary>(
                FontFamilyProperty,
                FontSizeProperty,
                ForegroundProperty,
                UseGraphColorProperty,
                BackgroundProperty,
                ShowTagsProperty);
            
            AffectsMeasure<CommitRefsSummary>(
                FontFamilyProperty,
                FontSizeProperty,
                ShowTagsProperty);
        }

        public override void Render(DrawingContext context)
        {
            base.Render(context);

            if (DataContext is not Models.Commit commit)
                return;

            var refs = GetFilteredRefs(commit);
            if (refs.Count == 0)
                return;

            var displayRef = GetDisplayRef(refs);
            if (displayRef == null)
                return;

            var typeface = new Typeface(FontFamily);
            var fg = Foreground;
            var bg = UseGraphColor ? commit.Brush : Brushes.Gray;

            // Create display text - show primary ref
            var displayText = displayRef.Name;
            if (refs.Count > 1)
            {
                displayText = $"{displayRef.Name} (+{refs.Count - 1})";
            }
            
            var label = new FormattedText(
                displayText,
                CultureInfo.CurrentCulture,
                FlowDirection.LeftToRight,
                typeface,
                FontSize,
                fg);

            var x = 2.0;
            var y = (Bounds.Height - 16) / 2;
            
            // Draw background
            var bgRect = new RoundedRect(new Rect(x, y, label.Width + 8, 16), new CornerRadius(2));
            
            if (Background != null)
                context.DrawRectangle(Background, null, bgRect);
                
            using (context.PushOpacity(0.3))
                context.DrawRectangle(bg, null, bgRect);
            
            // Draw border
            context.DrawRectangle(null, new Pen(bg, 1), bgRect);
            
            // Draw text
            context.DrawText(label, new Point(x + 4, y + 8 - label.Height * 0.5));
        }

        protected override void OnPointerEntered(PointerEventArgs e)
        {
            base.OnPointerEntered(e);
            UpdateTooltip();
        }

        protected override void OnDataContextChanged(EventArgs e)
        {
            base.OnDataContextChanged(e);
            UpdateTooltip();
        }

        private void UpdateTooltip()
        {
            if (DataContext is not Models.Commit commit)
            {
                ToolTip.SetTip(this, null);
                return;
            }

            var refs = GetFilteredRefs(commit);
            if (refs.Count <= 1)
            {
                ToolTip.SetTip(this, null);
                return;
            }

            // Build tooltip content as a list
            var sb = new StringBuilder();
            sb.AppendLine("Branches & Tags:");
            sb.AppendLine();
            
            var branches = new List<string>();
            var tags = new List<string>();
            
            foreach (var decorator in refs)
            {
                if (decorator.Type == Models.DecoratorType.Tag)
                {
                    tags.Add($"  • {decorator.Name}");
                }
                else
                {
                    var prefix = decorator.Type switch
                    {
                        Models.DecoratorType.CurrentBranchHead => "  ► ",
                        Models.DecoratorType.CurrentCommitHead => "  ▶ ",
                        Models.DecoratorType.RemoteBranchHead => "  ⇡ ",
                        _ => "  • "
                    };
                    branches.Add($"{prefix}{decorator.Name}");
                }
            }
            
            if (branches.Count > 0)
            {
                sb.AppendLine("Branches:");
                foreach (var branch in branches)
                    sb.AppendLine(branch);
            }
            
            if (tags.Count > 0)
            {
                if (branches.Count > 0)
                    sb.AppendLine();
                sb.AppendLine("Tags:");
                foreach (var tag in tags)
                    sb.AppendLine(tag);
            }
            
            var tooltip = new TextBlock
            {
                Text = sb.ToString().TrimEnd(),
                FontFamily = FontFamily,
                FontSize = FontSize - 1,
                Foreground = Foreground
            };
            
            ToolTip.SetTip(this, tooltip);
            ToolTip.SetShowDelay(this, 200);
        }

        private List<Models.Decorator> GetFilteredRefs(Models.Commit commit)
        {
            var result = new List<Models.Decorator>();
            if (commit.Decorators == null)
                return result;
                
            foreach (var decorator in commit.Decorators)
            {
                if (decorator.Type == Models.DecoratorType.Tag && !ShowTags)
                    continue;
                result.Add(decorator);
            }
            
            return result;
        }

        private Models.Decorator GetDisplayRef(List<Models.Decorator> refs)
        {
            if (refs.Count == 0)
                return null;
                
            // Prefer current branch head
            foreach (var decorator in refs)
            {
                if (decorator.Type == Models.DecoratorType.CurrentBranchHead)
                    return decorator;
            }
            
            // Then current commit head
            foreach (var decorator in refs)
            {
                if (decorator.Type == Models.DecoratorType.CurrentCommitHead)
                    return decorator;
            }
            
            // Then any branch
            foreach (var decorator in refs)
            {
                if (decorator.Type != Models.DecoratorType.Tag)
                    return decorator;
            }
            
            // Finally tags
            return refs[0];
        }

        protected override Size MeasureOverride(Size availableSize)
        {
            if (DataContext is not Models.Commit commit)
                return new Size(0, 0);

            var refs = GetFilteredRefs(commit);
            if (refs.Count == 0)
                return new Size(0, 0);

            var displayRef = GetDisplayRef(refs);
            if (displayRef == null)
                return new Size(0, 0);

            var typeface = new Typeface(FontFamily);
            
            // Create display text
            var displayText = displayRef.Name;
            if (refs.Count > 1)
            {
                displayText = $"{displayRef.Name} (+{refs.Count - 1})";
            }
            
            var label = new FormattedText(
                displayText,
                CultureInfo.CurrentCulture,
                FlowDirection.LeftToRight,
                typeface,
                FontSize,
                Foreground);

            return new Size(label.Width + 12, 20);
        }
    }
}