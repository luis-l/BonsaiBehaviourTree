
using System.Collections.Generic;
using NUnit.Framework;

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

      var root = Helper.CreateNode<Sequence>();
      root.AddChild(Helper.PassNode());
      root.AddChild(Helper.PassNode());
      root.AddChild(Helper.PassNode());
      tree.SetNodes(root);

      var result = Helper.RunBehaviourTree(tree);
      Assert.AreEqual(BehaviourNode.Status.Success, result);
    }

    [Test]
    public void SequenceFail()
    {
      BehaviourTree tree = Helper.CreateTree();

      var root = Helper.CreateNode<Sequence>();
      root.AddChild(Helper.PassNode());
      root.AddChild(Helper.FailNode());
      root.AddChild(Helper.PassNode());
      tree.SetNodes(root);

      var result = Helper.RunBehaviourTree(tree);
      Assert.AreEqual(BehaviourNode.Status.Failure, result);
    }

    [Test]
    public void RandomSequencePass()
    {
      BehaviourTree tree = Helper.CreateTree();

      var root = Helper.CreateNode<RandomSequence>();
      root.AddChild(Helper.PassNode());
      root.AddChild(Helper.PassNode());
      root.AddChild(Helper.PassNode());
      tree.SetNodes(root);

      var result = Helper.RunBehaviourTree(tree);
      Assert.AreEqual(BehaviourNode.Status.Success, result);
    }

    [Test]
    public void RandomSequenceFail()
    {
      BehaviourTree tree = Helper.CreateTree();

      var root = Helper.CreateNode<RandomSequence>();
      root.AddChild(Helper.PassNode());
      root.AddChild(Helper.FailNode());
      root.AddChild(Helper.PassNode());
      tree.SetNodes(root);

      var result = Helper.RunBehaviourTree(tree);
      Assert.AreEqual(BehaviourNode.Status.Failure, result);
    }

    [Test]
    public void SelectorPass()
    {
      BehaviourTree tree = Helper.CreateTree();

      var root = Helper.CreateNode<Selector>();
      root.AddChild(Helper.FailNode());
      root.AddChild(Helper.FailNode());
      root.AddChild(Helper.PassNode());
      tree.SetNodes(root);

      var result = Helper.RunBehaviourTree(tree);
      Assert.AreEqual(BehaviourNode.Status.Success, result);
    }

    [Test]
    public void SelectorFail()
    {
      BehaviourTree tree = Helper.CreateTree();

      var root = Helper.CreateNode<Selector>();
      root.AddChild(Helper.FailNode());
      root.AddChild(Helper.FailNode());
      root.AddChild(Helper.FailNode());
      tree.SetNodes(root);

      var result = Helper.RunBehaviourTree(tree);
      Assert.AreEqual(BehaviourNode.Status.Failure, result);
    }

    [Test]
    public void RandomSelectorPass()
    {
      BehaviourTree tree = Helper.CreateTree();

      var root = Helper.CreateNode<RandomSelector>();
      root.AddChild(Helper.FailNode());
      root.AddChild(Helper.FailNode());
      root.AddChild(Helper.PassNode());
      tree.SetNodes(root);

      var result = Helper.RunBehaviourTree(tree);
      Assert.AreEqual(BehaviourNode.Status.Success, result);
    }

    [Test]
    public void RandomSelectorFail()
    {
      BehaviourTree tree = Helper.CreateTree();

      var root = Helper.CreateNode<RandomSelector>();
      root.AddChild(Helper.FailNode());
      root.AddChild(Helper.FailNode());
      root.AddChild(Helper.FailNode());
      tree.SetNodes(root);

      var result = Helper.RunBehaviourTree(tree);
      Assert.AreEqual(BehaviourNode.Status.Failure, result);
    }

    [Test]
    public void ParallelPass()
    {
      BehaviourTree tree = Helper.CreateTree();

      var root = Helper.CreateNode<Parallel>();
      root.AddChild(Helper.PassNode());
      root.AddChild(Helper.PassNode());
      root.AddChild(Helper.PassNode());
      tree.SetNodes(root);

      var result = Helper.RunBehaviourTree(tree);
      Assert.AreEqual(BehaviourNode.Status.Success, result);
    }

    [Test]
    public void ParallelFail()
    {
      BehaviourTree tree = Helper.CreateTree();

      var root = Helper.CreateNode<Parallel>();
      root.AddChild(Helper.PassNode());
      root.AddChild(Helper.FailNode());
      root.AddChild(Helper.PassNode());
      tree.SetNodes(root);

      var result = Helper.RunBehaviourTree(tree);
      Assert.AreEqual(BehaviourNode.Status.Failure, result);
    }

    [Test]
    public void UtilitySelectorPass()
    {
      BehaviourTree tree = Helper.CreateTree();

      var root = Helper.CreateNode<UtilitySelector>();

      var alpha = Helper.PassNode().WithUtility(10f);
      var beta = Helper.FailNode().WithUtility(30f);
      var delta = Helper.FailNode().WithUtility(20f);

      var alphaDecorator = Helper.CreateNode<Success>();
      alphaDecorator.AddChild(alpha);

      root.AddChild(alphaDecorator);
      root.AddChild(beta);
      root.AddChild(delta);
      tree.SetNodes(root);

      var result = Helper.RunBehaviourTree(tree);
      Assert.AreEqual(BehaviourNode.Status.Success, result);
    }

    [Test]
    public void UtilitySelectorOrder()
    {
      BehaviourTree tree = Helper.CreateTree();

      var root = Helper.CreateNode<UtilitySelector>();

      var alpha = Helper.PassNode().WithUtility(10f);
      var beta = Helper.FailNode().WithUtility(30f);
      var delta = Helper.FailNode().WithUtility(20f);

      var alphaDecorator = Helper.CreateNode<Success>();
      alphaDecorator.AddChild(alpha);

      var betaDecorator = Helper.CreateNode<Failure>();
      betaDecorator.AddChild(beta);

      root.AddChild(alphaDecorator);
      root.AddChild(betaDecorator);
      root.AddChild(delta);
      tree.SetNodes(root);

      var result = Helper.RunBehaviourTree(tree);
      Assert.AreEqual(BehaviourNode.Status.Success, result);

      var expectedTraversal = new List<int>(new int[] { beta.PreOrderIndex, delta.PreOrderIndex, alpha.PreOrderIndex });
      Assert.AreEqual(expectedTraversal, tree.Blackboard.Get<List<int>>(TestNode.kHistoryKey));
    }

    [Test]
    public void UtilitySelectorFail()
    {
      BehaviourTree tree = Helper.CreateTree();
      var root = Helper.CreateNode<UtilitySelector>();

      var alpha = Helper.FailNode().WithUtility(10f);
      var beta = Helper.FailNode().WithUtility(30f);
      var delta = Helper.FailNode().WithUtility(20f);

      root.AddChild(alpha);
      root.AddChild(beta);
      root.AddChild(delta);
      tree.SetNodes(root);

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
