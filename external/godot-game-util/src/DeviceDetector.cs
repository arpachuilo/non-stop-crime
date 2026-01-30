using System;
using System.Collections.Generic;
using Godot;

/// <summary>
/// Helper class to detect the current input device
///
/// Contains helper method `IconFor` to get input icon for current device
///
/// The path is created from as follows
/// "BaseTexturePath}/{INPUT_ACTION}/{(KBM|Switch|Playstation|Xbox)TextureFilename}"
/// </summary>
public partial class DeviceDetector : Node {
  [Export]
  private string BaseTexturePath { get; set; } = "res://textures/input/actions";

  [Export]
  private string KBMTextureFilename { get; set; } = "kbm.png";

  [Export]
  private string SwitchTextureFilename { get; set; } = "switch.png";

  [Export]
  private string PlaystationTextureFilename { get; set; } = "playstation.png";

  [Export]
  private string XboxTextureFilename { get; set; } = "xbox.png";

  public static DeviceDetector Instance;

  public override void _Ready() {
    Instance = this;
    _device = GuessDeviceName();
    OnDeviceChanged?.Invoke(_device);
  }

  /// <summary>
  /// Delegates to run when device changes
  /// </summary>
  public static Action<Device> OnDeviceChanged = delegate { };

  /// <summary>
  /// Support input devices
  /// </summary>
  public enum Device {
    Keyboard,
    Xbox,
    Steam,
    PlayStation,
    Switch,
    Generic,
  }

  private Device _device;

  public static Device GetDevice() {
    if (Instance == null) {
      return Device.Keyboard;
    }

    return Instance._device;
  }

  private int _deviceIndex;
  private int _deviceLastChangedAt;

  private float _mouseMotionThreshold = 0.1f;
  private float _deadzone = 0.1f;

  public override void _Input(InputEvent @event) {
    Device nextDevice = _device;
    int nextDeviceIndex = _deviceIndex;

    // Did we just press a key on the keyboard or move the mouse?
    if (
        @event is InputEventKey
        || @event is InputEventMouseButton
        || (
            @event is InputEventMouseMotion motionEvent
            && motionEvent.Relative.LengthSquared() > _mouseMotionThreshold
        )
    ) {
      nextDevice = Device.Keyboard;
      nextDeviceIndex = -1;
    }
    // Did we just use a joypad?
    else if (
        @event is InputEventJoypadButton
        || (
            @event is InputEventJoypadMotion joypadMotionEvent
            && Math.Abs(joypadMotionEvent.AxisValue) > _deadzone
        )
    ) {
      nextDevice = GetSimplifiedDeviceName(Input.GetJoyName(@event.Device));
      nextDeviceIndex = @event.Device;
    }

    // Debounce changes for 1 second because some joypads register twice in Windows for some reason
    bool notChangedInLastSecond =
        Engine.GetFramesDrawn() - _deviceLastChangedAt > Engine.GetFramesPerSecond();
    if ((nextDevice != _device || nextDeviceIndex != _deviceIndex) && notChangedInLastSecond) {
      _deviceLastChangedAt = Engine.GetFramesDrawn();
      _device = nextDevice;
      _deviceIndex = nextDeviceIndex;
      OnDeviceChanged?.Invoke(_device);
    }
  }

  private readonly Dictionary<Device, List<string>> _keywords = new() {
    {
      Device.Xbox,
      new List<string> { "XInput", "XBox" }
    },
    {
      Device.PlayStation,
      new List<string> { "Sony", "PS5", "PS4", "Nacon" }
    },
    {
      Device.Steam,
      new List<string> { "Steam" }
    },
    {
      Device.Switch,
      new List<string> { "Switch", "Joy-Con (L)", "Joy-Con (R)" }
    },
  };

  private Device GetSimplifiedDeviceName(string rawName) {
    foreach (var deviceKey in _keywords.Keys) {
      foreach (var keyword in _keywords[deviceKey]) {
        if (rawName.ToLower().Contains(keyword.ToLower())) {
          return deviceKey;
        }
      }
    }

    return Device.Generic;
  }

  public static bool HasJoypad() {
    return Input.GetConnectedJoypads().Count > 0;
  }

  public static Device GuessDeviceName() {
    if (HasJoypad() && Instance != null) {
      return Instance.GetSimplifiedDeviceName(Input.GetJoyName(0));
    } else {
      return Device.Keyboard;
    }
  }

  /// <summary>
  /// Get the path to the input texture.
  /// </summary>
  /// <param name="inputMapAction">The input action to get the texture for.</param>
  /// <param name="device">The device to get the texture for.</param>
  /// <returns>The path to the input texture.</returns>
  public static Texture2D IconFor(string inputMapAction, Device device) {
    var fileName = device switch {
      Device.Keyboard => "kbm",
      Device.Switch => "switch",
      Device.PlayStation => "playstation",
      _ => "xbox",
    };


    var path = $"{Instance.BaseTexturePath}/{inputMapAction}/{fileName}";

    // check if path is valid and use Resource Loader which uses caching
    if (ResourceLoader.Exists(path))
      return ResourceLoader.Load(path) as Texture2D;
    else
      GD.PrintErr($"InputTexture.Get: Texture not found at path: {path}");

    return null;
  }
}
