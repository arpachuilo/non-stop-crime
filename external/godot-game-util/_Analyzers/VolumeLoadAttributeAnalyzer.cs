using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Diagnostics;
using System.Collections.Immutable;
using System.Linq;

namespace GodotAnalyzers
{
	[DiagnosticAnalyzer(LanguageNames.CSharp)]
	public class VolumeLoadAttributeAnalyzer : DiagnosticAnalyzer
	{
		public const string DiagnosticId = "VOLUME001";

		private static readonly LocalizableString Title =
				"VolumeLoad attribute must be used on Observable<float> properties";

		private static readonly LocalizableString MessageFormat =
				"Property '{0}' has [VolumeLoad] attribute but is not of type Observable<float>";

		private static readonly LocalizableString Description =
				"The [VolumeLoad] attribute can only be applied to properties of type Observable<float>.";

		private const string Category = "Usage";

		private static readonly DiagnosticDescriptor Rule = new DiagnosticDescriptor(
				DiagnosticId,
				Title,
				MessageFormat,
				Category,
				DiagnosticSeverity.Error,
				isEnabledByDefault: true,
				description: Description);

		public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics
				=> ImmutableArray.Create(Rule);

		public override void Initialize(AnalysisContext context)
		{
			context.ConfigureGeneratedCodeAnalysis(GeneratedCodeAnalysisFlags.None);
			context.EnableConcurrentExecution();

			context.RegisterSyntaxNodeAction(AnalyzeProperty, SyntaxKind.PropertyDeclaration);
		}

		private static void AnalyzeProperty(SyntaxNodeAnalysisContext context)
		{
			var propertyDeclaration = (PropertyDeclarationSyntax)context.Node;
			var propertySymbol = context.SemanticModel.GetDeclaredSymbol(propertyDeclaration);

			if (propertySymbol == null)
				return;

			var hasVolumeLoadAttribute = propertySymbol.GetAttributes()
					.Any(attr => attr.AttributeClass?.Name == "VolumeLoadAttribute");

			if (!hasVolumeLoadAttribute)
				return;

			var propertyType = propertySymbol.Type;

			if (!IsObservableFloat(propertyType))
			{
				var diagnostic = Diagnostic.Create(
						Rule,
						propertyDeclaration.Identifier.GetLocation(),
						propertySymbol.Name);

				context.ReportDiagnostic(diagnostic);
			}
		}

		private static bool IsObservableFloat(ITypeSymbol typeSymbol)
		{
			if (!(typeSymbol is INamedTypeSymbol namedType))
				return false;

			if (!namedType.IsGenericType ||
					namedType.Name != "Observable" ||
					namedType.TypeArguments.Length != 1)
				return false;

			var typeArgument = namedType.TypeArguments[0];
			return typeArgument.SpecialType == SpecialType.System_Single;
		}
	}
}
