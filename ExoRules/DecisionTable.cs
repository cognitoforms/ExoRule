using System;
using System.Collections.Generic;

namespace ExoRule
{
	#region DecisionTable<TDecision>

	/// <summary>
	/// Represents a set of time versioned decisions that evaluate one or more conditions
	/// to obtain one or more factors or amounts for use in rule calculations.
	/// </summary>
	/// <typeparam name="TDecision"></typeparam>
	public abstract class DecisionTable<TDecision>
		 where TDecision : class
	{
		static List<Version> versions = new List<Version>();

		/// <summary>
		/// Gets the set of decisions applicable for the specified date.
		/// </summary>
		/// <param name="asOfDate"></param>
		/// <returns></returns>
		protected static TDecision[] GetDecisions(DateTime asOfDate)
		{
			foreach (Version version in versions)
			{
				if (asOfDate >= version.EffectiveDate && asOfDate <= version.ExpirationDate)
					return version.Decisions;
			}
			return null;
		}

		/// <summary>
		/// Adds a new set of decisions for the specified time period.
		/// </summary>
		/// <param name="effectiveDate"></param>
		/// <param name="expirationDate"></param>
		/// <param name="decisions"></param>
		protected static void AddDecisions(DateTime effectiveDate, DateTime expirationDate, TDecision[] decisions)
		{
			versions.Add(new DecisionTable<TDecision>.Version(effectiveDate, expirationDate, decisions));
		}

		#region Version

		/// <summary>
		/// Represents a set of decisions for a decision table for a specific time period.
		/// </summary>
		class Version
		{
			public readonly DateTime EffectiveDate;
			public readonly DateTime ExpirationDate;
			public TDecision[] Decisions;

			/// <summary>
			/// Creates a new decision table version.
			/// </summary>
			/// <param name="effectiveDate"></param>
			/// <param name="expirationDate"></param>
			/// <param name="decisions"></param>
			public Version(DateTime effectiveDate, DateTime expirationDate, TDecision[] decisions)
			{
				this.EffectiveDate = effectiveDate;
				this.ExpirationDate = expirationDate;
				this.Decisions = decisions;
			}
		}

		#endregion
	}

	#endregion

	#region Condition<T>

	/// <summary>
	/// Represents a condition that compares a specified value to a comparison value.
	/// </summary>
	/// <typeparam name="T"></typeparam>
	public abstract class Condition<T>
		 where T : IComparable
	{
		/// <summary>
		/// Determines whether the specified value is valid for this condition.
		/// </summary>
		/// <param name="value"></param>
		/// <returns></returns>
		public abstract bool Evaluate(T value);

		/// <summary>
		/// Gets the set of values represented by the condition.
		/// </summary>
		public abstract T[] Values
		{
			get;
		}

		/// <summary>
		/// Automatically converts the condition to the underlying value to support
		/// easy retrieval of the value of the condition.
		/// </summary>
		/// <param name="c"></param>
		/// <returns></returns>
		public static implicit operator T(Condition<T> c)
		{
			if (c.Values.Length > 0)
				return c.Values[0];
			return default(T);
		}

		#region True

		public class True : Condition<T>
		{
			public override bool Evaluate(T value)
			{
				return true;
			}

			public override T[] Values
			{
				get
				{
					return new T[] { };
				}
			}
		}

		#endregion

		#region Composite

		public abstract class Composite : Condition<T>
		{
			Condition<T>[] conditions;
			T[] values;

			public Composite(params Condition<T>[] conditions)
			{
				this.conditions = conditions;
			}

			public Condition<T>[] Conditions
			{
				get
				{
					return conditions;
				}
			}

			public override T[] Values
			{
				get
				{
					List<T> values = new List<T>();
					foreach (Condition<T> condition in conditions)
						values.AddRange(condition.Values);
					return values.ToArray();
				}
			}
		}

		#endregion

		#region And

		public class And : Composite
		{
			public And(params Condition<T>[] conditions)
				: base(conditions)
			{ }

			public override bool Evaluate(T value)
			{
				foreach (Condition<T> condition in Conditions)
					if (!condition.Evaluate(value))
						return false;
				return true;
			}
		}

		#endregion

		#region Or

		public class Or : Composite
		{
			public Or(params Condition<T>[] conditions)
				: base(conditions)
			{ }

			public override bool Evaluate(T value)
			{
				foreach (Condition<T> condition in Conditions)
					if (condition.Evaluate(value))
						return true;
				return false;
			}
		}

		#endregion

		#region Relational

		public abstract class Relational : Condition<T>
		{
			T value;
			T[] values;

			protected Relational(T value)
			{
				this.value = value;
				this.values = new T[] { value };
			}

			public T Value
			{
				get
				{
					return value;
				}
			}

			public override T[] Values
			{
				get
				{
					return values;
				}
			}
		}

		#endregion

		#region Equal

		public class Equal : Relational
		{
			public Equal(T value)
				: base(value)
			{ }

			public override bool Evaluate(T value)
			{
				return ((IComparable)Value).CompareTo(value) == 0;
			}
		}

		#endregion

		#region NotEqual

		public class NotEqual : Relational
		{
			public NotEqual(T value)
				: base(value)
			{ }

			public override bool Evaluate(T value)
			{
				return ((IComparable)Value).CompareTo(value) != 0;
			}
		}

		#endregion

		#region LessThan

		public class LessThan : Relational
		{
			public LessThan(T value)
				: base(value)
			{ }

			public override bool Evaluate(T value)
			{
				return ((IComparable)Value).CompareTo(value) > 0;
			}
		}

		#endregion

		#region LessThanEqual

		public class LessThanEqual : Relational
		{
			public LessThanEqual(T value)
				: base(value)
			{ }

			public override bool Evaluate(T value)
			{
				return ((IComparable)Value).CompareTo(value) >= 0;
			}
		}

		#endregion

		#region GreaterThan

		public class GreaterThan : Relational
		{
			public GreaterThan(T value)
				: base(value)
			{ }

			public override bool Evaluate(T value)
			{
				return ((IComparable)Value).CompareTo(value) < 0;
			}
		}

		#endregion

		#region GreaterThanEqual

		public class GreaterThanEqual : Relational
		{
			public GreaterThanEqual(T value)
				: base(value)
			{ }

			public override bool Evaluate(T value)
			{
				return ((IComparable)Value).CompareTo(value) <= 0;
			}
		}

		#endregion
	}

	#endregion
}
