using System.Collections.Generic;
using NUnit.Framework;

public class BundleDependencyTopologyTests
{
    [Test]
    public void SortDependencyClosure_Chain_CBeforeBBeforeA()
    {
        var graph = new Dictionary<string, List<string>>
        {
            { "a.bundle", new List<string> { "b.bundle" } },
            { "b.bundle", new List<string> { "c.bundle" } },
            { "c.bundle", new List<string>() },
        };

        string[] sorted = BundleDependencyTopology.SortDependencyClosure(
            graph,
            new[] { "a.bundle", "b.bundle", "c.bundle" });

        Assert.AreEqual(3, sorted.Length);
        Assert.Less(IndexOf(sorted, "c.bundle"), IndexOf(sorted, "b.bundle"));
        Assert.Less(IndexOf(sorted, "b.bundle"), IndexOf(sorted, "a.bundle"));
    }

    [Test]
    public void SortDependencyClosure_Diamond_ValidOrder()
    {
        var graph = new Dictionary<string, List<string>>
        {
            { "top.bundle", new List<string> { "left.bundle", "right.bundle" } },
            { "left.bundle", new List<string> { "base.bundle" } },
            { "right.bundle", new List<string> { "base.bundle" } },
            { "base.bundle", new List<string>() },
        };

        string[] sorted = BundleDependencyTopology.SortDependencyClosure(
            graph,
            new[] { "top.bundle", "left.bundle", "right.bundle", "base.bundle" });

        Assert.Less(IndexOf(sorted, "base.bundle"), IndexOf(sorted, "left.bundle"));
        Assert.Less(IndexOf(sorted, "base.bundle"), IndexOf(sorted, "right.bundle"));
        Assert.Less(IndexOf(sorted, "left.bundle"), IndexOf(sorted, "top.bundle"));
        Assert.Less(IndexOf(sorted, "right.bundle"), IndexOf(sorted, "top.bundle"));
    }

    [Test]
    public void TryTopologicalSort_Cycle_ReturnsFalse()
    {
        var graph = new Dictionary<string, List<string>>
        {
            { "a.bundle", new List<string> { "b.bundle" } },
            { "b.bundle", new List<string> { "a.bundle" } },
        };

        bool ok = BundleDependencyTopology.TryTopologicalSort(
            new[] { "a.bundle", "b.bundle" },
            graph,
            out string[] sorted,
            out string cycleHint);

        Assert.IsFalse(ok);
        Assert.IsNotNull(cycleHint);
        Assert.IsEmpty(sorted);
    }

    [Test]
    public void SetsEqual_IgnoresOrderAndCasing()
    {
        bool equal = BundleDependencyTopology.SetsEqual(
            new[] { "UI.bundle", "Atlas.bundle" },
            new[] { "atlas.bundle", "ui.bundle" });

        Assert.IsTrue(equal);
    }

    static int IndexOf(string[] array, string value)
    {
        for (int i = 0; i < array.Length; i++)
        {
            if (string.Equals(array[i], value, System.StringComparison.OrdinalIgnoreCase))
                return i;
        }

        return -1;
    }
}
