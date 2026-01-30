using Microsoft.CodeAnalysis;
using System.Text;
using System.Linq;
using System.Collections.Generic;

public static class GodotProjectGenerator {
  public static string GenerateProject(AdditionalText godotFile) {
    string[] lines = godotFile.GetText()!.Lines.Select(line => line.ToString()).ToArray();

    if (lines.Length == 0) return "";

    var sb = new StringBuilder();
    Dictionary<string, int> collisionLayers = [];
    List<string> groups = [];
    List<string> inputs = [];

    var currentHeader = "";

    var layerHeader = "[layer_names]";
    var layerPrefix = "3d_physics/layer_";

    var groupHeader = "[global_group]";

    var inputHeader = "[input]";

    // Collect info we need
    foreach (var line in lines) {
      if (line == layerHeader) {
        currentHeader = layerHeader;
        continue;
      }

      if (line == groupHeader) {
        currentHeader = groupHeader;
        continue;
      }

      if (line == inputHeader) {
        currentHeader = inputHeader;
        continue;
      }

      // Headers we do not care about
      if (line.StartsWith("[")) {
        currentHeader = "";
        continue;
      }


      // Processing for layers
      if (currentHeader == layerHeader && line.StartsWith(layerPrefix)) {
        var text = line.Substring(layerPrefix.Length);
        string[] parts = text.Split('=');

        string layerStr = parts[0];
        string name = parts[1].Trim().Trim('"');

        if (int.TryParse(layerStr, out int layerIndex))
          collisionLayers[name] = layerIndex;
      }

      // Processing for groups
      if (currentHeader == groupHeader) {
        if (line.Trim() == "") continue;
        string[] parts = line.Split('=');
        groups.Add(parts[0].Trim());
      }

      // Processing for input
      if (currentHeader == inputHeader) {
        string[] parts = line.Split('=');
        if (parts.Length <= 1) continue;
        inputs.Add(parts[0].Trim());
      }


    }


    // Build layers
    sb.AppendLine("public static class Layer");
    sb.AppendLine("{");

    foreach (var kvp in collisionLayers) {
      sb.AppendLine($"\tpublic const int {kvp.Key} = {kvp.Value};");
    }

    sb.AppendLine("}");
    sb.AppendLine("");

    // Build groups
    sb.AppendLine("public static class Group");
    sb.AppendLine("{");

    foreach (var group in groups) {
      sb.AppendLine($"\tpublic const string {group} = \"{group}\";");
    }

    sb.AppendLine("}");
    sb.AppendLine("");

    // Build inputs
    sb.AppendLine("public static class InputActions");
    sb.AppendLine("{");

    foreach (var input in inputs) {
      sb.AppendLine($"\tpublic const string {input} = \"{input}\";");
    }

    sb.AppendLine("}");

    return sb.ToString();
  }
}
