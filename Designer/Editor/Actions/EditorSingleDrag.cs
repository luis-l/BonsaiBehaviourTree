
using UnityEngine;

namespace Bonsai.Designer
{
  public static class EditorSingleDrag
  {
    /// <summary>
    /// Returns the offset for the drag operation.
    /// </summary>
    /// <param name="node"></param>
    /// <param name="dragStartPosition"></param>
    public static Vector2 StartDrag(BonsaiNode node, Vector2 dragStartPosition)
    {
      return dragStartPosition - node.Center;
    }

    public static void Drag(BonsaiNode node, Vector2 position, Vector2 offset)
    {
      SetSubtreePosition(node, position, offset);
    }

    public static void FinishDrag(BonsaiNode node)
    {
      node.NotifyParentOfPostionalReordering();
    }

    /// <summary>
    /// Sets the position of the subtree at an offset.
    /// </summary>
    /// <param name="pos">The position of the subtree. </param>
    /// <param name="offset">Additional offset.</param>
    /// <param name="root">The subtree root.</param>
    public static void SetSubtreePosition(BonsaiNode root, Vector2 pos, Vector2 offset)
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
