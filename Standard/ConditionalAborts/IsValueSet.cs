using System.Text;
using Bonsai.Core;
using Bonsai.Designer;
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

    public override bool Condition()
    {
      return Blackboard.IsSet(key);
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