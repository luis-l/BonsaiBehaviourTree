
namespace Bonsai.Designer
{
  public static class EditorNodeConnecting
  {
    /// <summary>
    /// Get the output for the input to do the connection.
    /// The input is disconnected.
    /// </summary>
    /// <param name="input"></param>
    /// <returns></returns>
    public static BonsaiOutputPort StartConnection(BonsaiInputPort input)
    {
      // Check if we are making connection starting from input
      // Starting a connection from input means that its connected output will change its input.
      BonsaiOutputPort output = input.outputConnection;

      if (output != null)
      {
        // We disconnect the input since we want to change it to a new input.
        output.RemoveInputConnection(input);
      }

      return output;
    }

    /// <summary>
    /// Creates a connection between the output and the input or node under the mouse.
    /// </summary>
    /// <param name="coord"></param>
    /// <param name="output"></param>
    public static void FinishConnection(Coord coord, BonsaiOutputPort output)
    {
      coord.OnMouseOverNodeOrInput(node =>
      {
        output.Add(node.Input);

        // When a connection is made, we need to make sure the positional
        // ordering reflects the internal tree structure.
        node.NotifyParentOfPostionalReordering();
      });
    }

  }
}
