using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Text;
using System.Threading;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Text;

namespace Tekly.SuperSerial.Generator
{
	/// <summary>
	/// Emits an ISuperSerialize implementation for every partial type marked [SuperSerializable], and one
	/// registry per assembly that registers those types, every [SerialName] type and every concrete type that can
	/// sit in a polymorphic field, so a fresh SuperSerializer can read them after one RegisterAll call.
	/// </summary>
	[Generator(LanguageNames.CSharp)]
	public sealed class SuperSerialGenerator : IIncrementalGenerator
	{
		public const string ATTRIBUTE = "Tekly.SuperSerial.Serialization.SuperSerializableAttribute";
		public const string OPTIONAL_ATTRIBUTE = "Tekly.SuperSerial.Serialization.SerialOptionalAttribute";
		public const string SERIAL_NAME_ATTRIBUTE = "Tekly.SuperSerial.Serialization.SerialNameAttribute";
		public const string SUPER_SERIALIZE = "Tekly.SuperSerial.Serialization.ISuperSerialize";
		public const string SERIALIZE_FIELD_ATTRIBUTE = "UnityEngine.SerializeField";
		public const string NON_SERIALIZED_ATTRIBUTE = "System.NonSerializedAttribute";

		public void Initialize(IncrementalGeneratorInitializationContext context)
		{
			var types = context.SyntaxProvider.ForAttributeWithMetadataName(
				ATTRIBUTE,
				static (node, _) => node is ClassDeclarationSyntax || node is StructDeclarationSyntax,
				static (ctx, cancellation) => TypeAnalysis.Analyze(ctx, cancellation));

			context.RegisterSourceOutput(types, static (ctx, type) => {
				foreach (var diagnostic in type.Diagnostics) {
					ctx.ReportDiagnostic(diagnostic.Create());
				}

				if (type.Source != null) {
					ctx.AddSource(type.HintName, SourceText.From(type.Source, Encoding.UTF8));
				}
			});

			var registry = types.Collect().Combine(context.CompilationProvider);

			context.RegisterSourceOutput(registry, static (ctx, pair) => {
				var source = RegistryEmitter.Emit(pair.Right, pair.Left, ctx.CancellationToken);
				if (source != null) {
					ctx.AddSource("SuperSerialRegistry.g.cs", SourceText.From(source, Encoding.UTF8));
				}
			});
		}
	}

	/// <summary>
	/// A diagnostic that can live in the incremental cache. Location is kept as a span so equality is by value.
	/// </summary>
	public sealed class DiagnosticInfo : IEquatable<DiagnosticInfo>
	{
		public readonly DiagnosticDescriptor Descriptor;
		public readonly string FilePath;
		public readonly TextSpan Span;
		public readonly LinePositionSpan LineSpan;
		public readonly string[] Args;

		public DiagnosticInfo(DiagnosticDescriptor descriptor, Location location, params string[] args)
		{
			Descriptor = descriptor;
			Args = args;

			if (location != null && location.SourceTree != null) {
				FilePath = location.SourceTree.FilePath;
				Span = location.SourceSpan;
				LineSpan = location.GetLineSpan().Span;
			}
		}

		public Diagnostic Create()
		{
			var location = FilePath == null ? Location.None : Location.Create(FilePath, Span, LineSpan);
			return Diagnostic.Create(Descriptor, location, Args.Cast<object>().ToArray());
		}

		public bool Equals(DiagnosticInfo other)
		{
			return other != null && Descriptor.Id == other.Descriptor.Id && FilePath == other.FilePath && Span == other.Span && Args.SequenceEqual(other.Args);
		}

		public override bool Equals(object obj) => Equals(obj as DiagnosticInfo);
		public override int GetHashCode() => Descriptor.Id.GetHashCode() ^ Span.GetHashCode();
	}

	/// <summary>
	/// Everything the registry needs to know about one [SuperSerializable] type, plus its emitted source.
	/// Equality is by value so the incremental pipeline can skip unchanged types.
	/// </summary>
	public sealed class GeneratedType : IEquatable<GeneratedType>
	{
		public string HintName;
		public string Source;
		public string TypeName;
		public string MetadataName;
		public bool IsStruct;
		public bool IsConcrete;
		public bool IsAccessible;
		public List<string> PolymorphicDeclaredTypes = new List<string>();
		public List<DiagnosticInfo> Diagnostics = new List<DiagnosticInfo>();

		public bool Equals(GeneratedType other)
		{
			return other != null
			       && HintName == other.HintName
			       && Source == other.Source
			       && TypeName == other.TypeName
			       && MetadataName == other.MetadataName
			       && IsStruct == other.IsStruct
			       && IsConcrete == other.IsConcrete
			       && IsAccessible == other.IsAccessible
			       && PolymorphicDeclaredTypes.SequenceEqual(other.PolymorphicDeclaredTypes)
			       && Diagnostics.SequenceEqual(other.Diagnostics);
		}

		public override bool Equals(object obj) => Equals(obj as GeneratedType);
		public override int GetHashCode() => (HintName ?? "").GetHashCode();
	}

	/// <summary>
	/// How the generator reads and writes one field.
	/// </summary>
	public enum FieldKind
	{
		Bool,
		SignedVarint, // sbyte, short, int, long
		UnsignedVarint, // byte, ushort, uint, ulong, char
		Float,
		Double,
		DateTime,
		String,
		Enum,
		Vector2,
		Vector3,
		Quaternion,
		NestedStruct, // a struct implementing ISuperSerialize
		NestedSealedClass, // a sealed [SuperSerializable] class
		Boxed, // everything else, through WriteValue and ReadPayload
	}

	public sealed class FieldModel
	{
		public IFieldSymbol Symbol;
		public string Name;
		public string SerialName;
		public uint Hash;
		public string TypeName;
		public ITypeSymbol Type;
		public ITypeSymbol ValueType; // the type inside a Nullable<T>, else Type
		public bool IsNullable;
		public FieldKind Kind;
		public bool Omit;
		public bool OmitOnlyNull;

		/// <summary>
		/// The C# expression the field is reset to at the top of Read, or null when the initializer isn't
		/// something the generator can reproduce.
		/// </summary>
		public string DefaultExpression;
		public bool HasNonConstantInitializer;
		public string InitializerText;
		public Location Location;
	}

	public static class TypeAnalysis
	{
		public static readonly SymbolDisplayFormat TypeFormat = SymbolDisplayFormat.FullyQualifiedFormat
			.WithGlobalNamespaceStyle(SymbolDisplayGlobalNamespaceStyle.Omitted)
			.WithMiscellaneousOptions(SymbolDisplayMiscellaneousOptions.UseSpecialTypes | SymbolDisplayMiscellaneousOptions.ExpandNullable);

		public static readonly SymbolDisplayFormat MemberFormat = TypeFormat
			.WithMemberOptions(SymbolDisplayMemberOptions.IncludeContainingType);

		public static readonly SymbolDisplayFormat NullableTypeFormat = SymbolDisplayFormat.FullyQualifiedFormat
			.WithGlobalNamespaceStyle(SymbolDisplayGlobalNamespaceStyle.Omitted)
			.WithMiscellaneousOptions(SymbolDisplayMiscellaneousOptions.UseSpecialTypes);

		public static GeneratedType Analyze(GeneratorAttributeSyntaxContext context, CancellationToken cancellation)
		{
			var result = new GeneratedType();
			var symbol = (INamedTypeSymbol) context.TargetSymbol;
			var syntax = (TypeDeclarationSyntax) context.TargetNode;
			var compilation = context.SemanticModel.Compilation;

			result.TypeName = symbol.ToDisplayString(TypeFormat);
			result.MetadataName = MetadataName(symbol);
			result.HintName = result.TypeName.Replace('<', '_').Replace('>', '_').Replace(',', '_').Replace(' ', '_') + ".SuperSerial.g.cs";
			result.IsStruct = symbol.TypeKind == TypeKind.Struct;
			result.IsConcrete = !symbol.IsAbstract && !symbol.IsStatic;
			result.IsAccessible = IsAccessible(symbol);

			var typeLocation = syntax.Identifier.GetLocation();
			var ok = true;

			if (symbol.IsGenericType || symbol.ContainingType != null && ContainingTypesAreGeneric(symbol)) {
				result.Diagnostics.Add(new DiagnosticInfo(Diagnostics.GenericType, typeLocation, result.TypeName));
				ok = false;
			}

			if (!syntax.Modifiers.Any(SyntaxKind.PartialKeyword)) {
				result.Diagnostics.Add(new DiagnosticInfo(Diagnostics.NotPartial, typeLocation, result.TypeName));
				ok = false;
			}

			for (var container = symbol.ContainingType; container != null; container = container.ContainingType) {
				var containerIsPartial = container.DeclaringSyntaxReferences
					.Select(reference => reference.GetSyntax(cancellation))
					.OfType<TypeDeclarationSyntax>()
					.Any(declaration => declaration.Modifiers.Any(SyntaxKind.PartialKeyword));

				if (!containerIsPartial) {
					result.Diagnostics.Add(new DiagnosticInfo(Diagnostics.ContainingNotPartial, typeLocation, result.TypeName, container.ToDisplayString(TypeFormat)));
					ok = false;
				}
			}

			if (!result.IsStruct && result.IsConcrete && !HasAccessibleParameterlessConstructor(symbol)) {
				result.Diagnostics.Add(new DiagnosticInfo(Diagnostics.NoParameterlessConstructor, typeLocation, result.TypeName));
				ok = false;
			}

			var attribute = context.Attributes[0];
			var omitDefaults = attribute.NamedArguments.Any(argument => argument.Key == "OmitDefaults" && argument.Value.Value is bool value && value);

			var fields = CollectFields(symbol, compilation, omitDefaults, result, cancellation);

			var byHash = new Dictionary<uint, FieldModel>();
			foreach (var field in fields) {
				if (field.Hash == 0) {
					result.Diagnostics.Add(new DiagnosticInfo(Diagnostics.HashZero, field.Location, field.Name, result.TypeName));
					ok = false;
				} else if (byHash.TryGetValue(field.Hash, out var other)) {
					result.Diagnostics.Add(new DiagnosticInfo(Diagnostics.HashCollision, field.Location, field.Name, other.Name, result.TypeName));
					ok = false;
				} else {
					byHash.Add(field.Hash, field);
				}

				if (field.Kind == FieldKind.Boxed && !IsSerializable(field.Type)) {
					result.Diagnostics.Add(new DiagnosticInfo(Diagnostics.UnsupportedFieldType, field.Location, field.Name, result.TypeName, field.TypeName));
					ok = false;
				}

				if (field.Omit && field.HasNonConstantInitializer) {
					result.Diagnostics.Add(new DiagnosticInfo(Diagnostics.NonConstantDefault, field.Location, field.Name, result.TypeName, field.InitializerText));
					ok = false;
				}

				CollectPolymorphicTypes(field.Type, result.PolymorphicDeclaredTypes);
			}

			if (ok) {
				result.Source = TypeEmitter.Emit(symbol, syntax, fields, compilation);
			}

			return result;
		}

		private static bool ContainingTypesAreGeneric(INamedTypeSymbol symbol)
		{
			for (var container = symbol.ContainingType; container != null; container = container.ContainingType) {
				if (container.IsGenericType) {
					return true;
				}
			}

			return false;
		}

		public static string MetadataName(INamedTypeSymbol symbol)
		{
			var parts = new List<string>();
			for (var current = symbol; current != null; current = current.ContainingType) {
				parts.Add(current.MetadataName);
			}

			parts.Reverse();
			var name = string.Join("+", parts);

			if (symbol.ContainingNamespace != null && !symbol.ContainingNamespace.IsGlobalNamespace) {
				name = symbol.ContainingNamespace.ToDisplayString() + "." + name;
			}

			return name;
		}

		public static bool IsAccessible(INamedTypeSymbol symbol)
		{
			for (var current = symbol; current != null; current = current.ContainingType) {
				if (current.DeclaredAccessibility != Accessibility.Public && current.DeclaredAccessibility != Accessibility.Internal) {
					return false;
				}
			}

			return true;
		}

		private static bool HasAccessibleParameterlessConstructor(INamedTypeSymbol symbol)
		{
			return symbol.InstanceConstructors.Any(constructor =>
				constructor.Parameters.Length == 0
				&& (constructor.DeclaredAccessibility == Accessibility.Public || constructor.DeclaredAccessibility == Accessibility.Internal));
		}

		private static List<FieldModel> CollectFields(INamedTypeSymbol type, Compilation compilation, bool omitDefaults, GeneratedType result, CancellationToken cancellation)
		{
			// Root base first, so the data layout matches the reflection converter's.
			var hierarchy = new List<INamedTypeSymbol>();
			for (var current = type; current != null && current.SpecialType != SpecialType.System_Object && current.SpecialType != SpecialType.System_ValueType; current = current.BaseType) {
				hierarchy.Add(current);
			}

			hierarchy.Reverse();

			var fields = new List<FieldModel>();

			foreach (var current in hierarchy) {
				foreach (var member in current.GetMembers()) {
					if (!(member is IFieldSymbol field) || field.IsStatic || field.IsConst || field.IsImplicitlyDeclared) {
						continue;
					}

					if (HasAttribute(field, SuperSerialGenerator.NON_SERIALIZED_ATTRIBUTE)) {
						continue;
					}

					var optIn = HasAttribute(field, SuperSerialGenerator.SERIALIZE_FIELD_ATTRIBUTE) || HasAttribute(field, SuperSerialGenerator.SERIAL_NAME_ATTRIBUTE);
					if (field.DeclaredAccessibility != Accessibility.Public && !optIn) {
						continue;
					}

					var location = field.Locations.FirstOrDefault();

					if (field.IsReadOnly) {
						result.Diagnostics.Add(new DiagnosticInfo(Diagnostics.ReadOnlyField, location, field.Name, result.TypeName));
						continue;
					}

					if (!SymbolEqualityComparer.Default.Equals(current, type) && field.DeclaredAccessibility == Accessibility.Private) {
						result.Diagnostics.Add(new DiagnosticInfo(Diagnostics.InaccessibleBaseField, location, field.Name, current.ToDisplayString(TypeFormat), result.TypeName));
						continue;
					}

					fields.Add(CreateField(field, compilation, omitDefaults, cancellation));
				}
			}

			return fields;
		}

		private static FieldModel CreateField(IFieldSymbol field, Compilation compilation, bool omitDefaults, CancellationToken cancellation)
		{
			var model = new FieldModel {
				Symbol = field,
				Name = field.Name,
				Type = field.Type,
				Location = field.Locations.FirstOrDefault(),
			};

			model.SerialName = SerialName(field) ?? field.Name;
			model.Hash = XXHash32.Hash(model.SerialName);
			model.TypeName = field.Type.ToDisplayString(NullableTypeFormat);

			model.ValueType = field.Type;
			if (field.Type is INamedTypeSymbol named && named.OriginalDefinition.SpecialType == SpecialType.System_Nullable_T) {
				model.IsNullable = true;
				model.ValueType = named.TypeArguments[0];
			}

			model.Kind = Classify(model.ValueType, model.IsNullable);

			var optional = HasAttribute(field, SuperSerialGenerator.OPTIONAL_ATTRIBUTE);
			var isReference = field.Type.IsReferenceType && field.Type.SpecialType != SpecialType.System_String;

			if (optional) {
				model.Omit = true;
				model.OmitOnlyNull = isReference;
			} else if (omitDefaults && !isReference) {
				model.Omit = true;
			}

			AnalyzeInitializer(model, compilation, cancellation);

			return model;
		}

		private static void AnalyzeInitializer(FieldModel model, Compilation compilation, CancellationToken cancellation)
		{
			var declarator = model.Symbol.DeclaringSyntaxReferences
				.Select(reference => reference.GetSyntax(cancellation))
				.OfType<VariableDeclaratorSyntax>()
				.FirstOrDefault();

			var initializer = declarator?.Initializer?.Value;

			if (initializer == null) {
				model.DefaultExpression = model.Type.IsReferenceType ? "null" : "default";
				return;
			}

			model.InitializerText = initializer.ToString();

			var semanticModel = compilation.GetSemanticModel(initializer.SyntaxTree);

			if (initializer is DefaultExpressionSyntax || initializer.IsKind(SyntaxKind.DefaultLiteralExpression)) {
				model.DefaultExpression = "default";
				return;
			}

			var symbol = semanticModel.GetSymbolInfo(initializer, cancellation).Symbol;

			if (symbol is IFieldSymbol fieldSymbol && fieldSymbol.IsStatic && (fieldSymbol.IsReadOnly || fieldSymbol.IsConst)) {
				model.DefaultExpression = fieldSymbol.ToDisplayString(MemberFormat);
				return;
			}

			if (symbol is IPropertySymbol propertySymbol && propertySymbol.IsStatic) {
				model.DefaultExpression = propertySymbol.ToDisplayString(MemberFormat);
				return;
			}

			var constant = semanticModel.GetConstantValue(initializer, cancellation);
			if (constant.HasValue) {
				model.DefaultExpression = Literal(constant.Value, model.ValueType);
				return;
			}

			model.HasNonConstantInitializer = true;
		}

		/// <summary>
		/// A constant as C# source, cast to the field's type where the literal alone wouldn't convert.
		/// </summary>
		public static string Literal(object value, ITypeSymbol type)
		{
			if (value == null) {
				return "null";
			}

			if (type.TypeKind == TypeKind.Enum) {
				return "(" + type.ToDisplayString(TypeFormat) + ") " + Convert.ToInt64(value);
			}

			switch (value) {
				case bool b:
					return b ? "true" : "false";
				case string s:
					return SymbolDisplay.FormatLiteral(s, true);
				case char c:
					return SymbolDisplay.FormatLiteral(c, true);
				case float f:
					if (float.IsNaN(f)) return "float.NaN";
					if (float.IsPositiveInfinity(f)) return "float.PositiveInfinity";
					if (float.IsNegativeInfinity(f)) return "float.NegativeInfinity";
					return f.ToString("R", System.Globalization.CultureInfo.InvariantCulture) + "f";
				case double d:
					if (double.IsNaN(d)) return "double.NaN";
					if (double.IsPositiveInfinity(d)) return "double.PositiveInfinity";
					if (double.IsNegativeInfinity(d)) return "double.NegativeInfinity";
					return d.ToString("R", System.Globalization.CultureInfo.InvariantCulture) + "d";
				case decimal m:
					return m.ToString(System.Globalization.CultureInfo.InvariantCulture) + "m";
				case long l:
					return l.ToString(System.Globalization.CultureInfo.InvariantCulture) + "L";
				case ulong ul:
					return ul.ToString(System.Globalization.CultureInfo.InvariantCulture) + "UL";
				case uint ui:
					return ui.ToString(System.Globalization.CultureInfo.InvariantCulture) + "u";
				default:
					return Convert.ToString(value, System.Globalization.CultureInfo.InvariantCulture);
			}
		}

		public static FieldKind Classify(ITypeSymbol type, bool nullable)
		{
			switch (type.SpecialType) {
				case SpecialType.System_Boolean:
					return FieldKind.Bool;
				case SpecialType.System_SByte:
				case SpecialType.System_Int16:
				case SpecialType.System_Int32:
				case SpecialType.System_Int64:
					return FieldKind.SignedVarint;
				case SpecialType.System_Byte:
				case SpecialType.System_UInt16:
				case SpecialType.System_UInt32:
				case SpecialType.System_UInt64:
				case SpecialType.System_Char:
					return FieldKind.UnsignedVarint;
				case SpecialType.System_Single:
					return FieldKind.Float;
				case SpecialType.System_Double:
					return FieldKind.Double;
				case SpecialType.System_DateTime:
					return FieldKind.DateTime;
				case SpecialType.System_String:
					return FieldKind.String;
			}

			if (type.TypeKind == TypeKind.Enum) {
				return FieldKind.Enum;
			}

			var name = type.ToDisplayString(TypeFormat);
			switch (name) {
				case "UnityEngine.Vector2":
					return FieldKind.Vector2;
				case "UnityEngine.Vector3":
					return FieldKind.Vector3;
				case "UnityEngine.Quaternion":
					return FieldKind.Quaternion;
			}

			if (nullable) {
				// Nullable<T> of a struct that isn't on the fast path goes through the serializer.
				return FieldKind.Boxed;
			}

			// A [SuperSerializable] type's ISuperSerialize comes from generated code this generator can't see,
			// so the attribute counts as implementing it.
			if (type is INamedTypeSymbol named) {
				var generated = HasAttribute(named, SuperSerialGenerator.ATTRIBUTE);

				if (named.TypeKind == TypeKind.Struct && (generated || Implements(named, SuperSerialGenerator.SUPER_SERIALIZE))) {
					return FieldKind.NestedStruct;
				}

				if (named.TypeKind == TypeKind.Class && named.IsSealed && generated) {
					return FieldKind.NestedSealedClass;
				}
			}

			return FieldKind.Boxed;
		}

		private static bool IsSerializable(ITypeSymbol type)
		{
			if (type.TypeKind == TypeKind.Pointer || type.TypeKind == TypeKind.FunctionPointer || type.TypeKind == TypeKind.Delegate || type.TypeKind == TypeKind.TypeParameter || type.TypeKind == TypeKind.Dynamic) {
				return false;
			}

			if (type.IsRefLikeType) {
				return false;
			}

			if (type is IArrayTypeSymbol array) {
				return array.Rank == 1 && IsSerializable(array.ElementType);
			}

			return true;
		}

		/// <summary>
		/// Declared types a runtime value could be a subclass of: class and interface typed fields and the
		/// element types of lists, arrays and dictionaries, recursively. object is left out since every type would
		/// match it.
		/// </summary>
		private static void CollectPolymorphicTypes(ITypeSymbol type, List<string> result)
		{
			if (type is IArrayTypeSymbol array) {
				CollectPolymorphicTypes(array.ElementType, result);
				return;
			}

			if (!(type is INamedTypeSymbol named)) {
				return;
			}

			if (named.IsGenericType) {
				foreach (var argument in named.TypeArguments) {
					CollectPolymorphicTypes(argument, result);
				}

				return;
			}

			if (named.SpecialType == SpecialType.System_Object || named.SpecialType == SpecialType.System_String) {
				return;
			}

			if (named.TypeKind == TypeKind.Class && !named.IsSealed || named.TypeKind == TypeKind.Interface) {
				var name = MetadataName(named);
				if (!result.Contains(name)) {
					result.Add(name);
				}
			}
		}

		public static bool Implements(INamedTypeSymbol type, string interfaceMetadataName)
		{
			return type.AllInterfaces.Any(i => MetadataName(i) == interfaceMetadataName);
		}

		public static bool HasAttribute(ISymbol symbol, string metadataName)
		{
			return symbol.GetAttributes().Any(attribute => attribute.AttributeClass != null && MetadataName(attribute.AttributeClass) == metadataName);
		}

		public static string SerialName(ISymbol symbol)
		{
			foreach (var attribute in symbol.GetAttributes()) {
				if (attribute.AttributeClass != null && MetadataName(attribute.AttributeClass) == SuperSerialGenerator.SERIAL_NAME_ATTRIBUTE && attribute.ConstructorArguments.Length == 1) {
					return attribute.ConstructorArguments[0].Value as string;
				}
			}

			return null;
		}
	}

	/// <summary>
	/// Writes the partial type. The output is meant to be read: one obvious statement per field against the
	/// stream API, no helper layer in between.
	/// </summary>
	public static class TypeEmitter
	{
		public static string Emit(INamedTypeSymbol type, TypeDeclarationSyntax syntax, List<FieldModel> fields, Compilation compilation)
		{
			var builder = new StringBuilder();
			var indent = 0;

			builder.AppendLine("// <auto-generated>");
			builder.AppendLine("// Generated by Tekly.SuperSerial.Generator from [SuperSerializable]. Do not edit.");
			builder.AppendLine("// </auto-generated>");
			builder.AppendLine();
			builder.AppendLine("using Tekly.SuperSerial.Serialization;");
			builder.AppendLine("using Tekly.SuperSerial.Streams;");
			builder.AppendLine();

			var hasNamespace = type.ContainingNamespace != null && !type.ContainingNamespace.IsGlobalNamespace;
			if (hasNamespace) {
				builder.AppendLine("namespace " + type.ContainingNamespace.ToDisplayString());
				builder.AppendLine("{");
				indent++;
			}

			var containers = new List<INamedTypeSymbol>();
			for (var container = type.ContainingType; container != null; container = container.ContainingType) {
				containers.Add(container);
			}

			containers.Reverse();

			foreach (var container in containers) {
				Line(builder, indent, "partial " + Keyword(container) + " " + container.Name + TypeParameters(container));
				Line(builder, indent, "{");
				indent++;
			}

			var isStruct = type.TypeKind == TypeKind.Struct;
			Line(builder, indent, "partial " + Keyword(type) + " " + type.Name + " : ISuperSerialGenerated");
			Line(builder, indent, "{");
			indent++;

			var typeName = type.ToDisplayString(TypeAnalysis.TypeFormat);
			var modifier = MethodModifier(type, compilation);

			if (!isStruct && !type.IsAbstract) {
				var hides = BaseIsGenerated(type) ? "new " : "";
				Line(builder, indent, "/// <summary>");
				Line(builder, indent, "/// Used by the serializer to create an instance, so field initializers run and no reflection is needed.");
				Line(builder, indent, "/// </summary>");
				Line(builder, indent, "public static " + hides + type.Name + " CreateForSerialization() => new " + type.Name + "();");
				builder.AppendLine();
			}

			Line(builder, indent, "public " + modifier + "void Write(TokenOutputStream output, SuperSerializer superSerializer)");
			Line(builder, indent, "{");
			indent++;

			foreach (var field in fields) {
				EmitWrite(builder, indent, field);
			}

			if (fields.Count > 0) {
				builder.AppendLine();
			}

			Line(builder, indent, "output.WriteEndObject();");
			indent--;
			Line(builder, indent, "}");
			builder.AppendLine();

			Line(builder, indent, "public " + modifier + "void Read(TokenInputStream input, SuperSerializer superSerializer)");
			Line(builder, indent, "{");
			indent++;

			var resets = fields.Where(field => field.DefaultExpression != null).ToList();
			if (resets.Count > 0) {
				Line(builder, indent, "// A field that isn't in the data keeps its declared default.");
				foreach (var field in resets) {
					Line(builder, indent, field.Name + " = " + field.DefaultExpression + ";");
				}

				builder.AppendLine();
			}

			Line(builder, indent, "while (true) {");
			indent++;
			Line(builder, indent, "var hash = input.ReadFieldHash();");
			Line(builder, indent, "if (hash == 0) {");
			Line(builder, indent + 1, "break;");
			Line(builder, indent, "}");
			builder.AppendLine();
			Line(builder, indent, "var wireType = input.ReadWireType();");
			builder.AppendLine();
			Line(builder, indent, "switch (hash) {");
			indent++;

			foreach (var field in fields) {
				Line(builder, indent, "case " + Hex(field.Hash) + ": // " + field.SerialName);
				EmitRead(builder, indent + 1, field);
				Line(builder, indent + 1, "break;");
			}

			Line(builder, indent, "default:");
			Line(builder, indent + 1, "input.Skip(wireType);");
			Line(builder, indent + 1, "break;");

			indent--;
			Line(builder, indent, "}");
			indent--;
			Line(builder, indent, "}");
			indent--;
			Line(builder, indent, "}");

			indent--;
			Line(builder, indent, "}");

			foreach (var _ in containers) {
				indent--;
				Line(builder, indent, "}");
			}

			if (hasNamespace) {
				builder.AppendLine("}");
			}

			return builder.ToString();
		}

		private static void EmitWrite(StringBuilder builder, int indent, FieldModel field)
		{
			var hash = Hex(field.Hash);
			var name = field.Name;

			if (field.Omit) {
				Line(builder, indent, "if (" + OmitCondition(field) + ") {");
				indent++;
			}

			if (field.IsNullable && field.Kind != FieldKind.Boxed && field.Omit && OmitCondition(field) == name + ".HasValue") {
				// The omit check already proved there is a value.
				EmitWriteValue(builder, indent, field, name + ".Value", hash);
			} else if (field.IsNullable && field.Kind != FieldKind.Boxed) {
				Line(builder, indent, "if (" + name + ".HasValue) {");
				EmitWriteValue(builder, indent + 1, field, name + ".Value", hash);
				Line(builder, indent, "} else {");
				Line(builder, indent + 1, "output.WriteNullField(" + hash + "); // " + field.SerialName);
				Line(builder, indent, "}");
			} else {
				EmitWriteValue(builder, indent, field, name, hash);
			}

			if (field.Omit) {
				indent--;
				Line(builder, indent, "}");
			}
		}

		private static void EmitWriteValue(StringBuilder builder, int indent, FieldModel field, string value, string hash)
		{
			var comment = " // " + field.SerialName;

			switch (field.Kind) {
				case FieldKind.Bool:
				case FieldKind.SignedVarint:
				case FieldKind.UnsignedVarint:
				case FieldKind.Float:
				case FieldKind.Double:
				case FieldKind.DateTime:
				case FieldKind.String:
				case FieldKind.Vector2:
				case FieldKind.Vector3:
				case FieldKind.Quaternion:
					Line(builder, indent, "output.WriteField(" + hash + ", " + value + ");" + comment);
					break;

				case FieldKind.Enum:
					Line(builder, indent, "output.WriteField(" + hash + ", " + EnumToLong(field, value) + ");" + comment);
					break;

				case FieldKind.NestedStruct:
					Line(builder, indent, "output.BeginBlockField(" + hash + ");" + comment);
					Line(builder, indent, value + ".Write(output, superSerializer);");
					Line(builder, indent, "output.EndBlock();");
					break;

				case FieldKind.NestedSealedClass:
					Line(builder, indent, "if (" + value + " == null) {");
					Line(builder, indent + 1, "output.WriteNullField(" + hash + ");" + comment);
					Line(builder, indent, "} else {");
					Line(builder, indent + 1, "output.BeginBlockField(" + hash + ");" + comment);
					Line(builder, indent + 1, value + ".Write(output, superSerializer);");
					Line(builder, indent + 1, "output.EndBlock();");
					Line(builder, indent, "}");
					break;

				default:
					Line(builder, indent, "output.WriteFieldHash(" + hash + ");" + comment);
					Line(builder, indent, "superSerializer.WriteValue(output, " + value + ");");
					break;
			}
		}

		private static void EmitRead(StringBuilder builder, int indent, FieldModel field)
		{
			var name = field.Name;
			var fallback = name + " = superSerializer.ReadPayload(input, wireType, " + name + ");";

			if (field.Kind == FieldKind.Boxed) {
				Line(builder, indent, fallback);
				return;
			}

			string wireType;
			string read;

			switch (field.Kind) {
				case FieldKind.Bool:
					wireType = "Varint";
					read = "input.ReadVarUInt() != 0";
					break;
				case FieldKind.SignedVarint:
					wireType = "Varint";
					read = field.ValueType.SpecialType == SpecialType.System_Int64 ? "input.ReadVarInt()" : "checked((" + field.ValueType.ToDisplayString(TypeAnalysis.TypeFormat) + ") input.ReadVarInt())";
					break;
				case FieldKind.UnsignedVarint:
					wireType = "Varint";
					read = field.ValueType.SpecialType == SpecialType.System_UInt64 ? "input.ReadVarUInt()" : "checked((" + field.ValueType.ToDisplayString(TypeAnalysis.TypeFormat) + ") input.ReadVarUInt())";
					break;
				case FieldKind.Float:
					wireType = "Fixed32";
					read = "input.ReadFloat()";
					break;
				case FieldKind.Double:
					wireType = "Fixed64";
					read = "input.ReadDouble()";
					break;
				case FieldKind.DateTime:
					wireType = "Fixed64";
					read = "input.ReadDate()";
					break;
				case FieldKind.String:
					wireType = "Bytes";
					read = "input.ReadUtf8()";
					break;
				case FieldKind.Enum:
					wireType = "Varint";
					read = "(" + field.ValueType.ToDisplayString(TypeAnalysis.TypeFormat) + ") input.ReadVarInt()";
					break;
				case FieldKind.Vector2:
					wireType = "Bytes";
					read = "input.ReadVector2()";
					break;
				case FieldKind.Vector3:
					wireType = "Bytes";
					read = "input.ReadVector3()";
					break;
				case FieldKind.Quaternion:
					wireType = "Bytes";
					read = "input.ReadQuaternion()";
					break;
				case FieldKind.NestedStruct:
				case FieldKind.NestedSealedClass:
					wireType = "Block";
					read = null;
					break;
				default:
					throw new InvalidOperationException(field.Kind.ToString());
			}

			Line(builder, indent, "if (wireType == WireType." + wireType + ") {");

			switch (field.Kind) {
				case FieldKind.Vector2:
					EmitSizedRead(builder, indent + 1, name, read, "sizeof(float) * 2");
					break;
				case FieldKind.Vector3:
					EmitSizedRead(builder, indent + 1, name, read, "sizeof(float) * 3");
					break;
				case FieldKind.Quaternion:
					EmitSizedRead(builder, indent + 1, name, read, "sizeof(float) * 4");
					break;
				case FieldKind.NestedStruct:
					Line(builder, indent + 1, "var end = input.BeginBlock();");
					Line(builder, indent + 1, name + ".Read(input, superSerializer);");
					Line(builder, indent + 1, "input.EndBlock(end);");
					break;
				case FieldKind.NestedSealedClass:
					Line(builder, indent + 1, "if (" + name + " == null) {");
					Line(builder, indent + 2, name + " = " + field.ValueType.ToDisplayString(TypeAnalysis.TypeFormat) + ".CreateForSerialization();");
					Line(builder, indent + 1, "}");
					builder.AppendLine();
					Line(builder, indent + 1, "var end = input.BeginBlock();");
					Line(builder, indent + 1, name + ".Read(input, superSerializer);");
					Line(builder, indent + 1, "input.EndBlock(end);");
					break;
				default:
					Line(builder, indent + 1, name + " = " + read + ";");
					break;
			}

			Line(builder, indent, "} else {");
			Line(builder, indent + 1, fallback);
			Line(builder, indent, "}");
		}

		private static void EmitSizedRead(StringBuilder builder, int indent, string name, string read, string size)
		{
			Line(builder, indent, "if (input.ExpectLength(" + size + ")) {");
			Line(builder, indent + 1, name + " = " + read + ";");
			Line(builder, indent, "}");
		}

		private static string EnumToLong(FieldModel field, string value)
		{
			var underlying = ((INamedTypeSymbol) field.ValueType).EnumUnderlyingType;

			if (underlying != null && underlying.SpecialType == SpecialType.System_UInt64) {
				return "unchecked((long) (ulong) " + value + ")";
			}

			return "(long) " + value;
		}

		private static string OmitCondition(FieldModel field)
		{
			var name = field.Name;

			if (field.OmitOnlyNull) {
				return name + " != null";
			}

			var defaultValue = field.DefaultExpression ?? "default";

			if (field.IsNullable) {
				return defaultValue == "default" || defaultValue == "null" ? name + ".HasValue" : "!object.Equals(" + name + ", " + defaultValue + ")";
			}

			switch (field.Kind) {
				case FieldKind.Bool:
				case FieldKind.SignedVarint:
				case FieldKind.UnsignedVarint:
				case FieldKind.Float:
				case FieldKind.Double:
				case FieldKind.DateTime:
				case FieldKind.Enum:
					return name + " != " + defaultValue;
				case FieldKind.String:
					// null and "" both count as empty, so either reads back as the declared default.
					return defaultValue == "null" || defaultValue == "default" || defaultValue == "\"\""
						? "!string.IsNullOrEmpty(" + name + ")"
						: name + " != " + defaultValue;
			}

			if (field.Type.IsReferenceType) {
				return name + " != null";
			}

			return "!" + name + ".Equals(" + (defaultValue == "default" ? "default(" + field.TypeName + ")" : defaultValue) + ")";
		}

		private static string MethodModifier(INamedTypeSymbol type, Compilation compilation)
		{
			if (type.TypeKind == TypeKind.Struct) {
				return "";
			}

			if (BaseIsGenerated(type)) {
				return "override ";
			}

			for (var baseType = type.BaseType; baseType != null && baseType.SpecialType != SpecialType.System_Object; baseType = baseType.BaseType) {
				var baseWrite = baseType.GetMembers("Write").OfType<IMethodSymbol>().FirstOrDefault(method => method.Parameters.Length == 2 && !method.IsStatic);

				if (baseWrite != null) {
					return baseWrite.IsVirtual || baseWrite.IsOverride || baseWrite.IsAbstract ? "override " : "new ";
				}
			}

			return type.IsSealed ? "" : "virtual ";
		}

		/// <summary>
		/// Whether a base class is itself [SuperSerializable]. Its Write and Read are generated too, which this
		/// generator can't see as symbols, so the derived type overrides them on faith.
		/// </summary>
		private static bool BaseIsGenerated(INamedTypeSymbol type)
		{
			for (var baseType = type.BaseType; baseType != null && baseType.SpecialType != SpecialType.System_Object; baseType = baseType.BaseType) {
				if (TypeAnalysis.HasAttribute(baseType, SuperSerialGenerator.ATTRIBUTE)) {
					return true;
				}
			}

			return false;
		}

		private static string Keyword(INamedTypeSymbol type)
		{
			if (type.TypeKind == TypeKind.Struct) {
				return "struct";
			}

			return type.IsRecord ? "record" : "class";
		}

		private static string TypeParameters(INamedTypeSymbol type)
		{
			if (!type.IsGenericType) {
				return "";
			}

			return "<" + string.Join(", ", type.TypeParameters.Select(parameter => parameter.Name)) + ">";
		}

		public static string Hex(uint hash)
		{
			return "0x" + hash.ToString("X8") + "u";
		}

		private static void Line(StringBuilder builder, int indent, string text)
		{
			builder.Append('\t', indent).AppendLine(text);
		}
	}

	/// <summary>
	/// Writes the per assembly registry.
	/// </summary>
	public static class RegistryEmitter
	{
		public static string Emit(Compilation compilation, ImmutableArray<GeneratedType> types, CancellationToken cancellation)
		{
			if (compilation.GetTypeByMetadataName(SuperSerialGenerator.ATTRIBUTE) == null) {
				return null;
			}

			var generated = new Dictionary<string, GeneratedType>();
			var polymorphic = new List<INamedTypeSymbol>();

			foreach (var type in types) {
				generated[type.MetadataName] = type;

				foreach (var declared in type.PolymorphicDeclaredTypes) {
					var symbol = compilation.GetTypeByMetadataName(declared);
					if (symbol != null && !polymorphic.Contains(symbol, SymbolEqualityComparer.Default)) {
						polymorphic.Add(symbol);
					}
				}
			}

			var registerTypes = new List<string>();
			var registerStructs = new List<string>();
			var registerFactories = new List<string>();

			foreach (var type in AllTypes(compilation.Assembly.GlobalNamespace, cancellation)) {
				if (type.IsAbstract || type.IsStatic || type.IsGenericType || type.TypeKind != TypeKind.Class && type.TypeKind != TypeKind.Struct) {
					continue;
				}

				if (!TypeAnalysis.IsAccessible(type)) {
					continue;
				}

				var name = type.ToDisplayString(TypeAnalysis.TypeFormat);

				if (generated.TryGetValue(TypeAnalysis.MetadataName(type), out var model) && model.Source != null) {
					registerTypes.Add(name);

					if (model.IsStruct) {
						registerStructs.Add(name);
					} else {
						registerFactories.Add(name);
					}

					continue;
				}

				var wanted = TypeAnalysis.HasAttribute(type, SuperSerialGenerator.SERIAL_NAME_ATTRIBUTE)
				             || polymorphic.Any(declared => compilation.HasImplicitConversion(type, declared));

				if (wanted) {
					registerTypes.Add(name);
				}
			}

			if (registerTypes.Count == 0) {
				return null;
			}

			var builder = new StringBuilder();
			builder.AppendLine("// <auto-generated>");
			builder.AppendLine("// Generated by Tekly.SuperSerial.Generator. Do not edit.");
			builder.AppendLine("// </auto-generated>");
			builder.AppendLine();
			builder.AppendLine("using Tekly.SuperSerial.Serialization;");
			builder.AppendLine();
			builder.AppendLine("namespace Tekly.SuperSerial.Generated");
			builder.AppendLine("{");
			builder.AppendLine("\t/// <summary>");
			builder.AppendLine("\t/// Registers every type in " + compilation.AssemblyName + " that a SuperSerializer needs to know before it reads:");
			builder.AppendLine("\t/// [SuperSerializable] types, [SerialName] types, and concrete types that can appear in a polymorphic field.");
			builder.AppendLine("\t/// </summary>");
			builder.AppendLine("\t[UnityEngine.Scripting.Preserve]");
			builder.AppendLine("\tpublic static class " + ClassName(compilation.AssemblyName));
			builder.AppendLine("\t{");
			builder.AppendLine("\t\t/// <summary>");
			builder.AppendLine("\t\t/// Announces this registry so every SuperSerializer applies it. Runs before the first scene loads in a");
			builder.AppendLine("\t\t/// player and on every domain reload in the editor. Module initializers would be the natural tool but");
			builder.AppendLine("\t\t/// Unity lists them as unsupported, so this leans on the two hooks Unity promises to call.");
			builder.AppendLine("\t\t/// </summary>");
			builder.AppendLine("\t\t[UnityEngine.RuntimeInitializeOnLoadMethod(UnityEngine.RuntimeInitializeLoadType.SubsystemRegistration)]");
			builder.AppendLine("#if UNITY_EDITOR");
			builder.AppendLine("\t\t[UnityEditor.InitializeOnLoadMethod]");
			builder.AppendLine("#endif");
			builder.AppendLine("\t\tpublic static void Announce()");
			builder.AppendLine("\t\t{");
			builder.AppendLine("\t\t\tSuperSerialRegistries.Add(RegisterAll);");
			builder.AppendLine("\t\t}");
			builder.AppendLine();
			builder.AppendLine("\t\tpublic static void RegisterAll(SuperSerializer serializer)");
			builder.AppendLine("\t\t{");

			foreach (var name in registerTypes) {
				builder.AppendLine("\t\t\tserializer.RegisterType<" + name + ">();");
			}

			if (registerFactories.Count > 0) {
				builder.AppendLine();
				builder.AppendLine("\t\t\t// Classes are created through their generated factory instead of reflection.");
				foreach (var name in registerFactories) {
					builder.AppendLine("\t\t\tserializer.RegisterFactory<" + name + ">(" + name + ".CreateForSerialization);");
				}
			}

			if (registerStructs.Count > 0) {
				builder.AppendLine();
				builder.AppendLine("\t\t\t// Structs get ref based Write and Read delegates, so they can be serialized in place without boxing.");
				foreach (var name in registerStructs) {
					builder.AppendLine("\t\t\tSuperSerializeStructs<" + name + ">.Register<" + name + ">();");
				}
			}

			builder.AppendLine("\t\t}");
			builder.AppendLine("\t}");
			builder.AppendLine("}");

			return builder.ToString();
		}

		public static string ClassName(string assemblyName)
		{
			var builder = new StringBuilder("SuperSerialRegistry_");
			foreach (var c in assemblyName ?? "Assembly") {
				builder.Append(char.IsLetterOrDigit(c) ? c : '_');
			}

			return builder.ToString();
		}

		private static IEnumerable<INamedTypeSymbol> AllTypes(INamespaceSymbol ns, CancellationToken cancellation)
		{
			foreach (var member in ns.GetMembers()) {
				cancellation.ThrowIfCancellationRequested();

				if (member is INamespaceSymbol child) {
					foreach (var type in AllTypes(child, cancellation)) {
						yield return type;
					}
				} else if (member is INamedTypeSymbol type) {
					foreach (var nested in WithNested(type)) {
						yield return nested;
					}
				}
			}
		}

		private static IEnumerable<INamedTypeSymbol> WithNested(INamedTypeSymbol type)
		{
			yield return type;

			foreach (var nested in type.GetTypeMembers()) {
				foreach (var deeper in WithNested(nested)) {
					yield return deeper;
				}
			}
		}
	}
}
