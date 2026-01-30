using Godot;
using System;
using System.Reflection;

/// <summary>
/// Store settings for menu
///
/// Any settings should be marked as SettingAttribute
/// </summary>
public static partial class Settings {
	public static string FileName = "user://settings.cfg";

	static Settings() {
		Register();
		Load();
	}

	/// <summary>
	/// Attribute to mark properties for automatic settings serialization
	///
	/// NOTE: SettingGenerator creates an enum listing for the property marked with this
	/// NOTE: Can only be used on static observable properties
	/// NOTE: There exist a dynamically generated class of this that contains SetValues for types
	/// NOTE: https://github.com/dotnet/roslyn/issues/57239
	///        this is why there will be no usefully exported strings/enums for inspector view, just copy/paste the refs
	/// </summary>
	[AttributeUsage(AttributeTargets.Property)]
	private class SettingAttribute : Attribute {
		public string Section { get; }
		public object DefaultValue { get; }

		public SettingAttribute(string section, object defaultValue = null) {
			Section = section;
			DefaultValue = defaultValue;
		}
	}

	/// <summary>
	/// Register observables to trigger necessary functions on changes
	/// </summary>
	public static void Register() {
		var properties = typeof(Settings).GetProperties(BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Static);
		var methods = typeof(Settings).GetMethods(BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Static);

		foreach (var property in properties) {
			var settingAttr = property.GetCustomAttribute<SettingAttribute>();
			if (settingAttr != null && property.CanWrite) {
				var observable = property.GetValue(null);
				var valueProp = observable.GetType().GetProperty("Value");
				var defaultValue = settingAttr.DefaultValue ?? valueProp.GetValue(observable);

				// Register values with generated class variable
				Values.Add(property.Name, property);

				// Register handlers
				foreach (var method in methods) {
					var attribute = method.GetCustomAttribute<ObservableHandlerAttribute>();
					if (attribute != null) {
						method.Invoke(null, [property]);
					}
				}
			}
		}
	}

	/// <summary>
	/// Save settings file
	/// </summary>
	public static void Save() {
		var config = new ConfigFile();

		var properties = typeof(Settings).GetProperties(BindingFlags.Public | BindingFlags.Static);

		// Iterate through all properties and set config values
		foreach (var property in properties) {
			var settingAttr = property.GetCustomAttribute<SettingAttribute>();
			if (settingAttr != null) {
				var observable = property.GetValue(null);
				var valueProp = observable.GetType().GetProperty("Value");
				var value = valueProp.GetValue(observable);
				config.SetValue(settingAttr.Section, property.Name, ConvertTypeToVariant(value));
			}
		}

		Error err = config.Save(FileName);
		if (err != Error.Ok) {
			GD.PrintErr(err);
		}
	}

	/// <summary>
	/// Load settings file
	/// </summary>
	public static void Load() {
		var config = new ConfigFile();

		// Load data from a file.
		Error err = config.Load(FileName);

		// If loading failed, save defaults out to file
		if (err != Error.Ok) {
			Save();
		}

		// Iterate through all properties and update values
		var properties = typeof(Settings).GetProperties(BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Static);
		foreach (var property in properties) {
			var settingAttr = property.GetCustomAttribute<SettingAttribute>();
			if (settingAttr != null && property.CanWrite) {
				var observable = property.GetValue(null);
				var valueProp = observable.GetType().GetProperty("Value");
				var defaultValue = valueProp.GetValue(observable) ?? settingAttr.DefaultValue;
				// GD.Print("Load ", property.Name, " ", ConvertTypeToVariant(defaultValue));
				var value = config.GetValue(settingAttr.Section, property.Name, ConvertTypeToVariant(defaultValue));
				valueProp.SetValue(observable, ConvertVariantToType(value, valueProp.PropertyType));
			}
		}
	}

	/// <summary>
	/// Convert variable back to common types
	/// </summary>
	private static object ConvertVariantToType(Variant variant, Type targetType) {
		return targetType switch {
			Type t when t == typeof(bool) => variant.AsBool(),
			Type t when t == typeof(int) => variant.AsInt32(),
			Type t when t == typeof(long) => variant.AsInt64(),
			Type t when t == typeof(float) => variant.AsSingle(),
			Type t when t == typeof(double) => variant.AsDouble(),
			Type t when t == typeof(string) => variant.AsString(),
			Type t when t == typeof(Vector2) => variant.AsVector2(),
			Type t when t == typeof(Vector3) => variant.AsVector3(),
			Type t when t == typeof(Color) => variant.AsColor(),
			_ => variant.AsGodotObject()
		};
	}

	/// <summary>
	/// Convert common/Godot types to variant
	/// </summary>
	private static Variant ConvertTypeToVariant(object value) {
		if (value == null) return default;

		return value switch {
			bool b => Variant.From(b),
			int i => Variant.From(i),
			long l => Variant.From(l),
			float f => Variant.From(f),
			double d => Variant.From(d),
			string s => Variant.From(s),
			Vector2 v2 => Variant.From(v2),
			Vector3 v3 => Variant.From(v3),
			Vector4 v4 => Variant.From(v4),
			Color c => Variant.From(c),
			Rect2 r2 => Variant.From(r2),
			Transform2D t2d => Variant.From(t2d),
			Transform3D t3d => Variant.From(t3d),
			Quaternion q => Variant.From(q),
			Plane p => Variant.From(p),
			Aabb aabb => Variant.From(aabb),
			Basis basis => Variant.From(basis),
			_ => Variant.From(value)
		};
	}
}
