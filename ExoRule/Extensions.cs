using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using ExoModel;

namespace ExoRule
{
	public static class Extensions
	{
		/// <summary>
		/// Gets the <see cref="Type"/> of event registered with the specified name on the current <see cref="ModelType"/>.
		/// </summary>
		/// <param name="modelType"></param>
		/// <param name="eventName"></param>
		/// <returns></returns>
		public static Type GetEventType(this ModelType modelType, string eventName)
		{
			Type eventType = null;
			while (modelType != null)
			{
				modelType.GetExtension<Events>().TryGetValue(eventName, out eventType);
				if (eventType != null)
					return eventType;
				modelType = modelType.BaseType;
			}
			return eventType;
		}

		/// <summary>
		/// Gets the names of all events registered for the current <see cref="ModelType"/>.
		/// </summary>
		/// <param name="modelType"></param>
		/// <returns></returns>
		public static IEnumerable<string> GetEvents(this ModelType modelType)
		{
			return modelType.GetExtension<Events>().Keys;
		}

		/// <summary>
		/// Registers an event type with the specified name on the current <see cref="ModelType"/>.
		/// </summary>
		/// <param name="modelType"></param>
		/// <param name="eventName"></param>
		/// <param name="eventType"></param>
		public static void Subscribe<TEvent>(this ModelType modelType, string eventName, ModelType.CustomEvent<TEvent> handler)
		{
			modelType.GetExtension<Events>()[eventName] = typeof(TEvent);
			modelType.Subscribe<TEvent>(handler);
		}

		class Events : Dictionary<string, Type>
		{ }

		/// <summary>
		/// Returns all rules registered for the instance's type and ancestor types.
		/// </summary>
		/// <param name="instance"></param>
		/// <returns></returns>
		public static IEnumerable<Rule> GetRules(this ModelInstance instance)
		{
			return instance.Type.GetAncestorsInclusive().SelectMany(t => Rule.GetRegisteredRules(t));
		}

		/// <summary>
		/// Runs all property get rules pending invocation for the specified instance.
		/// </summary>
		/// <param name="instance"></param>
		public static void RunPendingPropertyGetRules(this ModelInstance instance, Func<ModelProperty, bool> when)
		{
			instance.GetExtension<RuleManager>().RunPendingPropertyGetRules(instance, when);
		}

		/// <summary>
		/// Runs all property get rules pending invocation for the specified instance.
		/// </summary>
		/// <param name="instance"></param>
		public static void RunPropertyGetRules(this ModelInstance instance, Func<ModelProperty, bool> when)
		{
			foreach (Rule rule in instance.GetRules().Where(rule => (rule.InvocationTypes & RuleInvocationType.PropertyGet) > 0 && rule.ReturnValues.Select(p => rule.RootType.Properties[p]).Any(when)))
				rule.Invoke(instance, null);
		}
	}
}
