
using UnityEngine;

using Bonsai.Core;
using Bonsai.Designer;

namespace Bonsai.Standard
{
  [NodeEditorProperties("Tasks/", "TreeIcon")]
  public class Include : Task
  {
    [Tooltip("The tree asset to include in this tree.")]
    public BehaviourTree tree;

    public override Status Run()
    {
      // This task never runs. It is symbolic in the editor and is replaced in runtime but the subtree asset.
      throw new UnityException("The Include task should never run.");
    }
  }
}