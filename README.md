[![License: MIT](https://img.shields.io/badge/License-MIT-yellow.svg)](https://opensource.org/licenses/MIT)
[![Join the chat at https://gitter.im/BonsaiBehaviourTree/community](https://badges.gitter.im/BonsaiBehaviourTree/community.svg)](https://gitter.im/BonsaiBehaviourTree/community?utm_source=badge&utm_medium=badge&utm_campaign=pr-badge&utm_content=badge)

## Bonsai Behaviour Tree

![Bonsai Logo](https://i.imgur.com/WTxGOZC.png) 

Advanced behavior tree solution with a graphical editor

![Bonsai Editor](https://i.imgur.com/KQZgWtF.png)

Goals of the project
- Lightweight, robust, and fast behaviour trees.
- Visual editor to build and view behaviour trees.
- Seamless integration with the Unity workflow/environment.
- No GC overhead.

Features Overview:

- The core behaviour tree engine with a set of standard composite, decorator, and task nodes.
- Blackboard to share data between tasks.
- A visual editor to create, edit, view, and debug trees.
- Conditional Aborts (observer aborts) which are event driven.
- Includes: Parallel execution, Interrupts, Guards, Services, Timers.
- Supports running Subtrees.
- Can easily create custom Composites, Decorators, Tasks.
- Behaviour trees are ScriptableObjects, so it integrates perfectly with the Unity Editor.

Behaviour tree running.

![Behaviour tree running](https://i.imgur.com/0DLgw5C.png)

### Editor Features and Limitations

During Play mode you can view how the a tree executes and see which nodes are running, the statuses returned (sucesss, failure, aborted, interrupted).

You can also edit certain properties of a node, like changing the abort type, or setting a new waiting time for the wait task via the Unity Inspector.

Things that cannot be currently edited in Play mode:
- Add or delete nodes
- Changing the root
- Changing connections between nodes

#### Overview

- A canvas which can be panned and zoomed
- Multi selection support so you can apply multi-edit/drag/duplicate/delete.
- Grid snapping
- Sub-tree dragging - when you drag a node, the entire sub-tree under it drags along.
- Save and load behaviour tree assets.
- Attributes which can be used on a custom node class to categorize and add an icon to your custom node.
- A simple [nicify](https://twitter.com/i/status/855851944103092224) feature which automatically lays out the tree neatly.
- Multiple behaviour tree editors can be opened at once.
- Viewing a running behaviour tree just requires clicking on a game object with behaviour tree component.
- Behaviour tree assets can be opened by double clicking on the asset file.
- Editor Preferences to change editor behaviour. Node look can be customized. Colors, size, and layouts can be changed.
- View variables when tree is running. e.g. The time left for the Wait Task. 

### API and Custom Tasks

These are the base nodes which can be extended:

- Composite
  - Parallel
- Decorator
  - Services
  - Conditional Abort
- Task

In order to add custom functionality you can override key methods:
```csharp
    // Called only once when the tree is started.
    public virtual void OnStart() { }

    // The logic that the node executes.
    public abstract Status Run();

    // Called when a traversal begins on the node.
    public virtual void OnEnter() { }

    // Called when a traversal on the node ends.
    public virtual void OnExit() { }

    // Called when a child caused an abort.
    public virtual void OnAbort(ConditionalAbort aborter) { }

    // Call when a child finished executing
    public virtual void OnChildExit(int childIndex, Status childStatus) { }
```
Example of a simple, custom Wait task:
```csharp
    [BonsaiNode("Tasks/", "Timer")]
    public class Wait : Task
    {
        private float timer = 0f;

        public float waitTime = 1f;

        public override void OnEnter()
        {
            timer = 0f;
        }

        public override Status Run()
        {
            timer += Time.deltaTime;

            if (timer >= waitTime) {
                return Status.Success;
            }

            return Status.Running;
        }
    }
```

### Performance

This is a benchmark running 5000 trees. No GC after startup. The tree in the image is the tree used for benchmark. Tested on a Intel Core i7-4790 @ 4 GHz. (Windows)

![Performance Benchmark](http://i.imgur.com/hm0yHM1.png)

The same benchmark was run on a Linux laptop. Intel i5-6200U @ 2.30 GHz. The "Time ms" was on average 4 ms.

### Limitations

Since the goal of this project was a lightweight system, a complete, built-in functionality for serialization is not provided. The tree and blackboard structure is saved as an asset, but changing data values will not be persistent between game runs. For example, if you have a blackboard variable ["timer", 0.0f] and during the game run, the value goes up to say 10.0, you would need to save and load that value manually so its persistent between game saves.

### Old Screenshots and videos

[![Bonsai Behaviour Tree Showcase](https://i.imgur.com/Cuddqco.png)](https://www.youtube.com/watch?v=BL6TUJwAFWg)

Videos:
- [Quick showcase of multiple features](https://twitter.com/i/status/866473174577401856)
- [Simple demo with agents](https://twitter.com/i/status/865356769572384776)
- [Nicify tree](https://twitter.com/i/status/855851944103092224)
- [Multi-Selection Actions](https://twitter.com/i/status/866830814234980352)
- [Interrupts and Guards](https://twitter.com/i/status/867516094537510912)

The IsKeyDown decorator has a lower priority abort type set, so the sub-trees to the right are highlighted since they can be aborted by the decorator.

![Priority Abort](http://i.imgur.com/S7SVlja.png)

Here the semaphore guards are linked which highlight in orange, you can also see the custom inspector for the guard, making it easy to link other guards together.

![Guards](http://i.imgur.com/9w3f1PE.png)
