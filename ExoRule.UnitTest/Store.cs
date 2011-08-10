using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using ExoGraph.UnitTest;

namespace ExoRule.UnitTest
{
	internal class Store : TestEntity
	{
		public ICollection<Product> Products
		{
			get { return Get(() => Products); }
			set { Set(() => Products, value); }
		}

		public ICollection<Milk> Milks
		{
			get { return Get(() => Milks); }
			set { Set(() => Milks, value); }
		}

		public decimal MilkGallons
		{
			get { return Get(() => MilkGallons); }
			set { Set(() => MilkGallons, value); }
		}

		public decimal BeerGallons
		{
			get { return Get(() => BeerGallons); }
			set { Set(() => BeerGallons, value); }
		}

		public decimal MilkFat
		{
			get { return Get(() => MilkFat); }
			set { Set(() => MilkFat, value); }
		}

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
