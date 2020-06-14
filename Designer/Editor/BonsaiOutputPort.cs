
using System.Collections.Generic;
using UnityEngine;

using Bonsai.Core;

namespace Bonsai.Designer
{
  public class BonsaiOutputPort : BonsaiPort
  {
    private readonly List<BonsaiInputPort> inputs = new List<BonsaiInputPort>();

    public BonsaiOutputPort(BonsaiNode node) : base(node)
    {

    }

    public IEnumerable<BonsaiInputPort> InputConnections
    {
      get { return inputs; }
    }

    public bool Contains(BonsaiInputPort input)
    {
      return inputs.Contains(input);
    }

    public void Add(BonsaiInputPort input)
    {
      // Avoid connecting it to a root.
      if (input.ParentNode.Behaviour == input.ParentNode.Behaviour.Tree.Root)
      {
        Debug.LogWarning("A root cannot be a child.");
        return;
      }

      // Avoid re-adding.
      if (Contains(input))
      {
        Debug.LogWarning("Already added.");
        return;
      }

      // Avoid cycles.
      if (CycleDetected(input))
      {
        Debug.LogWarning("Cycle detected.");
        return;
      }

      // If it is already parented, then unparent it.
      if (input.outputConnection != null)
      {
        input.outputConnection.RemoveInputConnection(input);
      }

      input.outputConnection = this;

      // Disconnect other inputs since we can only have 1.
      if (!ParentNode.bCanHaveMultipleChildren)
      {
        RemoveAllInputs();
      }

      inputs.Add(input);

      // Notify the parent that there was a new input.
      ParentNode.OnNewInputConnection(input);
    }

    private bool CycleDetected(BonsaiInputPort input)
    {
      var currentNode = this.ParentNode;

      while (currentNode != null)
      {

        // Cycle detected.
        if (input.ParentNode == currentNode)
        {
          return true;
        }

        // There are no more parents to traverse to.
        if (currentNode.Input == null || currentNode.Input.outputConnection == null)
        {
          break;
        }

        // Move up the tree.
        else
        {
          currentNode = currentNode.Input.outputConnection.ParentNode;
        }
      }

      // No cycle detected.
      return false;
    }

    internal void RemoveInputConnection(BonsaiInputPort input)
    {
      if (inputs.Remove(input))
      {
        ParentNode.OnInputConnectionRemoved(input);
        input.outputConnection = null;
      }
    }

    internal void RemoveAllInputs()
    {
      foreach (var i in inputs)
      {
        ParentNode.OnInputConnectionRemoved(i);
        i.outputConnection = null;
      }

      inputs.Clear();
    }

    /// <summary>
    /// Get the input connection at some index.
    /// </summary>
    /// <param name="index"></param>
    /// <returns></returns>
    public BonsaiInputPort GetInput(int index)
    {
      return inputs[index];
    }

    /// <summary>
    /// Get the number of connected inputs.
    /// </summary>
    /// <returns></returns>
    public int InputCount()
    {
      return inputs.Count;
    }

    /// <summary>
    /// Syncs the ordering of the inputs with the internal tree structure.
    /// </summary>
    public void SyncOrdering()
    {
      var composite = ParentNode.Behaviour as Composite;

      if (!composite) return;

      // This will make sure that the input port orders are in sync with the child orders.
      inputs.Sort();

      for (int i = 0; i < inputs.Count; ++i)
      {

        BehaviourNode b = inputs[i].ParentNode.Behaviour;

        // We can do this without destroying the association between parent and children nodes
        // since all we are doing is modifying the ordering of the child nodes in the children array.
        composite.SetChildAtIndex(b, i);
      }

      // Just make sure to sync the index ordering after swapping children.
      composite.UpdateIndexOrders();
    }

    /// <summary>
    /// Returns the y coordinate of the nearest input port on the y axis.
    /// </summary>
    /// <returns></returns>
    public float GetNearestInputY()
    {
      float nearestY = float.MaxValue;
      float nearestDist = float.MaxValue;

      foreach (BonsaiInputPort input in inputs)
      {

        Vector2 toChild = input.bodyRect.position - ParentNode.bodyRect.position;

        float yDist = Mathf.Abs(toChild.y);

        if (yDist < nearestDist)
        {
          nearestDist = yDist;
          nearestY = input.bodyRect.position.y;
        }
      }

      return nearestY;
    }

    /// <summary>
    /// Gets the max and min x coordinates between the children and the parent.
    /// </summary>
    /// <param name="minX"></param>
    /// <param name="maxX"></param>
    public void GetBoundsX(out float minX, out float maxX)
    {
      minX = ParentNode.bodyRect.center.x;
      maxX = ParentNode.bodyRect.center.x;

      foreach (BonsaiInputPort input in inputs)
      {

        float x = input.ParentNode.bodyRect.center.x;

        if (x < minX)
        {
          minX = x;
        }

        else if (x > maxX)
        {
          maxX = x;
        }
      }
    }
  }
}