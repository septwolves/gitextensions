﻿using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Forms;
using GitCommands;
using GitUI.BranchTreePanel.Interfaces;
using GitUI.CommandsDialogs;
using GitUI.Properties;
using GitUIPluginInterfaces;
using Microsoft.VisualStudio.Threading;
using ResourceManager;

namespace GitUI.BranchTreePanel
{
    partial class RepoObjectsTree
    {
        private class TagNode : BaseBranchNode, IGitRefActions, ICanDelete
        {
            private readonly IGitRef _tagInfo;

            public TagNode(Tree tree, string fullPath, IGitRef tagInfo) : base(tree, fullPath)
            {
                _tagInfo = tagInfo;
            }

            internal override void OnSelected()
            {
                if (Tree.IgnoreSelectionChangedEvent)
                {
                    return;
                }

                base.OnSelected();
                SelectRevision();
            }

            internal override void OnDoubleClick()
            {
                CreateBranch();
            }

            public bool CreateBranch()
            {
                return UICommands.StartCreateBranchDialog(TreeViewNode.TreeView, _tagInfo.ObjectId);
            }

            public bool Delete()
            {
                return UICommands.StartDeleteTagDialog(TreeViewNode.TreeView, _tagInfo.Name);
            }

            public bool Merge()
            {
                return UICommands.StartMergeBranchDialog(TreeViewNode.TreeView, FullPath);
            }

            protected override void ApplyStyle()
            {
                base.ApplyStyle();
                TreeViewNode.ImageKey = TreeViewNode.SelectedImageKey = nameof(Images.TagHorizontal);
            }

            public bool Checkout()
            {
                using (var form = new FormCheckoutRevision(UICommands))
                {
                    form.SetRevision(FullPath);
                    return form.ShowDialog(TreeViewNode.TreeView) != DialogResult.Cancel;
                }
            }
        }

        private sealed class TagTree : Tree
        {
            public TagTree(TreeNode treeNode, IGitUICommandsSource uiCommands)
                : base(treeNode, uiCommands)
            {
            }

            protected override void PostRepositoryChanged()
            {
                RefreshTreeAsync().FileAndForget();
            }

            public override async Task RefreshTreeAsync()
            {
                await ReloadNodesAsync(LoadNodesAsync);
            }

            private async Task LoadNodesAsync(CancellationToken token)
            {
                await TaskScheduler.Default;
                token.ThrowIfCancellationRequested();
                FillTagTree(Module.GetTagRefs(GitModule.GetTagRefsSortOrder.ByName), token);
            }

            private void FillTagTree(IEnumerable<IGitRef> tags, CancellationToken token)
            {
                var nodes = new Dictionary<string, BaseBranchNode>();
                foreach (var tag in tags)
                {
                    token.ThrowIfCancellationRequested();
                    var branchNode = new TagNode(this, tag.Name, tag);
                    var parent = branchNode.CreateRootNode(nodes,
                        (tree, parentPath) => new BasePathNode(tree, parentPath));
                    if (parent != null)
                    {
                        Nodes.AddNode(parent);
                    }
                }
            }

            protected override void PostFillTreeViewNode(CancellationToken token, bool firstTime)
            {
                if (firstTime)
                {
                    TreeViewNode.Collapse();
                }

                TreeViewNode.Text = $@"{Strings.Tags} ({Nodes.Count})";
            }
        }
    }
}
