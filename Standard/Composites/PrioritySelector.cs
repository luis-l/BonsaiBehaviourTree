
using System;

using Bonsai.Core;
using Bonsai.Designer;

namespace Bonsai.Standard
{
  [NodeEditorProperties("Composites/", "Priority")]
  public class PrioritySelector : Selector
  {
    // The indices of the children in priority order.
    private int[] _childrenOrder;

    public override void OnStart()
    {
      _childrenOrder = new int[ChildCount()];

      // Fill in with indices so it can be sorted.
      for (int i = 0; i < _childrenOrder.Length; ++i)
      {
        _childrenOrder[i] = i;
      }
    }

    // Order the child priorities
    public override void OnEnter()
    {
      sortPriorities();
      base.OnEnter();
    }

    // The order of the children is from highest to lowest priority.
    public override BehaviourNode NextChild()
    {
      if (_currentChildIndex >= _childrenOrder.Length)
      {
        return null;
      }

      int index = _childrenOrder[_currentChildIndex];
      return _children[index];
    }

    protected internal override void OnAbort(ConditionalAbort child)
    {
      if (IsChild(child))
      {

        sortPriorities();

        for (int i = 0; i < _childrenOrder.Length; ++i)
        {

          // Match found, start from this priority node.
          if (child._indexOrder == _childrenOrder[i])
          {
            _currentChildIndex = i;
            break;
          }
        }
      }
    }

    private void sortPriorities()
    {
      _currentChildIndex = 0;
      Array.Sort(_childrenOrder, comparePriorities);
    }

    private int comparePriorities(int indexA, int indexB)
    {
      return _children[indexA].Priority() > _children[indexB].Priority() ? -1 : 1;
    }
  }
}