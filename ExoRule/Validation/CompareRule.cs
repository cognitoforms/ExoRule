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
	public class CompareRule : PropertyRule
	{
		#region Fields

		ModelSource compareSource;

		#endregion

		#region Constructors

		/// <summary>
		/// Creates a new instance of <see cref="CompareRule"/> for the specified property.
		/// </summary>
		/// <param name="rootType"></param>
		/// <param name="property"></param>
		/// <param name="conditionType"></param>
		/// <param name="comparePath"></param>
		/// <param name="compareOperator"></param>
		/// <param name="invocationTypes"></param>
		public CompareRule(string rootType, string property, string compareSource, CompareOperator compareOperator)
			: this(rootType, property, compareSource, compareOperator, RuleInvocationType.PropertyChanged)
		{ }

		/// <summary>
		/// Creates a new instance of <see cref="CompareRule"/> for the specified property.
		/// </summary>
		/// <param name="rootType"></param>
		/// <param name="property"></param>
		/// <param name="conditionType"></param>
		/// <param name="comparePath"></param>
		/// <param name="compareOperator"></param>
		/// <param name="invocationTypes"></param>
		public CompareRule(string rootType, string property, string compareSource, CompareOperator compareOperator, RuleInvocationType invocationTypes)
			: base(rootType, property, CreateError(rootType, property, compareSource, compareOperator), invocationTypes, GetPredicates(rootType, property, compareSource))
		{
			// Get the model property from the base class
			ModelProperty p = Property;

			// Verify that the target property is supported by the rule
			if (p is ModelReferenceProperty)
			{
				if (p.IsList)
					throw new ArgumentException("The CompareRule does not support comparing list properties.");

				if (compareOperator != CompareOperator.Equal && compareOperator != CompareOperator.NotEqual)
					throw new ArgumentException("The CompareRule only supports the Equal and NotEqual operators for reference properties.");
			}
			else if (!typeof(IComparable).IsAssignableFrom(((ModelValueProperty)p).PropertyType))
				throw new ArgumentException("The CompareRule only supports value properties that implement IComparable.");

			this.CompareSource = compareSource;
			this.CompareOperator = compareOperator;
		}

		#endregion

		#region Properties

		/// <summary>
		/// The static or instance path of the property to compare to.
		/// </summary>
		public string CompareSource
		{
			get
			{
				return compareSource.Path;
			}
			private set
			{
				compareSource = new ModelSource(Property.DeclaringType, value);
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
		/// The type of comparison to perform.
		/// </summary>
		public CompareOperator CompareOperator
		{
			get;
			private set;
		}

		public string CompareOperatorText
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

		static Error CreateError(string rootType, string property, string compareSource, CompareOperator compareOperator)
		{
			// Determine the appropriate error message
			string message;
			var rootModelType = ModelContext.Current.GetModelType(rootType);
			var p = rootModelType.Properties[property];
			bool isDate = p is ModelValueProperty && ((ModelValueProperty)p).PropertyType == typeof(DateTime);
			switch (compareOperator)
			{
				case CompareOperator.Equal:
					message = "compare-equal";
					break;
				case CompareOperator.NotEqual:
					message = "compare-not-equal";
					break;
				case CompareOperator.GreaterThan:
					message = isDate ? "compare-after" : "compare-greater-than";
					break;
				case CompareOperator.GreaterThanEqual:
					message = isDate ? "compare-on-or-after" : "compare-greater-than-or-equal";
					break;
				case CompareOperator.LessThan:
					message = isDate ? "compare-before" : "compare-less-than";
					break;
				case CompareOperator.LessThanEqual:
					message = isDate ? "compare-on-or-before" : "compare-less-than-or-equal";
					break;
				default:
					throw new ArgumentException("Invalid comparison operator for compare rule");
			}

			// Get the comparison source
			var source = new ModelSource(rootModelType, compareSource);
			var sourceType = source.SourceType;
			var sourceProperty = source.SourceProperty;

			// Create and return the error
			return new Error(
				GetErrorCode(rootType, property, "Compare"), message, typeof(CompareRule),
				(s) => s
					.Replace("{property}", GetLabel(rootType, property))
					.Replace("{compareSource}", GetLabel(sourceType, sourceProperty)));
		}

		internal static string[] GetPredicates(string rootType, string property, string compareSource)
		{
			ModelSource source = new ModelSource(ModelContext.Current.GetModelType(rootType), compareSource);
			return source.IsStatic ? new string[] { property } : new string[] { property, compareSource };
		}

		/// <summary>
		/// Determines whether the comparison conditions are met by the given source value and compare value.
		/// </summary>
		/// <param name="sourceValue">The source value.</param>
		/// <param name="compareOperator">The comparison operator.</param>
		/// <param name="compareValue">The compare value.</param>
		/// <returns>True if the comparison passes, false if the comparison fails.</returns>
		protected internal static bool? Compare(object sourceValue, CompareOperator compareOperator, object compareValue)
		{
			if (sourceValue == null && compareValue == null)
			{
				if (compareOperator == CompareOperator.Equal) return true;
				else if (compareOperator == CompareOperator.NotEqual) return false;
				else return null;
			}
			else if (sourceValue == null || compareValue == null)
			{
				if (compareOperator == CompareOperator.Equal) return false;
				else if (compareOperator == CompareOperator.NotEqual) return true;
				else return null;
			}

			// Perform the comparison and return the result
			int compareResult = ((IComparable)sourceValue).CompareTo(compareValue);
			switch (compareOperator)
			{
				case CompareOperator.Equal: return compareResult == 0;
				case CompareOperator.NotEqual: return compareResult != 0;
				case CompareOperator.GreaterThan: return compareResult > 0;
				case CompareOperator.GreaterThanEqual: return compareResult >= 0;
				case CompareOperator.LessThan: return compareResult < 0;
				case CompareOperator.LessThanEqual: return compareResult <= 0;
				default: return false;
			}
		}

		/// <summary>
		/// Determines whether the value of the property is valid relative to the value of a comparison property.
		/// </summary>
		/// <param name="root"></param>
		/// <returns></returns>
		protected override bool ConditionApplies(ModelInstance root)
		{
			bool? result = Compare(root[Property], CompareOperator, compareSource.GetValue(root));
			return result.HasValue && !result.Value;
		}

		#endregion
	}
}
