
using System;
using UnityEngine;

namespace Bonsai.Designer
{
  [AttributeUsage(AttributeTargets.Class)]
  public class NodeEditorPropertiesAttribute : Attribute
  {
    public readonly string menuPath, textureName;

    public NodeEditorPropertiesAttribute(string menuPath, string texturePath)
    {
      this.menuPath = menuPath;
      this.textureName = texturePath;
    }
  }
}