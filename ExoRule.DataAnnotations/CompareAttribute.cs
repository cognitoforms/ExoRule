using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using ExoGraph;
using System.ComponentModel.DataAnnotations;
using ExoRule.Validation;

namespace ExoRule.DataAnnotations
{
	#region CompareAttribute


	[AttributeUsage(AttributeTargets.Property | AttributeTargets.Field, AllowMultiple = false, Inherited = true)]
	public class CompareAttribute : ValidationAttribute
	{
		/// <summary>
		/// 
		/// </summary>
		/// <param name="propertyName">Name of the property to compare with</param>
		/// <param name="op">The operator used to define the comparison</param>
		public CompareAttribute(string propertyName, CompareOperator op)
			: base(propertyName)
		{
			this.Operator = op;
			this.ComparisonPropertyName = propertyName;
		}

		#region Methods

		protected override ValidationResult IsValid(object value, ValidationContext validationContext)
		{
			GraphInstance instance = GraphContext.Current.GetGraphInstance(validationContext.ObjectInstance);
			GraphProperty sourceProperty = instance.Type.Properties[validationContext.MemberName];

			if (ComparisonPropertyName == null)
				return null;

			PathSource comparePropPath = new PathSource(instance.Type, ComparisonPropertyName);

			//Cannot perform comparison on a list property
			if (!sourceProperty.IsList && !comparePropPath.SourceProperty.IsList)
			{
				// Get the current property value

				object compareValue = comparePropPath.GetValue(instance);

				int comparison = ((IComparable)compareValue).CompareTo(value);
				switch (Operator)
				{
					case CompareOperator.Equal: return comparison == 0 ? null : new ValidationResult("Invalid value", new string[] { validationContext.MemberName });
					case CompareOperator.NotEqual: return comparison != 0 ? null : new ValidationResult("Invalid value", new string[] { validationContext.MemberName });
					case CompareOperator.GreaterThan: return comparison < 0 ? null : new ValidationResult("Invalid value", new string[] { validationContext.MemberName });
					case CompareOperator.GreaterThanEqual: return comparison <= 0 ? null : new ValidationResult("Invalid value", new string[] { validationContext.MemberName });
					case CompareOperator.LessThan: return comparison > 0 ? null : new ValidationResult("Invalid value", new string[] { validationContext.MemberName });
					case CompareOperator.LessThanEqual: return comparison >= 0 ? null : new ValidationResult("Invalid value", new string[] { validationContext.MemberName });
				}

				return null;
			}
			// List Property
			else
			{
				//if the property this annotation is applied to is a list, then this validation is not applicable
				return null;
			}
		}

		#endregion

		#region Properties

		public CompareOperator Operator { get; set; }

		public string ComparisonPropertyName  { get; set; }

		#endregion
	}

	#endregion
}
