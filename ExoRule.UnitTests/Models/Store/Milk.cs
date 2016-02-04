namespace ExoRule.UnitTests.Models.Store
{
	internal class Milk : Product
	{
		public decimal Percent { get; set; }

		public decimal Gallons { get; set; }

		public decimal Fat { get; set; }

		/// <summary>
		/// Calculates the amount of fat represented by the current milk instance.
		/// </summary>
		static Rule CalculateFat = Rule<Milk>.Calculate(m => m.Fat, m => m.Gallons * m.Percent);
	}
}
