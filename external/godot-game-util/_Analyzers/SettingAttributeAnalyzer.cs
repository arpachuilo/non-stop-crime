using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Diagnostics;
using System.Collections.Immutable;
using System.Linq;

namespace GodotAnalyzers
{
	[DiagnosticAnalyzer(LanguageNames.CSharp)]
	public class SettingAttributeAnalyzer : DiagnosticAnalyzer
	{
		public const string DiagnosticId = "SETTING001";

		private static readonly LocalizableString Title =
				"Setting attribute must be used on Observable<T> properties";

		private static readonly LocalizableString MessageFormat =
				"Property '{0}' has [Setting] attribute but is not of type Observable<T>";

		private static readonly LocalizableString Description =
				"The [Setting] attribute can only be applied to properties of type Observable<T>.";

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
			context.ConfigureGeneratedCodeAnalysis(
					GeneratedCodeAnalysisFlags.None);
			context.EnableConcurrentExecution();

			context.RegisterSyntaxNodeAction(AnalyzeProperty, SyntaxKind.PropertyDeclaration);
		}

		private static void AnalyzeProperty(SyntaxNodeAnalysisContext context)
		{
			var propertyDeclaration = (PropertyDeclarationSyntax)context.Node;
			var propertySymbol = context.SemanticModel.GetDeclaredSymbol(propertyDeclaration);

			if (propertySymbol == null)
				return;

			var hasSettingAttribute = propertySymbol.GetAttributes()
					.Any(attr => attr.AttributeClass?.Name == "SettingAttribute");

			if (!hasSettingAttribute)
				return;

			var propertyType = propertySymbol.Type;

			if (!IsObservableType(propertyType) || !propertySymbol.IsStatic)
			{
				var diagnostic = Diagnostic.Create(
						Rule,
						propertyDeclaration.Identifier.GetLocation(),
						propertySymbol.Name);

				context.ReportDiagnostic(diagnostic);
			}
		}

		private static bool IsObservableType(ITypeSymbol typeSymbol)
		{
			if (typeSymbol is not INamedTypeSymbol namedType)
				return false;

			return namedType.IsGenericType &&
						 namedType.Name == "Observable" &&
						 namedType.TypeArguments.Length == 1;
		}
	}
}
