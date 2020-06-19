
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
    public List<BonsaiNode> SelectedNodes { get; } = new List<BonsaiNode>();

    /// <summary>
    /// Referenced nodes of the current selection.
    /// </summary>
    public List<BehaviourNode> Referenced { get; } = new List<BehaviourNode>();

    /// <summary>
    /// The single selected node.
    /// null if there is no selection.
    /// </summary>
    public BonsaiNode SingleSelectedNode
    {
      get
      {
        return SelectedNodes.FirstOrDefault();
      }
    }

    public void SetMultiSelection(List<BonsaiNode> newSelection)
    {
      if (newSelection.Count == 1)
      {
        SetSingleSelection(newSelection[0]);
      }
      else
      {
        SelectedNodes.Clear();

        if (newSelection.Count > 0)
        {
          SelectedNodes.AddRange(newSelection);
          Selection.objects = newSelection.Select(node => node.Behaviour).ToArray();
        }
      }
    }

    public void SetSingleSelection(BonsaiNode newSingleSelected)
    {
      SelectedNodes.Clear();
      SelectedNodes.Add(newSingleSelected);
      Selection.activeObject = newSingleSelected.Behaviour;
      SingleSelected?.Invoke(this, newSingleSelected);
      NotifyIfAbortSelected(newSingleSelected);
      SelectReferencedNodes(newSingleSelected);
    }

    public void SetTreeSelection(BehaviourTree tree)
    {
      Referenced.Clear();
      ClearSelection();
      Selection.activeObject = tree;
    }

    public void ClearSelection()
    {
      SelectedNodes.Clear();
    }

    [Pure]
    public bool IsNodeSelected(BonsaiNode node)
    {
      return SelectedNodes.Contains(node);
    }

    [Pure]
    public int SelectedCount
    {
      get { return SelectedNodes.Count; }
    }

    [Pure]
    public bool IsNoneSelected
    {
      get { return SelectedNodes.Count == 0; }
    }

    [Pure]
    public bool IsSingleSelection
    {
      get { return SelectedNodes.Count == 1; }
    }

    [Pure]
    public bool IsMultiSelection
    {
      get { return SelectedNodes.Count > 1; }
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
        Referenced.AddRange(refs);
      }
    }
  }
}
