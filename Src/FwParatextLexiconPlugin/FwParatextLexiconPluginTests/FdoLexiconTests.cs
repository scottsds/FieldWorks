// Copyright (c) 2015-2018 SIL International
// This software is licensed under the LGPL, version 2.1 or later
// (http://www.gnu.org/licenses/lgpl-2.1.html)

using System;
using System.Collections.Generic;
using System.Linq;
using NUnit.Framework;
using Paratext.LexicalContracts;
using SIL.LCModel.Core.Text;
using SIL.FieldWorks.Common.FwUtils;
using SIL.LCModel;
using SIL.LCModel.DomainServices;
using SIL.LCModel.Infrastructure;
using SIL.WritingSystems;

namespace SIL.FieldWorks.ParatextLexiconPlugin
{
	/// <summary>
	/// FDO lexicon tests
	/// </summary>
	[TestFixture]
	public class FdoLexiconTests
	{
		private ThreadHelper m_threadHelper;
		private FdoLexicon m_lexicon;
		private LcmCache m_cache;

		/// <summary>
		/// Set up the unit tests
		/// </summary>
		[SetUp]
		public void SetUp()
		{
			// Force initialization in ILRepacked SIL.WritingSystems assembly,
			// even if a referenced SIl.WritingSystems assembly somewhere down
			// the dependency chain, that we won't be using, was initialized.
			if (!Sldr.IsInitialized)
			{
				Sldr.Initialize();
			}

			FwRegistryHelper.Initialize();
			m_threadHelper = new ThreadHelper();
			var ui = new DummyLcmUI(m_threadHelper);
			var projectId = new ParatextLexiconPluginProjectId(BackendProviderType.kMemoryOnly, "Test.fwdata");
			m_cache = LcmCache.CreateCacheWithNewBlankLangProj(projectId, "en", "fr", "en", ui,
				ParatextLexiconPluginDirectoryFinder.LcmDirectories, new LcmSettings());
			NonUndoableUnitOfWorkHelper.Do(m_cache.ActionHandlerAccessor, () =>
				{
					m_cache.ServiceLocator.WritingSystems.AddToCurrentAnalysisWritingSystems(m_cache.ServiceLocator.WritingSystemManager.Get("fr"));
					m_cache.ServiceLocator.WritingSystems.AddToCurrentVernacularWritingSystems(m_cache.ServiceLocator.WritingSystemManager.Get("en"));
					m_cache.LangProject.MorphologicalDataOA.ParserParameters = "<ParserParameters><XAmple><MaxNulls>1</MaxNulls><MaxPrefixes>5</MaxPrefixes><MaxInfixes>1</MaxInfixes><MaxSuffixes>5</MaxSuffixes><MaxInterfixes>0</MaxInterfixes><MaxAnalysesToReturn>10</MaxAnalysesToReturn></XAmple><ActiveParser>XAmple</ActiveParser></ParserParameters>";
				});
			m_lexicon = new FdoLexicon("Test", "FieldWorks:Test", m_cache, m_cache.DefaultVernWs);
		}

		/// <summary>
		/// Cleans up the unit tests
		/// </summary>
		[TearDown]
		public void TearDown()
		{
			m_lexicon.Dispose();
			m_cache.Dispose();
			m_threadHelper.Dispose();
		}

		#region Tests
		/// <summary>
		/// Test multiple lexeme creates where the IDs match
		/// </summary>
		[Test]
		public void MultipleCreatesIdsMatch()
		{
			Lexeme lex = m_lexicon.CreateLexeme(LexemeType.Word, "a");
			Lexeme lex2 = m_lexicon.CreateLexeme(LexemeType.Word, "a");
			Assert.AreEqual(lex.Id, lex2.Id);
			Assert.AreEqual(LexemeType.Word, lex.Type);
			Assert.AreEqual("a", lex.LexicalForm);
			Assert.AreEqual(LexemeType.Word, lex2.Type);
			Assert.AreEqual("a", lex2.LexicalForm);
		}

		/// <summary>
		/// Test multiple lexeme creates that refer to the same sense
		/// </summary>
		[Test]
		public void MultipleCreatesReferToSameSenses()
		{
			Lexeme lex = m_lexicon.CreateLexeme(LexemeType.Word, "a");
			Lexeme lex2 = m_lexicon.CreateLexeme(LexemeType.Word, "a");

			m_lexicon.AddLexeme(lex);
			LexiconSense sense = lex.AddSense();
			sense.AddGloss("en", "test");

			Assert.AreEqual(1, lex2.Senses.Count());

			// Make sure the one that was added has the right sense now
			lex = m_lexicon[lex.Id];
			Assert.AreEqual(LexemeType.Word, lex.Type);
			Assert.AreEqual("a", lex.LexicalForm);
			Assert.AreEqual(1, lex.Senses.Count());
			Assert.AreEqual("en", lex.Senses.First().Glosses.First().Language);
			Assert.AreEqual("test", lex.Senses.First().Glosses.First().Text);
		}

		/// <summary>
		/// Test that adding sense also adds lexeme
		/// </summary>
		[Test]
		public void AddingSenseAddsLexeme()
		{
			Lexeme lex = m_lexicon.CreateLexeme(LexemeType.Word, "a");
			LexiconSense sense = lex.AddSense();
			sense.AddGloss("en", "test");

			Assert.AreEqual(1, m_lexicon.Lexemes.Count());

			lex = m_lexicon[lex.Id]; // Make sure we're using the one stored in the lexicon
			Assert.AreEqual(LexemeType.Word, lex.Type);
			Assert.AreEqual("a", lex.LexicalForm);
			Assert.AreEqual(1, lex.Senses.Count());
			Assert.AreEqual("en", lex.Senses.First().Glosses.First().Language);
			Assert.AreEqual("test", lex.Senses.First().Glosses.First().Text);
		}

		/// <summary>
		/// Test that homograph increments
		/// </summary>
		[Test]
		public void HomographsIncrement()
		{
			Lexeme lex = m_lexicon.CreateLexeme(LexemeType.Stem, "a");
			Lexeme lex2 = m_lexicon.CreateLexeme(LexemeType.Stem, "a");

			m_lexicon.AddLexeme(lex);

			Assert.AreEqual(lex.Id, lex2.Id);

			Lexeme lex3 = m_lexicon.CreateLexeme(LexemeType.Stem, "a");
			Assert.AreNotEqual(lex.Id, lex3.Id);
			Assert.AreNotEqual(lex2.Id, lex3.Id);
		}

		/// <summary>
		///
		/// </summary>
		[Test]
		public void HomographsFind()
		{
			Lexeme lex = m_lexicon.CreateLexeme(LexemeType.Stem, "a");
			m_lexicon.AddLexeme(lex);
			Lexeme lex2 = m_lexicon.CreateLexeme(LexemeType.Stem, "a");
			m_lexicon.AddLexeme(lex2);
			Assert.AreNotEqual(lex.Id, lex2.Id);

			List<Lexeme> found = new List<Lexeme>(m_lexicon.Lexemes);
			Assert.AreEqual(2, found.Count);
			Assert.AreEqual(lex.Id, found[0].Id);
			Assert.AreEqual(lex2.Id, found[1].Id);
		}

		/// <summary>
		/// Test find or create lexeme
		/// </summary>
		[Test]
		public void FindOrCreate()
		{
			Lexeme lex = m_lexicon.CreateLexeme(LexemeType.Word, "a");
			LexiconSense sense = lex.AddSense();
			sense.AddGloss("en", "monkey");

			Lexeme lex2 = m_lexicon.FindOrCreateLexeme(LexemeType.Word, "a");
			Assert.AreEqual(lex.Id, lex2.Id);
			Assert.AreEqual(LexemeType.Word, lex2.Type);
			Assert.AreEqual("a", lex2.LexicalForm);
			Assert.AreEqual(1, lex2.Senses.Count());
			Assert.AreEqual(1, lex2.Senses.First().Glosses.Count());
			Assert.AreEqual("en", lex2.Senses.First().Glosses.First().Language);
			Assert.AreEqual("monkey", lex2.Senses.First().Glosses.First().Text);

			Lexeme lex3 = m_lexicon.FindOrCreateLexeme(LexemeType.Suffix, "bob");
			Assert.AreNotEqual(lex.Id, lex3.Id);
			Assert.AreNotEqual(lex2.Id, lex3.Id);
			Assert.AreEqual(LexemeType.Suffix, lex3.Type);
			Assert.AreEqual("bob", lex3.LexicalForm);
			Assert.AreEqual(0, lex3.Senses.Count());
		}

		/// <summary>
		/// Test indexer
		/// </summary>
		[Test]
		public void Indexer()
		{
			Lexeme lex = m_lexicon.CreateLexeme(LexemeType.Stem, "a");
			m_lexicon.AddLexeme(lex);

			lex = m_lexicon[lex.Id];
			Assert.IsNotNull(lex);
			Assert.AreEqual(LexemeType.Stem, lex.Type);
			Assert.AreEqual("a", lex.LexicalForm);

			Lexeme lex2 = m_lexicon.CreateLexeme(LexemeType.Suffix, "monkey");
			Assert.IsNull(m_lexicon[lex2.Id]);
		}

		/// <summary>
		/// Test that creating a lexeme does not add it
		/// </summary>
		[Test]
		public void CreatingDoesNotAdd()
		{
			m_lexicon.CreateLexeme(LexemeType.Word, "a");
			Assert.AreEqual(0, m_lexicon.Lexemes.Count());
		}

		/// <summary>
		/// Test that getting a sense from a created lexeme does not add it
		/// </summary>
		[Test]
		public void GettingSensesDoesNotAdd()
		{
			Lexeme lexeme = m_lexicon.CreateLexeme(LexemeType.Word, "a");
			lexeme.Senses.Count();
			Assert.AreEqual(0, m_lexicon.Lexemes.Count());
		}

		/// <summary>
		///
		/// </summary>
		[Test]
		public void AddSucceeds()
		{
			Lexeme lex = m_lexicon.CreateLexeme(LexemeType.Word, "a");
			m_lexicon.AddLexeme(lex);
			Assert.AreEqual(1, m_lexicon.Lexemes.Count());

			lex = m_lexicon[lex.Id]; // Make sure we're using the one stored in the lexicon
			Assert.AreEqual(LexemeType.Word, lex.Type);
			Assert.AreEqual("a", lex.LexicalForm);
		}

		/// <summary>
		/// Test that adding the same lexeme multiple times fails
		/// </summary>
		[Test]
		public virtual void MultipleAddsFail()
		{
			Lexeme lex = m_lexicon.CreateLexeme(LexemeType.Word, "a");
			Lexeme lex2 = m_lexicon.CreateLexeme(LexemeType.Word, "a");
			m_lexicon.AddLexeme(lex);
			Assert.Throws(typeof (ArgumentException), () => m_lexicon.AddLexeme(lex2));
		}

		/// <summary>
		///
		/// </summary>
		[Test]
		public void SensesRetained()
		{
			Lexeme lex = m_lexicon.CreateLexeme(LexemeType.Word, "a");
			m_lexicon.AddLexeme(lex);
			LexiconSense sense = lex.AddSense();
			sense.AddGloss("en", "glossen");
			sense.AddGloss("fr", "glossfr");

			Assert.AreEqual(1, lex.Senses.Count());
			Assert.AreEqual(2, lex.Senses.First().Glosses.Count());

			sense = m_lexicon[lex.Id].Senses.First(); // Make sure we're working with the one stored in the lexicon
			Assert.AreEqual("en", sense.Glosses.First().Language);
			Assert.AreEqual("glossen", sense.Glosses.First().Text);
			Assert.AreEqual("fr", sense.Glosses.ElementAt(1).Language);
			Assert.AreEqual("glossfr", sense.Glosses.ElementAt(1).Text);

			sense.RemoveGloss("en");

			sense = m_lexicon[lex.Id].Senses.First(); // Make sure we're working with the one stored in the lexicon
			Assert.AreEqual(1, sense.Glosses.Count());
			Assert.AreEqual("fr", sense.Glosses.First().Language);
			Assert.AreEqual("glossfr", sense.Glosses.First().Text);
		}

		/// <summary>
		///
		/// </summary>
		[Test]
		public void MorphTypeRetained()
		{
			Lexeme lex = m_lexicon.CreateLexeme(LexemeType.Word, "a");
			m_lexicon.AddLexeme(lex);
			Lexeme lex2 = m_lexicon.CreateLexeme(LexemeType.Prefix, "a");
			m_lexicon.AddLexeme(lex2);
			Lexeme lex3 = m_lexicon.CreateLexeme(LexemeType.Suffix, "a");
			m_lexicon.AddLexeme(lex3);
			Lexeme lex4 = m_lexicon.CreateLexeme(LexemeType.Stem, "a");
			m_lexicon.AddLexeme(lex4);

			Assert.AreEqual(4, m_lexicon.Lexemes.Count());
			Assert.IsTrue(m_lexicon.Lexemes.Contains(lex));
			Assert.IsTrue(m_lexicon.Lexemes.Contains(lex2));
			Assert.IsTrue(m_lexicon.Lexemes.Contains(lex3));
			Assert.IsTrue(m_lexicon.Lexemes.Contains(lex4));
		}

		/// <summary>
		/// Test removing lexemes
		/// </summary>
		[Test]
		public void RemoveLexemeSucceeds()
		{
			Lexeme lex = m_lexicon.CreateLexeme(LexemeType.Word, "a");
			m_lexicon.AddLexeme(lex);
			Lexeme lex2 = m_lexicon.CreateLexeme(LexemeType.Prefix, "a");

			Assert.IsTrue(m_lexicon.Lexemes.Contains(lex));
			Assert.IsFalse(m_lexicon.Lexemes.Contains(lex2));

			m_lexicon.RemoveLexeme(lex);
			Assert.IsFalse(m_lexicon.Lexemes.Contains(lex));

			m_lexicon.RemoveLexeme(lex2);
			Assert.IsFalse(m_lexicon.Lexemes.Contains(lex2));

			m_lexicon.AddLexeme(lex2);
			Lexeme lex3 = m_lexicon.CreateLexeme(LexemeType.Prefix, "a");
			m_lexicon.AddLexeme(lex3);

			m_lexicon.RemoveLexeme(lex2);
			Assert.IsFalse(m_lexicon.Lexemes.Contains(lex2));
			Assert.IsTrue(m_lexicon.Lexemes.Contains(lex3));
		}

		/// <summary>
		/// Test removing senses
		/// </summary>
		[Test]
		public void RemoveSenseSucceeds()
		{
			Lexeme lex = m_lexicon.CreateLexeme(LexemeType.Word, "a");
			m_lexicon.AddLexeme(lex);

			LexiconSense sense = lex.AddSense();
			sense.AddGloss("en", "gloss1");

			LexiconSense sense2 = lex.AddSense();
			sense.AddGloss("en", "gloss1");

			// Test remove at
			lex.RemoveSense(sense2);

			Assert.AreEqual(1, lex.Senses.Count());
			Assert.AreEqual(sense, lex.Senses.First());
		}

		/// <summary>
		/// Test unusual characters in the lexical form
		/// </summary>
		[Test]
		public void UnusualCharactersSupported()
		{
			var stems = new[] { "a:b:c", "a:b:", "a:2", "123-4", "!@#$%^&*()" };

			foreach (string stem in stems)
			{
				Lexeme lexeme = m_lexicon.FindOrCreateLexeme(LexemeType.Stem, stem);
				Assert.IsFalse(m_lexicon.Lexemes.Contains(lexeme));
				m_lexicon.AddLexeme(lexeme);
				Assert.IsTrue(m_lexicon.Lexemes.Contains(lexeme));

				// Add homomorph
				Lexeme lexeme2 = m_lexicon.CreateLexeme(LexemeType.Stem, stem);
				Assert.IsFalse(m_lexicon.Lexemes.Contains(lexeme2));
				m_lexicon.AddLexeme(lexeme2);
				Assert.IsTrue(m_lexicon.Lexemes.Contains(lexeme2));
			}
		}

		/// <summary>
		/// Tests that the lexicon correctly normalizes strings
		/// </summary>
		[Test]
		public void NormalizeStrings()
		{
			Lexeme lex = m_lexicon.CreateLexeme(LexemeType.Stem, "Vacaci\u00f3n"); // Uses composed accented letter 'o'
			m_lexicon.AddLexeme(lex);

			lex = m_lexicon[new LexemeKey(LexemeType.Stem, "Vacaci\u00f3n").Id];
			Assert.IsNotNull(lex);
			Assert.AreEqual(LexemeType.Stem, lex.Type);
			Assert.AreEqual("Vacaci\u00f3n", lex.LexicalForm);

			LexiconSense sense = lex.AddSense();
			Assert.IsNotNull(sense);

			LanguageText gloss = sense.AddGloss("en", "D\u00f3nde");

			Lexeme reGetLex = m_lexicon[lex.Id];
			Assert.AreEqual(gloss.Text, reGetLex.Senses.First().Glosses.First().Text);
		}

		#region Lexicon Events

		/// <summary>
		/// Test lexeme added event
		/// </summary>
		[Test]
		public void LexemeAddedEvent()
		{
			// Listen for events
			int lexemeAddedCount = 0;
			int senseAddedCount = 0;
			int glossAddedCount = 0;
			m_lexicon.LexemeAdded += (sender, e) => lexemeAddedCount++;
			m_lexicon.LexiconSenseAdded += (sender, e) => senseAddedCount++;
			m_lexicon.LexiconGlossAdded += (sender, e) => glossAddedCount++;

			Lexeme lexeme = m_lexicon.FindOrCreateLexeme(LexemeType.Word, "word");
			Assert.AreEqual(0, lexemeAddedCount);

			m_lexicon.AddLexeme(lexeme);
			Assert.AreEqual(1, lexemeAddedCount);
			Assert.AreEqual(0, senseAddedCount);
			Assert.AreEqual(0, glossAddedCount);

			// Adding sense adds lexeme
			Lexeme lexeme2 = m_lexicon.FindOrCreateLexeme(LexemeType.Word, "word2");
			lexeme2.AddSense();
			Assert.AreEqual(2, lexemeAddedCount);
		}

		/// <summary>
		/// Test sense added event
		/// </summary>
		[Test]
		public void SenseAddedEvent()
		{
			// Listen for events
			int lexemeAddedCount = 0;
			int senseAddedCount = 0;
			int glossAddedCount = 0;
			m_lexicon.LexemeAdded += (sender, e) => lexemeAddedCount++;
			m_lexicon.LexiconSenseAdded += (sender, e) => senseAddedCount++;
			m_lexicon.LexiconGlossAdded += (sender, e) => glossAddedCount++;

			Lexeme lexeme = m_lexicon.FindOrCreateLexeme(LexemeType.Word, "word");
			m_lexicon.AddLexeme(lexeme);
			lexeme.AddSense();
			Assert.AreEqual(1, senseAddedCount);
		}

		/// <summary>
		/// Test gloss added event
		/// </summary>
		[Test]
		public void GlossAddedEvent()
		{
			// Listen for events
			int lexemeAddedCount = 0;
			int senseAddedCount = 0;
			int glossAddedCount = 0;
			string glossText = "";
			m_lexicon.LexemeAdded += (sender, e) => lexemeAddedCount++;
			m_lexicon.LexiconSenseAdded += (sender, e) => senseAddedCount++;
			m_lexicon.LexiconGlossAdded += (sender, e) =>
				{
					glossAddedCount++;
					glossText = e.Gloss.Text;
				};

			Lexeme lexeme = m_lexicon.FindOrCreateLexeme(LexemeType.Word, "word");
			m_lexicon.AddLexeme(lexeme);
			LexiconSense sense = lexeme.AddSense();
			sense.AddGloss("en", "somegloss");

			Assert.AreEqual(1, glossAddedCount);
			Assert.AreEqual("somegloss", glossText);
		}

		#endregion

		/// <summary>
		/// Test find matching lexemes
		/// </summary>
		[Test]
		public void FindMatchingLexemes()
		{
			Lexeme[] matchingLexemes = m_lexicon.FindMatchingLexemes("a").ToArray();
			Assert.That(matchingLexemes, Is.Empty);

			// Just the stem
			Lexeme lexemeA = m_lexicon.CreateLexeme(LexemeType.Stem, "a");
			m_lexicon.AddLexeme(lexemeA);
			matchingLexemes = m_lexicon.FindMatchingLexemes("a").ToArray();
			Assert.That(matchingLexemes[0].LexicalForm, Is.EqualTo("a"));

			// Other parts, too
			Lexeme lexemePre = m_lexicon.CreateLexeme(LexemeType.Prefix, "pre");
			m_lexicon.AddLexeme(lexemePre);
			Lexeme lexemeSuf = m_lexicon.CreateLexeme(LexemeType.Suffix, "suf");
			m_lexicon.AddLexeme(lexemeSuf);
			m_lexicon.AddWordAnalysis(m_lexicon.CreateWordAnalysis("preasuf", new[] { lexemePre, lexemeA, lexemeSuf }));
			matchingLexemes = m_lexicon.FindMatchingLexemes("preasuf").ToArray();
			Assert.That(matchingLexemes.Length, Is.EqualTo(3));
			Assert.IsTrue(matchingLexemes.Contains(lexemePre));
			Assert.IsTrue(matchingLexemes.Contains(lexemeA));
			Assert.IsTrue(matchingLexemes.Contains(lexemeSuf));
		}

		/// <summary>
		/// Test find closest matching lexeme
		/// </summary>
		[Test]
		public void FindClosestMatchingLexeme()
		{
			// Nothing found
			Lexeme matchingLexeme = m_lexicon.FindClosestMatchingLexeme("a");
			Assert.IsNull(matchingLexeme);

			// Found by simple lexicon lookup
			Lexeme lexeme = m_lexicon.CreateLexeme(LexemeType.Stem, "a");
			m_lexicon.AddLexeme(lexeme);
			matchingLexeme = m_lexicon.FindClosestMatchingLexeme("a");
			Assert.IsTrue(matchingLexeme.LexicalForm == "a");

			// Found by parser
			lexeme = m_lexicon.CreateLexeme(LexemeType.Prefix, "pre");
			m_lexicon.AddLexeme(lexeme);
			matchingLexeme = m_lexicon.FindClosestMatchingLexeme("prea");
			Assert.IsTrue(matchingLexeme.LexicalForm == "a");

			// Found by unsupervised stemmer
			m_lexicon.AddLexeme(m_lexicon.CreateLexeme(LexemeType.Stem, "b"));
			m_lexicon.AddLexeme(m_lexicon.CreateLexeme(LexemeType.Stem, "c"));
			m_lexicon.AddLexeme(m_lexicon.CreateLexeme(LexemeType.Stem, "d"));
			m_lexicon.AddLexeme(m_lexicon.CreateLexeme(LexemeType.Stem, "bpos"));
			m_lexicon.AddLexeme(m_lexicon.CreateLexeme(LexemeType.Stem, "cpos"));
			m_lexicon.AddLexeme(m_lexicon.CreateLexeme(LexemeType.Stem, "dpos"));
			matchingLexeme = m_lexicon.FindClosestMatchingLexeme("apos");
			Assert.IsTrue(matchingLexeme.LexicalForm == "a");
		}

		/// <summary>
		/// Test when an entry has no morph type specified.
		/// </summary>
		[Test]
		public void NoMorphTypeSpecified()
		{
			NonUndoableUnitOfWorkHelper.Do(m_cache.ActionHandlerAccessor, () =>
			{
				ILexEntry entry = m_cache.ServiceLocator.GetInstance<ILexEntryFactory>().Create(m_cache.ServiceLocator.GetInstance<IMoMorphTypeRepository>().GetObject(MoMorphTypeTags.kguidMorphStem),
					TsStringUtils.MakeString("form", m_cache.DefaultVernWs), "gloss", new SandboxGenericMSA());
				entry.LexemeFormOA.MorphTypeRA = null;
			});

			Lexeme lexeme = m_lexicon.FindMatchingLexemes("form").Single();
			Assert.That(lexeme.LexicalForm, Is.EqualTo("form"));
			Assert.That(lexeme.Type, Is.EqualTo(LexemeType.Stem));
			lexeme = m_lexicon.Lexemes.Single();
			Assert.That(lexeme.LexicalForm, Is.EqualTo("form"));
			Assert.That(lexeme.Type, Is.EqualTo(LexemeType.Stem));
		}

		/// <summary>
		/// Test when an entry has a lexeme form with an empty default vernacular.
		/// </summary>
		[Test]
		public void EmptyDefaultVernLexemeForm()
		{
			NonUndoableUnitOfWorkHelper.Do(m_cache.ActionHandlerAccessor, () =>
			{
				m_cache.ServiceLocator.GetInstance<ILexEntryFactory>().Create(m_cache.ServiceLocator.GetInstance<IMoMorphTypeRepository>().GetObject(MoMorphTypeTags.kguidMorphStem),
					TsStringUtils.MakeString("form", m_cache.ServiceLocator.WritingSystems.VernacularWritingSystems.ElementAt(1).Handle), "gloss", new SandboxGenericMSA());
			});
			Assert.That(m_lexicon.Lexemes.Count(), Is.EqualTo(1));
			Assert.That(m_lexicon.Lexemes.Single().LexicalForm, Is.EqualTo(string.Empty));
		}

		/// <summary>
		/// Test when an entry has a null lexeme form.
		/// </summary>
		[Test]
		public void NullLexemeForm()
		{
			NonUndoableUnitOfWorkHelper.Do(m_cache.ActionHandlerAccessor, () =>
			{
				ILexEntry entry = m_cache.ServiceLocator.GetInstance<ILexEntryFactory>().Create(m_cache.ServiceLocator.GetInstance<IMoMorphTypeRepository>().GetObject(MoMorphTypeTags.kguidMorphStem),
					TsStringUtils.MakeString("form", m_cache.ServiceLocator.WritingSystems.VernacularWritingSystems.ElementAt(1).Handle), "gloss", new SandboxGenericMSA());
				entry.LexemeFormOA = null;
			});
			Assert.That(m_lexicon.Lexemes.Count(), Is.EqualTo(1));
			Assert.That(m_lexicon.Lexemes.Single().LexicalForm, Is.EqualTo(string.Empty));
		}

		/// <summary>
		/// Test that the name for entry tree lexical relations are correct.
		/// </summary>
		[Test]
		public void EntryTreeLexicalRelationName()
		{
			NonUndoableUnitOfWorkHelper.Do(m_cache.ActionHandlerAccessor, () =>
			{
				ILexEntry entry1 = m_cache.ServiceLocator.GetInstance<ILexEntryFactory>().Create(m_cache.ServiceLocator.GetInstance<IMoMorphTypeRepository>().GetObject(MoMorphTypeTags.kguidMorphStem),
					TsStringUtils.MakeString("form1", m_cache.DefaultVernWs), "gloss1", new SandboxGenericMSA());
				ILexEntry entry2 = m_cache.ServiceLocator.GetInstance<ILexEntryFactory>().Create(m_cache.ServiceLocator.GetInstance<IMoMorphTypeRepository>().GetObject(MoMorphTypeTags.kguidMorphStem),
					TsStringUtils.MakeString("form2", m_cache.DefaultVernWs), "gloss2", new SandboxGenericMSA());
				ILexEntry entry3 = m_cache.ServiceLocator.GetInstance<ILexEntryFactory>().Create(m_cache.ServiceLocator.GetInstance<IMoMorphTypeRepository>().GetObject(MoMorphTypeTags.kguidMorphStem),
					TsStringUtils.MakeString("form3", m_cache.DefaultVernWs), "gloss3", new SandboxGenericMSA());
				m_cache.LangProject.LexDbOA.ReferencesOA = m_cache.ServiceLocator.GetInstance<ICmPossibilityListFactory>().Create();

				ILexRefType entryLexRefType = m_cache.ServiceLocator.GetInstance<ILexRefTypeFactory>().Create();
				m_cache.LangProject.LexDbOA.ReferencesOA.PossibilitiesOS.Add(entryLexRefType);
				entryLexRefType.MappingType = (int) LexRefTypeTags.MappingTypes.kmtEntryTree;
				entryLexRefType.Name.SetAnalysisDefaultWritingSystem("Part");
				entryLexRefType.ReverseName.SetAnalysisDefaultWritingSystem("Whole");

				ILexReference entryLexRef = m_cache.ServiceLocator.GetInstance<ILexReferenceFactory>().Create();
				entryLexRefType.MembersOC.Add(entryLexRef);
				entryLexRef.TargetsRS.Add(entry1);
				entryLexRef.TargetsRS.Add(entry2);
				entryLexRef.TargetsRS.Add(entry3);
			});

			Lexeme lexeme = m_lexicon.FindMatchingLexemes("form1").Single();
			Assert.That(lexeme.LexicalRelations.Select(lr => lr.Name), Is.EquivalentTo(new[] {"Part", "Part"}));

			lexeme = m_lexicon.FindMatchingLexemes("form2").Single();
			Assert.That(lexeme.LexicalRelations.Select(lr => lr.Name), Is.EquivalentTo(new[] {"Whole", "Other"}));
		}

		/// <summary>
		/// Test that the name for sense tree lexical relations are correct.
		/// </summary>
		[Test]
		public void SenseTreeLexicalRelationName()
		{
			NonUndoableUnitOfWorkHelper.Do(m_cache.ActionHandlerAccessor, () =>
			{
				ILexEntry entry1 = m_cache.ServiceLocator.GetInstance<ILexEntryFactory>().Create(m_cache.ServiceLocator.GetInstance<IMoMorphTypeRepository>().GetObject(MoMorphTypeTags.kguidMorphStem),
					TsStringUtils.MakeString("form1", m_cache.DefaultVernWs), "gloss1", new SandboxGenericMSA());
				ILexEntry entry2 = m_cache.ServiceLocator.GetInstance<ILexEntryFactory>().Create(m_cache.ServiceLocator.GetInstance<IMoMorphTypeRepository>().GetObject(MoMorphTypeTags.kguidMorphStem),
					TsStringUtils.MakeString("form2", m_cache.DefaultVernWs), "gloss2", new SandboxGenericMSA());
				ILexEntry entry3 = m_cache.ServiceLocator.GetInstance<ILexEntryFactory>().Create(m_cache.ServiceLocator.GetInstance<IMoMorphTypeRepository>().GetObject(MoMorphTypeTags.kguidMorphStem),
					TsStringUtils.MakeString("form3", m_cache.DefaultVernWs), "gloss3", new SandboxGenericMSA());
				m_cache.LangProject.LexDbOA.ReferencesOA = m_cache.ServiceLocator.GetInstance<ICmPossibilityListFactory>().Create();

				ILexRefType senseLexRefType = m_cache.ServiceLocator.GetInstance<ILexRefTypeFactory>().Create();
				m_cache.LangProject.LexDbOA.ReferencesOA.PossibilitiesOS.Add(senseLexRefType);
				senseLexRefType.MappingType = (int) LexRefTypeTags.MappingTypes.kmtSenseTree;
				senseLexRefType.Name.SetAnalysisDefaultWritingSystem("Part");
				senseLexRefType.ReverseName.SetAnalysisDefaultWritingSystem("Whole");

				ILexReference senseLexRef = m_cache.ServiceLocator.GetInstance<ILexReferenceFactory>().Create();
				senseLexRefType.MembersOC.Add(senseLexRef);
				senseLexRef.TargetsRS.Add(entry1.SensesOS[0]);
				senseLexRef.TargetsRS.Add(entry2.SensesOS[0]);
				senseLexRef.TargetsRS.Add(entry3.SensesOS[0]);
			});

			Lexeme lexeme = m_lexicon.FindMatchingLexemes("form1").Single();
			Assert.That(lexeme.LexicalRelations.Select(lr => lr.Name), Is.EquivalentTo(new[] {"Part", "Part"}));

			lexeme = m_lexicon.FindMatchingLexemes("form2").Single();
			Assert.That(lexeme.LexicalRelations.Select(lr => lr.Name), Is.EquivalentTo(new[] {"Whole", "Other"}));
		}

		/// <summary>
		/// The Paratext Open in Lexicon button should be enabled if FW is found.
		/// Part of LT-18529 and LT-18721.
		/// </summary>
		[Test]
		public void CanOpenInLexicon_Works()
		{
			var origFwDirEnviromentSetting = Environment.GetEnvironmentVariable("FIELDWORKSDIR");
			try
			{
				Environment.SetEnvironmentVariable("FIELDWORKSDIR", null);
				// SUT
				Assert.That(m_lexicon.CanOpenInLexicon, Is.False,
					"Should not be able to open in Lexicon if FW location is not known.");

				var likeAPathToFw = System.IO.Path.GetDirectoryName(System.Reflection.Assembly.GetExecutingAssembly().Location);
				Environment.SetEnvironmentVariable("FIELDWORKSDIR", likeAPathToFw);
				// SUT
				Assert.That(m_lexicon.CanOpenInLexicon, Is.True,
					"Should report ablity to open in Lexicon if FW location is known.");
			}
			finally
			{
				Environment.SetEnvironmentVariable("FIELDWORKSDIR", origFwDirEnviromentSetting);
			}
		}

		#endregion
	}
}
