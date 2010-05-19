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
	/// Abstract base class for all rule instances. Rules will either be instances
	/// or subclasses of <see cref="Rule<T>"/> which inherits directly from <see cref="Rule"/>.
	/// </summary>
	[DataContract]
	public abstract class Rule : IRuleProvider
	{
		#region Constructors

		/// <summary>
		/// Creates a new rule instance.
		/// </summary>
		/// <param name="name"></param>
		/// <param name="invocationTypes"></param>
		/// <param name="predicates"></param>
		internal protected Rule(string name)
		{
			this.Name = name;
		}

		#endregion

		#region Properties

		/// <summary>
		/// Gets the name of the rule.
		/// </summary>
		public string Name { get; private set; }

		#endregion

		#region Methods

		/// <summary>
		/// Gets all static rules defined on the specified types.
		/// </summary>
		/// <param name="types"></param>
		/// <returns></returns>
		public static IEnumerable<Rule> GetRules(Type[] types)
		{
			// Fetch the set of rules declared on the specified types
			List<Rule> rules = new List<Rule>();
			foreach (Type type in types)
			{
				rules.AddRange(
					type.GetFields(BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic)
						.Where(field => typeof(IRuleProvider).IsAssignableFrom(field.FieldType))
						.SelectMany(field =>
						{
							IRuleProvider ruleProvider = (IRuleProvider)field.GetValue(null);
							if (ruleProvider != null)
								return ruleProvider.GetRules(field.Name);
							return null;
						})
						.Where(rule => rule != null)
				);

				// Ensure the error code has been set on all statically declared rule errors
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
		public static void RegisterRules(Type[] types)
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
			foreach (Rule rule in GetRules(assembly.GetTypes()))
				rule.Register();
		}

		/// <summary>
		/// Creates an instance of <see cref="IRuleRoot"/> that can be used as a
		/// graph extension factory when creating a <see cref="GraphContext"/> that will
		/// be using rules.  This allows the rule infrastructure to maintain state with each 
		/// <see cref="GraphInstance"/> necessary to control rule invocation.
		/// </summary>
		/// <param name="instance"></param>
		/// <returns></returns>
		public static IRuleRoot CreateRuleRoot(GraphInstance instance)
		{
			return new Root();
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
		public abstract void Register();

		#endregion

		#region Root

		/// <summary>
		/// Private implementation of <see cref="IRuleRoot"/> used to provide access
		/// to an instance of <see cref="RuleManager"/> for each <see cref="GraphInstance"/>.
		/// </summary>
		class Root : IRuleRoot
		{
			RuleManager manager;

			RuleManager IRuleRoot.Manager
			{
				get
				{
					if (manager == null)
						manager = new RuleManager();
					return manager;
				}
			}
		}

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

		IEnumerable<Rule> IRuleProvider.GetRules(string name)
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

		public Rule(RuleInvocationType invocationTypes, string rootType, Action<TRoot> action)
			: this(null, invocationTypes, rootType, null, action)
		{ }

		public Rule(RuleInvocationType invocationTypes, string rootType, string[] predicates, Action<TRoot> action)
			: this(null, invocationTypes, rootType, predicates, action)
		{ }
		
		public Rule(string name, Action<TRoot> action)
			: this(name, RuleInvocationType.PropertyChanged, null, null, action)
		{ }

		public Rule(string name, RuleInvocationType invocationTypes, Action<TRoot> action)
			: this(name, invocationTypes, null, null, action)
		{ }

		public Rule(string name, RuleInvocationType invocationTypes, string[] predicates, Action<TRoot> action)
			: this(name, invocationTypes, null, predicates, action)
		{ }

		public Rule(string name, RuleInvocationType invocationTypes, string rootType, Action<TRoot> action)
			: this(name, invocationTypes, rootType, null, action)
		{ }

		public Rule(string name, RuleInvocationType invocationTypes, string rootType, string[] predicates, Action<TRoot> action)
			: base(name)
		{
			this.InvocationTypes = invocationTypes;
			this.rootType = rootType;
			this.Predicates = predicates;
			Initialize(action);
		}

		internal Rule(string name, RuleInvocationType invocationTypes, string rootType, string[] predicates)
			: base(name)
		{
			this.InvocationTypes = invocationTypes;
			this.rootType = rootType;
			this.Predicates = predicates;
		}

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
		/// Gets the set of <see cref="RuleInvocationType"/> governing when the rule will run.
		/// </summary>
		public RuleInvocationType InvocationTypes { get; private set; }

		/// <summary>
		/// Gets the set of predicate paths that govern property get and change invocations.
		/// </summary>
		public string[] Predicates { get; private set; }

		#endregion

		#region Methods

		internal void Initialize(Action<TRoot> action)
		{
			// Set the rule action
			Action = action;

			// Automatically detect predicates if not were specified
			if (Predicates == null && ((InvocationTypes & (RuleInvocationType.PropertyChanged | RuleInvocationType.PropertyGet)) > 0))
				Predicates = PredicateBuilder.GetPredicates(action.Method, method => Rule<TRoot>.PredicateFilter(action.Method, method)).ToArray();

			// Determine the set of return values
			this.returnValues = Predicates
				.Where((predicate) => predicate.EndsWith(" return"))
				.Select((predicate) => predicate.Substring(0, predicate.Length - 7))
				.ToArray();
		}

		/// <summary>
		/// Gets the root <see cref="GraphType"/> for the current rule.
		/// </summary>
		/// <returns></returns>
		public GraphType GetRootType()
		{
			if (rootType != null)
				return GraphContext.Current.GetGraphType(rootType);
			else
				return GraphContext.Current.GetGraphType<TRoot>();
		}

		public override void  Register()
		{
			GraphType rootType = GetRootType();

			// Init Invocation
			if ((InvocationTypes & RuleInvocationType.InitExisting) == RuleInvocationType.InitExisting ||
				 (InvocationTypes & RuleInvocationType.InitNew) == RuleInvocationType.InitNew)
			{
				rootType.Init += (sender, e) =>
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
				//rootType.PropertyGet += (sender, e) =>
				//{
				//   if (e.IsFirstAccess && (e.Instance.IsNew || 

				//};
			}

			// Property Change Invocation
			if ((InvocationTypes & RuleInvocationType.PropertyChanged) == RuleInvocationType.PropertyChanged)
			{
				// Subscribe to property change notifications for all rule predicates
				foreach (string predicate in Predicates)
				{
					rootType.GetPath(predicate).Change += (sender, e) =>
					{
						// Get the join point for the current rule and instance
						RuleState state = e.Instance.GetExtension<IRuleRoot>().Manager.GetState(this);
						
						// Register the rule to run if it is not already registered
						if (!state.IsPendingInvocation)
						{
							// Flag the instance as pending invocation for this rule
							state.IsPendingInvocation = true;

							// Invoke the rule when the last graph event scope exits
							GraphEventScope.OnExit(() => Invoke(e.Instance, e));
						}
					};
				}
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
			root.GetExtension<IRuleRoot>().Manager.GetState(this).IsPendingInvocation = false;
			if(root.Instance is TRoot)
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
}
