using NUnit.Framework;
using Bonsai.Core;
using Bonsai.Standard;

namespace Tests
{
  public class CoreTreeTests
  {
    // A Test behaves as an ordinary method
    [Test]
    public void TreeCloning()
    {
      BehaviourTree tree = Helper.CreateTree();

      var root = Helper.CreateNode<Sequence>();
      var alpha = Helper.PassNode();
      var beta = Helper.PassNode();
      var delta = Helper.PassNode();
      root.SetChildren(new[] { alpha, beta, delta });
      tree.SetNodes(root);

      BehaviourTree cloneTree = BehaviourTree.Clone(tree);
      Assert.AreEqual(4, cloneTree.Nodes.Length);

      Assert.AreEqual(0, cloneTree.Nodes[0].PreOrderIndex);
      Assert.AreEqual(1, cloneTree.Nodes[1].PreOrderIndex);
      Assert.AreEqual(2, cloneTree.Nodes[2].PreOrderIndex);
      Assert.AreEqual(3, cloneTree.Nodes[3].PreOrderIndex);

      Assert.AreEqual(0, cloneTree.Nodes[0].ChildOrder);
      Assert.AreEqual(0, cloneTree.Nodes[1].ChildOrder);
      Assert.AreEqual(1, cloneTree.Nodes[2].ChildOrder);
      Assert.AreEqual(2, cloneTree.Nodes[3].ChildOrder);

      var parent = cloneTree.Root;
      Assert.AreEqual(parent, cloneTree.Nodes[1].Parent);
      Assert.AreEqual(parent, cloneTree.Nodes[2].Parent);
      Assert.AreEqual(parent, cloneTree.Nodes[3].Parent);

      var result = Helper.RunBehaviourTree(cloneTree);
      Assert.AreEqual(BehaviourNode.Status.Success, result);
    }

  }
}
