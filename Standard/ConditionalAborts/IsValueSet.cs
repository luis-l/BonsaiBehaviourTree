using System.Text;
using Bonsai.Core;
using UnityEngine;

namespace Bonsai.Standard
{
  /// <summary>
  /// Tests if the value at a given key is not set to its default value.
  /// </summary>
  [BonsaiNode("Conditional/", "Condition")]
  public class IsValueSet : ConditionalAbort, Blackboard.IObserver
  {
    [Tooltip("The key to check if it has a value set.")]
    public string key;

    public override bool Condition()
    {
      return Blackboard.IsSet(key);
    }

    protected override void OnObserverBegin()
    {
      Blackboard.AddObserver(this);
    }

    protected override void OnObserverEnd()
    {
      Blackboard.RemoveObserver(this);
    }

    public void OnBlackboardChange(Blackboard.KeyEvent e)
    {
      if (key == e.Key)
      {
        Evaluate();
      }
    }

    public override void Description(StringBuilder builder)
    {
      base.Description(builder);
      builder.AppendLine();

      if (key == null || key.Length == 0)
      {
        builder.Append("No key is set to check");
      }
      else
      {
        builder.AppendFormat("Blackboard key: {0}", key);
      }
    }
  }
}