
using System;
using System.Collections.Generic;
using Bonsai.Core;
using UnityEngine;
using UnityEditor;

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
    public static void PositionNodesNicely(BehaviourTree bt, IEnumerable<BonsaiNode> canvasNodes)
    {
      // Assumption for Nicify:
      // There must be a root set.
      if (bt.Root == null)
      {
        return;
      }

      // This is for the node editor to use to place the nodes.
      var positions = new Dictionary<BehaviourNode, Vector2>();
      var levels = CalculateLevels(bt);
      var posParams = new PositioningParameters();

      Action<BehaviourNode> positionInPlace = (node) =>
      {
        PositionNode(node, positions, levels, posParams);
      };

      TreeIterator<BehaviourNode>.Traverse(bt.Root, positionInPlace, Traversal.PostOrder);

      foreach (BonsaiNode editorNode in canvasNodes)
      {
        var behaviour = editorNode.Behaviour;

        if (positions.ContainsKey(behaviour))
        {
          Vector2 pos = positions[behaviour];
          editorNode.bodyRect.position = pos;
        }
      }
    }

    private static void PositionNode(
        BehaviourNode node,
        Dictionary<BehaviourNode, Vector2> positions,
        Dictionary<BehaviourNode, int> levels,
        PositioningParameters posParams)
    {
      // Obtained from level order of tree.
      float yLevel = levels[node] * posParams.yLevelOffset;

      int childCount = node.ChildCount();

      // If it is a parent of 2 or more children then center in between the children.
      if (childCount > 1)
      {
        BehaviourNode firstChild = node.GetChildAt(0);
        BehaviourNode lastChild = node.GetChildAt(childCount - 1);

        // Get the x-midpoint between the first and last children.
        Vector2 firstChildPos = positions[firstChild];
        Vector2 lastChildPos = positions[lastChild];

        float xMid = (firstChildPos.x + lastChildPos.x) / 2f;
        posParams.xIntermediate = xMid;

        positions.Add(node, new Vector2(xMid, yLevel));
      }

      // A node with 1 child
      else if (childCount == 1)
      {
        positions.Add(node, new Vector2(posParams.xIntermediate, yLevel));
      }

      // A leaf node
      else
      {
        Vector2 position = new Vector2(posParams.xLeaf, yLevel);

        posParams.xIntermediate = posParams.xLeaf;

        float width = CalculateNameWidth(node);

        // Offset the x leaf position for the next leaf node.
        if (width > BonsaiNode.kDefaultSize.x)
        {
          posParams.xLeaf += width + PositioningParameters.xPadding;
        }

        else
        {
          posParams.xLeaf += posParams.xDeltaLeaf;
        }

        positions.Add(node, position);
      }
    }

    // Calculate how many pixels the type name takes up.
    private static float CalculateNameWidth(BehaviourNode node)
    {
      string typename = node.GetType().Name;
      string niceName = ObjectNames.NicifyVariableName(typename);

      var content = new GUIContent(niceName);
      Vector2 size = new GUIStyle().CalcSize(content);

      return size.x + BonsaiNode.resizePaddingX;
    }

    // To do this, we do a regular DFS and just check the current
    // path length at a given node to determine its level.
    private static Dictionary<BehaviourNode, int> CalculateLevels(BehaviourTree bt)
    {
      if (bt.Root == null)
      {
        return null;
      }

      var levels = new Dictionary<BehaviourNode, int>();

      Action<BehaviourNode, TreeIterator<BehaviourNode>> setLevel = (node, itr) =>
      {
        levels.Add(node, itr.CurrentLevel);
      };

      TreeIterator<BehaviourNode>.Traverse(bt.Root, setLevel, Traversal.LevelOrder);

      return levels;
    }

    /// <summary>
    /// A helper class to hold some positioning data when building the canvas.
    /// </summary>
    private class PositioningParameters
    {
      public float xLeaf = 0f;

      // This is used for single child nodes.
      // That way it is chained in a line.
      public float xIntermediate = 0f;

      public static float xPadding = 30f;
      public static float yPadding = 40f;

      // The displacment between leaves in the x axis.
      public readonly float xDeltaLeaf = BonsaiNode.kDefaultSize.x + xPadding;

      // Displacment between nodes in the y axis.
      public readonly float yLevelOffset = BonsaiNode.kDefaultSize.y + yPadding;
    }
  }

}
