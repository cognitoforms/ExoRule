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
			: this(rootType, property, CreateError(rootType, property, sets))
		{ }

		public RequiredRule(string rootType, string property, RuleInvocationType invocationTypes, params ConditionTypeSet[] sets)
			: this(rootType, property, invocationTypes, CreateError(rootType, property, sets))
		{ }

		public RequiredRule(string rootType, string property, Error error)
			: this(rootType, property, RuleInvocationType.PropertyChanged, error)
		{ }

		public RequiredRule(string rootType, string property, RuleInvocationType invocationTypes, Error error)
			: base(rootType, property, error, invocationTypes, property)
		{ }

		#endregion

		#region Methods

		static Error CreateError(string rootType, string property, params ConditionTypeSet[] sets)
		{
			return new Error(
				GetErrorCode(rootType, property, "Required"),
				"required",	typeof(RequiredRule), (s) => s.Replace("{property}", ModelContext.Current.GetModelType(rootType).Properties[property].Label), sets);
		}

		/// <summary>
		/// Determines whether the rule should attach its condition to the given <see cref="ModelInstance"/>.
		/// </summary>
		/// <param name="root">The model instance to evaluate the rule for.</param>
		/// <returns>A boolean value indicating whether the state of the given <see cref="ModelInstance"/> violates the rule.</returns>
		protected override bool ConditionApplies(ModelInstance root)
		{
			return 
				root[Property] == null || 
				(Property is ModelReferenceProperty && Property.IsList && root.GetList((ModelReferenceProperty)Property).Count == 0);
		}

		#endregion
	}
}
