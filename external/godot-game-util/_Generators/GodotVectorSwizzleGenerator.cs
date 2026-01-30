using System.Text;
using System.Linq;
using System.Collections.Generic;

public static class GodotVectorSwizzleGenerator {
  public static string GenerateSwizzleExtensions() {
    var sb = new StringBuilder();

    sb.AppendLine("using Godot;");
    sb.AppendLine();
    sb.AppendLine("public static class VectorSwizzleExtensions");
    sb.AppendLine("{");

    var components2D = new[] { "X", "Y" };
    var components3D = new[] { "X", "Y", "Z" };
    var allComponents = new[] { "X", "Y", "Z", "W", "0", "1", "N" };

    // Generate Vector2 -> Vector2 swizzles
    GenerateSwizzles(sb, "Vector2", "Vector2", components2D, allComponents, 2);

    // Generate Vector2 -> Vector3 swizzles
    GenerateSwizzles(sb, "Vector2", "Vector3", components2D, allComponents, 3);

    // Generate Vector3 -> Vector2 swizzles
    GenerateSwizzles(sb, "Vector3", "Vector2", components3D, allComponents, 2);

    // Generate Vector3 -> Vector3 swizzles
    GenerateSwizzles(sb, "Vector3", "Vector3", components3D, allComponents, 3);

    sb.AppendLine("}");
    return sb.ToString();
  }

  static private void GenerateSwizzles(StringBuilder sb, string inputType, string outputType,
      string[] inputComponents, string[] allComponents, int outputDimensions) {
    sb.AppendLine($"    // {inputType} -> {outputType} swizzles");

    GenerateSwizzlePermutations(sb, inputType, outputType, inputComponents,
        allComponents, outputDimensions, 0, new string[outputDimensions]);

    sb.AppendLine();
  }

  static private void GenerateSwizzlePermutations(StringBuilder sb, string inputType, string outputType,
      string[] inputComponents, string[] allComponents, int dimensions, int currentIndex, string[] current) {
    if (currentIndex == dimensions) {
      GenerateSwizzleMethod(sb, inputType, outputType, current);
      return;
    }

    foreach (var component in allComponents) {
      // Skip components that don't exist in the input type (except special ones)
      if (component != "0" && component != "1" && component != "N" && !inputComponents.Contains(component))
        continue;

      current[currentIndex] = component;
      GenerateSwizzlePermutations(sb, inputType, outputType, inputComponents,
          allComponents, dimensions, currentIndex + 1, current);
    }
  }

  static private void GenerateSwizzleMethod(StringBuilder sb, string inputType, string outputType, string[] components) {
    var nCount = components.Count(c => c == "N");

    if (nCount == 0) {
      // Original swizzle without parameters
      GenerateStandardSwizzle(sb, inputType, outputType, components);
    } else {
      // Parameterized swizzle
      GenerateParameterizedSwizzle(sb, inputType, outputType, components, nCount);
    }
  }

  static private void GenerateStandardSwizzle(StringBuilder sb, string inputType, string outputType, string[] components) {
    var methodName = "_" + string.Join("", components);
    var parameters = new string[components.Length];

    for (int i = 0; i < components.Length; i++) {
      switch (components[i]) {
        case "0":
          parameters[i] = "0f";
          break;
        case "1":
          parameters[i] = "1f";
          break;
        default:
          parameters[i] = $"v.{components[i]}";
          break;
      }
    }

    var constructorCall = outputType == "Vector2"
        ? $"new Vector2({string.Join(", ", parameters)})"
        : $"new Vector3({string.Join(", ", parameters)})";

    sb.AppendLine($"    public static {outputType} {methodName}(this {inputType} v) => {constructorCall};");
  }

  static private void GenerateParameterizedSwizzle(StringBuilder sb, string inputType, string outputType,
      string[] components, int nCount) {
    var methodName = "_" + string.Join("", components);
    var parameters = new List<string>();
    var methodParams = new List<string>();

    // Generate method parameters based on N count
    for (int i = 0; i < nCount; i++) {
      methodParams.Add($"float n{(nCount > 1 ? (i + 1).ToString() : "")}");
    }

    // Generate constructor parameters
    int nIndex = 0;
    for (int i = 0; i < components.Length; i++) {
      switch (components[i]) {
        case "0":
          parameters.Add("0f");
          break;
        case "1":
          parameters.Add("1f");
          break;
        case "N":
          if (nCount == 1) {
            parameters.Add("n");
          } else {
            nIndex++;
            parameters.Add($"n{nIndex}");
          }
          break;
        default:
          parameters.Add($"v.{components[i]}");
          break;
      }
    }

    var constructorCall = outputType == "Vector2"
        ? $"new Vector2({string.Join(", ", parameters)})"
        : $"new Vector3({string.Join(", ", parameters)})";

    var methodSignature = $"public static {outputType} {methodName}(this {inputType} v, {string.Join(", ", methodParams)})";

    sb.AppendLine($"    {methodSignature} => {constructorCall};");
  }
}
