
using System;

namespace Bonsai.Utility
{
  [Serializable]
  public class Timer
  {
    /// <summary>
    /// The wait time for the timer in seconds.
    /// </summary>
    [UnityEngine.Min(0)]
    public float interval = 1f;

    /// <summary>
    /// The random deviation of the interval. 
    /// <para>
    /// If not zero, a random value between [-deviation, +deviation] 
    /// is added to <see cref="interval"/> on <see cref="Start"/>
    /// </para>
    /// </summary>
    [UnityEngine.Tooltip("Adds a random range value to the interval between [-Deviation, +Deviation]")]
    [UnityEngine.Min(0)]
    public float deviation = 0f;

    /// <summary>
    /// The time left on the timer.
    /// </summary>
    public float TimeLeft { get; private set; } = 0f;

    /// <summary>
    /// If the timer should <see cref="Start"/> itself again after time out.
    /// </summary>
    public bool AutoRestart { get; set; } = false;

    /// <summary>
    /// Occurs when <see cref="TimeLeft"/> reaches 0.
    /// </summary>
    public event Action OnTimeout = delegate { };

    /// <summary>
    /// Starts the timer. Time left is reset to the interval and applies random deviation.
    /// </summary>
    public void Start()
    {
      TimeLeft = interval;

      if (deviation != 0f)
      {
        TimeLeft += UnityEngine.Random.Range(-deviation, deviation);
      }
    }

    /// <summary>
    /// Updates the timer.
    /// </summary>
    /// <param name="delta">The elapsed time seconds to update the timer.</param>
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

    public override string ToString()
    {
      return TimeLeft.ToString();
    }
  }

}
