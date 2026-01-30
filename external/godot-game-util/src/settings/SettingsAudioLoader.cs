using Godot;
using System;
using System.Collections.Generic;
using System.Reflection;

public static partial class Settings {
  /// <summary>
  /// Attribute to mark property with custom audio load function
  /// </summary>
  [AttributeUsage(AttributeTargets.Property)]
  private class VolumeLoadAttribute : Attribute {
    public string Bus { get; }

    public VolumeLoadAttribute(string bus) {
      Bus = bus;
    }
  }

  public readonly static Dictionary<string, Observable<float>> BusSettingByPropertyName = [];
  public readonly static Dictionary<Observable<float>, string> BusNameBySetting = [];

  [ObservableHandler]
  private static void RegisterVolumeLoad(PropertyInfo property) {
    var volumeLoadAttr = property.GetCustomAttribute<VolumeLoadAttribute>();
    if (volumeLoadAttr != null) {
      if (property.GetValue(null) is Observable<float> observable) {
        BusSettingByPropertyName.Add(property.Name, observable);
        BusNameBySetting.Add(observable, volumeLoadAttr.Bus);
        observable.Subscribe((newValue) => {
          HandleVolumeAttribute((float)newValue, volumeLoadAttr.Bus);
        });
      }
    }
  }

  private static void HandleVolumeAttribute(float volume, string busName) {
    var bus = AudioServer.GetBusIndex(busName);
    if (bus < 0) {
      GD.PrintErr($"Audio bus '{busName}' not found");
      return;
    }

    AudioServer.SetBusVolumeDb(bus, Mathf.LinearToDb(volume));
  }
}
