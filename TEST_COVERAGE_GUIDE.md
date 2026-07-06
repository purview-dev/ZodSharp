# ZodSharp Test Coverage Guide

This document outlines comprehensive test coverage requirements for the ZodSharp Examples CLI project and the ZodSharp Source Generator to ensure all functionality is properly validated.

## Table of Contents

1. [ZodSharp.Examples.CLI Tests](#zodsharpexamplescli-tests)
2. [ZodSharp Source Generator Tests](#zodsharpsourcegenerator-tests)
3. [Integration Tests](#integration-tests)

---

## ZodSharp.Examples.CLI Tests

The Examples CLI demonstrates core ZodSharp functionality and advanced validation patterns. All features demonstrated should have comprehensive test coverage.

### 1. Basic Validation Tests (from Program.cs)

These tests verify fundamental schema creation and validation operations:

#### 1.1 String Validation
- **String.Min()**: Validate minimum length constraints
  - ✓ Valid: "John" with Min(3)
  - ✗ Invalid: "AB" with Min(3)
  - Edge case: Empty string "" with Min(0)
  - Edge case: Exactly min length

- **String.Max()**: Validate maximum length constraints
  - ✓ Valid: "John" with Max(50)
  - ✗ Invalid: String exceeding Max(50)
  - Edge case: Exactly max length
  - Edge case: Very long strings (>1000 chars)

- **String.Email()**: Email validation
  - ✓ Valid: "user@example.com", "first.last+tag@domain.co.uk"
  - ✗ Invalid: "invalid", "user@", "@example.com"
  - Edge cases: Internationalized emails, special characters

#### 1.2 Number Validation
- **Number.Min()**: Validate minimum value
  - ✓ Valid: 25 with Min(0)
  - ✗ Invalid: -1 with Min(0)
  - Edge cases: Negative numbers, zero, floating points

- **Number.Max()**: Validate maximum value
  - ✓ Valid: 25 with Max(120)
  - ✗ Invalid: 121 with Max(120)
  - Edge cases: Large numbers, decimals

- **Number.Int()**: Enforce integer type
  - ✓ Valid: 25.0 (exactly integer)
  - ✗ Invalid: 25.5 (has decimal)
  - Edge cases: Very large integers, negative integers

#### 1.3 Array Validation
- **Array.Min()**: Minimum array length
  - ✓ Valid: [1, 2, 3] with Min(1)
  - ✗ Invalid: [] with Min(1)
  - Edge case: Single element array with Min(1)

- **Array.Max()**: Maximum array length
  - ✓ Valid: [1, 2, 3] with Max(10)
  - ✗ Invalid: Array with 11+ elements with Max(10)
  - Edge case: Exactly max length

- **Array Element Type Validation**: Elements must match inner schema
  - ✓ Valid: Z.Array(Z.Number()) with numeric values
  - ✗ Invalid: Z.Array(Z.Number()) with mixed types
  - Edge cases: Empty arrays, nested arrays

#### 1.4 Object/Dictionary Validation
- **Object Field Validation**: Each field must match its schema
  - ✓ Valid: Complete object with all required fields
  - ✗ Invalid: Missing required fields
  - ✗ Invalid: Wrong field types

- **Object Field Types**: 
  - ✓ Valid: String field "name", Number field "age", Email field
  - ✗ Invalid: Wrong types for each field
  - Edge cases: null values, empty strings, zero values

#### 1.5 Optional Validation
- **Optional with null**: Should accept null
  - ✓ Valid: null with Z.Optional(Z.String())
  - ✓ Valid: "value" with Z.Optional(Z.String())
  - ✗ Invalid: null with Z.String() (not optional)

#### 1.6 Parse vs Validate
- **validate()**: Returns Result object
  - ✓ Returns Result with IsSuccess and Errors
  - ✓ Non-throwing for invalid data

- **Parse()**: Throws ZodException
  - ✓ Returns validated value on success
  - ✗ Throws ZodException on invalid data
  - Exception should contain error details

### 2. Advanced String Methods (from AdvancedExamples.cs)

#### 2.1 URL Validation
- **String.Url()**
  - ✓ Valid: "https://example.com", "http://sub.domain.co.uk/path"
  - ✗ Invalid: "not-a-url", "ftp://example.com" (protocol varies)
  - Edge cases: URLs with query params, fragments, auth

#### 2.2 UUID Validation
- **String.Uuid()**
  - ✓ Valid: "550e8400-e29b-41d4-a716-446655440000"
  - ✗ Invalid: "not-a-uuid", "550e8400-e29b-41d4-a716"
  - Edge cases: Different UUID versions (v1, v4, v5)

#### 2.3 String Pattern Methods
- **String.StartsWith()**
  - ✓ Valid: "https://example.com" with StartsWith("https://")
  - ✗ Invalid: "http://example.com" with StartsWith("https://")
  - Edge case: Empty prefix

- **String.EndsWith()**
  - ✓ Valid: "example.com" with EndsWith(".com")
  - ✗ Invalid: "example.org" with EndsWith(".com")
  - Edge case: Empty suffix

#### 2.4 String Transformation Methods
- **String.Trim()**
  - ✓ Input: "  hello  " → Output: "hello"
  - ✓ Works with tabs and other whitespace
  - Edge case: Already trimmed strings

- **String.ToUpper()**
  - ✓ Input: "hello" → Output: "HELLO"
  - ✓ Unicode handling
  - Edge case: Already uppercase

- **String.ToLower()**
  - ✓ Input: "HELLO" → Output: "hello"
  - ✓ Unicode handling
  - Edge case: Already lowercase

- **String.Length()**
  - ✓ Exact length validation: "1234567890" with Length(10)
  - ✗ Invalid: "123" with Length(10)
  - ✗ Invalid: "12345678901" with Length(10)

### 3. Advanced Number Methods

#### 3.1 Number Sign Validation
- **Number.Positive()**
  - ✓ Valid: 10, 0.1, 1000000
  - ✗ Invalid: -1, -0.5, 0 (depends on implementation)
  - Edge case: Very small positive numbers

- **Number.Negative()**
  - ✓ Valid: -5, -0.1
  - ✗ Invalid: 5, 1, 0

- **Number.MultipleOf()**
  - ✓ Valid: 30 with MultipleOf(10)
  - ✗ Invalid: 25 with MultipleOf(10)
  - Edge cases: Floating point multiples, zero

#### 3.2 Number Special Values
- **Number.Finite()**
  - ✓ Valid: 42, -1000, 0.5
  - ✗ Invalid: double.Infinity, double.NegativeInfinity
  - Edge case: Subnormal numbers

- **Number.Safe()**
  - ✓ Valid: 2147483647 (max safe integer)
  - ✗ Invalid: 9007199254740992 (exceeds safe range)
  - ✗ Invalid: Negative unsafe values

### 4. Transform Examples

#### 4.1 Single Transform
- **String Transform**: String.Transform(s => s.ToUpperInvariant())
  - ✓ Input: "hello" → Output: "hello" transformed to uppercase
  - ✓ Value is transformed in result

- **Number Transform**: Number.Transform(n => n * 2)
  - ✓ Input: 5 → Output: 10
  - Edge case: Zero, negative numbers

#### 4.2 Chained Transforms
- **Multiple transforms chained**: Trim().Transform().ToLower()
  - ✓ "  HELLO  " → "hello"
  - ✓ Order of operations matters
  - Edge case: Empty strings

### 5. Refinement Examples

#### 5.1 Simple Refinement
- **Number.Refine() for even numbers**
  - ✓ Valid: 4 with Refine(n => n % 2 == 0, "Must be even")
  - ✗ Invalid: 5 with same refinement
  - Custom error message validation

#### 5.2 Complex Refinement (Password Validation)
- **Password schema with multiple refinements**
  - ✓ Valid: "Password123" (has upper, lower, digit, min length)
  - ✗ Invalid: "password" (no uppercase)
  - ✗ Invalid: "PASSWORD123" (no lowercase)
  - ✗ Invalid: "Password" (no digit)
  - Error messages should be specific to each refinement

### 6. Discriminated Union Examples

#### 6.1 Union Type Discrimination
- **DiscriminatedUnion("type")**
  - ✓ User type: { type: "user", name: "John" }
  - ✓ Admin type: { type: "admin", name: "Admin", permissions: [...] }
  - ✗ Invalid: Missing discriminator field
  - ✗ Invalid: Unknown discriminator value
  - Validation should route to correct schema

### 7. Lazy Evaluation Examples

#### 7.1 Recursive Schemas
- **Z.Lazy() for recursive structures**
  - ✓ Nested categories with subcategories
  - ✓ Multiple levels of nesting
  - ✗ Circular reference detection (if applicable)
  - Performance: Deep nesting shouldn't cause stack overflow

### 8. Span Validation Examples

#### 8.1 Span<T> Support
- **ValidateSpan() for ReadOnlySpan<char>**
  - ✓ "user@example.com".AsSpan() validation
  - ✓ Works with all string validators
  - Edge case: Very long spans, empty spans
  - Performance: Span validation should be efficient

### 9. Compiled Validator Examples

#### 9.1 CompiledValidator.Compile()
- **Compiled validation**
  - ✓ Validation result matches non-compiled version
  - ✓ Multiple validations with compiled validator
  - Edge case: Compiled validator reuse

#### 9.2 CompiledValidator.CompileParser()
- **Compiled parser**
  - ✓ Returns parsed value on success
  - ✗ Throws ZodException on failure
  - Error messages preserved from compilation

### 10. JSON Integration Examples

#### 10.1 JSON Deserialization and Validation
- **schema.DeserializeAndValidate<T>(json)**
  - ✓ Valid JSON deserializes and validates successfully
  - ✗ Invalid JSON fails at parsing
  - ✗ Invalid schema fails at validation
  - Edge cases: null fields, empty objects, malformed JSON

#### 10.2 JSON Converter Integration
- **CreateValidatingConverter<T>()**
  - ✓ Newtonsoft.Json integration
  - ✓ Validation happens during deserialization
  - ✗ Invalid data throws during conversion
  - Settings integration

### 11. Default Value Examples

#### 11.1 Default Values
- **Schema.Default(value)**
  - ✓ null input with default → returns default value
  - ✓ Non-null input → returns input
  - Edge case: Empty values handling

#### 11.2 Validated Defaults
- **Schema with Min() and Default()**
  - ✓ Default must satisfy schema constraints
  - Default value validation during schema construction

### 12. Schema Caching Examples

#### 12.1 Schema Cache Operations
- **SchemaCache.GetOrCreate(key, factory)**
  - ✓ First call invokes factory
  - ✓ Subsequent calls return cached instance
  - ✓ SchemaCache.TryGet() finds cached schema

- **Cache Key Management**
  - ✓ Different keys create different schemas
  - ✓ Cache isolation between tests

### 13. Source Generator Examples (User class)

#### 13.1 Generated Schema Usage
- **UserSchema.Validate(user)**
  - ✓ Valid user validates successfully
  - ✗ Invalid user fails with appropriate errors
  - Generated code should follow all constraints

- **UserSchema.Parse(user)**
  - ✓ Valid user parses successfully
  - ✗ Invalid user throws ZodException

#### 13.2 Generated Schema Validation Rules
- Name: Required, StringLength(50, MinimumLength = 3)
  - ✓ Valid: 3-50 character string
  - ✗ Invalid: null
  - ✗ Invalid: < 3 characters
  - ✗ Invalid: > 50 characters

- Age: Required, Range(0, 120)
  - ✓ Valid: 0-120
  - ✗ Invalid: -1
  - ✗ Invalid: 121
  - ✗ Invalid: null

- Email: Optional, EmailAddress format
  - ✓ Valid: null, "user@example.com"
  - ✗ Invalid: "not-an-email"
  - Edge case: null email, empty email

---

## ZodSharp Source Generator Tests

The source generator transforms classes decorated with `[ZodSchema]` into validators. Comprehensive testing ensures correct code generation and validation logic.

### 1. Generator Initialization Tests

#### 1.1 Post-Initialization Outputs
- **EmbeddedAttribute Definition**
  - ✓ Adds EmbeddedAttribute to compilation
  - ✓ Handles cases where attribute already exists
  - ✓ Correct naming and accessibility

- **ZodSchemaAttribute Definition**
  - ✓ Attribute is added to compilation
  - ✓ Attribute can be used in target code

#### 1.2 Incremental Generator Setup
- **Syntax Target Identification**
  - ✓ Identifies ClassDeclarationSyntax
  - ✓ Identifies StructDeclarationSyntax
  - ✓ Identifies RecordDeclarationSyntax
  - ✗ Ignores non-target types

### 2. Target Detection Tests

#### 2.1 Attribute Detection
- **[ZodSchema] attribute detection**
  - ✓ Finds classes with [ZodSchema]
  - ✓ Finds structs with [ZodSchema]
  - ✓ Finds records with [ZodSchema]
  - ✗ Ignores classes without attribute

#### 2.2 Semantic Symbol Resolution
- **Symbol resolution for decorated types**
  - ✓ Resolves INamedTypeSymbol correctly
  - ✓ Handles nested types
  - ✓ Handles generic types (if applicable)
  - ✗ Handles invalid types gracefully

### 3. DataAnnotations Attribute Processing Tests

#### 3.1 Required Attribute
- **[Required] attribute**
  - ✓ Generates validation code for required fields
  - ✓ Non-nullable value types are implicitly required
  - ✓ Nullable reference types without [Required] are optional

#### 3.2 StringLength Attribute
- **[StringLength(max, MinimumLength = min)]**
  - ✓ Generates Min() and Max() calls
  - ✓ Handles max-only specification
  - ✓ Handles both min and max
  - ✓ Generated code validates both constraints

#### 3.3 Range Attribute
- **[Range(min, max)]**
  - ✓ Generates Min() and Max() for numbers
  - ✓ Handles integer ranges
  - ✓ Handles decimal ranges
  - ✓ Generated validation applies both bounds

#### 3.4 EmailAddress Attribute
- **[EmailAddress]**
  - ✓ Generates Email() validation
  - ✓ Applied only to string properties
  - ✓ Email format validation is included

#### 3.5 MinLength Attribute
- **[MinLength(length)]**
  - ✓ Generates Min(length) for collections
  - ✓ Generates Min(length) for strings
  - ✓ Compatible with [StringLength]

#### 3.6 MaxLength Attribute
- **[MaxLength(length)]**
  - ✓ Generates Max(length) for collections
  - ✓ Generates Max(length) for strings
  - ✓ Compatible with [StringLength]

### 4. Schema Class Generation Tests

#### 4.1 Generated Class Structure
- **{ClassName}Schema.g.cs file generation**
  - ✓ File is created with correct name
  - ✓ Generated file contains auto-generated header
  - ✓ File is in correct namespace
  - ✓ File uses #nullable enable

#### 4.2 Generated Class Accessibility
- **Access modifier matches source class**
  - ✓ Public class → Public schema
  - ✓ Internal class → Internal schema
  - ✓ Private class → Private schema
  - ✓ Sealed classes handled correctly

#### 4.3 Generated Class Attributes
- **GeneratedCode attribute**
  - ✓ Applied to generated class
  - ✓ Contains correct assembly name
  - ✓ Contains correct version

- **CompilerGenerated attribute**
  - ✓ Applied to generated class

- **ExcludeFromCodeCoverage attribute**
  - ✓ Applied to generated class

### 5. Property Processing Tests

#### 5.1 Property Detection
- **Public property detection**
  - ✓ Detects public auto-properties
  - ✓ Detects public properties with getters/setters
  - ✗ Ignores private properties
  - ✗ Ignores static properties
  - ✓ Counts properties accurately for logging

#### 5.2 Property Type Mapping
- **Type to schema mapping**
  - ✓ string → Z.String()
  - ✓ int/long/decimal → Z.Number()
  - ✓ bool → Z.Boolean()
  - ✓ T? (nullable) → Z.Optional()
  - ✓ Collection types → Z.Array()
  - ✓ Custom types (if applicable)

#### 5.3 Nullable Reference Types
- **Nullable string handling**
  - ✓ string → Required string
  - ✓ string? → Optional string
  - ✓ Generated code reflects nullability

#### 5.4 Nullable Value Types
- **int? handling**
  - ✓ int → Required Z.Number().Int()
  - ✓ int? → Optional Z.Number().Int()
  - ✓ Default values considered

### 6. Validation Code Generation Tests

#### 6.1 Validate Method Generation
- **Validate(T value) method**
  - ✓ Method is generated
  - ✓ Returns Result<T>
  - ✓ Applies all attribute-based validations
  - ✓ Proper error reporting

#### 6.2 Parse Method Generation
- **Parse(T value) method**
  - ✓ Method is generated
  - ✓ Returns T on success
  - ✓ Throws ZodException on failure
  - ✓ Exception contains error details

#### 6.3 Schema Property Generation
- **Static Schema property**
  - ✓ Generated and accessible
  - ✓ Returns correct schema instance
  - ✓ Can be used for custom validation

### 7. Error Handling and Diagnostics Tests

#### 7.1 Diagnostic Reporting
- **Invalid class configuration**
  - ✓ Appropriate diagnostic reported
  - ✓ Error location points to source
  - ✓ Message is informative

#### 7.2 Unhandled Exception Handling
- **Generator crashes gracefully**
  - ✓ Exception caught
  - ✓ Diagnostic reported to user
  - ✓ Class name and error message included

#### 7.3 Disabled Generator
- **Source generator disabled**
  - ✓ IsSourceGeneratorDisabled check honored
  - ✓ No code generated when disabled
  - ✓ No diagnostic errors

### 8. Logging Support Tests

#### 8.1 Execution Logging
- **GenerationLogger integration**
  - ✓ Schema name logged
  - ✓ Modifier logged
  - ✓ Namespace logged
  - ✓ Property count logged
  - ✓ Multiple log levels (Info, etc.)

#### 8.2 Debug Support
- **Logging disabled in release**
  - ✓ No performance impact when disabled
  - ✓ Conditional logging works

### 9. Code Generation Quality Tests

#### 9.1 Performance Considerations
- **StringBuilder capacity**
  - ✓ Estimated capacity based on property count
  - ✓ Avoids excessive reallocations
  - ✓ Reasonable for typical class sizes

#### 9.2 Generated Code Quality
- **Valid C# output**
  - ✓ Generated code compiles
  - ✓ No compilation warnings
  - ✓ Proper using statements
  - ✓ Correct namespace declaration

#### 9.3 Usings and Imports
- **Required using statements**
  - ✓ System namespace
  - ✓ System.Collections.Generic
  - ✓ System.Collections.Immutable
  - ✓ ZodSharp.Core

### 10. Validator Helpers Tests

#### 10.1 TypeHelpers
- **AccessibilityKeyword determination**
  - ✓ Maps ISymbol accessibility to C# keywords
  - ✓ Handles all accessibility levels
  - ✓ Generates valid keywords

- **Type categorization**
  - ✓ Identifies collection types
  - ✓ Identifies nullable types
  - ✓ Identifies value vs reference types

#### 10.2 CodeGenHelpers
- **Attribute generation**
  - ✓ GeneratedCode attribute format
  - ✓ ConditionalAttribute format
  - ✓ CompilerGenerated attribute format
  - ✓ EmbeddedAttribute format
  - ✓ ExcludeFromCodeCoverage format

- **Indentation handling**
  - ✓ Proper tab-based caching
  - ✓ Multiple indentation levels
  - ✓ Cached attributes with correct indentation

### 11. Complex Type Tests

#### 11.1 Nested Classes
- **Nested class with [ZodSchema]**
  - ✓ Schema generated correctly
  - ✓ Namespace reflects nesting (if applicable)
  - ✓ Access modifiers respected

#### 11.2 Generic Classes (if supported)
- **Generic<T> with [ZodSchema]**
  - ✓ Type parameters handled
  - ✓ Generated schema for generic constraints
  - ✓ Valid generated code

#### 11.3 Record Types
- **record class with [ZodSchema]**
  - ✓ Record properties handled
  - ✓ Primary constructor properties detected
  - ✓ Schema validates records correctly

#### 11.4 Struct Types
- **struct with [ZodSchema]**
  - ✓ Struct properties handled
  - ✓ Value type semantics preserved
  - ✓ Schema validates structs correctly

### 12. Collections and Complex Properties Tests

#### 12.1 Array Properties
- **Property of type T[]**
  - ✓ Generates Z.Array(schema) correctly
  - ✓ [MinLength] and [MaxLength] applied
  - ✓ Element type schema generated

#### 12.2 List<T> Properties
- **Property of type List<T>**
  - ✓ Generates Z.Array(schema) correctly
  - ✓ Collection constraints applied
  - ✓ Element type validated

#### 12.3 IEnumerable Properties
- **Property of type IEnumerable<T>**
  - ✓ Handled appropriately
  - ✓ Element type validation

---

## Integration Tests

### 1. End-to-End Generation Tests

#### 1.1 Full Compilation Cycle
- **Class → Attribute → Generation → Validation**
  - ✓ Project compiles successfully
  - ✓ Generated code is syntactically valid
  - ✓ Generated schema is usable
  - ✓ Validation produces expected results

#### 1.2 Multiple Classes
- **Multiple [ZodSchema] classes in same project**
  - ✓ All schemas generated
  - ✓ No conflicts between schemas
  - ✓ Each schema validates its type correctly

### 2. Real-World Scenarios

#### 2.1 Mixed Valid/Invalid Data
- **Process batch of mixed data**
  - ✓ Valid items pass validation
  - ✗ Invalid items fail with clear errors
  - ✓ Error reporting is comprehensive

#### 2.2 Performance Scenarios
- **Large batch validation**
  - ✓ Compiled validators improve performance
  - ✓ Memory usage is reasonable
  - ✓ No memory leaks

### 3. Error Recovery Tests

#### 3.1 Validation Failure Recovery
- **After validation failure**
  - ✓ Can validate other objects
  - ✓ Schema remains usable
  - ✓ No state corruption

### 4. Documentation Examples

#### 4.1 README Examples
- All code examples in documentation should pass tests
- Output should match expected behavior described

### 5. Regression Tests

#### 5.1 Known Issues
- Test any previously found and fixed issues
- Ensure bugs don't resurface

---

## Test Implementation Notes

### Test Framework
- Use xUnit, NUnit, or MSTest (whichever is standard for project)
- Organize tests by feature
- Use descriptive test names following pattern: `Feature_Scenario_ExpectedResult`

### Test Data
- Use realistic data for examples
- Include edge cases
- Include boundary values
- Include empty/null cases

### Assertion Strategy
- Assert on success/failure status
- Assert on error messages
- Assert on error locations (if applicable)
- Assert on transformed/parsed values

### Performance Baselines
- Establish performance baselines for validation
- Test compiled validators show improvement
- Monitor for performance regressions

### Code Generation Testing
- Verify generated code syntax
- Verify generated code compiles
- Verify generated code produces correct results
- Snapshot tests for generated code (optional but recommended)

---

## Continuous Integration Checklist

Before merging code:
- [ ] All unit tests pass
- [ ] All integration tests pass
- [ ] Code generation tests pass
- [ ] No new warnings in generated code
- [ ] No performance regressions
- [ ] Documentation examples all work
- [ ] Error messages are clear and helpful
