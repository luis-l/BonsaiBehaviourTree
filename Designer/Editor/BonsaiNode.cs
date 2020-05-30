
using UnityEngine;
using UnityEditor;

using Bonsai.Core;

namespace Bonsai.Designer
{
  public class BonsaiNode : TreeIterator<BonsaiNode>.IterableNode
  {
    /// <summary>
    /// The rect of the node in canvas space.
    /// </summary>
    public Rect bodyRect;

    private GUIStyle _iconNameStyle;
    private GUIContent _iconNameContent;

    protected BonsaiInputKnob _inputKnob;
    protected BonsaiOutputKnob _outputKnob;

    internal Texture iconTex;

    // Nodes fit well with snapping if their width has a multiple of snapStep and is even.
    public static readonly Vector2 kDefaultSize = new Vector2(BonsaiEditor.snapStep * 8f, 70);

    public readonly bool bCanHaveMultipleChildren = true;

    /// <summary>
    /// How much additional offset to apply when resizing.
    /// </summary>
    public const float resizePaddingX = 20f;

    /// <summary>
    /// A flag that helps the editor to highlight if it is selected from area selection.
    /// </summary>
    internal bool bAreaSelectionFlag = false;

    [SerializeField]
    internal BehaviourNode behaviour;

    /// <summary>
    /// Create a new node for the first time.
    /// </summary>
    /// <param name="parentCanvas">The canvas that the node belongs to.</param>
    /// <param name="bCreateInput">If the node should have an input.</param>
    /// <param name="bCreateOuput">If the node should have an output.</param>
    public BonsaiNode(BonsaiCanvas parentCanvas, bool bCreateInput, bool bCreateOuput, bool bCanHaveMultipleChildren)
    {
      bodyRect = new Rect(Vector2.zero, kDefaultSize);

      if (bCreateInput)
      {
        _inputKnob = new BonsaiInputKnob();
        _inputKnob.parentNode = this;
      }

      if (bCreateOuput)
      {
        _outputKnob = new BonsaiOutputKnob();
        _outputKnob.parentNode = this;
      }

      this.bCanHaveMultipleChildren = bCanHaveMultipleChildren;
    }

    /// <summary>
    /// Called when the output knob had an input connection removed.
    /// </summary>
    /// <param name="removedInputConnection"></param>
    public void OnInputConnectionRemoved(BonsaiInputKnob removedInputConnection)
    {
      var disconnectedNode = removedInputConnection.parentNode;
      removeChild(disconnectedNode.behaviour);
    }

    /// <summary>
    /// Called when the output knob made a connection to an input knob.
    /// </summary>
    /// <param name="newInput"></param>
    public void OnNewInputConnection(BonsaiInputKnob newInput)
    {
      var newChild = newInput.parentNode.behaviour;

      // If already connected, this occurs when 
      // building canvas from a loaded tree.
      if (containsChild(newChild))
      {
        return;
      }

      if (!canAddChild(newChild))
      {
        unparent(newChild);
      }

      addChild(newChild);
    }

    public void NotifyParentOfPostionalReordering()
    {
      if (!behaviour.Parent) return;

      _inputKnob.outputConnection.SyncOrdering();
    }

    public void Destroy()
    {
      removeAllChildren();
      unparent(behaviour);
      Object.DestroyImmediate(behaviour, true);

      if (_inputKnob != null)
      {
        _inputKnob.OnDestroy();
      }
    }

    public BonsaiInputKnob Input
    {
      get { return _inputKnob; }
    }

    public BonsaiOutputKnob Output
    {
      get { return _outputKnob; }
    }

    public BonsaiNode GetChildAt(int index)
    {
      return _outputKnob == null ? null : _outputKnob.GetInput(index).parentNode;
    }

    public int ChildCount()
    {
      return _outputKnob == null ? 0 : _outputKnob.InputCount();
    }

    #region Behaviour Node Operations

    private bool canAddChild(Core.BehaviourNode child)
    {
      if (behaviour && child)
      {
        return behaviour.CanAddChild(child);
      }

      return false;
    }

    private bool containsChild(Core.BehaviourNode child)
    {
      return child.Parent == behaviour;
    }

    /// <summary>
    /// Attempts to parent the child.
    /// </summary>
    /// <param name="parent"></param>
    /// <param name="child"></param>
    private void addChild(Core.BehaviourNode child)
    {
      if (behaviour && child)
      {
        behaviour.AddChild(child);
      }
    }

    /// <summary>
    /// Remove the child from its parent.
    /// </summary>
    /// <param name="parent"></param>
    /// <param name="child"></param>
    private void unparent(Core.BehaviourNode child)
    {
      if (child && child.Parent)
      {

        child.Parent.RemoveChild(child);
        Input.outputConnection.RemoveInputConnection(Input);
      }
    }

    /// <summary>
    /// Removes the child from this behaviour.
    /// </summary>
    /// <param name="parent"></param>
    /// <param name="child"></param>
    private void removeChild(Core.BehaviourNode child)
    {
      if (behaviour && child)
      {
        behaviour.RemoveChild(child);
      }
    }

    private void removeAllChildren()
    {
      if (behaviour && behaviour.ChildCount() > 0)
      {
        behaviour.ClearChildren();
        Output.RemoveAllInputs();
      }
    }

    #endregion

    #region Styles and Contents

    public string NiceName
    {
      get { return ObjectNames.NicifyVariableName(behaviour.GetType().Name); }
    }

    public GUIContent IconNameContent
    {
      get
      {
        if (_iconNameContent == null)
        {
          _iconNameContent = new GUIContent(NiceName, iconTex);
        }
        return _iconNameContent;
      }
    }

    public GUIStyle IconNameStyle
    {
      get
      {
        if (_iconNameStyle == null)
        {
          SetupStyle();
        }

        return _iconNameStyle;
      }
    }

    /// <summary>
    /// Sets up the style to render the node.
    /// </summary>
    public void SetupStyle()
    {
      _iconNameStyle = new GUIStyle();
      _iconNameStyle.normal.textColor = Color.white;
      _iconNameStyle.alignment = TextAnchor.LowerCenter;

      _iconNameStyle.imagePosition = ImagePosition.ImageAbove;

      // Test if the name fits
      Vector2 contentSize = _iconNameStyle.CalcSize(new GUIContent(NiceName));

      // Resize width of the node body.
      if (contentSize.x > bodyRect.width - resizePaddingX)
      {

        bodyRect.width = contentSize.x + resizePaddingX;

        // Make sure width is even for best results.
        bodyRect.width = Mathf.Ceil(bodyRect.width / 2f) * 2f;

        // Round it to the nearest multiple of the snap-step size.
        bodyRect.width = Mathf.Round(bodyRect.width / BonsaiEditor.snapStep) * BonsaiEditor.snapStep;

        // Should be whole number after rounding.
        int stepUnits = (int)(bodyRect.width / BonsaiEditor.snapStep);

        // Cannot be evenly divided by the snap step.
        if (stepUnits % 2 != 0)
        {

          // Add enough width so it can be evenly divided.
          bodyRect.width += BonsaiEditor.snapStep / 2f;

          // Round it to the nearest multiple of the snap-step size.
          bodyRect.width = Mathf.Round(bodyRect.width / BonsaiEditor.snapStep) * BonsaiEditor.snapStep;
        }

      }

      _iconNameStyle.fixedHeight = bodyRect.height - 5f;
      _iconNameStyle.fixedWidth = bodyRect.width;
    }

    #endregion
  }
}
