using System;
using System.Diagnostics;
using System.Linq;
using System.Collections.Generic;
using System.Reflection;
using System.Text.RegularExpressions;
using System.Collections;
using ExoModel;
using System.Runtime.Serialization;
using System.Linq.Expressions;

namespace ExoRule
{
	#region Rule

	/// <summary>
	/// Abstract base class for all rule instances. Rules for concrete types should inherit from <see cref="Rule<T>"/>.
	/// </summary>
	public abstract class Rule : IRuleProvider
	{
		#region Fields

		string rootTypeName;
		ModelType rootType;

		#endregion

		#region Constructors

		/// <summary>
		/// Creates a new rule instance.
		/// </summary>
		/// <param name="rootType">The root <see cref="ModelType"/> the rule is for</param>
		/// <param name="name"></param>
		/// <param name="predicates"></param>
		public Rule(string rootType, string name, params string[] predicates)
			: this(rootType, name, predicates != null && predicates.Length > 0 ? RuleInvocationType.PropertyChanged : 0, null, predicates)
		{ }

		/// <summary>
		/// Creates a new rule instance.
		/// </summary>
		/// <param name="rootType">The root <see cref="ModelType"/> the rule is for</param>
		/// <param name="name"></param>
		/// <param name="invocationTypes"></param>
		/// <param name="predicates"></param>
		public Rule(string rootType, string name, RuleInvocationType invocationTypes, params string[] predicates)
			: this(rootType, name, invocationTypes, null, predicates)
		{ }

		/// <summary>
		/// Creates a new rule instance.
		/// </summary>
		/// <param name="rootType">The root <see cref="ModelType"/> the rule is for</param>
		/// <param name="name"></param>
		/// <param name="invocationTypes"></param>
		/// <param name="predicates"></param>
		public Rule(string rootType, string name, RuleInvocationType invocationTypes, ConditionType[] conditionTypes, params string[] predicates)
		{
			this.rootTypeName = rootType;
			this.Name = name;
			this.InvocationTypes = invocationTypes;
			this.ConditionTypes = conditionTypes ?? new ConditionType[0];

			// Default the execution location to server
			this.ExecutionLocation = RuleExecutionLocation.Server;

			// Split the predicates into property change paths and return values
			if (predicates != null && predicates.Length > 0)
				SetPredicates(predicates);
		}

		#endregion

		#region Properties

		/// <summary>
		/// Gets the name of the rule.
		/// </summary>
		public string Name { get; protected set; }

		/// <summary>
		/// Gets the root <see cref="ModelType"/> of the rule.
		/// </summary>
		public virtual ModelType RootType
		{
			get
			{
				return rootType ?? ModelContext.Current.GetModelType(rootTypeName);
			}
		}

		/// <summary>
		/// Gets or sets the <see cref="RuleInvocationType"/> governing when the rule will run.
		/// </summary>
		public RuleInvocationType InvocationTypes { get; protected set; }

		/// <summary>
		/// Gets or sets the execution location governing where the rule will run: server (default) and/or client.
		/// </summary>
		public RuleExecutionLocation ExecutionLocation { get; protected set; }

		/// <summary>
		/// Gets the set of predicate paths that trigger property change invocations.
		/// </summary>
		public IEnumerable<string> Predicates { get; internal set; }

		/// <summary>
		/// Gets the set of properties that trigger property get invocations.
		/// </summary>
		public IEnumerable<string> ReturnValues { get; internal set; }

		/// <summary>
		/// Gets the set of <see cref="ConditionType"/> instances the current rule is responsible
		/// for associating with instances in the model.
		/// </summary>
		public IEnumerable<ConditionType> ConditionTypes
		{
			get; protected set;
		}

		#endregion

		#region Events

		/// <summary>
		/// Initialization event raised once for each rule immediately before it is registered
		/// for the first time, allowing rules to delay one time setup logic.
		/// </summary>
		protected internal event EventHandler Initialize;
	
		public static event EventHandler BeforeInvoke;

		public static event EventHandler AfterInvoke;

		#endregion

		#region Methods

		protected void SetPredicates(string[] predicates)
		{
			this.Predicates = predicates
					.Where(predicate => !predicate.StartsWith("return "))
					.ToArray(); ;

			this.ReturnValues = predicates
					.Where((predicate) => predicate.StartsWith("return "))
					.Select((predicate) => predicate.Substring(7))
					.Concat(this.ReturnValues ?? new string[0])
					.ToArray();

			// Automatically mark rules with return values as property get rules
			if (this.ReturnValues.Any())
			{
				// Remove property change invocation
				this.InvocationTypes &= ~RuleInvocationType.PropertyChanged;

				// Add property get invocation
				this.InvocationTypes |= RuleInvocationType.PropertyGet;
			}
		}

		protected virtual string[] GetPredicates()
		{
			return new string[] { };
		}

		/// <summary>
		/// Gets all static rules defined on the specified types.
		/// </summary>
		/// <param name="types"></param>
		/// <returns></returns>
		public static IEnumerable<Rule> GetRules(IEnumerable<Type> types)
		{
			// Fetch the set of rules declared on the specified types
			List<Rule> rules = new List<Rule>();
			foreach (Type type in types
				.Where(type => type.IsClass)
				.SelectMany(type => type.BaseType.IsGenericType ? new Type[] { type, type.BaseType } : new Type[] { type }))
			{
				rules.AddRange(
					type.GetFields(BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic)
						.Where(field => typeof(IRuleProvider).IsAssignableFrom(field.FieldType))
						.SelectMany(field =>
						{
							IRuleProvider ruleProvider = (IRuleProvider)field.GetValue(null);

							if (ruleProvider != null)
								return ruleProvider.GetRules(type, field.Name);
							else
							{
								StackTrace stackTrace = new StackTrace();
								List<MethodBase> callStackMethods = stackTrace.GetFrames()
									.Select(f => f.GetMethod())
									.ToList();

								Type currentType = callStackMethods.First().DeclaringType;
								
								callStackMethods.Reverse();
								MethodBase ruleProviderCall = callStackMethods.FirstOrDefault(method => currentType != method.DeclaringType && typeof(IRuleProvider).IsAssignableFrom(method.DeclaringType));

								if (ruleProviderCall != null)
								{
									string errorMessage = string.Format(
										"'{0}'.'{1}' is null, declared as a '{2}', and '{3}'.'{4}' is creating/accessing rules. As such, it appears that the '{2}' is still initializing and rules will not register properly. Please see the call stack.",
										type.Name, field.Name, typeof(IRuleProvider).Name, ruleProviderCall.DeclaringType.Name, ruleProviderCall.Name
									);
									throw new ApplicationException(errorMessage);
								}
							}

							return new Rule[] { };
						})
						.Where(rule => rule != null)
				);

				// Ensure the error code has been set on all statically declared condition types
				foreach (ConditionType error in
					type.GetFields(BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic)
						.Where(field => typeof(ConditionType).IsAssignableFrom(field.FieldType))
						.Select(field =>
						{
							ConditionType error = (ConditionType)field.GetValue(null);
							if (error != null && error.Code == null)
								error.Code = field.DeclaringType.Name + "." + field.Name;
							return error;
						}))
				{ }
			}
			return rules;
		}

		/// <summary>
		/// Registers all static rules defined on the specified types.
		/// </summary>
		/// <param name="types"></param>
		/// <returns></returns>
		public static void RegisterRules(IEnumerable<Type> types)
		{
			// Register each of the specified rules
			foreach (Rule rule in GetRules(types))
				rule.Register();
		}

		/// <summary>
		/// Registers all static rules defined on the types in the specified assembly.
		/// </summary>
		/// <param name="types"></param>
		/// <returns></returns>
		public static void RegisterRules(params Assembly[] assemblies)
		{
			RegisterRules(assemblies.SelectMany(a => a.GetTypes()));
		}

		/// <summary>
		/// Gets the set of rules registered for the specified <see cref="ModelType"/>.
		/// </summary>
		/// <param name="type"></param>
		/// <returns></returns>
		public static IEnumerable<Rule> GetRegisteredRules(ModelType type)
		{
			return type.GetExtension<List<Rule>>();
		}

		/// <summary>
		/// Invokes the rule on the specified <see cref="ModelInstance"/> as a result
		/// of the specified triggering <see cref="ModelEvent"/>
		/// </summary>
		/// <param name="root"></param>
		/// <param name="modelEvent"></param>
		internal void Invoke(ModelInstance root, ModelEvent modelEvent)
		{
			try
			{
				if (BeforeInvoke != null)
					BeforeInvoke(this, EventArgs.Empty);

				ModelEventScope.Perform(() => OnInvoke(root, modelEvent));
			}
			finally
			{
				if (AfterInvoke != null)
					AfterInvoke(this, EventArgs.Empty);
			}
		}

		/// <summary>
		/// Invokes the rule on the specified <see cref="ModelInstance"/> as a result
		/// of the specified triggering <see cref="ModelEvent"/>
		/// </summary>
		/// <param name="root"></param>
		/// <param name="modelEvent"></param>
		internal protected abstract void OnInvoke(ModelInstance root, ModelEvent modelEvent);

		/// <summary>
		/// Registers the rule with the current <see cref="ModelContext"/>.
		/// </summary>
		public void Register(ModelType rootType = null)
		{
			// Determine if the rule is be registered for a specific root model type
			if (rootType != null)
			{
				if (this.rootType != null)
					throw new InvalidOperationException("Rules cannot be explicitly registered for more than one model type.");
				this.rootType = rootType;
			}

			// Raise the Initialization event the first time the rule is registered
			if (Initialize != null)
			{
				Initialize(this, EventArgs.Empty);
				Initialize = null;
			}

			// Default the invocation type to PropertyChanged if none were assigned
			if ((int)InvocationTypes == 1)
				InvocationTypes = RuleInvocationType.PropertyChanged;

			// Automatically detect predicates if none were specified
			if (Predicates == null && ((InvocationTypes & (RuleInvocationType.PropertyChanged | RuleInvocationType.PropertyGet)) > 0))
				SetPredicates(GetPredicates());

			// Track and validate uniqueness of condition types
			HashSet<ConditionType> conditionTypes = RootType.GetExtension<HashSet<ConditionType>>();
			foreach (var conditionType in ConditionTypes)
			{
				if (conditionTypes.Contains(conditionType))
					throw new InvalidOperationException("Registered condition types must be unique for each model type.");
				conditionTypes.Add(conditionType);
			}

			// Track the rule registration for the root model type
			List<Rule> rules = RootType.GetExtension<List<Rule>>();
			rules.Add(this);

			// Do not perform model type event registration if the rule is not supposed to execute on the server
			if ((ExecutionLocation & RuleExecutionLocation.Server) == 0)
				return;

			// Init Invocation
			if ((InvocationTypes & RuleInvocationType.InitExisting) == RuleInvocationType.InitExisting ||
				 (InvocationTypes & RuleInvocationType.InitNew) == RuleInvocationType.InitNew)
			{
				RootType.Init += (sender, e) =>
				{
					if (((InvocationTypes & RuleInvocationType.InitExisting) == RuleInvocationType.InitExisting && !e.Instance.IsNew) ||
						((InvocationTypes & RuleInvocationType.InitNew) == RuleInvocationType.InitNew && e.Instance.IsNew))
						Invoke(e.Instance, e);
				};
			}

			// Property Get Invocation
			if ((InvocationTypes & RuleInvocationType.PropertyGet) == RuleInvocationType.PropertyGet)
			{
				// Subscribe to property get notifications for all return values
				RootType.PropertyGet += (sender, e) =>
				{
					// Determine if the rule is responsible for calculating the property being accessed
					if (ReturnValues.Contains(e.Property.Name))
					{
						// Get the rule manager for the current instance
						var manager = e.Instance.GetExtension<RuleManager>();

						// Determine if the rule needs to be run
						if ((e.IsFirstAccess && (!e.Property.IsPersisted || e.Instance.IsNew || manager.IsPendingInvocation(this))) || manager.IsPendingInvocation(this))
						{
							// Invoke the rule
							Invoke(e.Instance, e);

							// Mark the rule state as no longer requiring invocation
							manager.SetPendingInvocation(this, false);
						}
					}
				};

				// Subscribe to property change notifications for all rule predicates
				foreach (string predicate in Predicates)
				{
					RootType.GetPath(predicate).Change += (sender, e) =>
					{
						// Only invoke the rule if the instance is of the same type as the rule root type
						if (RootType.IsInstanceOfType(e.Instance))
						{
							// Get the rule manager for the current instance
							var manager = e.Instance.GetExtension<RuleManager>();

							// Mark the rule state as requiring invocation
							manager.SetPendingInvocation(this, true);

							// Raise property change notifications
							foreach (var property in ReturnValues)
								e.Instance.Type.Properties[property].NotifyPathChange(e.Instance);
						}
					};
				}
			}

			// Property Change Invocation
			if ((InvocationTypes & RuleInvocationType.PropertyChanged) == RuleInvocationType.PropertyChanged)
			{
				// Subscribe to property change notifications for all rule predicates
				foreach (string predicate in Predicates)
				{
					RootType.GetPath(predicate).Change += (sender, e) =>
					{
						// Only invoke the rule if the instance is of the same type as the rule root type
						if (RootType.IsInstanceOfType(e.Instance))
						{
							// Get the rule manager for the current instance
							var manager = e.Instance.GetExtension<RuleManager>();

							// Register the rule to run if it is not already registered
							if (!manager.IsPendingInvocation(this))
							{
								// Mark the rule state as requiring invocation
								manager.SetPendingInvocation(this, true);

								// Invoke the rule when the last model event scope exits
								ModelEventScope.OnExit(() =>
								{
									// Mark the rule state as no longer requiring invocation
									manager.SetPendingInvocation(this, false);

									// Invoke the rule
									Invoke(e.Instance, e);
								});
							}
						}
					};
				}
			}

			// Allow subclasses to perform additional registration logic
			OnRegister();
		}

		/// <summary>
		/// Allows subclasses to perform additional registration logic.
		/// </summary>
		protected internal virtual void OnRegister()
		{ }

		#endregion

		#region IRuleProvider Members

		IEnumerable<Rule> IRuleProvider.GetRules(Type sourceType, string name)
		{
			// Initialize the name of the rule if it has not already been set
			if (this.Name == null)
				this.Name = name;

			// Return the current rule
			yield return this;
		}

		#endregion
	}

	#endregion

	#region Rule<TRoot>

	/// <summary>
	/// Concrete subclass of <see cref="Rule"/> that represents a rule for a specific root type.
	/// </summary>
	/// <typeparam name="TRoot"></typeparam>
	public class Rule<TRoot> : Rule
			where TRoot : class
	{
		#region Constructors

		static Rule()
		{
			PredicateFilter = (action, method) => method.DeclaringType.Assembly == action.GetParameters()[0].ParameterType.Assembly;
		}

		public Rule(Action<TRoot> action)
			: this(null, (RuleInvocationType)(1), null, null, action)
		{ }

		public Rule(RuleInvocationType invocationTypes, Action<TRoot> action)
			: this(null, invocationTypes, null, null, action)
		{ }

		public Rule(RuleInvocationType invocationTypes, string[] predicates, Action<TRoot> action)
			: this(null, invocationTypes, predicates, action)
		{ }

		public Rule(RuleInvocationType invocationTypes, string[] predicates, ConditionType[] conditionTypes, Action<TRoot> action)
			: this(null, invocationTypes, predicates, conditionTypes, action)
		{ }

		public Rule(string name, Action<TRoot> action)
			: this(name, (RuleInvocationType)(1), null, null, action)
		{ }

		public Rule(string name, RuleInvocationType invocationTypes, Action<TRoot> action)
			: this(name, invocationTypes, null, null, action)
		{ }

		public Rule(string name, RuleInvocationType invocationTypes, string[] predicates, Action<TRoot> action)
			: this(name, invocationTypes, predicates, null, action)
		{ }

		public Rule(string name, RuleInvocationType invocationTypes, string[] predicates, ConditionType[] conditionTypes, Action<TRoot> action)
			: base(null, name, invocationTypes, conditionTypes, predicates)
		{
			Action = action;
		}

		internal Rule(string name, RuleInvocationType invocationTypes, string rootType, string[] predicates)
			: base(rootType ?? ModelContext.Current.GetModelType<TRoot>().Name, name, invocationTypes, predicates)
		{ }

		#endregion

		#region Properties

		/// <summary>
		/// Default filter used to determine if methods should be introspected during predicate detection for a rule.
		/// </summary>
		public static Func<MethodBase, MethodBase, bool> PredicateFilter { get; set; }

		/// <summary>
		/// Gets the action that will be performed when the rule is invoked.
		/// </summary>
		public Action<TRoot> Action { get; internal set; }

		/// <summary>
		/// Gets the root <see cref="ModelType"/> of the rule.
		/// </summary>
		public override ModelType RootType
		{
			get
			{
				return ModelContext.Current.GetModelType<TRoot>();
			}
		}

		#endregion

		#region Methods

		static MethodInfo assign = typeof(Expression).GetMethod("Assign");

		/// <summary>
		/// Creates a rule that calculates the value of a single property given a simple expression tree.
		/// </summary>
		/// <typeparam name="TProperty"></typeparam>
		/// <param name="property"></param>
		/// <param name="calculation"></param>
		/// <returns></returns>
		public static Rule<TRoot> Calculate<TProperty>(Expression<Func<TRoot, TProperty>> property, Expression<Func<TRoot, TProperty>> calculation)
		{
			// Create and return the new calculation rule
			return new Calculation<TProperty>(property, calculation);
		}

		public Rule<TRoot> OnInit()
		{
			this.InvocationTypes |= RuleInvocationType.InitNew | RuleInvocationType.InitExisting;
			return this;
		}

		public Rule<TRoot> OnInitNew()
		{
			this.InvocationTypes |= RuleInvocationType.InitNew;
			return this;
		}

		public Rule<TRoot> OnInitExisting()
		{
			this.InvocationTypes |= RuleInvocationType.InitExisting;
			return this;
		}

		public Rule<TRoot> OnChangeOf(params string[] predicates)
		{
			if (Predicates != null)
				Predicates = Predicates.Concat(predicates).ToArray();
			else
				Predicates = predicates;
			if ((this.InvocationTypes & RuleInvocationType.PropertyGet) == 0)
				this.InvocationTypes |= RuleInvocationType.PropertyChanged;
			return this;
		}

		public Rule<TRoot> RunOnServer()
		{
			this.ExecutionLocation = RuleExecutionLocation.Server;
			return this;
		}

		public Rule<TRoot> RunOnClient()
		{
			this.ExecutionLocation = RuleExecutionLocation.Client;
			return this;
		}

		public Rule<TRoot> RunOnServerAndClient()
		{
			this.ExecutionLocation = RuleExecutionLocation.ServerAndClient;
			return this;
		}

		public Rule<TRoot> Returns(params Expression<Func<TRoot, object>>[] properties)
		{
			return Returns(properties.Select(p => p.Body is MemberExpression ? p.Body : ((UnaryExpression)p.Body).Operand).OfType<MemberExpression>().Select(m => m.Member.Name).ToArray());
		}

		public Rule<TRoot> Returns(params string[] properties)
		{
			if (properties == null || properties.Length == 0)
				throw new ArgumentException("Rule must specify at least 1 property for Returns");

			ReturnValues = properties;
			this.InvocationTypes |= RuleInvocationType.PropertyGet;
			this.InvocationTypes &= ~RuleInvocationType.PropertyChanged;
			//this.InvocationTypes |= RuleInvocationType.InitExisting;
			//this.InvocationTypes |= RuleInvocationType.PropertyChanged;
			return this;
		}

		public Rule<TRoot> Asserts(params ConditionType[] conditionTypes)
		{
			this.ConditionTypes = conditionTypes;
			return this;
		}

		//protected override string[] GetPredicates()
		//{
		//    ModelPath path;
		//    return PredicateBuilder
		//        .GetPredicates(Action.Method, method => Rule<TRoot>.PredicateFilter(Action.Method, method), (InvocationTypes | RuleInvocationType.PropertyGet) > 0)
		//        .Where(predicate => RootType.TryGetPath(predicate.StartsWith("return ") ? predicate.Substring(7) : predicate, out path))
		//        .ToArray();
		//}

		/// <summary>
		/// Converts <see cref="Action<TRoot>"/> into a corresponding <see cref="Rule<TRoot>"/> instance.
		/// </summary>
		public static implicit operator Rule<TRoot>(Action<TRoot> action)
		{
			return new Rule<TRoot>(action);
		}

		/// <summary>
		/// Invokes the action for the current rule on the specified <see cref="ModelInstance"/>.
		/// </summary>
		/// <param name="root"></param>
		/// <param name="modelEvent"></param>
		internal protected override void OnInvoke(ModelInstance root, ModelEvent modelEvent)
		{
			Action((TRoot)root.Instance);
		}

		#endregion

		#region Calculation<TProperty>

		/// <summary>
		/// Special strongly-typed rule class responsible for automatically calculating the value
		/// of a property.
		/// </summary>
		/// <typeparam name="TProperty"></typeparam>
		internal class Calculation<TProperty> : Rule<TRoot>, ICalculationRule
		{
			string path;
			LambdaExpression calculation;

			/// <summary>
			/// Creates a new calculation for the specified property, including an action to set the property
			/// and the expression tree responsible for calculating the expected value.
			/// </summary>
			/// <param name="property"></param>
			/// <param name="action"></param>
			/// <param name="calculation"></param>
			internal Calculation(Expression<Func<TRoot, TProperty>> property, Expression<Func<TRoot, TProperty>> calculation)
				: base(null)
			{
				// Ensure the property argument is a valid expression
				if (property == null || !(property.Body is MemberExpression))
					throw new ArgumentException("The property expression must be a simple expression that returns a property from the root type (root => root.Property)", "property");

				// Ensure the property member expression is a property, not a field
				var propInfo = (PropertyInfo)((MemberExpression)property.Body).Member as PropertyInfo;
				if (propInfo == null)
					throw new ArgumentException("Only properties can be calculated via rules, not fields.", "property");

				this.Property = propInfo.Name;
				this.calculation = calculation;

				// Perform delayed initialization to be able to reference the model type information
				Initialize += (s, e) => 
				{
					var rootType = RootType;

					if (rootType == null)
						throw new ApplicationException(string.Format("Type '{0}' is not a model type.", typeof(TRoot).FullName));

					// Get the model property
					var prop = rootType.Properties[propInfo.Name];
					if (prop == null)
						throw new ArgumentException("Only valid model properties can be calculated: " + propInfo.Name, "property");

					// List property
					if (prop is ModelReferenceProperty && prop.IsList)
					{
						// Compile the calculation outside the action lambda to cache via closure
						var getListItems = calculation.Compile();

						this.Action = root =>
						{
							// Get the source list
							var source = ModelInstance.GetModelInstance(root).GetList((ModelReferenceProperty)prop);

							// Get the set of items the list should contain
							var items = ((IEnumerable)getListItems(root)).Cast<object>().Select(instance => ModelInstance.GetModelInstance(instance));

							// Update the list
							source.Update(items);
						};
					}

					// Reference or value property
					else
					{
						// Ensure the property can be set
						var setMethod = propInfo.GetSetMethod(true);
						if (setMethod == null)
							throw new ArgumentException("Read-only properties cannot be calculated: " + propInfo.Name, "property");

						// Create the expression to set the property using the calculation expression
						var root = Expression.Parameter(typeof(TRoot), "root");
						var setter = Expression.Call(root, setMethod, Expression.Invoke(calculation, root));
						this.Action = Expression.Lambda<Action<TRoot>>(setter, root).Compile();
					}

					// Assert that this rule returns the value of the calculated property
					Returns(this.Property);

					// Register for change events as well
					if (this.path == null)
						this.path = ModelContext.Current.GetModelType<TRoot>().GetPath(this.calculation).Path;
					if (!String.IsNullOrEmpty(this.path))
						OnChangeOf(this.path);
				};

				// Mark the rule for both server and client execution by default
				RunOnServerAndClient();
			}

			protected internal override void OnRegister()
			{
				base.OnRegister();
			}

			/// <summary>
			/// Gets the expression tree responsible for calculating the value of the property.
			/// </summary>
			LambdaExpression ICalculationRule.Calculation { get { return calculation; } }

			/// <summary>
			/// Gets the name of the property being calculated.
			/// </summary>
			public string Property { get; private set; }

			/// <summary>
			/// Returns null to indicate that calculation rules do not assert a condition.
			/// </summary>
			ConditionType IPropertyRule.ConditionType
			{
				get
				{
					return null;
				}
			}
		}

		#endregion

		#region Condition

		/// <summary>
		/// Special strongly-typed rule class responsible for automatically asserting a condition of the model.
		/// </summary>
		/// <typeparam name="TProperty"></typeparam>
		internal class Condition : Rule<TRoot>, IConditionRule
		{
			Expression<Func<TRoot, bool>> condition;
			Func<TRoot, bool> compiledCondition;
			string path;

			/// <summary>
			/// Creates a new calculation for the specified property, including an action to set the property
			/// and the expression tree responsible for calculating the expected value
			/// </summary>
			/// <param name="conditionType"></param>
			/// <param name="condition"></param>
			/// <param name="properties"></param>
			internal Condition(ConditionType conditionType, Expression<Func<TRoot, bool>> condition, params string[] properties)
				: base(0, null, new ConditionType[] { conditionType }, null)
			{
				this.condition = condition;
				this.compiledCondition = condition.Compile();
				this.Properties = properties;

				// Perform additional initialization during the initialize event to avoid accessing the model context too early
				Initialize += (s, e) =>
				{
					if (this.Action == null)
					{
						this.path = ModelContext.Current.GetModelType<TRoot>().GetPath(this.condition).Path;
						OnChangeOf(this.path);
						if (this.Properties == null || !this.Properties.Any())
							this.Properties = new string[] { path };
						this.Action = root => this.ConditionType.When(root, () => compiledCondition(root), this.Properties.ToArray());
					}
				};

				// Mark the rule for both server and client execution by default
				RunOnServerAndClient();
			}

			/// <summary>
			/// Gets the expression tree responsible for asserting the condition of the model.
			/// </summary>
			LambdaExpression IConditionRule.Condition { get { return condition; } }

			/// <summary>
			/// Gets the set of properties the condition should be associated with.
			/// </summary>
			public IEnumerable<string> Properties { get; private set; }

			/// <summary>
			/// Gets the type of condition being asserted.
			/// </summary>
			public ConditionType ConditionType
			{
				get
				{
					return ConditionTypes.First();
				}
			}
		}

		#endregion
	}

	#endregion

	#region Rule<TRule, TRoot>

	/// <summary>
	/// Concrete subclass of <see cref="Rule"/> that represents a rule for a specific root type.
	/// </summary>
	/// <typeparam name="TRule"></typeparam>
	/// <typeparam name="TRoot"></typeparam>
	public abstract class Rule<TRule, TRoot> : Rule<TRoot>
		where TRule : Rule<TRule, TRoot>
		where TRoot : class
	{
		protected Rule(RuleInvocationType invocationTypes, string rootType, string[] predicates)
			: base(typeof(TRule).Name, invocationTypes, rootType, predicates)
		{
			Action = OnInvoke;
		}

		protected abstract void OnInvoke(TRoot root);
	}

	#endregion

	#region Rule<TRule, TRoot, TEvent>

	/// <summary>
	/// Concrete subclass of <see cref="Rule"/> that represents a rule for a specific root type.
	/// </summary>
	/// <typeparam name="TRule"></typeparam>
	/// <typeparam name="TRoot"></typeparam>
	public abstract class Rule<TRule, TRoot, TEvent> : Rule<TRoot>
		where TRule : Rule<TRule, TRoot, TEvent>, new()
		where TRoot : class
		where TEvent : class
	{
		static Rule EventRule = new TRule();

		internal Rule(string name)
			: base(name, 0, null, new string[] { })
		{ }

		protected Rule()
			: base(typeof(TRule).Name, 0, null, new string[] { })
		{ }

		void OnInvoke(ModelInstance instance, TEvent e)
		{
			OnInvoke((TRoot)instance.Instance, e);
		}

		protected abstract void OnInvoke(TRoot root, TEvent e);

		/// <summary>
		/// Registers a named event on the root type for the current event rule.
		/// </summary>
		protected internal override void OnRegister()
		{
			base.OnRegister();

			RootType.Subscribe<TEvent>(Name, OnInvoke);
		}
	}

	#endregion
}
