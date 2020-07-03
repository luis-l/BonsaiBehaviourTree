
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using Bonsai.Core;
using Bonsai.Utility;
using UnityEditor;
using UnityEngine;

namespace Bonsai.Designer
{
  /// <summary>
  /// The editor applies changes to the canvas.
  /// </summary>
  public class BonsaiEditor
  {
    public BonsaiCanvas Canvas { get; private set; }
    public BonsaiInput Input { get; } = new BonsaiInput();
    public BonsaiViewer Viewer { get; set; }
    public EditorSelection NodeSelection { get; } = new EditorSelection();

    /// <summary>
    /// The multiple that grid snapping rounds to.
    /// It should be a multiple of the grid size.
    /// </summary>
    public static float SnapStep { get { return Preferences.snapStep; } }

    public enum Mode { Edit, View };
    public ReactiveValue<Mode> EditorMode = new ReactiveValue<Mode>();

    public event EventHandler CanvasChanged;

    private static Dictionary<Type, NodeTypeProperties> behaviourNodes;

    /// <summary>
    /// An action that uses the movement between a start and end location to apply a change.
    /// e.g. Area selection, node drag, connections.
    /// </summary>
    private Action<CanvasTransform> MotionAction;
    private Action<BonsaiInputEvent> ApplyAction;
    private Predicate<BonsaiInputEvent> CanApplyAction;

    // Context Menus block Events. 
    // When a new node is created from the context menu we need to position it under the mouse.
    private BonsaiNode lastCreatedNodeToPosition = null;

    public BonsaiEditor()
    {
      NodeSelection.SingleSelected += OnSingleSelected;
      NodeSelection.AbortSelected += OnAbortSelected;

      Input.selection = NodeSelection;
      Input.MouseDown += MouseDown;
      Input.Click += Clicked;
      Input.MouseUp += MouseUp;
      Input.CanvasLostFocus += CanvasLostFocus;
      Input.NodeContextClick += NodeContextClicked;
      Input.CanvasContextClick += CanvasContextClicked;
      Input.CreateNodeRequest += CreateNodeFromType;
      Input.NodeActionRequest += SingleNodeAction;
      Input.MultiNodeActionRequest += MultiNodeAction;

      EditorMode.ValueChanged += (s, mode) =>
      {
        Input.EditInputEnabled = mode == Mode.Edit;
        Viewer.SetEditorMode(mode);
      };
    }

    private void CreateNodeFromType(object sender, Type type)
    {
      BonsaiNode node = Canvas.CreateNode(type);
      NodeSelection.SetSingleSelection(node);
      lastCreatedNodeToPosition = node;
    }

    private void SingleNodeAction(object sender, BonsaiInput.NodeContext actionType)
    {
      switch (actionType)
      {
        case BonsaiInput.NodeContext.SetAsRoot:
          Canvas.SetRoot(NodeSelection.SingleSelectedNode);
          break;

        case BonsaiInput.NodeContext.Duplicate:
          Type nodeType = NodeSelection.SingleSelectedNode.Behaviour.GetType();
          EditorNodeCreation.DuplicateSingle(Canvas, NodeSelection.SingleSelectedNode);
          break;

        case BonsaiInput.NodeContext.ChangeType:
          // TODO
          BonsaiWindow.LogNotImplemented("Change Type");
          break;

        case BonsaiInput.NodeContext.Delete:
          Canvas.Remove(node => NodeSelection.IsNodeSelected(node));
          break;
      }
    }

    private void MultiNodeAction(object sender, BonsaiInput.NodeContext actionType)
    {
      switch (actionType)
      {
        case BonsaiInput.NodeContext.DuplicateSelection:
          var duplicates = EditorNodeCreation.DuplicateMultiple(Canvas, NodeSelection.SelectedNodes);
          NodeSelection.SetMultiSelection(duplicates);
          break;
        case BonsaiInput.NodeContext.DeleteSelection:
          Canvas.Remove(node => NodeSelection.IsNodeSelected(node));
          NodeSelection.SetTreeSelection(Canvas.Tree);
          break;
      }
    }

    private void MouseDown(object sender, BonsaiInputEvent inputEvent)
    {
      // Busy, action is active.
      if (MotionAction != null)
      {
        return;
      }

      if (inputEvent.IsPortFocused())
      {
        StartConnection(inputEvent);
      }

      else if (inputEvent.IsNodeFocused())
      {
        // Apply node linking.
        if (!NodeSelection.IsEmpty && Event.current.shift)
        {
          BonsaiNode sourceLink = NodeSelection.FirstNodeSelected;
          BonsaiNode nodeToLink = inputEvent.node;
          if (EditorNodeLinking.ApplyLink(sourceLink.Behaviour, nodeToLink.Behaviour))
          {
            NodeSelection.SetReferenced(sourceLink.Behaviour);
          }
        }

        // Extended selection mode.
        else if (Event.current.control)
        {
          NodeSelection.ToggleSelecion(inputEvent.node);
        }

        else if (!NodeSelection.IsMultiSelection)
        {
          // If we are not multi selecting, we can select the single node right now.
          // This condition is necessary, so multi-dragging works when clicking on a node under multi-select.
          NodeSelection.SetSingleSelection(inputEvent.node);
        }

        StartDrag(inputEvent);
      }

      else
      {
        NodeSelection.SetTreeSelection(Canvas.Tree);
        // Canvas was clicked on.
        StartAreaSelection(inputEvent);
      }
    }

    private void Clicked(object sender, BonsaiInputEvent inputEvent)
    {
      // Quickly clicked a node when in multi-selection.
      // Select the single node.
      if (!Event.current.control && inputEvent.node != null && NodeSelection.IsMultiSelection)
      {
        NodeSelection.SetSingleSelection(inputEvent.node);
      }
    }

    private void NodeContextClicked(object sender, BonsaiNode node)
    {
      NodeSelection.SetSingleSelection(node);
    }

    private void CanvasContextClicked(object sender, EventArgs e)
    {
      NodeSelection.SetTreeSelection(Canvas.Tree);
    }

    private void MouseUp(object sender, BonsaiInputEvent inputEvent)
    {
      if (CanApplyAction == null || CanApplyAction(inputEvent))
      {
        ApplyAction?.Invoke(inputEvent);
      }
      ClearActions();
    }

    public void ClearActions()
    {
      CanApplyAction = null;
      ApplyAction = null;
      MotionAction = null;
      Viewer.CustomDraw = null;
      Viewer.CustomOverlayDraw = null;
    }

    public void CanvasLostFocus(object sender, EventArgs e)
    {
      ClearActions();
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

    private void OnCanvasChanged()
    {
      CanvasChanged?.Invoke(this, EventArgs.Empty);
    }

    private void StartAreaSelection(BonsaiInputEvent startEvent)
    {
      Vector2 startScreenSpace = Event.current.mousePosition;
      Vector2 start = startEvent.canvasMousePostion;

      ApplyAction = (BonsaiInputEvent applyEvent) =>
      {
        Vector2 end = applyEvent.canvasMousePostion;
        var areaSelection = EditorAreaSelect.NodesUnderArea(Canvas.Nodes, start, end);
        NodeSelection.SetMultiSelection(areaSelection.ToList());
      };

      Viewer.CustomOverlayDraw = () =>
      {
        // Construct and display the rect.
        Vector2 endScreenSpace = Event.current.mousePosition;
        Rect selectionRect = EditorAreaSelect.SelectionArea(startScreenSpace, endScreenSpace);
        Color selectionColor = new Color(0f, 0.5f, 1f, 0.1f);
        Handles.DrawSolidRectangleWithOutline(selectionRect, selectionColor, Color.blue);
        OnCanvasChanged();
      };
    }

    private void StartConnection(BonsaiInputEvent startEvent)
    {
      BonsaiNode parent = startEvent.isOutputFocused
        ? startEvent.node
        : EditorNodeConnecting.StartConnection(startEvent.node);

      if (parent != null)
      {
        ApplyAction = (BonsaiInputEvent applyEvent)
          => EditorNodeConnecting.FinishConnection(Canvas, parent, applyEvent.node);

        CanApplyAction = (BonsaiInputEvent checkEvent) => checkEvent.node != null;

        Viewer.CustomDraw = (CanvasTransform t) =>
        {
          var start = t.CanvasToScreenSpace(parent.OutputRect.center);
          var end = Event.current.mousePosition;
          Drawer.DrawRectConnectionScreenSpace(start, end, Color.white);
          OnCanvasChanged();
        };
      }
    }

    private void StartDrag(BonsaiInputEvent e)
    {
      // Dragging is only only allowed in edit mode.
      if (IsEditMode)
      {
        if (NodeSelection.IsSingleSelection)
        {
          StartSingleDrag(e);
        }

        else if (NodeSelection.IsMultiSelection)
        {
          StartMultiDrag(e);
        }
      }
    }

    private void StartSingleDrag(BonsaiInputEvent startEvent)
    {
      BonsaiNode node = startEvent.node;
      Vector2 offset = EditorSingleDrag.StartDrag(node, startEvent.canvasMousePostion);
      MotionAction = (CanvasTransform t) => EditorSingleDrag.Drag(node, BonsaiInput.MousePosition(t), offset);
    }

    private void StartMultiDrag(BonsaiInputEvent startEvent)
    {
      var nodes = EditorMultiDrag.StartDrag(NodeSelection.SelectedNodes, startEvent.canvasMousePostion);
      MotionAction = (CanvasTransform t) => EditorMultiDrag.Drag(BonsaiInput.MousePosition(t), nodes);
    }

    public void SetBehaviourTree(BehaviourTree tree)
    {
      NodeSelection.ClearSelection();
      Canvas = new BonsaiCanvas(tree);
      Viewer.Canvas = Canvas;
      Viewer.NodeSelection = NodeSelection;
      Viewer.zoom = tree.zoomPosition;
      Viewer.panOffset = tree.panPosition;
    }

    private static BonsaiPreferences Preferences
    {
      get { return BonsaiPreferences.Instance; }
    }

    public void PollInput(Event e, CanvasTransform t, Rect inputRect)
    {
      if (lastCreatedNodeToPosition != null)
      {
        lastCreatedNodeToPosition.Center = BonsaiInput.MousePosition(t);
        lastCreatedNodeToPosition = null;
      }

      if (e.type == EventType.MouseDrag)
      {
        if (MotionAction != null)
        {
          MotionAction(t);
          OnCanvasChanged();
        }
      }

      if (BonsaiInput.IsPanAction(e))
      {
        Pan(e.delta);
        OnCanvasChanged();
      }

      if (BonsaiInput.IsZoomAction(e))
      {
        Zoom(e.delta.y);
        OnCanvasChanged();
      }

      Input.HandleMouseEvents(e, t, Canvas.Nodes, inputRect);
    }

    public bool IsEditMode { get { return EditorMode.Value == Mode.Edit; } }

    /// <summary>
    /// Translates the canvas.
    /// </summary>
    /// <param name="delta">The amount to translate the canvas.</param>
    public void Pan(Vector2 delta)
    {
      Viewer.panOffset += delta * Viewer.ZoomScale * BonsaiViewer.PanSpeed;

      // Round to keep panning sharp.
      Viewer.panOffset.x = Mathf.Round(Viewer.panOffset.x);
      Viewer.panOffset.y = Mathf.Round(Viewer.panOffset.y);
    }

    /// <summary>
    /// Scales the canvas.
    /// </summary>
    /// <param name="zoomDirection">+1 to zoom in and -1 to zoom out.</param>
    public void Zoom(float zoomDirection)
    {
      float scale = (zoomDirection < 0f) ? (1f - BonsaiViewer.ZoomDelta) : (1f + BonsaiViewer.ZoomDelta);
      Viewer.zoom *= scale;

      float cap = Mathf.Clamp(Viewer.zoom.x, BonsaiViewer.MinZoom, BonsaiViewer.MaxZoom);
      Viewer.zoom.Set(cap, cap);
    }

    public void UpdateNodeGUI(BehaviourNode behaviour)
    {
      BonsaiNode node = Canvas.Nodes.First(n => n.Behaviour == behaviour);
      node.UpdateGui();

      // Snap so connections align.
      node.Center = MathExtensions.SnapPosition(node.Center, SnapStep);
    }

    public void UpdateOrderIndices()
    {
      Canvas.Tree.CalculateTreeOrders();
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

        // Only composites and decorators have outputs.
        bool addOutput = !type.IsSubclassOf(typeof(Task));

        menuPath += type.Name;
        var prop = new NodeTypeProperties(menuPath, texName, addOutput);

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
      public bool addOutput;

      public NodeTypeProperties(string path, string texName, bool addOutput)
      {
        this.path = path;
        this.texName = texName;
        this.addOutput = addOutput;
      }
    }

    #endregion
  }
}