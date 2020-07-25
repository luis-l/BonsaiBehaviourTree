
using System;
using System.Linq;
using System.Collections.Generic;
using Bonsai.Core;
using UnityEditor;
using UnityEngine;

namespace Bonsai.Designer
{
  using FilePanelResult = Utility.Result<FilePanelError, string>;

  enum FilePanelError
  {
    Cancel,
    InvalidPath
  }

  /// <summary>
  /// Handles the saving and loading of tree assets.
  /// </summary>
  public class BonsaiSaver
  {
    public struct TreeMetaData
    {
      public Vector2 zoom;
      public Vector2 pan;
    }

    public event EventHandler<string> SaveMessage;

    // Tree is valid and exists in the Asset database.
    public bool CanSaveTree(BehaviourTree tree)
    {
      return tree && AssetDatabase.Contains(tree);
    }

    /// <summary>
    /// Prompts user to load a tree from file.
    /// </summary>
    /// <returns>The behaviour tree from the asset file. Null if load failed.</returns>
    public BehaviourTree LoadBehaviourTree()
    {
      FilePanelResult path = GetCanvasOpenFilePath();

      if (path.Success)
      {
        var tree = LoadBehaviourTree(path.Value);
        if (tree == null) OnLoadFailure(); else OnLoadSuccess();
        return tree;
      }
      else
      {
        OnInvalidPathError(path.Error);
        return null;
      }
    }

    /// <summary>
    /// Saves the behaviour tree from the canvas.
    /// If the tree is unsaved (new) then it prompts the user to specify a file to save.
    /// </summary>
    /// <param name="canvas"></param>
    public void SaveCanvas(BonsaiCanvas canvas, TreeMetaData meta)
    {
      // Tree is new, need to save to asset database.
      if (!AssetDatabase.Contains(canvas.Tree))
      {
        GetSaveFilePath()
          .OnSuccess(savePath =>
          {
            SaveNewTree(savePath, meta, canvas);
            OnTreeSaved();
          })
          .OnFailure(OnInvalidPathError);
      }

      // Tree is already saved. Save nodes and tree data.
      else
      {
        SaveTree(meta, canvas);
        OnTreeSaved();
      }
    }

    /// <summary>
    /// Creates a new Behaviour Tree instance with a blackboard.
    /// The tree has no BehaviourNodes and no root node.
    /// The instance is unsaved.
    /// </summary>
    /// <returns>The new Behaviour Tree with a blackboard set.</returns>
    public static BehaviourTree CreateBehaviourTree()
    {
      var bt = ScriptableObject.CreateInstance<BehaviourTree>();
      bt.blackboard = CreateBlackboard();
      return bt;
    }

    private static Blackboard CreateBlackboard()
    {
      var bb = ScriptableObject.CreateInstance<Blackboard>();
      bb.hideFlags = HideFlags.HideInHierarchy;
      return bb;
    }

    // Load a behaviour tree at the given path. The path is aboslute but the file must be under the Asset's folder.
    private static BehaviourTree LoadBehaviourTree(string absolutePath)
    {
      string path = AssetPath(absolutePath);
      var tree = AssetDatabase.LoadAssetAtPath<BehaviourTree>(path);

      // Add a blackboard if missing when opening in editor.
      AddBlackboardIfMissing(tree);

      return tree;
    }

    public static void AddBlackboardIfMissing(BehaviourTree tree)
    {
      if (tree && (tree.blackboard == null || !AssetDatabase.Contains(tree.blackboard)))
      {
        if (tree.blackboard == null)
        {
          tree.blackboard = CreateBlackboard();
        }

        AssetDatabase.AddObjectToAsset(tree.blackboard, tree);
      }
    }

    // Adds the tree to the database and saves the nodes to the database.
    private void SaveNewTree(string path, TreeMetaData meta, BonsaiCanvas canvas)
    {
      // Save tree and black board assets
      AssetDatabase.CreateAsset(canvas.Tree, path);
      AssetDatabase.AddObjectToAsset(canvas.Tree.blackboard, canvas.Tree);

      // Save nodes.
      SaveTree(meta, canvas);
    }

    // Saves the current tree and nodes.
    private void SaveTree(TreeMetaData meta, BonsaiCanvas canvas)
    {
      // If the blackboard is not yet in the database, then add.
      AddBlackboardIfMissing(canvas.Tree);

      var canvasBehaviours = canvas.Nodes.Select(n => n.Behaviour);

      AddNewNodeAssets(canvas.Tree, canvasBehaviours);

      // Clear all parent-child connections. These will be reconstructed to match the connection in the BonsaiNodes.
      canvas.Tree.ClearStructure();

      // Sort the canvas.
      // Only consider nodes with 2 or more children for sorting.
      foreach (BonsaiNode node in canvas.Nodes.Where(node => node.ChildCount() > 1))
      {
        node.SortChildren();
      }

      // Set parent-child connections matching those in the canvas. Only consider decorators and composites.
      SetCompositeChildren(canvas);
      SetDecoratorChildren(canvas);

      // Re-add nodes to tree.
      if (canvas.Root != null)
      {
        canvas.Tree.SetNodes(canvas.Root.Behaviour);
      }

      // Nodes not connected to he root will have an unset pre-order index.
      // Tree.ClearStructure unsets the index and is only set in Tree.SetNodes
      // for nodes under the root.
      canvas.Tree.unusedNodes = canvasBehaviours.Where(
        b => b.PreOrderIndex == BehaviourNode.kInvalidOrder).ToList();

      SaveTreeMetaData(meta, canvas);
      AssetDatabase.SaveAssets();
    }

    private void SetCompositeChildren(BonsaiCanvas canvas)
    {
      IEnumerable<BonsaiNode> compositeNodes = canvas.Nodes.Where(n => n.Behaviour.IsComposite());
      foreach (BonsaiNode node in compositeNodes)
      {
        var compositeBehaviour = node.Behaviour as Composite;
        compositeBehaviour.SetChildren(node.Children.Select(ch => ch.Behaviour).ToArray());
      }
    }

    private void SetDecoratorChildren(BonsaiCanvas canvas)
    {
      IEnumerable<BonsaiNode> decoratorNodes = canvas.Nodes
        .Where(n => n.Behaviour.IsDecorator() && n.ChildCount() == 1);

      foreach (BonsaiNode node in decoratorNodes)
      {
        var decoratorBehaviour = node.Behaviour as Decorator;
        decoratorBehaviour.SetChild(node.GetChildAt(0).Behaviour);
      }
    }

    private void AddNewNodeAssets(
      BehaviourTree treeAsset,
      IEnumerable<BehaviourNode> canvasNodes)
    {
      foreach (BehaviourNode node in canvasNodes)
      {
        if (!AssetDatabase.Contains(node))
        {
          node.name = node.GetType().Name;
          node.hideFlags = HideFlags.HideInHierarchy;
          AssetDatabase.AddObjectToAsset(node, treeAsset);
        }
      }
    }

    private void SaveTreeMetaData(TreeMetaData meta, BonsaiCanvas canvas)
    {
      foreach (var editorNode in canvas.Nodes)
      {
        editorNode.Behaviour.bonsaiNodePosition = editorNode.Position;
      }

      canvas.Tree.panPosition = meta.pan;
      canvas.Tree.zoomPosition = meta.zoom;
    }

    /// <summary>
    /// Gets the file path to save the canavs at.
    /// </summary>
    /// <returns></returns>
    private FilePanelResult GetSaveFilePath()
    {
      string path = EditorUtility.SaveFilePanelInProject("Save Bonsai Canvas", "NewBonsaiBT", "asset", "Select a destination to save the canvas.");

      if (string.IsNullOrEmpty(path))
      {
        return FilePanelResult.Fail(FilePanelError.Cancel);
      }

      return FilePanelResult.Ok(path);
    }

    /// <summary>
    /// Get the path from open file dialog.
    /// </summary>
    /// <returns></returns>
    private FilePanelResult GetCanvasOpenFilePath()
    {
      string path = EditorUtility.OpenFilePanel("Open Bonsai Canvas", "Assets/", "asset");

      if (string.IsNullOrEmpty(path))
      {
        return FilePanelResult.Fail(FilePanelError.Cancel);
      }

      // If the path is outside the project's asset folder.
      if (!path.Contains(Application.dataPath))
      {
        return FilePanelResult.Fail(FilePanelError.InvalidPath);
      }


      return FilePanelResult.Ok(path);
    }

    /// <summary>
    /// Converts the absolute path to a path relative to the Assets folder.
    /// </summary>
    private static string AssetPath(string absolutePath)
    {
      int assetIndex = absolutePath.IndexOf("/Assets/");
      return absolutePath.Substring(assetIndex + 1);
    }


    private void OnInvalidPathError(FilePanelError error)
    {
      if (error == FilePanelError.InvalidPath)
      {
        SaveMessage?.Invoke(this, "Please select a Bonsai asset within the project's Asset folder.");
      }
    }

    private void OnLoadFailure()
    {
      SaveMessage?.Invoke(this, "Failed to load tree.");
    }

    private void OnLoadSuccess()
    {
      SaveMessage?.Invoke(this, "Tree loaded");
    }

    private void OnTreeSaved()
    {
      SaveMessage?.Invoke(this, "Tree Saved");
    }

    private void OnTreeCopied()
    {
      SaveMessage?.Invoke(this, "Tree Copied");
    }
  }
}
