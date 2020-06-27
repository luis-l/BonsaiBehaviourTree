
using System.Collections.Generic;

namespace Bonsai.Designer
{
  /// <summary>
  /// View of the selection.
  /// </summary>
  public interface IReadOnlySelection
  {
    IReadOnlyList<BonsaiNode> SelectedNodes { get; }
    BonsaiNode SingleSelectedNode { get; }
    IReadOnlyList<Core.BehaviourNode> Referenced { get; }

    bool IsNodeSelected(BonsaiNode node);
    bool IsReferenced(BonsaiNode node);

    int SelectedCount { get; }
    bool IsEmpty { get; }
    bool IsSingleSelection { get; }
    bool IsMultiSelection { get; }
  }
}
