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
	/// <summary>
	/// Applies conditions when the value of a <see cref="ModelProperty"/> is
	/// null or an empty list.
	/// </summary>
	public class RequiredRule : PropertyRule
	{
		#region Constructors

		public RequiredRule(string rootType, string property, params ConditionTypeSet[] sets)
			: base(rootType, property, CreateError(property, sets), RuleInvocationType.InitNew | RuleInvocationType.PropertyChanged, property)
		{ }

		public RequiredRule(string rootType, string property, object requiredValue, params ConditionTypeSet[] sets)
			: base(rootType, property, CreateError(property, sets), RuleInvocationType.InitNew | RuleInvocationType.PropertyChanged, property)
		{
			RequiredValue = requiredValue;
		}

		public RequiredRule(string rootType, string property, string errorMessage, params ConditionTypeSet[] sets)
			: this(rootType, property, RuleInvocationType.InitNew | RuleInvocationType.PropertyChanged, new Error(GetErrorCode(rootType, property, "Required"), errorMessage, sets))
		{ }

		public RequiredRule(string rootType, string property, string errorMessage, object requiredValue, params ConditionTypeSet[] sets)
			: this(rootType, property, RuleInvocationType.InitNew | RuleInvocationType.PropertyChanged, new Error(GetErrorCode(rootType, property, "Required"), errorMessage, sets))
		{
			RequiredValue = requiredValue;
		}
		
		public RequiredRule(string rootType, string property, RuleInvocationType invocationTypes, params ConditionTypeSet[] sets)
			: base(rootType, property, CreateError(property, sets), invocationTypes, property)
		{ }

		public RequiredRule(string rootType, string property, RuleInvocationType invocationTypes, object requiredValue, params ConditionTypeSet[] sets)
			: base(rootType, property, CreateError(property, sets), invocationTypes, property)
		{
			RequiredValue = requiredValue;
		}

		public RequiredRule(string rootType, string property, RuleInvocationType invocationTypes, string errorMessage, params ConditionTypeSet[] sets)
			: this(rootType, property, invocationTypes, new Error(GetErrorCode(rootType, property, "Required"), errorMessage, sets))
		{ }

		public RequiredRule(string rootType, string property, RuleInvocationType invocationTypes, string errorMessage, object requiredValue, params ConditionTypeSet[] sets)
			: this(rootType, property, invocationTypes, new Error(GetErrorCode(rootType, property, "Required"), errorMessage, sets))
		{
			RequiredValue = requiredValue;
		}

		public RequiredRule(string rootType, string property, Error error)
			: this(rootType, property, RuleInvocationType.InitNew | RuleInvocationType.PropertyChanged, error)
		{ }

		public RequiredRule(string rootType, string property, Error error, object requiredValue)
			: this(rootType, property, RuleInvocationType.InitNew | RuleInvocationType.PropertyChanged, error)
		{
			RequiredValue = requiredValue;
		}

		public RequiredRule(string rootType, string property, RuleInvocationType invocationTypes, Error error)
			: base(rootType, property, error, invocationTypes, property)
		{ }

		#endregion

		#region Properties

		public object RequiredValue { get; private set; }

		#endregion

		#region Methods

		static Func<ModelType, ConditionType> CreateError(string property, params ConditionTypeSet[] sets)
		{
			return (ModelType rootType) =>
			{
				var label = rootType.Properties[property].Label;
				return new Error(GetErrorCode(rootType.Name, property, "Required"),	"required", typeof(RequiredRule), 
					(s) => s.Replace("{property}", label), sets);
			};
		}

		/// <summary>
		/// Determines whether the rule should attach its condition to the given <see cref="ModelInstance"/>.
		/// </summary>
		/// <param name="root">The model instance to evaluate the rule for.</param>
		/// <returns>A boolean value indicating whether the state of the given <see cref="ModelInstance"/> violates the rule.</returns>
		protected override bool ConditionApplies(ModelInstance root)
		{
			if (RequiredValue != null)
				return root[Property] == null || !root[Property].Equals(RequiredValue);
			else
				return 
					root[Property] == null || 
					(Property is ModelReferenceProperty && Property.IsList && root.GetList((ModelReferenceProperty)Property).Count == 0);
		}

		#endregion
	}
}
