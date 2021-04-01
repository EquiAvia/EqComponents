﻿using System;
using System.Collections.Generic;

namespace equiavia.components.Library.TreeView
{
    public class EqTreeItem
    {
        public string Key { get; set; }
        public string ParentKey { get; set; }
        public string Label { get; set; }
        public bool IsSelected { get; set; }
        public bool IsExpanded { get; set; }
        public bool IsDisabled { get; set; }
        public Guid UniqueIdentifier { get; internal set; }
        public int Level { get; internal set; } = 0;
        public List<EqTreeItem> Children { get; internal set; }
        public EqTreeItem Parent { get; internal set; }
        public EqTreeViewItem eqTreeViewItem { get; set; }
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

        public void AddParent(EqTreeItem parent)
        {
            Parent = parent;
            Level++;
            IncrementChildrenLevel();
        }

        public void AddChild(EqTreeItem child)
        {
            if (Children == null)
            {
                Children = new List<EqTreeItem>();
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