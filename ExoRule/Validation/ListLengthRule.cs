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
	/// Applies conditions when the length of a list of a <see cref="GraphProperty"/> is
	/// too short or long.
	/// </summary>
	[DataContract(Name = "listLength")]
	public class ListLengthRule : PropertyRule
	{
		#region Fields

		PathSource compareLengthSource;

		#endregion

		#region Constructors

		public ListLengthRule(string rootType, string property, int staticLength, string compareLengthSource, CompareOperator op, Func<string> label, Func<string> compareLabel)
			: this(rootType, property, staticLength, compareLengthSource, op, label, compareLabel, RuleInvocationType.PropertyChanged)
		{ }

		public ListLengthRule(string rootType, string property, int staticLength, string compareLengthSource, CompareOperator op, Func<string> label, Func<string> compareLabel, RuleInvocationType invocationTypes)
			: base(rootType, property, CreateError(rootType, property, staticLength, compareLengthSource, op, label, compareLabel), invocationTypes)
		{
			this.StaticLength = staticLength;
			this.CompareLengthSource = compareLengthSource;
			this.CompareOperator = op;
		}

		#endregion

		#region Properties

		[DataMember(Name = "staticLength", EmitDefaultValue = false)]
		public int StaticLength { get; private set; }

		/// <summary>
		/// The static or instance path of the property to compare to.
		/// </summary>
		[DataMember(Name = "compareLengthSource")]
		public string CompareLengthSource
		{
			get
			{
				return compareLengthSource == null ? "" : compareLengthSource.Path;
			}
			private set
			{
				if (!String.IsNullOrEmpty(value))
					compareLengthSource = new PathSource(Property.DeclaringType, value);
				else
					compareLengthSource = null;
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
		public bool CompareLengthSourceIsStatic
		{
			get
			{
				//return true when the compare property does not exist because we do not 
				//want the code using this property tack a "this." on the front of the property path
				return compareLengthSource == null ? true : compareLengthSource.IsStatic;
			}
		}

		#endregion

		#region Methods

		static Error CreateError(string rootType, string property, int staticLength, string compareLengthSource, CompareOperator op, Func<string> label, Func<string> compareLabel)
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

			return new Error(
				GetErrorCode(rootType, property, "ListLength"), message, typeof(ListLengthRule),
				(s) => s
					.Replace("{property}", label())
					.Replace("{compareSource}", staticLength >= 0 ? staticLength.ToString() : compareLabel()));
		}

		protected override bool ConditionApplies(GraphInstance root)
		{
			//if the Property is not a list then this rule cannot apply
			if (!Property.IsList)
				return false;

			//value should be the list you are comparing against
			object value = root[Property];

			//get the exograph list representation
			GraphInstanceList items = root.GetList((GraphReferenceProperty)Property);

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
				object compareVal = compareLengthSource.GetValue(root);
				bool success = Int32.TryParse(compareVal.ToString(), out lengthToCompareAgainst);

				//if it could not parse then this is not a valid integer property
				if (!success)
					return false;
			}

			bool? result = CompareRule.Compare(root, items.Count, CompareOperator, lengthToCompareAgainst);
			return !result.HasValue ? false : result.Value;
		}

		protected override string TypeName
		{
			get
			{
				return "listLength";
			}
		}

		#endregion
	}
}