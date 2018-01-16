using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace ExoRule
{
	/// <summary>
	/// Identifies the set of events that can cause a rule to run.
	/// </summary>
	[Flags]
	public enum RuleInvocationType
	{
		/// <summary>
		/// Occurs when an existing instance is initialized.
		/// </summary>
		InitExisting = 2,

		/// <summary>
		/// Occurs when a new instance is initialized.
		/// </summary>
		InitNew = 4,

		/// <summary>
		/// Occurs when a property value is retrieved.
		/// </summary>
		PropertyGet = 8,

		/// <summary>
		/// Occurs when a property value is changed.
		/// </summary>
		PropertyChanged = 16,

		/// <summary>
		/// Occurs when validation is explicitly performed on an existing instance.
		/// </summary>
		ValidateExisting = 32,

		/// <summary>
		/// Occurs when validation is explicitly performed on a new instance.
		/// </summary>
		ValidateNew = 64,

		/// <summary>
		/// Overrides registration of PropertyChanged events for PropertyGet rules
		/// </summary>
		SuppressPropertyChanged = 128
	}
}
