﻿using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading.Tasks;
using Statiq.Common;

namespace Statiq.Core
{
    /// <summary>
    /// Adds metadata to the input documents that describes the position of each one in a tree structure.
    /// </summary>
    /// <remarks>
    /// <para>
    /// By default, this module is configured to generate a tree that mimics the directory structure of each document's source path.
    /// Any documents with a file name of "index.*" are automatically
    /// promoted to the node that represents the parent folder level. For any folder that does not contain an "index.*" file,
    /// an empty placeholder tree node is used to represent the folder.
    /// </para>
    /// <para>
    /// Note that if you clone documents from the tree, the relationships of the cloned document (parent, child, etc.)
    /// will not be updated to the new clones. In other words, your new document will still be pointing to the old
    /// versions of it's parent, children, etc. To update the tree after cloning documents you will need to recreate it
    /// by rerunning this module on all the newly created documents again.
    /// </para>
    /// </remarks>
    /// <metadata cref="Keys.Children" usage="Output"/>
    /// <metadata cref="Keys.TreePath" usage="Output"/>
    /// <category>Metadata</category>
    public class CreateTree : Module
    {
        private static readonly ReadOnlyMemory<char> IndexFileName = "index.".AsMemory();

        private Config<bool> _isRoot;
        private Config<string[]> _treePath;
        private Func<string[], MetadataItems, IExecutionContext, IDocument> _placeholderFactory;
        private Comparison<IDocument> _sort;
        private bool _collapseRoot = false;
        private bool _nesting = false;

        private string _childrenKey = Keys.Children;
        private string _treePathKey = Keys.TreePath;

        /// <summary>
        /// Creates a new tree module.
        /// </summary>
        public CreateTree()
        {
            _isRoot = false;
            _treePath = Config.FromDocument((doc, ctx) =>
            {
                // Attempt to get the segments from the source path and then the destination path
                ReadOnlyMemory<char>[] segments =
                    doc.Source?.GetRelativeInputPath(ctx)?.Segments
                        ?? doc.Destination?.GetRelativeInputPath(ctx)?.Segments;
                if (segments == null)
                {
                    return null;
                }

                // Promote "index." pages up a level
                if (segments.Length > 0 && segments[segments.Length - 1].StartsWith(IndexFileName))
                {
                    return segments.Take(segments.Length - 1).Select(x => x.ToString()).ToArray();
                }
                return segments.Select(x => x.ToString()).ToArray();
            });
            _placeholderFactory = (treePath, items, context) =>
            {
                FilePath source = new FilePath(string.Join("/", treePath.Concat(new[] { "index.html" })));
                return context.CreateDocument(context.FileSystem.GetInputFile(source).Path.FullPath, source, items);
            };
            _sort = (x, y) => Comparer.Default.Compare(
                x.Get<object[]>(Keys.TreePath)?.LastOrDefault(),
                y.Get<object[]>(Keys.TreePath)?.LastOrDefault());
        }

        /// <summary>
        /// Allows you to specify a factory function for the creation of placeholder documents which get
        /// created to represent nodes in the tree for which there was no input document. The factory
        /// gets passed the current tree path, the set of tree metadata that should be set in the document,
        /// and the execution context which can be used to create a new document. If the factory function
        /// returns null, a new document with the tree metadata is created.
        /// </summary>
        /// <remarks>
        /// The default placeholder factory creates a document at the current tree path with a file name of <c>index.html</c>.
        /// </remarks>
        /// <param name="factory">The factory function.</param>
        /// <returns>The current module instance.</returns>
        public CreateTree WithPlaceholderFactory(Func<string[], MetadataItems, IExecutionContext, IDocument> factory)
        {
            _placeholderFactory = factory ?? throw new ArgumentNullException(nameof(factory));
            return this;
        }

        /// <summary>
        /// This specifies how the children of a given tree node should be sorted. The default behavior is to
        /// sort based on the string value of the last component of the child node's tree path (I.e., the folder
        /// or file name). The output document for each tree node is used as the input to the sort delegate.
        /// </summary>
        /// <param name="sort">A comparison delegate.</param>
        /// <returns>The current module instance.</returns>
        public CreateTree WithSort(Comparison<IDocument> sort)
        {
            _sort = sort ?? throw new ArgumentNullException(nameof(sort));
            return this;
        }

        /// <summary>
        /// Specifies for each document if it is a root of a tree. This results in splitting the generated tree into multiple smaller ones,
        /// removing the root node from the set of children of it's parent and setting it's parent to <c>null</c>.
        /// </summary>
        /// <param name="isRoot">A predicate (must return <c>bool</c>) that specifies if the current document is treated as the root of a new tree.</param>
        /// <returns>The current module instance.</returns>
        public CreateTree WithRoots(Config<bool> isRoot)
        {
            _isRoot = isRoot ?? throw new ArgumentNullException(nameof(isRoot));
            return this;
        }

        /// <summary>
        /// Defines the structure of the tree. If the delegate returns <c>null</c> the document
        /// is excluded from the tree.
        /// </summary>
        /// <param name="treePath">A delegate that must return a sequence of strings.</param>
        /// <returns>The current module instance.</returns>
        public CreateTree WithTreePath(Config<string[]> treePath)
        {
            _treePath = treePath ?? throw new ArgumentNullException(nameof(treePath));
            return this;
        }

        /// <summary>
        /// Changes the default children metadata key.
        /// </summary>
        /// <param name="childrenKey">The metadata key where child documents should be stored.</param>
        /// <returns>The current module instance.</returns>
        public CreateTree WithChildrenKey(string childrenKey = Keys.Children)
        {
            _childrenKey = childrenKey;
            return this;
        }

        /// <summary>
        /// Changes the default tree path metadata key.
        /// </summary>
        /// <param name="treePathKey">The metadata key where the tree path should be stored.</param>
        /// <returns>The current module instance.</returns>
        public CreateTree WithTreePathKey(string treePathKey = Keys.TreePath)
        {
            _treePathKey = treePathKey;
            return this;
        }

        /// <summary>
        /// Indicates that the module should only output root nodes (instead of all
        /// nodes which is the default behavior).
        /// </summary>
        /// <param name="nesting"><c>true</c> to enable nesting and only output the root nodes.</param>
        /// <param name="collapseRoot">
        /// Indicates that the root of the tree should be collapsed and the module
        /// should output first-level documents as if they were root documents. This setting
        /// has no effect if not nesting.
        /// </param>
        /// <returns>The current module instance.</returns>
        public CreateTree WithNesting(bool nesting = true, bool collapseRoot = false)
        {
            _nesting = nesting;
            _collapseRoot = collapseRoot;
            return this;
        }

        /// <inheritdoc />
        public override async Task<IEnumerable<IDocument>> ExecuteAsync(IExecutionContext context)
        {
            // Create a dictionary of tree nodes
            TreeNodeEqualityComparer treeNodeEqualityComparer = new TreeNodeEqualityComparer();
            Dictionary<string[], TreeNode> nodesDictionary = await context.Inputs
                .ToAsyncEnumerable()
                .SelectAwait(async input => new TreeNode(await _treePath.GetValueAsync(input, context), input))
                .Where(x => x.TreePath != null)
                .Distinct(treeNodeEqualityComparer)
                .ToDictionaryAsync(x => x.TreePath, new TreePathEqualityComparer());

            // Add links between parent and children (creating empty tree nodes as needed)
            Queue<TreeNode> nodesToProcess = new Queue<TreeNode>(nodesDictionary.Values);
            while (nodesToProcess.Count > 0)
            {
                TreeNode node = nodesToProcess.Dequeue();

                // Skip root nodes
                if (node.TreePath.Length == 0
                    || (node.InputDocument != null && await _isRoot.GetValueAsync(node.InputDocument, context)))
                {
                    continue;
                }

                // Skip the root node if not nesting or if collapsing the root
                string[] parentTreePath = node.GetParentTreePath();
                if (parentTreePath.Length == 0 && (!_nesting || _collapseRoot))
                {
                    continue;
                }

                // Find (or create) the parent
                if (!nodesDictionary.TryGetValue(parentTreePath, out TreeNode parent))
                {
                    parent = new TreeNode(parentTreePath);
                    nodesDictionary.Add(parentTreePath, parent);
                    nodesToProcess.Enqueue(parent);
                }

                // Add the parent and child relationship
                node.Parent = parent;
                parent.Children.Add(node);
            }

            // Recursively generate child output documents
            foreach (TreeNode node in nodesDictionary.Values.Where(x => x.Parent == null))
            {
                node.GenerateOutputDocuments(this, context);
            }

            // Return parent nodes or all nodes depending on nesting
            return nodesDictionary.Values
                .Where(x => (!_nesting || x.Parent == null) && x.OutputDocument != null)
                .Select(x => x.OutputDocument);
        }

        private class TreeNode
        {
            public string[] TreePath { get; }
            public IDocument InputDocument { get; }
            public IDocument OutputDocument { get; private set; }
            public TreeNode Parent { get; set; }
            public List<TreeNode> Children { get; } = new List<TreeNode>();

            // New placeholder node
            public TreeNode(string[] treePath)
            {
                TreePath = treePath ?? throw new ArgumentNullException(nameof(treePath));
            }

            // New node from an input document
            public TreeNode(string[] treePath, IDocument inputDocument)
                : this(treePath)
            {
                InputDocument = inputDocument;
            }

            // We need to build the tree from the bottom up so that the children don't have to be lazy
            // This also sorts the children once they're created
            public void GenerateOutputDocuments(CreateTree tree, IExecutionContext context)
            {
                // Recursively build output documents for children
                foreach (TreeNode child in Children)
                {
                    child.GenerateOutputDocuments(tree, context);
                }

                // We're done if we've already created the output document
                if (OutputDocument != null)
                {
                    return;
                }

                // Sort the child documents since they're created now
                Children.Sort((x, y) => tree._sort(x.OutputDocument, y.OutputDocument));

                // Create this output document
                MetadataItems metadata = new MetadataItems();
                if (tree._childrenKey != null)
                {
                    metadata.Add(tree._childrenKey, Children.Select(x => x.OutputDocument).ToImmutableArray());
                }
                if (tree._treePathKey != null)
                {
                    metadata.Add(tree._treePathKey, TreePath);
                }
                if (InputDocument == null)
                {
                    // There's no input document for this node so we need to make a placeholder
                    metadata.Add(Keys.TreePlaceholder, true);
                    OutputDocument = tree._placeholderFactory(TreePath, metadata, context) ?? context.CreateDocument(metadata);
                }
                else
                {
                    OutputDocument = InputDocument.Clone(metadata);
                }
            }

            public string[] GetParentTreePath() => TreePath.Take(TreePath.Length - 1).ToArray();
        }

        private class TreeNodeEqualityComparer : IEqualityComparer<TreeNode>
        {
            private readonly TreePathEqualityComparer _comparer = new TreePathEqualityComparer();

            public bool Equals(TreeNode x, TreeNode y) =>
                _comparer.Equals(x?.TreePath, y?.TreePath);

            public int GetHashCode(TreeNode obj) =>
                _comparer.GetHashCode(obj?.TreePath);
        }

        private class TreePathEqualityComparer : IEqualityComparer<string[]>
        {
            public bool Equals(string[] x, string[] y) => x.SequenceEqual(y);

            public int GetHashCode(string[] obj) =>
                obj?.Aggregate(17, (index, x) => (index * 23) + (x?.GetHashCode() ?? 0)) ?? 0;
        }
    }
}
