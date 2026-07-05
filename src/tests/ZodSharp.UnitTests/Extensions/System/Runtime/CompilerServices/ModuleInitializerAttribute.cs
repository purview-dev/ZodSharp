namespace System.Runtime.CompilerServices;

#if NETSTANDARD2_1_OR_GREATER
[Diagnostics.CodeAnalysis.SuppressMessage("Design", "CA1018:Mark attributes with AttributeUsageAttribute")]
sealed class ModuleInitializerAttribute : Attribute { }
#endif
