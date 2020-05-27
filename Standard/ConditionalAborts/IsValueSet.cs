
using System;
using UnityEngine;

using Bonsai.Core;
using Bonsai.Designer;

namespace Bonsai.Standard
{
    /// <summary>
    /// Tests if the value at a given key is not set to its default value.
    /// </summary>
    [NodeEditorProperties("Conditional/", "Condition")]
    public class IsValueSet : ConditionalAbort
    {
        [Tooltip("The key of the value to test if it's not set to its default value.")]
        public string key;

        // Cache the register at the blackboard key.
        private Blackboard.RegisterBase _registerOfKey;

        // Cache the default value for the value at the key.
        // This should not be done repeatedly for performance reasons.
        private object _defaultValue;

        // Cache if the value at the key is a value type.
        // If it is a reference type then the default value is null.
        private bool _bIsValueType;

        public override void OnStart()
        {
            CacheDefaultValue();
        }

        public override bool Condition()
        {
            // No register was cached.
            // This happenes when the key does not exist.
            if (_registerOfKey == null) {
                return false;
            }

            object value = _registerOfKey.GetValue();

            // Value types must be compared with the default value type.
            // All the reference types have a default value of null.
            return _bIsValueType ? !value.Equals(_defaultValue) : value != null;
        }

        /// <summary>
        /// Use this everytime you change the key.
        /// </summary>
        public void CacheDefaultValue()
        {
            // Reset values.
            _registerOfKey = null;
            _bIsValueType = false;
            _defaultValue = null;

            if (Blackboard.Exists(key)) {

                _registerOfKey = Blackboard.GetRegister(key);

                Type t = _registerOfKey.GetValueType();

                if (t.IsValueType) {
                    _bIsValueType = true;
                    _defaultValue = Activator.CreateInstance(t);
                }
            }
        }
    }
}