// Copyright (c) 2007-2013 SIL International
// This software is licensed under the LGPL, version 2.1 or later
// (http://www.gnu.org/licenses/lgpl-2.1.html)
//
// File: StyleInfoTests.cs
// Responsibility: TE Team
//
// <remarks>
// </remarks>
// ---------------------------------------------------------------------------------------------
using NUnit.Framework;
using SIL.LCModel.Core.KernelInterfaces;
using SIL.LCModel;
using SIL.FieldWorks.Common.ViewsInterfaces;
using SIL.LCModel.DomainServices;

namespace SIL.FieldWorks.FwCoreDlgControls
{
	/// ----------------------------------------------------------------------------------------
	/// <summary>
	///
	/// </summary>
	/// ----------------------------------------------------------------------------------------
	[TestFixture]
	public class StyleInfoTests : MemoryOnlyBackendProviderRestoredForEachTestTestBase
	{
		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Tests saving a new style info to the DB. In this case the context should be gotten
		/// from the based-on style.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		[Test]
		public void SaveToDB_NewInfo()
		{
			var styleFactory = Cache.ServiceLocator.GetInstance<IStStyleFactory>();
			var realStyle = styleFactory.Create();
			Cache.LanguageProject.StylesOC.Add(realStyle);
			realStyle.Context = ContextValues.Intro;
			realStyle.Function = FunctionValues.Table;
			realStyle.Structure = StructureValues.Heading;
			StyleInfo basedOn = new StyleInfo(realStyle);
			basedOn.UserLevel = 1;
			StyleInfo testInfo = new StyleInfo("New Style", basedOn,
				StyleType.kstParagraph, Cache);

			// simulate a save to the DB for the test style.
			var style = styleFactory.Create();
			Cache.LanguageProject.StylesOC.Add(style);
			testInfo.SaveToDB(style, false, false);

			Assert.AreEqual(ContextValues.Intro, testInfo.Context);
			Assert.AreEqual(StructureValues.Heading, testInfo.Structure);
			Assert.AreEqual(FunctionValues.Table, testInfo.Function);
			Assert.AreEqual(ContextValues.Intro, style.Context);
			Assert.AreEqual(StructureValues.Heading, style.Structure);
			Assert.AreEqual(FunctionValues.Table, style.Function);
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Tests saving a copy style info to the DB. In this case the context should be gotten
		/// from the based-on style.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		[Test]
		public void SaveToDB_CopyInfo()
		{
			StyleInfoTable styleTable = new StyleInfoTable("Normal", Cache.ServiceLocator.WritingSystemManager);
			var styleFactory = Cache.ServiceLocator.GetInstance<IStStyleFactory>();
			var normalStyle = styleFactory.Create();
			Cache.LanguageProject.StylesOC.Add(normalStyle);
			normalStyle.Name = "Normal";
			normalStyle.Context = ContextValues.Internal;
			normalStyle.Function = FunctionValues.Prose;
			normalStyle.Structure = StructureValues.Undefined;
			StyleInfo normal = new StyleInfo(normalStyle);
			styleTable.Add("Normal", normal);

			var realStyle = styleFactory.Create();
			Cache.LanguageProject.StylesOC.Add(realStyle);
			realStyle.Name = "Paragraph";
			realStyle.Context = ContextValues.Text;
			realStyle.Function = FunctionValues.Prose;
			realStyle.Structure = StructureValues.Body;
			realStyle.BasedOnRA = normalStyle;
			StyleInfo styleToCopyFrom = new StyleInfo(realStyle);
			styleTable.Add("Dictionary-Normal", styleToCopyFrom);

			StyleInfo testInfo = new StyleInfo(styleToCopyFrom, "Copy of Dictionary-Normal");
			styleTable.Add("Copy Dictionary-Normal", testInfo);
			styleTable.ConnectStyles();

			// simulate a save to the DB for the test style.
			var style = styleFactory.Create();
			Cache.LanguageProject.StylesOC.Add(style);
			testInfo.SaveToDB(style, false, false);

			Assert.AreEqual(ContextValues.Text, testInfo.Context);
			Assert.AreEqual(StructureValues.Body, testInfo.Structure);
			Assert.AreEqual(FunctionValues.Prose, testInfo.Function);
			Assert.AreEqual(ContextValues.Text, style.Context);
			Assert.AreEqual(StructureValues.Body, style.Structure);
			Assert.AreEqual(FunctionValues.Prose, style.Function);
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Tests saving a new style info to the DB. In this case the context should be gotten
		/// from the style of which the new style is a copy, rather than the based-on style.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		[Test]
		public void SaveToDB_CopyOfStyleBasedOnNormal()
		{
			StyleInfoTable styleTable = new StyleInfoTable("Normal", Cache.ServiceLocator.WritingSystemManager);
			var styleFactory = Cache.ServiceLocator.GetInstance<IStStyleFactory>();
			var normalStyle = styleFactory.Create();
			Cache.LanguageProject.StylesOC.Add(normalStyle);
			normalStyle.Name = "Normal";
			normalStyle.Context = ContextValues.Internal;
			normalStyle.Function = FunctionValues.Prose;
			normalStyle.Structure = StructureValues.Undefined;
			StyleInfo normal = new StyleInfo(normalStyle);
			styleTable.Add("Normal", normal);

			var realStyle = styleFactory.Create();
			Cache.LanguageProject.StylesOC.Add(realStyle);
			realStyle.Name = "Paragraph";
			realStyle.Context = ContextValues.Text;
			realStyle.Function = FunctionValues.Prose;
			realStyle.Structure = StructureValues.Body;
			realStyle.BasedOnRA = normalStyle;
			StyleInfo styleToCopyFrom = new StyleInfo(realStyle);
			styleTable.Add("Paragraph", styleToCopyFrom);

			StyleInfo testInfo = new StyleInfo(styleToCopyFrom, "Copy of Paragraph");
			styleTable.Add("Copy of Paragraph", testInfo);
			styleTable.ConnectStyles();

			// simulate a save to the DB for the test style.
			var style = styleFactory.Create();
			Cache.LanguageProject.StylesOC.Add(style);
			testInfo.SaveToDB(style, false, false);

			Assert.AreEqual(ContextValues.Text, testInfo.Context);
			Assert.AreEqual(StructureValues.Body, testInfo.Structure);
			Assert.AreEqual(FunctionValues.Prose, testInfo.Function);
			Assert.AreEqual(ContextValues.Text, style.Context);
			Assert.AreEqual(StructureValues.Body, style.Structure);
			Assert.AreEqual(FunctionValues.Prose, style.Function);
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Tests saving new style infos to the DB when one style is based on the other one.
		/// In this case the context should be gotten from the lowest level based-on style.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		[Test]
		public void SaveToDB_NewInfoAndBasedOnNewInfo()
		{
			var styleFactory = Cache.ServiceLocator.GetInstance<IStStyleFactory>();
			var realStyle = styleFactory.Create();
			Cache.LanguageProject.StylesOC.Add(realStyle);
			realStyle.Context = ContextValues.Intro;
			realStyle.Function = FunctionValues.Table;
			realStyle.Structure = StructureValues.Heading;
			StyleInfo basedOn = new StyleInfo(realStyle);
			basedOn.UserLevel = 1;
			StyleInfo testInfo1 = new StyleInfo("New Style 1", basedOn,
				StyleType.kstParagraph, Cache);
			StyleInfo testInfo2 = new StyleInfo("New Style 2", testInfo1,
				StyleType.kstParagraph, Cache);

			// simulate a save to the DB for the test styles. Save the second one first for
			// a better test.
			var style2 = styleFactory.Create();
			Cache.LanguageProject.StylesOC.Add(style2);
			testInfo2.SaveToDB(style2, false, false);
			var style = styleFactory.Create();
			Cache.LanguageProject.StylesOC.Add(style);
			testInfo1.SaveToDB(style, false, false);

			Assert.AreEqual(ContextValues.Intro, testInfo1.Context);
			Assert.AreEqual(StructureValues.Heading, testInfo1.Structure);
			Assert.AreEqual(FunctionValues.Table, testInfo1.Function);
			Assert.AreEqual(ContextValues.Intro, style.Context);
			Assert.AreEqual(StructureValues.Heading, style.Structure);
			Assert.AreEqual(FunctionValues.Table, style.Function);

			Assert.AreEqual(ContextValues.Intro, testInfo2.Context);
			Assert.AreEqual(StructureValues.Heading, testInfo2.Structure);
			Assert.AreEqual(FunctionValues.Table, testInfo2.Function);
			Assert.AreEqual(ContextValues.Intro, style2.Context);
			Assert.AreEqual(StructureValues.Heading, style2.Structure);
			Assert.AreEqual(FunctionValues.Table, style2.Function);
		}
	}
}
