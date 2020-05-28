
using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;

namespace Bonsai.Designer
{
  /// <summary>
  /// Holds node data and property data like current pan and zoom.
  /// </summary>
  public class BonsaiCanvas : IEnumerable<BonsaiNode>
  {
    public static float zoomDelta = 0.1f;
    public static float minZoom = 1f;
    public static float maxZoom = 4f;
    public static float panSpeed = 1.2f;

    internal Vector2 zoom = Vector2.one;
    internal Vector2 panOffset = Vector2.zero;

    private List<BonsaiNode> _nodes = new List<BonsaiNode>();
    internal IEnumerable<BonsaiNode> Nodes
    {
      get { return _nodes; }
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
      var node = createEditorNode(behaviour.GetType());
      node.behaviour = behaviour;

      // Setup the style with the updated properties such as name and texture.
      node.SetupStyle();

      return node;
    }

    // Creates an editor node.
    private BonsaiNode createEditorNode(Type behaviourType)
    {
      string texName = null;

      var prop = BonsaiEditor.GetNodeTypeProperties(behaviourType);
      var node = addEditorNode(prop.bCreateInput, prop.bCreateOutput, prop.bCanHaveMultipleChildren);

      texName = prop.texName;
      var tex = BonsaiResources.GetTexture(texName);

      // Failed to find texture, set default.
      if (tex == null)
      {
        tex = BonsaiResources.GetTexture("Play");
      }

      node.iconTex = BonsaiResources.GetTexture(texName);

      return node;
    }

    // Creates and adds an editor node to the canvas.
    private BonsaiNode addEditorNode(bool bCreateInput, bool bCreateOutput, bool bCanHaveMultipleChildren)
    {
      var node = new BonsaiNode(this, bCreateInput, bCreateOutput, bCanHaveMultipleChildren);

      _nodes.Add(node);
      return node;
    }

    internal void PushToEnd(BonsaiNode node)
    {
      bool bRemoved = _nodes.Remove(node);
      if (bRemoved)
      {
        _nodes.Add(node);
      }
    }

    internal void Remove(BonsaiNode node)
    {
      if (_nodes.Remove(node))
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

      _nodes.RemoveAll(match);
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
      get { return _nodes; }
    }

    /// <summary>
    /// Iterates through the nodes in reverse for input purposes
    /// so the top rendering node receives events first.
    /// </summary>
    /// <returns></returns>
    public IEnumerator<BonsaiNode> GetEnumerator()
    {
      for (int i = _nodes.Count - 1; i >= 0; --i)
      {
        yield return _nodes[i];
      }
    }

    IEnumerator IEnumerable.GetEnumerator()
    {
      return GetEnumerator();
    }
  }
}
