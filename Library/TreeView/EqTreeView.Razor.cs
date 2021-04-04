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
        [Parameter] public EventCallback<TValue> OnItemSelected { get; set; }
        [Parameter] public EventCallback<IEnumerable<TValue>> OnItemsRemoved { get; set; }
        [Parameter] public string Height { get; set; } = "100px";
        [Parameter] public bool CompactView { get; set; } = false;
        public EqTreeItem DraggedItem { get; set; }
        [Inject]
        public TreeViewJSInterop js { get; set; }
        public TValue SelectedItem
        {
            get
            {
                return FindObjectFromDatasource(_selectedItem);
            }
        }
        protected List<EqTreeItem> _treeItems;
        protected EqTreeItem _selectedItem;


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
        public void Add(TValue Item)
        {
            Datasource.Add(Item);
            var newItem = CreateTreeItem(Item);
            if (!String.IsNullOrEmpty(newItem.ParentKey))
            {
                SetTreeItemParent(newItem,newItem.ParentKey);
            }
            _treeItems.Add(newItem);
        }
        public void Update(TValue Item)
        {
            var treeItem = FindTreeItem(Item.GetPropValue(KeyPropertyName)?.ToString());
            if (treeItem == null)
            {
                Add(Item);
                return;
            }

            var existingDatasourceItem = FindObjectFromDatasource(treeItem);
            //Update the Datasource
            existingDatasourceItem.ShallowCopyPropertiesFrom(Item);
            //Update the TreeViewItem
            treeItem.Label = Item.GetPropValue(ValuePropertyName)?.ToString();
            treeItem.ParentKey = Item.GetPropValue(ParentKeyPropertyName)?.ToString();
            SetTreeItemParent(treeItem,treeItem.ParentKey);
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

            Console.WriteLine($"Successfully removed {treeItemToRemove.Label}");
            return true;
        }
        public void Filter(string searchTerm)
        {
            var matchedItems = _treeItems.Where(i => i.Label.Contains(searchTerm)).ToList();
            foreach(var item in _treeItems)
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
            foreach(var item in matchedItems)
            {
                ShowTreeItem(item);
            }
        }
        public void SetSelectedItem(TValue item)
        {
            var treeItem = FindTreeItem(item);
            SetSelectedTreeItem(treeItem);
            ShowTreeItem(treeItem);
            js.ScrollToElement($"EQ-{treeItem.UniqueIdentifier.ToString()}");
        }
        public void ShowItem(TValue item)
        {
            var treeItem = FindTreeItem(item);
            ShowTreeItem(treeItem);
            js.ScrollToElement($"EQ-{treeItem.UniqueIdentifier.ToString()}");
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
        public void Refresh()
        {
            foreach (var treeItem in _treeItems)
            {
                if (treeItem.eqTreeViewItem != null)
                {
                    (treeItem.eqTreeViewItem as EqTreeViewItem<TValue>).Refresh();
                }
                else
                {
                    Console.WriteLine($"TreeItem {treeItem.Label} does not have an control associated with it.");
                }
            }
        }
        #endregion
        #region Helper Methods
        protected void ShowTreeItem(EqTreeItem eqTreeItem)
        {
            if (eqTreeItem == null)
            {
                return;
            }

            eqTreeItem.IsExpanded = true;
            eqTreeItem.IsVisible = true;
            ShowTreeItem(eqTreeItem.Parent);
        }

        protected async Task ItemSelected(EqTreeItem selectedItem)
        {
            SetSelectedTreeItem(selectedItem);

            var originalObject = FindObjectFromDatasource(selectedItem);

            await OnItemSelected.InvokeAsync(originalObject);
        }

        protected void SetSelectedTreeItem(EqTreeItem newSelectedItem)
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

        protected List<EqTreeItem> GenerateTreeItems(List<TValue> datasource)
        {
            if (datasource == null)
            {
                return null;
            }

            var treeItems = new List<EqTreeItem>();

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

        protected void AddChildren(EqTreeItem treeItem, IEnumerable<EqTreeItem> treeItems)
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

        protected TValue FindObjectFromDatasource(EqTreeItem treeItem)
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

        protected EqTreeItem FindTreeItem(string key)
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

        protected List<EqTreeItem> FindAllTreeItemChildren(EqTreeItem item)
        {
            var childrenList = new List<EqTreeItem>();

            return TraverseChildren(item, childrenList);
        }

        protected List<EqTreeItem> TraverseChildren(EqTreeItem item, List<EqTreeItem> childrenList)
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

        protected EqTreeItem FindTreeItem(TValue item)
        {
            var keyToSearchFor = item.GetPropValue(KeyPropertyName);
            //        var treeItem = _treeItems.FirstOrDefault(o => (item.GetPropValue(KeyPropertyName) as string) == o.Key);
            return FindTreeItem(keyToSearchFor?.ToString());
        }

        protected EqTreeItem CreateTreeItem(TValue item)
        {
            return new EqTreeItem
            {
                Key = item.GetPropValue(KeyPropertyName)?.ToString(),
                ParentKey = item.GetPropValue(ParentKeyPropertyName)?.ToString(),
                Label = item.GetPropValue(ValuePropertyName)?.ToString(),
            };
        }

        protected void SetTreeItemParent(EqTreeItem treeItem, string newParentKey)
        {
            var currentParentkey = treeItem.Parent?.Key; //We use the object key to determine if the parent object has changed.
            if (currentParentkey == newParentKey)
            {
                return; //Don't anything if the parent has not changed.
            }

            //Delink the existing parent
            DelinkTreeItemFromParent(treeItem);

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

        protected static void DelinkTreeItemFromParent(EqTreeItem treeItem)
        {
            if (!treeItem.IsRootNode)
            {
                treeItem.Parent?.Children?.Remove(treeItem);
                treeItem.Parent = null;
            }
        }
        #endregion
    }
}
