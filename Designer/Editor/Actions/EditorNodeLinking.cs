
using System;
using Bonsai.Core;

namespace Bonsai.Designer
{
  public static class EditorNodeLinking
  {
    public static void ApplyLink(BonsaiNode node, Type linkType, Action<BehaviourNode> linker)
    {
      if (node != null && node.Behaviour.GetType() == linkType)
      {
        linker?.Invoke(node.Behaviour);
      }
    }
  }
}
