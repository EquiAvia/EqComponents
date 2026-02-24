using equiavia.components.Library.TreeView;
using equiavia.components.Shared;
using Xunit;

namespace equiavia.components.Tests.Library;

public class EqTreeItemTests
{
    // ── IsRootNode ────────────────────────────────────────────────────────────

    [Fact]
    public void IsRootNode_ReturnsTrue_WhenParentKeyIsNull()
    {
        var item = new EqTreeItem<Employee> { ParentKey = null };
        Assert.True(item.IsRootNode);
    }

    [Fact]
    public void IsRootNode_ReturnsTrue_WhenParentKeyIsEmpty()
    {
        var item = new EqTreeItem<Employee> { ParentKey = "" };
        Assert.True(item.IsRootNode);
    }

    [Fact]
    public void IsRootNode_ReturnsFalse_WhenParentKeyIsSet()
    {
        var item = new EqTreeItem<Employee> { ParentKey = "1" };
        Assert.False(item.IsRootNode);
    }

    // ── HasChildren ───────────────────────────────────────────────────────────

    [Fact]
    public void HasChildren_ReturnsFalse_WhenNoChildrenAdded()
    {
        var item = new EqTreeItem<Employee>();
        Assert.False(item.HasChildren);
    }

    [Fact]
    public void HasChildren_ReturnsTrue_AfterChildAdded()
    {
        var parent = new EqTreeItem<Employee> { Key = "1" };
        var child = new EqTreeItem<Employee> { Key = "2" };
        parent.AddChild(child);
        Assert.True(parent.HasChildren);
    }

    // ── AddChild ──────────────────────────────────────────────────────────────

    [Fact]
    public void AddChild_AddsChildToChildrenList()
    {
        var parent = new EqTreeItem<Employee> { Key = "1" };
        var child = new EqTreeItem<Employee> { Key = "2" };
        parent.AddChild(child);
        Assert.Contains(child, parent.Children);
    }

    [Fact]
    public void AddChild_SetsChildLevelToParentLevelPlusOne()
    {
        var parent = new EqTreeItem<Employee> { Key = "1" }; // Level = 0
        var child = new EqTreeItem<Employee> { Key = "2" };
        parent.AddChild(child);
        Assert.Equal(1, child.Level);
    }

    [Fact]
    public void AddChild_AllowsMultipleChildren()
    {
        var parent = new EqTreeItem<Employee> { Key = "1" };
        parent.AddChild(new EqTreeItem<Employee> { Key = "2" });
        parent.AddChild(new EqTreeItem<Employee> { Key = "3" });
        Assert.Equal(2, parent.Children.Count);
    }

    [Fact]
    public void AddChild_InitialisesChildrenList_WhenFirstChildAdded()
    {
        var parent = new EqTreeItem<Employee> { Key = "1" };
        Assert.Null(parent.Children);
        parent.AddChild(new EqTreeItem<Employee> { Key = "2" });
        Assert.NotNull(parent.Children);
    }

    // ── AddParent ─────────────────────────────────────────────────────────────

    [Fact]
    public void AddParent_SetsParentReference()
    {
        var parent = new EqTreeItem<Employee> { Key = "1" };
        var child = new EqTreeItem<Employee> { Key = "2" };
        child.AddParent(parent);
        Assert.Equal(parent, child.Parent);
    }

    [Fact]
    public void AddParent_IncrementsChildLevel()
    {
        var parent = new EqTreeItem<Employee> { Key = "1" }; // Level 0
        var child = new EqTreeItem<Employee> { Key = "2" };  // Level 0
        child.AddParent(parent);
        Assert.Equal(1, child.Level);
    }

    // ── IncrementChildrenLevel ────────────────────────────────────────────────

    [Fact]
    public void IncrementChildrenLevel_IncrementsAllChildLevels()
    {
        var parent = new EqTreeItem<Employee> { Key = "1" };
        var child1 = new EqTreeItem<Employee> { Key = "2" };
        var child2 = new EqTreeItem<Employee> { Key = "3" };
        parent.AddChild(child1); // child1.Level = 1
        parent.AddChild(child2); // child2.Level = 1
        parent.IncrementChildrenLevel();
        Assert.Equal(2, child1.Level);
        Assert.Equal(2, child2.Level);
    }

    [Fact]
    public void IncrementChildrenLevel_DoesNotThrow_WhenNoChildren()
    {
        var item = new EqTreeItem<Employee>();
        var ex = Record.Exception(() => item.IncrementChildrenLevel());
        Assert.Null(ex);
    }

    // ── Default state ─────────────────────────────────────────────────────────

    [Fact]
    public void NewItem_IsVisibleByDefault()
    {
        Assert.True(new EqTreeItem<Employee>().IsVisible);
    }

    [Fact]
    public void NewItem_IsNotExpandedByDefault()
    {
        Assert.False(new EqTreeItem<Employee>().IsExpanded);
    }

    [Fact]
    public void NewItem_IsNotSelectedByDefault()
    {
        Assert.False(new EqTreeItem<Employee>().IsSelected);
    }

    [Fact]
    public void NewItem_IsNotDisabledByDefault()
    {
        Assert.False(new EqTreeItem<Employee>().IsDisabled);
    }

    [Fact]
    public void NewItem_HasNonEmptyUniqueIdentifier()
    {
        Assert.NotEqual(Guid.Empty, new EqTreeItem<Employee>().UniqueIdentifier);
    }

    [Fact]
    public void TwoNewItems_HaveDifferentUniqueIdentifiers()
    {
        var a = new EqTreeItem<Employee>();
        var b = new EqTreeItem<Employee>();
        Assert.NotEqual(a.UniqueIdentifier, b.UniqueIdentifier);
    }

    [Fact]
    public void NewItem_HasNullChildrenList()
    {
        var item = new EqTreeItem<Employee>();
        Assert.Null(item.Children);
        Assert.False(item.HasChildren);
    }
}
