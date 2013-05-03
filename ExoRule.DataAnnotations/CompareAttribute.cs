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

			// Get the member name by looking up using the display name, since the member name is mysteriously null for MVC3 projects
			var propertyName = validationContext.MemberName ?? validationContext.ObjectType.GetProperties()
				.Where(p => p.GetCustomAttributes(false).OfType<DisplayAttribute>()
					.Any(a => a.Name == validationContext.DisplayName)).Select(p => p.Name).FirstOrDefault();

			ModelProperty sourceProperty = instance.Type.Properties[propertyName];

			if (ComparisonPropertyName == null)
				return null;

			ModelSource comparePropPath = new ModelSource(instance.Type, ComparisonPropertyName);

			object compareValue = comparePropPath.GetValue(instance);

			int comparison = ((IComparable)compareValue).CompareTo(value);
			switch (Operator)
			{
				case CompareOperator.Equal: return comparison == 0 ? null : new ValidationResult("Invalid value", new string[] { propertyName });
				case CompareOperator.NotEqual: return comparison != 0 ? null : new ValidationResult("Invalid value", new string[] { propertyName });
				case CompareOperator.GreaterThan: return comparison < 0 ? null : new ValidationResult("Invalid value", new string[] { propertyName });
				case CompareOperator.GreaterThanEqual: return comparison <= 0 ? null : new ValidationResult("Invalid value", new string[] { propertyName });
				case CompareOperator.LessThan: return comparison > 0 ? null : new ValidationResult("Invalid value", new string[] { propertyName });
				case CompareOperator.LessThanEqual: return comparison >= 0 ? null : new ValidationResult("Invalid value", new string[] { propertyName });
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
