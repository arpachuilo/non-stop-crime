using Microsoft.CodeAnalysis;
using System.Linq;

[Generator]
public class GodotGenerator : IIncrementalGenerator {
  public void Initialize(IncrementalGeneratorInitializationContext context) {
    context.RegisterPostInitializationOutput(ctx => {
      var swizzle = GodotVectorSwizzleGenerator.GenerateSwizzleExtensions();
      ctx.AddSource("GodotVectorSwizzle.g.cs", swizzle);
    });

    context.RegisterSourceOutput(context.CompilationProvider, (ctx, compilation) => {
      var settings = SettingsGenerator.GenerateSettings(compilation);
      ctx.AddSource("Settings.g.cs", settings);
    });

    var godotFiles = context.AdditionalTextsProvider
      .Where(file => file.Path.EndsWith("project.godot"));

    context.RegisterSourceOutput(godotFiles, (ctx, file) => {
      var project = GodotProjectGenerator.GenerateProject(file);
      ctx.AddSource("GodotProject.g.cs", project);
    });
  }
}

