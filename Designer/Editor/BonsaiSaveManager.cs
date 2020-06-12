
using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;
using UnityEditor.Callbacks;

using Bonsai.Utility;

namespace Bonsai.Designer
{
  /// <summary>
  /// Handles the saving and loading of tree assets.
  /// </summary>
  public class BonsaiSaveManager
  {
    public enum SaveState { NoTree, TempTree, SavedTree };

    // The FSM used to structure the logic control of saving and loading.
    private StateMachine<SaveState> _saveFSM;

    // The events that dictate the flow of the manager's FSM.
    private enum SaveOp { None, New, Load, Save, SaveAs };
    private SaveOp _saveOp = SaveOp.None;

    private BonsaiWindow _window;

    // Path that stores temporary canvases.
    private const string kTempCanvasPath = "Assets/Plugins/Bonsai/Temp/";
    private const string kTempFileName = "TempBT";

    public BonsaiSaveManager(BonsaiWindow w)
    {
      _window = w;

      _saveFSM = new StateMachine<SaveState>();

      var noTree = new StateMachine<SaveState>.State(SaveState.NoTree);
      var tempTree = new StateMachine<SaveState>.State(SaveState.TempTree);
      var savedTree = new StateMachine<SaveState>.State(SaveState.SavedTree);

      _saveFSM.AddState(noTree);
      _saveFSM.AddState(tempTree);
      _saveFSM.AddState(savedTree);

      // Actions to take when starting out on a window with no canvas.
      _saveFSM.AddTransition(noTree, tempTree, isNewRequested, createNewOnto_Window_WithTempOrEmpty);
      _saveFSM.AddTransition(noTree, savedTree, isLoadRequested, loadOnto_EmptyWindow);

      // Actions to take when the window has a temp canvas.
      _saveFSM.AddTransition(tempTree, tempTree, isNewRequested, createNewOnto_Window_WithTempOrEmpty);
      _saveFSM.AddTransition(tempTree, savedTree, isSaveAsRequested, saveTempAs);
      _saveFSM.AddTransition(tempTree, savedTree, isLoadRequested, loadOnto_Window_WithTempCanvas);

      // Actions to take when the window has a valid canvas (already saved).
      _saveFSM.AddTransition(savedTree, savedTree, isSaveRequested, save);
      _saveFSM.AddTransition(savedTree, savedTree, isSaveAsRequested, saveCloneAs);
      _saveFSM.AddTransition(savedTree, savedTree, isLoadRequested, loadOnto_Window_WithSavedCanvas);
      _saveFSM.AddTransition(savedTree, tempTree, isNewRequested, createNewOnto_Window_WithSavedCanvas);

      // Consume the save operation even after the transition is made.
      _saveFSM.OnStateChangedEvent += () => { _saveOp = SaveOp.None; };

      InitState();
    }

    /// <summary>
    /// This hanldes setting up the proper state based on the window's canvas.
    /// </summary>
    internal void InitState()
    {
      // If the window has a valid canvas and editable.
      if (_window.tree != null && _window.GetMode() == BonsaiWindow.Mode.Edit)
      {

        string path = getCurrentTreePath();

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
    private string getCanvasFilePath()
    {
      string path = EditorUtility.OpenFilePanel("Open Bonsai Canvas", "Assets/", "asset");

      // If the path is outside the project's asset folder.
      if (!path.Contains(Application.dataPath))
      {

        // If the selection was not cancelled...
        if (!string.IsNullOrEmpty(path))
        {
          _window.ShowNotification(new GUIContent("Please select a Bonsai asset within the project's Asset folder."));
          return null;
        }
      }

      return path;
    }

    /// <summary>
    /// Assumes that the path is already valid.
    /// </summary>
    /// <param name="path"></param>
    private void loadTree(string path)
    {
      int assetIndex = path.IndexOf("/Assets/");
      path = path.Substring(assetIndex + 1);

      var tree = AssetDatabase.LoadAssetAtPath<Core.BehaviourTree>(path);
      _window.SetTree(tree);
    }

    /// <summary>
    /// Gets the file path to save the canavs at.
    /// </summary>
    /// <returns></returns>
    private string getSaveFilePath()
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
    private Core.BehaviourTree createNew()
    {
      _window.ShowNotification(new GUIContent("New Tree Created"));
      return CreateBehaviourTree(getTemporaryPath());
    }

    // Create a new temp canvas on an empty window or with a temp canvas..
    private void createNewOnto_Window_WithTempOrEmpty()
    {
      _window.SetTree(createNew());
    }

    // Saves the current active canvas then loads a new canvas.
    private void createNewOnto_Window_WithSavedCanvas()
    {
      // Save the old canvas to avoid loss.
      AssetDatabase.SaveAssets();

      _window.SetTree(createNew());
    }

    // Load a canvas to a window that has no canvas active.
    private void loadOnto_EmptyWindow()
    {
      loadTree(getCanvasFilePath());
    }

    // Load a canvas to a window that has a temp canvas active.
    private void loadOnto_Window_WithTempCanvas()
    {
      string path = getCanvasFilePath();

      if (path != null)
      {

        // Get rid of the temporary canvas.
        AssetDatabase.DeleteAsset(getCurrentTreePath());

        loadTree(path);
      }
    }

    // Load a canvas to a window that has a saved canvas active.
    private void loadOnto_Window_WithSavedCanvas()
    {
      string path = getCanvasFilePath();

      if (path != null)
      {

        // Save the old canvas.
        save();

        loadTree(path);
      }
    }

    // Makes the temporary canvas into a saved canvas.
    private void saveTempAs()
    {
      string path = getSaveFilePath();

      if (path != null)
      {

        AssetDatabase.MoveAsset(getCurrentTreePath(), path);
        save();
      }
    }

    // Copies the current active canvas to a new location.
    private void saveCloneAs()
    {
      string path = getSaveFilePath();

      if (path != null)
      {

        // There seems to be a bug in the AssetDatabase.Copy
        // The asset hierarchy is not preserved since it follows a 
        // lexicographical traversal to copy.
        //
        // This means that if a subasset's name is lexicographically
        // first than the main asset, then it will become the main
        // asset in the copy while the original main asset becomes a subasset.

        // Rename subassets such that they are lexicographically after the main asset.
        foreach (var node in _window.editor.canvas)
        {
          node.Behaviour.name = _window.tree.name + node.Behaviour.GetType().Name;
        }

        AssetDatabase.CopyAsset(getCurrentTreePath(), path);

        save();
      }
    }

    // Saves the current canvas (not a temp canvas).
    private void save()
    {
      // Sort the nodes in pre order so it is easier to clone the tree.
      _window.tree.SortNodes();

      saveTreeMetaData();
      AssetDatabase.SaveAssets();

      _window.ShowNotification(new GUIContent("Tree Saved"));
    }

    private void saveTreeMetaData()
    {
      foreach (var editorNode in _window.editor.canvas)
      {
        editorNode.Behaviour.bonsaiNodePosition = editorNode.bodyRect.position;
      }

      _window.tree.panPosition = _window.editor.canvas.panOffset;
      _window.tree.zoomPosition = _window.editor.canvas.zoom;
    }

    #endregion

    /// <summary>
    /// Handles deleting temporary canvas or saving valid canvas.
    /// </summary>
    internal void OnCleanup()
    {
      // Only save/delete things if we are in edit mode.
      if (_window.GetMode() != BonsaiWindow.Mode.Edit)
      {
        return;
      }

      SaveState state = _saveFSM.CurrentState.Value;

      if (state == SaveState.TempTree)
      {
        AssetDatabase.DeleteAsset(getCurrentTreePath());
      }

      else if (state == SaveState.SavedTree)
      {
        save();
      }
    }

    /*
     * These are conditions used the save FSM to know when to transition.
     *  */
    private bool isNewRequested() { return _saveOp == SaveOp.New; }
    private bool isLoadRequested() { return _saveOp == SaveOp.Load; }
    private bool isSaveRequested() { return _saveOp == SaveOp.Save; }
    private bool isSaveAsRequested() { return _saveOp == SaveOp.SaveAs; }

    /*
     * These are the events that drive the save manager.
     * Whenever one of this is fired, the save operation is set 
     * and the save FSM updated.
     * */
    internal void RequestNew() { _saveOp = SaveOp.New; _saveFSM.Update(); }
    internal void RequestLoad() { _saveOp = SaveOp.Load; _saveFSM.Update(); }
    internal void RequestSave() { _saveOp = SaveOp.Save; _saveFSM.Update(); }
    internal void RequestSaveAs() { _saveOp = SaveOp.SaveAs; _saveFSM.Update(); }

    private string getTemporaryPath()
    {
      return kTempCanvasPath + kTempFileName + _window.GetInstanceID().ToString() + ".asset";
    }

    internal void SetState(SaveState state)
    {
      _saveFSM.SetCurrentState(state);
    }

    internal bool IsInNoCanvasState()
    {
      return _saveFSM.CurrentState.Value == SaveState.NoTree;
    }

    internal SaveState CurrentState()
    {
      return _saveFSM.CurrentState.Value;
    }

    private string getCurrentTreePath()
    {
      return AssetDatabase.GetAssetPath(_window.tree);
    }
  }
}
