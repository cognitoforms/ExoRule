using System.Linq;
using System.Runtime.Serialization;
using ExoGraph;
using System;

namespace ExoRule.Validation
{
	/// <summary>
	/// Applies conditions when the value of a <see cref="GraphProperty"/> is
	/// not an allowed value.
	/// </summary>
	[DataContract(Name = "allowedValues")]
	public class AllowedValuesRule : PropertyRule
	{
		#region Fields

		PathSource source;

		#endregion

		#region Constructors

		public AllowedValuesRule(string rootType, string property, string source, Func<string> label)
			: this(rootType, property, source, label, RuleInvocationType.PropertyChanged)
		{
			this.Source = source;
		}

		public AllowedValuesRule(string rootType, string property, string source, Func<string> label, RuleInvocationType invocationTypes)
			: base(rootType, property, CreateError(rootType, property, label), invocationTypes, CompareRule.GetPredicates(rootType, property, source))
		{
			this.Source = source;
		}

		#endregion

		#region Properties

		[DataMember(Name = "source")]
		public string Source
		{
			get
			{
				return source.Path;
			}
			private set
			{
				source = new PathSource(Property.DeclaringType, value);
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

		static Error CreateError(string rootType, string property, Func<string> label)
		{
			return new Error(
				GetErrorCode(rootType, property, "AllowedValues"),
				"allowed-values", typeof(AllowedValuesRule), (s) => s.Replace("{property}", label()), null);
		}

		protected override bool ConditionApplies(GraphInstance root)
		{
			// Get the list of allowed values
			GraphInstanceList allowedValues = source.GetList(root);
			if (allowedValues == null)
				return false;

			// List Property
			if (Property.IsList)
			{
				// Get the current property value
				GraphInstanceList values = root.GetList((GraphReferenceProperty)Property);

				// Determine whether the property value is in the list of allowed values
				return !(values == null || values.All(value => allowedValues.Contains(value)));
			}

			// Reference Property
			else
			{
				// Get the current property value
				GraphInstance value = root.GetReference((GraphReferenceProperty)Property);

				// Determine whether the property value is in the list of allowed values
				return !(value == null || allowedValues.Contains(value));
			}
		}

		protected override string TypeName
		{
			get
			{
				return "allowedValues";
			}
		}

		#endregion
	}
}
