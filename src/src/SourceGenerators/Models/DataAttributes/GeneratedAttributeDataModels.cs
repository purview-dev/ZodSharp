using System.Collections.Immutable;
using System.ComponentModel.DataAnnotations;
using Microsoft.CodeAnalysis;
using Purview.SourceGeneratorFramework.Testing.Generators;

namespace ZodSharp.SourceGenerators.Models.DataAttributes;

[GenerateAttributeDataModel(typeof(ValidationAttribute), MatchByInheritance = true)]
readonly partial record struct ValidationAttributeData(
	string? ErrorMessage,
	string? ErrorMessageResourceName,
	ITypeSymbol? ErrorMessageResourceType
);

[GenerateAttributeDataModel(typeof(RequiredAttribute))]
readonly partial record struct RequiredAttributeData(
	bool AllowEmptyStrings,
	[AttributeNestedModelProperty] ValidationAttributeData ValidationAttribute
);

[GenerateAttributeDataModel(typeof(CompareAttribute))]
readonly partial record struct CompareAttributeData(
	[AttributeCtorProperty("otherProperty")] string OtherProperty,
	string? OtherPropertyDisplayName,
	[AttributeNestedModelProperty] ValidationAttributeData ValidationAttribute
);

[GenerateAttributeDataModel(typeof(DisplayAttribute))]
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

[GenerateAttributeDataModel(typeof(EmailAddressAttribute))]
readonly partial record struct EmailAddressAttributeData(
	[AttributeNestedModelProperty] ValidationAttributeData ValidationAttributeData
);

[GenerateAttributeDataModel(typeof(CreditCardAttribute))]
readonly partial record struct CreditCardAttributeData(
	[AttributeNestedModelProperty] ValidationAttributeData ValidationAttribute
);

[GenerateAttributeDataModel(typeof(System.ComponentModel.DataAnnotations.PhoneAttribute))]
readonly partial record struct PhoneAttribute(
	[AttributeNestedModelProperty] ValidationAttributeData ValidationAttribute
);

[GenerateAttributeDataModel(typeof(System.ComponentModel.DataAnnotations.UrlAttribute))]
readonly partial record struct UrlAttribute(
	[AttributeNestedModelProperty] ValidationAttributeData ValidationAttribute
);

[GenerateAttributeDataModel(typeof(System.ComponentModel.DataAnnotations.StringLengthAttribute))]
readonly partial record struct StringLengthAttribute(
	[AttributeCtorProperty("maximumLength")] int MaximumLength,
	int MinimumLength,
	[AttributeNestedModelProperty] ValidationAttributeData ValidationAttribute
);

[GenerateAttributeDataModel(typeof(MinLengthAttribute))]
readonly partial record struct MinLengthAttributeData(
	[AttributeCtorProperty("length")] int Length,
	[AttributeNestedModelProperty] ValidationAttributeData ValidationAttribute
);

[GenerateAttributeDataModel(typeof(MaxLengthAttribute))]
readonly partial record struct MaxLengthAttributeData(
	[AttributeCtorProperty("length")] int Length,
	[AttributeNestedModelProperty] ValidationAttributeData ValidationAttribute
);

[GenerateAttributeDataModel(typeof(RegularExpressionAttribute))]
readonly partial record struct RegularExpressionAttributeData(
	[AttributeCtorProperty("pattern")] string? Pattern,
	int MatchTimeoutInMilliseconds,
	[AttributeNestedModelProperty] ValidationAttributeData ValidationAttribute
);

[GenerateAttributeDataModel("System.ComponentModel.DataAnnotations.AllowedValuesAttribute")]
readonly partial record struct AllowedValuesAttributeData(
	[AttributeCtorProperty(0)] ImmutableArray<TypedConstant> Values,
	[AttributeNestedModelProperty] ValidationAttributeData ValidationAttribute
);

[GenerateAttributeDataModel("System.ComponentModel.DataAnnotations.Base64StringAttribute")]
readonly partial record struct Base64StringAttributeData(
	[AttributeNestedModelProperty] ValidationAttributeData ValidationAttribute
);

[GenerateAttributeDataModel("System.ComponentModel.DataAnnotations.DeniedValuesAttribute")]
readonly partial record struct DeniedValuesAttributeData(
	[AttributeCtorProperty(0)] ImmutableArray<TypedConstant> Values,
	[AttributeNestedModelProperty] ValidationAttributeData ValidationAttribute
);

[GenerateAttributeDataModel("System.ComponentModel.DataAnnotations.LengthAttribute")]
readonly partial record struct LengthAttributeData(
	[AttributeCtorProperty(0)] int MinimumLength,
	[AttributeCtorProperty(1)] int MaximumLength,
	[AttributeNestedModelProperty] ValidationAttributeData ValidationAttribute
);
