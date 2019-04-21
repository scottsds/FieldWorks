// Copyright (c) 2007-2018 SIL International
// This software is licensed under the LGPL, version 2.1 or later
// (http://www.gnu.org/licenses/lgpl-2.1.html)

using System.Collections.Generic;
using System.Diagnostics;
using NUnit.Framework;
using SIL.FieldWorks.Common.FwUtils;
using SIL.LCModel;
using SIL.LCModel.Utils;
using SIL.LCModel.DomainServices;
using SIL.LCModel.Infrastructure;
using SIL.LCModel.Core.Scripture;
using SIL.LCModel.Core.Text;
using SIL.LCModel.Core.WritingSystems;
using SIL.LCModel.Core.KernelInterfaces;

namespace ParatextImport.ImportTests
{
	/// ----------------------------------------------------------------------------------------
	/// <summary>
	/// Base class for several import test classes
	/// </summary>
	/// ----------------------------------------------------------------------------------------
	public class ParatextImportTestsBase : ScrInMemoryLcmTestBase
	{
		#region Member variables
		/// <summary></summary>
		protected DummyParatextImporter m_importer;
		/// <summary></summary>
		protected int m_wsVern; // writing system info needed by tests
		/// <summary></summary>
		protected ITsTextProps m_ttpVernWS; // simple run text props expected by tests
		private static ITsStrBldr s_strBldr;
		/// <summary></summary>
		protected BCVRef m_titus;
		/// <summary></summary>
		protected LcmStyleSheet m_styleSheet;
		/// <summary></summary>
		protected IScrImportSet m_settings;
		/// <summary></summary>
		protected int m_wsAnal;
		/// <summary></summary>
		protected ITsTextProps m_ttpAnalWS;
		#endregion
		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Creates a Back Translation for the stuff in Exodus with the following layout:
		///
		///			()
		///		   BT Heading 1
		///	BT Intro text
		///		   BT Heading 2
		///	(1)1BT Verse one.
		///
		///	(1) = chapter number 1
		/// </summary>
		/// ------------------------------------------------------------------------------------
		protected void CreatePartialExodusBT(int wsAnal)
		{
			IScrBook book = m_scr.FindBook(2);
			IScrSection section = book.SectionsOS[0];
			IStTxtPara para = section.HeadingOA[0];
			ICmTranslation trans = para.GetOrCreateBT();
			AddRunToMockedTrans(trans, wsAnal, "BT Heading 1", null);

			para = section.ContentOA[0];
			trans = para.GetOrCreateBT();
			AddRunToMockedTrans(trans, wsAnal, "BT Intro text", null);

			section = book.SectionsOS[1];
			para = section.HeadingOA[0];
			trans = para.GetOrCreateBT();
			AddRunToMockedTrans(trans, wsAnal, "BT Heading 2", null);

			para = section.ContentOA[0];
			trans = para.GetOrCreateBT();
			AddRunToMockedTrans(trans, wsAnal, "1", ScrStyleNames.ChapterNumber);
			AddRunToMockedTrans(trans, wsAnal, "1", ScrStyleNames.VerseNumber);
			AddRunToMockedTrans(trans, wsAnal, "BT Verse one.", null);
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Creates a Back Translation for the stuff in Exodus with the following layout:
		///
		///		    (BT Title)
		///		   BT Heading 1
		///	BT Intro text
		///		   BT Heading 2
		///	(1)1BT Verse one.2BT Verse two.
		///	3BT Verse three.More BT of verse three.
		///	4BT Verse four.5BT Verse five.
		///		   BT Heading 3
		///	6BT Verse six.7BT Verse seven.
		///
		///	(1) = chapter number 1
		/// </summary>
		/// ------------------------------------------------------------------------------------
		protected void CreateExodusBT(int wsAnal)
		{
			CreatePartialExodusBT(wsAnal);

			IScrBook book = m_scr.FindBook(2);
			// Finish book title translation
			IStTxtPara para = book.TitleOA[0];
			ICmTranslation trans = para.GetOrCreateBT();
			AddRunToMockedTrans(trans, wsAnal, "BT Title", null);

			// Finish section two translation
			IScrSection section = book.SectionsOS[1];
			para = section.ContentOA[0];
			trans = para.GetOrCreateBT();
			AddRunToMockedTrans(trans, wsAnal, "2", ScrStyleNames.VerseNumber);
			AddRunToMockedTrans(trans, wsAnal, "BT Verse two.", null);

			para = section.ContentOA[1];
			trans = para.GetOrCreateBT();
			AddRunToMockedTrans(trans, wsAnal, "3", ScrStyleNames.VerseNumber);
			AddRunToMockedTrans(trans, wsAnal, "BT Verse three.", null);
			AddRunToMockedTrans(trans, wsAnal, "More BT verse three.", null);

			para = section.ContentOA[2];
			trans = para.GetOrCreateBT();
			AddRunToMockedTrans(trans, wsAnal, "4", ScrStyleNames.VerseNumber);
			AddRunToMockedTrans(trans, wsAnal, "BT Verse four.", null);
			AddRunToMockedTrans(trans, wsAnal, "5", ScrStyleNames.VerseNumber);
			AddRunToMockedTrans(trans, wsAnal, "BT Verse five.", null);

			// Finish section three translation
			section = book.SectionsOS[2];
			para = section.ContentOA[0];
			trans = para.GetOrCreateBT();
			AddRunToMockedTrans(trans, wsAnal, "6", ScrStyleNames.VerseNumber);
			AddRunToMockedTrans(trans, wsAnal, "BT Verse six.", null);
			AddRunToMockedTrans(trans, wsAnal, "7", ScrStyleNames.VerseNumber);
			AddRunToMockedTrans(trans, wsAnal, "BT Verse seven.", null);
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Creates a book (Leviticus) with 2 sections with the following layout:
		/// Leviticus
		/// Heading 1
		/// (1)1Verse one.
		/// Heading 2
		/// (2)1Verse one.2Verse two.
		/// (empty heading)
		/// (3)1Verse one.
		///
		/// Numbers in () are chapter numbers.
		/// </summary>
		/// <returns>the book of Leviticus for testing</returns>
		/// ------------------------------------------------------------------------------------
		protected IScrBook CreateLeviticusData()
		{
			return CreateBook(3, "Leviticus");
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Creates a book with 2 sections with the following layout:
		/// bookName
		/// Heading 1
		/// (1)1Verse one.
		/// Heading 2
		/// (2)1Verse one.2Verse two.
		/// (3)1Verse one.
		///
		/// Numbers in () are chapter numbers.
		/// </summary>
		/// <returns>the book for testing</returns>
		/// ------------------------------------------------------------------------------------
		protected IScrBook CreateBook(int nBookNumber, string bookName)
		{
			IScrBook book = AddBookToMockedScripture(nBookNumber, bookName);
			AddTitleToMockedBook(book, bookName);
			IScrSection section1 = AddSectionToMockedBook(book);
			AddSectionHeadParaToSection(section1, "Heading 1", ScrStyleNames.SectionHead);
			IStTxtPara para11 = AddParaToMockedSectionContent(section1, ScrStyleNames.NormalParagraph);
			AddRunToMockedPara(para11, "1", ScrStyleNames.ChapterNumber);
			AddRunToMockedPara(para11, "1", ScrStyleNames.VerseNumber);
			AddRunToMockedPara(para11, "Verse one.", null);

			IScrSection section2 = AddSectionToMockedBook(book);
			AddSectionHeadParaToSection(section2, "Heading 2", ScrStyleNames.SectionHead);
			IStTxtPara para21 = AddParaToMockedSectionContent(section2, ScrStyleNames.NormalParagraph);
			AddRunToMockedPara(para21, "2", ScrStyleNames.ChapterNumber);
			AddRunToMockedPara(para21, "1", ScrStyleNames.VerseNumber);
			AddRunToMockedPara(para21, "Verse one.", null);
			AddRunToMockedPara(para21, "2", ScrStyleNames.VerseNumber);
			AddRunToMockedPara(para21, "Verse two.", null);
			IStTxtPara para22 = AddParaToMockedSectionContent(section2, ScrStyleNames.NormalParagraph);
			AddRunToMockedPara(para22, "3", ScrStyleNames.ChapterNumber);
			AddRunToMockedPara(para22, "1", ScrStyleNames.VerseNumber);
			AddRunToMockedPara(para22, "Verse one.", null);

			return book;
		}

		#region Setup/Teardown
		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Test fixture setup
		/// </summary>
		/// ------------------------------------------------------------------------------------
		public override void FixtureSetup()
		{
			base.FixtureSetup();

			// Add Kalaba and set it as the default vernacular writing system.
			CoreWritingSystemDefinition xkalWs;
			Cache.ServiceLocator.WritingSystemManager.GetOrSet("qaa-x-kal", out xkalWs);
			NonUndoableUnitOfWorkHelper.Do(m_actionHandler, () =>
			{
				Cache.ServiceLocator.WritingSystems.VernacularWritingSystems.Add(xkalWs);
				Cache.ServiceLocator.WritingSystems.CurrentVernacularWritingSystems.Clear();
				Cache.ServiceLocator.WritingSystems.CurrentVernacularWritingSystems.Add(xkalWs);
			});
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Initialize the importer
		/// </summary>
		/// ------------------------------------------------------------------------------------
		public override void TestSetup()
		{
			base.TestSetup();

			m_styleSheet = new LcmStyleSheet();
			m_styleSheet.Init(Cache, Cache.LangProject.Hvo, LangProjectTags.kflidStyles);
			InitWsInfo();

			DummyParatextImporter.s_translatorNoteDefn = Cache.ServiceLocator.GetInstance<ICmAnnotationDefnRepository>().TranslatorAnnotationDefn;
			DummyParatextImporter.s_consultantNoteDefn = Cache.ServiceLocator.GetInstance<ICmAnnotationDefnRepository>().ConsultantAnnotationDefn;

			m_titus = new BCVRef(56001001);
			m_settings = m_scr.FindOrCreateDefaultImportSettings(TypeOfImport.Other, m_styleSheet, FwDirectoryFinder.FlexStylesPath);
			m_settings.StartRef = m_titus;
			m_settings.EndRef = m_titus;
			m_settings.ImportTranslation = true;
			InitializeImportSettings();

			m_actionHandler.EndUndoTask(); // Let the importer handle the undo/redo
			m_importer = new DummyParatextImporter(m_settings, this, m_styleSheet);
			m_importer.Initialize();
			m_importer.UndoInfo.StartImportingFiles();
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Initializes import settings (mappings and options) for "Other" type of import.
		/// Individual test fixtures can override for other types of import.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		protected virtual void InitializeImportSettings()
		{
			DummyParatextImporter.MakeSFImportTestSettings(m_settings);
			m_settings.ImportBackTranslation = false;
			m_settings.ImportBookIntros = true;
			m_settings.ImportAnnotations = false;
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// </summary>
		/// ------------------------------------------------------------------------------------
		[TearDown]
		public override void TestTearDown()
		{
			if (m_actionHandler.CurrentDepth > 0)
				m_importer.UndoInfo.DoneImportingFiles(true);
			m_importer.Dispose();
			m_importer = null;
			m_styleSheet = null;
			m_settings = null;

			// Restart an undo task so we don't crash :)
			m_actionHandler.BeginUndoTask("bla", "bla");
			base.TestTearDown();
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Init writing system info and some props needed by some tests.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		protected void InitWsInfo()
		{
			Debug.Assert(Cache != null);

			// get writing system info needed by tests
			m_wsVern = Cache.DefaultVernWs;
			m_wsAnal = Cache.DefaultAnalWs;

			// init simple run text props expected by tests
			ITsPropsBldr tsPropsBldr = TsStringUtils.MakePropsBldr();
			tsPropsBldr.SetIntPropValues((int)FwTextPropType.ktptWs, 0, m_wsVern);
			m_ttpVernWS = tsPropsBldr.GetTextProps();
			tsPropsBldr.SetIntPropValues((int)FwTextPropType.ktptWs, 0, m_wsAnal);
			m_ttpAnalWS = tsPropsBldr.GetTextProps();
		}
		#endregion

		#region Properties
		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// returns the default vernacular writing system
		/// </summary>
		/// ------------------------------------------------------------------------------------
		protected int DefaultVernWs
		{
			get { return m_wsVern; }
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// returns the default analysis writing system
		/// </summary>
		/// ------------------------------------------------------------------------------------
		protected int DefaultAnalWs
		{
			get { return m_wsAnal; }
		}
		#endregion

		#region Helper functions for tests
		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Tests that the given run of m_importer.NormalParaStrBldr contains the specified text
		/// and properties. This method assumes vernacular WS.
		/// </summary>
		/// <param name="iRun">zero-based run index</param>
		/// <param name="text">Expected run text</param>
		/// <param name="charStyleName">character style name, or null if expecting default
		/// paragraph character props</param>
		/// ------------------------------------------------------------------------------------
		protected void VerifyBldrRun(int iRun, string text, string charStyleName)
		{
			VerifyBldrRun(iRun, text, charStyleName, m_wsVern, m_importer.NormalParaStrBldr);
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Tests that the given run of m_importer.NormalParaStrBldr contains the specified text
		/// and properties.
		/// </summary>
		/// <param name="iRun">zero-based run index</param>
		/// <param name="text">Expected run text</param>
		/// <param name="charStyleName">character style name, or null if expecting default
		/// paragraph character props</param>
		/// <param name="wsExpected">expected writing system for the run</param>
		/// ------------------------------------------------------------------------------------
		protected void VerifyBldrRun(int iRun, string text, string charStyleName, int wsExpected)
		{
			VerifyBldrRun(iRun, text, charStyleName, wsExpected, m_importer.NormalParaStrBldr);
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Tests that the given run of the given ITsStrBldr contains the specified text
		/// and properties.
		/// </summary>
		/// <param name="iRun">zero-based run index</param>
		/// <param name="text">Expected run text</param>
		/// <param name="charStyleName">character style name, or null if expecting default
		/// paragraph character props</param>
		/// <param name="wsExpected">expected writing system for the run</param>
		/// <param name="strBldr">the string builder in which to verify the run</param>
		/// ------------------------------------------------------------------------------------
		protected void VerifyBldrRun(int iRun, string text, string charStyleName, int wsExpected,
			ITsStrBldr strBldr)
		{
			s_strBldr = strBldr;

			Assert.AreEqual(text, s_strBldr.get_RunText(iRun));
			ITsTextProps ttpExpected = CharStyleTextProps(charStyleName, wsExpected);
			ITsTextProps ttpRun = s_strBldr.get_Properties(iRun);
			string sWhy;
			if (!TsTextPropsHelper.PropsAreEqual(ttpExpected, ttpRun, out sWhy))
				Assert.Fail(sWhy);
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Get the requested footnote.
		/// </summary>
		/// <param name="iFootnoteIndex">zero-based footnote index</param>
		/// ------------------------------------------------------------------------------------
		protected IStFootnote GetFootnote(int iFootnoteIndex)
		{
			return m_importer.GetFootnote(iFootnoteIndex);
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Tests that the processing of a footnote has resulted in the the ORC (Object
		/// Replacement Character) being inserted in the proper place in the dummy
		/// importer's NormalParaStrBldr.
		/// </summary>
		/// <param name="iRun">Zero-base index of the run that should contain the ORC</param>
		/// <param name="iFootnoteIndex">zero-based footnote index</param>
		/// ------------------------------------------------------------------------------------
		protected void VerifyBldrFootnoteOrcRun(int iRun, int iFootnoteIndex)
		{
			ITsTextProps orcProps =
				m_importer.NormalParaStrBldr.get_Properties(iRun);
			IStFootnote footnote = GetFootnote(iFootnoteIndex);

			string objData = orcProps.GetStrPropValue((int)FwTextPropType.ktptObjData);
			Assert.AreEqual((char)(int)FwObjDataTypes.kodtOwnNameGuidHot, objData[0]);
			// Send the objData string without the first character because the first character
			// is the object replacement character and the rest of the string is the GUID.
			Assert.AreEqual(footnote.Guid, MiscUtils.GetGuidFromObjData(objData.Substring(1)));
			string sOrc = m_importer.NormalParaStrBldr.get_RunText(iRun);
			Assert.AreEqual(StringUtils.kChObject, sOrc[0]);
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Tests that a footnote has been created in the DB with the given number of runs,
		/// the first of which has the specified text, using the default footnote properties.
		/// </summary>
		/// <param name="iFootnoteIndex">zero-based footnote index</param>
		/// <param name="sFirstFootnoteSegment">Expected footnote contents</param>
		/// <param name="runCount">Number of runs expected in the footnote para</param>
		/// ------------------------------------------------------------------------------------
		protected ITsString VerifyComplexFootnote(int iFootnoteIndex, string sFirstFootnoteSegment,
			int runCount)
		{
			return VerifySimpleFootnote(iFootnoteIndex, sFirstFootnoteSegment, iFootnoteIndex, "a",
				ScrStyleNames.NormalFootnoteParagraph, runCount);
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Tests that a footnote has been created in the DB with a single default run having
		/// the specified text, using the default footnote properties.
		/// </summary>
		/// <param name="iFootnoteIndex">zero-based footnote index</param>
		/// <param name="sFootnoteSegment">Expected footnote contents</param>
		/// ------------------------------------------------------------------------------------
		protected void VerifySimpleFootnote(int iFootnoteIndex, string sFootnoteSegment)
		{
			VerifySimpleFootnote(iFootnoteIndex, sFootnoteSegment, "a");
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Tests that a footnote has been created in the DB with a single default run having
		/// the specified text.
		/// </summary>
		/// <param name="iFootnoteIndex">zero-based footnote index</param>
		/// <param name="sFootnoteSegment">Expected footnote contents</param>
		/// <param name="sMarker">One of: <c>"a"</c>, for automatic alpha sequence; <c>"*"</c>,
		/// for a literal marker (we always use "*" in these tests); or <c>string.Empty</c>,
		/// for no marker</param>
		/// ------------------------------------------------------------------------------------
		protected void VerifySimpleFootnote(int iFootnoteIndex, string sFootnoteSegment,
			string sMarker)
		{
			VerifySimpleFootnote(iFootnoteIndex, sFootnoteSegment, iFootnoteIndex, sMarker,
				ScrStyleNames.NormalFootnoteParagraph, 1);
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Tests that a footnote has been created in the DB with a single default run having
		/// the specified text.
		/// </summary>
		/// <param name="iFootnoteIndex">zero-based footnote index</param>
		/// <param name="sFootnoteSegment">Expected footnote contents</param>
		/// <param name="iAutoNumberedFootnoteIndex">zero-based index of this footnote
		/// in list of all auto-numbered footnotes. This could the be the third footnote
		/// overall, but only the second auto-numbered footnote.</param>
		/// <param name="sMarker">One of: <c>"a"</c>, for automatic alpha sequence; <c>"*"</c>,
		/// for a literal marker (we always use "*" in these tests); or <c>string.Empty</c>,
		/// for no marker</param>
		/// <param name="sParaStyleName">Name of the paragraph style</param>
		/// <param name="runCount">Number of runs expected in the footnote para</param>
		/// ------------------------------------------------------------------------------------
		protected ITsString VerifySimpleFootnote(int iFootnoteIndex, string sFootnoteSegment,
			int iAutoNumberedFootnoteIndex, string sMarker, string sParaStyleName, int runCount)
		{
			return m_importer.VerifySimpleFootnote(iFootnoteIndex, sFootnoteSegment, iAutoNumberedFootnoteIndex,
				sMarker, sParaStyleName, runCount);
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Tests that a footnote marker ORC (Object Replacement Character) has been inserted
		/// as the iRunth run in the given TsString (vernacular).
		/// </summary>
		/// <param name="tssPara">TsString to check</param>
		/// <param name="iRun">Zero-based index of run to check for the ORC</param>
		/// ------------------------------------------------------------------------------------
		protected void VerifyFootnoteMarkerOrcRun(ITsString tssPara, int iRun)
		{
			VerifyFootnoteMarkerOrcRun(tssPara, iRun, DefaultVernWs, false);
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Tests that a footnote marker ORC (Object Replacement Character) has been inserted
		/// as the iRunth run in the given TsString.
		/// </summary>
		/// <param name="tssPara">TsString to check</param>
		/// <param name="iRun">Zero-based index of run to check for the ORC</param>
		/// <param name="ws">Writing system that should be used for the ORC</param>
		/// <param name="fBT">Indicates whether this is checking a back translation.</param>
		/// ------------------------------------------------------------------------------------
		public static void VerifyFootnoteMarkerOrcRun(ITsString tssPara, int iRun, int ws,
			bool fBT)
		{
			Debug.Assert(tssPara.RunCount > iRun, "Trying to access run #" + iRun +
				" when there are only " + tssPara.RunCount + " run(s).");
			string sOrcRun = tssPara.get_RunText(iRun);
			Assert.AreEqual(1, sOrcRun.Length);
			Assert.AreEqual(StringUtils.kChObject, sOrcRun[0]);
			ITsTextProps ttpOrcRun = tssPara.get_Properties(iRun);
			int nDummy;
			int wsActual = ttpOrcRun.GetIntPropValues((int)FwTextPropType.ktptWs,
				out nDummy);
			Assert.AreEqual(ws, wsActual, "Wrong writing system for footnote marker in text");
			string objData = ttpOrcRun.GetStrPropValue((int)FwTextPropType.ktptObjData);
			FwObjDataTypes orcType = (fBT) ? FwObjDataTypes.kodtNameGuidHot :
				FwObjDataTypes.kodtOwnNameGuidHot;
			Assert.AreEqual((char)(int)orcType, objData[0]);
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Tests that a footnote has been created in the DB with a single default run having
		/// the specified text and the given translation.
		/// </summary>
		/// <param name="iFootnoteIndex">zero-based footnote index</param>
		/// <param name="sFootnoteSegment">Expected footnote contents</param>
		/// <param name="sFootnoteTransSegment">Expected footnote translation contents</param>
		/// <param name="sMarker">One of: <c>"a"</c>, for automatic alpha sequence; <c>"*"</c>,
		/// for a literal marker (we always use "*" in these tests); or <c>string.Empty</c>,
		/// for no marker</param>
		/// <param name="sParaStyleName">Name of the paragraph style</param>
		/// ------------------------------------------------------------------------------------
		public void VerifyFootnoteWithTranslation(int iFootnoteIndex, string sFootnoteSegment,
			string sFootnoteTransSegment, string sMarker, string sParaStyleName)
		{
			// Force reload of book
			IStFootnote footnote = m_importer.GetFootnote(iFootnoteIndex);
			if (sMarker == "a")
				sMarker = new string((char)((int)'a' + (iFootnoteIndex % 26)), 1);
			if (sMarker != null)
			{
				AssertEx.RunIsCorrect(footnote.FootnoteMarker, 0,
					sMarker, ScrStyleNames.FootnoteMarker, m_wsVern);
			}
			else
			{
				Assert.IsNull(footnote.FootnoteMarker.Text);
			}
			ILcmOwningSequence<IStPara> footnoteParas = footnote.ParagraphsOS;
			Assert.AreEqual(1, footnoteParas.Count);
			IStTxtPara para = (IStTxtPara)footnoteParas[0];
			Assert.AreEqual(StyleUtils.ParaStyleTextProps(sParaStyleName), para.StyleRules);
			ITsString tss = para.Contents;
			Assert.AreEqual(1, tss.RunCount);
			AssertEx.RunIsCorrect(tss, 0, sFootnoteSegment, null, m_wsVern);
			// Check Translation
			if (sFootnoteTransSegment != null)
			{
				Assert.AreEqual(1, para.TranslationsOC.Count);
				ICmTranslation trans = para.GetBT();
				tss = trans.Translation.AnalysisDefaultWritingSystem;
				Assert.AreEqual(1, tss.RunCount);
				AssertEx.RunIsCorrect(tss, 0, sFootnoteTransSegment, null, m_wsAnal);
			}
			else
			{
				Assert.AreEqual(1, para.TranslationsOC.Count);
				Assert.IsNull(para.GetBT().Translation.AnalysisDefaultWritingSystem.Text);
			}
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Tests that a picture has been created in the DB with a single default run having
		/// the specified text.
		/// </summary>
		/// <param name="para">paragraph that owns the picture</param>
		/// <param name="iPictureIndex">zero-based picture index</param>
		/// <param name="sPictureCaption">Expected picture caption</param>
		/// <param name="sPictureTransCaption">Expected picture caption translation</param>
		/// ------------------------------------------------------------------------------------
		public void VerifyPictureWithTranslation(IStTxtPara para, int iPictureIndex,
			string sPictureCaption, string sPictureTransCaption)
		{
			List<ICmPicture> pictures = para.GetPictures();
			ICmPicture picture = pictures[iPictureIndex];
			Assert.AreEqual(sPictureCaption, picture.Caption.VernacularDefaultWritingSystem.Text);
			Assert.AreEqual(sPictureTransCaption, picture.Caption.AnalysisDefaultWritingSystem.Text);
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Tests that a section has been properly added to the given ScrBook, including the
		/// section's heading and contents objects.
		/// </summary>
		/// <param name="book">the ScrBook object to be tested</param>
		/// <param name="iSection">index of the new section (must be the last section in book)
		/// </param>
		/// ------------------------------------------------------------------------------------
		protected void VerifyNewSectionExists(IScrBook book, int iSection)
		{
			Assert.AreEqual(iSection + 1, book.SectionsOS.Count);
			if (iSection < 0)
				return;

			Assert.IsTrue(book.SectionsOS[iSection].HeadingOA.IsValidObject);
			Assert.AreEqual(book.SectionsOS[iSection].HeadingOA,
				m_importer.SectionHeading); //empty section heading so far
			Assert.IsTrue(book.SectionsOS[iSection].ContentOA.IsValidObject);
			Assert.AreEqual(book.SectionsOS[iSection].ContentOA,
				m_importer.SectionContent); //empty section contents
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Returns run textprops representing a character style of the given name using the
		/// given writing system.
		/// </summary>
		/// <param name="styleName">The character style name for which to create props</param>
		/// <param name="ws">The writing system to use for the props</param>
		/// <returns>requested text props</returns>
		/// <remarks>If styleName is not given, the resulting props contains only the
		/// given writing system.</remarks>
		/// <remarks>For char style, props should contain ws (writing system) and char style
		/// name. Without a char style, props should contain ws only.</remarks>
		/// ------------------------------------------------------------------------------------
		protected ITsTextProps CharStyleTextProps(string styleName, int ws)
		{
			// If no style name, return simple ws props
			if (styleName == null || styleName.Length == 0)
			{
				if (ws == m_wsVern)
				{
					// for minor performance gain, use the vern ws props we've already built
					return m_ttpVernWS;
				}
			}

			// Return props for the given char style name
			return StyleUtils.CharStyleTextProps(styleName, ws);
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Returns run textprops representing a character style of the given name using the
		/// vernacular writing system.
		/// </summary>
		/// <param name="styleName">The character style name for which to create props</param>
		/// <returns>requested text props</returns>
		/// <remarks>If styleName is not given, the resulting props contains only the
		/// vernacular writing system.</remarks>
		/// <remarks>For char style, props should contain ws (writing system) and char style
		/// name. Without a char style, props should contain ws only.</remarks>
		/// ------------------------------------------------------------------------------------
		protected ITsTextProps CharStyleTextProps(string styleName)
		{
			return CharStyleTextProps(styleName, m_wsVern);
		}
		#endregion
	}
}
