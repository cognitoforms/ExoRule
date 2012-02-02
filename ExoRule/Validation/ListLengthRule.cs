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

		public ListLengthRule(string rootType, string property, int staticLength, string compareSource, CompareOperator op)
			: this(rootType, property, staticLength, compareSource, op, RuleInvocationType.PropertyChanged)
		{ }

		public ListLengthRule(string rootType, string property, int staticLength, string compareSource, CompareOperator op, RuleInvocationType invocationTypes)
			: base(rootType, property, CreateError(rootType, property, staticLength, compareSource, op), invocationTypes)
		{
			this.StaticLength = staticLength;
			this.CompareSource = compareSource;
			this.CompareOperator = op;
		}

		#endregion

		#region Properties

		public int StaticLength { get; private set; }

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

		/// <summary>
		/// Indicates whether the compare source is static.
		/// </summary>
		public bool CompareSourceIsStatic
		{
			get
			{
				//return true when the compare property does not exist because we do not 
				//want the code using this property tack a "this." on the front of the property path
				return compareSource == null ? true : compareSource.IsStatic;
			}
		}

		#endregion

		#region Methods

		static Error CreateError(string rootType, string property, int staticLength, string compareSource, CompareOperator op)
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
			var source = new ModelSource(ModelContext.Current.GetModelType(rootType), compareSource);
			var sourceType = source.SourceType;
			var sourceProperty = source.SourceProperty;

			return new Error(
				GetErrorCode(rootType, property, "ListLength"), message, typeof(ListLengthRule),
				(s) => s
					.Replace("{property}", GetLabel(rootType, property))
					.Replace("{compareSource}", staticLength >= 0 ? staticLength.ToString() : GetLabel(sourceType, sourceProperty)));
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
			if (StaticLength > 0)
			{
				lengthToCompareAgainst = StaticLength;
			}
			else
			{
				object compareVal = compareSource.GetValue(root);
				bool success = Int32.TryParse(compareVal.ToString(), out lengthToCompareAgainst);

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