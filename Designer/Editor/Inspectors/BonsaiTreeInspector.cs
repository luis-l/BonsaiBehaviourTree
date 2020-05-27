
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
                if (_labelSpacingOptions == null) {
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
                if (_darkBackgroundStyle == null) {
                    _darkBackgroundStyle = new GUIStyle();
                    _darkBackgroundStyle.normal.background = BonsaiResources.GetTexture("DarkGray");
                }

                return _darkBackgroundStyle;
            }
        }

        private static GUIStyle _whiteTextStyle;
        public static GUIStyle WhiteTextStyle
        {
            get
            {
                if (_whiteTextStyle == null) {
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
                if (_deleteButtonSize == null) {
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

            if (_bShowBlackboardFoldout) {

                // Indent the blackboard contents under the foldout.
                EditorGUILayout.BeginHorizontal();
                EditorGUILayout.Space();
                EditorGUILayout.BeginVertical();

                showBlackboardGUI(bb);

                EditorGUILayout.EndVertical();
                EditorGUILayout.EndHorizontal();
            }
        }

        private void showBlackboardGUI(Blackboard bb)
        {
            // Before any rendering is done, update the tree type selection (this removes/adds buttons).
            // If this step is not done here, it will cause a GUI Layout error.
            if (Event.current.type == EventType.Layout) {
                _typeSelectionInputTree.UpdateParentChildren();
            }

            addVariableGUI(bb);

            EditorGUILayout.Space();
            EditorGUILayout.Space();

            if (bb.Count > 0) {
                EditorGUILayout.LabelField("Contents");
            }

            else {
                EditorGUILayout.LabelField("Blackboard is empty");
            }

            string keyToRemove = null;

            float originalLabelWidth = EditorGUIUtility.labelWidth;
            EditorGUIUtility.labelWidth = labelWidth;

            foreach (var kvp in bb.Memory) {

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
                if (GUILayout.Button("X", DeleteButtonSize)) {
                    keyToRemove = key;
                }

                EditorGUILayout.EndHorizontal();

                EditorGUILayout.LabelField("Value: " + value, WhiteTextStyle);

                EditorGUILayout.EndVertical();
                EditorGUILayout.Space();
            }

            // Handle key removal, if requested.
            if (keyToRemove != null) {
                bb.Remove(keyToRemove);
            }

            if (bb.Count > 1 && GUILayout.Button("Clear All")) {
                bool bResult = EditorUtility.DisplayDialog("Clear Blackboard", "Are you sure you want to delete all variables?", "Yes", "No");
                if (bResult) {
                    bb.Clear();
                }
            }

            EditorGUIUtility.labelWidth = originalLabelWidth;
        }

        private void addVariableToBlackboard(Blackboard bb)
        {
            Type typeInput = _typeSelectionInputTree.GetCompleteType();

            if (!string.IsNullOrEmpty(_keyNameSelection) && typeInput != null) {

                bb.Add(_keyNameSelection, typeInput);

                // Restart the type selection.
                // GetCompleteType() sets a node's type so MakeGenericType() fails
                // in subsequent calls.
                _typeSelectionInputTree = new TypeSelectionTree();
            }
        }

        // Handles adding variables to the blackboard.
        private void addVariableGUI(Blackboard bb)
        {
            _bAddVariableFoldout = EditorGUILayout.Foldout(_bAddVariableFoldout, "Add Variable", true);

            if (_bAddVariableFoldout) {

                EditorGUILayout.BeginVertical(DarkBackgroundStyle);

                getVariableInfo();

                if (GUILayout.Button("Add")) {
                    addVariableToBlackboard(bb);
                }

                EditorGUILayout.EndVertical();
            }
        }

        // Gets the key and type of the variable.
        private void getVariableInfo()
        {
            string helpMsg = "To add a variable, enter a key name and specify its type from the drop box.";
            EditorGUILayout.HelpBox(helpMsg, MessageType.Info);

            getKeyInput();
            getTypeInput();
        }

        private void getKeyInput()
        {
            // Display the label key along with the text field to enter a key name.
            EditorGUILayout.BeginHorizontal();
            EditorGUILayout.LabelField("Key", WhiteTextStyle, LabelSpacingOptions);

            // Display the key name that will be added to the Blackboard.
            _keyNameSelection = EditorGUILayout.TextField(_keyNameSelection);

            EditorGUILayout.EndHorizontal();
        }

        private void getTypeInput()
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
            private TypeSelectionNode _root = new TypeSelectionNode();

            private List<TypeSelectionNode> _parentsToClearChildren = new List<TypeSelectionNode>();
            private List<TypeSelectionNode> _parentsToSetChildren = new List<TypeSelectionNode>();

            /// <summary>
            /// Get the complete type definition from the entire tree.
            /// The result is the type definition of the root with all generic argument
            /// types fully defined.
            /// </summary>
            /// <returns></returns>
            public Type GetCompleteType()
            {
                try {
                    return getType(_root).type;
                }

                catch (Exception e) {
                    Debug.LogError(e.Message);
                    return null;
                }
            }

            private TypeSelectionNode getType(TypeSelectionNode node)
            {
                if (node.type != null && node.type.IsGenericType) {

                    var types = new Type[node.children.Count];

                    // Recurse on children until leaves (non-generic types) are reached.
                    int i = 0;
                    foreach (var child in node.children) {
                        types[i++] = getType(child).type;
                    }

                    node.type = node.type.MakeGenericType(types);
                }

                // Concrete type, all generic arguments defined by a non-generic type.
                // Ex) Dictionary<TKey, TValue> becomes Dictionary<int, string>
                return node;
            }

            public void OnGUI()
            {
                onGUI(_root, 0);
            }

            // Recursive drawer that idents children under its parent.
            private void onGUI(TypeSelectionNode node, int depth)
            {
                // Indent based on the depth of the node.
                EditorGUILayout.BeginHorizontal();
                GUILayout.Space(kIndentationWidth * depth);

                node.OnGUI();

                EditorGUILayout.EndHorizontal();

                if (node.children.Count > 0) {

                    depth += 1;

                    EditorGUILayout.BeginVertical();

                    foreach (TypeSelectionNode child in node.children) {

                        onGUI(child, depth);
                    }

                    EditorGUILayout.EndVertical();
                }
            }

            /// <summary>
            /// Adds and removes children types based on current types selected.
            /// </summary>
            public void UpdateParentChildren()
            {
                _parentsToSetChildren.Clear();
                _parentsToClearChildren.Clear();

                TreeIterator<TypeSelectionNode>.Traverse(_root, markAdditionsAndRemovals);
                setParentChildren();
                clearParentChildren();
            }

            private void markAdditionsAndRemovals(TypeSelectionNode node)
            {
                Type t = node.type;

                if (t != null && t.IsGenericType && node.children.Count != t.GetGenericArguments().Length) {

                    // Generic types needs to have children to define the generic type argument.
                    _parentsToSetChildren.Add(node);
                }

                else if ((t == null || !t.IsGenericType) && node.children.Count != 0) {

                    // Not generic but has children means this node changed to a non-generic from a generic
                    _parentsToClearChildren.Add(node);
                }
            }

            private void setParentChildren()
            {
                // Add children generic type args for generic type parents.
                foreach (TypeSelectionNode parent in _parentsToSetChildren) {

                    parent.children.Clear();

                    foreach (var arg in parent.type.GetGenericArguments()) {
                        parent.children.Add(new TypeSelectionNode());
                    }
                }
            }

            private void clearParentChildren()
            {
                // Remove children for parents that became non-generic.
                foreach (TypeSelectionNode parent in _parentsToClearChildren) {
                    parent.children.Clear();
                }
            }

            private class TypeSelectionNode : TreeIterator<TypeSelectionNode>.IterableNode
            {
                public Type type = null;
                public List<TypeSelectionNode> children = new List<TypeSelectionNode>();

                public void OnGUI()
                {
                    string name = "None";
                    if (type != null) {
                        name = type.Name;
                    }

                    float size = new GUIStyle().CalcSize(new GUIContent(name)).x + 20f;
                    var opt = new GUILayoutOption[] { GUILayout.Width(size) };

                    // Display the type selection menu.
                    if (EditorGUILayout.DropdownButton(new GUIContent(name), FocusType.Keyboard, opt)) {

                        // var selectTypeTab = new Tab<Type>(

                        //     getValues: () => Blackboard.registerTypes,
                        //     getCurrent: () => type,
                        //     setTarget: (t) => { type = t; },
                        //     getValueName: (t) => t.GetNiceName(),
                        //     title: "Select Type"
                        // );

                        // SelectionWindow.Show(selectTypeTab);
                    }
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