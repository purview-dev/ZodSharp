// src/src/ZodSharp.NET.Schema/Types/Composition/CompositionSchemaBase.cs
using ZodSharp.NET.Schema.Types;

namespace ZodSharp.NET.Schema.Types.Composition
{
    /// <summary>
    /// Base abstract class for schemas supporting composition.
    /// Ensures adherence to IZodSchema and ICompositionSchema.
    /// </summary>
    public abstract class CompositionSchemaBase<T> : IZodSchema<T>, ICompositionSchema
    {
        public abstract T Parse(object input);
        public abstract T SafeParse(object input, out ErrorResult? errorResult);
        // Potentially other common IZodSchema methods or composition helpers defined here.
    }
}
