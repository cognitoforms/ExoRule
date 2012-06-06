using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Runtime.Serialization;
using System.Linq.Expressions;

namespace ExoRule
{
	public class Error : ConditionType
	{
		public Error(string message, params ConditionTypeSet[] sets)
			: this(null, message, sets)
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

	public class Error<TRoot> : Error
		where TRoot : class
	{
		/// <summary>
		/// Creates a new error and rule
		/// </summary>
		/// <param name="message">Message describing the error</param>
		/// <param name="condition">Condition that when true, indicates this error applies to the specified object.</param>
		/// <param name="sets">The list of <see cref="ConditionTypeSet"/> instances the rule is associated with</param>
		public Error(string message, Expression<Func<TRoot, bool>> condition, params ConditionTypeSet[] sets)
			: this(null, message, condition, sets)
		{ }

		/// <summary>
		/// Creates a new error and rule
		/// </summary>
		/// <param name="code">Unique code for the condition type</param>
		/// <param name="message">Message describing the error</param>
		/// <param name="condition">Condition that when true, indicates this error applies to the specified object.</param>
		/// <summary>
		public Error(string code, string message, Expression<Func<TRoot, bool>> condition, params ConditionTypeSet[] sets)
			: base(code, message, sets)
		{
			CreateConditionRule<TRoot>(condition);
		}

		/// <summary>
		/// Creates a new error and rule
		/// </summary>
		/// <param name="message">Message describing the error</param>
		/// <param name="condition">Condition that when true, indicates this error applies to the specified object.</param>
		/// <param name="properties">The set of properties to attach the condition to</param>
		/// <param name="sets">The list of <see cref="ConditionTypeSet"/> instances the rule is associated with</param>
		public Error(string message, Expression<Func<TRoot, bool>> condition, string properties, params ConditionTypeSet[] sets)
			: this(null, message, condition, properties, sets)
		{ }

		/// <summary>
		/// Creates a new error and rule
		/// </summary>
		/// <param name="code">Unique code for the condition type</param>
		/// <param name="message">Message describing the error</param>
		/// <param name="condition">Condition that when true, indicates this error applies to the specified object.</param>
		/// <param name="properties">The set of properties to attach the condition to</param>
		/// <param name="sets">The list of <see cref="ConditionTypeSet"/> instances the rule is associated with</param>
		public Error(string code, string message, Expression<Func<TRoot, bool>> condition, string properties, params ConditionTypeSet[] sets)
			: base(code, message, sets)
		{
			CreateConditionRule<TRoot>(condition, properties);
		}

		public Error<TRoot> OnInit()
		{
			((Rule<TRoot>)ConditionRule).OnInit();
			return this;
		}

		public Error<TRoot> OnInitNew()
		{
			((Rule<TRoot>)ConditionRule).OnInitNew();
			return this;
		}

		public Error<TRoot> OnInitExisting()
		{
			((Rule<TRoot>)ConditionRule).OnInitExisting();
			return this;
		}

	}
}
