namespace ZodSharp.SourceGenerators.Helpers;

partial class TypeLibrary
{
	public static class DataAnnotations
	{
		public const string Namespace = "System.ComponentModel.DataAnnotations";

		public static readonly TypeValueObject DisplayAttribute = new(
			nameof(DisplayAttribute),
			Namespace
		);
		public static readonly TypeValueObject RequiredAttribute = new(
			nameof(RequiredAttribute),
			Namespace
		);

		public static readonly TypeValueObject EmailAddressAttribute = new(
			nameof(EmailAddressAttribute),
			Namespace
		);
		public static readonly TypeValueObject StringLengthAttribute = new(
			nameof(StringLengthAttribute),
			Namespace
		);
		public static readonly TypeValueObject MinLengthAttribute = new(
			nameof(MinLengthAttribute),
			Namespace
		);
		public static readonly TypeValueObject MaxLengthAttribute = new(
			nameof(MaxLengthAttribute),
			Namespace
		);
		public static readonly TypeValueObject RangeAttribute = new(
			nameof(RangeAttribute),
			Namespace
		);
		public static readonly TypeValueObject LengthAttribute = new(
			nameof(LengthAttribute),
			Namespace
		);
		public static readonly TypeValueObject RegularExpressionAttribute = new(
			nameof(RegularExpressionAttribute),
			Namespace
		);
		public static readonly TypeValueObject AllowedValuesAttribute = new(
			nameof(AllowedValuesAttribute),
			Namespace
		);
		public static readonly TypeValueObject DeniedValuesAttribute = new(
			nameof(DeniedValuesAttribute),
			Namespace
		);
		public static readonly TypeValueObject UrlAttribute = new(nameof(UrlAttribute), Namespace);
		public static readonly TypeValueObject PhoneAttribute = new(
			nameof(PhoneAttribute),
			Namespace
		);
		public static readonly TypeValueObject CreditCardAttribute = new(
			nameof(CreditCardAttribute),
			Namespace
		);
		public static readonly TypeValueObject CompareAttribute = new(
			nameof(CompareAttribute),
			Namespace
		);
		public static readonly TypeValueObject Base64StringAttribute = new(
			nameof(Base64StringAttribute),
			Namespace
		);

		// This is abstract and the base class to all the other validation attributes,
		// so we can use it to get the base properties
		public static readonly TypeValueObject ValidationAttribute = new(
			nameof(ValidationAttribute),
			Namespace
		);
	}
}
