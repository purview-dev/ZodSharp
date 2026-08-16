using ZodSharp.Core;

namespace ZodSharp.AspNetCore;

sealed class SampleDiDto
{
	public string? Name { get; set; }
}

sealed class SampleDiDtoSchemaValidator : IZodSchemaValidator<SampleDiDto>
{
	public ValidationResult<SampleDiDto> Validate(SampleDiDto value) => ValidationResult<SampleDiDto>.Success(value);

	public ValueTask<ValidationResult<SampleDiDto>> ValidateAsync(
		SampleDiDto value,
		CancellationToken cancellationToken = default
	) => new(Validate(value));
}
