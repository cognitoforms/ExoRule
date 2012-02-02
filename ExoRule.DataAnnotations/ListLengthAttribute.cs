using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using ExoModel;
using System.ComponentModel.DataAnnotations;
using ExoRule.Validation;

namespace ExoRule.DataAnnotations
{
	[AttributeUsage(AttributeTargets.Property, AllowMultiple = false)]
	public class ListLengthAttribute : ValidationAttribute
	{
		/// <summary>
		/// 
		/// </summary>
		/// <param name="source"></param>
		/// <param name="staticLength">A positive integer that describes the required length of the array</param>
		public ListLengthAttribute(int staticLength, CompareOperator op)
		{
			this.LengthCompareProperty = null;
			this.StaticLength = staticLength;
			this.CompareOp = op;
		}

		public ListLengthAttribute(string lengthProperty, CompareOperator op)
		{
			this.LengthCompareProperty = lengthProperty;
			this.StaticLength = -1;
			this.CompareOp = op;
		}

		public string LengthCompareProperty { get; private set; }
		public int StaticLength { get; set; }
		public CompareOperator CompareOp { get; set; }

		protected override ValidationResult IsValid(object value, ValidationContext validationContext)
		{
			var instance = ModelContext.Current.GetModelInstance(validationContext.ObjectInstance);
			var property = instance.Type.Properties[validationContext.MemberName];
			int integerLengthValue = 0;

			if (LengthCompareProperty == null && StaticLength < 0)
				return null;

			if (LengthCompareProperty == null)
			{
				integerLengthValue = StaticLength;
			}
			else
			{
				var lengthProp = new ModelSource(instance.Type, LengthCompareProperty);

				// Get the integer length of the property.  If the property is not an integer return null
				object lengthPropertyValue = lengthProp.GetValue(instance);
				if (lengthPropertyValue == null)
					return null;

				bool success = Int32.TryParse(lengthPropertyValue.ToString(), out integerLengthValue);
				if (!success)
					return null;
			}

			// List Property
			if (property.IsList)
			{
				// Get the current property value
				ModelInstanceList items = instance.GetList((ModelReferenceProperty)property);

				// Determine whether the list size passes the operator's test
				switch (CompareOp)
				{
					//if they are not equal then it does not pass the equals test
					//comparison is opposite of the operator the user selected
					case CompareOperator.Equal:
						if (items != null && items.Count != integerLengthValue)
							return new ValidationResult("Invalid value", new string[] { validationContext.MemberName });
						break;
					case CompareOperator.NotEqual:
						if (items != null && items.Count == integerLengthValue)
							return new ValidationResult("Invalid value", new string[] { validationContext.MemberName });
						break;
					case CompareOperator.GreaterThan:
						if (items != null && items.Count <= integerLengthValue)
							return new ValidationResult("Invalid value", new string[] { validationContext.MemberName });
						break;
					case CompareOperator.GreaterThanEqual:
						if (items != null && items.Count < integerLengthValue)
							return new ValidationResult("Invalid value", new string[] { validationContext.MemberName });
						break;
					case CompareOperator.LessThan:
						if (items != null && items.Count >= integerLengthValue)
							return new ValidationResult("Invalid value", new string[] { validationContext.MemberName });
						break;
					case CompareOperator.LessThanEqual:
						if (items != null && items.Count > integerLengthValue)
							return new ValidationResult("Invalid value", new string[] { validationContext.MemberName });
						break;
					default: return null;
				}

				return null;
			}
			// Reference Property
			else
			{
				//if the property this annotation is applied to is not a list, then this validation is not applicable
				return null;
			}
		}
	}
}
