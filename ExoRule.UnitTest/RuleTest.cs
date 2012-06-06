using System;
using System.Text;
using System.Collections.Generic;
using System.Linq;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using ExoModel;
using System.Reflection;
using System.Linq.Expressions;
using System.Collections.ObjectModel;
using ExoModel.UnitTest;

namespace ExoRule.UnitTest
{
	[TestClass]
	public class RuleTest
	{
		[ClassInitialize]
		public static void Initialize(TestContext options)
		{
			// Initialize the model context to use the test model type provider
			ModelContext.Init(
				() => Rule.RegisterRules(Assembly.GetExecutingAssembly()),
				new TestModelTypeProvider(Assembly.GetExecutingAssembly()));
		}

		[TestMethod]
		public void TestCalculateFat()
		{
			var milk = new Milk() { Gallons = 0.5m, Percent = 0.02m };

			Assert.AreEqual(0.01m, milk.Fat);
		}

		[TestMethod]
		public void TestCalculateFatChange()
		{
			var milk = new Milk() { Gallons = 0.5m, Percent = 0.02m };

			Assert.AreEqual(0.01m, milk.Fat);
			milk.Gallons = 1;
			Assert.AreEqual(0.02m, milk.Fat);
		}

		[TestMethod]
		public void TestCalculateMilkFat()
		{
			var store = new Store()
				{
					Milks = new ObservableCollection<Milk>()
					{
						new Milk() { Gallons = 0.5m, Percent = 0.02m },
						new Milk() { Gallons = 1m, Percent = 0.005m }
					}
				};

			Assert.AreEqual(0.015m, store.MilkFat);
		}

		[TestMethod]
		public void TestSubTypeFilter()
		{
			var store = new Store()
			{
				Products = new ObservableCollection<Product>()
				{
					new Beer() { Gallons = 0.5m },
					new Beer() { Gallons = 0.5m },
					new Milk() { Gallons = 0.5m, Percent = 0.02m },
					new Milk() { Gallons = 1m, Percent = 0.005m }
				}
			};

			// Verify that the total amount of beer is correctly calculated as 1
			Assert.AreEqual(1m, store.BeerGallons);

			// Ensure that milk is ignored when counting the total number of instances in the specified filtered graph
			Assert.AreEqual(3, ModelContext.Current.GetModelType("Store").GetPath("Products<Beer>").GetInstances(ModelContext.Current.GetModelInstance(store)).Count);
		}

		[TestMethod]
		public void TestCalculateMilkFatChange()
		{
			Console.WriteLine("Start Construct Test Instances");
			var store = new Store()
			{
				Milks = new List<Milk>()
					{
						new Milk() { Gallons = 0.5m, Percent = 0.02m },
						new Milk() { Gallons = 1m, Percent = 0.005m }
					}
			};
			Console.WriteLine("Finish Construct Test Instances");

			Console.WriteLine("Start First Assert");
			Assert.AreEqual(0.015m, store.MilkFat);
			Console.WriteLine("Finish First Assert");

			Console.WriteLine("Start Property Change");
			store.Milks.First().Gallons = 1;
			Console.WriteLine("Finish Property Change");

			Console.WriteLine("Start Second Assert");
			Assert.AreEqual(0.025m, store.MilkFat);
			Console.WriteLine("Finish Second Assert");
		}

		[TestMethod]
		public void TestGetPath()
		{
			ModelType storeType = ModelContext.Current.GetModelType<Store>();

			var path = storeType.GetPath("{Products{Fat,Gallons},MilkFat,Milks{Percent,Brand,Gallons,Fat}}");
		}

		[TestMethod]
		public void TestClone()
		{
			var store = new Store()
			{
				Milks = new List<Milk>()
					{
						new Milk() { Gallons = 0.5m, Percent = 0.02m },
						new Milk() { Gallons = 1m, Percent = 0.005m }
					}
			};

			var store2 = (Store)ModelContext.Current.GetModelInstance(store).Clone("Milks").Invoke().Instance;
			store2.Milks.First().Gallons = 10m;

			Assert.AreEqual(0.015m, store.MilkFat);
			Assert.AreEqual(0.205m, store2.MilkFat);
		}

		[TestMethod]
		public void TestPaths()
		{
			Foo<Store>(s => s.Milks.Select(m => m.Fat));
		}

		public void Foo<T>(Expression<Func<T, object>> expr)
		{
			MemberExpression me;
			switch (expr.Body.NodeType)
			{
				case ExpressionType.Convert:
				case ExpressionType.ConvertChecked:
					var ue = expr.Body as UnaryExpression;
					me = ((ue != null) ? ue.Operand : null) as MemberExpression;
					break;
				default:
					me = expr.Body as MemberExpression;
					break;
			}

			while (me != null)
			{
				string propertyName = me.Member.Name;
				Type propertyType = me.Type;

				Console.WriteLine(propertyName + ": " + propertyType);

				me = me.Expression as MemberExpression;
			}
		}

	}
}
