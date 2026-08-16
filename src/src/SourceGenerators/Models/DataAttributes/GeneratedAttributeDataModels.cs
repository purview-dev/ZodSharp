using System.Collections.Immutable;
using Microsoft.CodeAnalysis;

namespace ZodSharp.SourceGenerators.Models.DataAttributes;

[Generate("System.ComponentModel.DataAnnotations.ValidationAttribute", MatchByInheritance = true)]
readonly partial record struct ValidationAttributeData(
	string? ErrorMessage,
	string? ErrorMessageResourceName,
	ITypeSymbol? ErrorMessageResourceType
);

[Generate("System.ComponentModel.DataAnnotations.RequiredAttribute")]
readonly partial record struct RequiredAttributeData(
	bool AllowEmptyStrings,
	[NestedModel] ValidationAttributeData ValidationAttribute
);

[Generate("System.ComponentModel.DataAnnotations.CompareAttribute")]
readonly partial record struct CompareAttributeData(
	[Argument("otherProperty")] string OtherProperty,
	string? OtherPropertyDisplayName,
	[NestedModel] ValidationAttributeData ValidationAttribute
);

[Generate("System.ComponentModel.DataAnnotations.DisplayAttribute")]
readonly partial record struct DisplayAttributeData(
	string? ShortName,
	string? Name,
	string? Description,
	string? Prompt,
	ITypeSymbol? ResourceType,
	bool AutoGenerateField,
	bool AutoGenerateFilter,
	int Order
);

[Generate("System.ComponentModel.DataAnnotations.EmailAddressAttribute")]
readonly partial record struct EmailAddressAttributeData([NestedModel] ValidationAttributeData ValidationAttributeData);

[Generate("System.ComponentModel.DataAnnotations.CreditCardAttribute")]
readonly partial record struct CreditCardAttributeData([NestedModel] ValidationAttributeData ValidationAttribute);

[Generate("System.ComponentModel.DataAnnotations.PhoneAttribute")]
readonly partial record struct PhoneAttribute([NestedModel] ValidationAttributeData ValidationAttribute);

[Generate("System.ComponentModel.DataAnnotations.UrlAttribute")]
readonly partial record struct UrlAttribute([NestedModel] ValidationAttributeData ValidationAttribute);

[Generate("System.ComponentModel.DataAnnotations.StringLengthAttribute")]
readonly partial record struct StringLengthAttribute(
	[Argument("maximumLength")] int MaximumLength,
	[Property] int MinimumLength,
	[NestedModel] ValidationAttributeData ValidationAttribute
);

[Generate("System.ComponentModel.DataAnnotations.MinLengthAttribute")]
readonly partial record struct MinLengthAttributeData(
	[Argument("length")] int Length,
	[NestedModel] ValidationAttributeData ValidationAttribute
);

[Generate("System.ComponentModel.DataAnnotations.MaxLengthAttribute")]
readonly partial record struct MaxLengthAttributeData(
	[Argument("length", DefaultValue = -1)] int Length,
	[NestedModel] ValidationAttributeData ValidationAttribute
);

[Generate("System.ComponentModel.DataAnnotations.RegularExpressionAttribute")]
readonly partial record struct RegularExpressionAttributeData(
	[Argument("pattern")] string? Pattern,
	[Property(DefaultValue = 2000)] int MatchTimeoutInMilliseconds,
	[NestedModel] ValidationAttributeData ValidationAttribute
);

[Generate("System.ComponentModel.DataAnnotations.AllowedValuesAttribute")]
readonly partial record struct AllowedValuesAttributeData(
	[Argument("values")] ImmutableArray<TypedConstant> Values,
	[NestedModel] ValidationAttributeData ValidationAttribute
);

[Generate("System.ComponentModel.DataAnnotations.Base64StringAttribute")]
readonly partial record struct Base64StringAttributeData([NestedModel] ValidationAttributeData ValidationAttribute);

[Generate("System.ComponentModel.DataAnnotations.DeniedValuesAttribute")]
readonly partial record struct DeniedValuesAttributeData(
	[Argument("values")] ImmutableArray<TypedConstant> Values,
	[NestedModel] ValidationAttributeData ValidationAttribute
);

[Generate("System.ComponentModel.DataAnnotations.LengthAttribute")]
readonly partial record struct LengthAttributeData(
	[Argument("minimumLength")] int MinimumLength,
	[Argument("maximumLength")] int MaximumLength,
	[NestedModel] ValidationAttributeData ValidationAttribute
);
