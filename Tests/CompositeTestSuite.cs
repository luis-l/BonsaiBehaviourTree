using System.Collections;
using System.Collections.Generic;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;

using Bonsai.Core;
using Bonsai.Standard;

namespace Tests
{
  public class CompositeTestSuite
  {
    [Test]
    public void SequencePass()
    {
      BehaviourTree tree = Helper.CreateTree();

      var root = Helper.CreateNode<Sequence>(tree);
      root.AddChild(Helper.CreateNode<Success>(tree));
      root.AddChild(Helper.CreateNode<Success>(tree));
      root.AddChild(Helper.CreateNode<Success>(tree));
      tree.Root = root;

      var result = Helper.RunBehaviourTree(tree);
      Assert.AreEqual(BehaviourNode.Status.Success, result);
    }

    [Test]
    public void SequenceFail()
    {
      BehaviourTree tree = Helper.CreateTree();

      var root = Helper.CreateNode<Sequence>(tree);
      root.AddChild(Helper.CreateNode<Success>(tree));
      root.AddChild(Helper.CreateNode<Fail>(tree));
      root.AddChild(Helper.CreateNode<Success>(tree));
      tree.Root = root;

      var result = Helper.RunBehaviourTree(tree);
      Assert.AreEqual(BehaviourNode.Status.Failure, result);
    }

    [Test]
    public void SelectorPass()
    {
      BehaviourTree tree = Helper.CreateTree();

      var root = Helper.CreateNode<Selector>(tree);
      root.AddChild(Helper.CreateNode<Fail>(tree));
      root.AddChild(Helper.CreateNode<Fail>(tree));
      root.AddChild(Helper.CreateNode<Success>(tree));
      tree.Root = root;

      var result = Helper.RunBehaviourTree(tree);
      Assert.AreEqual(BehaviourNode.Status.Success, result);
    }

    [Test]
    public void SelectorFail()
    {
      BehaviourTree tree = Helper.CreateTree();

      var root = Helper.CreateNode<Selector>(tree);
      root.AddChild(Helper.CreateNode<Fail>(tree));
      root.AddChild(Helper.CreateNode<Fail>(tree));
      root.AddChild(Helper.CreateNode<Fail>(tree));
      tree.Root = root;

      var result = Helper.RunBehaviourTree(tree);
      Assert.AreEqual(BehaviourNode.Status.Failure, result);
    }

    [Test]
    public void ParallelPass()
    {
      BehaviourTree tree = Helper.CreateTree();

      var root = Helper.CreateNode<Parallel>(tree);
      root.AddChild(Helper.CreateNode<Success>(tree));
      root.AddChild(Helper.CreateNode<Success>(tree));
      root.AddChild(Helper.CreateNode<Success>(tree));
      tree.Root = root;

      var result = Helper.RunBehaviourTree(tree);
      Assert.AreEqual(BehaviourNode.Status.Success, result);
    }

    [Test]
    public void ParallelFail()
    {
      BehaviourTree tree = Helper.CreateTree();

      var root = Helper.CreateNode<Parallel>(tree);
      root.AddChild(Helper.CreateNode<Success>(tree));
      root.AddChild(Helper.CreateNode<Fail>(tree));
      root.AddChild(Helper.CreateNode<Success>(tree));
      tree.Root = root;

      var result = Helper.RunBehaviourTree(tree);
      Assert.AreEqual(BehaviourNode.Status.Failure, result);
    }

    // A UnityTest behaves like a coroutine in Play Mode. In Edit Mode you can use
    // `yield return null;` to skip a frame.
    //[UnityTest]
    //public IEnumerator CompositeTestSuiteWithEnumeratorPasses()
    //{
    //  // Use the Assert class to test conditions.
    //  // Use yield to skip a frame.
    //  yield return null;
    //}
  }
}
