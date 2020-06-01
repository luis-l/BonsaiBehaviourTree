
using System.Collections.Generic;
using UnityEngine;
using Bonsai.Core;
using NUnit.Framework.Internal;

namespace Tests
{
  public class TestNode : Task
  {
    public float? PriorityValue { get; set; } = null;
    public float? Utility { get; set; } = null;
    public Status ReturnStatus { get; set; }

    public const string kHistoryKey = "TraverseHistory";

    public override void OnStart()
    {
      if (!Blackboard.Exists(kHistoryKey))
      {
        Blackboard.Add(kHistoryKey, new List<int>());
      }
    }

    public override Status Run()
    {
      return ReturnStatus;
    }

    public override float Priority()
    {
      return PriorityValue.GetValueOrDefault(base.Priority());
    }

    public override float UtilityValue()
    {
      return Utility.GetValueOrDefault(base.UtilityValue());
    }

    public override void OnEnter()
    {
      Blackboard.Get<List<int>>(kHistoryKey).Add(PreOrderIndex);
    }

    public TestNode WithPriority(float priority)
    {
      PriorityValue = priority;
      return this;
    }

    public TestNode WithUtility(float utility)
    {
      Utility = utility;
      return this;
    }
  }

  public static class Helper
  {
    public static void StartBehaviourTree(BehaviourTree tree)
    {
      tree.SetBlackboard(ScriptableObject.CreateInstance<Blackboard>());
      tree.SortNodes();
      tree.Start();
    }

    public static BehaviourNode.Status StepBehaviourTree(BehaviourTree tree)
    {
      if (tree.IsRunning())
      {
        tree.Update();
      }

      return tree.LastStatus();
    }

    public static BehaviourNode.Status RunBehaviourTree(BehaviourTree tree)
    {
      StartBehaviourTree(tree);

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

    static public TestNode PassNode(BehaviourTree tree)
    {
      var node = CreateNode<TestNode>(tree);
      node.ReturnStatus = BehaviourNode.Status.Success;
      return node;
    }

    static public TestNode FailNode(BehaviourTree tree)
    {
      var node = CreateNode<TestNode>(tree);
      node.ReturnStatus = BehaviourNode.Status.Failure;
      return node;
    }
  }
}

