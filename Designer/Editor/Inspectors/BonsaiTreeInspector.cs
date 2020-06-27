
using System;
using System.Collections.Generic;

using UnityEngine;
using UnityEditor;

using Bonsai.Core;

namespace Bonsai.Designer
{
  [CustomEditor(typeof(BehaviourTree))]
  public class BonsaiTreeInspector : Editor
  {
    public static float labelWidth = 50f;

    private string _keyNameSelection = "";
    private TypeSelectionTree _typeSelectionInputTree = new TypeSelectionTree();

    #region GUI Properties

    // The space in pixels of the indentation.
    private const float kIndentationWidth = 20f;

    // State of the fold out if it is opened or closed.
    private bool _bAddVariableFoldout = false;
    private bool _bShowBlackboardFoldout = true;

    // Cache the label spacing options.
    private static GUILayoutOption[] _labelSpacingOptions;
    public static GUILayoutOption[] LabelSpacingOptions
    {
      get
      {
        if (_labelSpacingOptions == null)
        {
          _labelSpacingOptions = new GUILayoutOption[] { GUILayout.Width(35f) };
        }

        return _labelSpacingOptions;
      }
    }

    private static GUIStyle _darkBackgroundStyle;
    public static GUIStyle DarkBackgroundStyle
    {
      get
      {
        if (_darkBackgroundStyle == null)
        {
          _darkBackgroundStyle = new GUIStyle();
          _darkBackgroundStyle.normal.background = BonsaiPreferences.Texture("DarkTexture");
        }

        return _darkBackgroundStyle;
      }
    }

    private static GUIStyle _whiteTextStyle;
    public static GUIStyle WhiteTextStyle
    {
      get
      {
        if (_whiteTextStyle == null)
        {
          _whiteTextStyle = new GUIStyle();
          _whiteTextStyle.normal.textColor = Color.white;
        }

        return _whiteTextStyle;
      }
    }

    private static GUILayoutOption[] _deleteButtonSize;
    public static GUILayoutOption[] DeleteButtonSize
    {
      get
      {
        if (_deleteButtonSize == null)
        {
          _deleteButtonSize = new GUILayoutOption[] { GUILayout.Width(18f), GUILayout.Height(18f) };
        }

        return _deleteButtonSize;
      }
    }

    #endregion

    void OnEnable()
    {
      EditorApplication.update += Repaint;
    }

    void OnDisable()
    {
      EditorApplication.update -= Repaint;
    }

    public override void OnInspectorGUI()
    {
      // Get access to the blackboard.
      var tree = target as BehaviourTree;
      var bb = tree.Blackboard;

      EditorGUILayout.LabelField("Behaviour Tree: " + tree.name);
      EditorGUILayout.Space();

      _bShowBlackboardFoldout = EditorGUILayout.Foldout(_bShowBlackboardFoldout, "Blackboard", true);

      if (_bShowBlackboardFoldout)
      {

        // Indent the blackboard contents under the foldout.
        EditorGUILayout.BeginHorizontal();
        EditorGUILayout.Space();
        EditorGUILayout.BeginVertical();

        ShowBlackboardGUI(bb);

        EditorGUILayout.EndVertical();
        EditorGUILayout.EndHorizontal();
      }
    }

    private void ShowBlackboardGUI(Blackboard bb)
    {
      // Before any rendering is done, update the tree type selection (this removes/adds buttons).
      // If this step is not done here, it will cause a GUI Layout error.
      if (Event.current.type == EventType.Layout)
      {
        _typeSelectionInputTree.UpdateParentChildren();
      }

      AddVariableGUI(bb);

      EditorGUILayout.Space();
      EditorGUILayout.Space();

      if (bb.Count > 0)
      {
        EditorGUILayout.LabelField("Contents");
      }

      else
      {
        EditorGUILayout.LabelField("Blackboard is empty");
      }

      string keyToRemove = null;

      float originalLabelWidth = EditorGUIUtility.labelWidth;
      EditorGUIUtility.labelWidth = labelWidth;

      foreach (var kvp in bb.Memory)
      {

        EditorGUILayout.BeginVertical(DarkBackgroundStyle);
        EditorGUILayout.BeginHorizontal();

        string key = kvp.Key;
        string type = kvp.Value.GetValueType().Name;

        object valueObj = kvp.Value.GetValue();
        string value = valueObj == null ? "null" : valueObj.ToString();

        // Display the key in the Blackboard.
        EditorGUILayout.LabelField(key, WhiteTextStyle);
        EditorGUILayout.LabelField(type, WhiteTextStyle);

        // Mark the key to remove.
        if (GUILayout.Button("X", DeleteButtonSize))
        {
          keyToRemove = key;
        }

        EditorGUILayout.EndHorizontal();

        EditorGUILayout.LabelField("Value: " + value, WhiteTextStyle);

        EditorGUILayout.EndVertical();
        EditorGUILayout.Space();
      }

      // Handle key removal, if requested.
      if (keyToRemove != null)
      {
        bb.Remove(keyToRemove);
      }

      if (bb.Count > 1 && GUILayout.Button("Clear All"))
      {
        bool bResult = EditorUtility.DisplayDialog("Clear Blackboard", "Are you sure you want to delete all variables?", "Yes", "No");
        if (bResult)
        {
          bb.Clear();
        }
      }

      EditorGUIUtility.labelWidth = originalLabelWidth;
    }

    private void AddVariableToBlackboard(Blackboard bb)
    {
      Type typeInput = _typeSelectionInputTree.GetCompleteType();

      if (!string.IsNullOrEmpty(_keyNameSelection) && typeInput != null)
      {

        bb.Add(_keyNameSelection, typeInput);

        // Restart the type selection.
        // GetCompleteType() sets a node's type so MakeGenericType() fails
        // in subsequent calls.
        _typeSelectionInputTree = new TypeSelectionTree();
      }
    }

    // Handles adding variables to the blackboard.
    private void AddVariableGUI(Blackboard bb)
    {
      _bAddVariableFoldout = EditorGUILayout.Foldout(_bAddVariableFoldout, "Add Variable", true);

      if (_bAddVariableFoldout)
      {

        EditorGUILayout.BeginVertical(DarkBackgroundStyle);

        GetVariableInfo();

        if (GUILayout.Button("Add"))
        {
          AddVariableToBlackboard(bb);
        }

        EditorGUILayout.EndVertical();
      }
    }

    // Gets the key and type of the variable.
    private void GetVariableInfo()
    {
      string helpMsg = "To add a variable, enter a key name and specify its type from the drop box.";
      EditorGUILayout.HelpBox(helpMsg, MessageType.Info);

      GetKeyInput();
      GetTypeInput();
    }

    private void GetKeyInput()
    {
      // Display the label key along with the text field to enter a key name.
      EditorGUILayout.BeginHorizontal();
      EditorGUILayout.LabelField("Key", WhiteTextStyle, LabelSpacingOptions);

      // Display the key name that will be added to the Blackboard.
      _keyNameSelection = EditorGUILayout.TextField(_keyNameSelection);

      EditorGUILayout.EndHorizontal();
    }

    private void GetTypeInput()
    {
      // Display the label "type" along with the button drop down to select the type.
      EditorGUILayout.BeginHorizontal();
      EditorGUILayout.LabelField("Type", WhiteTextStyle, LabelSpacingOptions);
      EditorGUILayout.BeginVertical();

      _typeSelectionInputTree.OnGUI();

      EditorGUILayout.EndVertical();
      EditorGUILayout.EndHorizontal();
    }

    /// <summary>
    /// Hanldes displaying the GUI to construct any type including generics.
    /// </summary>
    private class TypeSelectionTree
    {
      private readonly TypeSelectionNode root = new TypeSelectionNode();
      private readonly List<TypeSelectionNode> parentsToClearChildren = new List<TypeSelectionNode>();
      private readonly List<TypeSelectionNode> parentsToSetChildren = new List<TypeSelectionNode>();

      /// <summary>
      /// Get the complete type definition from the entire tree.
      /// The result is the type definition of the root with all generic argument
      /// types fully defined.
      /// </summary>
      /// <returns></returns>
      public Type GetCompleteType()
      {
        try
        {
          return GetType(root).type;
        }

        catch (Exception e)
        {
          Debug.LogError(e.Message);
          return null;
        }
      }

      private TypeSelectionNode GetType(TypeSelectionNode node)
      {
        if (node.type != null && node.type.IsGenericType)
        {

          var types = new Type[node.children.Count];

          // Recurse on children until leaves (non-generic types) are reached.
          int i = 0;
          foreach (var child in node.children)
          {
            types[i++] = GetType(child).type;
          }

          node.type = node.type.MakeGenericType(types);
        }

        // Concrete type, all generic arguments defined by a non-generic type.
        // Ex) Dictionary<TKey, TValue> becomes Dictionary<int, string>
        return node;
      }

      public void OnGUI()
      {
        OnGUI(root, 0);
      }

      // Recursive drawer that idents children under its parent.
      private void OnGUI(TypeSelectionNode node, int depth)
      {
        // Indent based on the depth of the node.
        EditorGUILayout.BeginHorizontal();
        GUILayout.Space(kIndentationWidth * depth);

        node.OnGUI();

        EditorGUILayout.EndHorizontal();

        if (node.children.Count > 0)
        {

          depth += 1;

          EditorGUILayout.BeginVertical();

          foreach (TypeSelectionNode child in node.children)
          {

            OnGUI(child, depth);
          }

          EditorGUILayout.EndVertical();
        }
      }

      /// <summary>
      /// Adds and removes children types based on current types selected.
      /// </summary>
      public void UpdateParentChildren()
      {
        parentsToSetChildren.Clear();
        parentsToClearChildren.Clear();

        TreeIterator<TypeSelectionNode>.Traverse(root, MarkAdditionsAndRemovals);
        SetParentChildren();
        ClearParentChildren();
      }

      private void MarkAdditionsAndRemovals(TypeSelectionNode node)
      {
        Type t = node.type;

        if (t != null && t.IsGenericType && node.children.Count != t.GetGenericArguments().Length)
        {

          // Generic types needs to have children to define the generic type argument.
          parentsToSetChildren.Add(node);
        }

        else if ((t == null || !t.IsGenericType) && node.children.Count != 0)
        {

          // Not generic but has children means this node changed to a non-generic from a generic
          parentsToClearChildren.Add(node);
        }
      }

      private void SetParentChildren()
      {
        // Add children generic type args for generic type parents.
        foreach (TypeSelectionNode parent in parentsToSetChildren)
        {

          parent.children.Clear();

          foreach (var arg in parent.type.GetGenericArguments())
          {
            parent.children.Add(new TypeSelectionNode());
          }
        }
      }

      private void ClearParentChildren()
      {
        // Remove children for parents that became non-generic.
        foreach (TypeSelectionNode parent in parentsToClearChildren)
        {
          parent.children.Clear();
        }
      }

      private class TypeSelectionNode : IIterableNode<TypeSelectionNode>
      {
        public Type type = null;
        public List<TypeSelectionNode> children = new List<TypeSelectionNode>();
        int index = 0;

        public void OnGUI()
        {
          // Display the type selection menu.
          index = EditorGUILayout.Popup(index, Blackboard.registerTypeNames);
          type = Blackboard.registerTypes[index];
        }

        public TypeSelectionNode GetChildAt(int index)
        {
          return children[index];
        }

        public int ChildCount()
        {
          return children.Count;
        }
      }
    }
  }
}