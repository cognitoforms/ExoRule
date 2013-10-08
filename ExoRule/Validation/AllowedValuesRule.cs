using System.Linq;
using System.Runtime.Serialization;
using ExoModel;
using System;
using System.Collections.Generic;
using System.Collections;
using System.Linq.Expressions;

namespace ExoRule.Validation
{
	/// <summary>
	/// Applies conditions when the value of a <see cref="ModelProperty"/> is
	/// not an allowed value.
	/// </summary>
	public class AllowedValuesRule : PropertyRule
	{
		#region Fields

		LambdaExpression expression;
		ModelSource modelSource;
		Type sourceType;

		#endregion

		#region Constructors

		public AllowedValuesRule(string rootType, string property, string source, bool ignoreValidation = false)
			: this(rootType, property, source, RuleInvocationType.PropertyChanged, ignoreValidation)
		{ }

		public AllowedValuesRule(string rootType, string property, string source, RuleInvocationType invocationTypes, bool ignoreValidation = false)
			: base(rootType, property, CreateError(property), invocationTypes)
		{
			this.IgnoreValidation = ignoreValidation;
			InitializeSource(source, null);
		}

		public AllowedValuesRule(string rootType, string property, string source, string errorMessage, bool ignoreValidation = false)
			: this(rootType, property, source, new Error(GetErrorCode(rootType, property, "AllowedValues"), errorMessage, null), RuleInvocationType.PropertyChanged, ignoreValidation)
		{ }

		public AllowedValuesRule(string rootType, string property, string source, Error error, RuleInvocationType invocationTypes, bool ignoreValidation = false)
			: base(rootType, property, error, invocationTypes)
		{
			this.IgnoreValidation = ignoreValidation;
			InitializeSource(source, null);
		}

		public AllowedValuesRule(string rootType, string property, LambdaExpression source, bool ignoreValidation = false)
			: this(rootType, property, source, RuleInvocationType.PropertyChanged, ignoreValidation)
		{ }

		public AllowedValuesRule(string rootType, string property, LambdaExpression source, RuleInvocationType invocationTypes, bool ignoreValidation = false)
			: base(rootType, property, CreateError(property), invocationTypes)
		{
			this.IgnoreValidation = ignoreValidation;
			InitializeSource(null, source);
		}

		public AllowedValuesRule(string rootType, string property, LambdaExpression source, string errorMessage, bool ignoreValidation = false)
			: this(rootType, property, source, new Error(GetErrorCode(rootType, property, "AllowedValues"), errorMessage, null), RuleInvocationType.PropertyChanged, ignoreValidation)
		{ }

		public AllowedValuesRule(string rootType, string property, LambdaExpression source, Error error, RuleInvocationType invocationTypes, bool ignoreValidation = false)
			: base(rootType, property, error, invocationTypes)
		{
			this.IgnoreValidation = ignoreValidation;
			InitializeSource(null, source);
		}

		#endregion

		#region Properties

		public string Source { get; private set; }

		public string Path { get; private set; }

		public bool IsStaticSource
		{
			get
			{
				return (modelSource != null && modelSource.IsStatic) || (sourceType != null && String.IsNullOrEmpty(Path));
			}
		}

		public ModelExpression SourceExpression
		{
			get
			{
				return expression != null ? new ModelExpression(RootType, expression) : 
					sourceType != null ? RootType.GetExpression(sourceType, Source) : null;
			}
		}

		public bool IgnoreValidation { get; private set; }

		#endregion

		#region Methods

		void InitializeSource(string source, LambdaExpression expression)
		{
			// Store the source
			this.Source = source;

			// Initialize the source during the rule initialization phase
			Initialize += (s, e) =>
			{	
				var rootType = RootType;
				var property = Property;

				// First, see if an explicit expression was specified for the rule source
				if (expression != null)
				{
					this.expression = expression;
					var sourceExpression = new ModelExpression(rootType, expression);
					Path = sourceExpression.Path.Path;
				}

				// Then see if the source represents a simple model source path
				else if (ModelSource.TryGetSource(RootType, source, out modelSource))
					Path = modelSource.IsStatic ? "" : modelSource.Path;

				// Then see if the source is a valid model expression
				else
				{
					sourceType = property is ModelValueProperty ? 
						typeof(IEnumerable<>).MakeGenericType(((ModelValueProperty)property).PropertyType) :
						((ModelReferenceProperty)property).PropertyType is IReflectionModelType ? 
						typeof(IEnumerable<>).MakeGenericType(((IReflectionModelType)((ModelReferenceProperty)property).PropertyType).UnderlyingType) 
						: typeof(IEnumerable);
					var sourceExpression = rootType.GetExpression(sourceType, source);
					Path = sourceExpression.Path.Path;
				}

				// Set the predicates based on the property and model path
				Predicates = String.IsNullOrEmpty(Path) ? new string[] { property.Name } : new string[] { property.Name, Path };
			};
				

		}

		static Func<ModelType, ConditionType> CreateError(string property)
		{
			return (ModelType rootType) => new Error(
				GetErrorCode(rootType.Name, property, "AllowedValues"),
				"allowed-values", typeof(AllowedValuesRule), 
				(s) => s.Replace("{property}", GetLabel(rootType, property)), null);
		}

		protected override bool ConditionApplies(ModelInstance root)
		{
			// Get the allowed values
			var allowedValues = GetAllowedValues(root);

			// Always consider as valid when there are no allowed values
			if (allowedValues == null)
				return false;

			// Value properties
			if (Property is ModelValueProperty)
			{
				// List
				if (Property.IsList)
				{
					// Get the current property value
					var values = root.GetValue((ModelValueProperty)Property) as IEnumerable;

					// Determine whether the property value is in the list of allowed values
					return !(values == null || values.Cast<object>().All(value => allowedValues.Contains(value)));
				}

				// Value
				else
				{
					// Get the current property value
					var value = root.GetValue((ModelValueProperty)Property);

					// Determine whether the property value is in the list of allowed values
					return !(value == null || allowedValues.Contains(value));
				}
			}

			// Reference properties
			else
			{
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
		}

		/// <summary>
		/// Gets the set of allowed values for the specified instance and property.
		/// </summary>
		/// <param name="instance"></param>
		/// <param name="property"></param>
		/// <returns></returns>
		public static IEnumerable<object> GetAllowedValues(ModelInstance root, ModelProperty property)
		{
			var rule = Rule.GetRegisteredRules(property.DeclaringType).OfType<AllowedValuesRule>().Where(r => r.Property == property).FirstOrDefault();
			if (rule == null)
				return null;

			return rule.GetAllowedValues(root);
		}

		IEnumerable<object> GetAllowedValues(ModelInstance root)
		{
			// Value properties
			if (Property is ModelValueProperty)
			{
				// Get the list of allowed values
				IEnumerable instances;
				if (modelSource != null)
					instances = modelSource.GetValue(root) as IEnumerable;
				else
					instances = SourceExpression.Invoke(root) as IEnumerable;
				return instances != null ? instances.Cast<object>().ToArray() : null;
			}

			// Reference properties
			else
			{
				// Get the model type of the property the rule applies to
				ModelType propertyType = ((ModelReferenceProperty)Property).PropertyType;

				// Get the list of allowed values
				if (modelSource != null)
				{
					// Model Source
					var instances = modelSource.GetList(root);
					if (instances != null)
						return instances.ToArray();
				}
				else
				{
					// Model Expression
					var instances = SourceExpression.Invoke(root) as IEnumerable;
					if (instances != null)
						return instances.Cast<object>().Select(i => propertyType.GetModelInstance(i)).ToArray();
				}
			}

			return null;
		}

		#endregion
	}
}
