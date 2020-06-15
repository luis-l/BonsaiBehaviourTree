## Bonsai Behaviour Tree

![Bonsai Logo](https://i.imgur.com/WTxGOZC.png) 

Advanced behavior tree solution with a graphical editor

![Bonsai Editor](https://i.imgur.com/KQZgWtF.png)

Goals of the project
- Lightweight, robust, and fast behaviour trees.
- Visual editor to improve the workflow when creating, running, and testing behaviour trees.
- Seamless integration with the Unity workflow/environment.

Features Overview:

- The core behaviour tree engine with a set of standard composite, decorator, and task nodes.
- Blackboard to share data between tasks.
- A visual editor to create, edit, view, and debug trees
- Conditional Aborts (AKA Observer aborts)
- Includes: Parallel execution, Interrupts, Guards, Services, Concurrent Branch Evaluation.
- Supports including Sub-trees
- Change node titles, descriptions and comments.
- Can easily create custom Composites, Decorators, Tasks
- Behaviour trees are ScriptableObjects, so it integrates perfectly with the Unity Editor.

Behaviour tree running.

![Behaviour tree running](https://i.imgur.com/0DLgw5C.png)

### Editor Features and Limitations

During Play mode you can view how the a tree executes and see which nodes are running, the statuses returned (success/failure) or if the nodes were aborted/interrupted.

You can also edit certain properties of a node, like changing the abort type, or setting a new waiting time for the wait task via the Unity Inspector.

Things that cannot be currently edited in Play mode:
- Adding/deleting nodes
- Changing the root
- Changing connections between nodes

### Editor Features

- A canvas which can be panned and zoomed
- Add, delete, drag, duplicate, and connect nodes.
- There is multi selection support so you can apply multi-edit/drag/duplicate/delete.
- Grid snapping
- Sub-tree dragging - when you drag a node, the entire sub-tree under it drags along.
- Save and load behaviour tree assets.
- Nodes resize properly to fit internal contents such as the name of node.
- Context menus to organize nodes.
- Attributes which can be used on a custom node class to categorize and add an icon to your custom node.
- A custom blackboard inspector to add variables (specify key and type).
- Custom inspector for nodes that need to reference other nodes like Interrupts and Semaphore Guards, the inspector lets you push a button to activate a linking action, in which you can click on nodes to link.
- A simple [nicify](https://twitter.com/i/status/855851944103092224) feature which automatically lays out the tree neatly.
- Visual highlighting feedback to quickly see what nodes are being referenced by other nodes and which nodes get aborted by a conditional abort.
- Multiple behaviour tree editors can be opened at once.
- Viewing a running behaviour tree just requires clicking on a game object with behaviour tree component.
- Behaviour tree assets can be opened by double clicking on the asset file.
- Editor Preferences to change editor behaviour. Node look can be customized. Colors, size, and layouts can be changed.

### API and Custom Tasks
There are four main categories of nodes which you can extend from to add functionality:

- Composite
- Decorator
- Services
- Conditional Abort
- Conditional Task
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
    public void OnChildExit(int childIndex, Status childStatus) { }
```
Example of a simple, custom Wait task:
```csharp
    [NodeEditorProperties("Tasks/", "Timer")]
    public class Wait : Task
    {
        private float _timer = 0f;

        public float waitTime = 1f;

        public override void OnEnter()
        {
            _timer = 0f;
        }

        public override Status Run()
        {
            _timer += Time.deltaTime;

            if (_timer >= waitTime) {
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

### Upcoming Features
- Undo functionality. Any modification to the tree will be undo-able.
- Show relevant information on Node visual.
- View node property during Play. (e.g. View time left on Wait Task)

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
