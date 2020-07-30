
using System;
using System.Text;
using Bonsai.Core;
using UnityEngine;

namespace Bonsai.Standard
{
  /// <summary>
  /// Tests if the value at a given key is not set to its default value.
  /// </summary>
  [BonsaiNode("Conditional/", "Condition")]
  public class IsValueSet : ConditionalAbort
  {
    [Tooltip("The key to check if it has a value set.")]
    public string key;

    private Action<Blackboard.KeyEvent> OnBlackboardChanged;

    public override void OnStart()
    {
      OnBlackboardChanged = delegate (Blackboard.KeyEvent e)
      {
        if (key == e.Key)
        {
          Evaluate();
        }
      };
    }

    public override bool Condition()
    {
      return Blackboard.IsSet(key);
    }

    protected override void OnObserverBegin()
    {
      Blackboard.AddObserver(OnBlackboardChanged);
    }

    protected override void OnObserverEnd()
    {
      Blackboard.RemoveObserver(OnBlackboardChanged);
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