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
		public Error(string message)
			: this(null, message)
		{ }

		public Error(string code, string message, params ConditionTypeSet[] sets)
			: base(code, ConditionCategory.Error, message, sets)
		{ }

		public Error(string code, string message, Type sourceType, Func<string, string> translator, params ConditionTypeSet[] sets)
			: base(code, ConditionCategory.Error, message, sourceType, translator, sets)
		{ }

		public static implicit operator Error(string message)
		{
			return new Error(message);
		}
	}

	[DataContract()]
	public class Error<TRoot> : Error
		where TRoot : class
	{
		/// <summary>
		/// Creates a new error and rule
		/// </summary>
		/// <param name="message">Message describing the error</param>
		/// <param name="condition">Condition that when true, indicates this error applies to the specified object.</param>
		/// <param name="sets">The list of <see cref="ConditionTypeSet"/> instances the rule is associated with</param>
		public Error(string message, Predicate<TRoot> condition, params ConditionTypeSet[] sets)
			: this(null, message, condition, sets)
		{ }

		/// <summary>
		/// Creates a new error and rule
		/// </summary>
		/// <param name="code">Unique code for the condition type</param>
		/// <param name="message">Message describing the error</param>
		/// <param name="condition">Condition that when true, indicates this error applies to the specified object.</param>
		/// <summary>
		public Error(string code, string message, Predicate<TRoot> condition, params ConditionTypeSet[] sets)
			: base(code, message, sets)
		{
			CreateConditionRule<TRoot>(condition, null, null);
		}
	}
}
