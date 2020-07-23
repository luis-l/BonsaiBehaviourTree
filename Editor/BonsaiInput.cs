
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
      FormatTree,
      Delete,
      DuplicateSelection,
      DeleteSelection
    };

    public event EventHandler<BonsaiInputEvent> MouseDown;
    public event EventHandler<BonsaiInputEvent> Click;
    public event EventHandler<BonsaiInputEvent> DoubleClick;
    public event EventHandler<BonsaiInputEvent> MouseUp;
    public event EventHandler<BonsaiNode> NodeContextClick;
    public event EventHandler CanvasContextClick;
    public event EventHandler<Type> CreateNodeRequest;
    public event EventHandler<NodeContext> NodeActionRequest;
    public event EventHandler<NodeContext> MultiNodeActionRequest;
    public event EventHandler<BonsaiNode> TypeChanged;

    public event EventHandler SaveRequest;
    public event EventHandler CanvasLostFocus;

    // Keeps track of time between mouse down and mouse up to determine if the event was a click.
    private readonly System.Timers.Timer clickTimer = new System.Timers.Timer(120);

    private readonly System.Timers.Timer doubleClickTimer = new System.Timers.Timer(400);

    // Keeps track of the number of quick clicks in succession. The time threshold is determined by clickTimer.
    private int quickClicksCount = 0;

    public IReadOnlySelection selection;

    public bool EditInputEnabled { get; set; }

    public BonsaiInput()
    {
      // Clicks are one-shot events.
      clickTimer.AutoReset = false;
      doubleClickTimer.AutoReset = false;

      // Double click time limit reached.
      doubleClickTimer.Elapsed += (s, e) => quickClicksCount = 0;

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
      IReadOnlyList<BonsaiNode> nodes,
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
      if (e.type == EventType.KeyUp && e.control && e.keyCode == KeyCode.S)
      {
        e.Use();
        SaveRequest?.Invoke(this, EventArgs.Empty);
      }
    }

    private void HandleClickActions(CanvasTransform t, IReadOnlyList<BonsaiNode> nodes, Event e)
    {
      if (IsClickAction(e))
      {
        if (quickClicksCount == 0)
        {
          doubleClickTimer.Start();
        }

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

        // Collect quick, consecutive clicks.
        if (doubleClickTimer.Enabled)
        {
          quickClicksCount++;
        }

        // Double click event occured.
        if (quickClicksCount >= 2)
        {
          DoubleClick?.Invoke(this, inputEvent);
          doubleClickTimer.Stop();
          quickClicksCount = 0;
        }

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

    public void ShowCreateNodeMenu()
    {
      nodeTypeSelectionMenu.ShowAsContext();
    }

    /// <summary>
    /// The callback to create the node via typename.
    /// </summary>
    /// <param name="o">The typename as a string.</param>
    private void OnCreateNodeRequest(object o)
    {
      CreateNodeRequest?.Invoke(this, o as Type);
    }

    private void HandleContextInput(CanvasTransform t, IReadOnlyList<BonsaiNode> nodes)
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

    private void HandleSingleContext(CanvasTransform t, IReadOnlyList<BonsaiNode> nodes)
    {
      BonsaiNode node = NodeUnderMouse(t, nodes);

      if (node != null)
      {
        NodeContextClick?.Invoke(this, node);
        CreateSingleSelectionContextMenu(node).ShowAsContext();
      }

      else
      {
        CanvasContextClick?.Invoke(this, EventArgs.Empty);
        ShowCreateNodeMenu();
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

    private GenericMenu CreateSingleSelectionContextMenu(BonsaiNode node)
    {
      var menu = new GenericMenu();
      menu.AddItem(new GUIContent("Set As Root"), false, OnNodeAction, NodeContext.SetAsRoot);
      menu.AddItem(new GUIContent("Duplicate"), false, OnNodeAction, NodeContext.Duplicate);
      menu.AddItem(new GUIContent("Format Subtree"), false, OnNodeAction, NodeContext.FormatTree);
      PopulateTypeConversions(menu, node);
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

    private void PopulateTypeConversions(GenericMenu menu, BonsaiNode node)
    {
      Type coreType = BonsaiEditor.CoreType(node.Behaviour);
      var behaviourTypes = BonsaiEditor.RegisteredBehaviourNodeTypes;

      foreach (Type subclass in behaviourTypes.Where(t => t.IsSubclassOf(coreType) && !t.IsAbstract))
      {
        menu.AddItem(new GUIContent("Change Type/" + subclass.Name), false, () =>
        {
          EditorChangeNodeType.ChangeType(node, subclass);
          TypeChanged?.Invoke(this, node);
        });
      }
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
    private static BonsaiNode NodeUnderMouse(CanvasTransform transform, IReadOnlyList<BonsaiNode> nodes)
    {
      // Iterate in reverse so the last drawn node (top) receives input first.
      for (int i = nodes.Count - 1; i >= 0; i--)
      {
        BonsaiNode node = nodes[i];
        if (IsUnderMouse(transform, node.RectPositon))
        {
          return node;
        }
      }

      return null;
    }

    private static BonsaiInputEvent CreateInputEvent(CanvasTransform transform, IReadOnlyList<BonsaiNode> nodes)
    {
      bool isInputFocused = false;
      bool isOutputFocused = false;
      BonsaiNode node = NodeUnderMouse(transform, nodes);

      if (node != null)
      {
        isInputFocused = IsInputUnderMouse(transform, node);
        isOutputFocused = IsOutputUnderMouse(transform, node);
      }

      return new BonsaiInputEvent
      {
        transform = transform,
        canvasMousePostion = MousePosition(transform),
        node = node,
        isInputFocused = isInputFocused,
        isOutputFocused = isOutputFocused
      };
    }

    /// <summary>
    /// Get the input for the node if under the mouse.
    /// </summary>
    /// <param name="node"></param>
    /// <returns></returns>
    private static bool IsInputUnderMouse(CanvasTransform t, BonsaiNode node)
    {
      return IsUnderMouse(t, node.InputRect);
    }

    /// <summary>
    /// Get the ouput for the node if under the mouse.
    /// </summary>
    /// <param name="node"></param>
    /// <returns></returns>
    private static bool IsOutputUnderMouse(CanvasTransform t, BonsaiNode node)
    {
      return node.HasOutput && IsUnderMouse(t, node.OutputRect);
    }

    public void Dispose()
    {
      clickTimer.Dispose();
      doubleClickTimer.Dispose();
    }
  }
}