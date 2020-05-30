
using UnityEngine;
using Bonsai.Core;

namespace Tests
{
  public class Success : Task
  {
    public override Status Run()
    {
      return Status.Success;
    }
  }

  public class Fail : Task
  {
    public override Status Run()
    {
      return Status.Failure;
    }
  }

  public static class Helper
  {
    public static BehaviourNode.Status RunBehaviourTree(BehaviourTree tree)
    {
      tree.SortNodes();
      tree.Start();

      while (tree.IsRunning())
      {
        tree.Update();
      }

      return tree.LastStatus();
    }

    static public BehaviourTree CreateTree()
    {
      return ScriptableObject.CreateInstance<BehaviourTree>();
    }

    static public T CreateNode<T>(BehaviourTree tree) where T : BehaviourNode
    {
      var node = ScriptableObject.CreateInstance<T>();
      node.Tree = tree;
      return node;
    }
  }

}

