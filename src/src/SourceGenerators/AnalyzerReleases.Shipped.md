## Release 1.0

### New Rules

| Rule ID | Category | Severity | Notes |
|---|---|---|---|
| ZODSGEN001 | ZodSharp.SourceGenerator | Error | This is effectively a fatal incident where the source generator has failed. |

## Release 2.0.0

### New Rules

| Rule ID | Category | Severity | Notes |
|---|---|---|---|
| ZODSGEN003 | ZodSharp.SourceGenerator | Error | Invalid LengthAttribute configuration
| ZODSGEN004 | ZodSharp.SourceGenerator | Error | Unsupported LengthAttribute target
| ZODSGEN005 | ZodSharp.SourceGenerator | Error | Invalid DataAnnotations error message resource configuration
| ZODSGEN006 | ZodSharp.SourceGenerator | Error | Unsupported DataAnnotations usage
| ZODSGEN007 | ZodSharp.SourceGenerator | Error | Custom validation method not found
| ZODSGEN008 | ZodSharp.SourceGenerator | Error | Invalid return type for custom validation method
| ZODSGEN009 | ZodSharp.SourceGenerator | Error | Invalid custom validation method parameter count
| ZODSGEN010 | ZodSharp.SourceGenerator | Error | Invalid custom validation model parameter
| ZODSGEN011 | ZodSharp.SourceGenerator | Error | Missing or invalid CancellationToken parameter
| ZODSGEN012 | ZodSharp.SourceGenerator | Error | Generic custom validation method unsupported
| ZODSGEN013 | ZodSharp.SourceGenerator | Error | Custom validation method must be static when defined on model type
| ZODSGEN014 | ZodSharp.SourceGenerator | Error | Inaccessible custom validation method
| ZODSGEN015 | ZodSharp.SourceGenerator | Error | Ambiguous custom validation method overloads
| ZODSGEN016 | ZodSharp.SourceGenerator | Error | Invalid custom validation method name
| ZODSGEN017 | ZodSharp.SourceGenerator | Error | Abstract custom validation method unsupported
| ZODSGEN018 | ZodSharp.SourceGenerator | Error | Partial custom validation method has not been implemented
| ZODSGEN019 | ZodSharp.SourceGenerator | Error | Invalid custom validation method parameter modifier
| ZODSGEN020 | ZodSharp.SourceGenerator | Error | CompareAttribute references an unknown property
| ZODSGEN021 | ZodSharp.SourceGenerator | Error | Add a reference to System.ComponentModel.DataAnnotations
| ZODSGEN027 | ZodSharp.SourceGenerator | Error | IValidateOptions generation requires a reference to Microsoft.Extensions.Options
| ZODSGEN028 | ZodSharp.SourceGenerator | Error | IValidateOptions generation requires a reference type
| ZODSGEN029 | ZodSharp.SourceGenerator | Error | OnZodValidate refinement hook and async custom validation methods are mutually exclusive
| ZODSGEN030 | ZodSharp.SourceGenerator | Error | Custom rule does not implement IValidationRule<T> for the property type (or an unbound generic rule cannot be closed with it)
| ZODSGEN031 | ZodSharp.SourceGenerator | Error | Unable to map an attribute value to a custom rule constructor parameter
| ZODSGEN032 | ZodSharp.SourceGenerator | Error | Unable to generate a validation attribute for a custom rule
| ZODSGEN033 | ZodSharp.SourceGenerator | Warning | Rule attribute is applied to a type that gets no generated schema
| ZODSGEN034 | ZodSharp.SourceGenerator | Error | OnZodValidate refinement hook requires the target type (and its containing types) to be declared partial
| ZODSGEN035 | ZodSharp.SourceGenerator | Error | OnZodValidate refinement hook must be declared as 'partial void OnZodValidate(RefineCtx<T> context)'
| ZODSGEN036 | ZodSharp.SourceGenerator | Error | The synchronous refinement method contract has been replaced by the OnZodValidate hook
| ZODSASP001 | ZodSharp.SourceGenerator | Warning | MessageFormat placeholder is not declared in Parameters
| ZODSASP002 | ZodSharp.SourceGenerator | Warning | Error type containing type must be partial
| ZODSASP003 | ZodSharp.SourceGenerator | Warning | ErrorType field must be static readonly
| ZODSASP100 | ZodSharp.SourceGenerator | Error | Unhandled exception in the ErrorType source generator
| ZODSASP101 | ZodSharp.SourceGenerator | Error | ErrorType Parameters could not be extracted
