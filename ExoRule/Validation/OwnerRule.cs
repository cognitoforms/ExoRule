using System;
using System.Linq;
using System.Text;
using System.Collections;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.Runtime.Serialization;
using ExoGraph;
using ExoRule;

namespace ExoRule.Validation
{
	/// <summary>
	/// Marks an instance as pending delete when a property considered to be the owner
	/// is set to null.
	/// </summary>
	public class OwnerRule : PropertyRule
	{
		#region Constructors

		public OwnerRule(string rootType, string property)
			: this(rootType, property, RuleInvocationType.PropertyChanged)
		{ }

		public OwnerRule(string rootType, string property, RuleInvocationType invocationTypes)
			: base(rootType, property, null, invocationTypes, property)
		{ }

		#endregion

		#region Methods

		protected internal override void OnInvoke(GraphInstance root, GraphEvent graphEvent)
		{
			// Marks the instance as pending delete if an owner property is set to null.
			if (Property is GraphReferenceProperty && !Property.IsList)
				root.IsPendingDelete = root.GetReference((GraphReferenceProperty)Property) == null;
		}

		protected override bool ConditionApplies(GraphInstance root)
		{
			throw new NotSupportedException();
		}

		#endregion
	}
}
