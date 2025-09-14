using System;
using System.Threading.Tasks;
using FluentAssertions;
using SourceGit.Models;
using Xunit;

namespace SourceGit.Tests.Models
{
    public class LRUCacheTests
    {
        private class TestItem
        {
            public string Value { get; set; } = string.Empty;
            public int Size { get; set; }
        }

        [Fact]
        public void Set_And_Get_ShouldStoreAndRetrieveItems()
        {
            // Arrange
            var cache = new LRUCache<string, TestItem>(
                maxCapacity: 10,
                maxMemoryMB: 1,
                sizeCalculator: item => item.Size
            );
            var item = new TestItem { Value = "test", Size = 10 };

            // Act
            cache.Set("key1", item);
            var retrieved = cache.Get("key1");

            // Assert
            retrieved.Should().NotBeNull();
            retrieved.Value.Should().Be("test");
            cache.Count.Should().Be(1);
        }

        [Fact]
        public void Get_ShouldReturnNull_WhenKeyDoesNotExist()
        {
            // Arrange
            var cache = new LRUCache<string, TestItem>(10, 1, item => item.Size);

            // Act
            var result = cache.Get("nonexistent");

            // Assert
            result.Should().BeNull();
        }

        [Fact]
        public void Set_ShouldEvictLeastRecentlyUsed_WhenMaxItemsExceeded()
        {
            // Arrange
            var cache = new LRUCache<string, TestItem>(
                maxCapacity: 3,
                maxMemoryMB: 1,
                sizeCalculator: item => item.Size
            );

            // Act
            cache.Set("key1", new TestItem { Value = "1", Size = 10 });
            cache.Set("key2", new TestItem { Value = "2", Size = 10 });
            cache.Set("key3", new TestItem { Value = "3", Size = 10 });
            cache.Set("key4", new TestItem { Value = "4", Size = 10 }); // Should evict key1

            // Assert
            cache.Get("key1").Should().BeNull(); // Evicted
            cache.Get("key2").Should().NotBeNull();
            cache.Get("key3").Should().NotBeNull();
            cache.Get("key4").Should().NotBeNull();
            cache.Count.Should().Be(3);
        }

        [Fact]
        public void Set_ShouldEvictItems_WhenMemoryLimitExceeded()
        {
            // Arrange
            var cache = new LRUCache<string, TestItem>(
                maxCapacity: 10,
                maxMemoryMB: 1,
                sizeCalculator: item => item.Size
            );

            // Act - Use sizes that will actually exceed 1MB
            cache.Set("key1", new TestItem { Value = "1", Size = 500000 }); // 500KB
            cache.Set("key2", new TestItem { Value = "2", Size = 500000 }); // 500KB
            cache.Set("key3", new TestItem { Value = "3", Size = 500000 }); // 500KB - Should trigger eviction

            // Assert
            cache.Count.Should().BeLessThanOrEqualTo(2); // Only 2 items should fit in 1MB
            var stats = cache.GetStatistics();
            stats.MemoryUsageBytes.Should().BeLessThanOrEqualTo(1048576); // 1MB in bytes
        }

        [Fact]
        public void Get_ShouldUpdateLRUOrder()
        {
            // Arrange
            var cache = new LRUCache<string, TestItem>(
                maxCapacity: 3,
                maxMemoryMB: 1,
                sizeCalculator: item => item.Size
            );
            
            cache.Set("key1", new TestItem { Value = "1", Size = 10 });
            cache.Set("key2", new TestItem { Value = "2", Size = 10 });
            cache.Set("key3", new TestItem { Value = "3", Size = 10 });

            // Act
            cache.Get("key1"); // Move key1 to most recently used
            cache.Set("key4", new TestItem { Value = "4", Size = 10 }); // Should evict key2

            // Assert
            cache.Get("key1").Should().NotBeNull(); // Still in cache
            cache.Get("key2").Should().BeNull(); // Evicted
            cache.Get("key3").Should().NotBeNull();
            cache.Get("key4").Should().NotBeNull();
        }

        [Fact]
        public void Clear_ShouldRemoveAllItems()
        {
            // Arrange
            var cache = new LRUCache<string, TestItem>(10, 1, item => item.Size);
            cache.Set("key1", new TestItem { Value = "1", Size = 10 });
            cache.Set("key2", new TestItem { Value = "2", Size = 10 });

            // Act
            cache.Clear();

            // Assert
            cache.Count.Should().Be(0);
            cache.Get("key1").Should().BeNull();
            cache.Get("key2").Should().BeNull();
        }

        [Fact]
        public void GetStatistics_ShouldReturnCorrectStats()
        {
            // Arrange
            var cache = new LRUCache<string, TestItem>(10, 1, item => item.Size);
            cache.Set("key1", new TestItem { Value = "1", Size = 20 });
            cache.Set("key2", new TestItem { Value = "2", Size = 30 });

            // Act
            var stats = cache.GetStatistics();

            // Assert
            stats.ItemCount.Should().Be(2);
            stats.MemoryUsageBytes.Should().Be(50);
            stats.MaxCapacity.Should().Be(10);
            stats.MaxMemoryBytes.Should().Be(1048576); // 1MB in bytes
        }

        [Fact]
        public void TrimExcess_ShouldReduceToHalfCapacity_WhenOverThreshold()
        {
            // Arrange
            var cache = new LRUCache<string, TestItem>(10, 1, item => item.Size);
            
            // Fill cache
            for (int i = 0; i < 10; i++)
            {
                cache.Set($"key{i}", new TestItem { Value = $"{i}", Size = 10 });
            }

            // Act
            cache.TrimExcess();

            // Assert
            cache.Count.Should().BeLessThanOrEqualTo(5); // Should trim to ~50%
        }

        [Fact]
        public async Task ThreadSafety_ConcurrentOperations_ShouldNotThrow()
        {
            // Arrange
            var cache = new LRUCache<string, TestItem>(100, 10, item => item.Size);
            var tasks = new Task[10];

            // Act
            for (int i = 0; i < 10; i++)
            {
                int threadId = i;
                tasks[i] = Task.Run(() =>
                {
                    for (int j = 0; j < 100; j++)
                    {
                        var key = $"thread{threadId}_item{j}";
                        cache.Set(key, new TestItem { Value = key, Size = 10 });
                        cache.Get(key);
                    }
                });
            }

            // Assert
            await Task.WhenAll(tasks);
            cache.Count.Should().BeGreaterThan(0);
            cache.Count.Should().BeLessThanOrEqualTo(100);
        }
    }
}