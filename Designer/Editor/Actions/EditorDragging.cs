
using UnityEngine;
using System.Collections.Generic;

namespace Bonsai.Designer
{
  public class EditorDragging
  {
    // The single node to drag.
    private BonsaiNode draggingNode;

    // The single node drag offset
    private Vector2 draggingOffset;

    // When doing a multi drag, only drag the root nodes in the selection.
    private readonly List<BonsaiNode> draggingSubroots = new List<BonsaiNode>();

    // The relative position between the node and the mouse when it was click for dragging.
    private readonly List<Vector2> multiDragOffsets = new List<Vector2>();

    public bool IsDragging { get; private set; } = false;

    /// <summary>
    /// Begin multi-drag on the selected nodes.
    /// </summary>
    /// <param name="nodes"></param>
    public void BeginDrag(List<BonsaiNode> nodes, Vector2 dragPosition)
    {
      // Nothing to drag.
      if (nodes.Count == 0)
      {
        return;
      }

      if (IsDragging)
      {
        Debug.LogError("End previous drag before starting a new drag action");
        return;
      }

      IsDragging = true;

      if (nodes.Count == 1)
      {
        BeginSingleDrag(nodes[0], dragPosition);
      }
      else if (nodes.Count > 1)
      {
        BeginMultiDrag(nodes, dragPosition);
      }
    }

    public void Drag(Vector2 dragPosition)
    {
      if (IsDragging)
      {
        if (draggingNode != null)
        {
          DragSingle(dragPosition);
        }
        else
        {
          DragMultiple(dragPosition);
        }
      }
    }

    public void EndDrag()
    {
      // After doing a drag, the children order might have changed, so to reflect
      // what we see in the editor to the internal tree structure, we notify the 
      // node of a positional reordering.
      if (draggingNode != null)
      {
        draggingNode.NotifyParentOfPostionalReordering();
      }
      else
      {
        foreach (BonsaiNode root in draggingSubroots)
        {
          root.NotifyParentOfPostionalReordering();
        }
      }

      IsDragging = false;
      draggingNode = null;
      draggingSubroots.Clear();
      multiDragOffsets.Clear();
    }

    private void BeginSingleDrag(BonsaiNode node, Vector2 dragPosition)
    {
      draggingNode = node;
      draggingOffset = dragPosition - draggingNode.Center;
    }

    private void BeginMultiDrag(List<BonsaiNode> nodes, Vector2 dragPosition)
    {
      // Find the selected roots to apply dragging.
      foreach (BonsaiNode node in nodes)
      {
        // Unparented nodes are roots.
        // Isolated nodes are their own roots.
        if (node.Input.outputConnection == null)
        {
          draggingSubroots.Add(node);
        }

        // Nodes that have a selected parent are not selected roots.
        else if (!nodes.Contains(node.Input.outputConnection.ParentNode))
        {
          draggingSubroots.Add(node);

        }
      }

      foreach (BonsaiNode root in draggingSubroots)
      {
        // Calculate the relative mouse position from the node for dragging.
        Vector2 offset = dragPosition - root.Center;
        multiDragOffsets.Add(offset);
      }
    }

    private void DragSingle(Vector2 dragPosition)
    {
      SetSubtreePosition(dragPosition, draggingOffset, draggingNode);
    }

    private void DragMultiple(Vector2 dragPosition)
    {
      int i = 0;
      foreach (BonsaiNode root in draggingSubroots)
      {
        Vector2 offset = multiDragOffsets[i++];
        SetSubtreePosition(dragPosition, offset, root);
      }
    }

    /// <summary>
    /// Sets the position of the subtree at an offset.
    /// </summary>
    /// <param name="pos">The position of the subtree. </param>
    /// <param name="offset">Additional offset.</param>
    /// <param name="root">The subtree root.</param>
    private void SetSubtreePosition(Vector2 pos, Vector2 offset, BonsaiNode root)
    {
      float min = float.MinValue;

      if (root.Input.outputConnection != null)
      {

        float nodeTop = root.Input.RectPosition.yMin;
        float parentBottom = root.Input.outputConnection.RectPosition.yMax;

        // The root cannot be above its parent.
        if (nodeTop < parentBottom)
        {
          min = parentBottom;
        }
      }

      // Record the old position so we can know by how much the root moved
      // so all children can be shifted by the pan delta.
      Vector2 oldPos = root.Center;

      // Clamp the position so it does not go above the parent.
      Vector2 diff = pos - offset;
      diff.y = Mathf.Clamp(diff.y, min, float.MaxValue);

      float snap = BonsaiPreferences.Instance.snapStep;

      Vector2 rounded = Coord.SnapPosition(diff, snap);
      root.Center = rounded;

      // Calculate the change of position of the root.
      Vector2 pan = root.Center - oldPos;

      // Move the entire subtree of the root.
      Core.TreeIterator<BonsaiNode>.Traverse(root, node =>
      {
        // For all children, pan by the same amount that the parent changed by.
        if (node != root)
          node.Center += Coord.SnapPosition(pan, snap);
      });
    }
  }
}
