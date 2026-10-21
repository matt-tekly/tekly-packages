using Microsoft.CodeAnalysis;

namespace Tekly.SuperSerial.Generator
{
	public static class Diagnostics
	{
		private const string CATEGORY = "SuperSerial";

		public static readonly DiagnosticDescriptor NotPartial = new DiagnosticDescriptor(
			"SUPERSERIAL001", "Type must be partial",
			"[SuperSerializable] type '{0}' must be declared partial so the generated Write and Read can be added to it.",
			CATEGORY, DiagnosticSeverity.Error, true);

		public static readonly DiagnosticDescriptor ContainingNotPartial = new DiagnosticDescriptor(
			"SUPERSERIAL002", "Containing type must be partial",
			"'{0}' is nested in '{1}', which must be declared partial for the generated code to reach it.",
			CATEGORY, DiagnosticSeverity.Error, true);

		public static readonly DiagnosticDescriptor NoParameterlessConstructor = new DiagnosticDescriptor(
			"SUPERSERIAL003", "No accessible parameterless constructor",
			"[SuperSerializable] class '{0}' needs a public or internal parameterless constructor so the generated CreateForSerialization can call it.",
			CATEGORY, DiagnosticSeverity.Error, true);

		public static readonly DiagnosticDescriptor HashCollision = new DiagnosticDescriptor(
			"SUPERSERIAL004", "Field name hash collision",
			"Fields '{0}' and '{1}' of '{2}' hash to the same value. Give one of them a [SerialName].",
			CATEGORY, DiagnosticSeverity.Error, true);

		public static readonly DiagnosticDescriptor HashZero = new DiagnosticDescriptor(
			"SUPERSERIAL005", "Field name hashes to zero",
			"Field '{0}' of '{1}' hashes to 0, which marks the end of an object. Give it a [SerialName].",
			CATEGORY, DiagnosticSeverity.Error, true);

		public static readonly DiagnosticDescriptor UnsupportedFieldType = new DiagnosticDescriptor(
			"SUPERSERIAL006", "Unsupported field type",
			"Field '{0}' of '{1}' has type '{2}', which SuperSerial cannot serialize.",
			CATEGORY, DiagnosticSeverity.Error, true);

		public static readonly DiagnosticDescriptor NonConstantDefault = new DiagnosticDescriptor(
			"SUPERSERIAL007", "Omit-defaults field needs a constant initializer",
			"Field '{0}' of '{1}' is omitted when it equals its default, but its initializer '{2}' is not a constant, literal, enum member, static readonly member or default. The generator cannot compare against it.",
			CATEGORY, DiagnosticSeverity.Error, true);

		public static readonly DiagnosticDescriptor ReadOnlyField = new DiagnosticDescriptor(
			"SUPERSERIAL009", "Readonly field skipped",
			"Field '{0}' of '{1}' is readonly, so the generated Read cannot assign it. It is left out of the data.",
			CATEGORY, DiagnosticSeverity.Warning, true);

		public static readonly DiagnosticDescriptor InaccessibleBaseField = new DiagnosticDescriptor(
			"SUPERSERIAL010", "Private base class field skipped",
			"Field '{0}' is private to base class '{1}', so the generated code in '{2}' cannot reach it. It is left out of the data. Make it protected, or mark the base class [SuperSerializable] and serialize it there.",
			CATEGORY, DiagnosticSeverity.Warning, true);

		public static readonly DiagnosticDescriptor GenericType = new DiagnosticDescriptor(
			"SUPERSERIAL008", "Generic types are not supported",
			"[SuperSerializable] type '{0}' is generic. The generator only handles closed types.",
			CATEGORY, DiagnosticSeverity.Error, true);
	}
}
