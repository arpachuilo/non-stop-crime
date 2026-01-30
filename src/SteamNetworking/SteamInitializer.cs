using Godot;
using System;
using System.IO;
using System.Runtime.InteropServices;

/// <summary>
/// Handles initialization and loading of the native Steam API library.
/// Must be called before any Steamworks functionality is used.
/// </summary>
/// <remarks>
/// The Steamworks SDK requires loading a platform-specific native library:
/// - Windows: steam_api64.dll
/// - macOS: libsteam_api.dylib
/// - Linux: libsteam_api.so
///
/// This class registers a custom DLL resolver to locate these libraries in various
/// common locations, since .NET's default resolver may not find them in Godot's
/// export folders.
/// </remarks>
public static class SteamInitializer {
  /// <summary>
  /// Registers the custom Steam library resolver. Must be called before SteamClient.Init().
  /// </summary>
  /// <remarks>
  /// IMPORTANT: This MUST be called before any other Steam API calls, otherwise
  /// the default .NET resolver will fail to find the library and throw a DllNotFoundException.
  /// </remarks>
  public static void Init() {
    NativeLibrary.SetDllImportResolver(typeof(Steamworks.SteamClient).Assembly, ResolveSteamLibrary);
  }

  /// <summary>
  /// Custom DLL resolver that locates the Steam API native library across multiple search paths.
  /// </summary>
  /// <param name="libraryName">The library name requested by the P/Invoke call</param>
  /// <param name="assembly">The assembly requesting the library</param>
  /// <param name="searchPath">Optional search path hint (unused)</param>
  /// <returns>A handle to the loaded library, or IntPtr.Zero if not found</returns>
  private static IntPtr ResolveSteamLibrary(string libraryName, System.Reflection.Assembly assembly, DllImportSearchPath? searchPath) {
    if (libraryName != "libsteam_api" && libraryName != "steam_api64")
      return IntPtr.Zero;

    string libName = OperatingSystem.IsWindows() ? "steam_api64.dll" :
                    OperatingSystem.IsMacOS() ? "libsteam_api.dylib" : "libsteam_api.so";


    // Try multiple paths
    string[] searchPaths = [
            OS.GetExecutablePath().GetBaseDir(),
            ProjectSettings.GlobalizePath("res://"),
            AppContext.BaseDirectory,
            System.Environment.CurrentDirectory
        ];

    foreach (string basePath in searchPaths) {
      string fullPath = Path.Combine(basePath, libName);

      if (File.Exists(fullPath) && NativeLibrary.TryLoad(fullPath, out IntPtr handle)) {
        return handle;
      }
    }

    return IntPtr.Zero;
  }
}

