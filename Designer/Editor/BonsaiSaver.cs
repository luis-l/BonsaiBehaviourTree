
using System;
using System.Linq;
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
    /// Prompts user to save a copy of the canvas to a new file.
    /// </summary>
    public void SaveCanvasAs(BonsaiCanvas canvas)
    {
      GetSaveFilePath()
        .OnSuccess(path =>
        {
          SaveTreeCopy(path, canvas);
          OnTreeCopied();
        })
        .OnFailure(OnInvalidPathError);
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
      bt.SetBlackboard(CreateBlackboard());
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
      if (tree && (tree.Blackboard == null || !AssetDatabase.Contains(tree.Blackboard)))
      {
        if (tree.Blackboard == null)
        {
          tree.SetBlackboard(CreateBlackboard());
        }

        AssetDatabase.AddObjectToAsset(tree.Blackboard, tree);
      }
    }

    // Adds the tree to the database and saves the nodes to the database.
    private void SaveNewTree(string path, TreeMetaData meta, BonsaiCanvas canvas)
    {
      // Save tree and black board assets
      AssetDatabase.CreateAsset(canvas.Tree, path);
      AssetDatabase.AddObjectToAsset(canvas.Tree.Blackboard, canvas.Tree);

      // Save nodes.
      SaveTree(meta, canvas);
    }

    // Copies the current active canvas to a new location.
    private void SaveTreeCopy(string path, BonsaiCanvas canvas)
    {
      // There seems to be a bug in the AssetDatabase.Copy
      // The asset hierarchy is not preserved since it follows a 
      // lexicographical traversal to copy.
      //
      // This means that if a subasset's name is lexicographically
      // first than the main asset, then it will become the main
      // asset in the copy while the original main asset becomes a subasset.

      // Rename subassets such that they are lexicographically after the main asset.
      foreach (var node in canvas.Nodes)
      {
        node.Behaviour.name = canvas.Tree.name + node.Behaviour.GetType().Name;
      }

      string sourcePath = AssetDatabase.GetAssetPath(canvas.Tree);
      AssetDatabase.CopyAsset(sourcePath, path);

      // TODO FIXME: Save the clone asset with the contents in the BonsaiCanvas.
      // Previous tree should not be modified.
    }

    // Saves the current tree and nodes.
    private void SaveTree(TreeMetaData meta, BonsaiCanvas canvas)
    {
      // If the blackboard is not yet in the database, then add.
      AddBlackboardIfMissing(canvas.Tree);

      var treeBehaviours = canvas.Tree.AllNodes;
      var canvasBehaviours = canvas.Nodes.Select(n => n.Behaviour);

      // New nodes that need to be added to the database.
      foreach (BehaviourNode newNodes in canvasBehaviours.Except(treeBehaviours))
      {
        newNodes.name = newNodes.GetType().Name;
        newNodes.hideFlags = HideFlags.HideInHierarchy;
        AssetDatabase.AddObjectToAsset(newNodes, canvas.Tree);
      }

      // Clear all parent-child connections. These will be reconstructed to match the connection in the BonsaiNodes.
      canvas.Tree.ClearStructure();

      // Sort the canvas.
      // Only consider nodes with 2 or more children for sorting.
      foreach (BonsaiNode node in canvas.Nodes.Where(node => node.ChildCount() > 1))
      {
        node.SortChildren();
      }

      // Set parent-child connections matching those in the canvas. Only consider decorators and composites.
      foreach (BonsaiNode node in canvas.Nodes.Where(node => node.ChildCount() > 0))
      {
        foreach (BonsaiNode child in node.Children)
        {
          node.Behaviour.AddChild(child.Behaviour);
        }
      }

      // Re-add nodes to tree.
      foreach (BonsaiNode node in canvas.Nodes)
      {
        node.Behaviour.Tree = canvas.Tree;
      }

      if (canvas.Root != null)
      {
        canvas.Tree.Root = canvas.Root.Behaviour;
      }

      // Sort the nodes in pre order so it is easier to clone the tree.
      canvas.Tree.SortNodes();

      SaveTreeMetaData(meta, canvas);
      AssetDatabase.SaveAssets();
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
