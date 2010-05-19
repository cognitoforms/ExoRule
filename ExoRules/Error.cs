using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Runtime.Serialization;

namespace ExoRule
{
	[DataContract]
	public class Error : ConditionType
	{
		public Error(string defaultMessage)
			: this(null, defaultMessage)
		{ }

		public Error(string code, string defaultMessage)
			: base(code, ConditionCategory.Error, defaultMessage)
		{ }

		public Error(ConditionTypeSet[] sets, string code, string defaultMessage)
			: base(sets, code, ConditionCategory.Error, defaultMessage)
		{ }

		public static implicit operator Error(string defaultMessage)
		{
			return new Error(defaultMessage);
		}
	}

	[DataContract()]
	public class Error<TRoot> : Error
		where TRoot : class
	{
		/// <summary>
		/// Creates a new error and rule
		/// </summary>
		/// <param name="defaultMessage">Message describing the error</param>
		/// <param name="condition">Condition that when true, indicates this error applies to the specified object.</param>
		public Error(string defaultMessage, Predicate<TRoot> condition)
			: this((ConditionTypeSet[])null, null, defaultMessage, condition)
		{ }

		/// <summary>
		/// Creates a new error and rule
		/// </summary>
		/// <param name="set">The ConditionTypeSet the rule is associated with</param>
		/// <param name="defaultMessage">Message describing the error</param>
		/// <param name="condition">Condition that when true, indicates this error applies to the specified object.</param>
		public Error(ConditionTypeSet set, string defaultMessage, Predicate<TRoot> condition)
			: this(new ConditionTypeSet[] {set}, null, defaultMessage, condition)
		{ }

		/// <summary>
		/// Creates a new error and rule
		/// </summary>
		/// <param name="sets">The list of ConditionTypeSets the rule is associated with</param>
		/// <param name="defaultMessage">Message describing the error</param>
		/// <param name="condition">Condition that when true, indicates this error applies to the specified object.</param>
		public Error(ConditionTypeSet[] sets, string defaultMessage, Predicate<TRoot> condition)
			: this(sets, null, defaultMessage, condition)
		{ }

		/// <summary>
		/// Creates a new error and rule
		/// </summary>
		/// <param name="code">Unique code for the condition type</param>
		/// <param name="defaultMessage">Message describing the error</param>
		/// <param name="condition">Condition that when true, indicates this error applies to the specified object.</param>
		/// <summary>
		public Error(string code, string defaultMessage, Predicate<TRoot> condition)
			: this((ConditionTypeSet[])null, code, defaultMessage, condition)
		{
		}

		/// <summary>
		/// Creates a new error and rule
		/// </summary>
		/// <param name="set">The ConditionTypeSet the rule is associated with</param>
		/// <param name="code">Unique code for the condition type</param>
		/// <param name="defaultMessage">Message describing the error</param>
		/// <param name="condition">Condition that when true, indicates this error applies to the specified object.</param>
		/// <summary>
		public Error(ConditionTypeSet set, string code, string defaultMessage, Predicate<TRoot> condition)
			: this(new ConditionTypeSet[] { set }, code, defaultMessage, condition)
		{
		}

		/// <summary>
		/// Creates a new error and rule
		/// </summary>
		/// <param name="sets">The list of ConditionTypeSets the rule is associated with</param>
		/// <param name="code">Unique code for the condition type</param>
		/// <param name="defaultMessage">Message describing the error</param>
		/// <param name="condition">Condition that when true, indicates this error applies to the specified object.</param>
		/// <summary>
		public Error(ConditionTypeSet[] sets, string code, string defaultMessage, Predicate<TRoot> condition)
			: base(sets, code, defaultMessage)
		{
			CreateConditionRule<TRoot>(condition, null, null);
		}
	}
}
