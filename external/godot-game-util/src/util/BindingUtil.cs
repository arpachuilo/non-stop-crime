using Godot;
using System;

/// Supports Key, Mouse Button and Joypad Button events
/// Key Format: Key:Ctrl+Alt+Shift+Meta:Keycode
/// Mouse Format: Mouse:Ctrl+Alt+Shift+Meta:ButtonIndex
/// Joypad Format: Joypad:ButtonIndex
public static class BindingUtil {
  /// <summary>
  /// Parse input event from string sequence
  ///
  /// Returns null if string format is unsupported
  /// </summary>
  public static InputEvent FromSequence(string sequence) {
    if (sequence.StartsWith("Key:")) {
      // Format: Key:Ctrl+Alt+Shift+Meta:Keycode
      var parts = sequence.Split(':');
      if (parts.Length < 3) return null;

      var mods = parts[1].Split(['+'], StringSplitOptions.RemoveEmptyEntries);

      var keyEvent = new InputEventKey();
      foreach (var mod in mods) {
        switch (mod) {
          case "Ctrl": keyEvent.CtrlPressed = true; break;
          case "Alt": keyEvent.AltPressed = true; break;
          case "Shift": keyEvent.ShiftPressed = true; break;
          case "Meta": keyEvent.MetaPressed = true; break;
        }
      }

      var key = parts[2];
      if (!Enum.TryParse(key, out Key physicalKeycode)) {
        physicalKeycode = (Key)long.Parse(key);
      }

      keyEvent.Keycode = DisplayServer.KeyboardGetKeycodeFromPhysical(physicalKeycode);
      return keyEvent;
    }

    if (sequence.StartsWith("Mouse:")) {
      // Format: Mouse:Ctrl+Alt+Shift+Meta:ButtonIndex
      var parts = sequence.Split(':');
      if (parts.Length < 3) return null;

      var mods = parts[1].Split(['+'], StringSplitOptions.RemoveEmptyEntries);

      var mouseEvent = new InputEventMouseButton();
      foreach (var mod in mods) {
        switch (mod) {
          case "Ctrl": mouseEvent.CtrlPressed = true; break;
          case "Alt": mouseEvent.AltPressed = true; break;
          case "Shift": mouseEvent.ShiftPressed = true; break;
          case "Meta": mouseEvent.MetaPressed = true; break;
        }
      }

      var buttonIndex = parts[2];
      mouseEvent.ButtonIndex = (MouseButton)int.Parse(buttonIndex);
      return mouseEvent;
    }

    if (sequence.StartsWith("Joypad:")) {
      // Format: Joypad:ButtonIndex
      var parts = sequence.Split(':');
      if (parts.Length < 2) return null;
      var joyEvent = new InputEventJoypadButton {
        ButtonIndex = (JoyButton)int.Parse(parts[1])
      };

      return joyEvent;
    }

    return null;
  }

  /// <summary>
  /// Helper to parse just first event from InputEvent array
  /// </summary>
  public static string FromInputEvents(Godot.Collections.Array<InputEvent> evs) {
    return FromInputEvent(evs[0]);
  }

  /// <summary>
  /// Convert InputEvent to string sequence
  ///
  /// Returns empty string if event type is unsupported
  /// </summary>
  public static string FromInputEvent(InputEvent ev) {
    if (ev is InputEventKey keyEvent) {
      string mods = "";
      if (keyEvent.CtrlPressed) mods += "Ctrl";
      if (keyEvent.AltPressed) mods += "Alt";
      if (keyEvent.ShiftPressed) mods += "Shift";
      if (keyEvent.MetaPressed) mods += "Meta";
      return $"Key:{string.Join("+", mods)}:{keyEvent.PhysicalKeycode}";
    }

    if (ev is InputEventMouseButton mouseEvent) {
      string mods = "";
      if (mouseEvent.CtrlPressed) mods += "Ctrl";
      if (mouseEvent.AltPressed) mods += "Alt";
      if (mouseEvent.ShiftPressed) mods += "Shift";
      if (mouseEvent.MetaPressed) mods += "Meta";
      return $"Mouse:{string.Join("+", mods)}:{(int)mouseEvent.ButtonIndex}";
    }

    if (ev is InputEventJoypadButton joyEvent) {
      return $"Joypad:{joyEvent.ButtonIndex}";
    }

    return "";
  }
}
