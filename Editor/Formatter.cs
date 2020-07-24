using Bonsai.Core;
using UnityEngine;

namespace Bonsai.Designer
{
  /// <summary>
  /// Provides utilities to format the visual tree in editor.
  /// </summary>
  public static class Formatter
  {
    /// <summary>
    /// Formats the tree to look nicely.
    /// </summary>
    public static void PositionNodesNicely(BonsaiNode root, Vector2 anchor)
    {
      // Sort parent-child connections so formatter uses latest changes.
      foreach (BonsaiNode node in TreeTraversal.PreOrder(root))
      {
        node.SortChildren();
      }

      var positioning = new FormatPositioning();

      foreach (BonsaiNode node in TreeTraversal.PostOrder(root))
      {
        PositionHorizontal(node, positioning);
      }

      foreach (BonsaiNode node in TreeTraversal.PreOrder(root))
      {
        PositionVertical(node);
      }

      // Move the entire subtree to the anchor.
      Vector2 offset = EditorSingleDrag.StartDrag(root, root.Center);
      EditorSingleDrag.SetSubtreePosition(root, anchor, offset);
    }

    private static void PositionHorizontal(BonsaiNode node, FormatPositioning positioning)
    {
      float xCoord;

      int childCount = node.ChildCount();

      // If it is a parent of 2 or more children then center in between the children.
      if (childCount > 1)
      {
        // Get the x-midpoint between the first and last children.
        Vector2 firstChildPos = node.GetChildAt(0).Center;
        Vector2 lastChildPos = node.GetChildAt(childCount - 1).Center;
        float xMid = (firstChildPos.x + lastChildPos.x) / 2f;

        xCoord = xMid;
        positioning.xIntermediate = xMid;
      }

      // A node with 1 child, place directly above child.
      else if (childCount == 1)
      {
        xCoord = positioning.xIntermediate;
      }

      // A leaf node
      else
      {
        float branchWidth = MaxWidthForBranchList(node);
        positioning.xLeaf += 0.5f * (positioning.lastLeafWidth + branchWidth) + FormatPositioning.xLeafSeparation;

        xCoord = positioning.xLeaf;
        positioning.xIntermediate = positioning.xLeaf;
        positioning.lastLeafWidth = branchWidth;
      }

      // Set to 0 on the y-axis for this pass.
      node.Center = new Vector2(xCoord, 0f);
    }

    private static void PositionVertical(BonsaiNode node)
    {
      BonsaiNode parent = node.Parent;
      if (parent != null)
      {
        float ySeperation = parent.ChildCount() == 1
          ? FormatPositioning.yLevelSeparation / 2f
          : FormatPositioning.yLevelSeparation;

        float x = node.Position.x;
        float y = parent.Position.y + parent.Size.y + ySeperation;
        node.Position = new Vector2(x, y);
      }
    }

    // A "branch list" is a tree branch where nodes only have a single child.
    // e.g. Decorator -> Decorator -> Decorator -> Task
    private static float MaxWidthForBranchList(BonsaiNode leaf)
    {
      float maxWidth = leaf.Size.x;
      var parent = leaf.Parent;

      while (parent != null && parent.ChildCount() == 1)
      {
        maxWidth = Mathf.Max(maxWidth, parent.Size.x);
        parent = parent.Parent;
      }

      return maxWidth;
    }

    /// <summary>
    /// A helper class to accumulate positioning data when formatting the tree.
    /// </summary>
    private class FormatPositioning
    {
      public float xLeaf = 0f;
      public float xIntermediate = 0f;
      public float lastLeafWidth = 0f;

      /// <summary>
      /// Horizontal separation between leaf nodes.
      /// </summary>
      public const float xLeafSeparation = 20f;

      /// <summary>
      /// Vertical separation between nodes.
      /// </summary>
      public const float yLevelSeparation = 50f;
    }
  }

}
