using System.Collections.Generic;
using System.Linq;
using ExoModel.Json;

namespace ExoRule.UnitTests.Models.Store
{
	internal class Store : JsonEntity
	{
		public ICollection<Product> Products { get; set; }

		public ICollection<Milk> Milks { get; set; }

		public decimal MilkGallons { get; set; }

		public decimal BeerGallons { get; set; }

		public decimal MilkFat { get; set; }

		/// <summary>
		/// Calculates the amount of fat represented by the current milk instance.
		/// </summary>
		static Rule CalculateMilkFat = new Rule<Store>(
			s =>
			{
				s.MilkFat = s.Milks.Sum(m => m.Fat);
			})
			.OnChangeOf("Milks.Fat")
			.Returns(s => s.MilkFat);

		/// <summary>
		/// Calculates the gallons of beer for the current store.
		/// </summary>
		static Rule CalculateBeerGallons = new Rule<Store>(
			s =>
			{
				s.BeerGallons = s.Products.OfType<Beer>().Sum(b => b.Gallons);
			})
			.OnChangeOf("Products<Beer>.Gallons")
			.Returns(s => s.BeerGallons);

	}
}
