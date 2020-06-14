
using System;
using System.Collections.Generic;

namespace Bonsai.Utility
{
  public static class TypeExtensions
  {

    public static readonly Dictionary<string, string> SimpleTypeNames = new Dictionary<string, string>()
    {
      { "Boolean", "bool"},
      { "Int32", "int"},
      { "Single", "float"},
      { "Double", "double"},
      { "String", "string"},
    };

    /// <summary>
    /// Get the simplified, alternative name.
    /// </summary>
    public static string SimplifiedName(Type type)
    {
      string name = type.Name;

      if (name == "Object")
      {
        name = typeof(UnityEngine.Object) == type ? "UnityObject" : "object";
      }
      else
      {
        if (SimpleTypeNames.TryGetValue(name, out string simple))
          name = simple;
      }

      return name;
    }

    /// <summary>
    /// Human-readable type name.
    /// </summary>
    public static string NiceName(Type type)
    {
      string name = SimplifiedName(type);

      // Trim off any generic type naming.
      int genericIndex = name.IndexOf('`');
      if (genericIndex >= 0)
      {
        name = name.Substring(0, genericIndex);
      }

      return name;
    }
  }
}
