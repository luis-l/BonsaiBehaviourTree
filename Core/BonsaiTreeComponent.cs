
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

    void OnDestroy()
    {
      // The manager handles destruction of the tree.
      BonsaiManager.DestroyTree(bt);
    }

    public BehaviourTree Tree
    {
      get { return bt; }
    }

    void Reset()
    {
      var manager = FindObjectOfType<BonsaiManager>();

      // Automaticall add the behaviour tree mananger if none exists.
      if (manager == null)
      {
        var gameobject = new GameObject();
        gameobject.AddComponent<BonsaiManager>();
        gameobject.transform.SetAsFirstSibling();
      }
    }
  }
}