
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace Bonsai.Designer
{
  /// <summary>
  /// Handles creation and deletion of nodes.
  /// </summary>
  public static class EditorNodeCreation
  {
    public static BonsaiNode DuplicateSingle(BonsaiCanvas canvas, BonsaiNode original)
    {
      BonsaiNode duplicate = canvas.CreateNode(original.Behaviour.GetType());

      // Duplicate nodes are placed offset from the original.
      duplicate.Position = original.Position + Vector2.one * 40f;

      return duplicate;
    }

    /// <summary>
    /// Duplicate multiple nodes and preserve the connections between parent and child nodes.
    /// </summary>
    /// <param name="canvas"></param>
    /// <param name="tree"></param>
    /// <param name="originals"></param>
    /// <returns></returns>
    public static List<BonsaiNode> DuplicateMultiple(BonsaiCanvas canvas, IEnumerable<BonsaiNode> originals)
    {
      var duplicateMap = originals.ToDictionary(og => og, og => DuplicateSingle(canvas, og));

      // Reconstruct connection in clone nodes.
      foreach (BonsaiNode original in originals)
      {
        for (int i = 0; i < original.ChildCount(); i++)
        {
          // Only consider children if they were also cloned.
          if (duplicateMap.TryGetValue(original.GetChildAt(i), out BonsaiNode cloneChild))
          {
            BonsaiNode cloneParent = duplicateMap[original];
            cloneChild.SetParent(cloneParent);
          }
        }
      }

      return duplicateMap.Values.ToList();
    }
  }
}

