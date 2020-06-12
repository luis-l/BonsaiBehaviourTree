
using UnityEngine;

namespace Bonsai.Designer
{
  public class BonsaiPreferences
  {
    public Texture2D backgroundTexture;
    public Texture2D rootSymbol;
    public Texture2D failureSymbol;
    public Texture2D successSymbol;
    public Texture2D selectedHighlightTexture;
    public Texture2D runningBackgroundTexture;
    public Texture2D abortHighlightTexture;
    public Texture2D referenceHighlightTexture;
    public Texture2D reevaluateHighlightTexture;

    public Texture2D portTexture;
    public Texture2D nodeBackgroundTexture;
    public Texture2D compositeTexture;
    public Texture2D taskTexture;
    public Texture2D decoratorTexture;
    public Texture2D conditionalTexture;
    public Texture2D serviceBackground;

    public Color rootSymbolColor;
    public Color runningStatusColor;
    public Color successColor;
    public Color failureColor;
    public Color abortedColor;
    public Color interruptedColor;
    public Color defaultConnectionColor = Color.white;

    public float defaultConnectionWidth = 4f;
    public float runningConnectionWidth = 4f;

    public BonsaiPreferences()
    {
      rootSymbolColor = new Color(0.3f, 0.3f, 0.3f, 1f);
      runningStatusColor = new Color(0.1f, 1f, 0.54f, 1f);
      successColor = new Color(0.1f, 1f, 0.54f, 0.25f);
      failureColor = new Color(1f, 0.1f, 0.1f, 0.25f);
      abortedColor = new Color(0.1f, 0.1f, 1f, 0.25f);
      interruptedColor = new Color(0.7f, 0.5f, 0.3f, 0.4f);

      backgroundTexture = BonsaiResources.GetTexture("Grid");
      rootSymbol = BonsaiResources.GetTexture("RootSymbol");
      successSymbol = BonsaiResources.GetTexture("Checkmark");
      failureSymbol = BonsaiResources.GetTexture("Cross");

      selectedHighlightTexture = BonsaiResources.GetTexture("SelectionHighlight");
      runningBackgroundTexture = BonsaiResources.GetTexture("GreenGradient");

      abortHighlightTexture = BonsaiResources.GetTexture("AbortHighlightGradient");
      referenceHighlightTexture = BonsaiResources.GetTexture("ReferenceHighlightGradient");
      reevaluateHighlightTexture = BonsaiResources.GetTexture("ReevaluateHighlightGradient");

      portTexture = BonsaiResources.GetTexture("PortTexture");
      nodeBackgroundTexture = BonsaiResources.GetTexture("NodeBackground");
      compositeTexture = BonsaiResources.GetTexture("CompositeBackground");
      taskTexture = BonsaiResources.GetTexture("TaskBackground");
      decoratorTexture = BonsaiResources.GetTexture("DecoratorBackground");
      conditionalTexture = BonsaiResources.GetTexture("ConditionalBackground");
      serviceBackground = BonsaiResources.GetTexture("ServiceBackground");
    }
  }
}
