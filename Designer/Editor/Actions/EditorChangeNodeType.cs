
using System;

namespace Bonsai.Designer
{
  public static class EditorChangeNodeType
  {
    public static bool ChangeType(BonsaiNode node, Type newType)
    {
      // Type conversion can only for same base types.
      if (node.Behaviour is Core.Composite && newType.IsSubclassOf(typeof(Core.Composite)))
      {

        return true;
      }
      else if (node.Behaviour is Core.Decorator && newType.IsSubclassOf(typeof(Core.Decorator)))
      {

        return true;
      }
      else if (node.Behaviour is Core.Task && newType.IsSubclassOf(typeof(Core.Task)))
      {

        return true;
      }

      // Can not convert.
      return false;
    }

    private static void ChangeCompositeType(BonsaiNode node, Type newType)
    {

    }

    private static void ChangeDecoratorType(BonsaiNode node, Type newType)
    {

    }

    private static void ChangeTaskType(BonsaiNode node, Type newType)
    {

    }
  }
}
