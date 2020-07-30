
using System;
using System.Text;
using Bonsai.Core;
using UnityEngine;

namespace Bonsai.Standard
{
  /// <summary>
  /// Compares two values from the blackboard.
  /// </summary>
  [BonsaiNode("Conditional/", "Condition")]
  public class CompareEntries : ConditionalAbort
  {
    public string key1;
    public string key2;

    [Tooltip("If the comparison should test for inequality")]
    public bool compareInequality = false;

    private Action<Blackboard.KeyEvent> OnBlackboardChanged;

    public override void OnStart()
    {
      OnBlackboardChanged = delegate (Blackboard.KeyEvent e)
      {
        if (e.Key == key1 || e.Key == key2)
        {
          Evaluate();
        }
      };

    }

    public override bool Condition()
    {
      Blackboard bb = Blackboard;

      // If any of the keys is non-existant then return false.
      if (!bb.Contains(key1) || !bb.Contains(key2))
      {
        return false;
      }

      object val1 = bb.Get(key1);
      object val2 = bb.Get(key2);

      // Use the polymorphic equals so value types are properly tested as well
      // as taking into account custom equality operations.
      bool bResult = val1.Equals(val2);

      return compareInequality ? !bResult : bResult;
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

      if (string.IsNullOrEmpty(key1) || string.IsNullOrEmpty(key2))
      {
        builder.AppendLine("Keys are not set");
      }

      else
      {
        builder.AppendFormat("Compare {0} and {1}", key1, key2);
      }

    }

  }
}