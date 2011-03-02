using System;
using System.Linq;
using System.Collections.Generic;
using System.Reflection;
using System.Text.RegularExpressions;
using System.Collections;
using ExoGraph;
using System.Runtime.Serialization;

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

		ConditionType[] conditionTypes;
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
			if (predicates != null)
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
		public GraphType RootType
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
		/// Gets the set of <see cref="RuleInvocationType"/> governing when the rule will run.
		/// </summary>
		public RuleInvocationType InvocationTypes { get; protected set; }

		public RuleExecutionLocation ExecutionLocation { get; set; }

		/// <summary>
		/// Gets the set of predicate paths that trigger property change invocations.
		/// </summary>
		public string[] Predicates { get; private set; }

		/// <summary>
		/// Gets the set of properties that trigger property get invocations.
		/// </summary>
		public string[] ReturnValues { get; private set; }

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
					.Where(predicate => !predicate.EndsWith(" return"))
					.ToArray(); ;

			this.ReturnValues = predicates
					.Where((predicate) => predicate.EndsWith(" return"))
					.Select((predicate) => predicate.Substring(0, predicate.Length - 7))
					.ToArray();
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
							if (error.Code == null)
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
					if (e.IsFirstAccess && this.ReturnValues.Contains(e.Property.Name))
						Invoke(e.Instance, e);
				};
			}

			// Property Change Invocation
			if ((InvocationTypes & RuleInvocationType.PropertyChanged) == RuleInvocationType.PropertyChanged)
			{
				// Subscribe to property change notifications for all rule predicates
				foreach (string predicate in Predicates)
				{
					RootType.GetPath(predicate).Change += (sender, e) =>
					{
						// Get the join point for the current rule and instance
						RuleState state = e.Instance.GetExtension<RuleManager>().GetState(this);

						// Register the rule to run if it is not already registered
						if (!state.IsPendingInvocation)
						{
							// Flag the instance as pending invocation for this rule
							state.IsPendingInvocation = true;

							// Invoke the rule when the last graph event scope exits
							GraphEventScope.OnExit(() => 
							{
								// Only invoke the rule if the instance is of the same type as the rule root type
								if (RootType.IsInstanceOfType(e.Instance)) 
									Invoke(e.Instance, e); 
							});
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

		#region RuleState

		/// <summary>
		/// Tracks instance-specific state for each rule.
		/// </summary>
		internal class RuleState
		{
			internal RuleState()
			{ }

			public bool ShouldInvoke { get; set; }
			public bool IsPendingInvocation { get; set; }
		}

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
	[DataContract]
	public class Rule<TRoot> : Rule
			where TRoot : class
	{
		#region Fields

		string rootType;
		string[] returnValues;

		#endregion

		#region Constructors

		static Rule()
		{
			PredicateFilter = (action, method) => method.DeclaringType.Assembly == action.GetParameters()[0].ParameterType.Assembly;
		}

		public Rule(Action<TRoot> action)
			: this(null, RuleInvocationType.PropertyChanged, null, null, action)
		{ }

		public Rule(RuleInvocationType invocationTypes, Action<TRoot> action)
			: this(null, invocationTypes, null, null, action)
		{ }

		public Rule(RuleInvocationType invocationTypes, string[] predicates, Action<TRoot> action)
			: this(null, invocationTypes, null, predicates, action)
		{ }

		public Rule(RuleInvocationType invocationTypes, string[] predicates, ConditionType[] conditionTypes, Action<TRoot> action)
			: this(null, invocationTypes, null, predicates, conditionTypes, action)
		{ }

		public Rule(RuleInvocationType invocationTypes, string rootType, Action<TRoot> action)
			: this(null, invocationTypes, rootType, null, action)
		{ }

		public Rule(RuleInvocationType invocationTypes, string rootType, string[] predicates, Action<TRoot> action)
			: this(null, invocationTypes, rootType, predicates, action)
		{ }

		public Rule(RuleInvocationType invocationTypes, string rootType, string[] predicates, ConditionType[] conditionTypes, Action<TRoot> action)
			: this(null, invocationTypes, rootType, predicates, conditionTypes, action)
		{ }

		public Rule(string name, Action<TRoot> action)
			: this(name, RuleInvocationType.PropertyChanged, null, null, null, action)
		{ }

		public Rule(string name, RuleInvocationType invocationTypes, Action<TRoot> action)
			: this(name, invocationTypes, null, null, null, action)
		{ }

		public Rule(string name, RuleInvocationType invocationTypes, string[] predicates, Action<TRoot> action)
			: this(name, invocationTypes, null, predicates, null, action)
		{ }

		public Rule(string name, RuleInvocationType invocationTypes, string rootType, Action<TRoot> action)
			: this(name, invocationTypes, rootType, null, null, action)
		{ }

		public Rule(string name, RuleInvocationType invocationTypes, string rootType, string[] predicates, Action<TRoot> action)
			: this(name, invocationTypes, rootType, predicates, null, action)
		{ }

		public Rule(string name, RuleInvocationType invocationTypes, string rootType, string[] predicates, ConditionType[] conditionTypes, Action<TRoot> action)
			: base(rootType ?? GraphContext.Current.GetGraphType<TRoot>().Name, name, invocationTypes, conditionTypes, predicates)
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

		#endregion

		#region Methods

		internal void Initialize(Action<TRoot> action)
		{
			// Set the rule action
			Action = action;

			// Automatically detect predicates if none were specified
			if (Predicates == null && ((InvocationTypes & (RuleInvocationType.PropertyChanged | RuleInvocationType.PropertyGet)) > 0))
			{
				GraphPath path;
				SetPredicates(
					PredicateBuilder.GetPredicates(action.Method, method => Rule<TRoot>.PredicateFilter(action.Method, method))
					.Where(predicate => RootType.TryGetPath(predicate, out path))
					.ToArray());
			}
		}



		/// <summary>
		/// Converts <see cref="Action<TRoot>"/> into a corresponding <see cref="Rule<TRoot>"/> instance.
		/// </summary>
		public static implicit operator Rule<TRoot>(Delegate action)
		{
			return new Rule<TRoot>((Action<TRoot>)action);
		}
		/// <summary>
		/// Invokes the action for the current rule on the specified <see cref="GraphInstance"/>.
		/// </summary>
		/// <param name="root"></param>
		/// <param name="graphEvent"></param>
		internal protected override void Invoke(GraphInstance root, GraphEvent graphEvent)
		{
			root.GetExtension<RuleManager>().GetState(this).IsPendingInvocation = false;
			Action((TRoot)root.Instance);
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
