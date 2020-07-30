
using System.Collections.Generic;
using System.Text;
using Bonsai.Core;

namespace Bonsai.Standard
{
  /// <summary>
  /// Runs the branch that has the highest utility.
  /// </summary>
  [BonsaiNode("Composites/", "Reactive")]
  public class UtilitySelector : Selector
  {
    /// <summary>
    /// Method to evaluate utility.
    /// </summary>
    public enum Evaluation { Sum, Max }

    public Evaluation evaluation = Evaluation.Sum;

    [UnityEngine.SerializeField]
    public Utility.Timer timer = new Utility.Timer();

    private int highestUtilityChild = 0;
    private List<int> branchesLeftToRun;
    private TreeQuery utilityQueryIterator;

    public override void OnStart()
    {
      branchesLeftToRun = new List<int>(ChildCount());
      utilityQueryIterator = new TreeQuery(Tree.Height - LevelOrder);

      timer.AutoRestart = true;
      timer.OnTimeout += Evaluate;
    }

    public override void OnEnter()
    {
      Tree.AddTimer(timer);
      timer.Start();

      branchesLeftToRun.Clear();
      for (int i = 0; i < ChildCount(); i++)
      {
        branchesLeftToRun.Add(i);
      }

      highestUtilityChild = HighestUtilityBranch();

      base.OnEnter();
    }

    public override void OnExit()
    {
      Tree.RemoveTimer(timer);
    }

    public override void OnChildExit(int childIndex, Status childStatus)
    {
      // Find next highest utility child.
      if (childStatus == Status.Failure)
      {
        branchesLeftToRun.Remove(childIndex);
        highestUtilityChild = HighestUtilityBranch();
      }

      base.OnChildExit(childIndex, childStatus);
    }

    public override void OnAbort(int childIndex)
    {
      highestUtilityChild = childIndex;
    }

    // Get children by utility order.
    public override BehaviourNode CurrentChild()
    {
      if (branchesLeftToRun.Count == 0)
      {
        return null;
      }

      return Children[highestUtilityChild];
    }

    private void Evaluate()
    {
      // Try to get a higher utility branch.
      int previousChild = highestUtilityChild;
      highestUtilityChild = HighestUtilityBranch();

      // Found new higher utility.
      if (previousChild != highestUtilityChild)
      {
        BehaviourTree.Interrupt(GetChildAt(previousChild));

        // Mark the interruption as a failure, so the select goes to next child.
        lastChildExitStatus = Status.Failure;
      }
    }

    private int HighestUtilityBranch()
    {
      // Calculate the utility value of each branch.
      switch (evaluation)
      {
        case Evaluation.Sum:
          return GreatestSumUtilityBranch();
        case Evaluation.Max:
          return MaxUtilityBranch();
        default:
          break;
      }

      return 0;
    }

    private int GreatestSumUtilityBranch()
    {
      int highestChild = -1;
      float highest = int.MinValue;
      foreach (int childIndex in branchesLeftToRun)
      {
        float childUtility = utilityQueryIterator.SumUtility(GetChildAt(childIndex));
        if (childUtility > highest)
        {
          highest = childUtility;
          highestChild = childIndex;
        }
      }
      return highestChild;
    }

    private int MaxUtilityBranch()
    {
      int highestChild = -1;
      float highest = int.MinValue;
      foreach (int childIndex in branchesLeftToRun)
      {
        float childUtility = utilityQueryIterator.MaxUtility(GetChildAt(childIndex));
        if (childUtility > highest)
        {
          highest = childUtility;
          highestChild = childIndex;
        }
      }
      return highestChild;
    }

    public override void Description(StringBuilder builder)
    {
      builder.AppendFormat("Reevaluate every {0:0.00}s", timer.interval);
    }

  }
}