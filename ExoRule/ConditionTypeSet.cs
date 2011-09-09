using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Runtime.Serialization;

namespace ExoRule
{

	/// <summary>
	/// A collection of condition types
	/// </summary>
	[Serializable]
	[DataContract]
	public class ConditionTypeSet
	{
		#region Fields

		static Dictionary<string, ConditionTypeSet> sets = new Dictionary<string, ConditionTypeSet>();

		#endregion

		#region Constructors

		public ConditionTypeSet(string name)
		{
			sets.Add(name, this);
			this.Name = name;
		}

		#endregion

		#region Properties

		/// <summary>
		/// The unique name of the set of conditions.
		/// </summary>
		public string Name { get; private set; }

		#endregion

		#region Methods

		/// <summary>
		/// Convert a string corresponding to a set name to a condition type set.
		/// </summary>
		public static implicit operator ConditionTypeSet(string name)
		{
			ConditionTypeSet set;
			sets.TryGetValue(name, out set);
			return set;
		}

		#endregion
	}
}
