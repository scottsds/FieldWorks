﻿// Copyright (c) 2014-2016 SIL International
// This software is licensed under the LGPL, version 2.1 or later
// (http://www.gnu.org/licenses/lgpl-2.1.html)

using System.Collections.Generic;
using NUnit.Framework;

namespace SIL.FieldWorks.XWorks
{
	[TestFixture]
	class DictionaryNodeOptionsTests
	{
		[Test]
		public void CanDeepCloneSenseOptions()
		{
			var orig = new DictionaryNodeSenseOptions
			{
				BeforeNumber = "BeforeNumber",
				NumberingStyle = "%O",
				AfterNumber = "AfterNumber",
				NumberStyle = "Dictionary-SenseNumber",
				NumberEvenASingleSense = true,
				ShowSharedGrammarInfoFirst = true,
				DisplayEachSenseInAParagraph = true,
				DisplayFirstSenseInline = true
			};

			// SUT
			var genericClone = orig.DeepClone();

			var clone = genericClone as DictionaryNodeSenseOptions;
			Assert.NotNull(clone, "Incorrect subclass returned; expected DictionaryNodeSenseOptions");
			Assert.AreNotSame(orig, clone, "Not deep cloned; shallow cloned");
			Assert.AreEqual(orig.BeforeNumber, clone.BeforeNumber);
			Assert.AreEqual(orig.NumberingStyle, clone.NumberingStyle);
			Assert.AreEqual(orig.AfterNumber, clone.AfterNumber);
			Assert.AreEqual(orig.NumberStyle, clone.NumberStyle);
			Assert.AreEqual(orig.NumberEvenASingleSense, clone.NumberEvenASingleSense);
			Assert.AreEqual(orig.ShowSharedGrammarInfoFirst, clone.ShowSharedGrammarInfoFirst);
			Assert.AreEqual(orig.DisplayEachSenseInAParagraph, clone.DisplayEachSenseInAParagraph);
			Assert.AreEqual(orig.DisplayFirstSenseInline, clone.DisplayFirstSenseInline);
		}

		[Test]
		public void CanDeepCloneListOptions()
		{
			var orig = new DictionaryNodeListOptions
			{
				ListId = DictionaryNodeListOptions.ListIds.Sense,
				Options = new List<DictionaryNodeListOptions.DictionaryNodeOption>
				{
					new DictionaryNodeListOptions.DictionaryNodeOption { Id = "Optn1", IsEnabled = true },
					new DictionaryNodeListOptions.DictionaryNodeOption { Id = "Optn2", IsEnabled = false },
					new DictionaryNodeListOptions.DictionaryNodeOption { Id = "Optn3", IsEnabled = true },
				}
			};

			// SUT
			var genericClone = orig.DeepClone();

			var clone = genericClone as DictionaryNodeListOptions;
			Assert.NotNull(clone, "Incorrect subclass returned; expected DictionaryNodeListOptions");
			Assert.Null(clone as DictionaryNodeListAndParaOptions, "Incorrect subclass returned; did not expect DictionaryNodeListAndParaOptions");
			Assert.AreNotSame(orig, clone, "Not deep cloned; shallow cloned");
			Assert.AreEqual(orig.ListId, clone.ListId);
			AssertListWasDeepCloned(orig.Options, clone.Options);
		}

		[Test]
		public void CanDeepCloneComplexFormOptions()
		{
			var orig = new DictionaryNodeListAndParaOptions
			{
				ListId = DictionaryNodeListOptions.ListIds.Minor,
				Options = new List<DictionaryNodeListOptions.DictionaryNodeOption>
				{
					new DictionaryNodeListOptions.DictionaryNodeOption { Id = "Optn1", IsEnabled = true },
					new DictionaryNodeListOptions.DictionaryNodeOption { Id = "Optn2", IsEnabled = false },
					new DictionaryNodeListOptions.DictionaryNodeOption { Id = "Optn3", IsEnabled = true },
				},
				DisplayEachInAParagraph = true
			};

			// SUT
			var genericClone = orig.DeepClone();

			var clone = genericClone as DictionaryNodeListAndParaOptions;
			Assert.NotNull(clone, "Incorrect subclass returned; expected DictionaryNodeListAndParaOptions");
			Assert.AreNotSame(orig, clone, "Not deep cloned; shallow cloned");
			Assert.AreEqual(orig.ListId, clone.ListId);
			Assert.AreEqual(orig.DisplayEachInAParagraph, clone.DisplayEachInAParagraph);
			AssertListWasDeepCloned(orig.Options, clone.Options);
		}

		[Test]
		public void CanDeepCloneWritingSystemOptions()
		{
			var orig = new DictionaryNodeWritingSystemOptions
			{
				WsType = DictionaryNodeWritingSystemOptions.WritingSystemType.Vernacular,
				Options = new List<DictionaryNodeListOptions.DictionaryNodeOption>
				{
					new DictionaryNodeListOptions.DictionaryNodeOption { Id = "ws1", IsEnabled = true },
					new DictionaryNodeListOptions.DictionaryNodeOption { Id = "ws2", IsEnabled = false },
					new DictionaryNodeListOptions.DictionaryNodeOption { Id = "ws3", IsEnabled = true },
				},
				DisplayWritingSystemAbbreviations = true
			};

			// SUT
			var genericClone = orig.DeepClone();

			var clone = genericClone as DictionaryNodeWritingSystemOptions;
			Assert.NotNull(clone, "Incorrect subclass returned; expected DictionaryNodeWritingSystemOptions");
			Assert.AreNotSame(orig, clone, "Not deep cloned; shallow cloned");
			Assert.AreEqual(orig.WsType, clone.WsType);
			Assert.AreEqual(orig.DisplayWritingSystemAbbreviations, clone.DisplayWritingSystemAbbreviations);
			AssertListWasDeepCloned(orig.Options, clone.Options);
		}

		internal static void AssertListWasDeepCloned(List<DictionaryNodeListOptions.DictionaryNodeOption> orig,
			List<DictionaryNodeListOptions.DictionaryNodeOption> clone)
		{
			Assert.AreNotSame(orig, clone, "Not deep cloned; shallow cloned");
			Assert.AreEqual(orig.Count, clone.Count);
			for (int i = 0; i < orig.Count; i++)
			{
				Assert.AreNotSame(orig[i], clone[i], "Not deep cloned; shallow cloned");
				Assert.AreEqual(orig[i].Id, clone[i].Id);
				Assert.AreEqual(orig[i].IsEnabled, clone[i].IsEnabled);
			}
		}
	}
}
