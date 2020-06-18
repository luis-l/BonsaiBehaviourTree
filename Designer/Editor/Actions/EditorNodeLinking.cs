
using System;
using Bonsai.Core;

namespace Bonsai.Designer
{
  public class EditorNodeLinking
  {
    private Type referenceLinkType;

    public bool IsLinking { get; private set; }

    private Action<BehaviourNode> onLink;

    public void BeginLinking(Type linkType, Action<BehaviourNode> onLink)
    {
      referenceLinkType = linkType;
      IsLinking = true;
      this.onLink = onLink;
    }

    public void TryLink(BonsaiNode node)
    {
      if (IsLinking && node.Behaviour.GetType() == referenceLinkType)
      {
        onLink?.Invoke(node.Behaviour);
      }
    }

    public void EndLinking()
    {
      IsLinking = false;
      onLink = null;
    }
  }
}
