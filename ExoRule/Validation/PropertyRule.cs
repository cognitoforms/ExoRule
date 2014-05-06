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
	public abstract class PropertyRule : Rule, IPropertyRule
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

		protected PropertyRule(string rootType, string property, RuleInvocationType invocationTypes, params string[] predicates)
			: base(rootType, property, predicates)
		{
			this.property = property;
			this.ExecutionLocation = RuleExecutionLocation.ServerAndClient;
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
			this.ExecutionLocation = RuleExecutionLocation.ServerAndClient;
		}

		/// <summary>
		/// Creates a new <see cref="PropertyRule"/> for the specified property and condition type.
		/// </summary>
		/// <param name="rootType"></param>
		/// <param name="property"></param>
		/// <param name="conditionType"></param>
		/// <param name="invocationTypes"></param>
		/// <param name="predicates"></param>
		public PropertyRule(string rootType, string property, Func<ModelType, ConditionType> conditionType, RuleInvocationType invocationTypes, params string[] predicates)
			: base(rootType, rootType + "." + property, invocationTypes, predicates)
		{
			this.property = property;
			this.ExecutionLocation = RuleExecutionLocation.ServerAndClient;

			if (conditionType != null)
			Initialize += (s, e) => 
			{
				// Make sure the condition type and rule have a unique name
				var type = RootType;
				var error = conditionType(type);
				var originalCode = error.Code;
				var uniqueCode = originalCode;
				int count = 1;
				while (ConditionType.GetConditionTypes(type).Any(ct => ct.Code == uniqueCode))
					uniqueCode = originalCode + count++;
				error.Code = uniqueCode;
				Name = uniqueCode;

				// Assign the condition type to the rule
				ConditionTypes = new ConditionType[] { error };
			};
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

		public ConditionType ConditionType
		{
			get
			{
				return ConditionTypes.First();
			}
		}

		#endregion

		#region Methods

		/// <summary>
		/// Initializes predicates for the specified property rule which leverages a comparison property.
		/// </summary>
		/// <param name="rule"></param>
		/// <param name="compareSource"></param>
		protected void InitializePredicates(params string[] paths)
		{
			if (paths == null || paths.Length == 0)
				Predicates = new string[] { this.property };
			else
				Initialize += (s, e) =>
				{	
					var rootType = RootType;
					Predicates = paths
						.Select(path => new ModelSource(rootType, path))
						.Where(source => !source.IsStatic)
						.Select(source => source.Path)
						.Union(new string[] { this.property })
						.ToArray();
				};
		}

		/// <summary>
		/// Generates a unique error code for a property-specific rule.
		/// </summary>
		/// <param name="rootType"></param>
		/// <param name="property"></param>
		/// <returns></returns>
		protected static string GetErrorCode(string rootType, string property, string rule)
		{
			return String.Format("{0}.{1}.{2}", rootType, property, rule);
		}

		/// <summary>
		/// Gets the label for the specified property.
		/// </summary>
		/// <param name="rootType"></param>
		/// <param name="property"></param>
		protected static string GetLabel(ModelType rootType, string property)
		{
			return rootType.Properties[property].Label;
		}

		/// <summary>
		/// Gets the label for the specified source property path.
		/// </summary>
		/// <param name="rootType"></param>
		/// <param name="source"></param>
		/// <returns></returns>
		protected static string GetSourceLabel(ModelType rootType, string source)
		{
			ModelSource s;
			ModelProperty sourceProperty;
			ModelSource.TryGetSource(rootType, source, out s, out sourceProperty);
			return sourceProperty.Label;
		}

		/// <summary>
		/// Formats the specified value based on the formatting associated with the specified property.
		/// </summary>
		/// <param name="rootType"></param>
		/// <param name="property"></param>
		/// <param name="value"></param>
		/// <returns></returns>
		protected static string Format(ModelType rootType, string property, object value)
		{
			return ((ModelValueProperty)rootType.Properties[property]).FormatValue(value);
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

		#region IPropertyRule

		/// <summary>
		/// Gets the name of the property being calculated.
		/// </summary>
		string IPropertyRule.Property
		{
			get { return property; }
		}

		/// <summary>
		/// Gets the unique name of the rule within the scope of the property to which it is assigned.
		/// </summary>
		string IPropertyRule.Name
		{
			get
			{
				// Get the type name
				string name = GetType().Name;
				
				// Strip Rule off the end
				if (name.EndsWith("Rule"))
					name = name.Substring(0, name.Length - 4);

				return name;

			}
		}

		#endregion
	}
}
