
using UnityEngine;

using Bonsai.Core;
using Bonsai.Designer;

namespace Bonsai.Standard
{
  [NodeEditorProperties("Conditional/", "Condition")]
  public class Chance : ConditionalTask
  {
    [Tooltip("The probability that the condition succeeds.")]
    [Range(0f, 1f)]
    public float chance = 0.5f;

    [Tooltip("The seed for the random generator to use.")]
    public int seed;

    public bool useSeed;

    private System.Random rand;

    public override void OnStart()
    {
      rand = useSeed ? new System.Random(seed) : new System.Random();
    }

    public override bool Condition()
    {
      // Get a random value between 0 and 1.
      float prob = (float)rand.NextDouble();

      // Return true if the probability is within the range [0, chance];
      return prob <= chance;
    }
  }
}