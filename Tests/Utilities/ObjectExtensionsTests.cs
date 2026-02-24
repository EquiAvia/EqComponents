using equiavia.components.Utilities;
using Xunit;

namespace equiavia.components.Tests.Utilities;

public class ObjectExtensionsTests
{
    private class Source
    {
        public string? Name { get; set; }
        public int Value { get; set; }
        public string? SourceOnly { get; set; }
    }

    private class Target
    {
        public string? Name { get; set; }
        public int Value { get; set; }
    }

    // ── ShallowCopyPropertiesTo ───────────────────────────────────────────────

    [Fact]
    public void ShallowCopyPropertiesTo_CopiesMatchingProperties()
    {
        var source = new Source { Name = "Alice", Value = 42 };
        var target = new Target();
        source.ShallowCopyPropertiesTo(target);
        Assert.Equal("Alice", target.Name);
        Assert.Equal(42, target.Value);
    }

    [Fact]
    public void ShallowCopyPropertiesTo_IgnoresSourceOnlyProperties()
    {
        var source = new Source { Name = "Alice", Value = 42, SourceOnly = "extra" };
        var target = new Target();
        var ex = Record.Exception(() => source.ShallowCopyPropertiesTo(target));
        Assert.Null(ex);
        Assert.Equal("Alice", target.Name);
    }

    [Fact]
    public void ShallowCopyPropertiesTo_DoesNothing_WhenTargetIsNull()
    {
        var source = new Source { Name = "Alice" };
        var ex = Record.Exception(() => source.ShallowCopyPropertiesTo(null!));
        Assert.Null(ex);
    }

    [Fact]
    public void ShallowCopyPropertiesTo_OverwritesExistingValues()
    {
        var source = new Source { Name = "New", Value = 2 };
        var target = new Target { Name = "Old", Value = 1 };
        source.ShallowCopyPropertiesTo(target);
        Assert.Equal("New", target.Name);
        Assert.Equal(2, target.Value);
    }

    // ── ShallowCopyPropertiesFrom ─────────────────────────────────────────────

    [Fact]
    public void ShallowCopyPropertiesFrom_CopiesMatchingProperties()
    {
        var source = new Source { Name = "Bob", Value = 99 };
        var target = new Target();
        target.ShallowCopyPropertiesFrom(source);
        Assert.Equal("Bob", target.Name);
        Assert.Equal(99, target.Value);
    }

    [Fact]
    public void ShallowCopyPropertiesFrom_OverwritesExistingValues()
    {
        var source = new Source { Name = "New", Value = 2 };
        var target = new Target { Name = "Old", Value = 1 };
        target.ShallowCopyPropertiesFrom(source);
        Assert.Equal("New", target.Name);
        Assert.Equal(2, target.Value);
    }

    [Fact]
    public void ShallowCopyPropertiesFrom_DoesNothing_WhenSourceIsNull()
    {
        var target = new Target { Name = "Unchanged" };
        var ex = Record.Exception(() => target.ShallowCopyPropertiesFrom(null!));
        Assert.Null(ex);
        Assert.Equal("Unchanged", target.Name);
    }
}
