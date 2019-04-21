﻿// Copyright (c) 2015-2016 SIL International
// This software is licensed under the LGPL, version 2.1 or later
// (http://www.gnu.org/licenses/lgpl-2.1.html)

using System.IO;
using System.Linq;
using NUnit.Framework;
using SIL.LCModel.Core.Text;
using SIL.FieldWorks.Common.Controls;
using SIL.LCModel.Core.KernelInterfaces;
using SIL.FieldWorks.Common.FwUtils;
using SIL.LCModel;
using SIL.LCModel.Application;
using SIL.LCModel.Infrastructure;
using SIL.LCModel.Utils;
using XCore;

// ReSharper disable InconsistentNaming

namespace SIL.FieldWorks.XWorks
{
	/// <summary>
	/// Tests of the decorator for dictionary views.
	/// </summary>
	[TestFixture]
	public class DictionaryPublicationDecoratorTests : XWorksAppTestBase
	{
		ILexEntryFactory m_entryFactory;
		ILexSenseFactory m_senseFactory;
		private ILexExampleSentenceFactory m_exampleFactory;
		private ILexEntryRefFactory m_lexEntryRefFactory;
		private ILexRefTypeFactory m_lexRefTypeFactory;
		private ILexReferenceFactory m_lexRefFactory;
		private ICmPossibilityListFactory m_possListFactory;
		private IReversalIndexFactory m_revIndexFactory;
		private IReversalIndexEntryFactory m_revIndexEntryFactory;

		private ICmPossibility m_mainDict; // the publication we test on.

		private ILexEntry m_blank; // rude entry excluded altogether.
		private ILexEntry m_blank2; // homograph 2
		private ILexEntry m_blank3; // homograph 3
		private ILexEntry m_blip; // rude synonym of blank
		private ILexEntry m_bother; // acceptable synonym of blank and blip
		private ILexEntry m_ouch; // another acceptable synonym
		private ILexEntry m_hot; // entry OK,
		private ILexEntry m_water;
		private ILexEntry m_water2; // rude homograph of water
		private ILexEntry m_waterPrefix; // water as a prefix is not a homograph.
		private ILexEntry m_hotWater;
		private ILexEntry m_problem;
		private ILexEntry m_body;
		private ILexEntry m_arm;
		private ILexEntry m_leg;
		private ILexEntry m_belly;
		private ILexEntry m_torso;
		private ILexEntry m_hotBlank; // for some reason this is a complex form that is not excluded though both its components are.
		private ILexEntry m_blueSky;

		private ILexSense m_hotTemp; // default sense of hot.
		private ILexSense m_trouble; // bad sense of hot, as in hot water or stolen goods
		private ILexSense m_desirable; // second OK sense of hot
		private ILexSense m_fastCar; // subsense of m_desirable
		private ILexSense m_waterH2O;
		private ILexSense m_blipOuch; // subsense
		private ILexSense m_bluer;
		private ILexSense m_skyReal;

		private ILexEntry m_blueColor;
		private ILexEntry m_blueCold;
		private ILexEntry m_blueSad;
		private ILexEntry m_blueMusic;
		private ILexEntry m_hotArm;
		private ILexEntryRef m_hotArmComponents;
		private ILexEntry m_nolanryan;
		private ILexEntryRef m_nolanryanComponents;

		private ILexEntry m_sky;

		private ILexExampleSentence m_goodHot;
		private ILexExampleSentence m_badHot; // REVIEW (Hasso) 2018.02: many of these are unused. Can they be removed?

		private ILexEntryRef m_hotWaterComponents;
		private ILexEntryRef m_blueSkyComponents;

		private ILexRefType m_synonym;
		private ILexRefType m_partWhole;
		private ILexReference m_blankSynonyms;
		private ILexReference m_problemSynonyms;
		private ILexReference m_bodyParts;
		private ILexReference m_torsoParts;

		private ICmSemanticDomain m_domainBadWords;
		private ICmSemanticDomain m_domainTemperature;

		private ILexEntry m_ringBell;
		private ILexEntry m_ringCircle; // don't make a headword for this.
		private ILexEntry m_ringGold;

		private ILexEntry m_blackVerb; // no headword
		private ILexEntry m_blackColor; // real HN 2, but published as 0.

		private ILexEntry m_edName;
		private ILexEntry m_edSuffix;

		private MockPublisher m_publisher;
		private MockPublisher m_revPublisher;
		private DictionaryPublicationDecorator m_decorator;
		private DictionaryPublicationDecorator m_revDecorator;

		private IReversalIndex m_revIndex;

		private const int kmainFlid = 89999956;

		private int m_flidReferringSenses;

		private PropertyTable PropertyTable => m_window.PropTable;

		protected override void Init()
		{
			m_entryFactory = Cache.ServiceLocator.GetInstance<ILexEntryFactory>();
			m_senseFactory = Cache.ServiceLocator.GetInstance<ILexSenseFactory>();
			m_exampleFactory = Cache.ServiceLocator.GetInstance<ILexExampleSentenceFactory>();
			m_lexEntryRefFactory = Cache.ServiceLocator.GetInstance<ILexEntryRefFactory>();
			m_lexRefTypeFactory = Cache.ServiceLocator.GetInstance<ILexRefTypeFactory>();
			m_lexRefFactory = Cache.ServiceLocator.GetInstance<ILexReferenceFactory>();
			m_possListFactory = Cache.ServiceLocator.GetInstance<ICmPossibilityListFactory>();
			m_revIndexFactory = Cache.ServiceLocator.GetInstance<IReversalIndexFactory>();
			m_revIndexEntryFactory = Cache.ServiceLocator.GetInstance<IReversalIndexEntryFactory>();

			m_flidReferringSenses = Cache.MetaDataCacheAccessor.GetFieldId2(CmSemanticDomainTags.kClassId, "ReferringSenses",
				false);
			m_application = new MockFwXApp(new MockFwManager { Cache = Cache }, null, null);
			var configFilePath = Path.Combine(FwDirectoryFinder.CodeDirectory, m_application.DefaultConfigurationPathname);
			m_window = new MockFwXWindow(m_application, configFilePath);
			((MockFwXWindow)m_window).Init(Cache); // initializes Mediator values

			UndoableUnitOfWorkHelper.Do("do", "undo", Cache.ActionHandlerAccessor,
				() =>
					{
						m_domainBadWords = Cache.ServiceLocator.GetInstance<ICmSemanticDomainFactory>().Create();
						m_domainTemperature = Cache.ServiceLocator.GetInstance<ICmSemanticDomainFactory>().Create();
						Cache.LangProject.SemanticDomainListOA.PossibilitiesOS.Add(m_domainBadWords);
						Cache.LangProject.SemanticDomainListOA.PossibilitiesOS.Add(m_domainTemperature);
						m_mainDict = Cache.LangProject.LexDbOA.PublicationTypesOA.PossibilitiesOS[0];
						m_blank = MakeEntry("blank", "swear word", true);
						m_blank.SensesOS[0].SemanticDomainsRC.Add(m_domainBadWords);
						m_hot = MakeEntry("hot", "high temperature", false);
						m_hotTemp = m_hot.SensesOS[0];
						m_hotTemp.SemanticDomainsRC.Add(m_domainTemperature);
						m_trouble = MakeSense(m_hot, "trouble");
						m_trouble.DoNotPublishInRC.Add(m_mainDict);
						m_trouble.PicturesOS.Add(Cache.ServiceLocator.GetInstance<ICmPictureFactory>().Create());
						m_desirable = MakeSense(m_hot, "desirable");
						m_fastCar = MakeSense(m_desirable, "fast (car)");

						m_badHot = MakeExample(m_hotTemp, "a hot pile of blank", true);
						m_goodHot = MakeExample(m_hotTemp, "a hot bath", false);

						m_water = MakeEntry("water", "H2O", false);
						m_waterH2O = m_water.SensesOS[0];
						m_hotWater = MakeEntry("hot water", "trouble", false);
						m_hotWaterComponents = MakeEntryRef(m_hotWater, new ICmObject[] { m_trouble, m_waterH2O },
							new ICmObject[] { m_trouble, m_waterH2O },
							LexEntryRefTags.krtComplexForm);

						m_blank2 = MakeEntry("blank", "vacant", false);
						m_blank3 = MakeEntry("blank", "erase", false);
						m_water2 = MakeEntry("water", "urinate", true);
						m_waterPrefix = MakeEntry("water", "aquatic", false);
						m_waterPrefix.LexemeFormOA.MorphTypeRA = Cache.ServiceLocator.GetInstance<IMoMorphTypeRepository>()
							.GetObject(MoMorphTypeTags.kguidMorphPrefix);

						m_synonym = MakeRefType("synonym", null, (int)LexRefTypeTags.MappingTypes.kmtSenseCollection);
						m_blip = MakeEntry("blip", "rude word", true);
						m_bother = MakeEntry("bother", "I'm annoyed by that", false);
						m_ouch = MakeEntry("ouch", "that hurt", false);
						m_blipOuch = MakeSense(m_blip.SensesOS[0], "rude ouch");
						m_blankSynonyms = MakeLexRef(m_synonym, new ICmObject[] {m_blank, m_ouch.SensesOS[0], m_blip.SensesOS[0], m_blipOuch, m_bother});

						m_problem = MakeEntry("problem", "difficulty", false);
						m_problemSynonyms = MakeLexRef(m_synonym, new ICmObject[] { m_problem, m_trouble });

						m_body = MakeEntry("body", "body", true);
						m_arm = MakeEntry("arm", "arm", false);
						m_leg = MakeEntry("leg", "leg", false);
						m_belly = MakeEntry("belly", "belly", true);
						m_torso = MakeEntry("torso", "torso", false);
						m_partWhole = MakeRefType("partWhole", null, (int)LexRefTypeTags.MappingTypes.kmtEntryTree);
						m_bodyParts = MakeLexRef(m_partWhole, new ICmObject[] {m_body, m_arm, m_leg.SensesOS[0], m_torso, m_belly});
						m_torsoParts = MakeLexRef(m_partWhole, new ICmObject[] {m_torso, m_arm, m_belly});

						m_hotBlank = MakeEntry("hotBlank", "problem rude word", false);
						MakeEntryRef(m_hotBlank, new ICmObject[] { m_trouble, m_water2 },
							new ICmObject[] { m_trouble, m_water2 },
							LexEntryRefTags.krtComplexForm);

						m_blueColor = MakeEntry("blue", "color blue", false);
						m_blueCold = MakeEntry("blue", "cold", false);
						m_blueMusic = MakeEntry("blue", "jazzy", false);
						m_blueSad = MakeEntry("blue", "sad", false);

						m_blueMusic.HomographNumber = 2; // will duplicate blue cold; pathological, but should not crash.
						m_blueSad.HomographNumber = 3; // will conflict with renumbered blueMusic

						m_bluer = m_blueColor.SensesOS[0];
						m_sky = MakeEntry("sky", "interface between atmosphere and space", false, true); // true excludes as headword
						m_skyReal = m_sky.SensesOS[0];
						m_blueSky = MakeEntry("blue sky", "clear, huge potential", false);
						m_blueSkyComponents = MakeEntryRef(m_blueSky, new ICmObject[] { m_blueColor, m_skyReal },
							new ICmObject[] { m_bluer, m_skyReal },
							LexEntryRefTags.krtComplexForm);

						m_ringBell = MakeEntry("ring", "bell", false);
						m_ringCircle = MakeEntry("ring", "circle", false, true);
						m_ringGold = MakeEntry("ring", "gold", false);

						m_blackVerb = MakeEntry("black", "darken", false, true);
						m_blackColor = MakeEntry("black", "dark", false);

						m_hotArm = MakeEntry("hotarm", "pitcher", false);
						m_hotArmComponents = MakeEntryRef(m_hotArm, new ICmObject[] { m_hot, m_arm },
												new ICmObject[] { m_hot, m_arm },
												LexEntryRefTags.krtComplexForm);
						m_hotArm.DoNotPublishInRC.Add(m_mainDict);
						m_hotArmComponents.ShowComplexFormsInRS.Add(m_hot);

						m_nolanryan = MakeEntry("Nolan_Ryan", "pitcher", false);
						m_nolanryanComponents = MakeEntryRef(m_nolanryan, new ICmObject[] { m_hot },
												new ICmObject[] { m_hot },
												LexEntryRefTags.krtVariant);
						m_nolanryanComponents.VariantEntryTypesRS.Add(
							(ILexEntryType)Cache.LangProject.LexDbOA.VariantEntryTypesOA.PossibilitiesOS[0]);
						m_nolanryan.DoNotPublishInRC.Add(m_mainDict);

						m_edName = MakeEntry("ed", "someone called ed", false);
						m_edSuffix = MakeEntry("ed", "past", false, false, true);

						m_revIndex = CreateInterestingReversalEntries();

						m_publisher = new MockPublisher((ISilDataAccessManaged)Cache.DomainDataByFlid, kmainFlid);
						m_publisher.SetOwningPropValue(Cache.LangProject.LexDbOA.Entries.Select(le => le.Hvo).ToArray());
						m_decorator = new DictionaryPublicationDecorator(Cache, m_publisher, ObjectListPublisher.OwningFlid);

						m_revPublisher = new MockPublisher((ISilDataAccessManaged)Cache.DomainDataByFlid, kmainFlid);
						m_revPublisher.SetOwningPropValue(m_revIndex.AllEntries.Select(rie => rie.Hvo).ToArray());
						m_revDecorator = new DictionaryPublicationDecorator(Cache, m_revPublisher, ObjectListPublisher.OwningFlid);
					});
		}

		private IReversalIndex CreateInterestingReversalEntries()
		{
			var index = m_revIndexFactory.Create();
			Cache.LangProject.LexDbOA.ReversalIndexesOC.Add(index);
			index.WritingSystem = "en";
			var indexEntry = m_revIndexEntryFactory.Create();
			index.EntriesOC.Add(indexEntry);
			var wsEn = Cache.ServiceLocator.WritingSystemManager.Get("en").Handle;
			indexEntry.ReversalForm.set_String(wsEn, "Reversal Form");
			indexEntry.SensesRS.Add(m_nolanryan.AllSenses[0]);
			var indexEntry2 = m_revIndexEntryFactory.Create();
			index.EntriesOC.Add(indexEntry2);
			indexEntry2.ReversalForm.set_String(wsEn, "Reversal 2 Form");
			indexEntry2.SensesRS.Add(m_water.AllSenses[0]);
			var entry2SubentryA = m_revIndexEntryFactory.Create();
			indexEntry2.SubentriesOS.Add(entry2SubentryA);
			entry2SubentryA.ReversalForm.set_String(wsEn, "Reversal 2a Form");
			entry2SubentryA.SensesRS.Add(m_water2.AllSenses[0]);
			var entry2SubentryB = m_revIndexEntryFactory.Create();
			indexEntry2.SubentriesOS.Add(entry2SubentryB);
			entry2SubentryB.ReversalForm.set_String(wsEn, "Reversal 2b Form");
			entry2SubentryB.SensesRS.Add(m_hotWater.AllSenses[0]);
			var subsubentry = m_revIndexEntryFactory.Create();
			entry2SubentryB.SubentriesOS.Add(subsubentry);
			subsubentry.ReversalForm.set_String(wsEn, "Reversal 2b || !2b Form");
			subsubentry.SensesRS.Add(m_waterH2O);
			var senseLessIndexEntry = m_revIndexEntryFactory.Create();
			index.EntriesOC.Add(senseLessIndexEntry);
			senseLessIndexEntry.ReversalForm.set_String(wsEn, "Reversal Form NoSense");
			return index;
		}

		private ILexReference MakeLexRef(ILexRefType owner, ICmObject[] targets)
		{
			var result = m_lexRefFactory.Create();
			owner.MembersOC.Add(result);
			foreach (var obj in targets)
				result.TargetsRS.Add(obj);
			return result;
		}

		/// <summary>
		/// DictionaryConfigurationListener was sending a localized configuration type to a switch
		/// statement in GetEntriesToPublish(), causing any localized version to not match a valid configuration.
		/// This test prevents a reoccurrence of that problem.
		/// </summary>
		[Test]
		public void GetEntriesToPublish_WorksWithFrenchUI()
		{
			PropertyTable.SetProperty("currentContentControl", "lexiconEdit", true);

			var englishEntries = m_decorator.GetEntriesToPublish(PropertyTable, ObjectListPublisher.OwningFlid);
			Assert.That(englishEntries.Length, Is.GreaterThan(0));

			// Set UI Language to French
			var wsm = Cache.ServiceLocator.WritingSystemManager;
			wsm.UserWritingSystem = wsm.Get("fr");

			// SUT
			var frenchEntries = m_decorator.GetEntriesToPublish(PropertyTable, ObjectListPublisher.OwningFlid);
			Assert.That(englishEntries.Length, Is.EqualTo(frenchEntries.Length));
		}

		[Test]
		public void GetSortedAndFilteredReversalEntries_ExcludesSubentriesAndUnpublishable()
		{
			// This test relies on the objects set up during the test FixtureSetup
			PropertyTable.SetProperty("currentContentControl", "reversalToolEditComplete", true);
			PropertyTable.SetProperty("ReversalIndexGuid", m_revIndex.Guid.ToString(), true);

			Assert.AreEqual(6, m_revDecorator.VecProp(m_revIndex.Hvo, ObjectListPublisher.OwningFlid).Length,
				"there should be 6 Reversal Entries and Sub[sub]entries");
			var entries = m_revDecorator.GetEntriesToPublish(PropertyTable, ObjectListPublisher.OwningFlid, "Reversal Index");
			// "Reversal Form" is linked to m_nolanryan which is excluded from publication
			Assert.AreEqual(2, entries.Length, "there should be only 2 main Reversal Entry that can be published");
			var entry = Cache.ServiceLocator.GetObject(entries[0]) as IReversalIndexEntry;
			Assert.IsNotNull(entry, "the single reversal entry really is a reversal entry");
			Assert.AreEqual("Reversal 2 Form", entry.ShortName, "'Reversal 2 Form' is the sole publishable main reversal entry");
			Assert.AreEqual(2, entry.SubentriesOS.Count, "'Reversal 2 Form' has two subentries");
			// "Reversal 2a Form" is linked to m_water2 which is excluded from publication
			var vec = m_revDecorator.VecProp(entry.Hvo, ReversalIndexEntryTags.kflidSubentries);
			Assert.AreEqual(1, vec.Length, "Only one of the subentries is publishable");
			var subentry = (IReversalIndexEntry)Cache.ServiceLocator.GetObject(vec[0]);
			Assert.AreEqual("Reversal 2b Form", subentry.ShortName, "'Reversal 2b Form' is the only publishable subentry of 'Reversal 2 Form'");
			Assert.IsTrue(m_revDecorator.IsExcludedObject(entry.SubentriesOS[0]), "First subentry ('Reversal 2a Form') should be excluded");
			Assert.IsFalse(m_revDecorator.IsExcludedObject(entry.SubentriesOS[1]), "Second subentry ('Reversal 2b Form') should not be excluded')");
		}

		[Test]
		public void GetSortedAndFilteredReversalEntries_IncludesSenselessReversalEntries()
		{
			// This test relies on the objects set up during the test FixtureSetup
			PropertyTable.SetProperty("currentContentControl", "reversalToolEditComplete", false);
			PropertyTable.SetProperty("ReversalIndexGuid", m_revIndex.Guid.ToString(), false);

			Assert.AreEqual(6, m_revDecorator.VecProp(m_revIndex.Hvo, ObjectListPublisher.OwningFlid).Length,
				"there should be 6 Reversal Entries and Sub[sub]entries");
			var entries = m_revDecorator.GetEntriesToPublish(PropertyTable, ObjectListPublisher.OwningFlid, "Reversal Index");
			// "Reversal Form" is linked to m_nolanryan which is excluded from publication
			Assert.AreEqual(2, entries.Length, "there should be only 2 main Reversal Entry that can be published");
			var revEntry = Cache.ServiceLocator.GetObject(entries[1]) as IReversalIndexEntry;
			Assert.IsNotNull(revEntry, "the single reversal entry really is a reversal entry");
			Assert.IsFalse(revEntry.SensesRS.Any(), "Test setup is broken, this entry should have no senses");
			// SUT
			Assert.IsFalse(m_revDecorator.IsExcludedObject(revEntry), "A reversal index entry with no senses should not be excluded");
		}

		/// <summary>
		/// From various properties things that are not in this publication should not show up.
		/// </summary>
		[Test]
		public void SimpleFiltering()
		{
			// Try all the variants of retrieving a vector property.
			var sensesOfHot = m_decorator.VecProp(m_hot.Hvo, LexEntryTags.kflidSenses);
			Assert.That(sensesOfHot.Length, Is.EqualTo(2), "one bad sense should be eliminated.");
			Assert.That(sensesOfHot[0], Is.EqualTo(m_hot.SensesOS[0].Hvo));
			Assert.That(sensesOfHot[1], Is.EqualTo(m_desirable.Hvo));

			Assert.That(m_decorator.get_VecSize(m_hot.Hvo, LexEntryTags.kflidSenses), Is.EqualTo(2));
			Assert.That(m_decorator.get_VecItem(m_hot.Hvo, LexEntryTags.kflidSenses, 1), Is.EqualTo(m_desirable.Hvo));

			// This test is perhaps redundant here: DictionaryPublicationDecorator does not have to do anything to get this behavior.
			using (ArrayPtr arrayPtr = MarshalEx.ArrayToNative<int>(2))
			{
				int chvo;
				m_decorator.VecProp(m_hot.Hvo, LexEntryTags.kflidSenses, 2, out chvo, arrayPtr);
				var values = MarshalEx.NativeToArray<int>(arrayPtr, 2);
				Assert.That(values[0], Is.EqualTo(m_hot.SensesOS[0].Hvo));
				Assert.That(values[1], Is.EqualTo(m_desirable.Hvo));
			}

			// This verifies both that examples are included in the bad objects, and that properties
			// that point at examples are automatically excluded.
			var hotTempExamples = m_decorator.VecProp(m_hotTemp.Hvo, LexSenseTags.kflidExamples);
			Assert.That(hotTempExamples.Length, Is.EqualTo(1));
			Assert.That(hotTempExamples[0], Is.EqualTo(m_goodHot.Hvo));

			// They should be filtered also from certain properties with CmObject signatures.
			var hotWaterComponents = m_decorator.VecProp(m_hotWaterComponents.Hvo, LexEntryRefTags.kflidComponentLexemes);
			Assert.That(hotWaterComponents.Length, Is.EqualTo(1));
			Assert.That(hotWaterComponents[0], Is.EqualTo(m_waterH2O.Hvo));
			// More cursory checks...there are many of these.
			Assert.That(m_decorator.VecProp(m_hotWaterComponents.Hvo, LexEntryRefTags.kflidPrimaryLexemes).Length, Is.EqualTo(1));
			// As well as checking that LexReference.Targets is filtered, this checks that we filter senses of excluded entries,
			// and that it really is possible for more than one thing to pass the filter.
			Assert.That(m_decorator.VecProp(m_blankSynonyms.Hvo, LexReferenceTags.kflidTargets).Length, Is.EqualTo(2));

			// They should be filtered from the top-level list of entries managed by the wrapped decorator
			var mainEntryList = m_decorator.VecProp(Cache.LangProject.LexDbOA.Hvo, ObjectListPublisher.OwningFlid);
			Assert.That(mainEntryList.Length,
				Is.EqualTo(Cache.LangProject.LexDbOA.Entries.Count(
					le => le.DoNotPublishInRC.Count == 0 &&
					le.DoNotShowMainEntryInRC.Count == 0)));
		}

		[Test]
		public void AffixAndStemAreNotHomographs()
		{
			Assert.That(m_decorator.get_IntProp(m_edName.Hvo, LexEntryTags.kflidHomographNumber), Is.EqualTo(0));
			Assert.That(m_decorator.get_IntProp(m_edSuffix.Hvo, LexEntryTags.kflidHomographNumber), Is.EqualTo(0));
		}

		/// <summary>
		/// Test that virtual properties are filtered. The selection of virtual properties to filter is automatic, so testing one
		/// (that is crucial for Classified Dictionary) is sufficient. (Enhance: maybe one day test at least one of each signature class?)
		/// Todo: some virtual properties should possibly give different answers because things they depend on are modified?
		/// So far we are only handling properties that return objects of the type that may be excluded, and only by excluding
		/// the unwanted objects.
		/// </summary>
		[Test]
		public void VirtualPropertyFiltering()
		{
			Assert.That(m_decorator.VecProp(m_domainBadWords.Hvo, m_flidReferringSenses), Has.Length.EqualTo(0), "should hide the unpublished sense");
			Assert.That(m_decorator.VecProp(m_domainTemperature.Hvo, m_flidReferringSenses), Has.Length.EqualTo(1), "should not hide the published sense");
		}

		[Test]
		public void HomographNumberAfterPublishedNonHeadword()
		{
			Assert.That(m_decorator.get_IntProp(m_ringGold.Hvo, LexEntryTags.kflidHomographNumber), Is.EqualTo(2));
			Assert.That(m_decorator.get_IntProp(m_ringCircle.Hvo, LexEntryTags.kflidHomographNumber), Is.EqualTo(0));
			Assert.That(m_decorator.get_IntProp(m_ringBell.Hvo, LexEntryTags.kflidHomographNumber), Is.EqualTo(1));
			Assert.That(m_decorator.get_IntProp(m_blackVerb.Hvo, LexEntryTags.kflidHomographNumber), Is.EqualTo(0)); // not shown as headword
			Assert.That(m_decorator.get_IntProp(m_blackColor.Hvo, LexEntryTags.kflidHomographNumber), Is.EqualTo(0)); // only black shown as headword
		}

		[Test]
		public void DuplicateHomographs()
		{
			Assert.That(m_decorator.get_IntProp(m_blueColor.Hvo, LexEntryTags.kflidHomographNumber), Is.EqualTo(1));
			Assert.That(m_decorator.get_IntProp(m_blueSad.Hvo, LexEntryTags.kflidHomographNumber), Is.EqualTo(4));
			int hnMusic = m_decorator.get_IntProp(m_blueMusic.Hvo, LexEntryTags.kflidHomographNumber);
			int hnCold = m_decorator.get_IntProp(m_blueCold.Hvo, LexEntryTags.kflidHomographNumber);
			// Can't predict which will come out first, but should be different.
			Assert.That(hnMusic == 2 && hnCold == 3 || hnMusic == 3 && hnCold == 2, Is.True);
		}

		/// <summary>
		/// From various properties things that are not in this publication should not show up.
		/// </summary>
		[Test]
		public void HomographAndHeadword()
		{
			Assert.That(m_blank2.HomographNumber, Is.EqualTo(2), "real HN should be set automatically");
			Assert.That(m_water2.HomographNumber, Is.EqualTo(2), "real HN should be set automatically");
			// These two are decremented because homograph 1 is not published
			Assert.That(m_decorator.get_IntProp(m_blank2.Hvo, LexEntryTags.kflidHomographNumber), Is.EqualTo(1));
			Assert.That(m_decorator.get_IntProp(m_blank3.Hvo, LexEntryTags.kflidHomographNumber), Is.EqualTo(2));
			// This one is reduced because with the only other homograph not published, it should not appear to be
			// a homograph at all.
			Assert.That(m_decorator.get_IntProp(m_water.Hvo, LexEntryTags.kflidHomographNumber), Is.EqualTo(0));
			int headwordFlid = Cache.MetaDataCacheAccessor.GetFieldId2(LexEntryTags.kClassId, "HeadWord", false);
			Assert.That(m_decorator.get_StringProp(m_blank2.Hvo, headwordFlid).Text, Is.EqualTo("blank1"));
			Assert.That(m_decorator.get_StringProp(m_water.Hvo, headwordFlid).Text, Is.EqualTo("water"));
			Assert.That(m_decorator.get_StringProp(m_waterPrefix.Hvo, headwordFlid).Text, Is.EqualTo("water-"));

			int mlHeadwordFlid = Cache.MetaDataCacheAccessor.GetFieldId2(LexEntryTags.kClassId, "MLHeadWord", false);
			Assert.That(m_decorator.get_MultiStringAlt(m_blank2.Hvo, mlHeadwordFlid, Cache.DefaultVernWs).Text, Is.EqualTo("blank1"));
			Assert.That(m_decorator.get_MultiStringAlt(m_water.Hvo, mlHeadwordFlid, Cache.DefaultVernWs).Text, Is.EqualTo("water"));
			Assert.That(m_decorator.get_MultiStringAlt(m_waterPrefix.Hvo, mlHeadwordFlid, Cache.DefaultVernWs).Text, Is.EqualTo("water-"));
		}

		/// <summary>
		/// Various lex references should be omitted if they don't have enough interesting content.
		/// </summary>
		[Test]
		public void IncompleteLexReferences()
		{
			int lexEntryRefsFlid = Cache.MetaDataCacheAccessor.GetFieldId2(LexEntryTags.kClassId, "LexEntryReferences", false);
			// There are two surviving synonyms so this one should survive.
			Assert.That(m_decorator.VecProp(m_bother.Hvo, lexEntryRefsFlid).Length, Is.EqualTo(1));

			// But only one of these synonyms survives so we hide the whole relationship
			Assert.That(m_decorator.VecProp(m_problem.Hvo, lexEntryRefsFlid).Length, Is.EqualTo(0));

			// Through the real cache we find two lexical relations involving 'arm'.
			Assert.That(Cache.DomainDataByFlid.get_VecSize(m_arm.Hvo, lexEntryRefsFlid), Is.EqualTo(2));
			// The first one (rooted at 'body') is eliminated because the first item is eliminated,
			// and it is the 'whole'.
			Assert.That(m_decorator.get_VecSize(m_arm.Hvo, lexEntryRefsFlid), Is.EqualTo(1));

			int lexSenseRefsFlid = Cache.MetaDataCacheAccessor.GetFieldId2(LexSenseTags.kClassId, "LexSenseReferences", false);
			Assert.That(Cache.DomainDataByFlid.get_VecSize(m_leg.SensesOS[0].Hvo, lexSenseRefsFlid), Is.EqualTo(1));
			Assert.That(m_decorator.get_VecSize(m_leg.SensesOS[0].Hvo, lexSenseRefsFlid), Is.EqualTo(0));
		}

		/// <summary>
		/// Lex entry refs should be omitted if they are for a not published complex form.
		/// </summary>
		[Test]
		public void VisibleComplexFormNotPublished()
		{
			Assert.That(m_hotArm.ComplexFormEntryRefs.Count(), Is.EqualTo(1),
				"Wrong number of Complex Form Entry Refs.");
			var complexRefsFlid = Cache.MetaDataCacheAccessor.GetFieldId2(
				LexEntryTags.kClassId, "VisibleComplexFormBackRefs", false);
			Assert.That(m_decorator.get_VecSize(m_hot.Hvo, complexRefsFlid),
				Is.EqualTo(0), "Decorator should have removed back reference to this Complex Form.");
		}

		/// <summary>
		/// Lex entry refs should be omitted if they are for a not published variant.
		/// </summary>
		[Test]
		public void VariantNotPublished()
		{
			Assert.That(m_nolanryan.VariantEntryRefs.Count(), Is.EqualTo(1),
				"Wrong number of Variant Entry Refs.");
			var variantRefsFlid = Cache.MetaDataCacheAccessor.GetFieldId2(
				LexEntryTags.kClassId, "VariantFormEntryBackRefs", false);
			Assert.That(m_decorator.get_VecSize(m_hot.Hvo, variantRefsFlid), Is.EqualTo(0),
				"Decorator should have removed back reference to this Variant.");
		}

		/// <summary>
		/// Lex entry refs should be omitted if they don't have enough interesting content.
		/// </summary>
		[Test]
		public void IncompleteLexEntryRefs()
		{
			Assert.That(m_hotBlank.ComplexFormEntryRefs.Count(), Is.EqualTo(1));
			int complexRefsFlid = Cache.MetaDataCacheAccessor.GetFieldId2(LexEntryTags.kClassId, "ComplexFormEntryRefs", false);
			Assert.That(m_decorator.get_VecSize(m_hotBlank.Hvo, complexRefsFlid), Is.EqualTo(0));
		}

		[Test]
		public void SpecialCases()
		{
			// Enhance JohnT: At some point we may want to intercept Sense.FullReferenceName, which uses HeadWord and
			// outline sense number. This is not currently necessary because, although it is marked as a Viewable property,
			// it is not currently used in the Dictionary view.

			// Don't show pictures of unpublished senses.
			int picsOfSensesFlid = Cache.MetaDataCacheAccessor.GetFieldId2(LexEntryTags.kClassId, "PicturesOfSenses", false);
			Assert.That(Cache.DomainDataByFlid.get_VecSize(m_hot.Hvo,picsOfSensesFlid), Is.EqualTo(1));
			Assert.That(m_decorator.get_VecSize(m_hot.Hvo, picsOfSensesFlid), Is.EqualTo(0));

			// Sense.LexSenseOutline: sense 2 of hot is not published.
			int senseOutlineFlid = Cache.MetaDataCacheAccessor.GetFieldId2(LexSenseTags.kClassId, "LexSenseOutline", false);
			Assert.That(Cache.DomainDataByFlid.get_StringProp(m_desirable.Hvo, senseOutlineFlid).Text, Is.EqualTo("3"));
			Assert.That(m_decorator.get_StringProp(m_desirable.Hvo, senseOutlineFlid).Text, Is.EqualTo("2"));
			Assert.That(Cache.DomainDataByFlid.get_StringProp(m_fastCar.Hvo, senseOutlineFlid).Text, Is.EqualTo("3.1"));
			Assert.That(m_decorator.get_StringProp(m_fastCar.Hvo, senseOutlineFlid).Text, Is.EqualTo("2.1"));

			// Sense.MLOwnerOutlineName
			int mlOwnerOutlineFlid = Cache.MetaDataCacheAccessor.GetFieldId2(LexSenseTags.kClassId, "MLOwnerOutlineName", false);
			Assert.That(Cache.DomainDataByFlid.get_MultiStringAlt(m_desirable.Hvo, mlOwnerOutlineFlid, Cache.DefaultVernWs).Text, Is.EqualTo("hot 3"));
			Assert.That(m_decorator.get_MultiStringAlt(m_desirable.Hvo, mlOwnerOutlineFlid, Cache.DefaultVernWs).Text, Is.EqualTo("hot 2"));
			Assert.That(Cache.DomainDataByFlid.get_MultiStringAlt(m_fastCar.Hvo, mlOwnerOutlineFlid, Cache.DefaultVernWs).Text, Is.EqualTo("hot 3.1"));
			Assert.That(m_decorator.get_MultiStringAlt(m_fastCar.Hvo, mlOwnerOutlineFlid, Cache.DefaultVernWs).Text, Is.EqualTo("hot 2.1"));
			Assert.That(Cache.DomainDataByFlid.get_MultiStringAlt(m_blank2.SensesOS[0].Hvo, mlOwnerOutlineFlid, Cache.DefaultVernWs).Text,
				Is.EqualTo("blank2"));
			Assert.That(m_decorator.get_MultiStringAlt(m_blank2.SensesOS[0].Hvo, mlOwnerOutlineFlid, Cache.DefaultVernWs).Text,
				Is.EqualTo("blank1"));

			// Entry.PublishAsMinorEntry
			int publishAsMinorEntryFlid = Cache.MetaDataCacheAccessor.GetFieldId2(LexEntryTags.kClassId, "PublishAsMinorEntry", false);
			Assert.That(Cache.DomainDataByFlid.get_BooleanProp(m_hotBlank.Hvo, publishAsMinorEntryFlid), Is.True);
			Assert.That(m_decorator.get_BooleanProp(m_hotBlank.Hvo, publishAsMinorEntryFlid), Is.False);
			Assert.That(m_decorator.get_BooleanProp(m_hotWater.Hvo, publishAsMinorEntryFlid), Is.True);
		}

		[Test]
		public void ShowAsHeadWord()
		{
			int headwordFlid = Cache.MetaDataCacheAccessor.GetFieldId2(LexEntryTags.kClassId, "HeadWord", false);
			// Sky should be excluded from the main entry list
			Assert.That(m_decorator.get_StringProp(m_sky.Hvo, headwordFlid).Text, Is.EqualTo("sky"), "sky not to be shown as main entry");

			// Blue should not be excluded from the main entry list
			Assert.That(m_decorator.get_StringProp(m_blueColor.Hvo, headwordFlid).Text, Is.EqualTo("blue1"), "blue should be avaialbe but isn't");

			// blue sky should not be excluded from the main entry list
			Assert.That(m_decorator.get_StringProp(m_blueSky.Hvo, headwordFlid).Text, Is.EqualTo("blue sky"), "blue sky should be avaialbe but isn't");
		}

		[Test]
		public void PropChanged()
		{
			var mockRoot = new MockNotifyChange();
			// When the root box asks the decorator to notify it, the decorator adds itself to the wrapped decorator instead.
			m_decorator.AddNotification(mockRoot);
			Assert.That(m_publisher.AddedNotification, Is.EqualTo(m_decorator));

			// Unknown flid PropChanged calls go right through.
			m_decorator.PropChanged(27, LexEntryTags.kflidLexemeForm, 10, 11, 12);
			VerifyPropChanged(mockRoot.LastPropChanged, 27, LexEntryTags.kflidLexemeForm, 10, 11, 12);

			// Flids we modify are overridden: a substitute PropChanged is sent, which does not know the
			// number deleted, but claims all current items are inserted.
			m_decorator.PropChanged(m_hot.Hvo, LexEntryTags.kflidSenses, 2, 1, 1);
			VerifyPropChanged(mockRoot.LastPropChanged, m_hot.Hvo, LexEntryTags.kflidSenses, 0, 2, 0);

			int lexEntryRefsFlid = Cache.MetaDataCacheAccessor.GetFieldId2(LexEntryTags.kClassId, "LexEntryReferences", false);
			m_decorator.PropChanged(m_arm.Hvo, lexEntryRefsFlid, 2, 1, 0);
			VerifyPropChanged(mockRoot.LastPropChanged, m_arm.Hvo, lexEntryRefsFlid, 0, 1, 0);

			int complexRefsFlid = Cache.MetaDataCacheAccessor.GetFieldId2(LexEntryTags.kClassId, "ComplexFormEntryRefs", false);
			m_decorator.PropChanged(m_hotBlank.Hvo, complexRefsFlid, 0, 0, 3);
			VerifyPropChanged(mockRoot.LastPropChanged, m_hotBlank.Hvo, complexRefsFlid, 0, 0, 0);

			// and when it asks to be removed, it removes itself.
			m_decorator.RemoveNotification(mockRoot);
			Assert.That(m_publisher.RemovedNotification, Is.EqualTo(m_decorator));
		}

		/// <summary>
		/// Test that things get updated by Refresh.
		/// </summary>
		[Test]
		public void Refresh()
		{
			UndoableUnitOfWorkHelper.Do("do", "undo", m_actionHandler,
				() =>
					{
						var goodWord = MakeEntry("good", "nice", false);
						var badSense = MakeSense(goodWord, "bad");
						badSense.DoNotPublishInRC.Add(m_mainDict);
						m_decorator.Refresh();
						Assert.That(m_decorator.get_VecSize(goodWord.Hvo, LexEntryTags.kflidSenses), Is.EqualTo(1));
					});
			// Get rid of it again: the underlying virtual list publisher is not aware of the new item,
			// which can mess up later tests.
			m_actionHandler.Undo();
			m_decorator.Refresh();
		}

		private void VerifyPropChanged(PropChangeInfo propChangeInfo, int hvo, int tag, int ivMin, int cvIns, int cvDel)
		{
			Assert.That(propChangeInfo.hvo, Is.EqualTo(hvo));
			Assert.That(propChangeInfo.tag, Is.EqualTo(tag));
			Assert.That(propChangeInfo.ivMin, Is.EqualTo(ivMin));
			Assert.That(propChangeInfo.cvIns, Is.EqualTo(cvIns));
			Assert.That(propChangeInfo.cvDel, Is.EqualTo(cvDel));
		}

		private class MockNotifyChange : IVwNotifyChange
		{
			public PropChangeInfo LastPropChanged { get; private set; }

			public void PropChanged(int hvo, int tag, int ivMin, int cvIns, int cvDel)
			{
				LastPropChanged = new PropChangeInfo() { hvo = hvo, tag = tag, ivMin = ivMin, cvIns = cvIns, cvDel = cvDel };
			}
		}

		ILexRefType MakeRefType(string name, string reverseName, int mapType)
		{
			if (Cache.LangProject.LexDbOA.ReferencesOA == null)
				Cache.LangProject.LexDbOA.ReferencesOA = m_possListFactory.Create();
			var result = m_lexRefTypeFactory.Create();
			Cache.LangProject.LexDbOA.ReferencesOA.PossibilitiesOS.Add(result);
			result.Name.AnalysisDefaultWritingSystem = AnalysisTss(name);
			if (reverseName != null)
				result.ReverseName.AnalysisDefaultWritingSystem = AnalysisTss(reverseName);
			result.MappingType = mapType;
			return result;
		}

		//IPartOfSpeech MakePos(string name)
		//{
		//    if (Cache.LangProject.LexDbOA.ReferencesOA == null)
		//        Cache.LangProject.LexDbOA.ReferencesOA = m_possListFactory.Create();
		//    var result = m_lexRefTypeFactory.Create();
		//    Cache.LangProject.LexDbOA.ReferencesOA.PossibilitiesOS.Add(result);
		//    result.Name.AnalysisDefaultWritingSystem = AnalysisTss(name);
		//    if (reverseName != null)
		//        result.ReverseName.AnalysisDefaultWritingSystem = AnalysisTss(reverseName);
		//    result.MappingType = mapType;
		//    return result;
		//}


		private ILexEntryRef MakeEntryRef(ILexEntry owner, ICmObject[] components, ICmObject[] primaryComponents, int type)
		{
			var result = m_lexEntryRefFactory.Create();
			owner.EntryRefsOS.Add(result);
			result.RefType = type;
			foreach (var obj in components)
				result.ComponentLexemesRS.Add(obj);
			foreach (var obj in primaryComponents)
				result.PrimaryLexemesRS.Add(obj);
			return result;
		}

		private ILexEntry MakeEntry(string form, string gloss, bool fExclude, bool hwExclude = false, bool suffix = false)
		{
			var entry = MakeEntry();
			var lexform = suffix ? MakeSuffix(entry) : MakeLexemeForm(entry);
			lexform.Form.VernacularDefaultWritingSystem = VernacularTss(form);
			MakeSense(entry, gloss);
			if (fExclude)
				entry.DoNotPublishInRC.Add(m_mainDict);
			if (hwExclude)
				entry.DoNotShowMainEntryInRC.Add(m_mainDict);
			return entry;
		}

		ILexExampleSentence MakeExample(ILexSense sense, string text, bool fExclude)
		{
			var result = m_exampleFactory.Create();
			sense.ExamplesOS.Add(result);
			result.Example.VernacularDefaultWritingSystem = VernacularTss(text);
			if (fExclude)
				result.DoNotPublishInRC.Add(m_mainDict);
			return result;
		}

		private ITsString VernacularTss(string form)
		{
			return TsStringUtils.MakeString(form, Cache.DefaultVernWs);
		}

		private ILexEntry MakeEntry()
		{
			var entry = m_entryFactory.Create();
			return entry;
		}

		private IMoStemAllomorph MakeLexemeForm(ILexEntry entry)
		{
			var form = Cache.ServiceLocator.GetInstance<IMoStemAllomorphFactory>().Create();
			entry.LexemeFormOA = form;
			entry.LexemeFormOA.MorphTypeRA =
				Cache.ServiceLocator.GetInstance<IMoMorphTypeRepository>().GetObject(MoMorphTypeTags.kguidMorphStem);
			return form;
		}

		private IMoForm MakeSuffix(ILexEntry entry)
		{
			var form = Cache.ServiceLocator.GetInstance<IMoAffixAllomorphFactory>().Create();
			entry.LexemeFormOA = form;
			entry.LexemeFormOA.MorphTypeRA =
				Cache.ServiceLocator.GetInstance<IMoMorphTypeRepository>().GetObject(MoMorphTypeTags.kguidMorphSuffix);
			return form;
		}

		private ILexSense MakeSense(ILexEntry owningEntry, string gloss)
		{
			var sense = m_senseFactory.Create();
			owningEntry.SensesOS.Add(sense);
			sense.Gloss.AnalysisDefaultWritingSystem = AnalysisTss(gloss);
			return sense;
		}

		private ITsString AnalysisTss(string form)
		{
			return TsStringUtils.MakeString(form, Cache.DefaultAnalWs);
		}

		private ILexSense MakeSense(ILexSense owningSense, string gloss)
		{
			var sense = m_senseFactory.Create();
			owningSense.SensesOS.Add(sense);
			sense.Gloss.AnalysisDefaultWritingSystem = AnalysisTss(gloss);
			return sense;
		}
	}

	class MockPublisher : ObjectListPublisher
	{
		public MockPublisher(ISilDataAccessManaged domainDataByFlid, int flid) : base(domainDataByFlid, flid)
		{
		}

		public IVwNotifyChange AddedNotification { get; set; }

		public override void AddNotification(IVwNotifyChange nchng)
		{
			AddedNotification = nchng;
		}

		public IVwNotifyChange RemovedNotification { get; set; }

		public override void RemoveNotification(IVwNotifyChange nchng)
		{
			RemovedNotification = nchng;
		}
		// Expose the protected SendPropChanged so we can simulate PropChanged coming up from the domain.
		public void DoSendPropChanged(int hvo, int tag, int ivMin, int cvIns, int cvDel)
		{
			SendPropChanged(hvo, tag, ivMin, cvIns, cvDel);
		}
	}
	public struct PropChangeInfo
	{
		public int hvo;
		public int tag;
		public int ivMin;
		public int cvIns;
		public int cvDel;
	}
}
