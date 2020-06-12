
using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;

using Bonsai.Core;

namespace Bonsai.Designer
{
  /// <summary>
  /// Handles inputs and stores input states.
  /// </summary>
  public class BonsaiInputHandler
  {
    private BonsaiWindow _window;
    private GenericMenu _nodeTypeSelectionMenu;
    private GenericMenu _nodeContextMenu;
    private GenericMenu _multiNodeContextMenu;

    private Action<BonsaiNode> onSingleSelected;
    public enum NodeContext { SetAsRoot, Duplicate, ChangeType, Delete, DuplicateSelection, DeleteSelection };

    #region Input State Properties and Members

    // The relative position between the node and the mouse when it was click for dragging.
    private Vector2 _singleDragOffset = Vector2.zero;
    private List<Vector2> _multiDragOffsets = new List<Vector2>();

    // The nodes that will be dragged.
    private List<BonsaiNode> _draggingSubroots = new List<BonsaiNode>();

    private bool _bIsDragging = false;
    private Vector2 _areaSelectStartPos = Vector2.zero;

    // The node that is currently dragged.
    private BonsaiNode _draggingNode = null;

    // The nodes selected from an area/multi selection.
    private List<BonsaiNode> _selectedNodes = new List<BonsaiNode>();
    private Type _referenceLinkType = typeof(BehaviourNode);
    private Action<BehaviourNode> onSelectedForLinking = null;

    internal bool IsRefLinking { get; private set; } = false;

    public bool IsMakingConnection { get; private set; } = false;

    public BonsaiOutputPort OutputToConnect { get; private set; } = null;

    public bool IsAreaSelecting { get; private set; } = false;

    private void resetState()
    {
      IsAreaSelecting = false;
      _bIsDragging = false;
      IsMakingConnection = false;

      _draggingNode = null;
      OutputToConnect = null;
    }

    public BonsaiNode SelectedNode { get; private set; } = null;

    #endregion

    public BonsaiInputHandler(BonsaiWindow w)
    {
      _window = w;
      _nodeTypeSelectionMenu = new GenericMenu();
      _nodeContextMenu = new GenericMenu();
      _multiNodeContextMenu = new GenericMenu();

      // Setup Node Selection menu.
      foreach (var kvp in BonsaiEditor.Behaviours)
      {

        Type nodeType = kvp.Key;
        BonsaiEditor.NodeTypeProperties prop = kvp.Value;

        _nodeTypeSelectionMenu.AddItem(new GUIContent(prop.path), false, onNodeCreateCallback, nodeType);
      }

      // Setup node context menu.
      _nodeContextMenu.AddItem(new GUIContent("Set As Root"), false, onNodeContextCallback, NodeContext.SetAsRoot);
      _nodeContextMenu.AddItem(new GUIContent("Duplicate"), false, onNodeContextCallback, NodeContext.Duplicate);
      _nodeContextMenu.AddItem(new GUIContent("Change Type"), false, onNodeContextCallback, NodeContext.ChangeType);
      _nodeContextMenu.AddItem(new GUIContent("Delete"), false, onNodeContextCallback, NodeContext.Delete);

      // Setup area selection context menu.
      _multiNodeContextMenu.AddItem(new GUIContent("Duplicate"), false, onMultiNodeCallback, NodeContext.DuplicateSelection);
      _multiNodeContextMenu.AddItem(new GUIContent("Delete"), false, onMultiNodeCallback, NodeContext.DeleteSelection);

      // Define the actions to take when a single node is selected.
      onSingleSelected = (node) =>
      {
        // Only apply single node selection on a node that is
        // not currently under area selection.
        if (!node.bAreaSelectionFlag)
        {

          _window.editor.ClearReferencedNodes();
          clearAreaSelection();

          SelectedNode = node;
          _window.editor.Canvas.PushToEnd(SelectedNode);
          Selection.activeObject = node.Behaviour;

          handleOnAborterSelected(node);
          handleOnReferenceContainerSelected(node);
        }
      };
    }

    internal void HandleMouseEvents(Event e)
    {
      // Mouse must be inside the editor canvas.
      if (!_window.CanvasInputRect.Contains(e.mousePosition))
      {
        resetState();
        return;
      }

      // NOTE:
      // The order of these matter.
      // For example we want to handle node dragging before
      // area selection.
      handleLinking(e);
      handleCanvasInputs(e);

      if (_window.GetMode() == BonsaiWindow.Mode.Edit)
      {
        handleEditorShortcuts(e);
        handleContextInput(e);
        handleNodeDragging(e);
        handleNodeConnection(e);
        handleAreaSelection(e);
      }
    }

    private void handleEditorShortcuts(Event e)
    {
      // CTRL S shortcut to save the tree quickly.
      if (e.control && e.keyCode == KeyCode.S)
      {

        var state = _window.saveManager.CurrentState();

        if (state == BonsaiSaveManager.SaveState.TempTree)
        {
          _window.saveManager.RequestSaveAs();
        }

        else if (state == BonsaiSaveManager.SaveState.SavedTree)
        {
          _window.saveManager.RequestSave();
        }
      }
    }

    private void handleCanvasInputs(Event e)
    {
      // Zoom
      if (e.type == EventType.ScrollWheel)
      {
        e.Use();
        _window.editor.Zoom(e.delta.y);
      }

      // Pan
      if (e.type == EventType.MouseDrag)
      {
        if (e.button == 2)
        {
          e.Use();
          _window.editor.Pan(e.delta);
        }
      }

      if (e.type == EventType.MouseDown && e.button == 0)
      {

        bool bNodeSelected = _window.editor.Coordinates.OnMouseOverNode(onSingleSelected);

        // Select tree
        if (!bNodeSelected && (Selection.activeObject != _window.tree))
        {

          setTreeAsSelected();
          clearAreaSelection();
        }
      }
    }

    private void handleNodeConnection(Event e)
    {
      // Start a connection action.
      if (e.type == EventType.MouseDown && e.button == 0)
      {

        Action<BonsaiOutputPort> outputCallback = (output) =>
        {
          e.Use();
          IsMakingConnection = true;
          OutputToConnect = output;
        };

        // Check to see if we are making connection starting from output port.
        bool bResult = _window.editor.Coordinates.OnMouseOverOutput(outputCallback);

        // Check if we are making connection starting from input
        if (!bResult)
        {

          Action<BonsaiInputPort> inputCallback = (input) =>
          {
            // Starting a connection from input means that its connected
            // output will change its input.
            if (input.outputConnection != null)
            {

              e.Use();
              IsMakingConnection = true;
              OutputToConnect = input.outputConnection;

              // We disconnect the input since we want to change it to a new input.
              input.outputConnection.RemoveInputConnection(input);
            }
          };

          _window.editor.Coordinates.OnMouseOverInput(inputCallback);
        }
      }

      // Finish making the connection.
      else if (e.type == EventType.MouseUp && e.button == 0 && IsMakingConnection)
      {

        Action<BonsaiNode> callback = (node) =>
        {
          OutputToConnect.Add(node.Input);

          // When a connection is made, we need to make sure the positional
          // ordering reflects the internal tree structure.
          node.NotifyParentOfPostionalReordering();
        };

        _window.editor.Coordinates.OnMouseOverNodeOrInput(callback);

        IsMakingConnection = false;
        OutputToConnect = null;
      }
    }

    /// <summary>
    /// The callback to create the node via typename.
    /// </summary>
    /// <param name="o">The typename as a string.</param>
    private void onNodeCreateCallback(object o)
    {
      var node = _window.editor.Canvas.CreateNode(o as Type, _window.tree);
      _window.editor.SetNewNodeToPositionUnderMouse(node);

      // Make the created node the current focus of selection.
      Selection.activeObject = node.Behaviour;
    }

    private void handleOnAborterSelected(BonsaiNode node)
    {
      // Make sure to keep the order indices updated
      // so we can get feed back about the abort.
      // We only do it when clicking on an aborter for efficiency
      // purposes.
      var aborter = node.Behaviour as ConditionalAbort;
      if (aborter && aborter.abortType != AbortType.None)
      {
        _window.editor.UpdateOrderIndices();
      }
    }

    private void handleOnReferenceContainerSelected(BonsaiNode node)
    {
      Type type = node.Behaviour.GetType();

      bool bIsRefContainer = _window.editor.referenceContainerTypes.Contains(type);

      // Cache referenced nodes for highlighting.
      if (bIsRefContainer)
      {

        var refNodes = node.Behaviour.GetReferencedNodes();
        _window.editor.SetReferencedNodes(refNodes);
      }
    }

    #region Reference Linking

    private void handleLinking(Event e)
    {
      if (!IsRefLinking) return;

      if (e.type == EventType.MouseDown && e.button == 0)
      {

        Action<BonsaiNode> collectRefLinks = (node) =>
        {
          if (node.Behaviour.GetType() == _referenceLinkType)
          {
            onSelectedForLinking(node.Behaviour);
          }
        };

        bool bResult = _window.editor.Coordinates.OnMouseOverNode(collectRefLinks);

        // Abort linking
        if (!bResult)
        {
          EndReferenceLinking();
        }

        e.Use();
      }
    }

    internal void StartReferenceLinking(Type refType, Action<BehaviourNode> onNodeSelected)
    {
      _referenceLinkType = refType;
      onSelectedForLinking = onNodeSelected;
      IsRefLinking = true;
    }

    internal void EndReferenceLinking()
    {
      onSelectedForLinking = null;
      IsRefLinking = false;
    }

    #endregion

    #region Context Inputs

    private void handleContextInput(Event e)
    {
      if (e.type != EventType.ContextClick)
      {
        return;
      }

      int selectionCount = _selectedNodes.Count;

      if (selectionCount == 0)
      {
        handleSingleContext(e);
      }

      else
      {
        handleMultiContext();
        e.Use();
      }
    }

    private void handleSingleContext(Event e)
    {
      // Show node context menu - delete and duplicate.
      Action<BonsaiNode> callback = (node) =>
      {
        onSingleSelected(node);
        _nodeContextMenu.ShowAsContext();
      };

      // Context click over the node.
      bool bClickedNode = _window.editor.Coordinates.OnMouseOverNode(callback);

      if (!bClickedNode)
      {

        // Display node creation menu.
        _nodeTypeSelectionMenu.ShowAsContext();
        e.Use();
      }
    }

    private void handleMultiContext()
    {
      _multiNodeContextMenu.ShowAsContext();
    }

    private void onNodeContextCallback(object o)
    {
      NodeContext context = (NodeContext)o;

      Type nodeType = SelectedNode.Behaviour.GetType();

      switch (context)
      {
        case NodeContext.SetAsRoot:
          _window.tree.Root = SelectedNode.Behaviour;
          break;

        case NodeContext.Duplicate:
          onNodeCreateCallback(nodeType);
          break;

        case NodeContext.ChangeType:
          // TODO
          BonsaiWindow.LogNotImplemented("Change Type");
          break;

        case NodeContext.Delete:
          _window.editor.Canvas.Remove(SelectedNode);
          break;
      }
    }

    private void onMultiNodeCallback(object o)
    {
      NodeContext context = (NodeContext)o;

      switch (context)
      {

        case NodeContext.DuplicateSelection:

          BonsaiCanvas canvas = _window.editor.Canvas;
          BehaviourTree bt = _window.tree;

          for (int i = 0; i < _selectedNodes.Count; ++i)
          {

            // The original nodes will become selected.
            BonsaiNode node = _selectedNodes[i];
            node.bAreaSelectionFlag = false;

            Type t = node.Behaviour.GetType();

            // Duplicate nodes become selected and are spawned
            // at offset from their original.
            BonsaiNode duplicate = canvas.CreateNode(t, bt);
            duplicate.bAreaSelectionFlag = true;
            duplicate.bodyRect.position = node.bodyRect.position + Vector2.one * 40f;

            // Replace in the list with new selections.
            _selectedNodes[i] = duplicate;
          }

          // Notify inspector about the new selections.
          setSelectedInInspector();

          break;

        case NodeContext.DeleteSelection:

          _window.editor.Canvas.RemoveSelected();
          clearAreaSelection();
          setTreeAsSelected();
          break;
      }
    }

    #endregion

    #region Dragging

    private void handleNodeDragging(Event e)
    {
      bool bIsMulti = _selectedNodes.Count > 0;

      // Start node drag.
      if (e.type == EventType.MouseDown && e.button == 0)
      {

        if (bIsMulti) startMultiDrag(e);
        else startSingleDrag(e);
      }

      // End node drag.
      else if (e.type == EventType.MouseUp && e.button == 0 && _bIsDragging)
      {

        if (bIsMulti) endMultiDrag();
        else endSingleDrag();
      }

      // On Node Drag.
      if (_bIsDragging && e.type == EventType.MouseDrag)
      {

        if (bIsMulti) onMultiDrag();
        else onSingleDrag();

        e.Use();
      }
    }

    private void startSingleDrag(Event e)
    {
      Action<BonsaiNode> callback = (node) =>
      {
        e.Use();
        _bIsDragging = true;
        _draggingNode = node;

        // Calculate the relative mouse position from the node for dragging.
        Vector2 mpos = _window.editor.Coordinates.MousePosition();
        _singleDragOffset = mpos - _draggingNode.bodyRect.center;
      };

      _window.editor.Coordinates.OnMouseOverNode(callback);
    }

    private void endSingleDrag()
    {
      // After doing a drag, the children order might have changed, so to reflect
      // what we see in the editor to the internal tree structure, we notify the 
      // node of a positional reordering.
      _draggingNode.NotifyParentOfPostionalReordering();

      _bIsDragging = false;
      _draggingNode = null;
    }

    private void onSingleDrag()
    {
      // Have the node and its subtree follow the mouse.
      Vector2 mpos = _window.editor.Coordinates.MousePosition();
      _window.editor.SetSubtreePosition(mpos, _singleDragOffset, _draggingNode);
    }

    private void startMultiDrag(Event e)
    {
      // Make sure to drag when clicking on a selected node.
      bool bStartDrag = false;

      foreach (BonsaiNode node in _selectedNodes)
      {
        if (_window.editor.Coordinates.IsUnderMouse(node.bodyRect))
        {
          bStartDrag = true;
          break;
        }
      }

      if (!bStartDrag)
      {
        return;
      }

      e.Use();
      _bIsDragging = true;

      _draggingSubroots.Clear();
      _multiDragOffsets.Clear();

      // Find the selected roots to apply dragging.
      foreach (BonsaiNode node in _selectedNodes)
      {

        // Unparented nodes are roots.
        // Isolated nodes are their own roots.
        if (node.Input.outputConnection == null)
        {
          _draggingSubroots.Add(node);
        }

        // Nodes that have a selected parent are not selected roots.
        else if (!node.Input.outputConnection.parentNode.bAreaSelectionFlag)
        {
          _draggingSubroots.Add(node);
        }
      }

      Vector2 mpos = _window.editor.Coordinates.MousePosition();

      foreach (BonsaiNode root in _draggingSubroots)
      {

        // Calculate the relative mouse position from the node for dragging.
        Vector2 offset = mpos - root.bodyRect.center;

        _multiDragOffsets.Add(offset);
      }
    }

    private void endMultiDrag()
    {

      foreach (BonsaiNode root in _draggingSubroots)
      {
        root.NotifyParentOfPostionalReordering();
      }

      _draggingSubroots.Clear();
      _multiDragOffsets.Clear();

      _bIsDragging = false;
    }

    private void onMultiDrag()
    {
      Vector2 mpos = _window.editor.Coordinates.MousePosition();

      int i = 0;
      foreach (BonsaiNode root in _draggingSubroots)
      {

        Vector2 offset = _multiDragOffsets[i++];

        _window.editor.SetSubtreePosition(mpos, offset, root);
      }
    }

    #endregion

    #region Area Selection

    private void handleAreaSelection(Event e)
    {
      // Start area selection.
      if (e.type == EventType.MouseDown && e.button == 0)
      {

        _areaSelectStartPos = Event.current.mousePosition;
        IsAreaSelecting = true;
        e.Use();
      }

      // End area selection
      else if (e.type == EventType.MouseUp && e.button == 0 && IsAreaSelecting)
      {

        collectAreaSelectedNodes();
        IsAreaSelecting = false;
      }

      // Doing area selection.
      if (IsAreaSelecting)
      {
        setSelectedNodesUnderAreaSelection();
      }
    }

    /// <summary>
    /// Handles cleaning up after a area selection terminates.
    /// </summary>
    private void clearAreaSelection()
    {
      _bIsDragging = false;

      foreach (BonsaiNode node in _selectedNodes)
      {
        node.bAreaSelectionFlag = false;
      }

      _draggingSubroots.Clear();
      _multiDragOffsets.Clear();

      _selectedNodes.Clear();
    }

    private void setSelectedInInspector()
    {
      // Store all the behaviours into an array so Selection.objects can use it.
      var selectedBehaviours = new BehaviourNode[_selectedNodes.Count];

      int i = 0;
      foreach (BonsaiNode node in _selectedNodes)
      {
        selectedBehaviours[i++] = node.Behaviour;
      }

      if (selectedBehaviours.Length > 0)
      {
        Selection.objects = selectedBehaviours;
      }
    }

    private void setSelectedNodesUnderAreaSelection()
    {
      Rect selectRect = SelectionCanvasSpace();

      // Mark nodes as selected if they overlap the selection area.
      foreach (BonsaiNode node in _window.editor.Canvas)
      {
        if (node.bodyRect.Overlaps(selectRect))
        {
          node.bAreaSelectionFlag = true;
        }

        else
        {
          node.bAreaSelectionFlag = false;
        }
      }
    }

    private void collectAreaSelectedNodes()
    {
      _selectedNodes.Clear();
      Rect selectionRect = SelectionCanvasSpace();

      // Collect all nodes overlapping the selection rect.
      foreach (BonsaiNode node in _window.editor.Canvas)
      {
        if (node.bodyRect.Overlaps(selectionRect))
        {
          node.bAreaSelectionFlag = true;
          _selectedNodes.Add(node);
        }
      }

      setSelectedInInspector();
    }

    #endregion

    private void setTreeAsSelected()
    {
      EndReferenceLinking();
      _window.editor.ClearReferencedNodes();
      SelectedNode = null;
      Selection.activeObject = _window.tree;
    }

    /// <summary>
    /// Returns the area selection in screen space.
    /// </summary>
    /// <returns></returns>
    internal Rect SelectionScreenSpace()
    {
      // The two corners defining the selection rect.
      Vector2 startPos = _areaSelectStartPos;
      Vector2 mousePos = Event.current.mousePosition;

      // Need to find the proper min and max values to 
      // create a rect without negative width/height values.
      float xmin, xmax;
      float ymin, ymax;

      if (startPos.x < mousePos.x)
      {
        xmin = startPos.x;
        xmax = mousePos.x;
      }

      else
      {
        xmax = startPos.x;
        xmin = mousePos.x;
      }

      if (startPos.y < mousePos.y)
      {
        ymin = startPos.y;
        ymax = mousePos.y;
      }

      else
      {
        ymax = startPos.y;
        ymin = mousePos.y;
      }

      return Rect.MinMaxRect(xmin, ymin, xmax, ymax);
    }

    /// <summary>
    /// Returns the selection rect in canvas space.
    /// </summary>
    /// <returns></returns>
    internal Rect SelectionCanvasSpace()
    {
      Rect screenRect = SelectionScreenSpace();

      Vector2 min = _window.editor.Coordinates.ScreenToCanvasSpace(screenRect.min);
      Vector2 max = _window.editor.Coordinates.ScreenToCanvasSpace(screenRect.max);

      return Rect.MinMaxRect(min.x, min.y, max.x, max.y);
    }
  }
}