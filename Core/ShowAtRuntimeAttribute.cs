

using System;

namespace Bonsai
{
  /// <summary>
  /// Displays the field or property value in the BehaviourNode inspector when the tree runs.
  /// </summary>
  [AttributeUsage(AttributeTargets.Field | AttributeTargets.Property)]
  public class ShowAtRuntimeAttribute : Attribute
  {
    public readonly string label;
    public ShowAtRuntimeAttribute(string label = null)
    {
      this.label = label;
    }
  }
}
