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
		var instance = ModelInstance.GetModelInstance(validationContext.ObjectInstance);
		var source = new ModelSource(instance.Type, Source);

		// Get the member name by looking up using the display name, since the member name is mysteriously null for MVC3 projects
		var propertyName = validationContext.MemberName ?? validationContext.ObjectType.GetProperties()
			.Where(p => p.GetCustomAttributes(false).OfType<DisplayAttribute>()
				.Any(a => a.Name == validationContext.DisplayName)).Select(p => p.Name).FirstOrDefault();

		var property = instance.Type.Properties[propertyName];

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
				return new ValidationResult("Invalid value", new string[] { propertyName });
		}

		// Reference Property
		else
		{
			// Get the current property value
			ModelInstance item = instance.GetReference((ModelReferenceProperty)property);

			// Determine whether the property value is in the list of allowed values
			if (!(item == null || allowedValues.Contains(item)))
				return new ValidationResult("Invalid value", new string[] { propertyName });
		}

		return null;
	}
}


}
