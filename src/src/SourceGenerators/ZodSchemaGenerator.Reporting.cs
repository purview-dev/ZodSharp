using Microsoft.CodeAnalysis;

namespace ZodSharp.SourceGenerators;

partial class ZodSchemaGenerator
{
	static void ReportDiagnostics(
		SourceProductionContext context,
		DiagnosticInfo diagnostic,
		GenerationLogger? logger
	) => ReportDiagnostics(context, [diagnostic], logger);

	static void ReportDiagnostics(
		SourceProductionContext context,
		IEnumerable<DiagnosticInfo> diagnostics,
		GenerationLogger? logger
	)
	{
		foreach (var diagnosticInfo in diagnostics)
		{
			var diagnostic = diagnosticInfo.ToDiagnostic();
			context.ReportDiagnostic(diagnostic);

			logger?.Diagnostic(diagnosticInfo);
		}
	}
}
