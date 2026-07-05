using System.ComponentModel.DataAnnotations;

namespace ZodSharp;

[ZodSchema]
sealed class User
{
	[Required]
	[StringLength(50, MinimumLength = 3)]
	public string Name { get; set; } = string.Empty;

	[Required]
	[Range(0, 120)]
	public int Age { get; set; }

	[EmailAddress]
	public string? Email { get; set; }
}
