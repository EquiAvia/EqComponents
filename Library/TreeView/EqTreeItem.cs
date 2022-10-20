using System;
using System.Collections.Generic;

namespace equiavia.components.Library.TreeView
{
    public class EqTreeItem<TValue>
    {
        public string Key { get; set; }
        public string ParentKey { get; set; }
        public string Label { get; set; }
        public bool IsSelected { get; set; }
        public bool IsVisible { get; set; } = true;
        public bool IsExpanded { get; set; }
        public bool IsDisabled { get; set; }
        public Guid UniqueIdentifier { get; internal set; }
        public int Level { get; internal set; } = 0;
        public List<EqTreeItem<TValue>> Children { get; internal set; }
        public EqTreeItem<TValue> Parent { get; internal set; }
        public object eqTreeViewItem { get; set; }
        public TValue Data { get; set; }
        public bool IsRootNode
        {
            get
            {
                return String.IsNullOrEmpty(ParentKey);
            }
        }

        public bool HasChildren
        {
            get
            {
                return (Children?.Count > 0);
            }
        }

        public EqTreeItem()
        {
            UniqueIdentifier = Guid.NewGuid();
        }

        public void AddParent(EqTreeItem<TValue> parent)
        {
            Parent = parent;
            Level++;
            IncrementChildrenLevel();
        }

        public void AddChild(EqTreeItem<TValue> child)
        {
            if (Children == null)
            {
                Children = new List<EqTreeItem<TValue>>();
            }
            Children.Add(child);
            child.Level = this.Level + 1;
        }

        public void IncrementChildrenLevel()
        {
            if (Children != null)
            {
                foreach (var child in Children)
                {
                    child.Level++;
                }
            }
        }
    }
}
