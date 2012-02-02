using System.Linq;
using System.Runtime.Serialization;
using ExoModel;
using System;
using System.Collections.Generic;

namespace ExoRule.Validation
{
	/// <summary>
	/// Applies conditions when the value of a <see cref="ModelProperty"/> is
	/// not an allowed value.
	/// </summary>
	public class AllowedValuesRule : PropertyRule
	{
		#region Fields

		ModelSource source;

		#endregion

		#region Constructors

		public AllowedValuesRule(string rootType, string property, string source)
			: this(rootType, property, source, RuleInvocationType.PropertyChanged)
		{
			this.Source = source;
		}

		public AllowedValuesRule(string rootType, string property, string source, RuleInvocationType invocationTypes)
			: base(rootType, property, CreateError(rootType, property), invocationTypes, CompareRule.GetPredicates(rootType, property, source))
		{
			this.Source = source;
		}

		#endregion

		#region Properties

		public string Source
		{
			get
			{
				return source.Path;
			}
			private set
			{
				source = new ModelSource(RootType, value);
			}
		}

		public bool IsStaticSource
		{
			get
			{
				return source.IsStatic;
			}
		}

		#endregion

		#region Methods

		static Error CreateError(string rootType, string property)
		{
			return new Error(
				GetErrorCode(rootType, property, "AllowedValues"),
				"allowed-values", typeof(AllowedValuesRule), 
				(s) => s.Replace("{property}", GetLabel(rootType, property)), null);
		}

		protected override bool ConditionApplies(ModelInstance root)
		{
			// Get the list of allowed values
			ModelInstanceList allowedValues = source.GetList(root);
			if (allowedValues == null)
				return false;

			// List Property
			if (Property.IsList)
			{
				// Get the current property value
				ModelInstanceList values = root.GetList((ModelReferenceProperty)Property);

				// Determine whether the property value is in the list of allowed values
				return !(values == null || values.All(value => allowedValues.Contains(value)));
			}

			// Reference Property
			else
			{
				// Get the current property value
				ModelInstance value = root.GetReference((ModelReferenceProperty)Property);

				// Determine whether the property value is in the list of allowed values
				return !(value == null || allowedValues.Contains(value));
			}
		}

		/// <summary>
		/// Gets the set of allowed values for the specified instance and property.
		/// </summary>
		/// <param name="instance"></param>
		/// <param name="property"></param>
		/// <returns></returns>
		public static IEnumerable<ModelInstance> GetAllowedValues(ModelInstance instance, ModelProperty property)
		{
			var allowedValuesRule = Rule.GetRegisteredRules(property.DeclaringType).OfType<AllowedValuesRule>().Where(r => r.Property == property).FirstOrDefault();
			if (allowedValuesRule == null)
				return null;

			return allowedValuesRule.source.GetList(instance);
		}

		#endregion
	}
}
