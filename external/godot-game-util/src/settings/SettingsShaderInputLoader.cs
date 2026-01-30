using Godot;
using System;
using System.Collections.Generic;
using System.Reflection;

public static partial class Settings {
  /// <summary>
  /// Attribute to mark property with custom Keymap load function
  /// </summary>
  [AttributeUsage(AttributeTargets.Property)]
  private class KeymapLoadAttribute : Attribute {
    public string Parameter { get; }

    public KeymapLoadAttribute(string param) {
      Parameter = param;
    }
  }

  public readonly static Dictionary<string, Observable<string>> BindByInput = [];
  public readonly static Dictionary<string, Observable<string>> BindBySetting = [];

  [ObservableHandler]
  public static void RegisterKeymapLoad(PropertyInfo property) {
    var keymapLoadAttr = property.GetCustomAttribute<KeymapLoadAttribute>();
    if (keymapLoadAttr != null) {
      if (property.GetValue(null) is Observable<string> observable) {
        var mappings = Godot.InputMap.ActionGetEvents(keymapLoadAttr.Parameter);
        observable.Value = BindingUtil.FromInputEvents(mappings);
        BindByInput.Add(keymapLoadAttr.Parameter, observable);
        BindBySetting.Add(property.Name, observable);
        observable.Subscribe((newValue) => {
          HandleKeymapLoadAttribute(ConvertTypeToVariant(newValue), keymapLoadAttr.Parameter);
        });
      }
    }
  }

  private static void HandleKeymapLoadAttribute(Variant value, string param) {
    Godot.InputMap.ActionEraseEvents(param);

    if (value.AsString() == "")
      return;

    var mapping = BindingUtil.FromSequence(value.AsString());
    Godot.InputMap.ActionAddEvent(param, mapping);
  }
}
