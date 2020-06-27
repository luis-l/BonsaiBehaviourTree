
using System;
using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEngine;

namespace Bonsai.Designer
{
  /// <summary>
  /// Emits inputs events for the editor.
  /// </summary>
  public class BonsaiInput : IDisposable
  {
    private readonly GenericMenu nodeTypeSelectionMenu = new GenericMenu();

    public enum NodeContext
    {
      SetAsRoot,
      Duplicate,
      ChangeType,
      Delete,
      DuplicateSelection,
      DeleteSelection
    };

    public event EventHandler<BonsaiInputEvent> MouseDown;
    public event EventHandler<BonsaiInputEvent> Click;
    public event EventHandler<BonsaiInputEvent> MouseUp;
    public event EventHandler<BonsaiNode> NodeContextClick;
    public event EventHandler CanvasContextClick;
    public event EventHandler<Type> CreateNodeRequest;
    public event EventHandler<NodeContext> NodeActionRequest;
    public event EventHandler<NodeContext> MultiNodeActionRequest;

    public event EventHandler SaveRequest;
    public event EventHandler CanvasLostFocus;

    // Keeps track of time between mouse down and mouse up to determine if the event was a click.
    private readonly System.Timers.Timer clickTimer = new System.Timers.Timer(100);

    public IReadOnlySelection selection;

    public bool EditInputEnabled { get; set; }

    public BonsaiInput()
    {
      // Clicks are one-shot events.
      clickTimer.AutoReset = false;

      // Setup Node Selection menu.
      foreach (var kvp in BonsaiEditor.Behaviours)
      {
        Type nodeType = kvp.Key;
        BonsaiEditor.NodeTypeProperties prop = kvp.Value;
        nodeTypeSelectionMenu.AddItem(new GUIContent(prop.path), false, OnCreateNodeRequest, nodeType);
      }

      // Setup node context menu.
    }

    public void HandleMouseEvents(
      Event e,
      CanvasTransform transform,
      IEnumerable<BonsaiNode> nodes,
      Rect inputRect)
    {
      // Mouse must be inside the editor canvas.
      if (!inputRect.Contains(e.mousePosition))
      {
        CanvasLostFocus(this, EventArgs.Empty);
        return;
      }

      HandleClickActions(transform, nodes, e);

      if (EditInputEnabled)
      {
        HandleEditorShortcuts(e);

        if (e.type == EventType.ContextClick)
        {
          HandleContextInput(transform, nodes);
          e.Use();
        }
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

    private void HandleClickActions(CanvasTransform t, IEnumerable<BonsaiNode> nodes, Event e)
    {
      if (IsClickAction(e))
      {
        clickTimer.Start();
        MouseDown?.Invoke(this, CreateInputEvent(t, nodes));
      }

      else if (IsUnlickAction(e))
      {
        BonsaiInputEvent inputEvent = CreateInputEvent(t, nodes);

        // A node click is registered if below a time threshold.
        if (clickTimer.Enabled)
        {
          Click?.Invoke(this, inputEvent);
        }

        // Reset for next click.
        clickTimer.Stop();
        MouseUp?.Invoke(this, inputEvent);
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

    public static bool IsPanAction(Event e)
    {
      return e.type == EventType.MouseDrag && e.button == 2;
    }

    public static bool IsZoomAction(Event e)
    {
      return e.type == EventType.ScrollWheel;
    }

    /// <summary>
    /// The callback to create the node via typename.
    /// </summary>
    /// <param name="o">The typename as a string.</param>
    private void OnCreateNodeRequest(object o)
    {
      CreateNodeRequest?.Invoke(this, o as Type);
    }

    private void HandleContextInput(CanvasTransform t, IEnumerable<BonsaiNode> nodes)
    {
      if (selection.IsMultiSelection)
      {
        HandleMultiContext();
      }
      else
      {
        HandleSingleContext(t, nodes);
      }
    }

    private void HandleSingleContext(CanvasTransform t, IEnumerable<BonsaiNode> nodes)
    {
      BonsaiNode node = NodeUnderMouse(t, nodes);

      if (node != null)
      {
        NodeContextClick?.Invoke(this, node);
        CreateSingleSelectionContextMenu().ShowAsContext();
      }

      else
      {
        CanvasContextClick?.Invoke(this, EventArgs.Empty);
        nodeTypeSelectionMenu.ShowAsContext();
      }
    }

    private void HandleMultiContext()
    {
      CreateMultiSelectionContextMenu().ShowAsContext();
    }

    private void OnNodeAction(object o)
    {
      NodeActionRequest?.Invoke(this, (NodeContext)o);
    }

    private void OnMultiNodeAction(object o)
    {
      MultiNodeActionRequest?.Invoke(this, (NodeContext)o);
    }

    private GenericMenu CreateSingleSelectionContextMenu()
    {
      var menu = new GenericMenu();
      menu.AddItem(new GUIContent("Set As Root"), false, OnNodeAction, NodeContext.SetAsRoot);
      menu.AddItem(new GUIContent("Duplicate"), false, OnNodeAction, NodeContext.Duplicate);
      menu.AddItem(new GUIContent("Change Type"), false, OnNodeAction, NodeContext.ChangeType);
      menu.AddSeparator("");
      menu.AddItem(new GUIContent("Delete"), false, OnNodeAction, NodeContext.Delete);
      return menu;
    }

    private GenericMenu CreateMultiSelectionContextMenu()
    {
      // Setup area selection context menu.
      var menu = new GenericMenu();
      menu.AddItem(new GUIContent("Duplicate"), false, OnMultiNodeAction, NodeContext.DuplicateSelection);
      menu.AddItem(new GUIContent("Delete"), false, OnMultiNodeAction, NodeContext.DeleteSelection);
      return menu;
    }

    /// <summary>
    /// Returns the mouse position in canvas space.
    /// </summary>
    /// <returns></returns>
    public static Vector2 MousePosition(CanvasTransform transform)
    {
      return transform.ScreenToCanvasSpace(Event.current.mousePosition);
    }

    /// <summary>
    /// Tests if the rect is under the mouse.
    /// </summary>
    /// <param name="r"></param>
    /// <returns></returns>
    public static bool IsUnderMouse(CanvasTransform transform, Rect r)
    {
      return r.Contains(MousePosition(transform));
    }

    /// <summary>
    /// Get the first node detected under the mouse. Ports are counted as port of the check.
    /// </summary>
    /// <returns></returns>
    private static BonsaiNode NodeUnderMouse(CanvasTransform transform, IEnumerable<BonsaiNode> nodes)
    {
      return nodes.FirstOrDefault(node => IsUnderMouse(transform, node.RectPositon));
    }

    private static BonsaiInputEvent CreateInputEvent(
      CanvasTransform transform,
      IEnumerable<BonsaiNode> nodes)
    {
      BonsaiInputPort input = null;
      BonsaiOutputPort output = null;
      BonsaiNode node = NodeUnderMouse(transform, nodes);

      if (node != null)
      {
        input = InputUnderMouse(transform, node);
        output = OutputUnderMouse(transform, node);
      }

      return new BonsaiInputEvent
      {
        transform = transform,
        canvasMousePostion = MousePosition(transform),
        node = node,
        inputPort = input,
        outputPort = output
      };
    }

    /// <summary>
    /// Get the input for the node if under the mouse.
    /// </summary>
    /// <param name="node"></param>
    /// <returns></returns>
    private static BonsaiInputPort InputUnderMouse(CanvasTransform t, BonsaiNode node)
    {
      if (node.Input != null && IsUnderMouse(t, node.Input.RectPosition))
      {
        return node.Input;
      }

      return null;
    }

    /// <summary>
    /// Get the ouput for the node if under the mouse.
    /// </summary>
    /// <param name="node"></param>
    /// <returns></returns>
    private static BonsaiOutputPort OutputUnderMouse(CanvasTransform t, BonsaiNode node)
    {
      if (node.Output != null && IsUnderMouse(t, node.Output.RectPosition))
      {
        return node.Output;
      }
      return null;
    }

    public void Dispose()
    {
      clickTimer.Dispose();
    }
  }
}