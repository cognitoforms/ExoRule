using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using ExoGraph;

namespace ExoRule
{
	public static class Extensions
	{
		/// <summary>
		/// Gets the <see cref="Type"/> of event registered with the specified name on the current <see cref="GraphType"/>.
		/// </summary>
		/// <param name="graphType"></param>
		/// <param name="eventName"></param>
		/// <returns></returns>
		public static Type GetEventType(this GraphType graphType, string eventName)
		{
			Type eventType = null;
			while (graphType != null)
			{
				graphType.GetExtension<Events>().TryGetValue(eventName, out eventType);
				if (eventType != null)
					return eventType;
				graphType = graphType.BaseType;
			}
			return eventType;
		}

		/// <summary>
		/// Gets the names of all events registered for the current <see cref="GraphType"/>.
		/// </summary>
		/// <param name="graphType"></param>
		/// <returns></returns>
		public static IEnumerable<string> GetEvents(this GraphType graphType)
		{
			return graphType.GetExtension<Events>().Keys;
		}

		/// <summary>
		/// Registers an event type with the specified name on the current <see cref="GraphType"/>.
		/// </summary>
		/// <param name="graphType"></param>
		/// <param name="eventName"></param>
		/// <param name="eventType"></param>
		public static void Subscribe<TEvent>(this GraphType graphType, string eventName, GraphType.CustomEvent<TEvent> handler)
		{
			graphType.GetExtension<Events>()[eventName] = typeof(TEvent);
			graphType.Subscribe<TEvent>(handler);
		}

		class Events : Dictionary<string, Type>
		{ }

		/// <summary>
		/// Returns all rules registered for the instance's type and ancestor types.
		/// </summary>
		/// <param name="instance"></param>
		/// <returns></returns>
		public static IEnumerable<Rule> GetRules(this GraphInstance instance)
		{
			return instance.Type.GetAncestorsInclusive().SelectMany(t => Rule.GetRegisteredRules(t));
		}

		/// <summary>
		/// Runs all property get rules pending invocation for the specified instance.
		/// </summary>
		/// <param name="instance"></param>
		public static void RunPendingPropertyGetRules(this GraphInstance instance, Func<GraphProperty, bool> when)
		{
			instance.GetExtension<RuleManager>().RunPendingPropertyGetRules(instance, when);
		}

		/// <summary>
		/// Runs all property get rules pending invocation for the specified instance.
		/// </summary>
		/// <param name="instance"></param>
		public static void RunPropertyGetRules(this GraphInstance instance, Func<GraphProperty, bool> when)
		{
			foreach (Rule rule in instance.GetRules().Where(rule => (rule.InvocationTypes & RuleInvocationType.PropertyGet) > 0 && rule.ReturnValues.Select(p => rule.RootType.Properties[p]).Any(when)))
				rule.Invoke(instance, null);
		}
	}
}
