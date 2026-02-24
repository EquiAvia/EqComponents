using Bunit;
using equiavia.components.Library;
using equiavia.components.Library.TreeView;
using equiavia.components.Shared;
using Microsoft.AspNetCore.Components;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.JSInterop;
using System.Reflection;
using Xunit;

namespace equiavia.components.Tests.Library;

public class EqTreeViewTests : BunitContext
{
    // TreeViewJSInterop only implements IAsyncDisposable; bUnit disposes services
    // synchronously, so we add a no-op IDisposable implementation for tests.
    private sealed class TestJSInterop : TreeViewJSInterop, IDisposable
    {
        public TestJSInterop(IJSRuntime jsRuntime) : base(jsRuntime) { }
        public void Dispose() { }
    }

    // ── Setup ─────────────────────────────────────────────────────────────────

    public EqTreeViewTests()
    {
        JSInterop.Mode = JSRuntimeMode.Loose;
        // Handle the lazy JS module load that TreeViewJSInterop triggers
        JSInterop.SetupModule("./_content/equiavia.components.Library/TreeViewJsInterop.js")
                 .Setup<bool>("scrollToElementId").SetResult(true);
        Services.AddScoped<TreeViewJSInterop, TestJSInterop>();
    }

    private static List<Employee> BasicEmployees() => new()
    {
        new Employee { Id = 1, Name = "CEO",      ParentId = null },
        new Employee { Id = 2, Name = "Manager",  ParentId = 1   },
        new Employee { Id = 3, Name = "Employee", ParentId = 2   },
    };

    private IRenderedComponent<EqTreeView<Employee>> RenderWithData(List<Employee> data) =>
        Render<EqTreeView<Employee>>(p => p.Add(x => x.Datasource, data));

    /// <summary>Accesses the protected _treeItems field via reflection.</summary>
    private static List<EqTreeItem<Employee>>? TreeItems(EqTreeView<Employee> instance)
    {
        var field = typeof(EqTreeView<Employee>)
            .GetField("_treeItems", BindingFlags.NonPublic | BindingFlags.Instance);
        return field?.GetValue(instance) as List<EqTreeItem<Employee>>;
    }

    // ── Rendering ─────────────────────────────────────────────────────────────

    [Fact]
    public void Render_WithNullDatasource_DoesNotRenderContainer()
    {
        var cut = Render<EqTreeView<Employee>>(
            p => p.Add(x => x.Datasource, (List<Employee>)null!));
        Assert.DoesNotContain("treeview-container", cut.Markup);
    }

    [Fact]
    public void Render_WithData_ShowsTreeviewContainer()
    {
        var cut = RenderWithData(BasicEmployees());
        Assert.Contains("treeview-container", cut.Markup);
    }

    [Fact]
    public void Render_EmptyDatasource_ShowsNoRecordsFoundTemplate()
    {
        var cut = Render<EqTreeView<Employee>>(p => p
            .Add(x => x.Datasource, new List<Employee>())
            .Add(x => x.NoRecordsFoundTemplate, (RenderFragment)(b =>
                b.AddMarkupContent(0, "<div>No Records</div>"))));
        Assert.Contains("No Records", cut.Markup);
    }

    // ── Add ───────────────────────────────────────────────────────────────────

    [Fact]
    public async Task Add_NewRootItem_AppearsInDatasource()
    {
        var employees = BasicEmployees();
        var cut = RenderWithData(employees);
        var newEmp = new Employee { Id = 10, Name = "Intern", ParentId = null };

        await cut.Instance.Add(newEmp);

        Assert.Contains(newEmp, cut.Instance.Datasource);
    }

    [Fact]
    public async Task Add_NewRootItem_IsRootNodeInTree()
    {
        var employees = BasicEmployees();
        var cut = RenderWithData(employees);

        await cut.Instance.Add(new Employee { Id = 10, Name = "NewRoot", ParentId = null });

        var items = TreeItems(cut.Instance);
        Assert.Contains(items!, i => i.Label == "NewRoot" && i.IsRootNode);
    }

    [Fact]
    public async Task Add_NewChildItem_AppearsUnderCorrectParent()
    {
        var employees = BasicEmployees();
        var cut = RenderWithData(employees);

        await cut.Instance.Add(new Employee { Id = 10, Name = "Intern", ParentId = 1 });

        var items = TreeItems(cut.Instance);
        var ceo = items!.First(i => i.Key == "1");
        Assert.Contains(ceo.Children, c => c.Label == "Intern");
    }

    // ── Remove ────────────────────────────────────────────────────────────────

    [Fact]
    public async Task Remove_ExistingItem_ReturnsTrueAndRemovesFromDatasource()
    {
        var employees = BasicEmployees();
        var cut = RenderWithData(employees);
        var ceo = employees.First(e => e.Name == "CEO");

        var result = await cut.Instance.Remove(ceo);

        Assert.True(result);
        Assert.DoesNotContain(employees, e => e.Name == "CEO");
    }

    [Fact]
    public async Task Remove_ParentItem_RemovesAllDescendants()
    {
        var employees = BasicEmployees();
        var cut = RenderWithData(employees);
        var ceo = employees.First(e => e.Name == "CEO");

        await cut.Instance.Remove(ceo);

        // CEO + Manager + Employee all removed
        Assert.Empty(employees);
    }

    [Fact]
    public async Task Remove_LeafItem_DoesNotRemoveParent()
    {
        var employees = BasicEmployees();
        var cut = RenderWithData(employees);
        var leaf = employees.First(e => e.Name == "Employee");

        await cut.Instance.Remove(leaf);

        Assert.Contains(employees, e => e.Name == "CEO");
        Assert.Contains(employees, e => e.Name == "Manager");
        Assert.DoesNotContain(employees, e => e.Name == "Employee");
    }

    [Fact]
    public async Task Remove_NullItem_ReturnsFalse()
    {
        var cut = RenderWithData(BasicEmployees());
        var result = await cut.Instance.Remove(null!);
        Assert.False(result);
    }

    // ── Update ────────────────────────────────────────────────────────────────

    [Fact]
    public async Task Update_Item_UpdatesTreeLabel()
    {
        var employees = BasicEmployees();
        var cut = RenderWithData(employees);

        await cut.Instance.Update(new Employee { Id = 1, Name = "Chief Executive", ParentId = null });

        Assert.Contains(TreeItems(cut.Instance)!, i => i.Label == "Chief Executive");
    }

    [Fact]
    public async Task Update_Item_UpdatesDatasourceObject()
    {
        var employees = BasicEmployees();
        var cut = RenderWithData(employees);

        await cut.Instance.Update(new Employee { Id = 3, Name = "Senior Employee", ParentId = 2 });

        Assert.Contains(employees, e => e.Name == "Senior Employee");
    }

    [Fact]
    public async Task Update_Item_ReparentsToNewParent()
    {
        var employees = BasicEmployees();
        var cut = RenderWithData(employees);

        // Move Employee (Id=3) from Manager (Id=2) to CEO (Id=1)
        await cut.Instance.Update(new Employee { Id = 3, Name = "Employee", ParentId = 1 });

        var ceo = TreeItems(cut.Instance)!.First(i => i.Key == "1");
        Assert.Contains(ceo.Children, c => c.Label == "Employee");
    }

    // ── Filter ────────────────────────────────────────────────────────────────

    [Fact]
    public void Filter_HidesNonMatchingItems()
    {
        var cut = RenderWithData(BasicEmployees());

        cut.Instance.Filter("CEO", false);

        var items = TreeItems(cut.Instance)!;
        Assert.True(items.First(i => i.Label == "CEO").IsVisible);
        Assert.False(items.First(i => i.Label == "Manager").IsVisible);
    }

    [Fact]
    public void Filter_CaseInsensitive_MatchesRegardlessOfCase()
    {
        var cut = RenderWithData(BasicEmployees());

        cut.Instance.Filter("ceo", false);

        Assert.True(TreeItems(cut.Instance)!.First(i => i.Label == "CEO").IsVisible);
    }

    [Fact]
    public void Filter_CaseSensitive_DoesNotMatchWrongCase()
    {
        var cut = RenderWithData(BasicEmployees());

        cut.Instance.Filter("ceo", caseSensitive: true);

        Assert.False(TreeItems(cut.Instance)!.First(i => i.Label == "CEO").IsVisible);
    }

    [Fact]
    public void Filter_EmptyTerm_ShowsAllItems()
    {
        var cut = RenderWithData(BasicEmployees());
        cut.Instance.Filter("CEO", false);

        cut.Instance.Filter("", false);

        Assert.True(TreeItems(cut.Instance)!.All(i => i.IsVisible));
    }

    // ── ExpandAll / CollapseAll ───────────────────────────────────────────────

    [Fact]
    public void ExpandAll_SetsAllItemsExpanded()
    {
        var cut = RenderWithData(BasicEmployees());

        cut.Instance.ExpandAll();

        Assert.True(TreeItems(cut.Instance)!.All(i => i.IsExpanded));
    }

    [Fact]
    public void CollapseAll_SetsAllItemsCollapsed()
    {
        var cut = RenderWithData(BasicEmployees());
        cut.Instance.ExpandAll();

        cut.Instance.CollapseAll();

        Assert.True(TreeItems(cut.Instance)!.All(i => !i.IsExpanded));
    }

    // ── Refresh ───────────────────────────────────────────────────────────────

    [Fact]
    public async Task Refresh_ReplacesDatasourceAndRebuildsTree()
    {
        var cut = RenderWithData(BasicEmployees());
        var newData = new List<Employee>
        {
            new Employee { Id = 99, Name = "OnlyPerson", ParentId = null }
        };

        await cut.Instance.Refresh(newData);

        Assert.Equal(newData, cut.Instance.Datasource);
        var items = TreeItems(cut.Instance)!;
        Assert.Single(items);
        Assert.Equal("OnlyPerson", items[0].Label);
    }

    // ── SelectedItem ──────────────────────────────────────────────────────────

    [Fact]
    public void SelectedItem_IsNullInitially()
    {
        var cut = RenderWithData(BasicEmployees());
        Assert.Null(cut.Instance.SelectedItem);
    }

    [Fact]
    public async Task SetSelectedItem_UpdatesSelectedItemProperty()
    {
        var employees = BasicEmployees();
        var cut = RenderWithData(employees);
        var manager = employees.First(e => e.Name == "Manager");

        await cut.Instance.SetSelectedItem(manager);

        Assert.Equal(manager, cut.Instance.SelectedItem);
    }
}
