using UnityEngine;

namespace Bonsai.Designer
{
  [CreateAssetMenu(fileName = "BonsaiPreferences", menuName = "Bonsai/Preferences")]
  public class BonsaiPreferences : ScriptableObject
  {
    // The unit length of the grid in pixels.
    // Note: Grid Texture has 12.8 as length, fix texture to be even.
    private const int kGridSize = 12;

    [Header("Editor")]
    public int snapStep = kGridSize;
    public float zoomDelta = 0.2f;

    [Min(0.1f)]
    public float minZoom = 1f;

    public float maxZoom = 5f;
    public float panSpeed = 1.2f;

    [Space()]
    public Texture2D gridTexture;
    public Texture2D failureSymbol;
    public Texture2D successSymbol;

    [Header("Node Textures")]
    public Texture2D nodeBackgroundTexture;
    public Texture2D nodeGradient;
    public Texture2D portTexture;

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

    [Header("Node Body Layout")]
    [Tooltip("Controls additional node size.")]
    public Vector2 nodeSizePadding = new Vector2(12f, 6f);

    [Tooltip("Controls the thickness of left and right edges.")]
    public float nodeWidthPadding = 12f;

    [Tooltip("Controls how thick the ports are. Changes the nodes overall height too.")]
    public float portHeight = 20f;

    [Tooltip("Control how far the ports extend in the node.")]
    public float portWidthTrim = 50f;

    public float iconSize = 32f;
    public float statusIconSize = 16f;

    private static BonsaiPreferences instance = null;

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
      var prefs = Resources.Load<BonsaiPreferences>("DefaultBonsaiPreferences");

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
