#nullable enable
using System;
using System.Runtime.InteropServices;

namespace Steam;

/// <summary>
/// Provides extension methods for serializing structs to byte arrays and deserializing back.
/// Used primarily for network protocol messages that need to be sent over Steam connections.
/// </summary>
internal static class SteamExtensions {
  /// <summary>
  /// Converts a struct to a byte array for network transmission.
  /// </summary>
  /// <typeparam name="T">The struct type to serialize. Must be a value type.</typeparam>
  /// <param name="t">The struct instance to convert</param>
  /// <returns>A byte array containing the binary representation of the struct</returns>
  /// <remarks>
  /// MEMORY SAFETY: Allocates unmanaged memory temporarily. Uses try/finally to ensure
  /// cleanup even if an exception occurs during marshaling.
  /// </remarks>
  public static byte[] ToBytes<T>(this T t) where T : struct {
    int size = Marshal.SizeOf(t);
    byte[] arr = new byte[size];

    IntPtr ptr = IntPtr.Zero;
    try {
      ptr = Marshal.AllocHGlobal(size);
      Marshal.StructureToPtr(t, ptr, true);
      Marshal.Copy(ptr, arr, 0, size);
    } finally {
      Marshal.FreeHGlobal(ptr);
    }

    return arr;
  }

  /// <summary>
  /// Converts a byte array back to a struct.
  /// </summary>
  /// <typeparam name="T">The struct type to deserialize to</typeparam>
  /// <param name="bytes">The byte array containing the serialized struct</param>
  /// <returns>The deserialized struct, or null if deserialization fails</returns>
  /// <exception cref="Exception">Thrown if the byte array is too small for the struct size</exception>
  public static T? ToStruct<T>(this byte[] bytes) {
    int size = Marshal.SizeOf(typeof(T));
    if (bytes.Length < size)
      throw new Exception("Invalid parameter");

    IntPtr ptr = Marshal.AllocHGlobal(size);
    try {
      Marshal.Copy(bytes, 0, ptr, size);
      return Marshal.PtrToStructure<T>(ptr);
    } finally {
      Marshal.FreeHGlobal(ptr);
    }
  }
}
