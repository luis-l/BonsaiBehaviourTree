
using System.Collections.Generic;

using UnityEngine;
using UnityEditor;
using UnityEditor.Callbacks;

using Bonsai.Core;

namespace Bonsai.Designer
{
  public class BonsaiWindow : EditorWindow
  {
    [MenuItem("Window/Bonsai Designer")]
    static void Init()
    {
      var window = EditorWindow.CreateInstance<BonsaiWindow>();
      window.titleContent = new GUIContent("Bonsai");
      window.Show();
    }

    public float toolbarHeight = 20;

    // We serialize the reference to the opened tree.
    // This way, when a editor window is left opened and Unity closes,
    // the tree opens up with the editor window.
    [SerializeField]
    internal Core.BehaviourTree tree;

    internal BonsaiEditor editor;
    internal BonsaiInputHandler inputHandler;
    internal BonsaiSaveManager saveManager;

    public enum Mode { Edit, View };
    private Mode _mode;
    public Mode GetMode()
    {
      return _mode;
    }

    void OnEnable()
    {
      editor = new BonsaiEditor(this);
      BonsaiEditor.FetchBehaviourNodes();

      inputHandler = new BonsaiInputHandler(this);
      saveManager = new BonsaiSaveManager(this);

      buildCanvas();

      // Always start in edit mode.
      //
      // The only way it can be in view mode is if the window is
      // already opened and the user selects a game object with a
      // behaviour tree component.
      _mode = Mode.Edit;
    }

    void OnDisable()
    {
      saveManager.OnCleanup();
    }

    void OnGUI()
    {
      if (tree == null)
      {

        editor.DrawStaticGrid();
        editor.DrawMode();

        _mode = Mode.Edit;

        // Asset removed.
        if (!saveManager.IsInNoCanvasState())
        {
          saveManager.InitState();
        }
      }

      else
      {

        // Make sure to build a canvas for an active tree.
        if (editor.Canvas == null)
        {
          buildCanvas();
        }

        editor.Draw();

        inputHandler.HandleMouseEvents(Event.current);
      }

      // Always draw the toolbar.
      drawToolbar();
    }

    void Update()
    {
      // Check if there is a request to view a tree.
      goToViewMode();

      // Update the window during the play mode when the window
      // is viewing a tree instance of a game object.
      // This is to quicky update all changes of the tree.
      bool bConditions =
          tree &&
          _mode == Mode.View &&
          EditorApplication.isPlaying &&
          tree.IsRunning();

      if (bConditions)
      {
        Repaint();
      }
    }

    private void goToViewMode()
    {
      if (!EditorApplication.isPlaying || !Selection.activeTransform)
      {
        return;
      }

      BehaviourTree treeToView = null;

      var btc = Selection.activeTransform.gameObject.GetComponent<BonsaiTreeComponent>();

      if (btc)
      {
        treeToView = btc.Tree;
      }

      // There must be a non-null tree to view,
      // it must be a different tree than the active tree for this window,
      // and must not be opened somewhere else.
      if (treeToView && tree != treeToView)
      {

        var windows = Resources.FindObjectsOfTypeAll<BonsaiWindow>();

        // Look and check if this tree is already being viewed.
        foreach (var w in windows)
        {
          if (w.tree == treeToView)
          {
            return;
          }

          // Have the window without a set tree to view the tree selected.
          else if (!w.tree)
          {
            w.Repaint();
            w.SetTree(treeToView, Mode.View);
            return;
          }
        }

        Repaint();

        // Cleanup window before putting new tree.
        saveManager.OnCleanup();

        SetTree(treeToView, Mode.View);
      }
    }

    private void buildCanvas()
    {
      if (tree)
      {
        editor.SetBehaviourTree(tree);
        Repaint();
      }
    }

    private void nicifyTree()
    {
      if (tree && editor.Canvas != null)
      {
        editor.PositionNodesNicely();
      }
    }

    public void SetTree(Core.BehaviourTree bt, Mode mode = Mode.Edit)
    {
      tree = bt;
      buildCanvas();

      _mode = mode;
    }

    private void drawToolbar()
    {
      EditorGUILayout.BeginHorizontal("Toolbar");

      if (GUILayout.Button("File", EditorStyles.toolbarDropDown, GUILayout.Width(50f)))
      {

        if (_mode == Mode.Edit)
        {
          createFileMenuEditable();
        }

        else
        {
          createFileMenuViewOnly();
        }
      }

      if (GUILayout.Button("View", EditorStyles.toolbarDropDown, GUILayout.Width(50f)))
      {

        var fileMenu = new GenericMenu();

        fileMenu.AddItem(new GUIContent("Nicefy Tree"), false, nicifyTree);
        fileMenu.AddItem(new GUIContent("Home Zoom"), false, homeZoom);

        fileMenu.DropDown(new Rect(55f, toolbarHeight, 0f, 0f));
      }

      if (GUILayout.Button("Tools", EditorStyles.toolbarDropDown, GUILayout.Width(50f)))
      {

        var fileMenu = new GenericMenu();

        fileMenu.AddItem(new GUIContent("Refresh Editor"), false, refreshEditor);

        fileMenu.DropDown(new Rect(105f, toolbarHeight, 0f, 0f));
      }

      GUILayout.FlexibleSpace();

      string name = "None";
      if (tree != null)
      {
        name = tree.name;
      }

      GUILayout.Label(name);

      EditorGUILayout.EndHorizontal();
    }

    private void createFileMenuEditable()
    {
      var fileMenu = new GenericMenu();

      fileMenu.AddItem(new GUIContent("Create New"), false, saveManager.RequestNew);
      fileMenu.AddItem(new GUIContent("Load"), false, saveManager.RequestLoad);

      fileMenu.AddSeparator("");
      fileMenu.AddItem(new GUIContent("Save"), false, saveManager.RequestSave);
      fileMenu.AddItem(new GUIContent("Save As"), false, saveManager.RequestSaveAs);

      fileMenu.DropDown(new Rect(5f, toolbarHeight, 0f, 0f));
    }

    private void createFileMenuViewOnly()
    {
      var fileMenu = new GenericMenu();

      fileMenu.AddDisabledItem(new GUIContent("Create New"));
      fileMenu.AddDisabledItem(new GUIContent("Load"));

      fileMenu.AddSeparator("");
      fileMenu.AddDisabledItem(new GUIContent("Save"));
      fileMenu.AddDisabledItem(new GUIContent("Save As"));

      fileMenu.DropDown(new Rect(5f, toolbarHeight, 0f, 0f));
    }

    private void setGrayBackround(float value)
    {
      GUI.backgroundColor = new Color(value, value, value, 1.0f);
    }

    // Centers and fits the entire tree in the view center.
    private void homeZoom()
    {
      if (!tree) return;

      LogNotImplemented("Home Zoom");
    }

    private void refreshEditor()
    {
      BonsaiResources.LoadStandardTextures();
      editor.preferences = new BonsaiPreferences();
      buildCanvas();
    }

    /// <summary>
    /// The size of the window.
    /// </summary>
    public Rect CanvasRect
    {
      get { return new Rect(Vector2.zero, position.size); }
    }

    /// <summary>
    /// The rect used to filter input.
    /// This is so the toolbar is not ignored by editor inputs.
    /// </summary>
    public Rect CanvasInputRect
    {
      get
      {
        var rect = CanvasRect;

        rect.y += toolbarHeight;
        rect.height -= toolbarHeight;

        return rect;
      }
    }

    internal static void LogNotImplemented(string msg)
    {
      Debug.Log("<color=maroon> Feature not implemented: " + msg + "</color>");
    }

    /// <summary>
    /// Opens up the Bonsai window from asset selection.
    /// </summary>
    /// <param name="instanceID"></param>
    /// <param name="line"></param>
    /// <returns></returns>
    [OnOpenAsset(1)]
    private static bool OpenCanvasAsset(int instanceID, int line)
    {
      var treeSelected = EditorUtility.InstanceIDToObject(instanceID) as Core.BehaviourTree;

      if (treeSelected != null)
      {

        BonsaiWindow windowToUse = null;

        // Try to find an editor window without a canvas...
        var bonsaiWindows = Resources.FindObjectsOfTypeAll<BonsaiWindow>();
        foreach (var w in bonsaiWindows)
        {

          // The canvas is already opened
          if (w.tree == treeSelected)
          {
            return false;
          }

          // Found a window with no active canvas.
          if (w.tree == null)
          {
            windowToUse = w;
            break;
          }
        }

        // No windows available...just make a new one.
        if (!windowToUse)
        {
          windowToUse = EditorWindow.CreateInstance<BonsaiWindow>();
          windowToUse.titleContent = new GUIContent("Bonsai");
          windowToUse.Show();
        }

        windowToUse.SetTree(treeSelected);
        windowToUse.saveManager.InitState();
        windowToUse.Repaint();

        return true;
      }

      return false;
    }
  }
}