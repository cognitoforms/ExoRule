using System;
using System.Linq;
using System.Text;
using System.Collections;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.Runtime.Serialization;
using ExoGraph;
using ExoRule;

namespace ExoRule.Validation
{
	/// <summary>
	/// Applies conditions when the value of a <see cref="GraphProperty"/> is
	/// too short or long.
	/// </summary>
	[DataContract(Name = "stringLength")]
	public class StringLengthRule : PropertyRule
	{
		#region Constructors

		public StringLengthRule(string rootType, string property, int minimum, int maximum, Func<string> label, Func<int, string> format)
			: this(rootType, property, minimum, maximum, label, format, RuleInvocationType.PropertyChanged)
		{ }

		public StringLengthRule(string rootType, string property, int minimum, int maximum, Func<string> label, Func<int, string> format, RuleInvocationType invocationTypes)
			: base(rootType, property, CreateError(rootType, property, minimum, maximum, label, format), invocationTypes)
		{
			this.Minimum = minimum;
			this.Maximum = maximum;
		}

		#endregion

		#region Properties

		[DataMember(Name = "min", EmitDefaultValue = false)]
		public int Minimum { get; private set; }

		[DataMember(Name = "max", EmitDefaultValue = false)]
		public int Maximum { get; private set; }

		#endregion

		#region Methods

		static Error CreateError(string rootType, string property, int minimum, int maximum, Func<string> label, Func<int, string> format)
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
					.Replace("{property}", label())
					.Replace("{min}", format(minimum))
					.Replace("{max}", format(maximum)), null);
		}

		protected override bool ConditionApplies(GraphInstance root)
		{
			object value = root[Property];

			if (value == null)
				return false;

			string str = value.ToString();

			if (str == null)
				return false;

			int len = str.Length;
			return (Maximum > 0 && len > Maximum) || (Minimum > 0 && len < Minimum);
		}

		protected override string TypeName
		{
			get
			{
				return "stringLength";
			}
		}

		#endregion
	}
}