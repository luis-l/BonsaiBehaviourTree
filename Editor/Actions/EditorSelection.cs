
using System;
using System.Collections.Generic;
using System.Diagnostics.Contracts;
using System.Linq;
using Bonsai.Core;
using UnityEditor;

namespace Bonsai.Designer
{
  public class EditorSelection : IReadOnlySelection
  {
    public event EventHandler<BonsaiNode> SingleSelected;

    private readonly List<BonsaiNode> selectedNodes = new List<BonsaiNode>();
    private readonly List<BehaviourNode> referencedNodes = new List<BehaviourNode>();

    /// <summary>
    /// The currently selected nodes.
    /// </summary>
    public IReadOnlyList<BonsaiNode> SelectedNodes { get { return selectedNodes; } }

    /// <summary>
    /// Referenced nodes of the current selection.
    /// </summary>
    public IReadOnlyList<BehaviourNode> Referenced { get { return referencedNodes; } }

    /// <summary>
    /// The single selected node.
    /// null if there is no selection.
    /// </summary>
    public BonsaiNode SingleSelectedNode
    {
      get { return SelectedNodes.FirstOrDefault(); }
    }

    /// <summary>
    /// Same as SingleSelectedNode but with different semantics.
    /// </summary>
    public BonsaiNode FirstNodeSelected
    {
      get { return SelectedNodes.FirstOrDefault(); }
    }

    public void SetMultiSelection(List<BonsaiNode> newSelection)
    {
      if (newSelection.Count == 1)
      {
        SetSingleSelection(newSelection[0]);
      }
      else
      {
        selectedNodes.Clear();

        if (newSelection.Count > 0)
        {
          selectedNodes.AddRange(newSelection);
          Selection.objects = newSelection.Select(node => node.Behaviour).ToArray();
        }
      }
    }

    public void SetSingleSelection(BonsaiNode newSingleSelected)
    {
      selectedNodes.Clear();
      selectedNodes.Add(newSingleSelected);
      Selection.objects = null;
      Selection.activeObject = newSingleSelected.Behaviour;
      SingleSelected?.Invoke(this, newSingleSelected);
      SelectReferencedNodes(newSingleSelected);
    }

    public void ToggleSelecion(BonsaiNode node)
    {
      if (IsNodeSelected(node))
      {
        selectedNodes.Remove(node);
      }
      else
      {
        selectedNodes.Add(node);
      }

      if (IsSingleSelection)
      {
        BonsaiNode selectedNode = SingleSelectedNode;
        Selection.objects = null;
        Selection.activeObject = selectedNode.Behaviour;
        SingleSelected?.Invoke(this, selectedNode);
        SelectReferencedNodes(selectedNode);
      }

      else if (IsMultiSelection)
      {
        Selection.objects = selectedNodes.Select(n => n.Behaviour).ToArray();
      }
    }

    public void SetTreeSelection(BehaviourTree tree)
    {
      referencedNodes.Clear();
      ClearSelection();
      Selection.activeObject = tree;
    }

    public void ClearSelection()
    {
      if (!IsEmpty)
      {
        selectedNodes.Clear();
        Selection.objects = null;
        Selection.activeObject = null;
      }
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
    public bool IsEmpty
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

    private void SelectReferencedNodes(BonsaiNode node)
    {
      SetReferenced(node.Behaviour);
    }

    public void SetReferenced(BehaviourNode node)
    {
      referencedNodes.Clear();
      BehaviourNode[] refs = node.GetReferencedNodes();
      if (refs != null && refs.Length != 0)
      {
        referencedNodes.AddRange(refs);
      }
    }
  }
}
