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
	/// Applies conditions when the length of a list of a <see cref="ModelProperty"/> is
	/// too short or long.
	/// </summary>
	public class ListLengthRule : PropertyRule
	{
		#region Fields

		ModelSource compareSource;

		#endregion

		#region Constructors

		public ListLengthRule(string rootType, string property, int compareValue, CompareOperator compareOperator)
			: this(rootType, property, compareValue, compareOperator, RuleInvocationType.InitNew | RuleInvocationType.PropertyChanged)
		{ }

		public ListLengthRule(string rootType, string property, int compareValue, CompareOperator compareOperator, RuleInvocationType invocationTypes)
			: this(rootType, property, compareValue, compareOperator, CreateError(rootType, property, compareValue, null, compareOperator), invocationTypes)
		{ }

		public ListLengthRule(string rootType, string property, int compareValue, CompareOperator compareOperator, string errorMessage)
			: this(rootType, property, compareValue, compareOperator, new Error(GetErrorCode(rootType, property, "ListLength"), errorMessage, null), RuleInvocationType.InitNew | RuleInvocationType.PropertyChanged)
		{ }

		public ListLengthRule(string rootType, string property, int compareValue, CompareOperator compareOperator, Error error, RuleInvocationType invocationTypes)
			: base(rootType, property, error, invocationTypes)
		{
			this.CompareValue = compareValue;
			this.CompareOperator = compareOperator;
		}

		public ListLengthRule(string rootType, string property, int compareValue, string compareSource, CompareOperator compareOperator)
			: this(rootType, property, compareSource, compareOperator, RuleInvocationType.InitNew | RuleInvocationType.PropertyChanged)
		{ }

		public ListLengthRule(string rootType, string property, string compareSource, CompareOperator compareOperator, RuleInvocationType invocationTypes)
			: this(rootType, property, compareSource, compareOperator, CreateError(rootType, property, 0, compareSource, compareOperator), invocationTypes)
		{ }

		public ListLengthRule(string rootType, string property, int compareValue, string compareSource, CompareOperator compareOperator, string errorMessage)
			: this(rootType, property, compareSource, compareOperator, new Error(GetErrorCode(rootType, property, "ListLength"), errorMessage, null), RuleInvocationType.InitNew | RuleInvocationType.PropertyChanged)
		{ }

		public ListLengthRule(string rootType, string property, string compareSource, CompareOperator compareOperator, Error error, RuleInvocationType invocationTypes)
			: base(rootType, property, error, invocationTypes)
		{
			this.CompareSource = compareSource;
			this.CompareOperator = compareOperator;
		}

		#endregion

		#region Properties

		public int CompareValue { get; private set; }

		/// <summary>
		/// The static or instance path of the property to compare to.
		/// </summary>
		public string CompareSource
		{
			get
			{
				return compareSource == null ? "" : compareSource.Path;
			}
			private set
			{
				if (!String.IsNullOrEmpty(value))
					compareSource = new ModelSource(Property.DeclaringType, value);
				else
					compareSource = null;
			}
		}

		/// <summary>
		/// The type of comparison to perform.
		/// </summary>
		public CompareOperator CompareOperator
		{
			get;
			private set;
		}

		#endregion

		#region Methods

		static Error CreateError(string rootType, string property, int compareValue, string compareSource, CompareOperator op)
		{
			string message;
			switch (op)
			{
				case CompareOperator.Equal:
					message = "listlength-compare-equal";
					break;
				case CompareOperator.NotEqual:
					message = "listlength-compare-not-equal";
					break;
				case CompareOperator.GreaterThan:
					message = "listlength-compare-greater-than";
					break;
				case CompareOperator.GreaterThanEqual:
					message = "listlength-compare-greater-than-or-equal";
					break;
				case CompareOperator.LessThan:
					message = "listlength-compare-less-than";
					break;
				case CompareOperator.LessThanEqual:
					message = "listlength-compare-less-than-or-equal";
					break;
				default:
					throw new ArgumentException("Invalid comparison operator for list length rule");
			}

			// Get the comparison source
			if (!String.IsNullOrEmpty(compareSource))
			{
				var source = new ModelSource(ModelContext.Current.GetModelType(rootType), compareSource);
				var sourceType = source.SourceType;
				var sourceProperty = source.SourceProperty;

				return new Error(
					GetErrorCode(rootType, property, "ListLength"), message, typeof(ListLengthRule),
					(s) => s
						.Replace("{property}", GetLabel(rootType, property))
						.Replace("{compareSource}", GetLabel(sourceType, sourceProperty)));
			}
			else
			{
				//there is no compare source and we are using a static length
				return new Error(
					GetErrorCode(rootType, property, "ListLength"), message, typeof(ListLengthRule),
					(s) => s
						.Replace("{property}", GetLabel(rootType, property))
						.Replace("{compareSource}", compareValue.ToString()));
			}
		}

		protected override bool ConditionApplies(ModelInstance root)
		{
			//if the Property is not a list then this rule cannot apply
			if (!Property.IsList)
				return false;

			//value should be the list you are comparing against
			object value = root[Property];

			//get the exomodel list representation
			ModelInstanceList items = root.GetList((ModelReferenceProperty)Property);

			if (value == null)
				return false;

			int lengthToCompareAgainst = 0;

			//now see if the rule is using a static length or the compare property
			if (CompareSource == null)
				lengthToCompareAgainst = CompareValue;
			else
			{
				object compareVal = compareSource.GetValue(root);
				bool success = Int32.TryParse(compareVal + "", out lengthToCompareAgainst);

				//if it could not parse then this is not a valid integer property
				if (!success)
					return false;
			}

			bool? result = CompareRule.Compare(items.Count, CompareOperator, lengthToCompareAgainst);
			return !result.HasValue ? false : result.Value;
		}

		#endregion
	}
}