
namespace Bonsai.Designer
{
  public static class EditorNodeConnecting
  {
    /// <summary>
    /// Get the parent to begin the connection.
    /// </summary>
    /// <param name="child">The child to orphan.</param>
    /// <returns></returns>
    public static BonsaiNode StartConnection(BonsaiNode child)
    {
      // Starting a connection from a child means that its parent will make a new connection.
      // So the child becomes orphaned.
      BonsaiNode parent = child.Parent;
      child.SetParent(null);
      return parent;
    }

    /// <summary>
    /// Creates a connection between the output and the input or node under the mouse.
    /// </summary>
    /// <param name="coord"></param>
    /// <param name="output"></param>
    public static void FinishConnection(BonsaiCanvas canvas, BonsaiNode parent, BonsaiNode child)
    {
      canvas.AddChild(parent, child);
    }
  }
}
