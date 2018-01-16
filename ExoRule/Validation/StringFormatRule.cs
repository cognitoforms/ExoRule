using System;
using System.Text.RegularExpressions;
using System.Linq;
using System.Text;
using System.Collections;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.Runtime.Serialization;
using ExoModel;
using ExoRule;

namespace ExoRule.Validation
{
	/// <summary>
	/// Applies conditions when the value of a <see cref="ModelProperty"/> is not in the correct format.
	/// </summary>
	public class StringFormatRule : PropertyRule
	{
		#region Fields

		Func<string> formatDescription;
		Func<Regex> formatExpression;
		Func<string> reformatExpression;

		#endregion

		#region Constructors

		public StringFormatRule(string rootType, string property, Func<string> formatDescription, Func<Regex> formatExpression, Func<string> reformatExpression = null, RuleInvocationType invocationTypes = RuleInvocationType.PropertyChanged)
			: base(rootType, property, CreateError(property, formatDescription), invocationTypes, property)
		{
			this.formatDescription = formatDescription;
			this.formatExpression = formatExpression;
			this.reformatExpression = reformatExpression;
		}

		public StringFormatRule(string rootType, string property, Error error, Func<Regex> formatExpression, Func<string> reformatExpression = null, RuleInvocationType invocationTypes = RuleInvocationType.PropertyChanged)
			: base(rootType, property, error, invocationTypes, property)
		{
			this.formatExpression = formatExpression;
			this.reformatExpression = reformatExpression;
		}

		#endregion

		#region Properties

		public string FormatDescription
		{
			get
			{
				return formatDescription != null ? formatDescription() : null;
			}
		}

		public Regex FormatExpression
		{
			get
			{
				return formatExpression != null ? formatExpression() : null;
			}
		}

		public string ReformatExpression
		{
			get
			{
				return reformatExpression != null ? reformatExpression() : null;
			}
		}

		#endregion

		#region Methods

		static Func<ModelType, ConditionType> CreateError(string property, Func<string> formatDescription)
		{
			return (ModelType rootType) =>
			{
				var label = GetLabel(rootType, property);

				return new Error(GetErrorCode(rootType.Name, property, "StringFormat"), "string-format", typeof(StringFormatRule),
				(s) => s
					.Replace("{property}", label)
					.Replace("{formatDescription}", formatDescription()));
			};
		}

		protected override bool ConditionApplies(ModelInstance root)
		{
			// First, get the string value to validate
			var value = root[Property] as string;
			if (String.IsNullOrEmpty(value))
				return false;

			// Then get the regular expression to use
			Regex exp = FormatExpression;
			if (exp == null)
				return false;

			// Indicate that the value is invalid if the regular expression does not match
			if (!exp.IsMatch(value))
				return true;

			// Reformat if necessary
			var reformat = ReformatExpression;
			if (reformat != null)
				root[Property] = exp.Replace(value, reformat);

			// Indicate that the value is valid
			return false;
		}

		#endregion
	}
}