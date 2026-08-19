namespace ZodSharp.SourceGenerators.Helpers;

static class CodeGenHelpers
{
	public static CodeWriter WriteRule(
		this CodeWriter writer,
		string propertyName,
		string comparison,
		string errorCode,
		string errorMessage
	)
	{
		using (writer.OpenBlockScope($"if ({comparison})"))
		{
			writer.WriteLine(
				$"errors ??= new {TypeLibrary.Collections.List.MakeGeneric(TypeLibrary.ValidationError)}();"
			);
			writer.OpenDelimitedBlock(
				$"errors.Add(new {TypeLibrary.ValidationError}",
				"(",
				"));",
				bodyWriter =>
				{
					bodyWriter.Write(errorCode.Surround()).WriteLine(",");
					bodyWriter.Write(errorMessage.Surround()).WriteLine(",");
					bodyWriter.WriteLine($"new[] {{ \"{propertyName}\" }}");
				}
			);
		}

		return writer.NewLine();
	}

	public static string GetPathFieldName(string propertyName) => $"Path_{propertyName}";

	public static string GetLocalIdentifier(string propertyName, string suffix) =>
		string.IsNullOrEmpty(propertyName)
			? suffix
			: char.ToLowerInvariant(propertyName[0]) + propertyName.Substring(1) + suffix;

	public static string Quote(string value) => $"\"{value.Replace("\\", "\\\\").Replace("\"", "\\\"")}\"";

	public static string QuoteChar(char value)
	{
		return value switch
		{
			'\'' => "'\\''",
			'\\' => "'\\\\'",
			'\0' => "'\\0'",
			'\a' => "'\\a'",
			'\b' => "'\\b'",
			'\f' => "'\\f'",
			'\n' => "'\\n'",
			'\r' => "'\\r'",
			'\t' => "'\\t'",
			'\v' => "'\\v'",
			_ => $"'{value}'",
		};
	}
}
