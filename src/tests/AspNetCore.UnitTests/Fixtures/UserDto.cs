using ZodSharp;

namespace ZodSharp.AspNetCore.Fixtures;

[ZodSchema]
public class UserDto
{
	public string? Name { get; set; }
	public int Age { get; set; }
}
