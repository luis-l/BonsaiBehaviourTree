
using System;
using System.Collections;
using System.Collections.Generic;
using Bonsai.Core;
using UnityEngine;

namespace Bonsai.Designer
{
  /// <summary>
  /// Holds node data and property data like current pan and zoom.
  /// </summary>
  public class BonsaiCanvas : IEnumerable<BonsaiNode>
  {
    public static float ZoomDelta { get { return BonsaiPreferences.Instance.zoomDelta; } }
    public static float MinZoom { get { return BonsaiPreferences.Instance.minZoom; } }
    public static float MaxZoom { get { return BonsaiPreferences.Instance.maxZoom; } }
    public static float PanSpeed { get { return BonsaiPreferences.Instance.panSpeed; } }

    public Vector2 zoom = Vector2.one;
    public Vector2 panOffset = Vector2.zero;

    private readonly List<BonsaiNode> nodes = new List<BonsaiNode>();

    public IEnumerable<BonsaiNode> Nodes
    {
      get { return nodes; }
    }

    /// <summary>
    /// Builds the canvas given the behaviour tree.
    /// </summary>
    /// <param name="treeBehaviours"></param>
    public BonsaiCanvas(BehaviourTree tree)
    {
      var nodeMap = ReconstructEditorNodes(tree.AllNodes);
      ReconstructEditorConnections(nodeMap);
      zoom = tree.zoomPosition;
      panOffset = tree.panPosition;
    }

    /// <summary>
    /// Create a node and its behaviour from the type.
    /// </summary>
    /// <param name="nodeTypename"></param>
    public BonsaiNode CreateNode(Type behaviourType, BehaviourTree bt)
    {
      var behaviour = BonsaiSaveManager.CreateBehaviourNode(behaviourType, bt);
      return CreateNode(behaviour);
    }

    /// <summary>
    /// Create a node from an existing behaviour.
    /// </summary>
    /// <param name="behaviour"></param>
    /// <returns></returns>
    public BonsaiNode CreateNode(BehaviourNode behaviour)
    {
      var node = CreateEditorNode(behaviour.GetType());
      node.Behaviour = behaviour;
      return node;
    }

    // Creates an editor node.
    private BonsaiNode CreateEditorNode(Type behaviourType)
    {
      var prop = BonsaiEditor.GetNodeTypeProperties(behaviourType);
      var tex = BonsaiPreferences.Texture(prop.texName);
      var node = AddEditorNode(prop.bCreateInput, prop.bCreateOutput, prop.bCanHaveMultipleChildren, tex);
      return node;
    }

    // Creates and adds an editor node to the canvas.
    private BonsaiNode AddEditorNode(bool bCreateInput, bool bCreateOutput, bool bCanHaveMultipleChildren, Texture icon = null)
    {
      var node = new BonsaiNode(bCreateInput, bCreateOutput, bCanHaveMultipleChildren, icon);

      nodes.Add(node);
      return node;
    }

    public void PushToEnd(BonsaiNode node)
    {
      bool bRemoved = nodes.Remove(node);
      if (bRemoved)
      {
        nodes.Add(node);
      }
    }

    public void Remove(BonsaiNode node)
    {
      if (nodes.Remove(node))
      {
        node.Destroy();
      }
    }

    public void RemoveSelected()
    {
      Predicate<BonsaiNode> match = (node) =>
      {
        bool bRemove = node.bAreaSelectionFlag;

        if (bRemove)
        {
          node.Destroy();
        }

        return bRemove;
      };

      nodes.RemoveAll(match);
    }

    public float ZoomScale
    {
      get { return zoom.x; }
    }

    /// <summary>
    /// Iterate through the nodes in the proper draw order
    /// where the last element renders on top of all nodes.
    /// </summary>
    public IEnumerable<BonsaiNode> NodesInDrawOrder
    {
      get { return nodes; }
    }

    /// <summary>
    /// Iterates through the nodes in reverse for input purposes
    /// so the top rendering node receives events first.
    /// </summary>
    /// <returns></returns>
    public IEnumerator<BonsaiNode> GetEnumerator()
    {
      for (int i = nodes.Count - 1; i >= 0; --i)
      {
        yield return nodes[i];
      }
    }

    IEnumerator IEnumerable.GetEnumerator()
    {
      return GetEnumerator();
    }

    // Reconstruct editor nodes from the tree.
    private Dictionary<BehaviourNode, BonsaiNode> ReconstructEditorNodes(IEnumerable<BehaviourNode> treeBehaviours)
    {
      var nodeMap = new Dictionary<BehaviourNode, BonsaiNode>();

      foreach (BehaviourNode behaviour in treeBehaviours)
      {
        BonsaiNode node = CreateNode(behaviour);
        node.Behaviour = behaviour;
        node.bodyRect.position = behaviour.bonsaiNodePosition;
        nodeMap.Add(behaviour, node);
      }

      return nodeMap;
    }

    // Reconstruct the editor connections from the tree.
    private void ReconstructEditorConnections(Dictionary<BehaviourNode, BonsaiNode> nodeMap)
    {
      // Create the connections
      foreach (BonsaiNode node in Nodes)
      {
        for (int i = 0; i < node.Behaviour.ChildCount(); ++i)
        {
          BehaviourNode child = node.Behaviour.GetChildAt(i);
          BonsaiInputPort input = nodeMap[child].Input;
          node.Output.Add(input);
        }
      }
    }

  }
}
