using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace ExoRule
{
	/// <summary>
	/// Base class for simple events that represent both the payload 
	/// and execution logic associated with the event.  
	/// </summary>
	/// <typeparam name="TEvent"></typeparam>
	/// <typeparam name="TRoot"></typeparam>
	public abstract class Event<TEvent, TRoot>
		where TEvent : Event<TEvent, TRoot>, new()
		where TRoot : class
	{
		static Rule EventRule = new Rule();

		protected abstract void OnInvoke(TRoot root);

		class Rule : Rule<Rule, TRoot, TEvent>
		{
			public Rule()
				: base(typeof(TEvent).Name)
			{ }

			protected override void OnInvoke(TRoot root, TEvent e)
			{
				e.OnInvoke(root);
			}
		}
	}
}
