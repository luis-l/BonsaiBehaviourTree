
using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Bonsai.Designer
{
  /// <summary>
  /// Holds node data and property data like current pan and zoom.
  /// </summary>
  public class BonsaiCanvas : IEnumerable<BonsaiNode>
  {
    public static float zoomDelta = 0.2f;
    public static float minZoom = 1f;
    public static float maxZoom = 5f;
    public static float panSpeed = 1.2f;

    internal Vector2 zoom = Vector2.one;
    internal Vector2 panOffset = Vector2.zero;

    private readonly List<BonsaiNode> nodes = new List<BonsaiNode>();
    internal IEnumerable<BonsaiNode> Nodes
    {
      get { return nodes; }
    }

    /// <summary>
    /// Create a node and its behaviour from the type.
    /// </summary>
    /// <param name="nodeTypename"></param>
    internal BonsaiNode CreateNode(Type behaviourType, Core.BehaviourTree bt)
    {
      var behaviour = BonsaiSaveManager.CreateBehaviourNode(behaviourType, bt);
      var node = CreateNode(behaviour);

      return node;
    }

    /// <summary>
    /// Create a node from an existing behaviour.
    /// </summary>
    /// <param name="behaviour"></param>
    /// <returns></returns>
    internal BonsaiNode CreateNode(Core.BehaviourNode behaviour)
    {
      var node = CreateEditorNode(behaviour.GetType());
      node.Behaviour = behaviour;
      return node;
    }

    // Creates an editor node.
    private BonsaiNode CreateEditorNode(Type behaviourType)
    {
      var prop = BonsaiEditor.GetNodeTypeProperties(behaviourType);
      var tex = BonsaiResources.GetTexture(prop.texName);
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

    internal void PushToEnd(BonsaiNode node)
    {
      bool bRemoved = nodes.Remove(node);
      if (bRemoved)
      {
        nodes.Add(node);
      }
    }

    internal void Remove(BonsaiNode node)
    {
      if (nodes.Remove(node))
      {
        node.Destroy();
      }
    }

    internal void RemoveSelected()
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
  }
}
