using System;
using System.Numerics;

/// <summary>
/// Utilities for bitmask operations
/// </summary>
public static class BitUtil {
  /// <summary>
  /// Toggle given layer in bitmask
  /// </summary>
  public static T ToggleLayer<T, TLayer>(this T mask, TLayer layer)
    where T : INumber<T>, IBitwiseOperators<T, T, T>, IShiftOperators<T, int, T>
    where TLayer : IConvertible {
    int shift = Convert.ToInt32(layer) - 1;
    T bit = T.One << shift;
    return mask ^ bit;
  }

  /// <summary>
  /// Check if bit mask has given layer
  /// </summary>
  public static bool HasLayer<T, TLayer>(this T mask, TLayer layer)
    where T : INumber<T>, IBitwiseOperators<T, T, T>, IShiftOperators<T, int, T>
    where TLayer : IConvertible {
    int shift = Convert.ToInt32(layer) - 1;
    T bit = T.One << shift;
    return (mask & bit) != T.Zero;
  }

  /// <summary>
  /// Check if bit mask has given enum layer
  /// </summary>
  public static TEnum ToggleLayerEnum<TEnum, TLayer>(this TEnum mask, TLayer layer)
    where TEnum : struct, Enum
    where TLayer : IConvertible {
    long maskValue = Convert.ToInt64(mask);
    int shift = Convert.ToInt32(layer) - 1;
    long bit = 1L << shift;
    long result = maskValue ^ bit;
    return (TEnum)Enum.ToObject(typeof(TEnum), result);
  }

  /// <summary>
  /// Check if bit mask has given enum layer
  /// </summary>
  public static bool HasLayerEnum<TEnum, TLayer>(this TEnum mask, TLayer layer)
    where TEnum : struct, Enum
    where TLayer : IConvertible {
    int maskValue = Convert.ToInt32(mask);
    int shift = Convert.ToInt32(layer) - 1;
    int bit = 1 << shift;
    return (maskValue & bit) != 0;
  }
}
