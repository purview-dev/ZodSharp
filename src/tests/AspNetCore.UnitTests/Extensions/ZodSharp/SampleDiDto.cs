using ZodSharp.Core;

namespace ZodSharp;

sealed class SampleDIDto
{
	public string? Name { get; set; }
}

sealed class SampleDIDtoSchemaValidator : IZodSchemaValidator<SampleDIDto>
{
	public ValidationResult<SampleDIDto> Validate(SampleDIDto value) => ValidationResult<SampleDIDto>.Success(value);

	public ValueTask<ValidationResult<SampleDIDto>> ValidateAsync(SampleDIDto value, CancellationToken _ = default) =>
		new(Validate(value));
}
