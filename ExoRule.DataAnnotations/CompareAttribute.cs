using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using ExoModel;
using System.ComponentModel.DataAnnotations;
using ExoRule.Validation;

namespace ExoRule.DataAnnotations
{
	#region CompareAttribute


	[AttributeUsage(AttributeTargets.Property | AttributeTargets.Field, AllowMultiple = true, Inherited = true)]
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
			ModelInstance instance = ModelInstance.GetModelInstance(validationContext.ObjectInstance);
			ModelProperty sourceProperty = instance.Type.Properties[validationContext.MemberName];

			if (ComparisonPropertyName == null)
				return null;

			ModelSource comparePropPath = new ModelSource(instance.Type, ComparisonPropertyName);

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

		#endregion

		#region Properties

		public CompareOperator Operator { get; set; }

		public string ComparisonPropertyName  { get; set; }

		#endregion
	}

	#endregion
}
