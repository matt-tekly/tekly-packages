using DotLiquid.Util;

namespace DotLiquid
{
	/// <summary>
	/// Utiliy containing regexes for Liquid syntax and registering default tags and blocks
	/// </summary>
	public static class Liquid
	{
		internal static readonly ResourceManager ResourceManager = new ResourceManager();

		public static readonly string FilterSeparator = R.Q(@"\|");
		public static readonly string ArgumentSeparator = R.Q(@",");
		public static readonly string FilterArgumentSeparator = R.Q(@":");
		public static readonly string VariableAttributeSeparator = R.Q(@".");
		public static readonly string TagStart = R.Q(@"\{\%");
		public static readonly string TagEnd = R.Q(@"\%\}");
		public static readonly string VariableSignature = R.Q(@"\(?[\w\-\.\[\]]\)?");
		public static readonly string VariableSegment = R.Q(@"[\w\-]");
		public static readonly string VariableStart = R.Q(@"\{\{");
		public static readonly string VariableEnd = R.Q(@"\}\}");
		public static readonly string QuotedString = R.Q(@"""[^""]*""|'[^']*'");
		public static readonly string QuotedFragment = string.Format(R.Q(@"{0}|(?:[^\s,\|'""]|{0})+"), QuotedString);

		public static readonly string QuotedAssignFragment =
			string.Format(R.Q(@"{0}|(?:[^\s\|'""]|{0})+"), QuotedString);

		public static readonly string TagAttributes = string.Format(R.Q(@"(\w+)\s*\:\s*({0})"), QuotedFragment);
		public static readonly string AnyStartingTag = R.Q(@"\{\{|\{\%");
		public static readonly string VariableParser = string.Format(R.Q(@"\[[^\]]+\]|{0}+\??"), VariableSegment);
		public static readonly string LiteralShorthand = R.Q(@"^(?:\{\{\{\s?)(.*?)(?:\s*\}\}\})$");
		public static readonly string CommentShorthand = R.Q(@"^(?:\{\s?\#\s?)(.*?)(?:\s*\#\s?\})$");
		public static bool UseRubyDateFormat = false;

		static Liquid()
		{
			Template.RegisterTag<Tags.Assign>("assign");
			Template.RegisterTag<Tags.Block>("block");
			Template.RegisterTag<Tags.Capture>("capture");
			Template.RegisterTag<Tags.Case>("case");
			Template.RegisterTag<Tags.Comment>("comment");
			Template.RegisterTag<Tags.Cycle>("cycle");
			Template.RegisterTag<Tags.Extends>("extends");
			Template.RegisterTag<Tags.For>("for");
			Template.RegisterTag<Tags.Break>("break");
			Template.RegisterTag<Tags.Continue>("continue");
			Template.RegisterTag<Tags.If>("if");
			Template.RegisterTag<Tags.IfChanged>("ifchanged");
			Template.RegisterTag<Tags.Include>("include");
			Template.RegisterTag<Tags.Literal>("literal");
			Template.RegisterTag<Tags.Unless>("unless");
			Template.RegisterTag<Tags.Raw>("raw");

			Template.RegisterTag<Tags.Html.TableRow>("tablerow");

			Template.RegisterFilter(typeof(StandardFilters));
		}
	}

	public class ResourceManager
	{
		public string GetString(string resource)
		{
			return GetStaticString(resource);
		}
		
		public static string GetStaticString(string resource)
		{
			switch (resource) {
				case "AssignTagSyntaxException":
					return "Syntax Error in 'assign' tag - Valid syntax: assign [var] = [source]";
				case "BlankFileSystemDoesNotAllowIncludesException":
					return "Error - This liquid context does not allow includes<";
				case "BlockTagAlreadyDefinedException": return "Liquid Error - Block '{0}' already defined<";
				case "BlockTagNoElseException": return "{0} tag does not expect else tag<";
				case "BlockTagNoEndException": return "'end' is not a valid delimiter for {0} tags. Use {1}";
				case "BlockTagNotClosedException": return "{0} tag was never closed<";
				case "BlockTagNotTerminatedException": return "Tag '{0}' was not properly terminated with regexp: {1}";
				case "BlockTagSyntaxException": return "Syntax Error in 'block' tag - Valid syntax: block [name]";
				case "BlockUnknownTagException": return "Unknown tag '{0}";
				case "BlockVariableNotTerminatedException":
					return "Variable '{0}' was not properly terminated with regexp: {1}";
				case "CaptureTagSyntaxException": return "Syntax Error in 'capture' tag - Valid syntax: capture [var]";
				case "CaseTagElseSyntaxException":
					return "Syntax Error in 'case' tag - Valid else condition: {{% else %}} (no parameters)";
				case "CaseTagSyntaxException": return "Syntax Error in 'case' tag - Valid syntax: case [condition]";
				case "CaseTagWhenSyntaxException":
					return
						"Syntax Error in 'case' tag - Valid when condition: {{% when [condition] [or condition2...] %";
				case "ConditionUnknownOperatorException": return "Unknown operator {0}";
				case "ContextLiquidError": return "Liquid error: {0}";
				case "ContextLiquidSyntaxError": return "Liquid syntax error: {0}";
				case "ContextObjectInvalidException":
					return
						"Object '{0}' is invalid because it is neither a built-in type nor implements ILiquidizable<";
				case "ContextStackException": return "Nesting too deep<";
				case "CycleTagSyntaxException":
					return "Syntax Error in 'cycle' tag - Valid syntax: cycle [name :] var [, var2, var3 .";
				case "DropWrongNamingConventionMessage": return "Missing property. Did you mean '{0}";
				case "ExtendsTagCanBeUsedOneException": return "Liquid Error - 'extends' tag can be used only once<";
				case "ExtendsTagMustBeFirstTagException":
					return "Liquid Error - 'extends' must be the first tag in an extending template<";
				case "ExtendsTagSyntaxException":
					return "Syntax Error in 'extends' tag - Valid syntax: extends [template]";
				case "ExtendsTagUnallowedTagsException":
					return "Liquid Error - Only 'comment' and 'block' tags are allowed in an extending template<";
				case "ForTagSyntaxException":
					return "Syntax Error in 'for' tag - Valid syntax: for [item] in [collection]";
				case "IfTagSyntaxException": return "Syntax Error in 'if' tag - Valid syntax: if [expression]";
				case "IncludeTagSyntaxException":
					return "Syntax Error in 'include' tag - Valid syntax: include [template]";
				case "LocalFileSystemIllegalTemplateNameException": return "Error - Illegal template name '{0}";
				case "LocalFileSystemIllegalTemplatePathException": return "Error - Illegal template path '{0}";
				case "LocalFileSystemTemplateNotFoundException": return "Error - No such template '{0}";
				case "StrainerFilterHasNoValueException":
					return "Error - Filter '{0}' does not have a default value for '{1}' and no value was supplied<";
				case "TableRowTagSyntaxException":
					return
						"Syntax Error in 'tablerow' tag - Valid syntax: tablerow [item] in [collection] cols=[number]";
				case "VariableFilterNotFoundException": return "Error - Filter '{0}' in '{1}' could not be found.";
				case "VariableNotFoundException": return "Error - Variable '{0}' could not be found<";
				case "VariableNotTerminatedException": return "Variable '{0}' was not properly terminated<";
				case "WeakTableKeyNotFoundException": return "key could not be found<";
				case "IfTagTooMuchConditionsException":
					return "Syntax Error in 'if' tag - max 500 conditions are allowed<";
				case "ForTagMaximumIterationsExceededException":
					return "Render Error - Maximum number of iterations {0} exceeded<";
				case "SimpleTagSyntaxException": return "Syntax Error in '{0}' tag - Valid syntax: {0}";
				case "ParamTagSyntaxException": return "Unsupported parameter: '{0}', supported options are: {1}";
				case "ParamOptionSyntaxException": return "Invalid {0} option: '{1}', supported options are: {2}";
				case "CultureNotFoundException": return "Culture '{0}' is not supported<";
				case "DecrementSyntaxException":
					return "Syntax Error in 'decrement' tag - Valid syntax: decrement [var]";
				case "IncrementSyntaxException":
					return "Syntax Error in 'increment' tag - Valid syntax: increment [var]";
			}

			return resource;
		}
	}
}