using System.Linq;
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

    /// <summary>
    /// Sets the position of the subtree at an offset.
    /// </summary>
    /// <param name="dragPosition">The drag position for the subtree. </param>
    /// <param name="offset">Additional offset.</param>
    /// <param name="root">The subtree root.</param>
    public static void SetSubtreePosition(BonsaiNode root, Vector2 dragPosition, Vector2 offset)
    {
      float min = float.MinValue;

      if (!root.IsOrphan())
      {
        float nodeTop = root.RectPositon.yMin;
        float parentBottom = root.Parent.RectPositon.yMax;

        // The root cannot be above its parent.
        if (nodeTop < parentBottom)
        {
          min = parentBottom;
        }
      }

      // Record the old position to later determine the translation delta to move children.
      Vector2 oldPosition = root.Center;

      // Clamp the position so it does not go above the parent.
      Vector2 newPosition = dragPosition - offset;
      newPosition.y = Mathf.Clamp(newPosition.y, min, float.MaxValue);

      float snap = BonsaiPreferences.Instance.snapStep;

      root.Center = Utility.MathExtensions.SnapPosition(newPosition, snap);

      // Calculate the change of position of the root.
      Vector2 pan = root.Center - oldPosition;

      // Move the entire subtree of the root.
      // For all children, pan by the same amount that the parent changed by.
      foreach (BonsaiNode node in Core.TreeTraversal.PreOrder(root).Skip(1))
      {
        node.Center = Utility.MathExtensions.SnapPosition(node.Center + pan, snap);
      }
    }

  }
}
