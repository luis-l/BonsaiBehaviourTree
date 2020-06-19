
using System;
using System.Runtime.CompilerServices;
using Bonsai.Core;
using Bonsai.Utility;
using UnityEditor;
using UnityEditor.Callbacks;
using UnityEngine;

namespace Bonsai.Designer
{
  public class BonsaiWindow : EditorWindow
  {
    [MenuItem("Window/Bonsai Designer")]
    static void Init()
    {
      var window = CreateInstance<BonsaiWindow>();
      window.titleContent = new GUIContent("Bonsai");
      window.Show();
    }

    public const float toolbarHeight = 20;

    // We serialize the reference to the opened tree.
    // This way, when a editor window is left opened and Unity closes,
    // the tree opens up with the editor window.
    [SerializeField]
    private BehaviourTree behaviourTree;
    public BehaviourTree Tree { get { return behaviourTree; } }

    public BonsaiEditor Editor { get; private set; }
    public BonsaiInput Input { get; private set; }
    public BonsaiSaveManager SaveManager { get; private set; }

    public enum Mode { Edit, View };
    public ReactiveValue<Mode> EditorMode = new ReactiveValue<Mode>();

    void OnEnable()
    {
      BonsaiPreferences.Instance = BonsaiPreferences.LoadDefaultPreferences();

      Editor = new BonsaiEditor(this);
      BonsaiEditor.FetchBehaviourNodes();

      EditorMode.ValueChanged += Editor.EditorStatusChanged;

      Input = new BonsaiInput(this);
      SaveManager = new BonsaiSaveManager(this);

      Input.SaveRequest += Save;
      Input.Pan += (s, pan) => Editor.Pan(pan);
      Input.Zoom += (s, zoom) => Editor.Zoom(zoom);

      Input.NodeClick += Editor.NodeClicked;
      Input.NodeClick += (sender, e) => Repaint();

      Input.CanvasLostFocus += Editor.CanvasLostFocus;
      Input.InputClick += Editor.InputClicked;
      Input.OutputClick += Editor.OutputClicked;

      Input.Unclick += Editor.Unclicked;
      Input.NodeUnclick += Editor.NodeUnclicked;
      Input.Unclick += (sender, e) => Repaint();

      Input.CanvasClicked += (sender, e) =>
      {
        Editor.NodeSelection.SetTreeSelection(Tree);
        Editor.CanvasClicked(sender, e);
        Repaint();
      };

      Editor.RepaintRequired += (sender, e) => Repaint();

      BuildCanvas();

      // Always start in edit mode.
      //
      // The only way it can be in view mode is if the window is
      // already opened and the user selects a game object with a
      // behaviour tree component.
      EditorMode.Value = Mode.Edit;
    }

    void OnDisable()
    {
      SaveManager.OnCleanup();
    }

    void OnGUI()
    {
      if (Tree == null)
      {
        Editor.DrawStaticGrid();
        Editor.DrawMode();
        EditorMode.Value = Mode.Edit;

        // Asset removed.
        if (!SaveManager.IsInNoCanvasState())
        {
          SaveManager.InitState();
        }
      }

      else
      {
        // Make sure to build a canvas for an active tree.
        if (Editor.Canvas == null)
        {
          BuildCanvas();
        }

        Editor.Update();
        Editor.Draw();
        Input.HandleMouseEvents(Event.current, CanvasInputRect);
      }

      // Always draw the toolbar.
      DrawToolbar();
    }

    void Update()
    {
      // Check if there is a request to view a tree.
      GoToViewMode();

      // Update the window during the play mode when the window
      // is viewing a tree instance of a game object.
      // This is to quicky update all changes of the tree.
      bool bConditions =
          Tree &&
          EditorMode.Value == Mode.View &&
          EditorApplication.isPlaying &&
          Tree.IsRunning();

      if (bConditions)
      {
        Repaint();
      }
    }

    /// <summary>
    /// Call to update the editor with new behaviour changes.
    /// </summary>
    /// <param name="behaviour"></param>
    public void BehaviourNodeEdited(BehaviourNode behaviour)
    {
      Editor.UpdateNodeGUI(behaviour);
      Repaint();
    }

    private void GoToViewMode()
    {
      if (!EditorApplication.isPlaying || !Selection.activeGameObject)
      {
        return;
      }

      BehaviourTree treeToView = null;

      var btc = Selection.activeGameObject.GetComponent<BonsaiTreeComponent>();

      if (btc)
      {
        treeToView = btc.Tree;
      }

      // There must be a non-null tree to view,
      // it must be a different tree than the active tree for this window,
      // and must not be opened somewhere else.
      if (treeToView && Tree != treeToView)
      {
        var windows = Resources.FindObjectsOfTypeAll<BonsaiWindow>();

        // Look and check if this tree is already being viewed.
        foreach (var w in windows)
        {
          if (w.Tree == treeToView)
          {
            return;
          }

          // Have the window without a set tree to view the tree selected.
          else if (!w.Tree)
          {
            w.Repaint();
            w.SetTree(treeToView, Mode.View);
            return;
          }
        }

        Repaint();

        // Cleanup window before putting new tree.
        SaveManager.OnCleanup();

        SetTree(treeToView, Mode.View);
      }
    }

    private void BuildCanvas()
    {
      if (Tree)
      {
        Editor.SetBehaviourTree(Tree);
        Repaint();
      }
    }

    private void NicifyTree()
    {
      if (Tree && Editor.Canvas != null)
      {
        Formatter.PositionNodesNicely(Tree, Editor.Canvas);
      }
    }

    public void SetTree(BehaviourTree bt, Mode mode = Mode.Edit)
    {
      behaviourTree = bt;
      BuildCanvas();
      EditorMode.Value = mode;
    }

    private void DrawToolbar()
    {
      EditorGUILayout.BeginHorizontal("Toolbar");

      if (GUILayout.Button("File", EditorStyles.toolbarDropDown, GUILayout.Width(50f)))
      {
        if (EditorMode.Value == Mode.Edit)
        {
          CreateFileMenuEditable();
        }
        else
        {
          CreateFileMenuViewOnly();
        }
      }

      if (GUILayout.Button("View", EditorStyles.toolbarDropDown, GUILayout.Width(50f)))
      {
        var fileMenu = new GenericMenu();
        fileMenu.AddItem(new GUIContent("Nicefy Tree"), false, NicifyTree);
        fileMenu.AddItem(new GUIContent("Home Zoom"), false, HomeZoom);
        fileMenu.DropDown(new Rect(55f, toolbarHeight, 0f, 0f));
      }

      if (GUILayout.Button("Tools", EditorStyles.toolbarDropDown, GUILayout.Width(50f)))
      {
        var fileMenu = new GenericMenu();
        fileMenu.AddItem(new GUIContent("Refresh Editor"), false, RefreshEditor);
        fileMenu.DropDown(new Rect(105f, toolbarHeight, 0f, 0f));
      }

      GUILayout.FlexibleSpace();

      string name = "None";
      if (Tree != null)
      {
        name = Tree.name;
      }

      GUILayout.Label(name);

      EditorGUILayout.EndHorizontal();
    }

    private void CreateFileMenuEditable()
    {
      var fileMenu = new GenericMenu();

      fileMenu.AddItem(new GUIContent("Create New"), false, SaveManager.RequestNew);
      fileMenu.AddItem(new GUIContent("Load"), false, SaveManager.RequestLoad);

      fileMenu.AddSeparator("");
      fileMenu.AddItem(new GUIContent("Save"), false, SaveManager.RequestSave);
      fileMenu.AddItem(new GUIContent("Save As"), false, SaveManager.RequestSaveAs);

      fileMenu.DropDown(new Rect(5f, toolbarHeight, 0f, 0f));
    }

    private void CreateFileMenuViewOnly()
    {
      var fileMenu = new GenericMenu();

      fileMenu.AddDisabledItem(new GUIContent("Create New"));
      fileMenu.AddDisabledItem(new GUIContent("Load"));

      fileMenu.AddSeparator("");
      fileMenu.AddDisabledItem(new GUIContent("Save"));
      fileMenu.AddDisabledItem(new GUIContent("Save As"));

      fileMenu.DropDown(new Rect(5f, toolbarHeight, 0f, 0f));
    }

    // Centers and fits the entire tree in the view center.
    private void HomeZoom()
    {
      if (!Tree) return;

      LogNotImplemented("Home Zoom");
    }

    private void RefreshEditor()
    {
      // Reload preferences.
      BonsaiPreferences.Instance = BonsaiPreferences.LoadDefaultPreferences();
      BuildCanvas();
    }

    private void Save(object sender, EventArgs e)
    {
      var state = SaveManager.CurrentState();

      if (state == BonsaiSaveManager.SaveState.TempTree)
      {
        SaveManager.RequestSaveAs();
      }

      else if (state == BonsaiSaveManager.SaveState.SavedTree)
      {
        SaveManager.RequestSave();
      }
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

    public static void LogNotImplemented(string msg)
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
          if (w.Tree == treeSelected)
          {
            return false;
          }

          // Found a window with no active canvas.
          if (w.Tree == null)
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
        windowToUse.SaveManager.InitState();
        windowToUse.Repaint();
        return true;
      }

      return false;
    }
  }
}