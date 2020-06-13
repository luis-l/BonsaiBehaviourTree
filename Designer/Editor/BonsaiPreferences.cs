using UnityEngine;

namespace Bonsai.Designer
{
  [CreateAssetMenu(fileName = "BonsaiPreferences", menuName = "Bonsai/Create Preferences")]
  public class BonsaiPreferences : ScriptableObject
  {
    [Header("Editor Textures")]
    public Texture2D gridTexture;
    public Texture2D failureSymbol;
    public Texture2D successSymbol;

    [Header("Node Textures")]
    public Texture2D nodeBackgroundTexture;
    public Texture2D nodeGradient;
    public Texture2D portTexture;
    public Texture2D rootSymbol;

    [Header("Node Colors")]
    public Color compositeColor;
    public Color decoratorColor;
    public Color conditionalColor;
    public Color serviceColor;
    public Color taskColor;

    [Header("Status Colors")]
    public Color defaultNodeBackgroundColor;
    public Color selectedColor;
    public Color runningColor;
    public Color abortColor;
    public Color referenceColor;
    public Color evaluateColor;
    public Color rootSymbolColor = new Color(0.3f, 0.3f, 0.3f, 1f);

    [Header("Runtime Colors")]
    public Color runningStatusColor = new Color(0.1f, 1f, 0.54f, 1f);
    public Color successColor = new Color(0.1f, 1f, 0.54f, 0.25f);
    public Color failureColor = new Color(1f, 0.1f, 0.1f, 0.25f);
    public Color abortedColor = new Color(0.1f, 0.1f, 1f, 0.25f);
    public Color interruptedColor = new Color(0.7f, 0.5f, 0.3f, 0.4f);
    public Color defaultConnectionColor = Color.white;

    [Header("Connection Lines")]
    public float defaultConnectionWidth = 4f;
    public float runningConnectionWidth = 4f;

    [Header("Node Properties")]
    public Vector2 nodeBodyPadding = new Vector2(80f, 50f);
    public Vector2 nodeContentOffset = new Vector2(20f, 5f);
    public Vector2 nodeContentPadding = new Vector2(60f, 10f);
    public float portHeight = 15f;

    private static BonsaiPreferences instance;

    public static BonsaiPreferences Instance
    {
      get
      {
        if (instance == null)
        {
          instance = LoadDefaultPreferences();
        }
        return instance;
      }

      set
      {
        instance = value;
      }
    }

    public static BonsaiPreferences LoadDefaultPreferences()
    {
      BonsaiPreferences prefs = Resources.Load<BonsaiPreferences>("DefaultBonsaiPreferences");

      if (prefs == null)
      {
        Debug.LogWarning("Failed to load DefaultBonsaiPreferences");
        // Empty preferences. Editor will not render nodes correctly.
        prefs = CreateInstance<BonsaiPreferences>();
      }

      return prefs;
    }

    public static Texture2D Texture(string name)
    {
      return Resources.Load<Texture2D>(name);
    }
  }
}
