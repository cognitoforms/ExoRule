using System;
using System.Linq;
using System.Text;
using System.Collections;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.Runtime.Serialization;
using ExoGraph;
using ExoRule;

namespace ExoRule.Validation
{
	/// <summary>
	/// Abstract base for simple validation rules for properties in a model.
	/// </summary>
	[DataContract]
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
			ConditionType.MapResources(new Type[] { typeof(RequiredRule), typeof(RequiredIfRule), typeof(StringLengthRule), typeof(RangeRule), typeof(CompareRule), typeof(AllowedValuesRule) }, typeof(PropertyRule));
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
			conditionType.ConditionRule = this;
			this.ExecutionLocation = RuleExecutionLocation.ServerAndClient;
		}

		#endregion

		#region Properties

		/// <summary>
		/// The <see cref="Property"/> representing the property that this rule
		/// will depend on and that any <see cref="ConditionTarget"/>s will target.
		/// </summary>
		public GraphProperty Property
		{
			get
			{
				return RootType.Properties[property];
			}
		}

		/// <summary>
		/// Text version of <see cref="Property"/> to support WCF serialization.
		/// </summary>
		[DataMember(Name = "property")]
		string PropertyName
		{
			get
			{
				return property;
			}
			set
			{
				property = value;
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

		protected internal override void OnInvoke(GraphInstance root, GraphEvent graphEvent)
		{
			ConditionTypes.First().When(root.Instance, () => ConditionApplies(root), new string[] { Property.Name });
		}

		/// <summary>
		/// Overridden in subclasses to determine if the <see cref="ConditionType"/> applies.
		/// </summary>
		/// <returns>true if <paramref name="root"/> should be associated with the <see cref="ConditionType"/></returns>
		protected abstract bool ConditionApplies(GraphInstance root);

		public static IEnumerable<ConditionType> InferConditionTypes(GraphType type, params Func<Attribute, ConditionType>[] converters)
		{
			List<ConditionType> conditionTypes = new List<ConditionType>();
			foreach (var property in type.Properties)
			{
				foreach (var converter in converters)
				{
					Attribute source = property.GetAttributes<Attribute>().Where(attribute => attribute.GetType() == converter.Method.GetParameters()[0].ParameterType).FirstOrDefault();
					if (source != null)
						conditionTypes.Add(converter(source));
				}
			}
			return conditionTypes;
		}

		#endregion
	}
}
