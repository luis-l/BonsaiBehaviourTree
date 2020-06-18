
namespace Bonsai.Designer
{
  public class EditorMakingConnection
  {

    public bool IsMakingConnection { get; private set; }

    public BonsaiOutputPort OutputToConnect { get; private set; }

    public void BeginConnection(BonsaiInputPort port)
    {
      BeginConnection(port.outputConnection);

      // We disconnect the input since we want to change it to a new input.
      port.outputConnection.RemoveInputConnection(port);
    }

    public void BeginConnection(BonsaiOutputPort port)
    {
      IsMakingConnection = true;
      OutputToConnect = port;
    }

    public void EndConnection()
    {
      OutputToConnect = null;
      IsMakingConnection = false;
    }
  }
}
