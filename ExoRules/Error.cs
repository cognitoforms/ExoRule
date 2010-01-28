using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace ExoRule
{
	public class Error : ConditionType
	{
		public Error(string defaultMessage)
			: this(null, defaultMessage)
		{ }

		public Error(string code, string defaultMessage)
			: base(code, ConditionCategory.Error, defaultMessage)
		{ }

		public static implicit operator Error(string defaultMessage)
		{
			return new Error(defaultMessage);
		}
	}

	public class Error<TRoot> : Error
		where TRoot : class
	{
		public Error(string defaultMessage, Predicate<TRoot> condition)
			: this(null, defaultMessage, condition)
		{ }

		public Error(string code, string defaultMessage, Predicate<TRoot> condition)
			: base(code, defaultMessage)
		{
			CreateConditionRule<TRoot>(condition, null, null);
		}
	}
}
