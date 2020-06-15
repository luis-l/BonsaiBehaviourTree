
using UnityEngine;
using Bonsai.Core;
using Bonsai.Designer;
using System.Text;
using System.Linq;

namespace Bonsai.Standard
{
  /// <summary>
  /// Displays a message.
  /// </summary>
  [BonsaiNode("Tasks/", "Log")]
  public class Print : Task
  {
    public enum LogType { Normal, Warning, Error };

    [Multiline]
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

    public override void Description(StringBuilder builder)
    {
      builder.AppendFormat("{0} log", logType.ToString());

      // Nothing to display.
      if (message.Length == 0)
      {
        return;
      }

      string displayed = message;

      // Only consider display the message up to the newline.
      int newLineIndex = message.IndexOf('\n');
      if (newLineIndex >= 0)
      {
        displayed = message.Substring(0, newLineIndex);
      }

      // Nothing to display.
      if (displayed.Length == 0)
      {
        return;
      }

      // Separation for message.
      builder.AppendLine();

      // Cap the message length to display to keep things compact.
      int maxCharacters = 20;
      if (displayed.Length > maxCharacters)
      {
        builder.Append(displayed.Substring(0, maxCharacters));
        builder.Append("...");
      }
      else
      {
        builder.Append(displayed);
      }
    }

  }
}