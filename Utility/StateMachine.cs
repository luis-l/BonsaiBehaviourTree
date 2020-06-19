using System;
using System.Collections.Generic;
using UnityEngine;

namespace Bonsai.Utility
{
  /// <summary>
  /// A finite state machine.
  /// </summary>
  /// <typeparam name="T">The date type to be stored by the machine states.</typeparam>
  public class StateMachine<T>
  {
    /// <summary>
    /// Called when the machine finishes transitioning to another state.
    /// </summary>
    public Action StateChanged;

    protected readonly Dictionary<T, State> states = new Dictionary<T, State>();

    /// <summary>
    /// Get all the state data.
    /// </summary>
    /// <returns></returns>
    public IEnumerable<T> Data => states.Keys;

    public State CurrentState { get; protected set; }

    /// <summary>
    /// Adds a state to the machine.
    /// </summary>
    /// <param name="data"></param>
    public void AddState(T data)
    {
      var s = new State(data);
      states.Add(data, s); ;
    }

    public void AddState(State s)
    {
      states.Add(s.Value, s);
    }

    public void AddTransition(State start, State end, Func<bool> condition, Action onMakingTransition)
    {
      var t = new Transition(condition)
      {
        Transitioned = onMakingTransition
      };

      AddTransition(start, end, t);
    }

    public void AddTransition(State start, State end, Transition t)
    {
      start.Add(t);
      t.SetNextState(end);
    }

    /// <summary>
    /// Add a transition that goes from start to end state.
    /// </summary>
    /// <param name="start"></param>
    /// <param name="end"></param>
    /// <param name="t"></param>
    public void AddTransition(T start, T end, Transition t)
    {
      var startST = GetState(start);
      var endST = GetState(end);

      if (startST == null || endST == null)
      {
        Debug.LogError("State(s) are not in the state machine");
        return;
      }

      AddTransition(startST, endST, t);
    }

    /// <summary>
    /// Add two transitions.
    /// One from start to end.
    /// Another from end to start.
    /// </summary>
    /// <param name="start"></param>
    /// <param name="end"></param>
    /// <param name="startToEnd"></param>
    /// <param name="endToStart"></param>
    public void AddBiTransition(T start, T end, Transition startToEnd, Transition endToStart)
    {
      var startST = GetState(start);
      var endST = GetState(end);

      if (startST == null || endST == null)
      {
        Debug.LogError("State(s) are not in the state machine");
        return;
      }

      AddTransition(startST, endST, startToEnd);
      AddTransition(endST, startST, endToStart);
    }

    /// <summary>
    /// Gets the state associated with the data.
    /// </summary>
    /// <param name="data"></param>
    /// <returns></returns>
    public State GetState(T data)
    {
      if (states.ContainsKey(data))
      {
        return states[data];
      }

      return null;
    }

    /// <summary>
    /// Sets the current active state of the machine.
    /// </summary>
    /// <param name="data"></param>
    public void SetCurrentState(T data)
    {
      var state = GetState(data);

      if (state == null)
      {
        Debug.LogError(data + " is not in the state machine.");
      }

      else
      {
        CurrentState = state;
      }
    }

    /// <summary>
    /// Handles moving to next state when conditions are met.
    /// </summary>
    public void Update()
    {
      if (CurrentState == null)
      {
        return;
      }

      Transition validTrans = null;

      // Pick the next state if the transition conditions are met.
      for (int i = 0; i < CurrentState.Transitions.Count; i++)
      {
        if (CurrentState.Transitions[i].AllConditionsMet())
        {
          validTrans = CurrentState.Transitions[i];
          break;
        }
      }

      if (validTrans != null)
      {
        // Exit current state.
        CurrentState.StateExited?.Invoke();
        validTrans.Transitioned?.Invoke();

        // Enter new state.
        CurrentState = validTrans.NextState;
        CurrentState.StateEntered?.Invoke();
        StateChanged?.Invoke();
      }
    }

    /// <summary>
    /// A transition between two states than only occurs if all
    /// its conditions are satisfied.
    /// </summary>
    public class Transition
    {
      private readonly List<Func<bool>> conditions = new List<Func<bool>>();

      /// <summary>
      /// Called after the 'from' state exits and before the 'to' state enters.
      /// </summary>
      public Action Transitioned;

      public Transition()
      {

      }

      /// <summary>
      /// Pass in initial conditions
      /// </summary>
      /// <param name="?"></param>
      public Transition(params Func<bool>[] conditions)
      {
        foreach (var c in conditions)
        {
          AddCondition(c);
        }
      }

      /// <summary>
      /// Adds a condition that must be satisfied in order to do the transition.
      /// </summary>
      /// <param name="cond"></param>
      public void AddCondition(Func<bool> cond)
      {
        conditions.Add(cond);
      }

      /// <summary>
      /// Tests if all the conditions of the transition are satisfied.
      /// </summary>
      /// <returns></returns>
      public bool AllConditionsMet()
      {
        for (int i = 0; i < conditions.Count; i++)
        {
          if (!conditions[i]()) return false;
        }

        // All conditions returned true.
        return true;
      }

      /// <summary>
      /// Set the state that transition goes to.
      /// </summary>
      /// <param name="next"></param>
      public void SetNextState(State next)
      {
        NextState = next;
      }

      /// <summary>
      /// The state that transition goes to.
      /// </summary>
      public State NextState { get; private set; } = null;
    }

    /// <summary>
    /// A state of the machine.
    /// </summary>
    public class State
    {
      /// <summary>
      /// Executes when the machine transitions into this state.
      /// </summary>
      public Action StateEntered;

      /// <summary>
      /// Executes when the machine transitions out of this state.
      /// </summary>
      public Action StateExited;

      /// <summary>
      /// Construct a state with its data.
      /// </summary>
      /// <param name="data"></param>
      public State(T data)
      {
        Value = data;
      }

      /// <summary>
      /// Adds a transition to the state.
      /// </summary>
      /// <param name="t"></param>
      public void Add(Transition t)
      {
        Transitions.Add(t);
      }

      /// <summary>
      /// The data held by the state.
      /// </summary>
      public T Value { get; }

      /// <summary>
      /// The transitions connected to the state.
      /// </summary>
      public List<Transition> Transitions { get; } = new List<Transition>();
    }
  }
}