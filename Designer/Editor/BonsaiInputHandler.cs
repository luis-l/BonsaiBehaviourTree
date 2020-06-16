
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
    private readonly BonsaiWindow window;
    private readonly GenericMenu nodeTypeSelectionMenu;
    private readonly GenericMenu nodeContextMenu;
    private readonly GenericMenu multiNodeContextMenu;

    private readonly Action<BonsaiNode> onSingleSelected;
    public enum NodeContext { SetAsRoot, Duplicate, ChangeType, Delete, DuplicateSelection, DeleteSelection };

    // The relative position between the node and the mouse when it was click for dragging.
    private Vector2 singleDragOffset = Vector2.zero;
    private readonly List<Vector2> multiDragOffsets = new List<Vector2>();

    // The nodes that will be dragged.
    private readonly List<BonsaiNode> draggingSubroots = new List<BonsaiNode>();

    private bool isDragging = false;
    private Vector2 areaSelectStartPos = Vector2.zero;

    // The node that is currently dragged.
    private BonsaiNode draggingNode = null;

    // The nodes selected from an area/multi selection.
    private readonly List<BonsaiNode> selectedNodes = new List<BonsaiNode>();
    private Type referenceLinkType = typeof(BehaviourNode);
    private Action<BehaviourNode> onSelectedForLinking = null;

    public bool IsRefLinking { get; private set; } = false;
    public bool IsMakingConnection { get; private set; } = false;
    public BonsaiOutputPort OutputToConnect { get; private set; } = null;
    public bool IsAreaSelecting { get; private set; } = false;
    public BonsaiNode SelectedNode { get; private set; } = null;

    private void ResetState()
    {
      IsAreaSelecting = false;
      isDragging = false;
      IsMakingConnection = false;

      draggingNode = null;
      OutputToConnect = null;
    }

    public BonsaiInputHandler(BonsaiWindow w)
    {
      window = w;
      nodeTypeSelectionMenu = new GenericMenu();
      nodeContextMenu = new GenericMenu();
      multiNodeContextMenu = new GenericMenu();

      // Setup Node Selection menu.
      foreach (var kvp in BonsaiEditor.Behaviours)
      {
        Type nodeType = kvp.Key;
        BonsaiEditor.NodeTypeProperties prop = kvp.Value;
        nodeTypeSelectionMenu.AddItem(new GUIContent(prop.path), false, OnNodeCreateCallback, nodeType);
      }

      // Setup node context menu.
      nodeContextMenu.AddItem(new GUIContent("Set As Root"), false, OnNodeContextCallback, NodeContext.SetAsRoot);
      nodeContextMenu.AddItem(new GUIContent("Duplicate"), false, OnNodeContextCallback, NodeContext.Duplicate);
      nodeContextMenu.AddItem(new GUIContent("Change Type"), false, OnNodeContextCallback, NodeContext.ChangeType);
      nodeContextMenu.AddItem(new GUIContent("Delete"), false, OnNodeContextCallback, NodeContext.Delete);

      // Setup area selection context menu.
      multiNodeContextMenu.AddItem(new GUIContent("Duplicate"), false, OnMultiNodeCallback, NodeContext.DuplicateSelection);
      multiNodeContextMenu.AddItem(new GUIContent("Delete"), false, OnMultiNodeCallback, NodeContext.DeleteSelection);

      // Define the actions to take when a single node is selected.
      onSingleSelected = (node) =>
      {
        // Only apply single node selection on a node that is
        // not currently under area selection.
        if (!node.isUnderAreaSelection)
        {
          window.Editor.ClearReferencedNodes();
          ClearAreaSelection();

          SelectedNode = node;
          window.Editor.Canvas.PushToEnd(SelectedNode);
          Selection.activeObject = node.Behaviour;

          HandleOnAborterSelected(node);
          HandleOnReferenceContainerSelected(node);
          window.Repaint();
        }
      };
    }

    public void HandleMouseEvents(Event e)
    {
      // Mouse must be inside the editor canvas.
      if (!window.CanvasInputRect.Contains(e.mousePosition))
      {
        ResetState();
        return;
      }

      // NOTE:
      // The order of these matter.
      // For example we want to handle node dragging before
      // area selection.
      HandleLinking(e);
      HandleCanvasInputs(e);

      if (window.EditorMode == BonsaiWindow.Mode.Edit)
      {
        HandleEditorShortcuts(e);
        HandleContextInput(e);
        HandleNodeDragging(e);
        HandleNodeConnection(e);
        HandleAreaSelection(e);
      }
    }

    private void HandleEditorShortcuts(Event e)
    {
      // CTRL S shortcut to save the tree quickly.
      if (e.control && e.keyCode == KeyCode.S)
      {
        var state = window.SaveManager.CurrentState();

        if (state == BonsaiSaveManager.SaveState.TempTree)
        {
          window.SaveManager.RequestSaveAs();
        }

        else if (state == BonsaiSaveManager.SaveState.SavedTree)
        {
          window.SaveManager.RequestSave();
        }
      }
    }

    private void HandleCanvasInputs(Event e)
    {
      // Zoom
      if (e.type == EventType.ScrollWheel)
      {
        e.Use();
        window.Editor.Zoom(e.delta.y);
      }

      // Pan
      if (e.type == EventType.MouseDrag)
      {
        if (e.button == 2)
        {
          e.Use();
          window.Editor.Pan(e.delta);
        }
      }

      if (e.type == EventType.MouseDown && e.button == 0)
      {
        bool bNodeSelected = window.Editor.Coordinates.OnMouseOverNode(onSingleSelected);

        // Select tree
        if (!bNodeSelected && (Selection.activeObject != window.Tree))
        {
          SetTreeAsSelected();
          ClearAreaSelection();
        }
      }
    }

    private void HandleNodeConnection(Event e)
    {
      // Start a connection action.
      if (e.type == EventType.MouseDown && e.button == 0)
      {
        // Check to see if we are making connection starting from output port.
        bool isMouseOverOuput = window.Editor.Coordinates.OnMouseOverOutput(output =>
        {
          e.Use();
          IsMakingConnection = true;
          OutputToConnect = output;
        });

        // Check if we are making connection starting from input
        if (!isMouseOverOuput)
        {
          window.Editor.Coordinates.OnMouseOverInput(input =>
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
          });
        }

      }

      // Finish making the connection.
      else if (e.type == EventType.MouseUp && e.button == 0 && IsMakingConnection)
      {
        window.Editor.Coordinates.OnMouseOverNodeOrInput(node =>
        {
          OutputToConnect.Add(node.Input);

          // When a connection is made, we need to make sure the positional
          // ordering reflects the internal tree structure.
          node.NotifyParentOfPostionalReordering();
        });

        IsMakingConnection = false;
        OutputToConnect = null;
      }
    }

    /// <summary>
    /// The callback to create the node via typename.
    /// </summary>
    /// <param name="o">The typename as a string.</param>
    private void OnNodeCreateCallback(object o)
    {
      var node = window.Editor.Canvas.CreateNode(o as Type, window.Tree);
      window.Editor.SetNewNodeToPositionUnderMouse(node);

      // Make the created node the current focus of selection.
      Selection.activeObject = node.Behaviour;
    }

    private void HandleOnAborterSelected(BonsaiNode node)
    {
      // Make sure to keep the order indices updated
      // so we can get feed back about the abort.
      // We only do it when clicking on an aborter for efficiency
      // purposes.
      var aborter = node.Behaviour as ConditionalAbort;
      if (aborter && aborter.abortType != AbortType.None)
      {
        window.Editor.UpdateOrderIndices();
      }
    }

    private void HandleOnReferenceContainerSelected(BonsaiNode node)
    {
      Type type = node.Behaviour.GetType();

      bool bIsRefContainer = window.Editor.referenceContainerTypes.Contains(type);

      // Cache referenced nodes for highlighting.
      if (bIsRefContainer)
      {
        var refNodes = node.Behaviour.GetReferencedNodes();
        window.Editor.SetReferencedNodes(refNodes);
      }
    }

    #region Reference Linking

    private void HandleLinking(Event e)
    {
      if (!IsRefLinking) return;

      if (e.type == EventType.MouseDown && e.button == 0)
      {
        bool bResult = window.Editor.Coordinates.OnMouseOverNode(node =>
        {
          if (node.Behaviour.GetType() == referenceLinkType)
          {
            onSelectedForLinking(node.Behaviour);
          }
        });

        // Abort linking
        if (!bResult)
        {
          EndReferenceLinking();
        }

        e.Use();
      }
    }

    public void StartReferenceLinking(Type refType, Action<BehaviourNode> onNodeSelected)
    {
      referenceLinkType = refType;
      onSelectedForLinking = onNodeSelected;
      IsRefLinking = true;
    }

    public void EndReferenceLinking()
    {
      onSelectedForLinking = null;
      IsRefLinking = false;
    }

    #endregion

    #region Context Inputs

    private void HandleContextInput(Event e)
    {
      if (e.type != EventType.ContextClick)
      {
        return;
      }

      int selectionCount = selectedNodes.Count;

      if (selectionCount == 0)
      {
        HandleSingleContext(e);
      }

      else
      {
        HandleMultiContext();
        e.Use();
      }
    }

    private void HandleSingleContext(Event e)
    {
      // Show node context menu - delete and duplicate.
      // Context click over the node.
      bool bClickedNode = window.Editor.Coordinates.OnMouseOverNode(node =>
      {
        onSingleSelected(node);
        nodeContextMenu.ShowAsContext();
      });

      if (!bClickedNode)
      {
        // Display node creation menu.
        nodeTypeSelectionMenu.ShowAsContext();
        e.Use();
      }
    }

    private void HandleMultiContext()
    {
      multiNodeContextMenu.ShowAsContext();
    }

    private void OnNodeContextCallback(object o)
    {
      NodeContext context = (NodeContext)o;

      Type nodeType = SelectedNode.Behaviour.GetType();

      switch (context)
      {
        case NodeContext.SetAsRoot:
          window.Tree.Root = SelectedNode.Behaviour;
          break;

        case NodeContext.Duplicate:
          OnNodeCreateCallback(nodeType);
          break;

        case NodeContext.ChangeType:
          // TODO
          BonsaiWindow.LogNotImplemented("Change Type");
          break;

        case NodeContext.Delete:
          window.Editor.Canvas.Remove(SelectedNode);
          break;
      }
    }

    private void OnMultiNodeCallback(object o)
    {
      NodeContext context = (NodeContext)o;

      switch (context)
      {
        case NodeContext.DuplicateSelection:

          BonsaiCanvas canvas = window.Editor.Canvas;
          BehaviourTree bt = window.Tree;

          for (int i = 0; i < selectedNodes.Count; ++i)
          {
            // The original nodes will become selected.
            BonsaiNode node = selectedNodes[i];
            node.isUnderAreaSelection = false;

            Type t = node.Behaviour.GetType();

            // Duplicate nodes become selected and are spawned
            // at offset from their original.
            BonsaiNode duplicate = canvas.CreateNode(t, bt);
            duplicate.isUnderAreaSelection = true;
            duplicate.Position = node.Position + Vector2.one * 40f;

            // Replace in the list with new selections.
            selectedNodes[i] = duplicate;
          }

          // Notify inspector about the new selections.
          SetSelectedInInspector();
          break;

        case NodeContext.DeleteSelection:
          window.Editor.Canvas.RemoveSelected();
          ClearAreaSelection();
          SetTreeAsSelected();
          break;
      }
    }

    #endregion

    #region Dragging

    private void HandleNodeDragging(Event e)
    {
      bool bIsMulti = selectedNodes.Count > 0;

      // Start node drag.
      if (e.type == EventType.MouseDown && e.button == 0)
      {
        if (bIsMulti) StartMultiDrag(e);
        else StartSingleDrag(e);
      }

      // End node drag.
      else if (e.type == EventType.MouseUp && e.button == 0 && isDragging)
      {
        if (bIsMulti) EndMultiDrag();
        else EndSingleDrag();
      }

      // On Node Drag.
      if (isDragging && e.type == EventType.MouseDrag)
      {
        if (bIsMulti) OnMultiDrag();
        else OnSingleDrag();
        e.Use();
      }
    }

    private void StartSingleDrag(Event e)
    {
      window.Editor.Coordinates.OnMouseOverNode(node =>
      {
        e.Use();
        isDragging = true;
        draggingNode = node;

        // Calculate the relative mouse position from the node for dragging.
        Vector2 mpos = window.Editor.Coordinates.MousePosition();
        singleDragOffset = mpos - draggingNode.Center;
      });
    }

    private void EndSingleDrag()
    {
      // After doing a drag, the children order might have changed, so to reflect
      // what we see in the editor to the internal tree structure, we notify the 
      // node of a positional reordering.
      draggingNode.NotifyParentOfPostionalReordering();
      isDragging = false;
      draggingNode = null;
    }

    private void OnSingleDrag()
    {
      // Have the node and its subtree follow the mouse.
      Vector2 mpos = window.Editor.Coordinates.MousePosition();
      window.Editor.SetSubtreePosition(mpos, singleDragOffset, draggingNode);
    }

    private void StartMultiDrag(Event e)
    {
      // Make sure to drag when clicking on a selected node.
      bool bStartDrag = false;

      foreach (BonsaiNode node in selectedNodes)
      {
        if (window.Editor.Coordinates.IsUnderMouse(node.RectPositon))
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
      isDragging = true;

      draggingSubroots.Clear();
      multiDragOffsets.Clear();

      // Find the selected roots to apply dragging.
      foreach (BonsaiNode node in selectedNodes)
      {
        // Unparented nodes are roots.
        // Isolated nodes are their own roots.
        if (node.Input.outputConnection == null)
        {
          draggingSubroots.Add(node);
        }

        // Nodes that have a selected parent are not selected roots.
        else if (!node.Input.outputConnection.ParentNode.isUnderAreaSelection)
        {
          draggingSubroots.Add(node);
        }
      }

      Vector2 mpos = window.Editor.Coordinates.MousePosition();

      foreach (BonsaiNode root in draggingSubroots)
      {
        // Calculate the relative mouse position from the node for dragging.
        Vector2 offset = mpos - root.Center;

        multiDragOffsets.Add(offset);
      }
    }

    private void EndMultiDrag()
    {
      foreach (BonsaiNode root in draggingSubroots)
      {
        root.NotifyParentOfPostionalReordering();
      }

      draggingSubroots.Clear();
      multiDragOffsets.Clear();
      isDragging = false;
    }

    private void OnMultiDrag()
    {
      Vector2 mpos = window.Editor.Coordinates.MousePosition();

      int i = 0;
      foreach (BonsaiNode root in draggingSubroots)
      {
        Vector2 offset = multiDragOffsets[i++];
        window.Editor.SetSubtreePosition(mpos, offset, root);
      }
    }

    #endregion

    #region Area Selection

    private void HandleAreaSelection(Event e)
    {
      // Start area selection.
      if (e.type == EventType.MouseDown && e.button == 0)
      {
        areaSelectStartPos = Event.current.mousePosition;
        IsAreaSelecting = true;
        e.Use();
      }

      // End area selection
      else if (e.type == EventType.MouseUp && e.button == 0 && IsAreaSelecting)
      {
        CollectAreaSelectedNodes();
        IsAreaSelecting = false;
      }

      // Doing area selection.
      if (IsAreaSelecting)
      {
        SetSelectedNodesUnderAreaSelection();
      }
    }

    /// <summary>
    /// Handles cleaning up after a area selection terminates.
    /// </summary>
    private void ClearAreaSelection()
    {
      isDragging = false;

      foreach (BonsaiNode node in selectedNodes)
      {
        node.isUnderAreaSelection = false;
      }

      draggingSubroots.Clear();
      multiDragOffsets.Clear();
      selectedNodes.Clear();
    }

    private void SetSelectedInInspector()
    {
      // Store all the behaviours into an array so Selection.objects can use it.
      var selectedBehaviours = new BehaviourNode[selectedNodes.Count];

      int i = 0;
      foreach (BonsaiNode node in selectedNodes)
      {
        selectedBehaviours[i++] = node.Behaviour;
      }

      if (selectedBehaviours.Length > 0)
      {
        Selection.objects = selectedBehaviours;
      }
    }

    private void SetSelectedNodesUnderAreaSelection()
    {
      Rect selectRect = SelectionCanvasSpace();

      // Mark nodes as selected if they overlap the selection area.
      foreach (BonsaiNode node in window.Editor.Canvas)
      {
        if (node.RectPositon.Overlaps(selectRect))
        {
          node.isUnderAreaSelection = true;
        }

        else
        {
          node.isUnderAreaSelection = false;
        }
      }
    }

    private void CollectAreaSelectedNodes()
    {
      selectedNodes.Clear();
      Rect selectionRect = SelectionCanvasSpace();

      // Collect all nodes overlapping the selection rect.
      foreach (BonsaiNode node in window.Editor.Canvas)
      {
        if (node.RectPositon.Overlaps(selectionRect))
        {
          node.isUnderAreaSelection = true;
          selectedNodes.Add(node);
        }
      }

      SetSelectedInInspector();
    }

    #endregion

    private void SetTreeAsSelected()
    {
      EndReferenceLinking();
      window.Editor.ClearReferencedNodes();
      SelectedNode = null;
      Selection.activeObject = window.Tree;
    }

    /// <summary>
    /// Returns the area selection in screen space.
    /// </summary>
    /// <returns></returns>
    public Rect SelectionScreenSpace()
    {
      // The two corners defining the selection rect.
      Vector2 startPos = areaSelectStartPos;
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
    public Rect SelectionCanvasSpace()
    {
      Rect screenRect = SelectionScreenSpace();
      Vector2 min = window.Editor.Coordinates.ScreenToCanvasSpace(screenRect.min);
      Vector2 max = window.Editor.Coordinates.ScreenToCanvasSpace(screenRect.max);
      return Rect.MinMaxRect(min.x, min.y, max.x, max.y);
    }
  }
}