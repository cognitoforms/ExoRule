using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.ComponentModel.DataAnnotations;
using ExoGraph;
using System.Text.RegularExpressions;
using ExoRule.Validation;
using System.Reflection;

namespace ExoRule.DataAnnotations
{
	/// <summary>
	/// Automatically creates rules based on the presence of data annotation attributes
	/// on properties of the specified graph types.
	/// </summary>
	public class AnnotationsRuleProvider : IRuleProvider
	{
		#region Fields

		IEnumerable<Type> types;
		List<Rule> rules;

		#endregion

		#region Constructors

		/// <summary>
		/// Automatically creates property validation rules for the specified <see cref="GraphType"/> instances
		/// based on data annotation attributes associated with properties declared on each type.
		/// </summary>
		/// <param name="types"></param>
		public AnnotationsRuleProvider(IEnumerable<Type> types)
		{
			this.types = types;
		}

		/// <summary>
		/// Automatically creates property validation rules for the specified <see cref="GraphType"/> instances
		/// based on data annotation attributes associated with properties declared on each type.
		/// </summary>
		/// <param name="assembly"></param>
		public AnnotationsRuleProvider(Assembly assembly)
		{
			this.types = assembly.GetTypes();
		}

		#endregion

		#region Methods

		/// <summary>
		/// Gets the set of precondition rules created by the provider.
		/// </summary>
		/// <param name="name"></param>
		/// <returns></returns>
		IEnumerable<Rule> IRuleProvider.GetRules(Type sourceType, string name)
		{
			if (rules == null)
			{
				// Process each type
				var context = GraphContext.Current;
				rules = new List<Rule>();
				foreach (var type in types.Select(t => context.GetGraphType(t)).Where(t => t != null))
				{
					// Process each instance property declared on the current type
					foreach (var property in type.Properties.Where(property => property.DeclaringType == type && !property.IsStatic))
					{
						// Required Attribute
						foreach (var attr in property.GetAttributes<RequiredAttribute>().Take(1))
							rules.Add(new RequiredRule(type.Name, property.Name));

						// String Length Attribute
						foreach (var attr in property.GetAttributes<StringLengthAttribute>().Take(1))
							rules.Add(new StringLengthRule(type.Name, property.Name, attr.MinimumLength, attr.MaximumLength));

						// Range Attribute
						foreach (var attr in property.GetAttributes<RangeAttribute>().Take(1))
							rules.Add(new RangeRule(type.Name, property.Name, (IComparable)attr.Minimum, (IComparable)attr.Maximum));

						//Compare Attribute
						foreach (var attr in property.GetAttributes<CompareAttribute>().Take(1))
							rules.Add(new CompareRule(type.Name, property.Name, attr.ComparisonPropertyName, attr.Operator));

						// ListLength Attribute
						foreach (var attr in property.GetAttributes<ListLengthAttribute>().Take(1))
							rules.Add(new ListLengthRule(type.Name, property.Name, attr.StaticLength, attr.LengthCompareProperty, attr.CompareOp));

						// Allowed Values Attribute
						GraphReferenceProperty reference = property as GraphReferenceProperty;
						if (reference != null)
						{
							foreach (var source in property.GetAttributes<AllowedValuesAttribute>()
								.Select(attr => attr.Source)
								.Union(reference.PropertyType.GetAttributes<AllowedValuesAttribute>()
								.Select(attr => attr.Source.Contains('.') ? attr.Source : reference.PropertyType.Name + '.' + attr.Source)
								.Take(1)))
								rules.Add(new AllowedValuesRule(type.Name, property.Name, source));
						}
					}
				}
			}
			return rules;
		}

		#endregion
	}
}
