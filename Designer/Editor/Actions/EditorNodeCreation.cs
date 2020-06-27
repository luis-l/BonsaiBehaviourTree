
using UnityEngine;
using Bonsai.Core;
using System.Collections.Generic;
using System.Linq;

namespace Bonsai.Designer
{
  /// <summary>
  /// Handles creation and deletion of nodes.
  /// </summary>
  public static class EditorNodeCreation
  {
    public static BonsaiNode DuplicateSingle(BonsaiCanvas canvas, BehaviourTree tree, BonsaiNode original)
    {
      BonsaiNode duplicate = canvas.CreateNode(original.Behaviour.GetType(), tree);

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
    public static List<BonsaiNode> DuplicateMultiple(
      BonsaiCanvas canvas,
      BehaviourTree tree,
     IEnumerable<BonsaiNode> originals)
    {
      var duplicateMap = originals.ToDictionary(og => og, og => DuplicateSingle(canvas, tree, og));

      // Reconstruct connection in clone nodes.
      foreach (BonsaiNode original in originals)
      {
        for (int i = 0; i < original.ChildCount(); i++)
        {
          // Only consider children if they were also cloned.
          if (duplicateMap.TryGetValue(original.GetChildAt(i), out BonsaiNode cloneChild))
          {
            BonsaiNode cloneParent = duplicateMap[original];

            // Connect parent/child clones.
            cloneParent.Output.Add(cloneChild.Input);
          }
        }
      }

      return duplicateMap.Values.ToList();
    }
  }
}

