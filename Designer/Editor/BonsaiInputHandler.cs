
using System;
using System.Linq;
using Bonsai.Core;
using UnityEditor;
using UnityEngine;

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

    public enum NodeContext { SetAsRoot, Duplicate, ChangeType, Delete, DuplicateSelection, DeleteSelection };

    public event EventHandler<BonsaiNode> NodeClicked = delegate { };
    public event EventHandler<BehaviourTree> TreeClicked = delegate { };

    public event EventHandler<float> Zoomed = delegate { };
    public event EventHandler<Vector2> Panned = delegate { };

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
    }

    public void HandleMouseEvents(Event e)
    {
      // Mouse must be inside the editor canvas.
      if (!window.CanvasInputRect.Contains(e.mousePosition))
      {
        window.Editor.NodeDragging.EndDrag();
        window.Editor.NodeAreaSelect.EndAreaSelection();
        window.Editor.MakingConnection.EndConnection();
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
      if (IsZoomAction(e))
      {
        e.Use();
        Zoomed(this, e.delta.y);
        window.Editor.Zoom(e.delta.y);
      }

      if (IsPanAction(e))
      {
        e.Use();
        Panned(this, e.delta);
        window.Editor.Pan(e.delta);
      }

      if (IsSelectObjectAction(e))
      {
        BonsaiNode node = window.Editor.Coordinates.NodeUnderMouse();

        if (node != null)
        {
          if (!window.Editor.NodeSelection.IsMultiSelection)
          {
            NodeClicked(this, node);
            window.Editor.NodeSelection.SelectSingleNode(node);
          }
        }

        else if (Selection.activeObject != window.Tree)
        {
          TreeClicked(this, window.Tree);
          window.Editor.NodeSelection.SelectTree(window.Tree);
        }
      }
    }

    private static bool IsSelectObjectAction(Event e)
    {
      return e.type == EventType.MouseDown && e.button == 0;
    }

    private static bool IsPanAction(Event e)
    {
      return e.type == EventType.MouseDrag && e.button == 2;
    }

    private static bool IsZoomAction(Event e)
    {
      return e.type == EventType.ScrollWheel;
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
          window.Editor.MakingConnection.BeginConnection(output);
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
              window.Editor.MakingConnection.BeginConnection(input);
            }
          });
        }

      }

      // Finish making the connection.
      else if (e.type == EventType.MouseUp && e.button == 0 && window.Editor.MakingConnection.IsMakingConnection)
      {
        window.Editor.Coordinates.OnMouseOverNodeOrInput(node =>
        {
          window.Editor.MakingConnection.OutputToConnect.Add(node.Input);

          // When a connection is made, we need to make sure the positional
          // ordering reflects the internal tree structure.
          node.NotifyParentOfPostionalReordering();
        });

        window.Editor.MakingConnection.EndConnection();
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

    private void HandleLinking(Event e)
    {
      if (window.Editor.NodeLinking.IsLinking)
      {
        if (e.type == EventType.MouseDown && e.button == 0)
        {
          bool isOverNode = window.Editor.Coordinates.OnMouseOverNode(node =>
          {
            window.Editor.NodeLinking.TryLink(node);
          });

          // Abort linking
          if (!isOverNode)
          {
            window.Editor.NodeLinking.EndLinking();
          }
          e.Use();
        }
      }
    }

    private void HandleContextInput(Event e)
    {
      if (e.type != EventType.ContextClick)
      {
        return;
      }

      if (!window.Editor.NodeSelection.IsMultiSelection)
      {
        HandleSingleContext(e);
      }

      if (window.Editor.NodeSelection.IsMultiSelection)
      {
        HandleMultiContext();
        e.Use();
      }
    }

    private void HandleSingleContext(Event e)
    {
      // Show node context menu - delete and duplicate.
      // Context click over the node.
      bool isOverNode = window.Editor.Coordinates.OnMouseOverNode(node =>
      {
        NodeClicked(this, node);
        window.Editor.NodeSelection.SelectSingleNode(node);
        nodeContextMenu.ShowAsContext();
      });

      if (!isOverNode)
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

      BonsaiNode selected = window.Editor.NodeSelection.SelectedNode;

      switch (context)
      {
        case NodeContext.SetAsRoot:
          window.Tree.Root = selected.Behaviour;
          break;

        case NodeContext.Duplicate:
          Type nodeType = selected.Behaviour.GetType();
          OnNodeCreateCallback(nodeType);
          break;

        case NodeContext.ChangeType:
          // TODO
          BonsaiWindow.LogNotImplemented("Change Type");
          break;

        case NodeContext.Delete:
          window.Editor.Canvas.Remove(selected);
          break;
      }
    }

    private void OnMultiNodeCallback(object o)
    {
      NodeContext context = (NodeContext)o;

      switch (context)
      {
        case NodeContext.DuplicateSelection:
          var duplicates = window.Editor.NodeSelection.Selected.Select(node => DuplicateNode(node));
          window.Editor.NodeSelection.SetCurrentSelection(duplicates.ToList());
          break;

        case NodeContext.DeleteSelection:
          window.Editor.Canvas.Remove(node => window.Editor.NodeSelection.IsNodeSelected(node));
          window.Editor.NodeSelection.SelectTree(window.Tree);
          break;
      }
    }

    private BonsaiNode DuplicateNode(BonsaiNode original)
    {
      BonsaiNode duplicate = window.Editor.Canvas.CreateNode(original.Behaviour.GetType(), window.Tree);

      // Duplicate nodes are placed offset from the original.
      duplicate.Position = original.Position + Vector2.one * 40f;

      return duplicate;
    }

    private void HandleNodeDragging(Event e)
    {
      // Start node drag.
      if (e.type == EventType.MouseDown && e.button == 0)
      {
        if (window.Editor.Coordinates.IsMouseOverNode())
        {
          var selection = window.Editor.NodeSelection.Selected;
          window.Editor.NodeDragging.BeginDrag(selection, window.Editor.Coordinates.MousePosition());
          e.Use();
        }
      }

      // End node drag.
      else if (e.type == EventType.MouseUp && e.button == 0 && window.Editor.NodeDragging.IsDragging)
      {
        window.Editor.NodeDragging.EndDrag();
      }

      // On Node Drag.
      if (window.Editor.NodeDragging.IsDragging && e.type == EventType.MouseDrag)
      {
        window.Editor.NodeDragging.Drag(window.Editor.Coordinates.MousePosition());
        e.Use();
      }
    }

    private void HandleAreaSelection(Event e)
    {
      // Start area selection.
      if (e.type == EventType.MouseDown && e.button == 0)
      {
        window.Editor.NodeAreaSelect.BeginAreaSelection(Event.current.mousePosition);
        e.Use();
      }

      // End area selection
      else if (e.type == EventType.MouseUp && e.button == 0 && window.Editor.NodeAreaSelect.IsSelecting)
      {
        var selected = window.Editor.NodeAreaSelect.NodesUnderSelection(
          window.Editor.Coordinates,
          Event.current.mousePosition,
          window.Editor.Canvas);

        window.Editor.NodeSelection.SetCurrentSelection(selected.ToList());
        window.Editor.NodeAreaSelect.EndAreaSelection();
      }

      // Doing area selection.
      if (e.type == EventType.MouseMove && window.Editor.NodeAreaSelect.IsSelecting)
      {
        //var selected = window.Editor.NodeAreaSelect.NodesUnderSelection(
        //  window.Editor.Coordinates,
        //  Event.current.mousePosition,
        //  window.Editor.Canvas);
        //window.Editor.NodeSelection.SetCurrentSelection(selected.ToList());
      }
    }

  }
}