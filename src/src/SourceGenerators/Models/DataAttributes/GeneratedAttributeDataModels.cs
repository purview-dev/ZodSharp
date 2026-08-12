using System.Collections.Immutable;
using System.ComponentModel.DataAnnotations;
using Microsoft.CodeAnalysis;
using Purview.SourceGeneratorFramework.Testing.Generators;

namespace ZodSharp.SourceGenerators.Models.DataAttributes;

[GenerateAttributeDataModel(typeof(ValidationAttribute), MatchByInheritance = true)]
readonly partial record struct ValidationAttributeData(
	[AttributeProperty] string? ErrorMessage,
	[AttributeProperty] string? ErrorMessageResourceName,
	[AttributeProperty] ITypeSymbol? ErrorMessageResourceType
);

[GenerateAttributeDataModel(typeof(RequiredAttribute))]
readonly partial record struct RequiredAttributeData(
	[AttributeProperty] bool AllowEmptyStrings,
	[AttributeProperty(Source = AttributePropertySource.NestedModel)]
		ValidationAttributeData ValidationAttribute
);

[GenerateAttributeDataModel(typeof(CompareAttribute))]
readonly partial record struct CompareAttributeData(
	[AttributeProperty(
		Source = AttributePropertySource.ConstructorName,
		Name = "otherProperty",
		DefaultValue = ""
	)]
		string OtherProperty,
	[AttributeProperty] string? OtherPropertyDisplayName,
	[AttributeProperty(Source = AttributePropertySource.NestedModel)]
		ValidationAttributeData ValidationAttribute
);

[GenerateAttributeDataModel(typeof(DisplayAttribute))]
readonly partial record struct DisplayAttributeData(
	[AttributeProperty] string? ShortName,
	[AttributeProperty] string? Name,
	[AttributeProperty] string? Description,
	[AttributeProperty] string? Prompt,
	[AttributeProperty] ITypeSymbol? ResourceType,
	[AttributeProperty] bool AutoGenerateField,
	[AttributeProperty] bool AutoGenerateFilter,
	[AttributeProperty] int Order
);

[GenerateAttributeDataModel(typeof(EmailAddressAttribute))]
readonly partial record struct EmailAddressAttributeData(
	[AttributeProperty(Source = AttributePropertySource.NestedModel)]
		ValidationAttributeData ValidationAttributeData
);

[GenerateAttributeDataModel(typeof(CreditCardAttribute))]
readonly partial record struct CreditCardAttributeData(
	[AttributeProperty(Source = AttributePropertySource.NestedModel)]
		ValidationAttributeData ValidationAttribute
);

[GenerateAttributeDataModel(typeof(System.ComponentModel.DataAnnotations.PhoneAttribute))]
readonly partial record struct PhoneAttribute(
	[AttributeProperty(Source = AttributePropertySource.NestedModel)]
		ValidationAttributeData ValidationAttribute
);

[GenerateAttributeDataModel(typeof(System.ComponentModel.DataAnnotations.UrlAttribute))]
readonly partial record struct UrlAttribute(
	[AttributeProperty(Source = AttributePropertySource.NestedModel)]
		ValidationAttributeData ValidationAttribute
);

[GenerateAttributeDataModel(typeof(System.ComponentModel.DataAnnotations.StringLengthAttribute))]
readonly partial record struct StringLengthAttribute(
	[AttributeProperty(
		Source = AttributePropertySource.ConstructorName,
		Name = "maximumLength",
		DefaultValue = int.MaxValue
	)]
		int MaximumLength,
	[AttributeProperty] int MinimumLength,
	[AttributeProperty(Source = AttributePropertySource.NestedModel)]
		ValidationAttributeData ValidationAttribute
);

[GenerateAttributeDataModel(typeof(MinLengthAttribute))]
readonly partial record struct MinLengthAttributeData(
	[AttributeProperty(Source = AttributePropertySource.ConstructorName, Name = "length")]
		int Length,
	[AttributeProperty(Source = AttributePropertySource.NestedModel)]
		ValidationAttributeData ValidationAttribute
);

[GenerateAttributeDataModel(typeof(MaxLengthAttribute))]
readonly partial record struct MaxLengthAttributeData(
	[AttributeProperty(Source = AttributePropertySource.ConstructorName, Name = "length")]
		int Length,
	[AttributeProperty(Source = AttributePropertySource.NestedModel)]
		ValidationAttributeData ValidationAttribute
);

[GenerateAttributeDataModel(typeof(RegularExpressionAttribute))]
readonly partial record struct RegularExpressionAttributeData(
	[AttributeProperty(Source = AttributePropertySource.ConstructorName, Name = "pattern")]
		string? Pattern,
	[AttributeProperty(Source = AttributePropertySource.NamedArgument)]
		int MatchTimeoutInMilliseconds,
	[AttributeProperty(Source = AttributePropertySource.NestedModel)]
		ValidationAttributeData ValidationAttribute
);

[GenerateAttributeDataModel("System.ComponentModel.DataAnnotations.AllowedValuesAttribute")]
readonly partial record struct AllowedValuesAttributeData(
	[AttributeProperty(Source = AttributePropertySource.ConstructorIndex, Index = 0)]
		ImmutableArray<TypedConstant> Values,
	[AttributeProperty(Source = AttributePropertySource.NestedModel)]
		ValidationAttributeData ValidationAttribute
);

[GenerateAttributeDataModel("System.ComponentModel.DataAnnotations.Base64StringAttribute")]
readonly partial record struct Base64StringAttributeData(
	[AttributeProperty(Source = AttributePropertySource.NestedModel)]
		ValidationAttributeData ValidationAttribute
);

[GenerateAttributeDataModel("System.ComponentModel.DataAnnotations.DeniedValuesAttribute")]
readonly partial record struct DeniedValuesAttributeData(
	[AttributeProperty(Source = AttributePropertySource.ConstructorIndex, Index = 0)]
		ImmutableArray<TypedConstant> Values,
	[AttributeProperty(Source = AttributePropertySource.NestedModel)]
		ValidationAttributeData ValidationAttribute
);

[GenerateAttributeDataModel("System.ComponentModel.DataAnnotations.LengthAttribute")]
readonly partial record struct LengthAttributeData(
	[AttributeProperty(Source = AttributePropertySource.ConstructorIndex, Index = 0)]
		int MinimumLength,
	[AttributeProperty(
		Source = AttributePropertySource.ConstructorIndex,
		Index = 1,
		DefaultValue = int.MaxValue
	)]
		int MaximumLength,
	[AttributeProperty(Source = AttributePropertySource.NestedModel)]
		ValidationAttributeData ValidationAttribute
);
