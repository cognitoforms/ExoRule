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
	/// Abstract base for simple validation rules for properties in a model.
	/// </summary>
	public abstract class PropertyRule : Rule
	{
		#region Fields

		string property;

		#endregion

		#region Constructors

		/// <summary>
		/// Register resources for validation rule subclasses.
		/// </summary>
		static PropertyRule()
		{
			var propertyRuleType = typeof(PropertyRule);
			ConditionType.MapResources(propertyRuleType.Assembly.GetTypes().Where(t => t.IsSubclassOf(propertyRuleType)), propertyRuleType);
		}

		/// <summary>
		/// Creates a new <see cref="PropertyRule"/> for the specified property and condition type.
		/// </summary>
		/// <param name="rootType"></param>
		/// <param name="property"></param>
		/// <param name="conditionType"></param>
		/// <param name="invocationTypes"></param>
		/// <param name="predicates"></param>
		public PropertyRule(string rootType, string property, ConditionType conditionType, RuleInvocationType invocationTypes, params string[] predicates)
			: base(rootType, conditionType.Code, invocationTypes, new ConditionType[] { conditionType }, predicates)
		{
			this.property = property;
			if (conditionType != null)
				conditionType.ConditionRule = this;
			this.ExecutionLocation = RuleExecutionLocation.ServerAndClient;
		}

		#endregion

		#region Properties

		/// <summary>
		/// The <see cref="Property"/> representing the property that this rule
		/// will depend on and that any <see cref="ConditionTarget"/>s will target.
		/// </summary>
		public ModelProperty Property
		{
			get
			{
				return RootType.Properties[property];
			}
		}

		#endregion

		#region Methods

		/// <summary>
		/// Generates a unique error code for the current rule;
		/// </summary>
		/// <param name="rootType"></param>
		/// <param name="property"></param>
		/// <returns></returns>
		protected static string GetErrorCode(string rootType, string property, string rule)
		{
			string code = String.Format("{0}.{1}.{2}", rootType, property, rule);
			int count = 1;
			ConditionType ct = code;
			while (ct != null)
				ct = code = String.Format("{0}.{1}.{2}", rootType, property, rule) + count++;
			return code;
		}

		/// <summary>
		/// Gets the label for the specified property.
		/// </summary>
		/// <param name="rootType"></param>
		/// <param name="property"></param>
		protected static string GetLabel(string rootType, string property)
		{
			return ModelContext.Current.GetModelType(rootType).Properties[property].Label;
		}

		/// <summary>
		/// Formats the specified value based on the formatting associated with the specified property.
		/// </summary>
		/// <param name="rootType"></param>
		/// <param name="property"></param>
		/// <param name="value"></param>
		/// <returns></returns>
		protected static string Format(string rootType, string property, object value)
		{
			return ((ModelValueProperty)ModelContext.Current.GetModelType(rootType).Properties[property]).FormatValue(value);
		}

		protected internal override void OnInvoke(ModelInstance root, ModelEvent modelEvent)
		{
			ConditionTypes.First().When(root.Instance, () => ConditionApplies(root), new string[] { Property.Name });
		}

		/// <summary>
		/// Overridden in subclasses to determine if the <see cref="ConditionType"/> applies.
		/// </summary>
		/// <returns>true if <paramref name="root"/> should be associated with the <see cref="ConditionType"/></returns>
		protected abstract bool ConditionApplies(ModelInstance root);

		#endregion
	}
}
