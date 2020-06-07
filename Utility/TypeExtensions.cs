
using System;
using System.Collections.Generic;
using System.Runtime.InteropServices.WindowsRuntime;

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
  public static string SimplifiedName(this Type type)
  {
    string typeName = type.Name;

    if (typeName == "Object")
    {
      typeName = typeof(UnityEngine.Object) == type ? "UnityObject" : "object";
    }
    else
    {
      if (SimpleTypeNames.TryGetValue(typeName, out string altTypeName))
        typeName = altTypeName;
    }

    return typeName;
  }

  /// <summary>
  /// Human-readable type name.
  /// </summary>
  public static string NiceName(this Type type)
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
