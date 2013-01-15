using System;
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
	/// Applies conditions when the value of a <see cref="ModelProperty"/> is
	/// too short or long.
	/// </summary>
	public class StringLengthRule : PropertyRule
	{
		#region Constructors

		public StringLengthRule(string rootType, string property, int minimum, int maximum)
			: this(rootType, property, minimum, maximum, RuleInvocationType.PropertyChanged)
		{ }

		public StringLengthRule(string rootType, string property, int minimum, int maximum, RuleInvocationType invocationTypes)
			: this(rootType, property, minimum, maximum, CreateError(rootType, property, minimum, maximum), invocationTypes)
		{}

		public StringLengthRule(string rootType, string property, int minimum, int maximum, string errorMessage)
			: this(rootType, property, minimum, maximum, new Error(GetErrorCode(rootType, property, "StringLength"), errorMessage, null), RuleInvocationType.PropertyChanged)
		{}

		public StringLengthRule(string rootType, string property, int minimum, int maximum, Error error, RuleInvocationType invocationTypes)
			: base(rootType, property, error, invocationTypes, property)
		{
			this.Minimum = minimum;
			this.Maximum = maximum;
		}

		#endregion

		#region Properties

		public int Minimum { get; private set; }

		public int Maximum { get; private set; }

		#endregion

		#region Methods

		static Error CreateError(string rootType, string property, int minimum, int maximum)
		{
			string message;
			if (minimum > 0 && maximum > 0)
				message = "string-length-between";
			else if (minimum > 0)
				message = "string-length-at-least";
			else if (maximum > 0)
				message = "string-length-at-most";
			else
				throw new ArgumentException("Either the minimum or maximum characters must be greater than zero for a string length rule.");

			return new Error(
				GetErrorCode(rootType, property, "StringLength"), message, typeof(StringLengthRule),
				(s) => s
					.Replace("{property}", GetLabel(rootType, property))
					.Replace("{min}", minimum.ToString())
					.Replace("{max}", maximum.ToString()), null);
		}

		protected override bool ConditionApplies(ModelInstance root)
		{
			var value = root[Property] as string;
			if (String.IsNullOrEmpty(value))
				return false;

			return (Maximum > 0 && value.Length > Maximum) || (Minimum > 0 && value.Length < Minimum);
		}

		#endregion
	}
}