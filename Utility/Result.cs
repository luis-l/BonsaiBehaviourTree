
using System;

namespace Bonsai.Utility
{
  public class Result<TError, TValue>
  {
    public TError Error { get; }
    public TValue Value { get; }

    public bool Success { get; }
    public bool Failure { get { return !Success; } }

    protected Result(TError error)
    {
      Error = error;
      Success = false;
    }

    protected Result(TValue value)
    {
      Value = value;
      Success = true;
    }

    public static Result<TError, TValue> Fail(TError error)
    {
      return new Result<TError, TValue>(error);
    }

    public static Result<TError, TValue> Ok(TValue value)
    {
      return new Result<TError, TValue>(value);
    }

    public Result<TError, TValue> OnSuccess(Action<TValue> action)
    {
      if (Success)
      {
        action(Value);

        // Propagate the value. 
        return Ok(Value);
      }

      // Propagate the error.
      return Fail(Error);
    }


    public Result<TError, TValue> OnFailure(Action<TError> action)
    {
      if (Failure)
      {
        action(Error);
        return Fail(Error);
      }

      return Ok(Value);
    }

  }
}
