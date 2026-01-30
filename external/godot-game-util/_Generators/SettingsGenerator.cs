using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using System.Text;
using System.Linq;
using System.Collections.Generic;

public static class SettingsGenerator {
  public static string GenerateSettings(Compilation compilation) {
    var sb = new StringBuilder();
    sb.AppendLine("using Godot;");
    sb.AppendLine("using System;");
    sb.AppendLine("using System.Reflection;");
    sb.AppendLine("using System.Collections.Generic;");
    sb.AppendLine();

    var settingsByType = new Dictionary<string, List<string>>();

    foreach (var syntaxTree in compilation.SyntaxTrees) {
      var semanticModel = compilation.GetSemanticModel(syntaxTree);
      var root = syntaxTree.GetRoot();

      var properties = root.DescendantNodes()
        .OfType<PropertyDeclarationSyntax>();

      foreach (var property in properties) {
        var propertySymbol = semanticModel.GetDeclaredSymbol(property) as IPropertySymbol;
        if (propertySymbol == null) continue;

        var settingAttribute = propertySymbol.GetAttributes()
          .FirstOrDefault(attr => attr.AttributeClass?.Name == "SettingAttribute");

        if (settingAttribute != null) {
          var propertyName = propertySymbol.Name;

          if (propertySymbol.Type is INamedTypeSymbol namedType && namedType.IsGenericType) {
            var typeArg = namedType.TypeArguments.FirstOrDefault();
            if (typeArg != null) {
              var typeName = typeArg.ToDisplayString();

              if (!settingsByType.ContainsKey(typeName)) {
                settingsByType[typeName] = new List<string>();
              }
              settingsByType[typeName].Add(propertyName);
            }
          }
        }
      }
    }

    sb.AppendLine("public static partial class Settings");
    sb.AppendLine("{");
    sb.AppendLine("\tprivate static Dictionary<string, PropertyInfo> Values = [];");
    sb.AppendLine();

    foreach (var kvp in settingsByType.OrderBy(x => x.Key)) {
      var typeName = kvp.Key;
      var settings = kvp.Value;

      var enumName = GetSettingsClassName(typeName);
      var constName = $"{enumName}Reference";
      var values = string.Join(",", settings.OrderBy(s => s));
      sb.AppendLine($"\tpublic const string {constName} = \"{values}\";");
    }

    foreach (var kvp in settingsByType.OrderBy(x => x.Key)) {
      var typeName = kvp.Key;
      var setterMethodName = GetSetterMethodName(typeName);
      var getterMethodName = GetGetterMethodName(typeName);

      sb.AppendLine($"\tpublic static void {setterMethodName}(string name, {typeName} value)");
      sb.AppendLine("\t{");
      sb.AppendLine("\t\tif (Values.TryGetValue(name, out PropertyInfo property))");
      sb.AppendLine("\t\t{");
      sb.AppendLine("\t\t\tvar observable = property.GetValue(null);");
      sb.AppendLine("\t\t\tvar valueProp = observable.GetType().GetProperty(\"Value\");");
      sb.AppendLine("\t\t\tvalueProp.SetValue(observable, value);");
      sb.AppendLine("\t\t}");
      sb.AppendLine("\t\telse");
      sb.AppendLine("\t\t{");
      sb.AppendLine("\t\t\tGD.PrintErr($\"Settings: No setting named {name}\");");
      sb.AppendLine("\t\t}");
      sb.AppendLine("\t}");
      sb.AppendLine();

      sb.AppendLine($"\tpublic static {typeName} {getterMethodName}(string name)");
      sb.AppendLine("\t{");
      sb.AppendLine("\t\tif (Values.TryGetValue(name, out PropertyInfo property))");
      sb.AppendLine("\t\t{");
      sb.AppendLine("\t\t\tvar observable = property.GetValue(null);");
      sb.AppendLine("\t\t\tvar valueProp = observable.GetType().GetProperty(\"Value\");");
      sb.AppendLine($"\t\t\treturn ({typeName})valueProp.GetValue(observable);");
      sb.AppendLine("\t\t}");
      sb.AppendLine("\t\telse");
      sb.AppendLine("\t\t{");
      sb.AppendLine("\t\t\tGD.PrintErr($\"Settings: No setting named {name}\");");
      sb.AppendLine("\t\t\treturn default;");
      sb.AppendLine("\t\t}");
      sb.AppendLine("\t}");
      sb.AppendLine();
    }
    sb.AppendLine("}");

    return sb.ToString();
  }

  private static string GetSettingsClassName(string typeName) {
    var sanitized = typeName.Replace(".", "").Replace("<", "").Replace(">", "").Replace(",", "").Replace(" ", "");
    if (sanitized.Length > 0 && char.IsLower(sanitized[0])) {
      sanitized = char.ToUpper(sanitized[0]) + sanitized.Substring(1);
    }
    return $"{sanitized}Settings";
  }

  private static string GetSetterMethodName(string typeName) {
    var sanitized = typeName.Replace(".", "").Replace("<", "").Replace(">", "").Replace(",", "").Replace(" ", "");
    if (sanitized.Length > 0 && char.IsLower(sanitized[0])) {
      sanitized = char.ToUpper(sanitized[0]) + sanitized.Substring(1);
    }
    return $"Set{sanitized}Value";
  }

  private static string GetGetterMethodName(string typeName) {
    var sanitized = typeName.Replace(".", "").Replace("<", "").Replace(">", "").Replace(",", "").Replace(" ", "");
    if (sanitized.Length > 0 && char.IsLower(sanitized[0])) {
      sanitized = char.ToUpper(sanitized[0]) + sanitized.Substring(1);
    }
    return $"Get{sanitized}Value";
  }
}
