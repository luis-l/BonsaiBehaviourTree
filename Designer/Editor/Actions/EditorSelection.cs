
using System;
using System.Collections.Generic;
using System.Diagnostics.Contracts;
using System.Linq;
using Bonsai.Core;
using UnityEditor;

namespace Bonsai.Designer
{
  public class EditorSelection
  {
    public event EventHandler<BonsaiNode> SingleSelected;
    public event EventHandler<ConditionalAbort> AbortSelected;

    /// <summary>
    /// The currently selected nodes.
    /// </summary>
    public List<BonsaiNode> Selected { get; } = new List<BonsaiNode>();

    /// <summary>
    /// Referenced nodes of the current selection.
    /// </summary>
    public HashSet<BehaviourNode> Referenced { get; } = new HashSet<BehaviourNode>();

    /// <summary>
    /// The single selected node.
    /// null if there is no selection.
    /// </summary>
    public BonsaiNode SelectedNode
    {
      get
      {
        return Selected.FirstOrDefault();
      }
    }

    public void SetCurrentSelection(List<BonsaiNode> newSelection)
    {
      if (newSelection.Count == 1)
      {
        SelectSingleNode(newSelection[0]);
      }
      else
      {
        Selected.Clear();

        if (newSelection.Count > 0)
        {
          Selected.AddRange(newSelection);
          Selection.objects = newSelection.Select(node => node.Behaviour).ToArray();
        }
      }
    }

    public void SelectSingleNode(BonsaiNode newSingleSelected)
    {
      Selected.Clear();
      Selected.Add(newSingleSelected);
      Selection.activeObject = newSingleSelected.Behaviour;
      SingleSelected?.Invoke(this, newSingleSelected);
      NotifyIfAbortSelected(newSingleSelected);
      SelectReferencedNodes(newSingleSelected);
    }

    public void SelectTree(BehaviourTree tree)
    {
      Referenced.Clear();
      ClearSelection();
      Selection.activeObject = tree;
    }

    public void ClearSelection()
    {
      Selected.Clear();
    }

    [Pure]
    public bool IsNodeSelected(BonsaiNode node)
    {
      return Selected.Contains(node);
    }

    [Pure]
    public int SelectedCount
    {
      get { return Selected.Count; }
    }

    [Pure]
    public bool IsNoneSelected
    {
      get { return Selected.Count == 0; }
    }

    [Pure]
    public bool IsSingleSelection
    {
      get { return Selected.Count == 1; }
    }

    [Pure]
    public bool IsMultiSelection
    {
      get { return Selected.Count > 1; }
    }

    [Pure]
    public bool IsReferenced(BonsaiNode node)
    {
      return Referenced.Contains(node.Behaviour);
    }

    private void NotifyIfAbortSelected(BonsaiNode node)
    {
      var aborter = node.Behaviour as ConditionalAbort;
      if (aborter && aborter.abortType != AbortType.None)
      {
        AbortSelected?.Invoke(this, aborter);
      }
    }

    private void SelectReferencedNodes(BonsaiNode node)
    {
      SetReferenced(node.Behaviour);
    }

    public void SetReferenced(BehaviourNode node)
    {
      Referenced.Clear();
      BehaviourNode[] refs = node.GetReferencedNodes();
      if (refs != null && refs.Length != 0)
      {
        Referenced.UnionWith(refs);
      }
    }
  }
}
