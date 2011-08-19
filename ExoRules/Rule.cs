using System;
using System.Linq;
using System.Collections.Generic;
using System.Reflection;
using System.Text.RegularExpressions;
using System.Collections;
using ExoGraph;
using System.Runtime.Serialization;
using System.Linq.Expressions;

namespace ExoRule
{
	#region Rule

	/// <summary>
	/// Abstract base class for all rule instances. Rules for concrete types should inherit from <see cref="Rule<T>"/>.
	/// </summary>
	[DataContract]
	public abstract class Rule : IRuleProvider
	{
		#region Fields

		internal ConditionType[] conditionTypes;
		string rootType;

		#endregion

		#region Constructors

		/// <summary>
		/// Creates a new rule instance.
		/// </summary>
		/// <param name="rootType">The root <see cref="GraphType"/> the rule is for</param>
		/// <param name="name"></param>
		/// <param name="predicates"></param>
		public Rule(string rootType, string name, params string[] predicates)
			: this(rootType, name, predicates != null && predicates.Length > 0 ? RuleInvocationType.PropertyChanged : 0, null, predicates)
		{ }

		/// <summary>
		/// Creates a new rule instance.
		/// </summary>
		/// <param name="rootType">The root <see cref="GraphType"/> the rule is for</param>
		/// <param name="name"></param>
		/// <param name="invocationTypes"></param>
		/// <param name="predicates"></param>
		public Rule(string rootType, string name, RuleInvocationType invocationTypes, params string[] predicates)
			: this(rootType, name, invocationTypes, null, predicates)
		{ }

		/// <summary>
		/// Creates a new rule instance.
		/// </summary>
		/// <param name="rootType">The root <see cref="GraphType"/> the rule is for</param>
		/// <param name="name"></param>
		/// <param name="invocationTypes"></param>
		/// <param name="predicates"></param>
		public Rule(string rootType, string name, RuleInvocationType invocationTypes, ConditionType[] conditionTypes, params string[] predicates)
		{
			this.rootType = rootType;
			this.Name = name;
			this.InvocationTypes = invocationTypes;
			this.conditionTypes = conditionTypes ?? new ConditionType[0];

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
		public string Name { get; private set; }

		/// <summary>
		/// Gets the root <see cref="GraphType"/> of the rule.
		/// </summary>
		public virtual GraphType RootType
		{
			get
			{
				return GraphContext.Current.GetGraphType(rootType);
			}
		}

		/// <summary>
		/// Gets the type name for serialization purposes.
		/// </summary>
		[DataMember(Name = "type")]
		protected virtual string TypeName
		{
			get
			{
				return Name;
			}
		}

		/// <summary>
		/// Gets or sets the <see cref="RuleInvocationType"/> governing when the rule will run.
		/// </summary>
		public RuleInvocationType InvocationTypes { get; protected set; }

		/// <summary>
		/// Gets or sets the execution location governing where the rule will run: server (default) and/or client.
		/// </summary>
		public RuleExecutionLocation ExecutionLocation { get; set; }

		/// <summary>
		/// Gets the set of predicate paths that trigger property change invocations.
		/// </summary>
		public string[] Predicates { get; internal set; }

		/// <summary>
		/// Gets the set of properties that trigger property get invocations.
		/// </summary>
		public string[] ReturnValues { get; internal set; }

		/// <summary>
		/// Gets the set of <see cref="ConditionType"/> instances the current rule is responsible
		/// for associating with instances in the graph.
		/// </summary>
		public IEnumerable<ConditionType> ConditionTypes
		{
			get
			{
				return conditionTypes;
			}
		}

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
					.ToArray();

			// Automatically mark rules with return values as property get rules
			if (this.ReturnValues.Length > 0)
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
				.SelectMany(type => type.BaseType.IsGenericType ? new Type[] {type, type.BaseType} : new Type[] { type }))
			{
				rules.AddRange(
					type.GetFields(BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic)
						.Where(field => typeof(IRuleProvider).IsAssignableFrom(field.FieldType))
						.SelectMany(field =>
						{
							IRuleProvider ruleProvider = (IRuleProvider)field.GetValue(null);
							if (ruleProvider != null)
								return ruleProvider.GetRules(type, field.Name);
							return new Rule[] {};
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
				{}
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
			foreach (Rule rule in GetRules(types))
				rule.Register();
		}

		/// <summary>
		/// Registers all static rules defined on the types in the specified assembly.
		/// </summary>
		/// <param name="types"></param>
		/// <returns></returns>
		public static void RegisterRules(Assembly assembly)
		{
			RegisterRules(assembly.GetTypes());
		}

		/// <summary>
		/// Gets the set of rules registered for the specified <see cref="GraphType"/>.
		/// </summary>
		/// <param name="type"></param>
		/// <returns></returns>
		public static IEnumerable<Rule> GetRegisteredRules(GraphType type)
		{
			return type.GetExtension<List<Rule>>();
		}

		/// <summary>
		/// Invokes the rule on the specified <see cref="GraphInstance"/> as a result
		/// of the specified triggering <see cref="GraphEvent"/>
		/// </summary>
		/// <param name="root"></param>
		/// <param name="graphEvent"></param>
		internal protected abstract void Invoke(GraphInstance root, GraphEvent graphEvent);

		/// <summary>
		/// Registers the rule with the current <see cref="GraphContext"/>.
		/// </summary>
		public void Register()
		{
			// Default the invocation type to PropertyChanged if none were assigned
			if ((int)InvocationTypes == 1)
				InvocationTypes = RuleInvocationType.PropertyChanged;

			// Automatically detect predicates if none were specified
			if (Predicates == null && ((InvocationTypes & (RuleInvocationType.PropertyChanged | RuleInvocationType.PropertyGet)) > 0))
				SetPredicates(GetPredicates());

			// Track the rule registration for the root graph type
			List<Rule> rules = RootType.GetExtension<List<Rule>>();
			rules.Add(this);

			// Do not perform graph type event registration if the rule is not supposed to execute on the server
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
						if (e.IsFirstAccess || manager.IsPendingInvocation(this))
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

								// Invoke the rule when the last graph event scope exits
								GraphEventScope.OnExit(() =>
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
			Initialize(action);
		}

		internal Rule(string name, RuleInvocationType invocationTypes, string rootType, string[] predicates)
			: base(rootType ?? GraphContext.Current.GetGraphType<TRoot>().Name, name, invocationTypes, predicates)
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
		public Action<TRoot> Action { get; private set; }

		/// <summary>
		/// Gets the root <see cref="GraphType"/> of the rule.
		/// </summary>
		public override GraphType RootType
		{
			get
			{
				return GraphContext.Current.GetGraphType<TRoot>();
			}
		}

		#endregion

		#region Methods

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
			Predicates = predicates;
			if ((this.InvocationTypes & RuleInvocationType.PropertyGet) == 0)
				this.InvocationTypes |= RuleInvocationType.PropertyChanged;
			return this;
		}

		public Rule<TRoot> Returns(params Expression<Func<TRoot, object>>[] properties)
		{
			return Returns(properties.Select(p => p.Body is MemberExpression ? p.Body : ((UnaryExpression)p.Body).Operand).OfType<MemberExpression>().Select(m => m.Member.Name).ToArray());
		}

		public Rule<TRoot> Returns(params string[] properties)
		{
			ReturnValues = properties;
			this.InvocationTypes |= RuleInvocationType.PropertyGet;
			this.InvocationTypes &= ~RuleInvocationType.PropertyChanged;
			return this;
		}

		public Rule<TRoot> Asserts(params ConditionType[] conditionTypes)
		{
			this.conditionTypes = conditionTypes;
			return this;
		}

		protected override string[] GetPredicates()
		{
			GraphPath path;
			return PredicateBuilder
				.GetPredicates(Action.Method, method => Rule<TRoot>.PredicateFilter(Action.Method, method), (InvocationTypes | RuleInvocationType.PropertyGet) > 0)
				.Where(predicate => RootType.TryGetPath(predicate.StartsWith("return ") ? predicate.Substring(7) : predicate, out path))
				.ToArray();
		}

		internal void Initialize(Action<TRoot> action)
		{
			// Set the rule action
			Action = action;
		}

		/// <summary>
		/// Converts <see cref="Action<TRoot>"/> into a corresponding <see cref="Rule<TRoot>"/> instance.
		/// </summary>
		public static implicit operator Rule<TRoot>(Action<TRoot> action)
		{
			return new Rule<TRoot>(action);
		}

		/// <summary>
		/// Invokes the action for the current rule on the specified <see cref="GraphInstance"/>.
		/// </summary>
		/// <param name="root"></param>
		/// <param name="graphEvent"></param>
		internal protected override void Invoke(GraphInstance root, GraphEvent graphEvent)
		{
			Action((TRoot)root.Instance);
		}

		#endregion
	}

	public delegate void Rule2<TRoot>(TRoot root);

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
			Initialize(OnInvoke);
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
			: base(typeof(TRule).Name, 0, null, new string[] {})
		{ }

		void OnInvoke(GraphInstance instance, TEvent e)
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
