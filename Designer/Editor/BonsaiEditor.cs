
using System;
using System.Collections.Generic;
using System.Diagnostics.Contracts;
using System.Linq;
using System.Reflection;
using Bonsai.Core;
using UnityEditor;
using UnityEngine;

namespace Bonsai.Designer
{
  /// <summary>
  /// The editor handles rendering, manipulation and viewing of the canvas.
  /// </summary>
  public class BonsaiEditor
  {
    private readonly BonsaiWindow window;
    public BonsaiCanvas Canvas { get; private set; }
    public Coord Coordinates { get; private set; }

    public EditorSelection NodeSelection { get; } = new EditorSelection();

    /// <summary>
    /// The multiple that grid snapping rounds to.
    /// It should be a multiple of the grid size.
    /// </summary>
    public static float SnapStep { get { return Preferences.snapStep; } }

    /// <summary>
    /// An external action is an action that starts outside the editor. e.g. Inspectors.
    /// Only one external action can be active at a time.
    /// </summary>
    public bool IsExternalActionActive { get; private set; } = false;

    public event EventHandler RepaintRequired;

    private static Dictionary<Type, NodeTypeProperties> behaviourNodes;

    private BonsaiNode nodeToPositionUnderMouse = null;

    private readonly GUIStyle modeStatusStyle = new GUIStyle { fontSize = 36, fontStyle = FontStyle.Bold };
    private readonly Rect modeStatusRect = new Rect(20f, 20f, 250f, 150f);

    private string editorModeLabel = "No Tree Set";

    private class EditorAction
    {
      public Action Apply;
      public Action Update = delegate { };
      public Action Draw = delegate { };
      public Action DrawOverlay = delegate { };

      public bool IsOneShot { get; set; } = true;
      public bool IsExternalAllowed { get; set; } = false;
    }

    private readonly static EditorAction emptyAction = new EditorAction
    {
      Apply = delegate { },
    };

    private EditorAction currentAction = emptyAction;

    public BonsaiEditor(BonsaiWindow window)
    {
      this.window = window;
      modeStatusStyle.normal.textColor = new Color(1f, 1f, 1f, 0.2f);

      NodeSelection.SingleSelected += OnSingleSelected;
      NodeSelection.AbortSelected += OnAbortSelected;
    }

    public void CancelAction()
    {
      currentAction = emptyAction;
      EditorStatusChanged(this, window.EditorMode.Value);
      IsExternalActionActive = false;
    }

    public void CanvasLostFocus(object sender, EventArgs e)
    {
      // External actions can be active when the editor is out of focus.
      if (!currentAction.IsExternalAllowed)
      {
        CancelAction();
      }
    }

    public void NodeClicked(object sender, BonsaiNode node)
    {
      // Not doing any action.
      if (currentAction == emptyAction)
      {
        // If we are not multi selecting, we can select the single node right now.
        // This condition is necessary, so multi-dragging works when clicking on a node under multi-select.
        if (!NodeSelection.IsMultiSelection)
        {
          NodeSelection.SelectSingleNode(node);
        }

        StartDrag();
      }
    }

    public void NodeUnclicked(object sender, BonsaiNode node)
    {
      // Quickly clicked a node when in multi-selection.
      // Select the single node.
      if (NodeSelection.IsMultiSelection)
      {
        NodeSelection.SelectSingleNode(node);
      }
    }

    public void InputClicked(object sender, BonsaiInputPort input)
    {
      // Connecting is only allowed in edit mode.
      if (IsEditMode)
      {
        StartConnection(EditorNodeConnecting.StartConnection(input));
      }
    }

    public void OutputClicked(object sender, BonsaiOutputPort output)
    {
      // Connecting is only allowed in edit mode.
      if (IsEditMode)
      {
        StartConnection(output);
      }
    }

    public void CanvasClicked(object sender, EventArgs args)
    {
      StartAreaSelection();
    }

    /// <summary>
    /// Apply the current action on unlick.
    /// </summary>
    /// <param name="sender"></param>
    /// <param name="args"></param>
    public void Unclicked(object sender, Event args)
    {
      currentAction.Apply();

      if (currentAction.IsOneShot)
      {
        currentAction = emptyAction;
      }
    }

    private void OnSingleSelected(object sender, BonsaiNode node)
    {
      // Push to end so it is rendered above all other nodes.
      Canvas.PushToEnd(node);
    }

    private void OnAbortSelected(object sender, ConditionalAbort abort)
    {
      UpdateOrderIndices();
    }

    private void StartAreaSelection()
    {
      Vector2 start = Event.current.mousePosition;
      SetAction(new EditorAction
      {
        Apply = () =>
        {
          Vector2 end = Event.current.mousePosition;
          var areaSelection = EditorAreaSelect.NodesUnderArea(Coordinates, Canvas, start, end);
          NodeSelection.SetCurrentSelection(areaSelection.ToList());
        },
        DrawOverlay = () =>
        {
          // Construct and display the rect.
          Vector2 end = Event.current.mousePosition;
          Rect selectionRect = EditorAreaSelect.SelectionScreenSpace(start, end);
          Color selectionColor = new Color(0f, 0.5f, 1f, 0.1f);
          Handles.DrawSolidRectangleWithOutline(selectionRect, selectionColor, Color.blue);
          OnRepaintRequired();
        }
      });
    }

    private void StartConnection(BonsaiOutputPort output)
    {
      if (output != null)
      {
        SetAction(new EditorAction
        {
          Apply = () =>
          {
            EditorNodeConnecting.FinishConnection(Coordinates, output);
          },
          Draw = () =>
          {
            var start = Coordinates.CanvasToScreenSpace(output.RectPosition.center);
            var end = Event.current.mousePosition;
            Drawer.DrawRectConnectionScreenSpace(start, end, Color.white);
            OnRepaintRequired();
          }
        });
      }
    }

    private void StartDrag()
    {
      // Dragging is only only allowed in edit mode.
      if (IsEditMode)
      {
        if (NodeSelection.IsSingleSelection)
        {
          StartSingleDrag();
        }

        else if (NodeSelection.IsMultiSelection)
        {
          StartMultiDrag();
        }
      }
    }

    private void StartSingleDrag()
    {
      BonsaiNode node = NodeSelection.SelectedNode;
      Vector2 offset = EditorSingleDrag.StartDrag(node, Coordinates.MousePosition());
      SetAction(new EditorAction
      {
        Apply = () => EditorSingleDrag.FinishDrag(node),
        Update = () =>
        {
          if (Event.current.type == EventType.MouseDrag)
          {
            EditorSingleDrag.Drag(node, Coordinates.MousePosition(), offset);
            OnRepaintRequired();
          }
        }
      });
    }

    private void StartMultiDrag()
    {
      var nodes = EditorMultiDrag.StartDrag(NodeSelection.Selected, Coordinates.MousePosition());
      SetAction(new EditorAction
      {
        Apply = () => EditorMultiDrag.FinishDrag(nodes),
        Update = () =>
        {
          if (Event.current.type == EventType.MouseDrag)
          {
            EditorMultiDrag.Drag(Coordinates.MousePosition(), nodes);
            OnRepaintRequired();
          }
        }
      });
    }

    public void StartLink(Type linkType, Action<BehaviourNode> linker)
    {
      SetAction(new EditorAction
      {
        Apply = () =>
        {
          BonsaiNode node = Coordinates.NodeUnderMouse();
          if (node != null)
          {
            EditorNodeLinking.ApplyLink(node, linkType, linker);
          }
          else
          {
            // Clicked canvas, cancel action.
            CancelAction();
          }
        },

        IsOneShot = false,
        IsExternalAllowed = true
      });

      IsExternalActionActive = true;
      editorModeLabel = "Link References";
    }

    private void SetAction(EditorAction action)
    {
      CancelAction();
      currentAction = action;
    }

    public void SetBehaviourTree(BehaviourTree tree)
    {
      Canvas = new BonsaiCanvas(tree);
      Coordinates = new Coord(Canvas, window);
    }

    private static BonsaiPreferences Preferences
    {
      get { return BonsaiPreferences.Instance; }
    }

    private bool IsEditMode
    {
      get { return window.EditorMode.Value == BonsaiWindow.Mode.Edit; }
    }

    public void Update()
    {
      currentAction.Update();
    }

    public void Draw()
    {
      // We need to wait for the generic menu to finish
      // in order to get a valid mouse position.
      HandleNewNodeToPositionUnderMouse();

      if (Event.current.type == EventType.Repaint)
      {
        DrawGrid();
        DrawMode();
      }

      DrawCanvasContents();
    }

    public void EditorStatusChanged(object sender, BonsaiWindow.Mode status)
    {
      if (!window.Tree)
      {
        editorModeLabel = "No Tree Set";
      }
      else
      {
        editorModeLabel = status == BonsaiWindow.Mode.Edit ? "Edit" : "View";
      }
    }

    #region Editing

    /// <summary>
    /// Translates the canvas.
    /// </summary>
    /// <param name="delta">The amount to translate the canvas.</param>
    public void Pan(Vector2 delta)
    {
      Canvas.panOffset += delta * Canvas.ZoomScale * BonsaiCanvas.PanSpeed;

      // Round to keep panning sharp.
      Canvas.panOffset.x = Mathf.Round(Canvas.panOffset.x);
      Canvas.panOffset.y = Mathf.Round(Canvas.panOffset.y);
    }

    /// <summary>
    /// Scales the canvas.
    /// </summary>
    /// <param name="zoomDirection">+1 to zoom in and -1 to zoom out.</param>
    public void Zoom(float zoomDirection)
    {
      float scale = (zoomDirection < 0f) ? (1f - BonsaiCanvas.ZoomDelta) : (1f + BonsaiCanvas.ZoomDelta);
      Canvas.zoom *= scale;

      float cap = Mathf.Clamp(Canvas.zoom.x, BonsaiCanvas.MinZoom, BonsaiCanvas.MaxZoom);
      Canvas.zoom.Set(cap, cap);
    }

    public void UpdateNodeGUI(BehaviourNode behaviour)
    {
      BonsaiNode node = Canvas.First(n => n.Behaviour == behaviour);
      node.UpdateGui();
      node.UpdatePortPositions();

      // Snap so connections align.
      node.Center = Coord.SnapPosition(node.Center, SnapStep);
    }

    public void UpdateOrderIndices()
    {
      window.Tree.CalculateTreeOrders();
    }

    #endregion

    #region Drawing

    protected virtual void OnRepaintRequired()
    {
      RepaintRequired?.Invoke(this, EventArgs.Empty);
    }

    public void DrawStaticGrid()
    {
      Drawer.DrawStaticGrid(window.CanvasRect, Preferences.gridTexture);
    }

    private void DrawGrid()
    {
      Drawer.DrawGrid(window.CanvasRect, Preferences.gridTexture, Canvas.ZoomScale, Canvas.panOffset);
    }

    private void DrawCanvasContents()
    {
      ScaleUtility.BeginScale(window.CanvasRect, Canvas.ZoomScale, BonsaiWindow.toolbarHeight);

      currentAction.Draw();
      DrawPortConnections();
      DrawNodes();

      ScaleUtility.EndScale(window.CanvasRect, Canvas.ZoomScale, BonsaiWindow.toolbarHeight);

      // Overlays and idependent of zoom.
      currentAction.DrawOverlay();
    }

    private void DrawNodes()
    {
      if (window.EditorMode.Value == BonsaiWindow.Mode.Edit)
      {
        DrawNodesInEditMode();
      }
      else
      {
        DrawNodesInViewMode();
      }
    }

    private void DrawNodesInEditMode()
    {
      foreach (var node in Canvas.NodesInDrawOrder)
      {
        if (Coordinates.IsInView(node))
        {
          Drawer.DrawNode(Coordinates, node, NodeStatusColor(node));
          Drawer.DrawPorts(Coordinates, node);
        }
      }
    }

    // Does not render ports in view mode since nodes cannot be changed.
    private void DrawNodesInViewMode()
    {
      foreach (var node in Canvas.NodesInDrawOrder)
      {
        if (Coordinates.IsInView(node))
        {
          Drawer.DrawNode(Coordinates, node, NodeStatusColor(node));
        }
      }
    }

    private void DrawPortConnections()
    {
      foreach (var node in Canvas.NodesInDrawOrder)
      {
        if (node.Output != null)
        {
          Drawer.DrawDefaultPortConnections(Coordinates, node);
        }
      }
    }

    /// <summary>
    /// Draw the window mode in the background.
    /// </summary>
    public void DrawMode()
    {
      GUI.Label(modeStatusRect, editorModeLabel, modeStatusStyle);
    }

    #endregion

    private Color NodeStatusColor(BonsaiNode node)
    {
      if (IsNodeEvaluating(node))
      {
        return Preferences.evaluateColor;
      }
      else if (IsNodeRunning(node))
      {
        return Preferences.runningColor;
      }
      else if (NodeSelection.IsNodeSelected(node))
      {
        return Preferences.selectedColor;
      }
      else if (NodeSelection.IsReferenced(node))
      {
        return Preferences.referenceColor;
      }
      else if (IsNodeAbortable(node))
      {
        return Preferences.abortColor;
      }
      else if (window.Tree.Root == node.Behaviour)
      {
        return Preferences.rootSymbolColor;
      }

      return Preferences.defaultNodeBackgroundColor;
    }

    [Pure]
    private bool IsNodeRunning(BonsaiNode node)
    {
      return node.Behaviour.GetStatusEditor() == BehaviourNode.StatusEditor.Running;
    }

    // Highlights nodes that can be aborted by the currently selected node.
    [Pure]
    private bool IsNodeAbortable(BonsaiNode node)
    {
      // Root must exist.
      if (!window.Tree.Root)
      {
        return false;
      }

      BonsaiNode selected = NodeSelection.SelectedNode;

      // A node must be selected.
      if (selected == null)
      {
        return false;
      }

      // The selected node must be a conditional abort.
      var aborter = selected.Behaviour as ConditionalAbort;

      // Node can be aborted by the selected aborter.
      return aborter && ConditionalAbort.IsAbortable(aborter, node.Behaviour);
    }

    /// <summary>
    /// Highlights nodes that are being re-evaluated, like abort nodes.
    /// </summary>
    /// <param name="node"></param>
    [Pure]
    private bool IsNodeEvaluating(BonsaiNode node)
    {
      if (!EditorApplication.isPlaying)
      {
        return false;
      }

      BehaviourIterator itr = node.Behaviour.Iterator;

      if (itr != null && itr.IsRunning)
      {
        var aborter = node.Behaviour as ConditionalAbort;
        int index = itr.CurrentIndex;
        if (index != -1)
        {
          BehaviourNode currentNode = window.Tree.GetNode(index);

          // The current running node can be aborted by it.
          return aborter && ConditionalAbort.IsAbortable(aborter, currentNode);
        }
      }

      return false;
    }

    private void HandleNewNodeToPositionUnderMouse()
    {
      if (nodeToPositionUnderMouse != null)
      {
        nodeToPositionUnderMouse.Position = Coordinates.MousePosition();
        nodeToPositionUnderMouse = null;
      }
    }

    public void SetNewNodeToPositionUnderMouse(BonsaiNode node)
    {
      nodeToPositionUnderMouse = node;
    }

    #region Node Type Properties

    public static void FetchBehaviourNodes()
    {
      behaviourNodes = new Dictionary<Type, NodeTypeProperties>();

      IEnumerable<Type> behaviourTypes = AppDomain.CurrentDomain.GetAssemblies()
         .Where(asm => asm.FullName.Contains("Assembly") && !asm.FullName.Contains("Editor"))
         .SelectMany(asm => asm.GetTypes())
         .Where(t => t.IsSubclassOf(typeof(BehaviourNode)) && !t.IsAbstract);

      foreach (Type type in behaviourTypes)
      {
        var nodeMeta = type.GetCustomAttribute<BonsaiNodeAttribute>(false);

        // Default menu path if unspecified.
        string menuPath = "User/";

        // Service base class is abstract. For Service types, default to Service texture.
        string texName = typeof(Service).IsAssignableFrom(type) ? "Service" : "Play";

        if (nodeMeta != null)
        {
          if (!string.IsNullOrEmpty(nodeMeta.menuPath))
          {
            menuPath = nodeMeta.menuPath;
          }

          // Texxture names are optional. Use only if specified.
          if (!string.IsNullOrEmpty(nodeMeta.texturePath))
          {
            texName = nodeMeta.texturePath;
          }
        }

        bool bCreateInput = false;
        bool bCreateOutput = false;
        bool bCanHaveMultipleChildren = false;

        // Only action nodes have an input and no output.
        if (type.IsSubclassOf(typeof(Task)))
        {
          bCreateInput = true;
        }

        // Composites and decorators have in and out.
        else
        {
          bCreateInput = true;
          bCreateOutput = true;

          // Only composites can have more than 1 child.
          if (type.IsSubclassOf(typeof(Composite)))
          {
            bCanHaveMultipleChildren = true;
          }
        }

        menuPath += type.Name;
        var prop = new NodeTypeProperties(menuPath, texName, bCreateInput, bCreateOutput, bCanHaveMultipleChildren);

        behaviourNodes.Add(type, prop);
      }
    }

    public static IEnumerable<KeyValuePair<Type, NodeTypeProperties>> Behaviours
    {
      get { return behaviourNodes; }
    }

    public static NodeTypeProperties GetNodeTypeProperties(Type t)
    {
      if (behaviourNodes.ContainsKey(t))
      {
        return behaviourNodes[t];
      }

      return null;
    }

    public class NodeTypeProperties
    {
      public string path, texName;
      public bool bCreateInput, bCreateOutput, bCanHaveMultipleChildren;

      public NodeTypeProperties(string path, string texName, bool bCreateInput, bool bCreateOutput, bool bCanHaveMultipleChildren)
      {
        this.path = path;
        this.texName = texName;
        this.bCreateInput = bCreateInput;
        this.bCreateOutput = bCreateOutput;
        this.bCanHaveMultipleChildren = bCanHaveMultipleChildren;
      }
    }

    #endregion
  }
}