﻿// Copyright (c) 2007-2017 SIL International
// This software is licensed under the LGPL, version 2.1 or later
// (http://www.gnu.org/licenses/lgpl-2.1.html)

using NUnit.Framework;
using SIL.LCModel.Core.Text;
using SIL.LCModel.Core.WritingSystems;
using SIL.LCModel.Core.KernelInterfaces;

namespace SIL.FieldWorks.Common.RootSites.SimpleRootSiteTests
{
	/// ----------------------------------------------------------------------------------------
	/// <summary>
	/// Other tests for SimpleRootSite
	/// </summary>
	/// ----------------------------------------------------------------------------------------
	[TestFixture]
	public class TsStringWrapperTests
	{
		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Creates a TsString from the given string, and round trips it
		/// with a TsStringWrapper, and compares the result of the initial TsString to the
		/// TsString returned from TsStringWrapper.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		[Test]
		[TestCase("simple text.", "Title", null, null, TestName="Simple1")]
		[TestCase("simple text1", "Title", "simple text2", "Conclusion", TestName="Simple2")]
		// Test some text with paragraph breaks in it. (Note: we intentionally do not use
		// Environment.Newline for the paragraph break because a simple newline character is
		// sufficient and the TSStringSerializer doesn't preserve the \r anyway.)
		[TestCase("simple text1", "Title", "simple text2\nsimple text3", "Conclusion", TestName="WithParaBreaks")]
		[TestCase("得到", "Title", null, null, TestName="WithNonRoman")]
		public void TestTsStringWrapperRoundTrip(string str1, string namedStyle1, string str2, string namedStyle2)
		{
			var wsFact = new WritingSystemManager();
			ITsStrBldr bldr = TsStringUtils.MakeStrBldr();
			ITsPropsBldr ttpBldr = TsStringUtils.MakePropsBldr();
			wsFact.get_Engine("en");
			ttpBldr.SetIntPropValues((int)FwTextPropType.ktptWs, (int)FwTextPropVar.ktpvDefault, wsFact.GetWsFromStr("en"));
			ttpBldr.SetStrPropValue((int)FwTextPropType.ktptNamedStyle, namedStyle1);
			bldr.Replace(bldr.Length, bldr.Length, str1, ttpBldr.GetTextProps());
			if (namedStyle2 != null && str2 != null)
			{
				ttpBldr.SetStrPropValue((int)FwTextPropType.ktptNamedStyle, namedStyle2);
				bldr.Replace(bldr.Length, bldr.Length, str2, ttpBldr.GetTextProps());
			}
			var tsString1 = bldr.GetString();

			var strWrapper = new TsStringWrapper(tsString1, wsFact);

			var tsString2 = strWrapper.GetTsString(wsFact);

			Assert.AreEqual(tsString1.Text, tsString2.Text);
		}
	}
}
