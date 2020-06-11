
using System;

namespace Bonsai.Utility
{

  public class Timer
  {
    public float WaitTime { get; set; } = 0f;
    public float TimeLeft { get; private set; } = 0f;
    public bool AutoRestart { get; set; } = false;

    public Action OnTimeout = delegate { };

    public void Start()
    {
      TimeLeft = WaitTime;
    }

    public void Update(float delta)
    {
      if (TimeLeft > 0f)
      {
        TimeLeft -= delta;
        if (IsDone)
        {
          OnTimeout();
          if (AutoRestart)
          {
            Start();
          }
        }
      }

    }

    public bool IsDone { get { return TimeLeft <= 0f; } }

    public bool IsRunning { get { return !IsDone; } }
  }

}
