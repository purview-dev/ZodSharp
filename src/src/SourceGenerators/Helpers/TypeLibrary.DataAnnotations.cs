namespace ZodSharp.SourceGenerators.Helpers;

partial class TypeLibrary
{
	public static class DataAnnotations
	{
		public const string Namespace = "System.ComponentModel.DataAnnotations";

		public static readonly TypeIdentity DisplayAttribute = new(nameof(DisplayAttribute), Namespace);
		public static readonly TypeIdentity RequiredAttribute = new(nameof(RequiredAttribute), Namespace);

		public static readonly TypeIdentity EmailAddressAttribute = new(nameof(EmailAddressAttribute), Namespace);
		public static readonly TypeIdentity StringLengthAttribute = new(nameof(StringLengthAttribute), Namespace);
		public static readonly TypeIdentity MinLengthAttribute = new(nameof(MinLengthAttribute), Namespace);
		public static readonly TypeIdentity MaxLengthAttribute = new(nameof(MaxLengthAttribute), Namespace);
		public static readonly TypeIdentity RangeAttribute = new(nameof(RangeAttribute), Namespace);
		public static readonly TypeIdentity LengthAttribute = new(nameof(LengthAttribute), Namespace);
		public static readonly TypeIdentity RegularExpressionAttribute = new(
			nameof(RegularExpressionAttribute),
			Namespace
		);
		public static readonly TypeIdentity AllowedValuesAttribute = new(nameof(AllowedValuesAttribute), Namespace);
		public static readonly TypeIdentity DeniedValuesAttribute = new(nameof(DeniedValuesAttribute), Namespace);
		public static readonly TypeIdentity UrlAttribute = new(nameof(UrlAttribute), Namespace);
		public static readonly TypeIdentity PhoneAttribute = new(nameof(PhoneAttribute), Namespace);
		public static readonly TypeIdentity CreditCardAttribute = new(nameof(CreditCardAttribute), Namespace);
		public static readonly TypeIdentity CompareAttribute = new(nameof(CompareAttribute), Namespace);
		public static readonly TypeIdentity Base64StringAttribute = new(nameof(Base64StringAttribute), Namespace);

		// This is abstract and the base class to all the other validation attributes,
		// so we can use it to get the base properties
		public static readonly TypeIdentity ValidationAttribute = new(nameof(ValidationAttribute), Namespace);
	}
}
