using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.ComponentModel.DataAnnotations;
using ExoModel;
using System.Text.RegularExpressions;
using ExoRule.Validation;
using System.Reflection;

namespace ExoRule.DataAnnotations
{
	/// <summary>
	/// Automatically creates rules based on the presence of data annotation attributes
	/// on properties of the specified model types.
	/// </summary>
	public class AnnotationsRuleProvider : IRuleProvider
	{
		#region Fields

		IEnumerable<Type> types;
		List<Rule> rules;

		#endregion

		#region Constructors

		/// <summary>
		/// Automatically creates property validation rules for the specified <see cref="ModelType"/> instances
		/// based on data annotation attributes associated with properties declared on each type.
		/// </summary>
		/// <param name="types"></param>
		public AnnotationsRuleProvider(IEnumerable<Type> types)
		{
			this.types = types;
		}

		/// <summary>
		/// Automatically creates property validation rules for the specified <see cref="ModelType"/> instances
		/// based on data annotation attributes associated with properties declared on each type.
		/// </summary>
		/// <param name="assembly"></param>
		public AnnotationsRuleProvider(params Assembly[] assemblies)
		{
			this.types = assemblies.SelectMany(a => a.GetTypes());
		}

		#endregion

		#region Methods

		/// <summary>
		/// Gets the set of precondition rules created by the provider.
		/// </summary>
		/// <param name="name"></param>
		/// <returns></returns>
		public virtual IEnumerable<Rule> GetRules(Type sourceType, string name)
		{
			if (rules == null)
			{
				// Process each type
				var context = ModelContext.Current;
				rules = new List<Rule>();
				foreach (var type in types.Select(t => context.GetModelType(t)).Where(t => t != null))
				{
					// Process each instance property declared on the current type
					foreach (var property in type.Properties.Where(property => property.DeclaringType == type && !property.IsStatic))
					{
						// Required Attribute
						foreach (var attr in property.GetAttributes<RequiredAttribute>().Take(1))
						{
							object requiredValue = null;
							if (property is ModelValueProperty && ((ModelValueProperty)property).PropertyType == typeof(bool))
								requiredValue = true;

							// Use the error message if one is specifed, otherwise use the default bahavior
							if (string.IsNullOrEmpty(attr.ErrorMessage))
								rules.Add(new RequiredRule(type.Name, property.Name, requiredValue));
							else
								rules.Add(new RequiredRule(type.Name, property.Name, attr.ErrorMessage, requiredValue));
						}

						// String Length Attribute
						foreach (var attr in property.GetAttributes<StringLengthAttribute>().Take(1))
						{
							// Use the error message if one is specifed, otherwise use the default bahavior
							if (string.IsNullOrEmpty(attr.ErrorMessage))
								rules.Add(new StringLengthRule(type.Name, property.Name, attr.MinimumLength, attr.MaximumLength));
							else
								rules.Add(new StringLengthRule(type.Name, property.Name, attr.MinimumLength, attr.MaximumLength, attr.ErrorMessage));
						}

						// Range Attribute
						foreach (var attr in property.GetAttributes<RangeAttribute>().Take(1))
						{
							// Use the error message if one is specifed, otherwise use the default bahavior
							if (string.IsNullOrEmpty(attr.ErrorMessage))
								rules.Add(new RangeRule(type.Name, property.Name, (IComparable)attr.Minimum, (IComparable)attr.Maximum));
							else
								rules.Add(new RangeRule(type.Name, property.Name, (IComparable)attr.Minimum, (IComparable)attr.Maximum, attr.ErrorMessage));
						}

						//Compare Attribute
						foreach (var attr in property.GetAttributes<CompareAttribute>().Take(1))
						{
							// Use the error message if one is specifed, otherwise use the default bahavior
							if (string.IsNullOrEmpty(attr.ErrorMessage))
								rules.Add(new CompareRule(type.Name, property.Name, attr.ComparisonPropertyName, attr.Operator));
							else
								rules.Add(new CompareRule(type.Name, property.Name, attr.ComparisonPropertyName, attr.Operator, attr.ErrorMessage));
						}

						// ListLength Attribute
						foreach (var attr in property.GetAttributes<ListLengthAttribute>().Take(1))
						{
							// Use the error message if one is specifed, otherwise use the default bahavior
							if (string.IsNullOrEmpty(attr.ErrorMessage))
								rules.Add(new ListLengthRule(type.Name, property.Name, attr.StaticLength, attr.LengthCompareProperty, attr.CompareOp));
							else 
								rules.Add(new ListLengthRule(type.Name, property.Name, attr.StaticLength, attr.LengthCompareProperty, attr.CompareOp, attr.ErrorMessage));
						}

                        // Regular Expression Attribute
                        foreach (var attr in property.GetAttributes<RegularExpressionAttribute>().Take(1))
							rules.Add(new StringFormatRule(type.Name, property.Name, () => attr.ErrorMessage, () => new Regex(attr.Pattern), () => (attr is RegularExpressionReformatAttribute) ? ((RegularExpressionReformatAttribute)attr).ReformatExpression : null, RuleInvocationType.PropertyChanged));
                        
                        // Allowed Values Attribute
						ModelReferenceProperty reference = property as ModelReferenceProperty;
						if (reference != null)
						{
							string errorMessage = null;
							foreach (var attr in property.GetAttributes<AllowedValuesAttribute>().Take(1))
								errorMessage = attr.ErrorMessage;

							foreach (var source in property.GetAttributes<AllowedValuesAttribute>()
								.Select(attr => attr.Source)
								.Union(reference.PropertyType.GetAttributes<AllowedValuesAttribute>()
								.Select(attr => attr.Source.Contains('.') ? attr.Source : reference.PropertyType.Name + '.' + attr.Source)
								.Take(1)))
								rules.Add(string.IsNullOrEmpty(errorMessage) ? new AllowedValuesRule(type.Name, property.Name, source) : new AllowedValuesRule(type.Name, property.Name, source, errorMessage));
						}
					}
				}
			}
			return rules;
		}

		#endregion
	}
}
