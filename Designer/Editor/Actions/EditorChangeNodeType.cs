
using System;
using Bonsai.Core;
using UnityEngine;

namespace Bonsai.Designer
{
  public static class EditorChangeNodeType
  {
    public static void ChangeType(BonsaiNode node, Type newType)
    {
      var newBehaviour = ScriptableObject.CreateInstance(newType) as BehaviourNode;
      node.SetBehaviour(newBehaviour, NodeIcon(newType));
    }

    private static Texture NodeIcon(Type behaviourType)
    {
      var prop = BonsaiEditor.GetNodeTypeProperties(behaviourType);
      return BonsaiPreferences.Texture(prop.texName);
    }
  }
}
