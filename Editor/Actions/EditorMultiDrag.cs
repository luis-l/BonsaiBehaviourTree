
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace Bonsai.Designer
{
  public static class EditorMultiDrag
  {
    public struct DraggingNode
    {
      public BonsaiNode node;
      public Vector2 offset;
    }

    /// <summary>
    /// Gets all the subroots to do a multi drag. 
    /// Only subroots are dragged since moving a root moves then children.
    /// </summary>
    /// <param name="nodes"></param>
    /// <param name="dragStartPosition"></param>
    public static IReadOnlyList<DraggingNode> StartDrag(IReadOnlyList<BonsaiNode> nodes, Vector2 dragStartPosition)
    {
      var draggingSubroots = new List<DraggingNode>();

      // Find the selected roots to apply dragging.
      foreach (BonsaiNode node in nodes)
      {
        // Unparented nodes are roots.
        // Isolated nodes are their own roots.
        if (node.IsOrphan())
        {
          // Calculate the relative position from the node for dragging.
          var draggingRoot = new DraggingNode
          {
            node = node,
            offset = dragStartPosition - node.Center
          };

          draggingSubroots.Add(draggingRoot);
        }

        // Nodes that have a selected parent are not selected roots.
        else if (!nodes.Contains(node.Parent))
        {
          // Calculate the relative position from the node for dragging.
          var draggingRoot = new DraggingNode
          {
            node = node,
            offset = dragStartPosition - node.Center
          };

          draggingSubroots.Add(draggingRoot);
        }
      }

      return draggingSubroots;
    }

    public static void Drag(Vector2 dragPosition, IReadOnlyList<DraggingNode> nodes)
    {
      foreach (DraggingNode root in nodes)
      {
        EditorSingleDrag.SetSubtreePosition(root.node, dragPosition, root.offset);
      }
    }
  }
}
