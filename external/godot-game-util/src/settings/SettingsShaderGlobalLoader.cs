using Godot;
using System;
using System.Reflection;

public static partial class Settings {
  /// <summary>
  /// Attribute to mark property with custom shader load function
  /// </summary>
  [AttributeUsage(AttributeTargets.Property)]
  public class ShaderLoadAttribute : Attribute {
    public string Parameter { get; }

    public ShaderLoadAttribute(string param) {
      Parameter = param;
    }
  }

  [ObservableHandler]
  public static void RegisterShaderLoad(PropertyInfo property) {
    var shaderLoadAttr = property.GetCustomAttribute<ShaderLoadAttribute>();
    if (shaderLoadAttr != null) {
      if (property.GetValue(null) is IObservable observable) {
        observable.Subscribe((newValue) => {
          HandleShaderLoadAttribute(ConvertTypeToVariant(newValue), shaderLoadAttr.Parameter);
        });
      }
    }
  }

  private static void HandleShaderLoadAttribute(Variant value, string param) {
    RenderingServer.GlobalShaderParameterSet(param, value);
  }
}
