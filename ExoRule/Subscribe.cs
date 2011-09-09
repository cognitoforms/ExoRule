using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using ExoGraph;
using System.Reflection;

namespace ExoRule
{
	/// <summary>
	/// Helper class that simplifies the process of declaring and subscribing to custom events.
	/// If the event class implements the interface <see cref="ExoRule.ISimpleEvent"/>, then
	/// rule implementation code does not need to be defined with the registration of the event,
	/// since the interface implementation is used.  Alternatively, if the class defines a method
	/// that accepts a single argument that is the type of the class that raises the event
	/// (the "TSource" type parameter), then the name of the method can be passed as a second
	/// argument and it is not necessary for the event to implement a special interface.
	/// </summary>
	/// <typeparam name="TSource">The type that raises an event.</typeparam>
	public static class Subscribe<TSource>
		where TSource : class
	{
		#region Fields

		static Dictionary<Type, MethodInfo> cachedMethods;

		#endregion

		#region Methods

		private static void Perform<TEvent>(GraphContext ctx, GraphType.CustomEvent<TEvent> handler)
		{
			ctx.GetGraphType<TSource>().Subscribe<TEvent>(handler);
		}

		/// <summary>
		/// Subscribe to the event type on the given context.
		/// </summary>
		/// <typeparam name="TEvent">The type of event to subscribe to.  Must implement ISimpleEvent&lt;TSource&gt;</typeparam>
		/// <param name="ctx">The graph context.</param>
		public static void To<TEvent>(GraphContext ctx)
			where TEvent : ISimpleEvent<TSource>
		{
			Perform<TEvent>(ctx, (instance, evt) => evt.Execute((TSource)instance.Instance));
		}

		/// <summary>
		/// Subscribe to the event type on the given context, using a method with the given name.  The 
		/// event type must contain a method with the given name with a single argument of type TSource.
		/// </summary>
		/// <typeparam name="TEvent">The type of event to subscribe to.</typeparam>
		/// <param name="ctx">The graph context.</param>
		/// <param name="methodName">The name of the method to invoke when the event is raised.</param>
		public static void To<TEvent>(GraphContext ctx, string methodName)
			where TEvent : class
		{
			MethodInfo method = null;

			bool methodCached = false;

			// Determine if the method has been cached.  This may happen because contexts are created 
			// and events are registered per-thread, but we only need to inspect the type once.
			if (cachedMethods == null)
				cachedMethods = new Dictionary<Type, MethodInfo>();
			else
				methodCached = cachedMethods.ContainsKey(typeof(TEvent));

			if (methodCached)
			{
				method = cachedMethods[typeof(TEvent)];
			}
			else
			{
				// Find a method of the given name with a single parameter of type TSource
				method = typeof(TEvent).GetMethod(methodName, new Type[] { typeof(TSource) });
				cachedMethods[typeof(TEvent)] = method;
			}

			if (method == null)
			{
				throw new ApplicationException(string.Format("Couldn't find method with signature " +
					"\"void {0}({1} arg)\" on type {2}.", methodName, typeof(TSource).Name, typeof(TEvent).Name));
			}

			Perform<TEvent>(ctx, (instance, evt) => method.Invoke(evt, new object[] { (TSource)instance.Instance }));
		}

		#endregion
	}
}
