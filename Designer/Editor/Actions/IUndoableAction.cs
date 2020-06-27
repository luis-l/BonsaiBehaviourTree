
namespace Bonsai.Designer
{
  public interface IUndoableAction
  {
    void Undo();
    void Redo();
  }
}
