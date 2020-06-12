
using UnityEngine;

using Bonsai.Core;
using Bonsai.Designer;

namespace Bonsai.Standard
{
  [BonsaiNode("Conditional/", "Condition")]
  public class Chance : ConditionalTask
  {
    [Tooltip("The probability that the condition succeeds.")]
    [Range(0f, 1f)]
    public float chance = 0.5f;

    public override bool Condition()
    {
      // Get a random value between 0 and 1.
      float prob = (float)Tree.Random.NextDouble();

      // Return true if the probability is within the range [0, chance];
      return prob <= chance;
    }
  }
}