using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using ExoModel;
using System.ComponentModel.DataAnnotations;
using ExoRule.Validation;

namespace ExoRule.DataAnnotations
{
public class AllowedValuesAttribute : ValidationAttribute
{
	public AllowedValuesAttribute(string source)
	{
		this.Source = source;
	}

	public string Source { get; private set; }

	protected override ValidationResult IsValid(object value, ValidationContext validationContext)
	{
		var instance = ModelContext.Current.GetModelInstance(validationContext.ObjectInstance);
		var source = new ModelSource(instance.Type, Source);
		var property = instance.Type.Properties[validationContext.MemberName];

		// Get the list of allowed values
		ModelInstanceList allowedValues = source.GetList(instance);
		if (allowedValues == null)
			return null;

		// List Property
		if (property.IsList)
		{
			// Get the current property value
			ModelInstanceList items = instance.GetList((ModelReferenceProperty)property);

			// Determine whether the property value is in the list of allowed values
			if (!(items == null || items.All(item => allowedValues.Contains(item))))
				return new ValidationResult("Invalid value", new string[] { validationContext.MemberName });
		}

		// Reference Property
		else
		{
			// Get the current property value
			ModelInstance item = instance.GetReference((ModelReferenceProperty)property);

			// Determine whether the property value is in the list of allowed values
			if (!(item == null || allowedValues.Contains(item)))
				return new ValidationResult("Invalid value", new string[] { validationContext.MemberName });
		}

		return null;
	}
}


}
