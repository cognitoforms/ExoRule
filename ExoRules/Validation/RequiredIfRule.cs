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
	[DataContract(Name = "requiredIf")]
	public class RequiredIfRule : PropertyRule
	{
		#region Fields

		PathSource compareSource;

		#endregion

		#region Constructors

		public RequiredIfRule(string rootType, string property, string compareSource, CompareOperator compareOperator, object compareValue, Func<string> label, Func<string> compareLabel, Func<object, string> format, params ConditionTypeSet[] sets)
			: this(rootType, property, compareSource, compareOperator, compareValue, label, compareLabel, format, RuleInvocationType.PropertyChanged, sets)
		{ }

		public RequiredIfRule(string rootType, string property, string compareSource, CompareOperator compareOperator, object compareValue, Error error)
			: this(rootType, property, compareSource, compareOperator, compareValue, RuleInvocationType.PropertyChanged, error)
		{ }

		public RequiredIfRule(string rootType, string property, string compareSource, CompareOperator compareOperator, object compareValue, Func<string> label, Func<string> compareLabel, Func<object, string> format, RuleInvocationType invocationTypes, params ConditionTypeSet[] sets)
			: this(rootType, property, compareSource, compareOperator, compareValue, invocationTypes,
				CreateError(rootType, property, compareSource, compareOperator, compareValue, label, compareLabel, format, sets))
		{ }

		public RequiredIfRule(string rootType, string property, string compareSource, CompareOperator compareOperator, object compareValue, RuleInvocationType invocationTypes, Error error)
			: base(rootType, property, error, invocationTypes, CompareRule.GetPredicates(rootType, property, compareSource))
		{
			this.CompareSource = compareSource;
			this.CompareOperator = compareOperator;
			this.CompareValue = compareValue;
		}
		#endregion

		#region Properties

		/// <summary>
		/// Gets the path to the comparison property.
		/// </summary>
		[DataMember(Name = "compareSource")]
		public string CompareSource
		{
			get
			{
				return compareSource.Path;
			}
			private set
			{
				compareSource = new PathSource(Property.DeclaringType, value);
			}
		}

		/// <summary>
		/// Indicates whether the compare source is static.
		/// </summary>
		public bool CompareSourceIsStatic
		{
			get
			{
				return compareSource.IsStatic;
			}
		}

		/// <summary>
		/// The value to compare to.
		/// </summary>
		[DataMember(Name = "compareValue")]
		public object CompareValue
		{
			get;
			private set;
		}

		/// <summary>
		/// The type of comparison to perform.
		/// </summary>
		public CompareOperator CompareOperator
		{
			get;
			private set;
		}

		/// <summary>
		/// Private text version of <see cref="CompareOperator"/> to support WCF serialization.
		/// </summary>
		[DataMember(Name = "compareOperator")]
		string CompareOperatorText
		{
			get
			{
				return CompareOperator.ToString();
			}
			set
			{
				CompareOperator = (CompareOperator)Enum.Parse(typeof(CompareOperator), value);
			}
		}

		#endregion

		#region Methods

		static Error CreateError(string rootType, string property, string compareSource, CompareOperator compareOperator, object compareValue, Func<string> label, Func<string> compareLabel, Func<object, string> format, params ConditionTypeSet[] sets)
		{
			// Determine the appropriate error message
			string message;
			if (compareValue == null)
				message = compareOperator == CompareOperator.Equal ? "required-if-not-exists" : "required-if-exists";
			else
			{
				bool isDate = compareValue is DateTime;
				switch (compareOperator)
				{
					case CompareOperator.Equal: 
						message = "required-if-equal"; 
						break;
					case CompareOperator.NotEqual: 
						message = "required-if-not-equal"; 
						break;
					case CompareOperator.GreaterThan:
						message = isDate ? "required-if-after" : "required-if-greater-than"; 
						break;
					case CompareOperator.GreaterThanEqual:
						message = isDate ? "required-if-on-or-after": "required-if-greater-than-or-equal";
						break;
					case CompareOperator.LessThan:
						message = isDate ? "required-if-before" : "required-if-less-than";
						break;
					case CompareOperator.LessThanEqual:
						message = isDate ? "required-if-on-or-before" : "required-if-less-than-or-equal";
						break;
					default:
						throw new ArgumentException("Invalid comparison operator for required if rule");
				}
			}

			// Create and return the error
			return new Error(
				GetErrorCode(rootType, property, "RequiredIf"), message, typeof(RequiredIfRule),
				(s) => s
					.Replace("{property}", label())
					.Replace("{compareSource}", compareLabel())
					.Replace("{compareValue}", compareValue == null ? "" : format(compareValue)), sets);
		}

		protected override bool ConditionApplies(GraphInstance root)
		{
			// Exit immediately if the target property has a value
			if (root[Property] != null ||
				(Property is GraphReferenceProperty && Property.IsList && root.GetList((GraphReferenceProperty)Property).Count > 0))
				return false;

			// If the value to compare is null, then evaluate whether the compare source has a value
			if (CompareValue == null)
				return CompareOperator == CompareOperator.Equal ? !compareSource.HasValue(root) : compareSource.HasValue(root);

			// Otherwise, perform a comparison of the compare source relative to the compare value
			return CompareRule.Compare(root, compareSource.GetValue(root), CompareOperator, CompareValue);
		}

		protected override string TypeName
		{
			get
			{
				return "requiredIf";
			}
		}

		#endregion
	}
}
