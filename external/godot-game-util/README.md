# Godot Game Util

A collection of utilities, source generators, and analyzers to accelerate Godot C# game development.

**NOTE:** Readme largely clanker generated

## Features

- **Source Generators**: Auto-generate code for common Godot patterns
- **Code Analyzers**: Compile-time validation for attributes and patterns
- **Settings System**: Type-safe, observable settings with automatic serialization
- **Vector Swizzling**: GLSL-like-style vector component manipulation
- **Observable Pattern**: Reactive property system with change notifications
- **Utility Classes**: Helper functions for common game development tasks (bit manipulation, 3D operations, random selection, etc.)

## Installation

### Adding as a Git Subtree

Add this repository to your project as a git subtree in an `external` directory:

```bash
// subtree
git remote add submodule-remote git@github.com:arpachuilo/godot-game-util.git
git fetch submodule-remote
git subtree add --prefix=external/godot-game-util submodule-remote --squash

// to update subtree
git subtree pull --prefix=external/godot-game-util submodule-remote main --squash
```

### Project Configuration

Add the following to your `.csproj` file:

```xml
<Project Sdk="Godot.NET.Sdk/4.5.1">
  <PropertyGroup>
    <TargetFramework>net8.0</TargetFramework>
    <EnableDynamicLoading>true</EnableDynamicLoading>
    <RootNamespace>YourNamespace</RootNamespace>
  </PropertyGroup>

  <ItemGroup>
    <!-- Required: Add project.godot as additional file for generators -->
    <AdditionalFiles Include="project.godot" />
  </ItemGroup>

  <ItemGroup>
    <!-- Exclude generator/analyzer source files from compilation -->
    <Compile Remove="external/godot-game-util/_Generators\**\*.cs" />
    <Compile Remove="external/godot-game-util/_Analyzers\**\*.cs" />
  </ItemGroup>

  <ItemGroup>
    <!-- Reference generators and analyzers -->
    <ProjectReference Include="external/godot-game-util/_Generators\Generators.csproj" 
                      OutputItemType="Analyzer" />
    <ProjectReference Include="external/godot-game-util/_Analyzers\Analyzers.csproj" 
                      OutputItemType="Analyzer" 
                      ReferenceOutputAssembly="false" />
  </ItemGroup>
</Project>
```

## Source Generators

### GodotProjectGenerator

Automatically generates type-safe constants from your `project.godot` file.

**What it generates:**
- `Layer` - Physics layer constants from `[layer_names]` section
- `Group` - Node group constants from `[global_group]` section  
- `InputActions` - Input action constants from `[input]` section

**Example `project.godot`:**
```ini
[layer_names]
3d_physics/layer_1="Player"
3d_physics/layer_2="Enemy"

[global_group]
PlayerGroup=""
EnemyGroup=""

[input]
move_forward={ ... }
jump={ ... }
```

**Generated code:**
```csharp
public static class Layer
{
    public const int Player = 1;
    public const int Enemy = 2;
}

public static class Group
{
    public const string PlayerGroup = "PlayerGroup";
    public const string EnemyGroup = "EnemyGroup";
}

public static class InputActions
{
    public const string move_forward = "move_forward";
    public const string jump = "jump";
}
```

**Usage:**
```csharp
// Check collision layers
if (body.CollisionLayer == Layer.Player) { }

// Check node groups
if (node.IsInGroup(Group.PlayerGroup)) { }

// Check input actions
if (Input.IsActionPressed(InputActions.move_forward)) { }
```

### GodotVectorSwizzleGenerator

Generates GLSL-style vector swizzling extension methods for `Vector2` and `Vector3`.

**Features:**
- All standard component swizzles (X, Y, Z)
- Constant swizzles (0, 1)
- Parameter swizzles (N) for runtime values
- Supports Vector2 ↔ Vector3 conversions

**Examples:**
```csharp
Vector3 v = new Vector3(1, 2, 3);

// Component swizzles
Vector2 xy = v._XY();     // (1, 2)
Vector3 zyx = v._ZYX();   // (3, 2, 1)

// Constant swizzles
Vector3 v2 = v._XY0();    // (1, 2, 0)
Vector3 v3 = v._X1Z();    // (1, 1, 3)

// Parameter swizzles
Vector3 v4 = v._XYN(5f);  // (1, 2, 5)
Vector3 v5 = v._NNN(7f, 7f, 7f);  // (7, 7, 7)
```

### SettingsGenerator

Generates type-safe getter/setter methods and reference constants for all properties marked with `[Setting]` attribute.

**What it generates:**
- Type-specific setter methods: `SetFloatValue(name, value)`, `SetBoolValue(name, value)`, etc.
- Type-specific getter methods: `GetFloatValue(name)`, `GetBoolValue(name)`, etc.
- Reference string constants grouped by type: `FloatSettingsReference`, `BoolSettingsReference`, etc.

**Example usage in Settings.cs:**
```csharp
public static partial class Settings {
    [Setting("Audio", 1.0f)]
    [VolumeLoad("Master")]
    public static Observable<float> MasterVolume { get; set; } = 1.0f;

    [Setting("Graphics", 1.0f)]
    [ShaderLoad("Gamma")]
    public static Observable<float> Gamma { get; set; } = 1.0f;

    [Setting("Graphics", false)]
    public static Observable<bool> VSync { get; set; } = false;
}
```

**Generated methods:**
```csharp
// Auto-generated
Settings.SetFloatValue("MasterVolume", 0.5f);
float volume = Settings.GetFloatValue("MasterVolume");

// Reference constants
// FloatSettingsReference = "MasterVolume,Gamma,..."
// BoolSettingsReference = "VSync,..."
```

## Code Analyzers

### ObservableHandlerAnalyzer (OBSERVABLE001)

Validates that methods marked with `[ObservableHandler]` have the correct signature.

**Required signature:**
```csharp
[ObservableHandler]
private static void MethodName(PropertyInfo property) { }
```

**Enforces:**
- Method must be static
- Method must return void
- Method must accept exactly one parameter of type `System.Reflection.PropertyInfo`

### SettingAttributeAnalyzer (SETTING001)

Ensures `[Setting]` attributes are only used on valid properties.

**Valid usage:**
```csharp
[Setting("Section", defaultValue)]
public static Observable<T> PropertyName { get; set; }
```

**Enforces:**
- Property must be of type `Observable<T>`
- Property must be static

### VolumeLoadAttributeAnalyzer (VOLUME001)

Validates that `[VolumeLoad]` attributes are used correctly for audio settings.

**Valid usage:**
```csharp
[VolumeLoad("Master")]
public static Observable<float> Volume { get; set; }
```

**Enforces:**
- Property must be of type `Observable<float>` (not any other numeric type)

## Settings System

The Settings system provides automatic serialization/deserialization of game settings with reactive updates.

### Core Components

**Observable&lt;T&gt;** - Generic reactive property container
```csharp
public class Observable<T> where T : IEquatable<T> {
    public T Value { get; set; }
    public event OnChangeHandler OnChange;
    public void Subscribe(Action<object> handler);
}
```

**Settings** - Static class that manages all settings
- `Settings.Save()` - Saves to ConfigFile (default: `user://settings.cfg`)
- `Settings.Load()` - Loads from ConfigFile
- `Settings.Register()` - Registers all observable handlers (called automatically)

### Defining Settings

```csharp
public static partial class Settings {
    [Setting("Audio", 1.0f)]
    public static Observable<float> MasterVolume { get; set; } = 1.0f;

    [Setting("Graphics", true)]
    public static Observable<bool> Fullscreen { get; set; } = true;

    [Setting("Gameplay", 5)]
    public static Observable<int> Difficulty { get; set; } = 5;
}
```

### Accessing Settings

```csharp
// Direct access
float volume = Settings.MasterVolume.Value;
Settings.MasterVolume.Value = 0.8f;

// Subscribe to changes
Settings.MasterVolume.OnChange += (newValue) => {
    GD.Print($"Volume changed to {newValue}");
};

// Persistence
Settings.Save();  // Saves to user://settings.cfg
Settings.Load();  // Loads from user://settings.cfg
```

### Built-in Loaders

#### VolumeLoadAttribute
Automatically syncs Observable&lt;float&gt; settings to AudioServer buses.

```csharp
[Setting("Audio", 1.0f)]
[VolumeLoad("Master")]  // Syncs to "Master" audio bus
public static Observable<float> MasterVolume { get; set; } = 1.0f;

[VolumeLoad("Music")]
public static Observable<float> MusicVolume { get; set; } = 1.0f;
```

#### ShaderLoadAttribute
Automatically syncs settings to global shader parameters.

```csharp
[Setting("Graphics", 1.0f)]
[ShaderLoad("time_scale")]  // Syncs to global shader parameter
public static Observable<float> TimeScale { get; set; } = 1.0f;
```

#### KeymapLoadAttribute
Automatically syncs input bindings to InputActions.

```csharp
[Setting("Controls", "")]
[KeymapLoad("move_forward")]  // Syncs to InputActions action
public static Observable<string> ForwardKey { get; set; } = "";

// Access binding dictionaries
Observable<string> binding = Settings.BindByInput["move_forward"];
Observable<string> binding2 = Settings.BindBySetting["ForwardKey"];
```

### Creating Custom Loaders

Create custom observable handlers to react to setting changes:

```csharp
public static partial class Settings {
    [AttributeUsage(AttributeTargets.Property)]
    private class MyCustomAttribute : Attribute {
        public string Parameter { get; }
        public MyCustomAttribute(string param) => Parameter = param;
    }

    [ObservableHandler]
    private static void RegisterMyCustom(PropertyInfo property) {
        var attr = property.GetCustomAttribute<MyCustomAttribute>();
        if (attr != null && property.GetValue(null) is IObservable observable) {
            observable.Subscribe((newValue) => {
                // Handle the value change
                GD.Print($"Setting changed: {attr.Parameter} = {newValue}");
            });
        }
    }

    [Setting("MySection", "default")]
    [MyCustom("my_parameter")]
    public static Observable<string> MySetting { get; set; } = "default";
}
```

## Utility Classes

The library includes several utility classes for common game development tasks:

- **BindingUtil** - Input event serialization and deserialization
- **BitUtil** - Bit manipulation utilities
- **DeviceDetector** - Input device detection and icon management
- **Node3DUtil** - Helper functions for 3D node operations
- **Poller** - Interval-based action execution helper
- **RandomUtil** - Random number and selection utilities
- **Vector3DUtil** - Extended 3D vector operations

### DeviceDetector

A singleton node that automatically detects the current input device (keyboard, gamepad types) and provides device-specific input icons.

**Features:**
- Automatic device detection from input events
- Support for Keyboard/Mouse, Xbox, PlayStation, Switch, Steam, and Generic controllers
- Device change callbacks
- Input icon management with customizable texture paths

**Setup:**
Add the DeviceDetector node to your scene tree (usually in an autoload):

```csharp
// Access the singleton instance
Device currentDevice = DeviceDetector.GetDevice();

// Subscribe to device changes
DeviceDetector.OnDeviceChanged += (device) => {
    GD.Print($"Input device changed to: {device}");
    UpdateUIForDevice(device);
};

// Get input icons for actions
Texture2D jumpIcon = DeviceDetector.IconFor("jump", DeviceDetector.GetDevice());
```

**Supported Devices:**
```csharp
public enum Device {
    Keyboard,   // Keyboard and mouse
    Xbox,       // Xbox controllers
    Steam,      // Steam Deck/Controller
    PlayStation,// PlayStation controllers
    Switch,     // Nintendo Switch controllers
    Generic,    // Other controllers
}
```

**Configuration:**
The DeviceDetector exposes several exported properties for customizing icon paths:
- `BaseTexturePath` - Base directory for input icons (default: `res://textures/input/actions`)
- `KBMTextureFilename` - Keyboard/mouse icon filename (default: `kbm.png`)
- `SwitchTextureFilename` - Switch icon filename (default: `switch.png`)
- `PlaystationTextureFilename` - PlayStation icon filename (default: `playstation.png`)
- `XboxTextureFilename` - Xbox icon filename (default: `xbox.png`)

**Icon Path Structure:**
Icons should be organized as: `{BaseTexturePath}/{INPUT_ACTION}/{DEVICE_FILENAME}`

Example: `res://textures/input/actions/jump/xbox.png`

### Poller

A utility class for executing actions at regular time intervals without requiring a dedicated Node or timer.

**Features:**
- Lightweight interval-based polling
- Frame-rate independent timing
- Simple API for periodic action execution
- No need for Timer nodes or signals

**Usage:**
```csharp
// Create a poller with 2-second interval
Poller healthRegenPoller = new Poller(2.0f);

// In your _Process or _PhysicsProcess method
public override void _Process(double delta) {
    // Execute the action every 2 seconds
    healthRegenPoller.Poll(() => {
        health += 5;
        GD.Print("Health regenerated!");
    });
}

// Adjust interval dynamically
healthRegenPoller.Interval = 1.5f;
```

**Example - Multiple Pollers:**
```csharp
private Poller _enemySpawner = new Poller(5.0f);
private Poller _resourceCollector = new Poller(1.0f);

public override void _Process(double delta) {
    _enemySpawner.Poll(() => SpawnEnemy());
    _resourceCollector.Poll(() => CollectResources());
}
```

## License

UNLICENSED
