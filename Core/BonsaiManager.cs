
using System;
using System.Collections.Generic;
using UnityEngine;

namespace Bonsai.Core
{
  /// <summary>
  /// Class collects all behaviour trees into a list and runs them on Update.
  /// </summary>
  public class BonsaiManager : MonoBehaviour
  {
    private static readonly Lazy<BonsaiManager> lazyInstance = new Lazy<BonsaiManager>(
      GetOrCreateSceneBonsaiManager,
      isThreadSafe: false);

    private const string kName = "Bonsai Manager";

    private readonly List<BehaviourTree> trees = new List<BehaviourTree>();

    public static BonsaiManager Instance
    {
      get { return lazyInstance.Value; }
    }

    public void AddTree(BonsaiTreeComponent btc)
    {
      BehaviourTree blueprint = btc.TreeBlueprint;

      if (blueprint)
      {
        var tree = BehaviourTree.Clone(blueprint);
        tree.actor = btc.gameObject;
        btc.bt = tree;
        trees.Add(tree);
      }

      else
      {
        Debug.LogWarning("The behaviour tree is null for " + btc.gameObject);
      }
    }

    public void RemoveTree(BehaviourTree tree)
    {
      trees.Remove(tree);
    }

    private static BonsaiManager GetOrCreateSceneBonsaiManager()
    {
      var manager = FindObjectOfType<BonsaiManager>();
      if (!manager)
      {
        var gameobject = new GameObject();
        gameobject.AddComponent<BonsaiManager>();
        gameobject.transform.SetAsFirstSibling();
        manager = gameobject.GetComponent<BonsaiManager>();
      }
      return manager;
    }

    void Start()
    {
      foreach (BehaviourTree tree in trees)
      {
        tree.Start();
      }
    }

    void Update()
    {
      for (int i = 0; i < trees.Count; ++i)
      {
        trees[i].Update();
      }
    }

    void OnDestroy()
    {
      trees.Clear();
    }

    void OnValidate()
    {
      name = kName;
    }

    void Reset()
    {
      name = kName;
    }
  }
}