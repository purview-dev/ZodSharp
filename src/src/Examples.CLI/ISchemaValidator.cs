using ZodSharp.Core;

namespace ZodSharp.Examples.CLI;

interface ISchemaValidator<T>
{
	ValidationResult<T> Validate(T value);
}
