
using System.Text;
using Bonsai.Core;
using UnityEditor;
using UnityEngine;

namespace Bonsai.Designer
{
  public class BonsaiNode : IIterableNode<BonsaiNode>
  {
    /// <summary>
    /// The rect of the node in canvas space.
    /// </summary>
    public Rect bodyRect;

    private Rect contentRect;
    public Rect ContentRect
    {
      get { return contentRect; }
    }

    public GUIStyle HeaderStyle { get; } = CreateHeaderStyle();
    public GUIStyle BodyStyle { get; } = CreateBodyStyle();

    public GUIContent HeaderContent { get; } = new GUIContent();
    public GUIContent BodyContent { get; } = new GUIContent();

    protected BonsaiInputPort inputPort;
    protected BonsaiOutputPort outputPort;

    private readonly Texture icon;

    // Nodes fit well with snapping if their width has a multiple of snapStep and is even.
    public static readonly Vector2 kDefaultSize = Vector2.one * BonsaiEditor.snapStep * 8f;
    public static readonly Vector2 kContentOffset = new Vector2(20f, 5f);

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
    private BehaviourNode behaviour;

    public BehaviourNode Behaviour
    {
      get { return behaviour; }
      set
      {
        behaviour = value;
        UpdateGui();
      }
    }

    /// <summary>
    /// Create a new node for the first time.
    /// </summary>
    /// <param name="bCreateInput">If the node should have an input.</param>
    /// <param name="bCreateOuput">If the node should have an output.</param>
    public BonsaiNode(bool bCreateInput, bool bCreateOuput, bool bCanHaveMultipleChildren, Texture icon = null)
    {
      bodyRect = new Rect(Vector2.zero, kDefaultSize);

      if (bCreateInput)
      {
        inputPort = new BonsaiInputPort { parentNode = this };
      }

      if (bCreateOuput)
      {
        outputPort = new BonsaiOutputPort { parentNode = this };
      }

      this.bCanHaveMultipleChildren = bCanHaveMultipleChildren;
      this.icon = icon;
    }

    /// <summary>
    /// Called when the output port had an input connection removed.
    /// </summary>
    /// <param name="removedInputConnection"></param>
    public void OnInputConnectionRemoved(BonsaiInputPort removedInputConnection)
    {
      var disconnectedNode = removedInputConnection.parentNode;
      RemoveChild(disconnectedNode.behaviour);
    }

    /// <summary>
    /// Called when the output port made a connection to an input port.
    /// </summary>
    /// <param name="newInput"></param>
    public void OnNewInputConnection(BonsaiInputPort newInput)
    {
      var newChild = newInput.parentNode.behaviour;

      // If already connected, this occurs when 
      // building canvas from a loaded tree.
      if (ContainsChild(newChild))
      {
        return;
      }

      if (!CanAddChild(newChild))
      {
        Unparent(newChild);
      }

      AddChild(newChild);
    }

    public void NotifyParentOfPostionalReordering()
    {
      if (!behaviour.Parent) return;

      inputPort.outputConnection.SyncOrdering();
    }

    public void Destroy()
    {
      RemoveAllChildren();
      Unparent(behaviour);
      Object.DestroyImmediate(behaviour, true);

      if (inputPort != null)
      {
        inputPort.OnDestroy();
      }
    }

    public BonsaiInputPort Input
    {
      get { return inputPort; }
    }

    public BonsaiOutputPort Output
    {
      get { return outputPort; }
    }

    public BonsaiNode GetChildAt(int index)
    {
      return outputPort?.GetInput(index).parentNode;
    }

    public int ChildCount()
    {
      return outputPort == null ? 0 : outputPort.InputCount();
    }

    #region Behaviour Node Operations

    private bool CanAddChild(BehaviourNode child)
    {
      if (behaviour && child)
      {
        return behaviour.CanAddChild(child);
      }

      return false;
    }

    private bool ContainsChild(BehaviourNode child)
    {
      return child.Parent == behaviour;
    }

    /// <summary>
    /// Attempts to parent the child.
    /// </summary>
    /// <param name="parent"></param>
    /// <param name="child"></param>
    private void AddChild(BehaviourNode child)
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
    private void Unparent(BehaviourNode child)
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
    private void RemoveChild(BehaviourNode child)
    {
      if (behaviour && child)
      {
        behaviour.RemoveChild(child);
      }
    }

    private void RemoveAllChildren()
    {
      if (behaviour && behaviour.ChildCount() > 0)
      {
        behaviour.ClearChildren();
        Output.RemoveAllInputs();
      }
    }

    #endregion

    #region Styles and Contents

    public void UpdateGui()
    {
      HeaderContent.text = HeaderText();
      BodyContent.text = BodyText();

      if (icon)
      {
        HeaderContent.image = icon;
      }

      ResizeToFitContent();
    }

    private string HeaderText()
    {
      string text = behaviour.title;

      // Fall back to node name if there is no brief supplied.
      if (text == null || text.Length == 0)
      {
        text = NiceName();
      }

      return text;
    }

    private string BodyText()
    {
      var text = new StringBuilder();
      behaviour.Description(text);

      if (text.Length == 0)
      {
        text.Append(NiceName());
      }

      return text.ToString();
    }

    private static GUIStyle CreateHeaderStyle()
    {
      var style = new GUIStyle();
      style.normal.textColor = Color.white;
      style.fontSize = 15;
      style.fontStyle = FontStyle.Bold;
      style.imagePosition = ImagePosition.ImageLeft;
      style.contentOffset = kContentOffset;

      return style;
    }

    private static GUIStyle CreateBodyStyle()
    {
      var style = new GUIStyle();
      style.normal.textColor = Color.white;
      style.contentOffset = kContentOffset;
      return style;
    }

    private void ResizeToFitContent()
    {
      var prefs = BonsaiPreferences.Instance;

      Vector2 headerSize = HeaderContentSize();
      Vector2 bodySize = BodyContentSize();

      // The minium size to fit content.
      var contentSize = new Vector2(
        Mathf.Max(headerSize.x, bodySize.x),
        headerSize.y + bodySize.y);

      bodyRect.size = contentSize + prefs.nodeBodyPadding + prefs.nodeContentOffset;

      // Set the fixed width and height so icons are contrained and do not expand.
      Vector2 styleSize = headerSize + prefs.nodeContentPadding;
      HeaderStyle.fixedWidth = styleSize.x;
      HeaderStyle.fixedHeight = styleSize.y;

      contentRect.x = kContentOffset.x / 2f;
      contentRect.y = prefs.portHeight;
      contentRect.width = bodyRect.width - kContentOffset.x;
      contentRect.height = bodyRect.height - prefs.portHeight * 2f;
    }

    private Vector2 HeaderContentSize()
    {
      // Do not consider icon size. Manually set a size from text.
      // Round for sharp GUI content.
      Vector2 size = HeaderStyle.CalcSize(new GUIContent(HeaderContent.text));
      size.x = Mathf.Round(size.x);
      size.y = Mathf.Round(size.y);
      return size;
    }

    private Vector2 BodyContentSize()
    {
      Vector2 size = BodyStyle.CalcSize(BodyContent);
      size.x = Mathf.Round(size.x);
      size.y = Mathf.Round(size.y);
      return size;
    }

    private string NiceName()
    {
      return ObjectNames.NicifyVariableName(behaviour.GetType().Name);
    }

    #endregion
  }
}
