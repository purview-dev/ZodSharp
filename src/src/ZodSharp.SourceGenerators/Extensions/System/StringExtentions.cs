using System.ComponentModel;

namespace System;

[EditorBrowsable(EditorBrowsableState.Never)]
static class StringExtentions
{
	public static string PropertyName(this string message, string propertyName) =>
		message.Replace("{PropertyName}", propertyName);
}
