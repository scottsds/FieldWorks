// Copyright (c) 2004-2017 SIL International
// This software is licensed under the LGPL, version 2.1 or later
// (http://www.gnu.org/licenses/lgpl-2.1.html)

using System;
using System.Collections.Generic;
using System.Diagnostics;
using NUnit.Framework;
using SIL.LCModel.Core.Text;
using SIL.LCModel.Core.WritingSystems;
using SIL.LCModel.Core.KernelInterfaces;
using SIL.FieldWorks.Common.FwUtils;
using SIL.LCModel.Utils;

namespace SIL.FieldWorks.Common.Widgets
{

	/// ----------------------------------------------------------------------------------------
	/// <summary>
	/// Tests for FontHeightAdjuster.
	/// </summary>
	/// ----------------------------------------------------------------------------------------
	[TestFixture]
	public class FontHeightAdjusterTests
	{

		#region Data Members
		TestFwStylesheet m_stylesheet;
		WritingSystemManager m_wsManager;
		int m_hvoGermanWs;
		int m_hvoEnglishWs;
		#endregion

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Set up some dummy styles for testing purposes
		/// </summary>
		/// ------------------------------------------------------------------------------------
		[TestFixtureSetUp]
		public void FixtureSetup()
		{
			m_stylesheet = new TestFwStylesheet();
			m_wsManager = new WritingSystemManager();

			// English
			CoreWritingSystemDefinition enWs;
			m_wsManager.GetOrSet("en", out enWs);
			m_hvoEnglishWs = enWs.Handle;
			Assert.IsTrue(m_hvoEnglishWs > 0, "Should have gotten an hvo for the English WS");
			// German
			CoreWritingSystemDefinition deWs;
			m_wsManager.GetOrSet("de", out deWs);
			m_hvoGermanWs = deWs.Handle;
			Assert.IsTrue(m_hvoGermanWs > 0, "Should have gotten an hvo for the German WS");
			Assert.IsTrue(m_hvoEnglishWs != m_hvoGermanWs, "Writing systems should have different IDs");

			// Create a couple of styles
			int hvoStyle = m_stylesheet.MakeNewStyle();
			ITsPropsBldr propsBldr = TsStringUtils.MakePropsBldr();
			propsBldr.SetStrPropValue((int)FwTextStringProp.kstpFontFamily, "Arial");
			m_stylesheet.PutStyle("StyleA", "bla", hvoStyle, 0, hvoStyle, 1, false, false,
				propsBldr.GetTextProps());

			hvoStyle = m_stylesheet.MakeNewStyle();
			propsBldr.SetStrPropValue((int)FwTextStringProp.kstpFontFamily, "Times New Roman");
			m_stylesheet.PutStyle("StyleB", "bla", hvoStyle, 0, hvoStyle, 1, false, false,
				propsBldr.GetTextProps());

			// Override the font size for each writing system and each style.
			List<FontOverride> fontOverrides = new List<FontOverride>(2);
			FontOverride fo;
			fo.writingSystem = m_hvoEnglishWs;
			fo.fontSize = 21;
			fontOverrides.Add(fo);
			fo.writingSystem = m_hvoGermanWs;
			fo.fontSize = 13;
			fontOverrides.Add(fo);
			m_stylesheet.OverrideFontsForWritingSystems("StyleA", fontOverrides);

			fontOverrides.Clear();
			fo.writingSystem = m_hvoEnglishWs;
			fo.fontSize = 20;
			fontOverrides.Add(fo);
			fo.writingSystem = m_hvoGermanWs;
			fo.fontSize = 56;
			fontOverrides.Add(fo);
			m_stylesheet.OverrideFontsForWritingSystems("StyleB", fontOverrides);
		}

		/// -------------------------------------------------------------------------------------
		/// <summary>
		/// Test the GetFontHeightForStyle method.
		/// </summary>
		/// -------------------------------------------------------------------------------------
		[Test]
		public void TestGetFontHeightForStyle()
		{
			Assert.AreEqual(13000, FontHeightAdjuster.GetFontHeightForStyle("StyleA",
				m_stylesheet, m_hvoGermanWs, m_wsManager));
			Assert.AreEqual(21000, FontHeightAdjuster.GetFontHeightForStyle("StyleA",
				m_stylesheet, m_hvoEnglishWs, m_wsManager));
			Assert.AreEqual(56000, FontHeightAdjuster.GetFontHeightForStyle("StyleB",
				m_stylesheet, m_hvoGermanWs, m_wsManager));
			Assert.AreEqual(20000, FontHeightAdjuster.GetFontHeightForStyle("StyleB",
				m_stylesheet, m_hvoEnglishWs, m_wsManager));
		}

		private static int GetUbuntuVersion()
		{
			if (!MiscUtils.IsUnix)
				return 0;

			try
			{
				var startInfo = new ProcessStartInfo {
					RedirectStandardOutput = true,
					UseShellExecute = false,
					FileName = "lsb_release",
					Arguments = "-r -s"
				};
				using (var proc = Process.Start(startInfo))
				{
					var value = proc.StandardOutput.ReadToEnd().TrimEnd();
					proc.WaitForExit();
					if (value.Contains("."))
						return int.Parse(value.Split('.')[0]);
				}
			}
			catch (Exception)
			{
				// Just ignore and continue with the default
			}
			return -1;
		}

		private const int ExpectedFontHeightForArial = 20750;

		/// -------------------------------------------------------------------------------------
		/// <summary>
		/// Test the GetAdjustedTsString method.
		/// </summary>
		/// -------------------------------------------------------------------------------------
		[Test]
		public void TestGetAdjustedTsString()
		{
			ITsStrBldr strBldr = TsStringUtils.MakeStrBldr();
			ITsStrBldr strBldrExpected = TsStringUtils.MakeStrBldr();
			ITsPropsBldr propsBldr = TsStringUtils.MakePropsBldr();

			propsBldr.SetStrPropValue((int)FwTextPropType.ktptNamedStyle, "StyleA");
			propsBldr.SetIntPropValues((int)FwTextPropType.ktptWs, 0, m_hvoGermanWs);
			strBldr.ReplaceRgch(0, 0, "Hello People", 12, propsBldr.GetTextProps());
			strBldrExpected.ReplaceRgch(0, 0, "Hello People", 12, propsBldr.GetTextProps());

			propsBldr.SetStrPropValue((int)FwTextPropType.ktptNamedStyle, "StyleA");
			propsBldr.SetIntPropValues((int)FwTextPropType.ktptWs, 0, m_hvoEnglishWs);
			strBldr.ReplaceRgch(0, 0, "Hello numero dos", 16, propsBldr.GetTextProps());
			var propsBldrExpected = propsBldr;
			propsBldrExpected.SetIntPropValues((int)FwTextPropType.ktptFontSize,
				(int)FwTextPropVar.ktpvMilliPoint, 20500);
			strBldrExpected.ReplaceRgch(0, 0, "Hello numero dos", 16, propsBldrExpected.GetTextProps());

			propsBldr.SetStrPropValue((int)FwTextPropType.ktptNamedStyle, "StyleB");
			propsBldr.SetIntPropValues((int)FwTextPropType.ktptWs, 0, m_hvoGermanWs);
			strBldr.ReplaceRgch(0, 0, "3 Hello", 7, propsBldr.GetTextProps());
			propsBldrExpected = propsBldr;
			propsBldrExpected.SetIntPropValues((int)FwTextPropType.ktptFontSize,
				(int)FwTextPropVar.ktpvMilliPoint, ExpectedFontHeightForArial);
			strBldrExpected.ReplaceRgch(0, 0, "3 Hello", 7, propsBldrExpected.GetTextProps());

			propsBldr.SetStrPropValue((int)FwTextPropType.ktptNamedStyle, "StyleB");
			propsBldr.SetIntPropValues((int)FwTextPropType.ktptWs, 0, m_hvoEnglishWs);
			strBldr.ReplaceRgch(0, 0, "This is 4", 9, propsBldr.GetTextProps());
			strBldrExpected.ReplaceRgch(0, 0, "This is 4", 9, propsBldr.GetTextProps());

			var tss = FontHeightAdjuster.GetAdjustedTsString(strBldr.GetString(), 23000, m_stylesheet, m_wsManager);
			var propsWithWiggleRoom = new Dictionary<int, int>();
			if (GetUbuntuVersion() == 14)
				propsWithWiggleRoom[(int)FwTextPropType.ktptFontSize] = 1000; // millipoints. for some reason, the result is sometimes a point smaller on Trusty
			AssertEx.AreTsStringsEqual(strBldrExpected.GetString(), tss, propsWithWiggleRoom);
		}
	}
}
