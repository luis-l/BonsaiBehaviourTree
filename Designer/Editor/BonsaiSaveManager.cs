
using System;
using Bonsai.Utility;
using UnityEditor;
using UnityEngine;

namespace Bonsai.Designer
{
  /// <summary>
  /// Handles the saving and loading of tree assets.
  /// </summary>
  public class BonsaiSaveManager
  {
    public enum SaveState { NoTree, TempTree, SavedTree };

    // The FSM used to structure the logic control of saving and loading.
    private readonly StateMachine<SaveState> saveFsm = new StateMachine<SaveState>();

    // The events that dictate the flow of the manager's FSM.
    private enum SaveOp { None, New, Load, Save, SaveAs };
    private SaveOp requestedSaveOp = SaveOp.None;

    private readonly BonsaiWindow window;

    // Path that stores temporary canvases.
    private const string kTempCanvasPath = "Assets/Plugins/Bonsai/Temp/";
    private const string kTempFileName = "TempBT";

    public BonsaiSaveManager(BonsaiWindow w)
    {
      window = w;

      var noTree = new StateMachine<SaveState>.State(SaveState.NoTree);
      var tempTree = new StateMachine<SaveState>.State(SaveState.TempTree);
      var savedTree = new StateMachine<SaveState>.State(SaveState.SavedTree);

      saveFsm.AddState(noTree);
      saveFsm.AddState(tempTree);
      saveFsm.AddState(savedTree);

      // Actions to take when starting out on a window with no canvas.
      saveFsm.AddTransition(noTree, tempTree, IsNewRequested, CreateNewOnto_Window_WithTempOrEmpty);
      saveFsm.AddTransition(noTree, savedTree, IsLoadRequested, LoadOntoEmptyWindow);

      // Actions to take when the window has a temp canvas.
      saveFsm.AddTransition(tempTree, tempTree, IsNewRequested, CreateNewOnto_Window_WithTempOrEmpty);
      saveFsm.AddTransition(tempTree, savedTree, IsSaveAsRequested, SaveTempAs);
      saveFsm.AddTransition(tempTree, savedTree, IsLoadRequested, LoadOnto_Window_WithTempCanvas);

      // Actions to take when the window has a valid canvas (already saved).
      saveFsm.AddTransition(savedTree, savedTree, IsSaveRequested, Save);
      saveFsm.AddTransition(savedTree, savedTree, IsSaveAsRequested, SaveCloneAs);
      saveFsm.AddTransition(savedTree, savedTree, IsLoadRequested, LoadOnto_Window_WithSavedCanvas);
      saveFsm.AddTransition(savedTree, tempTree, IsNewRequested, CreateNewOnto_Window_WithSavedCanvas);

      // Consume the save operation even after the transition is made.
      saveFsm.StateChanged += () => { requestedSaveOp = SaveOp.None; };

      InitState();
    }

    /// <summary>
    /// This hanldes setting up the proper state based on the window's canvas.
    /// </summary>
    public void InitState()
    {
      // If the window has a valid canvas and editable.
      if (window.Tree != null && window.Editor.EditorMode.Value == BonsaiEditor.Mode.Edit)
      {
        string path = GetCurrentTreePath();

        // If the canvas is temp.
        if (path.Contains(kTempCanvasPath))
        {
          SetState(SaveState.TempTree);
        }

        // If the canvas is saved (not a temp).
        else
        {
          SetState(SaveState.SavedTree);
        }
      }

      // Window is fresh, no canvas yet set.
      else
      {
        SetState(SaveState.NoTree);
      }
    }

    /// <summary>
    /// Get the path from open file dialog.
    /// </summary>
    /// <returns></returns>
    private string GetCanvasFilePath()
    {
      string path = EditorUtility.OpenFilePanel("Open Bonsai Canvas", "Assets/", "asset");

      // If the path is outside the project's asset folder.
      if (!path.Contains(Application.dataPath))
      {

        // If the selection was not cancelled...
        if (!string.IsNullOrEmpty(path))
        {
          window.ShowNotification(new GUIContent("Please select a Bonsai asset within the project's Asset folder."));
          return null;
        }
      }

      return path;
    }

    /// <summary>
    /// Assumes that the path is already valid.
    /// </summary>
    /// <param name="path"></param>
    private void LoadTree(string path)
    {
      int assetIndex = path.IndexOf("/Assets/");
      path = path.Substring(assetIndex + 1);

      var tree = AssetDatabase.LoadAssetAtPath<Core.BehaviourTree>(path);
      window.SetTree(tree);
    }

    /// <summary>
    /// Gets the file path to save the canavs at.
    /// </summary>
    /// <returns></returns>
    private string GetSaveFilePath()
    {
      string path = EditorUtility.SaveFilePanelInProject("Save Bonsai Canvas", "NewBonsaiBT", "asset", "Select a destination to save the canvas.");

      if (string.IsNullOrEmpty(path))
      {
        return null;
      }

      return path;
    }

    #region Save Operations

    /// <summary>
    /// Creates and adds a node to the tree.
    /// </summary>
    /// <param name="t"></param>
    /// <param name="bt"></param>
    /// <returns></returns>
    public static Core.BehaviourNode CreateBehaviourNode(Type t, Core.BehaviourTree bt)
    {
      try
      {
        var behaviour = ScriptableObject.CreateInstance(t) as Core.BehaviourNode;
        AssetDatabase.AddObjectToAsset(behaviour, bt);
        behaviour.Tree = bt;
        return behaviour;
      }

      catch (Exception e)
      {
        throw new UnityException(e.Message);
      }
    }

    /// <summary>
    /// Creates and adds a node to the tree.
    /// </summary>
    /// <typeparam name="T"></typeparam>
    /// <param name="bt"></param>
    /// <returns></returns>
    public static Core.BehaviourNode CreateBehaviourNode<T>(Core.BehaviourTree bt) where T : Core.BehaviourNode
    {
      var behaviour = ScriptableObject.CreateInstance<T>();
      AssetDatabase.AddObjectToAsset(behaviour, bt);
      behaviour.Tree = bt;
      return behaviour;
    }

    private static void CreateBlackboard(Core.BehaviourTree bt)
    {
      var bb = ScriptableObject.CreateInstance<Core.Blackboard>();
      AssetDatabase.AddObjectToAsset(bb, bt);
      bt.SetBlackboard(bb);
    }

    /// <summary>
    /// Creates a tree asset and saves it.
    /// </summary>
    /// <param name="path">The full path including name and extension.</param>
    /// <returns></returns>
    public static Core.BehaviourTree CreateBehaviourTree(string path)
    {
      // We create a tree asset in the data base in order to add node assets
      // under the tree. This way things are organized in the editor.
      //
      // The drawback is that we need to create a temp asset for the tree
      // and make sure it does not linger if the temp asset is discarded.
      //
      // This means that we need to have a persistent directoy to store temp
      // assets.
      var bt = ScriptableObject.CreateInstance<Core.BehaviourTree>();
      AssetDatabase.CreateAsset(bt, path);
      CreateBlackboard(bt);
      AssetDatabase.SaveAssets();
      return bt;
    }

    /// <summary>
    /// Creates a new temporary canvas.
    /// </summary>
    /// <returns></returns>
    private Core.BehaviourTree CreateNew()
    {
      window.ShowNotification(new GUIContent("New Tree Created"));
      return CreateBehaviourTree(GetTemporaryPath());
    }

    // Create a new temp canvas on an empty window or with a temp canvas..
    private void CreateNewOnto_Window_WithTempOrEmpty()
    {
      window.SetTree(CreateNew());
    }

    // Saves the current active canvas then loads a new canvas.
    private void CreateNewOnto_Window_WithSavedCanvas()
    {
      // Save the old canvas to avoid loss.
      AssetDatabase.SaveAssets();

      window.SetTree(CreateNew());
    }

    // Load a canvas to a window that has no canvas active.
    private void LoadOntoEmptyWindow()
    {
      LoadTree(GetCanvasFilePath());
    }

    // Load a canvas to a window that has a temp canvas active.
    private void LoadOnto_Window_WithTempCanvas()
    {
      string path = GetCanvasFilePath();

      if (path != null)
      {
        // Get rid of the temporary canvas.
        AssetDatabase.DeleteAsset(GetCurrentTreePath());
        LoadTree(path);
      }
    }

    // Load a canvas to a window that has a saved canvas active.
    private void LoadOnto_Window_WithSavedCanvas()
    {
      string path = GetCanvasFilePath();

      if (path != null)
      {
        // Save the old canvas.
        Save();
        LoadTree(path);
      }
    }

    // Makes the temporary canvas into a saved canvas.
    private void SaveTempAs()
    {
      string path = GetSaveFilePath();

      if (!string.IsNullOrEmpty(path))
      {

        AssetDatabase.MoveAsset(GetCurrentTreePath(), path);
        Save();
      }
    }

    // Copies the current active canvas to a new location.
    private void SaveCloneAs()
    {
      string path = GetSaveFilePath();

      if (!string.IsNullOrEmpty(path))
      {
        // There seems to be a bug in the AssetDatabase.Copy
        // The asset hierarchy is not preserved since it follows a 
        // lexicographical traversal to copy.
        //
        // This means that if a subasset's name is lexicographically
        // first than the main asset, then it will become the main
        // asset in the copy while the original main asset becomes a subasset.

        // Rename subassets such that they are lexicographically after the main asset.
        foreach (var node in window.Editor.Canvas)
        {
          node.Behaviour.name = window.Tree.name + node.Behaviour.GetType().Name;
        }

        AssetDatabase.CopyAsset(GetCurrentTreePath(), path);
        Save();
      }
    }

    // Saves the current canvas (not a temp canvas).
    private void Save()
    {
      // Sort the nodes in pre order so it is easier to clone the tree.
      window.Tree.SortNodes();

      SaveTreeMetaData();
      AssetDatabase.SaveAssets();

      window.ShowNotification(new GUIContent("Tree Saved"));
    }

    private void SaveTreeMetaData()
    {
      foreach (var editorNode in window.Editor.Canvas)
      {
        editorNode.Behaviour.bonsaiNodePosition = editorNode.Position;
      }

      window.Tree.panPosition = window.Viewer.panOffset;
      window.Tree.zoomPosition = window.Viewer.zoom;
    }

    #endregion

    /// <summary>
    /// Handles deleting temporary canvas or saving valid canvas.
    /// </summary>
    public void OnCleanup()
    {
      // Only save/delete things if we are in edit mode.
      if (window.Editor.EditorMode.Value != BonsaiEditor.Mode.Edit)
      {
        return;
      }

      SaveState state = saveFsm.CurrentState.Value;

      if (state == SaveState.TempTree)
      {
        AssetDatabase.DeleteAsset(GetCurrentTreePath());
      }

      else if (state == SaveState.SavedTree)
      {
        Save();
      }
    }

    /*
     * These are conditions used the save FSM to know when to transition.
     *  */
    private bool IsNewRequested() { return requestedSaveOp == SaveOp.New; }
    private bool IsLoadRequested() { return requestedSaveOp == SaveOp.Load; }
    private bool IsSaveRequested() { return requestedSaveOp == SaveOp.Save; }
    private bool IsSaveAsRequested() { return requestedSaveOp == SaveOp.SaveAs; }

    /*
     * These are the events that drive the save manager.
     * Whenever one of this is fired, the save operation is set 
     * and the save FSM updated.
     * */
    public void RequestNew() { requestedSaveOp = SaveOp.New; saveFsm.Update(); }
    public void RequestLoad() { requestedSaveOp = SaveOp.Load; saveFsm.Update(); }
    public void RequestSave() { requestedSaveOp = SaveOp.Save; saveFsm.Update(); }
    public void RequestSaveAs() { requestedSaveOp = SaveOp.SaveAs; saveFsm.Update(); }

    private string GetTemporaryPath()
    {
      return kTempCanvasPath + kTempFileName + window.GetInstanceID().ToString() + ".asset";
    }

    public void SetState(SaveState state)
    {
      saveFsm.SetCurrentState(state);
    }

    public bool IsInNoCanvasState()
    {
      return saveFsm.CurrentState.Value == SaveState.NoTree;
    }

    public SaveState CurrentState()
    {
      return saveFsm.CurrentState.Value;
    }

    private string GetCurrentTreePath()
    {
      return AssetDatabase.GetAssetPath(window.Tree);
    }
  }
}
