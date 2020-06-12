
using System.Text;
using Bonsai.Core;
using UnityEditor;
using UnityEngine;

namespace Bonsai.Designer
{
  public class StyledContent
  {
    public GUIContent content;
    public GUIStyle style;
  }

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

    private StyledContent header;
    private StyledContent body;

    public StyledContent Header
    {
      get
      {
        if (header == null)
        {
          header = new StyledContent { content = CreateHeaderContent() };
          header.style = CreateHeaderStyle(header.content);
        }
        return header;
      }
    }

    public StyledContent Body
    {
      get
      {
        if (body == null)
        {
          body = new StyledContent { content = CreateBodyContent() };
          body.style = CreateBodyStyle(body.content);
        }
        return body;
      }
    }

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
      if (header == null)
      {
        header = new StyledContent();
      }

      if (body == null)
      {
        body = new StyledContent();
      }

      header.content = CreateHeaderContent();
      header.style = CreateHeaderStyle(header.content);

      body.content = CreateBodyContent();
      body.style = CreateBodyStyle(body.content);
    }

    private GUIContent CreateHeaderContent()
    {
      string header = behaviour.brief;

      // Fall back to node name if there is no brief supplied.
      if (header == null || header.Length == 0)
      {
        header = NiceName();
      }

      if (icon)
      {
        return new GUIContent(header, icon);
      }
      else
      {
        return new GUIContent(header);
      }
    }

    private GUIContent CreateBodyContent()
    {
      var body = new StringBuilder();
      behaviour.StaticDescription(body);

      if (body.Length == 0)
      {
        body.Append(NiceName());
      }

      return new GUIContent(body.ToString());
    }

    private GUIStyle CreateHeaderStyle(GUIContent content)
    {
      var style = new GUIStyle();
      style.normal.textColor = Color.white;
      style.fontSize = 15;
      style.fontStyle = FontStyle.Bold;
      style.imagePosition = ImagePosition.ImageLeft;
      style.contentOffset = kContentOffset;

      // Do not consider icon size. Manually set a size from text.
      // Round for sharp GUI content.
      Vector2 contentSize = style.CalcSize(new GUIContent(content.text));
      contentSize.x = Mathf.Round(contentSize.x);
      contentSize.y = Mathf.Round(contentSize.y);

      bodyRect.width = contentSize.x + 80f + kContentOffset.x;
      bodyRect.height = contentSize.y + 50f + kContentOffset.y;

      style.fixedWidth = contentSize.x + 60f;
      style.fixedHeight = contentSize.y + 10f;

      contentRect.x = kContentOffset.x / 2f;
      contentRect.y = BonsaiPort.kMinSize.y;
      contentRect.width = bodyRect.width - kContentOffset.x;
      contentRect.height = bodyRect.height - BonsaiPort.kMinSize.y * 2f;

      return style;
    }

    private GUIStyle CreateBodyStyle(GUIContent content)
    {
      var style = new GUIStyle();
      style.normal.textColor = Color.white;
      style.contentOffset = kContentOffset;
      Vector2 contentSize = style.CalcSize(content);
      bodyRect.height += contentSize.y;

      contentRect.height += contentSize.y;

      return style;
    }

    private string NiceName()
    {
      return ObjectNames.NicifyVariableName(behaviour.GetType().Name);
    }

    #endregion
  }
}
