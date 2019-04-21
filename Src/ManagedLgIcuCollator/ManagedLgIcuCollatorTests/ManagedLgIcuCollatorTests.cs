// Copyright (c) 2015 SIL International
// This software is licensed under the LGPL, version 2.1 or later
// (http://www.gnu.org/licenses/lgpl-2.1.html)

using NUnit.Framework;
using System;
using SIL.FieldWorks.Common.ViewsInterfaces;

namespace SIL.FieldWorks.Language
{
	[TestFixture()]
	public class ManagedLgIcuCollatorTests
	{
		protected ManagedLgIcuCollator ManagedLgIcuCollatorInitializerHelper()
		{
			var icuCollator = new ManagedLgIcuCollator();
			icuCollator.Open("en");
			return icuCollator;
		}

		[Test()]
		[Category("ByHand")]
		public void OpenTest()
		{
			using (var icuCollator = ManagedLgIcuCollatorInitializerHelper())
				Assert.IsNotNull(icuCollator);
		}

		[Test()]
		[Category("ByHand")]
		public void GeneralTestWithTwoDifferentWs()
		{
			using (var icuCollator1 = new ManagedLgIcuCollator())
			{
				icuCollator1.Open("en");
				var options = new LgCollatingOptions();
				object result1 = icuCollator1.get_SortKeyVariant("test", options);
				using (var icuCollator2 = new ManagedLgIcuCollator())
				{
					icuCollator2.Open("fr");
					object result2 = icuCollator2.get_SortKeyVariant("test", options);
				}
			}
		}

		[Test()]
		[Category("ByHand")]
		public void CloseTest()
		{
			using (var icuCollator = ManagedLgIcuCollatorInitializerHelper())
			{
				Assert.IsNotNull(icuCollator);

				icuCollator.Close();
			}
		}

		[Test()]
		[Category("ByHand")]
		[ExpectedException(typeof(NotImplementedException))]
		public void GetSortKeyTest()
		{
			using (var icuCollator = ManagedLgIcuCollatorInitializerHelper())
			{
				Assert.IsNotNull(icuCollator);

				var options = new LgCollatingOptions();

				string result = icuCollator.get_SortKey("abc", options);
				Assert.IsNotEmpty(result);

				icuCollator.Close();
			}
		}

		[Test()]
		[Category("ByHand")]
		public void GetSortKeyVariantTest()
		{
			using (var icuCollator= ManagedLgIcuCollatorInitializerHelper())
			{
				Assert.IsNotNull(icuCollator);

				var options = new LgCollatingOptions();

				object obj = icuCollator.get_SortKeyVariant("abc", options);
				Assert.IsNotNull(obj);

				icuCollator.Close();
			}
		}

		[Test()]
		[Category("ByHand")]
		[ExpectedException(typeof(NotImplementedException))]
		public void GetSortKeyRgchTest()
		{
			using (var icuCollator = ManagedLgIcuCollatorInitializerHelper())
			{
				Assert.IsNotNull(icuCollator);

				int cchOut;
				icuCollator.SortKeyRgch(null, 0, new LgCollatingOptions(), 0, null, out cchOut);
			}
		}

		[Test()]
		[Category("ByHand")]
		public void SortKeyVariantTestWithValues()
		{
			using (var icuCollator = ManagedLgIcuCollatorInitializerHelper())
			{
				Assert.IsNotNull(icuCollator);

				var options = new LgCollatingOptions();

				object obj1 = icuCollator.get_SortKeyVariant("action", options);

				Assert.That((obj1 as byte[])[0], Is.EqualTo(41));
				Assert.That((obj1 as byte[])[1], Is.EqualTo(45));
				Assert.That((obj1 as byte[])[2], Is.EqualTo(79));
				Assert.That((obj1 as byte[])[3], Is.EqualTo(57));
				Assert.That((obj1 as byte[])[4], Is.EqualTo(69));
				Assert.That((obj1 as byte[])[5], Is.EqualTo(67));
				Assert.That((obj1 as byte[])[6], Is.EqualTo(1));
				Assert.That((obj1 as byte[])[7], Is.EqualTo(10));
				Assert.That((obj1 as byte[])[8], Is.EqualTo(1));
				Assert.That((obj1 as byte[])[9], Is.EqualTo(10));
				Assert.That((obj1 as byte[])[10], Is.EqualTo(0));
			}
		}


		[Test()]
		[Category("ByHand")]
		public void CompareVariantTest1()
		{
			using (var icuCollator = ManagedLgIcuCollatorInitializerHelper())
			{
				Assert.IsNotNull(icuCollator);

				var options = new LgCollatingOptions();

				object obj1 = icuCollator.get_SortKeyVariant("abc", options);
				object obj2 = obj1;
				object obj3 = icuCollator.get_SortKeyVariant("def", options);

				Assert.IsTrue(icuCollator.CompareVariant(obj1, obj2, options) == 0, " obj1 == obj2");
				Assert.IsTrue(icuCollator.CompareVariant(obj1, obj3, options) != 0, " obj1 != obj3");

				icuCollator.Close();
			}
		}

		[Test()]
		[Category("ByHand")]
		public void CompareVariantTest2()
		{
			using (var icuCollator = ManagedLgIcuCollatorInitializerHelper())
			{
				Assert.IsNotNull(icuCollator);

				var options = new LgCollatingOptions();

				object obj1 = icuCollator.get_SortKeyVariant("action", options);
				object obj2 = icuCollator.get_SortKeyVariant("actiom", options);

				Assert.IsTrue(icuCollator.CompareVariant(obj1, obj2, options) != 0, " action != actionm");

				obj1 = icuCollator.get_SortKeyVariant("tenepa", options);
				obj2 = icuCollator.get_SortKeyVariant("tenepo", options);
				Assert.IsTrue(icuCollator.CompareVariant(obj1, obj2, options) != 0, " tenepa != tenepo");

				obj1 = icuCollator.get_SortKeyVariant("hello", options);
				obj2 = icuCollator.get_SortKeyVariant("hello", options);

				Assert.IsTrue(icuCollator.CompareVariant(obj1, obj2, options) == 0, " hello == hello");


				obj1 = icuCollator.get_SortKeyVariant("tenepaa", options);
				obj2 = icuCollator.get_SortKeyVariant("tenepa", options);

				Assert.IsTrue(icuCollator.CompareVariant(obj1, obj2, options) > 0, " tenepaa > tenepa");

				obj1 = icuCollator.get_SortKeyVariant("tenepa", options);
				obj2 = icuCollator.get_SortKeyVariant("tenepaa", options);

				Assert.IsTrue(icuCollator.CompareVariant(obj1, obj2, options) < 0, " tenepaa < tenepa");

				icuCollator.Close();
			}
		}

		[Test()]
		[Category("ByHand")]
		public void CompareTest()
		{
			using (var icuCollator = ManagedLgIcuCollatorInitializerHelper())
			{
				Assert.IsNotNull(icuCollator);

				var options = new LgCollatingOptions();

				Assert.IsTrue(icuCollator.Compare(string.Empty, String.Empty, options) == 0);
				Assert.IsTrue(icuCollator.Compare("abc", "abc", options) == 0);
				Assert.IsTrue(icuCollator.Compare("abc", "def", options) != 0);
			}
		}
	}
}
