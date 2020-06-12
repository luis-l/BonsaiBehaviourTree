
using UnityEngine;
using Bonsai.Core;
using Bonsai.Designer;

namespace Bonsai.Standard
{
  /// <summary>
  /// Displays a message.
  /// </summary>
  [BonsaiNode("Tasks/", "Log")]
  public class Print : Task
  {
    public enum LogType { Normal, Warning, Error };

    public string message = "Print Node";

    [Tooltip("The type of message to display.")]
    public LogType logType = LogType.Normal;

    public override Status Run()
    {
      switch (logType)
      {

        case LogType.Normal:
          Debug.Log(message);
          break;

        case LogType.Warning:
          Debug.LogWarning(message);
          break;

        case LogType.Error:
          Debug.LogError(message);
          break;
      }

      return Status.Success;
    }
  }
}