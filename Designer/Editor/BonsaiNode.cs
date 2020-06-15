
using System.Text;
using Bonsai.Core;
using Bonsai.Utility;
using UnityEditor;
using UnityEngine;

namespace Bonsai.Designer
{
  public class BonsaiNode : IIterableNode<BonsaiNode>
  {
    private Rect rectPosition;

    /// <summary>
    /// The rect of the node in canvas space.
    /// </summary>
    public Rect RectPositon { get { return rectPosition; } }

    private Rect contentRect;
    public Rect ContentRect { get { return contentRect; } }

    public GUIStyle HeaderStyle { get; } = CreateHeaderStyle();
    public GUIStyle BodyStyle { get; } = CreateBodyStyle();
    public GUIContent HeaderContent { get; } = new GUIContent();
    public GUIContent BodyContent { get; } = new GUIContent();

    protected BonsaiInputPort inputPort;
    protected BonsaiOutputPort outputPort;

    // Nodes fit well with snapping if their width has a multiple of snapStep and is even.
    public static readonly Vector2 kDefaultSize = Vector2.one * 100;

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
      if (bCreateInput)
      {
        inputPort = new BonsaiInputPort(this);
      }

      if (bCreateOuput)
      {
        outputPort = new BonsaiOutputPort(this);
      }

      this.bCanHaveMultipleChildren = bCanHaveMultipleChildren;

      if (icon)
      {
        HeaderContent = new GUIContent(icon);
      }
    }

    public Vector2 Position
    {
      get { return rectPosition.position; }
      set
      {
        rectPosition.position = value;
        UpdatePortPositions();
      }
    }

    public Vector2 Size
    {
      get { return rectPosition.size; }
      set
      {
        rectPosition.size = value;
        UpdatePortPositions();
      }
    }

    public Vector2 Center
    {
      get { return rectPosition.center; }
      set
      {
        rectPosition.center = value;
        UpdatePortPositions();
      }
    }

    /// <summary>
    /// Called when the output port had an input connection removed.
    /// </summary>
    /// <param name="removedInputConnection"></param>
    public void OnInputConnectionRemoved(BonsaiInputPort removedInputConnection)
    {
      var disconnectedNode = removedInputConnection.ParentNode;
      RemoveChild(disconnectedNode.behaviour);
    }

    /// <summary>
    /// Called when the output port made a connection to an input port.
    /// </summary>
    /// <param name="newInput"></param>
    public void OnNewInputConnection(BonsaiInputPort newInput)
    {
      var newChild = newInput.ParentNode.behaviour;

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
      return outputPort?.GetInput(index).ParentNode;
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


    public void UpdatePortPositions()
    {
      float w = rectPosition.width - BonsaiPreferences.Instance.portWidthTrim;
      float h = BonsaiPreferences.Instance.portHeight;

      if (Input != null)
      {
        float x = rectPosition.x + (rectPosition.width - Input.RectPosition.width) / 2f;
        float y = rectPosition.yMin;
        Input.RectPosition = new Rect(x, y, w, h);

      }

      if (Output != null)
      {
        float x = rectPosition.x + (rectPosition.width - Output.RectPosition.width) / 2f;
        float y = rectPosition.yMax - Output.RectPosition.height;
        Output.RectPosition = new Rect(x, y, w, h);
      }
    }

    #region Styles and Contents

    public void UpdateGui()
    {
      HeaderContent.text = HeaderText();
      BodyContent.text = BodyText();
      ResizeToFitContent();
      UpdatePortPositions();
    }

    private string HeaderText()
    {
      string text = behaviour.title;

      // Fall back to node name if there is no brief supplied.
      if (string.IsNullOrEmpty(text))
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

      if (!string.IsNullOrEmpty(behaviour.comment))
      {
        text.AppendLine();
        text.AppendLine();
        text.Append(behaviour.comment);
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
      return style;
    }

    private static GUIStyle CreateBodyStyle()
    {
      var style = new GUIStyle();
      style.normal.textColor = Color.white;
      return style;
    }

    private void ResizeToFitContent()
    {
      var prefs = BonsaiPreferences.Instance;

      float portHeights = 2f * prefs.portHeight;
      Vector2 contentSize = MinimumRequiredContentSize();

      rectPosition.size = contentSize
        + 2f * prefs.nodeSizePadding
        + 2f * Vector2.right * prefs.nodeWidthPadding
        + Vector2.up * portHeights;

      contentRect.width = rectPosition.width - 2f * prefs.nodeWidthPadding;
      contentRect.height = rectPosition.height - portHeights;
      contentRect.x = prefs.nodeWidthPadding;
      contentRect.y = prefs.portHeight;

      // Place content relative to the content rect.
      Vector2 contentOffset = contentRect.position + prefs.nodeSizePadding;
      HeaderStyle.contentOffset = MathExtensions.Round(contentOffset);
      BodyStyle.contentOffset = MathExtensions.Round(contentOffset);

      // Round for UI Sharpness.
      contentRect = MathExtensions.Round(contentRect);
      rectPosition = MathExtensions.Round(rectPosition);
    }

    private Vector2 MinimumRequiredContentSize()
    {
      Vector2 headerSize = HeaderContentSize();
      Vector2 bodySize = BodyContentSize();
      float maxContentWidth = Mathf.Max(headerSize.x, bodySize.x);
      float totalContentHeight = headerSize.y + bodySize.y;
      return new Vector2(maxContentWidth, totalContentHeight);
    }

    private Vector2 HeaderContentSize()
    {
      // Manually add the icon size specified in preferences.
      // This was done because using CalcSize(HeaderContent) (with the icon set in GUIContent's image)
      // caused the nodes to be incorrectly sized when opening a tree from the inspector.
      // e.g. Clicking on a GameObjects tree asset from Bonsai Tree Component.
      float iconSize = BonsaiPreferences.Instance.iconSize;
      Vector2 size = HeaderStyle.CalcSize(new GUIContent(HeaderText()));
      return new Vector2(size.x + iconSize, Mathf.Max(size.y, iconSize));
    }

    private Vector2 BodyContentSize()
    {
      return BodyStyle.CalcSize(BodyContent);
    }

    private string NiceName()
    {
      return ObjectNames.NicifyVariableName(behaviour.GetType().Name);
    }

    #endregion
  }
}
