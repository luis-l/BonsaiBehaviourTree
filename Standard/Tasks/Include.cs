
using System.Text;
using Bonsai.Core;
using Bonsai.Designer;
using UnityEngine;

namespace Bonsai.Standard
{
  [BonsaiNode("Tasks/", "TreeIcon")]
  public class Include : Task
  {
    [Tooltip("The tree asset to include in this tree.")]
    public BehaviourTree tree;

    public BehaviourTree RunningTree { get; private set; }

    public override void OnStart()
    {
      if (tree)
      {
        RunningTree = BehaviourTree.Clone(tree);
        RunningTree.actor = Actor;
        RunningTree.Start();
      }
    }

    public override Status Run()
    {
      if (RunningTree)
      {
        RunningTree.Update();
        return RunningTree.IsRunning() ? Status.Running : RunningTree.LastStatus();
      }

      // No tree was included. Just fail.
      return Status.Failure;
    }

    public override void Description(StringBuilder builder)
    {
      if (tree)
      {
        builder.AppendFormat("Include {0}", tree.name);
      }
      else
      {
        builder.Append("Tree not set");
      }
    }
  }
}