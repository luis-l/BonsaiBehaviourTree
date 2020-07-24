
using System.Collections.Generic;
using Bonsai.Core;
using Bonsai.Standard;
using NUnit.Framework;

namespace Tests
{
  public class CompositeTestSuite
  {
    [Test]
    public void SequencePass()
    {
      var root = Helper.CreateNode<Sequence>();
      root.SetChildren(new[] { Helper.PassNode(), Helper.PassNode(), Helper.PassNode() });

      BehaviourTree tree = Helper.CreateTree();
      tree.SetNodes(root);

      var result = Helper.RunBehaviourTree(tree);
      Assert.AreEqual(BehaviourNode.Status.Success, result);
    }

    [Test]
    public void SequenceFail()
    {
      var root = Helper.CreateNode<Sequence>();
      root.SetChildren(new[] { Helper.PassNode(), Helper.FailNode(), Helper.PassNode() });

      BehaviourTree tree = Helper.CreateTree();
      tree.SetNodes(root);

      var result = Helper.RunBehaviourTree(tree);
      Assert.AreEqual(BehaviourNode.Status.Failure, result);
    }

    [Test]
    public void RandomSequencePass()
    {
      var root = Helper.CreateNode<RandomSequence>();
      root.SetChildren(new[] { Helper.PassNode(), Helper.PassNode(), Helper.PassNode() });

      BehaviourTree tree = Helper.CreateTree();
      tree.SetNodes(root);

      var result = Helper.RunBehaviourTree(tree);
      Assert.AreEqual(BehaviourNode.Status.Success, result);
    }

    [Test]
    public void RandomSequenceFail()
    {
      var root = Helper.CreateNode<RandomSequence>();
      root.SetChildren(new[] { Helper.PassNode(), Helper.FailNode(), Helper.PassNode() });

      BehaviourTree tree = Helper.CreateTree();
      tree.SetNodes(root);

      var result = Helper.RunBehaviourTree(tree);
      Assert.AreEqual(BehaviourNode.Status.Failure, result);
    }

    [Test]
    public void SelectorPass()
    {
      var root = Helper.CreateNode<Selector>();
      root.SetChildren(new[] { Helper.FailNode(), Helper.FailNode(), Helper.PassNode() });

      BehaviourTree tree = Helper.CreateTree();
      tree.SetNodes(root);

      var result = Helper.RunBehaviourTree(tree);
      Assert.AreEqual(BehaviourNode.Status.Success, result);
    }

    [Test]
    public void SelectorFail()
    {
      var root = Helper.CreateNode<Selector>();
      root.SetChildren(new[] { Helper.FailNode(), Helper.FailNode(), Helper.FailNode() });

      BehaviourTree tree = Helper.CreateTree();
      tree.SetNodes(root);

      var result = Helper.RunBehaviourTree(tree);
      Assert.AreEqual(BehaviourNode.Status.Failure, result);
    }

    [Test]
    public void RandomSelectorPass()
    {
      var root = Helper.CreateNode<RandomSelector>();
      root.SetChildren(new[] { Helper.FailNode(), Helper.FailNode(), Helper.PassNode() });

      BehaviourTree tree = Helper.CreateTree();
      tree.SetNodes(root);

      var result = Helper.RunBehaviourTree(tree);
      Assert.AreEqual(BehaviourNode.Status.Success, result);
    }

    [Test]
    public void RandomSelectorFail()
    {
      var root = Helper.CreateNode<RandomSelector>();
      root.SetChildren(new[] { Helper.FailNode(), Helper.FailNode(), Helper.FailNode() });

      BehaviourTree tree = Helper.CreateTree();
      tree.SetNodes(root);

      var result = Helper.RunBehaviourTree(tree);
      Assert.AreEqual(BehaviourNode.Status.Failure, result);
    }

    [Test]
    public void ParallelPass()
    {
      var root = Helper.CreateNode<Parallel>();
      root.SetChildren(new[] { Helper.PassNode(), Helper.PassNode(), Helper.PassNode() });

      BehaviourTree tree = Helper.CreateTree();
      tree.SetNodes(root);

      var result = Helper.RunBehaviourTree(tree);
      Assert.AreEqual(BehaviourNode.Status.Success, result);
    }

    [Test]
    public void ParallelFail()
    {
      var root = Helper.CreateNode<Parallel>();
      root.SetChildren(new[] { Helper.PassNode(), Helper.FailNode(), Helper.PassNode() });

      BehaviourTree tree = Helper.CreateTree();
      tree.SetNodes(root);

      var result = Helper.RunBehaviourTree(tree);
      Assert.AreEqual(BehaviourNode.Status.Failure, result);
    }

    [Test]
    public void UtilitySelectorPass()
    {
      var alpha = Helper.PassNode().WithUtility(10f);
      var beta = Helper.FailNode().WithUtility(30f);
      var delta = Helper.FailNode().WithUtility(20f);

      var alphaDecorator = Helper.CreateNode<Success>();
      alphaDecorator.SetChild(alpha);

      var root = Helper.CreateNode<UtilitySelector>();
      root.SetChildren(new BehaviourNode[] { alphaDecorator, beta, delta });

      BehaviourTree tree = Helper.CreateTree();
      tree.SetNodes(root);

      var result = Helper.RunBehaviourTree(tree);
      Assert.AreEqual(BehaviourNode.Status.Success, result);
    }

    [Test]
    public void UtilitySelectorOrder()
    {
      var alpha = Helper.PassNode().WithUtility(10f);
      var beta = Helper.FailNode().WithUtility(30f);
      var delta = Helper.FailNode().WithUtility(20f);

      var alphaDecorator = Helper.CreateNode<Success>();
      alphaDecorator.SetChild(alpha);

      var betaDecorator = Helper.CreateNode<Failure>();
      betaDecorator.SetChild(beta);

      var root = Helper.CreateNode<UtilitySelector>();
      root.SetChildren(new BehaviourNode[] { alphaDecorator, betaDecorator, delta });

      BehaviourTree tree = Helper.CreateTree();
      tree.SetNodes(root);

      var result = Helper.RunBehaviourTree(tree);
      Assert.AreEqual(BehaviourNode.Status.Success, result);

      var expectedTraversal = new List<int>(new int[] { beta.PreOrderIndex, delta.PreOrderIndex, alpha.PreOrderIndex });
      Assert.AreEqual(expectedTraversal, tree.blackboard.Get<List<int>>(TestNode.kHistoryKey));
    }

    [Test]
    public void UtilitySelectorFail()
    {
      var alpha = Helper.FailNode().WithUtility(10f);
      var beta = Helper.FailNode().WithUtility(30f);
      var delta = Helper.FailNode().WithUtility(20f);

      var root = Helper.CreateNode<UtilitySelector>();
      root.SetChildren(new[] { alpha, beta, delta });

      BehaviourTree tree = Helper.CreateTree();
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
