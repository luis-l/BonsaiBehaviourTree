
using System;
using System.Collections.Generic;
using Bonsai.Core;
using Bonsai.Standard;

namespace Bonsai.Designer
{
  using LinkArguments = Tuple<Type, Type>;
  using LinkAction = Action<BehaviourNode, BehaviourNode>;

  public static class EditorNodeLinking
  {
    /// <summary>
    /// Registered link actions given the Linkable Node types.
    /// </summary>
    private readonly static Dictionary<LinkArguments, LinkAction> linkActions = new Dictionary<LinkArguments, LinkAction>();

    /// <summary>
    /// Links the source with the other node.
    /// </summary>
    /// <param name="source"></param>
    /// <param name="other"></param>
    /// <returns>True if the link was applied.</returns>
    public static bool ApplyLink(BehaviourNode source, BehaviourNode other)
    {
      var key = new LinkArguments(source.GetType(), other.GetType());
      if (linkActions.TryGetValue(key, out LinkAction linker))
      {
        linker(source, other);
        return true;
      }

      return false;
    }

    static EditorNodeLinking()
    {
      AddLinkAction<Interruptor, Interruptable>(LinkInterruptions);
      AddLinkAction<Guard, Guard>(LinkGuards);
    }

    /// <summary>
    /// Register a link action between two node types.
    /// </summary>
    /// <typeparam name="TLinkerNode"></typeparam>
    /// <typeparam name="TLinkedNode"></typeparam>
    /// <param name="action"></param>
    public static void AddLinkAction<TLinkerNode, TLinkedNode>(LinkAction action)
    {
      linkActions.Add(new LinkArguments(typeof(TLinkerNode), typeof(TLinkedNode)), action);
    }

    public static void LinkInterruptions(BehaviourNode sourceNode, BehaviourNode otherNode)
    {
      var interruptor = sourceNode as Interruptor;

      var interruptable = otherNode as Interruptable;
      bool bAlreadyLinked = interruptor.linkedInterruptables.Contains(interruptable);

      // Works as a toggle, if already linked then unlink.
      if (bAlreadyLinked)
      {
        interruptor.linkedInterruptables.Remove(interruptable);

      }

      // If unlinked, then link.
      else
      {
        interruptor.linkedInterruptables.Add(interruptable);
      }
    }


    public static void LinkGuards(BehaviourNode sourceNode, BehaviourNode otherNode)
    {
      var guard = sourceNode as Guard;

      // Cannot link itself.
      if (otherNode == guard)
      {
        return;
      }

      var otherGuard = otherNode as Guard;
      bool isAlreadyLinked = guard.linkedGuards.Contains(otherGuard);

      // Works as a toggle, if already linked then unlink.
      if (isAlreadyLinked)
      {
        guard.linkedGuards.Remove(otherGuard);

        // The rest of the guards forget about this unlinked guard too.
        foreach (Guard linkedGuard in guard.linkedGuards)
        {
          linkedGuard.linkedGuards.Remove(otherGuard);
        }

        // The unlinked guard also loses all its linked guards.
        otherGuard.linkedGuards.Clear();
      }

      // If unlinked, then link.
      else
      {
        // The other guards must reference the new linked guard too.
        foreach (Guard link in guard.linkedGuards)
        {
          link.linkedGuards.Add(otherGuard);

          // The new linked guard must know about the other already linked guards.
          otherGuard.linkedGuards.Add(link);
        }

        // This guard add the new linked guard.
        guard.linkedGuards.Add(otherGuard);

        // The new linked guard must also know about this guard.
        otherGuard.linkedGuards.Add(guard);
      }
    }
  }
}
