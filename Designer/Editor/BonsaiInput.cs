
using System;
using System.Collections.Generic;
using System.Linq;
using Bonsai.Utility;
using UnityEditor;
using UnityEngine;

namespace Bonsai.Designer
{
  /// <summary>
  /// Handles inputs and stores input states.
  /// </summary>
  public class BonsaiInput : IDisposable
  {
    private readonly BonsaiWindow window;
    private readonly GenericMenu nodeTypeSelectionMenu = new GenericMenu();
    private readonly GenericMenu nodeContextMenu = new GenericMenu();
    private readonly GenericMenu multiNodeContextMenu = new GenericMenu();

    public enum NodeContext
    {
      SetAsRoot,
      Duplicate,
      ChangeType,
      Delete,
      DuplicateSelection,
      DeleteSelection
    };

    public event EventHandler<BonsaiNode> NodeClick;
    public event EventHandler<BonsaiOutputPort> OutputClick;
    public event EventHandler<BonsaiInputPort> InputClick;
    public event EventHandler CanvasClicked;
    public event EventHandler<Event> Unclick;
    public event EventHandler<BonsaiNode> NodeUnclick;

    public event EventHandler SaveRequest;
    public event EventHandler CanvasLostFocus;

    public event EventHandler<float> Zoom = delegate { };
    public event EventHandler<Vector2> Pan = delegate { };

    // Keeps track of time between mouse down and mouse up to determine if the event was a click.
    private readonly System.Timers.Timer clickTimer = new System.Timers.Timer(100);

    public BonsaiInput(BonsaiWindow w)
    {
      window = w;

      // Clicks are one-shot events.
      clickTimer.AutoReset = false;

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

    public void HandleMouseEvents(Event e, Rect inputRect)
    {
      // Mouse must be inside the editor canvas.
      if (!inputRect.Contains(e.mousePosition))
      {
        CanvasLostFocus(this, EventArgs.Empty);
        return;
      }

      HandleCanvasInputs(e);
      HandleClickActions(e);

      if (window.EditorMode.Value == BonsaiWindow.Mode.Edit)
      {
        HandleEditorShortcuts(e);
        HandleContextInput(e);
      }
    }

    private void HandleEditorShortcuts(Event e)
    {
      // control+s shortcut to save the tree.
      if (e.control && e.keyCode == KeyCode.S)
      {
        SaveRequest?.Invoke(this, EventArgs.Empty);
      }
    }

    private void HandleCanvasInputs(Event e)
    {
      if (IsZoomAction(e))
      {
        e.Use();
        Zoom(this, e.delta.y);
      }

      if (IsPanAction(e))
      {
        e.Use();
        Pan(this, e.delta);
      }
    }

    private void HandleClickActions(Event e)
    {
      if (IsClickAction(e))
      {
        clickTimer.Start();
        Coord.MouseQueryResult result = window.Editor.Coordinates.QueryUnderMouse(
          out BonsaiNode node,
          out BonsaiInputPort input,
          out BonsaiOutputPort output);

        switch (result)
        {
          case Coord.MouseQueryResult.Node:
            NodeClick?.Invoke(this, node);
            break;
          case Coord.MouseQueryResult.Input:
            InputClick?.Invoke(this, input);
            break;
          case Coord.MouseQueryResult.Output:
            OutputClick?.Invoke(this, output);
            break;
          default:
            CanvasClicked?.Invoke(this, EventArgs.Empty);
            break;
        }
      }
      else if (IsUnlickAction(e))
      {
        // A node click is registered if below a time threshold.
        if (clickTimer.Enabled)
        {
          // Process node unlicks first, then general unclick event.
          BonsaiNode node = window.Editor.Coordinates.NodeUnderMouse();
          if (node != null)
          {
            NodeUnclick?.Invoke(this, node);
          }
        }

        // Reset for next click.
        clickTimer.Stop();

        Unclick?.Invoke(this, e);
      }
    }

    private static bool IsClickAction(Event e)
    {
      return e.type == EventType.MouseDown && e.button == 0;
    }

    private static bool IsUnlickAction(Event e)
    {
      return e.type == EventType.MouseUp && e.button == 0;
    }

    private static bool IsPanAction(Event e)
    {
      return e.type == EventType.MouseDrag && e.button == 2;
    }

    private static bool IsZoomAction(Event e)
    {
      return e.type == EventType.ScrollWheel;
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
        //NodeClicked?.Invoke(this, node);
        window.Editor.NodeSelection.SetSingleSelection(node);
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

      BonsaiNode selected = window.Editor.NodeSelection.SingleSelectedNode;

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
          if (window.Editor.NodeSelection.IsMultiSelection)
          {
            var duplicates = MultiDuplicateNodes(window.Editor.NodeSelection.SelectedNodes);
            window.Editor.NodeSelection.SetMultiSelection(duplicates);
          }
          else if (window.Editor.NodeSelection.IsSingleSelection)
          {
            var duplicate = DuplicateNode(window.Editor.NodeSelection.SingleSelectedNode);
            window.Editor.NodeSelection.SetSingleSelection(duplicate);
          }
          break;
        case NodeContext.DeleteSelection:
          window.Editor.Canvas.Remove(node => window.Editor.NodeSelection.IsNodeSelected(node));
          window.Editor.NodeSelection.SetTreeSelection(window.Tree);
          break;
      }
    }

    private BonsaiNode DuplicateNode(BonsaiNode original)
    {
      return EditorDuplicateNode.DuplicateSingle(window.Editor.Canvas, window.Tree, original);
    }

    private List<BonsaiNode> MultiDuplicateNodes(List<BonsaiNode> originals)
    {
      return EditorDuplicateNode.DuplicateMultiple(window.Editor.Canvas, window.Tree, originals);
    }

    public void Dispose()
    {
      clickTimer.Dispose();
    }
  }
}