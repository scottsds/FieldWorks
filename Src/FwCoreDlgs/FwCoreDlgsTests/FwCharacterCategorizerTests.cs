// Copyright (c) 2015 SIL International
// This software is licensed under the LGPL, version 2.1 or later
// (http://www.gnu.org/licenses/lgpl-2.1.html)

using System.Collections.Generic;
using NUnit.Framework;
using SIL.LCModel.Core.WritingSystems;
using SIL.FieldWorks.Common.FwUtils;
using SIL.LCModel;
using SIL.WritingSystems;

namespace SIL.FieldWorks.FwCoreDlgs
{
	/// ----------------------------------------------------------------------------------------
	/// <summary>
	/// Tests use of the FwCharacterCategorizer that uses ICU instead of .NET to determine
	/// the category of characters for a particular writing system.
	/// </summary>
	/// ----------------------------------------------------------------------------------------
	[TestFixture]
	public class FwCharacterCategorizerTests : MemoryOnlyBackendProviderTestBase
	{
		#region Tests
		///--------------------------------------------------------------------------------------
		/// <summary>
		/// Tests getting punctuation and word-forming charcters when the '#' is not defined
		/// as a word-forming character.
		/// </summary>
		///--------------------------------------------------------------------------------------
		[Test]
		public void SymbolPunctuationOnly()
		{
			CoreWritingSystemDefinition ws = Cache.ServiceLocator.WritingSystemManager.Create("th");
			ws.CharacterSets.Clear();
			ws.CharacterSets.Add(new CharacterSetDefinition("main") {Characters = {"a", "b", "c", "d", "e"}});
			ws.CharacterSets.Add(new CharacterSetDefinition("numeric") {Characters = {"1", "2", "3", "4", "5"}});
			ws.CharacterSets.Add(new CharacterSetDefinition("punctuation") {Characters = {"'", "-", "#"}});
			ValidCharacters validChars = ValidCharacters.Load(ws);

			var categorizer = new FwCharacterCategorizer(validChars);
			Assert.IsTrue(categorizer.IsPunctuation('#'));
			Assert.IsFalse(categorizer.IsWordFormingCharacter('#'));
		}

		///--------------------------------------------------------------------------------------
		/// <summary>
		/// Tests the WordsAndPuncs class when the '.' character is defined as a word-forming
		/// character.
		/// </summary>
		///--------------------------------------------------------------------------------------
		[Test]
		public void WordAndPuncs_OverridePunc()
		{
			CoreWritingSystemDefinition ws = Cache.ServiceLocator.WritingSystemManager.Create("th");
			ws.CharacterSets.Clear();
			ws.CharacterSets.Add(new CharacterSetDefinition("main") {Characters = {"a", "b", "c", "d", "e", "."}});
			ws.CharacterSets.Add(new CharacterSetDefinition("numeric") {Characters = {"1", "2", "3", "4", "5"}});
			ws.CharacterSets.Add(new CharacterSetDefinition("punctuation") {Characters = {"'", "-", "#"}});
			ValidCharacters validChars = ValidCharacters.Load(ws);

			var categorizer = new FwCharacterCategorizer(validChars);

			List<WordAndPunct> wordsAndPunc = categorizer.WordAndPuncts("abc.de");

			// We expect one word to be returned.
			Assert.AreEqual(1, wordsAndPunc.Count);
			Assert.AreEqual("abc.de", wordsAndPunc[0].Word);
		}


		///--------------------------------------------------------------------------------------
		/// <summary>
		/// Tests the FwCharacterCategorizer class when the WordsAndPuncs method processes a
		/// string containing only spaces.
		/// </summary>
		///--------------------------------------------------------------------------------------
		[Test]
		public void WordAndPuncs_Spaces()
		{
			CoreWritingSystemDefinition ws = Cache.ServiceLocator.WritingSystemManager.Create("th");
			ws.CharacterSets.Clear();
			ws.CharacterSets.Add(new CharacterSetDefinition("main") {Characters = {"a", "b", "c"}});
			ws.CharacterSets.Add(new CharacterSetDefinition("numeric") {Characters = {"1", "2", "3", "4", "5"}});
			ws.CharacterSets.Add(new CharacterSetDefinition("punctuation") {Characters = {"-", " "}});
			ValidCharacters validChars = ValidCharacters.Load(ws);

			var categorizer = new FwCharacterCategorizer(validChars);

			List<WordAndPunct> wordsAndPunc = categorizer.WordAndPuncts(" ");
			Assert.AreEqual(0, wordsAndPunc.Count);

			wordsAndPunc = categorizer.WordAndPuncts("   ");
			Assert.AreEqual(0, wordsAndPunc.Count);
		}

		///--------------------------------------------------------------------------------------
		/// <summary>
		/// Tests the FwCharacterCategorizer class when the WordsAndPuncs method processes an
		/// empty string.
		/// </summary>
		///--------------------------------------------------------------------------------------
		[Test]
		public void WordAndPuncs_EmptyString()
		{
			CoreWritingSystemDefinition ws = Cache.ServiceLocator.WritingSystemManager.Create("th");
			ws.CharacterSets.Clear();
			ws.CharacterSets.Add(new CharacterSetDefinition("main") {Characters = {"a", "b", "c"}});
			ws.CharacterSets.Add(new CharacterSetDefinition("numeric") {Characters = {"1", "2", "3", "4", "5"}});
			ws.CharacterSets.Add(new CharacterSetDefinition("punctuation") {Characters = {"-", " "}});
			ValidCharacters validChars = ValidCharacters.Load(ws);

			var categorizer = new FwCharacterCategorizer(validChars);

			List<WordAndPunct> wordsAndPunc = categorizer.WordAndPuncts("");

			// We expect one word to be returned.
			Assert.AreEqual(0, wordsAndPunc.Count);
		}

		///--------------------------------------------------------------------------------------
		/// <summary>
		/// Tests the WordsAndPuncs class when the '.' character is not defined as a word-forming
		/// character.
		/// </summary>
		///--------------------------------------------------------------------------------------
		[Test]
		public void WordAndPuncs_NoOverridePunc()
		{
			CoreWritingSystemDefinition ws = Cache.ServiceLocator.WritingSystemManager.Create("th");
			ws.CharacterSets.Clear();
			ws.CharacterSets.Add(new CharacterSetDefinition("main") {Characters = {"a", "b", "c", "d", "e"}});
			ws.CharacterSets.Add(new CharacterSetDefinition("numeric") {Characters = {"1", "2", "3", "4", "5"}});
			ws.CharacterSets.Add(new CharacterSetDefinition("punctuation") {Characters = {"'", "-", "#"}});
			ValidCharacters validChars = ValidCharacters.Load(ws);

			var categorizer = new FwCharacterCategorizer(validChars);

			List<WordAndPunct> wordsAndPunc = categorizer.WordAndPuncts("abc.de");

			// We expect two words to be returned.
			Assert.AreEqual(2, wordsAndPunc.Count);
			Assert.AreEqual("abc", wordsAndPunc[0].Word);
			Assert.AreEqual("de", wordsAndPunc[1].Word);
		}
		#endregion
	}
}
