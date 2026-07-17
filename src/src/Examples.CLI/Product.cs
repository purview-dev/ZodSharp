namespace ZodSharp.Examples.CLI;

/// <summary>
/// A plain DTO with no <c>[ZodSchema]</c> attribute, so no source generator runs for it.
/// Demonstrated alongside <see cref="User"/> to show source-gen'd + hand-built schemas
/// coexisting in the same <c>IServiceProvider</c>.
/// </summary>
sealed class Product
{
	public string Sku { get; set; } = string.Empty;
	public double Price { get; set; }
	public int Stock { get; set; }
}
