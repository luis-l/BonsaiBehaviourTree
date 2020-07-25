using System;

namespace Bonsai.Core
{
  /// <summary>
  /// <para>Flag that the Utility.Timer will be updated during Tree tick.</para>
  /// <para>
  /// This is an optimization to let the Tree know how many timers will be used
  /// so enough capacity is reserved.
  /// </para>
  /// Attribute will only be considered for Utility.Timer fields.
  /// </summary>
  [AttributeUsage(AttributeTargets.Field)]
  public class TreeTimerAttribute : Attribute
  {
  }
}
