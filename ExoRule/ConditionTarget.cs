using System;
using System.Collections.Generic;
using System.Linq;
using System.Text; 
using ExoModel;

namespace ExoRule
{
	/// <summary>
	/// Represents the target of an condition, including the set of properties involved.
	/// </summary>
	public class ConditionTarget
	{
		WeakReference target;
		IEnumerable<string> properties;

		internal ConditionTarget(Condition condition, ModelInstance target, params string[] properties)
		{
			this.Condition = condition;
			this.target = new WeakReference(target);
			this.properties = properties;
			target.GetExtension<RuleManager>().SetCondition(this);
		}

		public Condition Condition { get; private set; }

		public ModelInstance Target
		{
			get
			{
				return (ModelInstance)(target.IsAlive ? target.Target : null);
			}
		}

		internal void AddProperty(string property)
		{
			// Ensure the property store is a mutable list
			if (properties == null)
				properties = new List<string>();
			else if (properties is string[])
				properties = new List<string>(properties);

			// Add the property
			((List<string>)properties).Add(property);
		}

		public IEnumerable<string> Properties
		{
			get
			{
				return properties;
			}
		}
	}
}
