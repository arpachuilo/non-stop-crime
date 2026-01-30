using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Diagnostics;
using System.Collections.Immutable;
using System.Linq;

namespace GodotAnalyzers
{
	[DiagnosticAnalyzer(LanguageNames.CSharp)]
	public class ObservableHandlerAnalyzer : DiagnosticAnalyzer
	{
		public const string DiagnosticId = "OBSERVABLE001";

		private static readonly LocalizableString Title =
				"ObservableHandler must have correct method signature";

		private static readonly LocalizableString MessageFormat =
				"Method '{0}' with [ObservableHandler] must have signature: void MethodName(PropertyInfo property)";

		private static readonly LocalizableString Description =
				"Methods decorated with [ObservableHandler] must accept a single PropertyInfo parameter.";

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

			context.RegisterSyntaxNodeAction(AnalyzeMethod, SyntaxKind.MethodDeclaration);
		}

		private static void AnalyzeMethod(SyntaxNodeAnalysisContext context)
		{
			var methodDeclaration = (MethodDeclarationSyntax)context.Node;
			var methodSymbol = context.SemanticModel.GetDeclaredSymbol(methodDeclaration);

			if (methodSymbol == null)
				return;

			var hasObservableHandlerAttribute = methodSymbol.GetAttributes()
					.Any(attr => attr.AttributeClass?.Name == "ObservableHandlerAttribute");

			if (!hasObservableHandlerAttribute)
				return;

			if (!HasCorrectSignature(methodSymbol))
			{
				var diagnostic = Diagnostic.Create(
						Rule,
						methodDeclaration.Identifier.GetLocation(),
						methodSymbol.Name);

				context.ReportDiagnostic(diagnostic);
			}
		}

		private static bool HasCorrectSignature(IMethodSymbol methodSymbol)
		{
			if (!methodSymbol.IsStatic)
				return false;

			if (methodSymbol.ReturnsVoid == false)
				return false;

			if (methodSymbol.Parameters.Length != 1)
				return false;

			var parameter = methodSymbol.Parameters[0];
			var parameterType = parameter.Type;

			return parameterType.Name == "PropertyInfo" &&
						 parameterType.ContainingNamespace?.ToDisplayString() == "System.Reflection";
		}
	}
}
