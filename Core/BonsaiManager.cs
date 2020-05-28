
using System.Collections.Generic;
using UnityEngine;

namespace Bonsai.Core
{
  /// <summary>
  /// Class collects all behaviour trees into a list and runs them on Update.
  /// </summary>
  public class BonsaiManager : MonoBehaviour
  {
    private const string kName = "Bonsai BT Manager";

    private static BonsaiManager _manager;
    public static BonsaiManager Manager
    {
      get
      {
        if (_manager == null)
        {
          _manager = FindObjectOfType<BonsaiManager>();
          _manager.name = kName;
        }

        return _manager;
      }
    }

    private static List<BehaviourTree> _trees = new List<BehaviourTree>();

    public static void AddTree(BehaviourTree blueprint)
    {
      var btInstance = BehaviourTree.Clone(blueprint);
      btInstance.Start();

      _trees.Add(btInstance);
    }

    public static void DestroyTree(BehaviourTree tree)
    {
      _trees.Remove(tree);
      ScriptableObject.Destroy(tree);
    }

    void Awake()
    {
      _trees.Clear();

      var btComps = FindObjectsOfType<BonsaiTreeComponent>();

      foreach (BonsaiTreeComponent btc in btComps)
      {

        BehaviourTree blueprint = btc.TreeBlueprint;

        if (blueprint)
        {

          var tree = BehaviourTree.Clone(blueprint);
          tree.parentGameObject = btc.gameObject;
          btc.bt = tree;

          _trees.Add(tree);
        }

        else
        {
          Debug.LogError("The associated behaviour tree is null.");
        }
      }
    }

    // Use this for initialization
    void Start()
    {
      foreach (BehaviourTree tree in _trees)
      {
        tree.Start();
      }
    }

    // Update is called once per frame
    void Update()
    {
      for (int i = 0; i < _trees.Count; ++i)
      {
        _trees[i].Update();
      }
    }

    void OnDestroy()
    {
      _trees.Clear();
    }

    void OnValidate()
    {
      name = kName;
    }

    void Reset()
    {
      name = kName;
    }

#if UNITY_EDITOR

    void OnDrawGizmos()
    {
      for (int i = 0; i < _trees.Count; ++i)
      {
        _trees[i].OnDrawGizmos();
      }
    }
#endif

  }
}