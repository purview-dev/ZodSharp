using ZodSharp.Core;
using ZodSharp.Schemas;

namespace ZodSharp.JsonSchema;

/// <summary>
/// Options for converting ZodSharp schemas to JSON Schema.
/// </summary>
public class ToJsonSchemaOptions
{
    /// <summary>
    /// Whether to include the $schema property in the output.
    /// Default: true
    /// </summary>
    public bool IncludeSchema { get; set; } = true;
    
    /// <summary>
    /// Custom $id for the schema.
    /// </summary>
    public string? Id { get; set; }
    
    /// <summary>
    /// Custom title for the schema.
    /// </summary>
    public string? Title { get; set; }
}

/// <summary>
/// Converts ZodSharp schemas to JSON Schema (Draft 2020-12).
/// </summary>
public static class ToJsonSchemaConverter
{
    private const string SchemaUri = "https://json-schema.org/draft/2020-12/schema";
    
    /// <summary>
    /// Converts a ZodSharp schema to JSON Schema.
    /// </summary>
    /// <typeparam name="T">The schema output type</typeparam>
    /// <param name="schema">The ZodSharp schema to convert</param>
    /// <param name="options">Conversion options</param>
    /// <returns>A JSON Schema definition</returns>
    public static JsonSchemaDefinition Convert<T>(IZodSchema<T, T> schema, ToJsonSchemaOptions? options = null)
    {
        options ??= new ToJsonSchemaOptions();
        var context = new ConversionContext();
        
        var result = ConvertSchema(schema, context);
        
        // Add schema metadata
        if (options.IncludeSchema)
        {
            result.Schema = SchemaUri;
        }
        
        if (options.Id != null)
        {
            result.Id = options.Id;
        }
        
        if (options.Title != null)
        {
            result.Title = options.Title;
        }
        
        return result;
    }
    
    private class ConversionContext
    {
        public HashSet<object> Seen { get; } = new();
        public Dictionary<string, JsonSchemaDefinition> Defs { get; } = new();
        public int Counter { get; set; }
    }
    
    private static JsonSchemaDefinition ConvertSchema(object schema, ConversionContext ctx)
    {
        // Handle circular references
        if (ctx.Seen.Contains(schema))
        {
            var defId = $"__schema{ctx.Counter++}";
            return new JsonSchemaDefinition { Ref = $"#/$defs/{defId}" };
        }
        ctx.Seen.Add(schema);
        
        var result = schema switch
        {
            ZodString zodString => ConvertString(zodString),
            ZodNumber zodNumber => ConvertNumber(zodNumber),
            ZodBoolean => new JsonSchemaDefinition { Type = "boolean" },
            ZodNull => new JsonSchemaDefinition { Type = "null" },
            ZodObject zodObject => ConvertObject(zodObject, ctx),
            ZodOptional<object> zodOptional => ConvertOptional(zodOptional, ctx),
            ZodUnion zodUnion => ConvertUnion(zodUnion, ctx),
            _ => ConvertGeneric(schema, ctx)
        };
        
        // Add description if available
        if (schema is ZodType<object, object> zodType && zodType.Description != null)
        {
            result.Description = zodType.Description;
        }
        
        ctx.Seen.Remove(schema);
        return result;
    }
    
    private static JsonSchemaDefinition ConvertString(ZodString schema)
    {
        var result = new JsonSchemaDefinition { Type = "string" };
        
        // Extract rules from the schema using reflection (since rules are private)
        // We'll use the built-in rule inspection if available
        var schemaType = schema.GetType();
        var rulesField = schemaType.BaseType?.BaseType?.GetField("_rules", 
            System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
        
        if (rulesField?.GetValue(schema) is System.Collections.IEnumerable rules)
        {
            foreach (var rule in rules)
            {
                var ruleType = rule.GetType();
                var ruleName = ruleType.Name;
                
                switch (ruleName)
                {
                    case "MinLengthRule":
                        var minLengthField = ruleType.GetField("_minLength", 
                            System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
                        if (minLengthField?.GetValue(rule) is int minLength)
                            result.MinLength = minLength;
                        break;
                        
                    case "MaxLengthRule":
                        var maxLengthField = ruleType.GetField("_maxLength", 
                            System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
                        if (maxLengthField?.GetValue(rule) is int maxLength)
                            result.MaxLength = maxLength;
                        break;
                        
                    case "EmailRule":
                        result.Format = "email";
                        break;
                        
                    case "UrlRule":
                        result.Format = "uri";
                        break;
                        
                    case "UuidRule":
                        result.Format = "uuid";
                        break;
                        
                    case "RegexRule":
                        var patternField = ruleType.GetField("_pattern", 
                            System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
                        if (patternField?.GetValue(rule) is System.Text.RegularExpressions.Regex regex)
                            result.Pattern = regex.ToString();
                        break;
                }
            }
        }
        
        // Also check for description
        if (schema.Description != null)
        {
            result.Description = schema.Description;
        }
        
        return result;
    }
    
    private static JsonSchemaDefinition ConvertNumber(ZodNumber schema)
    {
        var result = new JsonSchemaDefinition { Type = "number" };
        
        // Extract rules from the schema
        var schemaType = schema.GetType();
        var rulesField = schemaType.BaseType?.BaseType?.GetField("_rules", 
            System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
        
        if (rulesField?.GetValue(schema) is System.Collections.IEnumerable rules)
        {
            foreach (var rule in rules)
            {
                var ruleType = rule.GetType();
                var ruleName = ruleType.Name;
                
                if (ruleName.StartsWith("MinValueRule"))
                {
                    var minValueField = ruleType.GetField("_minValue", 
                        System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
                    if (minValueField?.GetValue(rule) is double minValue)
                        result.Minimum = minValue;
                }
                else if (ruleName.StartsWith("MaxValueRule"))
                {
                    var maxValueField = ruleType.GetField("_maxValue", 
                        System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
                    if (maxValueField?.GetValue(rule) is double maxValue)
                        result.Maximum = maxValue;
                }
                else if (ruleName == "IntRule")
                {
                    result.Type = "integer";
                }
                else if (ruleName == "MultipleOfRule")
                {
                    var divisorField = ruleType.GetField("_divisor", 
                        System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
                    if (divisorField?.GetValue(rule) is double divisor)
                        result.MultipleOf = divisor;
                }
            }
        }
        
        if (schema.Description != null)
        {
            result.Description = schema.Description;
        }
        
        return result;
    }
    
    private static JsonSchemaDefinition ConvertObject(ZodObject schema, ConversionContext ctx)
    {
        var result = new JsonSchemaDefinition
        {
            Type = "object",
            Properties = new Dictionary<string, JsonSchemaDefinition>(),
            Required = new List<string>(),
            AdditionalProperties = false
        };
        
        // Get the shape from ZodObject using reflection
        var shapeField = typeof(ZodObject).GetField("_shape", 
            System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
        
        if (shapeField?.GetValue(schema) is System.Collections.Immutable.ImmutableDictionary<string, IZodSchema<object, object>> shape)
        {
            foreach (var (key, propSchema) in shape)
            {
                result.Properties[key] = ConvertSchema(propSchema, ctx);
                
                // Check if the property is optional
                var propSchemaType = propSchema.GetType();
                if (!propSchemaType.Name.StartsWith("ZodOptional"))
                {
                    result.Required.Add(key);
                }
            }
        }
        
        // Remove required array if empty
        if (result.Required.Count == 0)
        {
            result.Required = null;
        }
        
        return result;
    }
    
    private static JsonSchemaDefinition ConvertOptional<T>(ZodOptional<T> schema, ConversionContext ctx) where T : class
    {
        // Get inner schema
        var innerField = typeof(ZodOptional<T>).GetField("_innerSchema", 
            System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
        
        if (innerField?.GetValue(schema) is object innerSchema)
        {
            return ConvertSchema(innerSchema, ctx);
        }
        
        return new JsonSchemaDefinition();
    }
    
    private static JsonSchemaDefinition ConvertUnion(ZodUnion schema, ConversionContext ctx)
    {
        var result = new JsonSchemaDefinition
        {
            AnyOf = new List<JsonSchemaDefinition>()
        };
        
        // Get options from ZodUnion
        var optionsField = typeof(ZodUnion).GetField("_options", 
            System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
        
        if (optionsField?.GetValue(schema) is IZodSchema<object, object>[] options)
        {
            foreach (var option in options)
            {
                result.AnyOf.Add(ConvertSchema(option, ctx));
            }
        }
        
        return result;
    }
    
    private static JsonSchemaDefinition ConvertGeneric(object schema, ConversionContext ctx)
    {
        var schemaType = schema.GetType();
        var typeName = schemaType.Name;
        
        // Handle ZodArray<T>
        if (typeName.StartsWith("ZodArray"))
        {
            var result = new JsonSchemaDefinition { Type = "array" };
            
            // Get element schema
            var elementField = schemaType.GetField("_elementSchema", 
                System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
            
            if (elementField?.GetValue(schema) is object elementSchema)
            {
                result.Items = ConvertSchema(elementSchema, ctx);
            }
            
            // Get min/max from rules
            var rulesField = schemaType.BaseType?.BaseType?.GetField("_rules", 
                System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
            
            if (rulesField?.GetValue(schema) is System.Collections.IEnumerable rules)
            {
                foreach (var rule in rules)
                {
                    var ruleType = rule.GetType();
                    var ruleName = ruleType.Name;
                    
                    if (ruleName == "MinItemsRule")
                    {
                        var minField = ruleType.GetField("_minItems", 
                            System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
                        if (minField?.GetValue(rule) is int min)
                            result.MinItems = min;
                    }
                    else if (ruleName == "MaxItemsRule")
                    {
                        var maxField = ruleType.GetField("_maxItems", 
                            System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
                        if (maxField?.GetValue(rule) is int max)
                            result.MaxItems = max;
                    }
                }
            }
            
            return result;
        }
        
        // Handle ZodLiteral<T>
        if (typeName.StartsWith("ZodLiteral"))
        {
            var valueField = schemaType.GetField("_value", 
                System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
            
            if (valueField?.GetValue(schema) is object value)
            {
                var result = new JsonSchemaDefinition { Const = value };
                
                // Set type based on value type
                result.Type = value switch
                {
                    string => "string",
                    int or long or double or float => "number",
                    bool => "boolean",
                    null => "null",
                    _ => null
                };
                
                return result;
            }
        }
        
        // Handle ZodNullable<T>
        if (typeName.StartsWith("ZodNullable"))
        {
            var innerField = schemaType.GetField("_innerSchema", 
                System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
            
            if (innerField?.GetValue(schema) is object innerSchema)
            {
                var inner = ConvertSchema(innerSchema, ctx);
                return new JsonSchemaDefinition
                {
                    AnyOf = new List<JsonSchemaDefinition>
                    {
                        inner,
                        new JsonSchemaDefinition { Type = "null" }
                    }
                };
            }
        }
        
        // Handle ZodLazy<T>
        if (typeName.StartsWith("ZodLazy"))
        {
            // For lazy schemas, we need special handling to avoid infinite recursion
            // Return a $ref placeholder
            var defId = $"__lazy{ctx.Counter++}";
            return new JsonSchemaDefinition { Ref = $"#/$defs/{defId}" };
        }
        
        // Default: empty schema (any)
        return new JsonSchemaDefinition();
    }
}
