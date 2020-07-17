
using UnityEngine;

namespace Bonsai.Core
{
  public class BonsaiTreeComponent : MonoBehaviour
  {
    /// <summary>
    /// The tree blueprint used.
    /// </summary>
    [SerializeField]
    public BehaviourTree TreeBlueprint;

    // The instance of the behaviour tree associated with this game object component.
    internal BehaviourTree bt;

    void Awake()
    {
      BonsaiManager.Instance.AddTree(this);
    }

    void OnDestroy()
    {
      if (BonsaiManager.Instance)
      {
        BonsaiManager.Instance.RemoveTree(bt);
      }

      if (bt)
      {
        Destroy(bt);
      }
    }

    public BehaviourTree Tree
    {
      get { return bt; }
    }
  }
}