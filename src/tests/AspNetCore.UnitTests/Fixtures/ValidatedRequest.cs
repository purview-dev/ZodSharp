using System.ComponentModel.DataAnnotations;

namespace ZodSharp.AspNetCore.Fixtures;

[ZodSchema]
public class ValidatedRequest
{
	[Required]
	[MinLength(3)]
	public string? Name { get; set; }
}
