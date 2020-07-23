using System.Collections.Generic;
using System.Linq;
using Bonsai.Core;
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

    private BonsaiEditor Editor { get; set; }
    public IReadOnlyList<BonsaiNode> Nodes { get { return Editor.Canvas.Nodes; } }
    public BonsaiViewer Viewer { get; private set; }
    public BonsaiSaver Saver { get; private set; }

    // The editor state without needing a reference to the Editor instance.
    // This is used to solve initialization order issues for OnEnable.
    // This allows Inspectors to view the editor mode if they were enabled before the window.
    public BonsaiEditor.Mode EditorMode { get; private set; }

    void OnEnable()
    {
      BonsaiPreferences.Instance = BonsaiPreferences.LoadDefaultPreferences();
      BonsaiEditor.FetchBehaviourNodes();

      Editor = new BonsaiEditor();
      Viewer = new BonsaiViewer();
      Saver = new BonsaiSaver();

      Saver.SaveMessage += (sender, message) => ShowNotification(new GUIContent(message));

      Editor.Viewer = Viewer;
      Editor.Input.SaveRequest += (s, e) => Save();
      Editor.CanvasChanged += (s, e) => Repaint();
      Editor.Input.MouseDown += (s, e) => Repaint();
      Editor.Input.MouseUp += (s, e) => Repaint();
      Editor.EditorMode.ValueChanged += (s, mode) => { EditorMode = mode; };

      EditorApplication.playModeStateChanged += PlayModeStateChanged;
      AssemblyReloadEvents.beforeAssemblyReload += BeforeAssemblyReload;
      Selection.selectionChanged += SelectionChanged;

      BuildCanvas();
      Editor.EditorMode.Value = BonsaiEditor.Mode.Edit;
      SwitchToViewModeIfRequired();
    }

    void OnDisable()
    {
      EditorApplication.playModeStateChanged -= PlayModeStateChanged;
      AssemblyReloadEvents.beforeAssemblyReload -= BeforeAssemblyReload;
      Selection.selectionChanged -= SelectionChanged;
    }

    void OnDestroy()
    {
      OnExit();
    }

    void OnGUI()
    {
      if (Tree == null)
      {
        Viewer.DrawStaticGrid(position.size);
        Viewer.DrawMode();
        Editor.EditorMode.Value = BonsaiEditor.Mode.Edit;
      }

      else
      {
        // Make sure to build a canvas for an active tree.
        if (Editor.Canvas == null)
        {
          BuildCanvas();
        }

        CanvasTransform t = Transform;
        Editor.PollInput(Event.current, t, CanvasInputRect);
        Viewer.Draw(t);
      }

      DrawToolbar();
      UpdateWindowTitle();
    }

    void Update()
    {
      // Update the window during the play mode when the window
      // is viewing a tree instance of a game object.
      // This is to quicky update all changes of the tree.
      bool bConditions =
          Editor.EditorMode.Value == BonsaiEditor.Mode.View &&
          EditorApplication.isPlaying &&
          Tree &&
          Tree.IsRunning();

      if (bConditions)
      {
        Repaint();
      }
    }

    private void BeforeAssemblyReload()
    {
      // Do not attempt to do saving if about to enter play mode since that is handled in PlayModeStateChanged.
      if (!EditorApplication.isPlayingOrWillChangePlaymode)
      {
        OnExit();
      }
    }

    private void PlayModeStateChanged(PlayModeStateChange state)
    {
      // Before entering play mode, attempt to save the current tree asset. 
      if (state == PlayModeStateChange.ExitingEditMode)
      {
        QuickSave();
      }

      if (state == PlayModeStateChange.EnteredPlayMode)
      {
        SwitchToViewModeIfRequired();
      }
    }

    private void SelectionChanged()
    {
      SwitchToViewModeIfRequired();
    }

    /// <summary>
    /// Updates the GUI contents for each node that is currently selected.
    /// </summary>
    public void UpdateSelectedNodesGUI()
    {
      Editor.UpdateNodesGUI(Editor.NodeSelection.SelectedNodes);
      Repaint();
    }

    /// <summary>
    /// Updates the GUI contents for the node.
    /// </summary>
    /// <param name="behaviour">The associated visual node will be update for this behaviour.</param>
    public void UpdateNodeGUI(BehaviourNode behaviour)
    {
      Editor.UpdateNodeGUI(behaviour);
      Repaint();
    }

    public bool ContainsNode(BehaviourNode behaviour)
    {
      return Editor.Canvas.Nodes.Select(n => n.Behaviour).Contains(behaviour);
    }

    private void SwitchToViewModeIfRequired()
    {
      // Cannot go to view mode.
      if (!EditorApplication.isPlaying || !Selection.activeGameObject)
      {
        return;
      }

      var btc = Selection.activeGameObject.GetComponent<BonsaiTreeComponent>();
      BehaviourTree treeToView = btc ? btc.Tree : null;

      // There must be a non-null tree to view,
      // it must be a different tree than the active tree for this window,
      // and must not be opened somewhere else.
      if (treeToView && Tree != treeToView)
      {
        var windows = Resources.FindObjectsOfTypeAll<BonsaiWindow>();

        bool alreadyInView = windows.Any(w => w.Tree == treeToView);

        if (alreadyInView)
        {
          return;
        }

        BonsaiWindow window = windows.FirstOrDefault(w => !w.Tree);

        // Have the window without a set tree to view the tree selected.
        if (window)
        {
          window.SetTree(treeToView, BonsaiEditor.Mode.View);
        }
        else
        {
          // View tree in this window.
          SetTree(treeToView, BonsaiEditor.Mode.View);
        }

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
        if (Editor.Canvas.Root == null)
        {
          ShowNotification(new GUIContent("Set a root to nicely format the tree!"));
        }
        else
        {
          Formatter.PositionNodesNicely(Editor.Canvas.Root, Vector2.zero);
        }
      }
    }

    public void SetTree(BehaviourTree bt, BonsaiEditor.Mode mode = BonsaiEditor.Mode.Edit)
    {
      behaviourTree = bt;
      BuildCanvas();
      Editor.EditorMode.Value = mode;
    }

    private void DrawToolbar()
    {
      EditorGUILayout.BeginHorizontal("Toolbar");

      if (GUILayout.Button("File", EditorStyles.toolbarDropDown, GUILayout.Width(50f)))
      {
        if (Editor.EditorMode.Value == BonsaiEditor.Mode.Edit)
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
        fileMenu.AddItem(new GUIContent("Home Zoom"), false, HomeZoom);
        fileMenu.DropDown(new Rect(55f, toolbarHeight, 0f, 0f));
      }

      if (GUILayout.Button("Tools", EditorStyles.toolbarDropDown, GUILayout.Width(50f)))
      {
        var fileMenu = new GenericMenu();
        fileMenu.AddItem(new GUIContent("Nicefy Tree"), false, NicifyTree);
        fileMenu.AddItem(new GUIContent("Refresh Editor"), false, RefreshEditor);
        fileMenu.DropDown(new Rect(105f, toolbarHeight, 0f, 0f));
      }

      GUILayout.FlexibleSpace();
      GUILayout.Label(TreeName());
      EditorGUILayout.EndHorizontal();
    }

    private string TreeName()
    {
      return Tree
        ? (Tree.name.Length == 0 ? "New Tree" : Tree.name)
        : "None";
    }

    private void UpdateWindowTitle()
    {
      if (Tree != null && Tree.name.Length != 0)
      {
        if (titleContent.text != Tree.name)
        {
          titleContent.text = Tree.name;
        }
      }
      else
      {
        titleContent.text = "Bonsai";
      }
    }

    private void CreateFileMenuEditable()
    {
      var fileMenu = new GenericMenu();

      fileMenu.AddItem(new GUIContent("Create New"), false, CreateNew);
      fileMenu.AddSeparator("");
      fileMenu.AddItem(new GUIContent("Load"), false, Load);
      fileMenu.AddItem(new GUIContent("Save"), false, Save);
      fileMenu.DropDown(new Rect(5f, toolbarHeight, 0f, 0f));
    }

    private void CreateFileMenuViewOnly()
    {
      var fileMenu = new GenericMenu();

      fileMenu.AddDisabledItem(new GUIContent("Create New"));
      fileMenu.AddSeparator("");
      fileMenu.AddDisabledItem(new GUIContent("Load"));
      fileMenu.AddDisabledItem(new GUIContent("Save"));
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

    private void CreateNew()
    {
      QuickSave();
      SetTree(BonsaiSaver.CreateBehaviourTree());
      ShowNotification(new GUIContent("New Tree Created"));
    }

    private void Load()
    {
      // Save current canvas.
      QuickSave();

      BehaviourTree tree = Saver.LoadBehaviourTree();
      if (tree)
      {
        SetTree(tree);
      }
    }

    // Standard save procedure. Tree not in the AssetDatabase will prompt the user to select a save file.
    private void Save()
    {
      if (Editor.Canvas != null)
      {
        Saver.SaveCanvas(Editor.Canvas, TreeMetaData);
      }
    }

    // A quick save only saves tree assets that already exist in the AssetDatabase.
    private void QuickSave()
    {
      if (EditorMode == BonsaiEditor.Mode.Edit && Saver.CanSaveTree(Tree))
      {
        Saver.SaveCanvas(Editor.Canvas, TreeMetaData);
      }
    }

    private void OnExit()
    {
      // This is to prevent active selection on objects that are no longer focused or do not exist after destroy.
      Editor.NodeSelection.ClearSelection();
      QuickSave();
    }

    private CanvasTransform Transform
    {
      get
      {
        return new CanvasTransform
        {
          pan = Viewer.panOffset,
          zoom = Viewer.ZoomScale,
          size = position.size
        };
      }
    }

    private BonsaiSaver.TreeMetaData TreeMetaData
    {
      get
      {
        return new BonsaiSaver.TreeMetaData
        {
          zoom = Viewer.zoom,
          pan = Viewer.panOffset
        };
      }
    }

    /// <summary>
    /// The rect used to filter input.
    /// This is so the toolbar is not ignored by editor inputs.
    /// </summary>
    public Rect CanvasInputRect
    {
      get
      {
        var rect = new Rect(Vector2.zero, position.size);
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
    /// Opens the tree in a Bonsai Window.
    /// </summary>
    /// <param name="tree">The tree to open</param>
    /// <returns>
    /// The window that opens the tree. Null if already opened.
    /// </returns>
    public static BonsaiWindow OpenTree(BehaviourTree tree, BonsaiEditor.Mode mode = BonsaiEditor.Mode.Edit)
    {
      if (!tree)
      {
        return null;
      }

      // Try to find an editor window without a canvas...
      var windows = Resources.FindObjectsOfTypeAll<BonsaiWindow>();

      bool isAlreadyOpened = windows.Any(w => w.Tree == tree);

      if (isAlreadyOpened)
      {
        return null;
      }

      // Find a window without any tree.
      BonsaiWindow window = windows.FirstOrDefault(w => w.Tree == null);

      // No windows available, make a new one.
      if (!window)
      {
        window = CreateInstance<BonsaiWindow>();
        window.Show();
      }

      window.SetTree(tree, mode);
      return window;
    }

    /// <summary>
    /// Opens up the Bonsai window from asset selection.
    /// </summary>
    /// <param name="instanceID"></param>
    /// <param name="line"></param>
    /// <returns></returns>
    [OnOpenAsset(0)]
    static bool OpenCanvasAsset(int instanceID, int line)
    {
      var tree = EditorUtility.InstanceIDToObject(instanceID) as BehaviourTree;
      BonsaiWindow w = OpenTree(tree);
      if (w)
      {
        // If a tree asset was created but has no blackboard, add one upon opening.
        // This is for convenience.
        BonsaiSaver.AddBlackboardIfMissing(tree);

        w.SwitchToViewModeIfRequired();
      }

      return w;
    }
  }
}