
using UnityEngine;

namespace Bonsai.Core
{
  public class BonsaiTreeComponent : MonoBehaviour
  {
    /// <summary>
    /// The tree blueprint asset used.
    /// </summary>
    [SerializeField]
    public BehaviourTree TreeBlueprint;

    // Tree instance of the blueprint. This is a clone of the tree blueprint asset.
    // The tree instance is what runs in game.
    internal BehaviourTree treeInstance;

    void Awake()
    {
      if (TreeBlueprint)
      {
        treeInstance = BehaviourTree.Clone(TreeBlueprint);
        treeInstance.actor = gameObject;
      }
      else
      {
        Debug.LogError("The behaviour tree is not set for " + gameObject);
      }
    }

    void Start()
    {
      treeInstance.Start();
      treeInstance.BeginTraversal();
    }

    void Update()
    {
      treeInstance.Update();
    }

    void OnDestroy()
    {
      Destroy(treeInstance);
    }

    /// <summary>
    /// The tree instance running in game.
    /// </summary>
    public BehaviourTree Tree
    {
      get { return treeInstance; }
    }
  }
}