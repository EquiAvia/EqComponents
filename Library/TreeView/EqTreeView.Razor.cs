using equiavia.components.Utilities;
using Microsoft.AspNetCore.Components;
using Microsoft.JSInterop;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace equiavia.components.Library.TreeView
{
    public partial class EqTreeView<TValue>
    {
        [Parameter] public List<TValue> Datasource { get; set; } = new List<TValue>();
        [Parameter] public string KeyPropertyName { get; set; } = "Id";
        [Parameter] public string ValuePropertyName { get; set; } = "Name";
        [Parameter] public string ParentKeyPropertyName { get; set; } = "ParentId";
        [Parameter] public string SelectedCSSClass { get; set; } = "selected-item";
        [Parameter] public string ItemCSSClass { get; set; } = "item";
        [Parameter] public RenderFragment NoRecordsFoundTemplate { get; set; }
        [Parameter] public RenderFragment<EqTreeItem<TValue>> ItemTemplate { get; set; }
        [Parameter] public RenderFragment<EqTreeItem<TValue>> SelectedItemTemplate { get; set; }
        [Parameter] public EventCallback<TValue> OnItemSelected { get; set; }
        [Parameter] public EventCallback<TValue> OnItemDoubleClicked { get; set; }
        [Parameter] public EventCallback<TValue> OnItemRightClicked { get; set; }
        [Parameter] public EventCallback<IEnumerable<TValue>> OnItemsRemoved { get; set; }
        [Parameter] public EventCallback<List<TValue>> DatasourceChanged { get; set; }
        [Parameter] public string Height { get; set; } = "100px";
        [Parameter] public bool CompactView { get; set; } = false;
        [Parameter] public int MaxNumOfRootNodesToDisplay { get; set; } = 100000;
        public EqTreeItem<TValue> DraggedItem { get; set; }
        [Inject]
        public TreeViewJSInterop js { get; set; }
        public TValue SelectedItem
        {
            get
            {
                return FindObjectFromDatasource(_selectedItem);
            }
        }
        protected List<EqTreeItem<TValue>> _treeItems;
        protected EqTreeItem<TValue> _selectedItem;


        #region Life Cycle Methods
        protected override Task OnParametersSetAsync()
        {
            if (_treeItems == null)
            {
                _treeItems = GenerateTreeItems(Datasource);
            }
            return base.OnParametersSetAsync();
        }
        #endregion
        #region API
        public async Task Add(TValue Item)
        {
            Datasource.Add(Item);
            var newItem = CreateTreeItem(Item);
            if (!String.IsNullOrEmpty(newItem.ParentKey))
            {
                SetTreeItemParent(newItem, newItem.ParentKey, null);
            }
            _treeItems.Add(newItem);
            await NotifyDatasourceChanged();
        }

        public async Task Update(TValue Item)
        {
            var treeItem = FindTreeItem(Item.GetPropValue(KeyPropertyName)?.ToString());
            if (treeItem == null)
            {
                await Add(Item);
                return;
            }

            var existingParentKey = treeItem.ParentKey;
            var newParentKey = Item.GetPropValue(ParentKeyPropertyName)?.ToString();
            var existingDatasourceItem = FindObjectFromDatasource(treeItem);

            //Update the Datasource
            existingDatasourceItem.ShallowCopyPropertiesFrom(Item);
            //Update the TreeViewItem
            treeItem.Label = Item.GetPropValue(ValuePropertyName)?.ToString();

            SetTreeItemParent(treeItem, newParentKey, existingParentKey);
            await NotifyDatasourceChanged();
        }
        public async Task<bool> Remove(TValue datasourceItemToDelete)
        {
            if (datasourceItemToDelete == null)
            {
                return false;
            }

            var eventItemsToRemove = new List<TValue>();
            //Find all the tree items that need to be removed
            var treeItemToRemove = FindTreeItem(datasourceItemToDelete);
            //This list includes the item to delete as well.
            var treeItemsToRemove = FindAllTreeItemChildren(treeItemToRemove);

            //Remove the child items from the datasource and treeItems
            foreach (var treeitem in treeItemsToRemove)
            {
                //Remove the child from the parent treeitem.
                DelinkTreeItemFromParent(treeitem);
                _treeItems.Remove(treeitem);

                //Remove the item from Original Datasource
                var childItemToRemove = FindObjectFromDatasource(treeitem);
                eventItemsToRemove.Add(childItemToRemove);
                Datasource.Remove(childItemToRemove);
            }

            await OnItemsRemoved.InvokeAsync(eventItemsToRemove);
            await NotifyDatasourceChanged();
            Console.WriteLine($"Successfully removed {treeItemToRemove.Label}");
            return true;
        }

        public void Filter(string searchTerm, bool caseSensitive = false)
        {
            List<EqTreeItem<TValue>> matchedItems = null;
            if (caseSensitive)
            {
                matchedItems = _treeItems.Where(i => i.Label.Contains(searchTerm)).ToList();

            }
            else
            {
                matchedItems = _treeItems.Where(i => i.Label.ToLower().Contains(searchTerm.ToLower())).ToList();
            }

            FilterTreeItems(matchedItems);
        }

        public async Task SetSelectedItem(TValue item)
        {
            var treeItem = FindTreeItem(item);
            if (treeItem != null)
            {
                SetSelectedTreeItem(treeItem);
                ShowTreeItem(treeItem);
                await js.ScrollToElement($"EQ-{treeItem?.UniqueIdentifier.ToString()}");
            }
        }
        public async Task ShowItem(TValue item)
        {
            var treeItem = FindTreeItem(item);
            ShowTreeItem(treeItem);
            await js.ScrollToElement($"EQ-{treeItem.UniqueIdentifier.ToString()}");
        }
        public void ExpandAll()
        {
            foreach (var treeItem in _treeItems)
            {
                treeItem.IsExpanded = true;
            }
            Console.WriteLine("Expand All");
        }
        public void CollapseAll()
        {
            foreach (var treeItem in _treeItems)
            {
                treeItem.IsExpanded = false;
            }
            Console.WriteLine("Collapse All");
        }
        public async Task Refresh(List<TValue> newDatasource)
        {
            Datasource = newDatasource;
            //Rebuild the Tree
            _treeItems = GenerateTreeItems(Datasource);
            await NotifyDatasourceChanged();
        }
        #endregion
        #region Helper Methods
        protected void ShowTreeItem(EqTreeItem<TValue> eqTreeItem)
        {
            if (eqTreeItem == null)
            {
                return;
            }

            eqTreeItem.IsExpanded = true;
            eqTreeItem.IsVisible = true;
            ShowTreeItem(eqTreeItem.Parent);
        }

        protected async Task ItemSelected(EqTreeItem<TValue> selectedItem)
        {
            SetSelectedTreeItem(selectedItem);

            var originalObject = FindObjectFromDatasource(selectedItem);

            await OnItemSelected.InvokeAsync(originalObject);
        }

        protected async Task ItemRightClicked(EqTreeItem<TValue> selectedItem)
        {
            SetSelectedTreeItem(selectedItem);

            var originalObject = FindObjectFromDatasource(selectedItem);

            await OnItemRightClicked.InvokeAsync(originalObject);
        }

        protected async Task ItemDoubleClicked(EqTreeItem<TValue> selectedItem)
        {
            SetSelectedTreeItem(selectedItem);

            var originalObject = FindObjectFromDatasource(selectedItem);

            await OnItemDoubleClicked.InvokeAsync(originalObject);
        }

        protected void SetSelectedTreeItem(EqTreeItem<TValue> newSelectedItem)
        {
            if (_selectedItem != null)
            {
                _selectedItem.IsSelected = false; //Notify the previously selected item that it has been deselected.
            }
            _selectedItem = newSelectedItem;
            if (_selectedItem != null)
            {
                _selectedItem.IsSelected = true;
            }
        }

        protected List<EqTreeItem<TValue>> GenerateTreeItems(List<TValue> datasource)
        {
            if (datasource == null)
            {
                return null;
            }

            var treeItems = new List<EqTreeItem<TValue>>();

            foreach (var item in datasource)
            {
                treeItems.Add(CreateTreeItem(item));
            }

            foreach (var treeItem in treeItems.Where(t => t.IsRootNode))
            {
                AddChildren(treeItem, treeItems);
            }

            return treeItems;
        }

        protected void AddChildren(EqTreeItem<TValue> treeItem, IEnumerable<EqTreeItem<TValue>> treeItems)
        {
            //Find all this nodes children
            var nodeChildren = treeItems.Where(c => c.ParentKey == treeItem.Key).ToList();

            //If node exit
            if (nodeChildren.Count == 0)
            {
                return;
            }

            //Add the children.
            foreach (var child in nodeChildren)
            {
                child.Parent = treeItem;
                treeItem.AddChild(child);
                //Call each child to add their children
                AddChildren(child, treeItems);
            }

        }

        protected TValue FindObjectFromDatasource(EqTreeItem<TValue> treeItem)
        {
            if (treeItem == null)
            {
                return default(TValue);
            }

            foreach (var item in Datasource)
            {
                if (treeItem.Key == item.GetPropValue(KeyPropertyName)?.ToString())
                {
                    return item;
                }
            }
            return default(TValue);
        }

        protected EqTreeItem<TValue> FindTreeItem(string key)
        {
            foreach (var treeItem in _treeItems)
            {
                if (treeItem.Key == key)
                {
                    return treeItem;
                }
            }
            return null;
        }

        protected List<EqTreeItem<TValue>> FindAllTreeItemChildren(EqTreeItem<TValue> item)
        {
            var childrenList = new List<EqTreeItem<TValue>>();

            return TraverseChildren(item, childrenList);
        }

        protected void FilterTreeItems(List<EqTreeItem<TValue>> matchedItems)
        {
            foreach (var item in _treeItems)
            {
                if (!matchedItems.Any(i => i.Key == item.Key))
                {
                    item.IsVisible = false;
                    item.IsSelected = false;
                }
                else
                {
                    item.IsVisible = true;
                }
            }
            foreach (var item in matchedItems)
            {
                ShowTreeItem(item);
            }
        }

        protected List<EqTreeItem<TValue>> TraverseChildren(EqTreeItem<TValue> item, List<EqTreeItem<TValue>> childrenList)
        {
            if (item.HasChildren)
            {
                foreach (var child in item.Children)
                {
                    TraverseChildren(child, childrenList);
                }
            }

            childrenList.Add(item);

            return childrenList;
        }

        protected EqTreeItem<TValue> FindTreeItem(TValue item)
        {
            var keyToSearchFor = item.GetPropValue(KeyPropertyName);
            //        var treeItem = _treeItems.FirstOrDefault(o => (item.GetPropValue(KeyPropertyName) as string) == o.Key);
            return FindTreeItem(keyToSearchFor?.ToString());
        }

        protected EqTreeItem<TValue> CreateTreeItem(TValue item)
        {
            return new EqTreeItem<TValue>
            {
                Key = item.GetPropValue(KeyPropertyName)?.ToString(),
                ParentKey = item.GetPropValue(ParentKeyPropertyName)?.ToString(),
                Label = item.GetPropValue(ValuePropertyName)?.ToString(),
                Data = item
            };
        }

        protected void SetTreeItemParent(EqTreeItem<TValue> treeItem, string newParentKey, string currentParentkey = null)
        {
            if (currentParentkey == newParentKey)
            {
                return; //Don't anything if the parent has not changed.
            }

            //Delink the existing parent
            DelinkTreeItemFromParent(treeItem);
            treeItem.ParentKey = newParentKey;

            if (newParentKey == null)
            {
                return; //The item is being moved to root.
            }

            var parent = FindTreeItem(newParentKey);
            if (parent == null)
            {
                throw new ApplicationException($"The {ParentKeyPropertyName} : {newParentKey} does not refer to a valid item and could not be set as a parent for item {treeItem.Label}");
            }

            treeItem.AddParent(parent);
            treeItem.Parent.AddChild(treeItem);
        }

        protected static void DelinkTreeItemFromParent(EqTreeItem<TValue> treeItem)
        {
            if (!treeItem.IsRootNode)
            {
                treeItem.Parent?.Children?.Remove(treeItem);
                treeItem.Parent = null;
            }
        }

        protected async Task NotifyDatasourceChanged()
        {
            await DatasourceChanged.InvokeAsync(Datasource);
        }
        #endregion
    }
}
