## Bonsai Behaviour Tree
Advanced behavior tree solution with a graphical editor

![Bonsai Logo](http://i.imgur.com/rq9Tfja.png)

[![Bonsai Behaviour Tree Showcase](https://i.imgur.com/Cuddqco.png)](https://www.youtube.com/watch?v=BL6TUJwAFWg)

Videos:
- [Quick showcase of multiple features](https://twitter.com/i/status/866473174577401856)
- [Simple demo with agents](https://twitter.com/i/status/865356769572384776)
- [Nicify tree](https://twitter.com/i/status/855851944103092224)
- [Multi-Selection Actions](https://twitter.com/i/status/866830814234980352)
- [Interrupts and Guards](https://twitter.com/i/status/867516094537510912)


Goals of the project
- Lightweight, robust, and fast behaviour trees.
- Visual editor to improve the workflow when creating, running, and testing behaviour trees.
- Seamless integration with the Unity workflow/environment.

Features Overview:

- The core behaviour tree engine with a set of standard composite, decorator, and task nodes.
- Blackboard to share data between tasks.
- A visual editor to create, edit, view, and debug trees
- Conditional Aborts (AKA Observer aborts)
- Includes: Parallel execution, Interrupts, Semaphore Guards, Reactive (Dynamic) Selectors.
- Supports including Sub-trees
- Can easily create custom Composites, Decorators, Tasks
- Behaviour trees are ScriptableObjects, so it integrates perfectly with the Unity Editor.

Behaviour tree running.

![Behaviour tree running](http://i.imgur.com/aUe8neD.png)

### Run-time Editor Features and Limitations

During run-time you can view how the a tree executes and see which nodes are running, the statuses returned (success/failure) or if the nodes were aborted/interrupted.

You can also edit certain properties of a node, like changing the abort type, or setting a new waiting time for the wait task via the Unity Inspector.

Things that cannot be currently edited in run-time are (this may change in the future):
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
- The UNDO is still not implemented.

### API and Custom Tasks
There are four main categories of nodes which you can extend from to add functionality:

- Composite
- Decorator
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

    // The priority value of the node.
    // Default value for all nodes is the negated pre-order index,
    // since lower preorders are executed first (default behaviour).
    public virtual float Priority() { }

    // Called when a child caused an abort.
    protected internal virtual void OnAbort(ConditionalAbort aborter) { }

    // Call when a child finished executing
    protected internal virtual void OnChildExit(int childIndex, Status childStatus) { }

    // Called once after the entire tree is finished being copied.
    // Should be used to setup special BehaviourNode references.
    public virtual void OnCopy() { }
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
The trickier nodes to extend are composite nodes since they require knowing how to manipulate the "Iterator" in order to traverse nodes. The iterator can be manipulated to dictate how to traverse the tree.

### Performance

This is a benchmark running 5000 trees. No GC after startup. The tree in the image is the tree used for benchmark. Tested on a Intel Core i7-4790 @ 4 GHz. (Windows)

![Performance Benchmark](http://i.imgur.com/hm0yHM1.png)

I also ran the same benchmark on a Linux laptop. Intel i5-6200U @ 2.30 GHz. The "Time ms" was on average 4 ms.

### Limitations

Since I was aiming for a lightweight system, I decided not to provide complete, built-in functionality for serialization (data persistence is not available between running  game build sessions). The tree and blackboard structure is saved as an asset, but changing data values will not be persistent between game runs. For example, if you have a blackboard variable ["timer", 0.0f] and during the game run, the value goes up to say 10.0, you would need to save and load that value manually so its persistent between game saves.

I might add a simple system to serialize basic variable types in the blackboard such as int, string, Vector, structs...etc, the difficult, tricky part would be saving persistent object references or very complex objects like dictionaries with objects.

### Upcoming Features

- Undo functionality. Any modification to the tree will be undo-able.
- Run-time tree editing. This will allow you to (when the game/tree is running):
  - Add and delete nodes
  - Add children and re-parent nodes
  - Change connections between nodes
  - Change the child execution order
  - Change the root
  - Change the type of node, for example switching a Sequence to a Selector. The more complex version for this would changing a node to a Parallel node.
  - Change node references (changing linked interrupts or guards)
  - Plus more
- Extendable friendly editor. I want plugin creation to be simple to implement. There would be an Editor API that allows you to add custom behavior to the editor or allow you to completely change the look of the editor.

### Screenshots

The IsKeyDown decorator has a lower priority abort type set, so the sub-trees to the right are highlighted since they can be aborted by the decorator.

![Priority Abort](http://i.imgur.com/S7SVlja.png)

Here the semaphore guards are linked which highlight in orange, you can also see the custom inspector for the guard, making it easy to link other guards together.

![Guards](http://i.imgur.com/9w3f1PE.png)
