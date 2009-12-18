using System;
using System.Linq;
using System.Collections.Generic;
using System.Reflection;
using System.Text.RegularExpressions;
using System.Collections;
using ExoGraph;

namespace ExoRule
{
	#region Rule

	/// <summary>
	/// Abstract base class for all rule instances. Rules will either be instances
	/// or subclasses of <see cref="Rule<T>"/> which inherits directly from <see cref="Rule"/>.
	/// </summary>
	public abstract class Rule
	{
		string[] returnValues;

		/// <summary>
		/// Creates a new rule instance.
		/// </summary>
		/// <param name="name"></param>
		/// <param name="invocationTypes"></param>
		/// <param name="predicates"></param>
		internal Rule(string name, RuleInvocationType invocationTypes, string[] predicates, MethodBase action)
		{
			this.Name = name;
			this.InvocationTypes = invocationTypes;

			// Automatically detect predicates if not were specified
			if (predicates == null && ((invocationTypes & (RuleInvocationType.PropertyChanged | RuleInvocationType.PropertyGet)) > 0))
			{
				Assembly rootAssembly = action.GetParameters()[0].ParameterType.Assembly;
				predicates = PredicateBuilder.GetPredicates(action,
					(method) => method.DeclaringType.Assembly == rootAssembly).ToArray();
			}

			this.Predicates = predicates;
			this.returnValues = predicates
				.Where((predicate) => predicate.EndsWith(" return") )
				.Select((predicate) => predicate.Substring(0, predicate.Length - 7))
				.ToArray();
		}

		/// <summary>
		/// Gets the name of the rule.
		/// </summary>
		public string Name { get; private set; }

		/// <summary>
		/// Gets the set of <see cref="RuleInvocationType"/> governing when the rule will run.
		/// </summary>
		public RuleInvocationType InvocationTypes { get; private set; }

		/// <summary>
		/// Gets the set of predicate paths that govern property get and change invocations.
		/// </summary>
		public string[] Predicates { get; private set; }

		/// <summary>
		/// Gets the root <see cref="GraphType"/> for the current rule.
		/// </summary>
		/// <returns></returns>
		public abstract GraphType GetRootType();

		/// <summary>
		/// Invokes the rule on the specified <see cref="GraphInstance"/> as a result
		/// of the specified triggering <see cref="GraphEvent"/>
		/// </summary>
		/// <param name="root"></param>
		/// <param name="graphEvent"></param>
		internal abstract void Invoke(GraphInstance root, GraphEvent graphEvent);

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
		/// Registers the rule with the current <see cref="GraphContext"/>.
		/// </summary>
		public void Register()
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
	}

	#endregion

	#region Rule<T>
	
	/// <summary>
	/// Concrete subclass of <see cref="Rule"/> that represents a rule for a specific root type.
	/// </summary>
	/// <typeparam name="TRoot"></typeparam>
	public class Rule<TRoot> : Rule
			where TRoot : class
	{
		string rootType;

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
			: base(name, invocationTypes, predicates, action.Method)
		{
			this.rootType = rootType;
			this.Action = action;
		}

		/// <summary>
		/// Gets the action that will be performed when the rule is invoked.
		/// </summary>
		public Action<TRoot> Action { get; private set; }

		/// <summary>
		/// Gets the root <see cref="GraphType"/> for the current rule.
		/// </summary>
		/// <returns></returns>
		public override GraphType GetRootType()
		{
			if (rootType != null)
				return GraphContext.Current.GetGraphType(rootType);
			else
				return GraphContext.Current.GetGraphType<TRoot>();
		}

		/// <summary>
		/// Invokes the action for the current rule on the specified <see cref="GraphInstance"/>.
		/// </summary>
		/// <param name="root"></param>
		/// <param name="graphEvent"></param>
		internal override void Invoke(GraphInstance root, GraphEvent graphEvent)
		{
			Action((TRoot)root.Instance);
		}
	}

	#endregion
}
