// Copyright (c) 2004-2018 SIL International
// This software is licensed under the LGPL, version 2.1 or later
// (http://www.gnu.org/licenses/lgpl-2.1.html)

using System.Collections.Generic;
using System.Diagnostics;
using NUnit.Framework;
using SIL.LCModel;
using SIL.LCModel.DomainServices;
using SIL.LCModel.Utils;

namespace ParatextImport
{
	#region class DummyBookMerger
	/// ------------------------------------------------------------------------------------
	/// <summary>
	/// For testing
	/// </summary>
	/// ------------------------------------------------------------------------------------
	public class DummyBookMerger : BookMerger
	{
		private VerseIteratorForSetOfStTexts m_VerseIteratorForSetOfStTexts;
		private VerseIteratorForStText m_VerseIteratorForStText;

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Initializes a new instance of the <see cref="DummyBookMerger"/> class.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		public DummyBookMerger(LcmCache cache, IScrBook book)
			: base(cache, book)
		{
		}

		/// <summary/>
		protected override void Dispose(bool fDisposing)
		{
			if (fDisposing)
			{
				if (m_VerseIteratorForSetOfStTexts != null)
					m_VerseIteratorForSetOfStTexts.Dispose();
				if (m_VerseIteratorForStText != null)
					m_VerseIteratorForStText.Dispose();
			}
			m_VerseIteratorForSetOfStTexts = null;
			m_VerseIteratorForStText = null;
			base.Dispose(fDisposing);
		}

		/// --------------------------------------------------------------------------------
		/// <summary>
		/// Expose the InitPrevVerseForBook method, for the current book, for testing.
		/// </summary>
		/// --------------------------------------------------------------------------------
		public void InitPrevVerseForBookCurr()
		{
			InitPrevVerseForBook(BookCurr, out m_endOfPrevVerseCurr);
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Gets a value indicating whether to display the progress dialog.
		/// </summary>
		/// <value>Always <c>false</c> in our tests.</value>
		/// ------------------------------------------------------------------------------------
		protected override bool DisplayUi
		{
			get { return false; }
		}

		/// --------------------------------------------------------------------------------
		/// <summary>
		/// Expose the DetectDifferencesInListOfStTexts for testing.
		/// </summary>
		/// <param name="stTextsCurr"></param>
		/// <param name="stTextsRev"></param>
		/// --------------------------------------------------------------------------------
		new public void DetectDifferencesInListOfStTexts(List<IStText> stTextsCurr, List<IStText> stTextsRev)
		{
			// init our cluster differences list
			ClusterDiffs = new List<Difference>();

			base.DetectDifferencesInListOfStTexts(stTextsCurr, stTextsRev);

			// copy differences to the master list, where it's easy to verify them
			Differences.Clear();
			Differences.AddRange(ClusterDiffs);
		}

		/// --------------------------------------------------------------------------------
		/// <summary>
		///
		/// </summary>
		/// <param name="progressDlg"></param>
		/// --------------------------------------------------------------------------------
		new public void DetectDifferences(IThreadedProgress progressDlg)
		{
			base.DetectDifferences(new DummyProgressDlg());
		}

		/// --------------------------------------------------------------------------------
		/// <summary>
		/// Recheck for differences. Useful in ReplaceCurWithRev tests.
		/// You could call DetectDifferences() directly to do the recheck, but his method
		/// provides cleaner error reporting in case the test didn't revert all
		/// original diffs first.
		/// </summary>
		/// --------------------------------------------------------------------------------
		public void DetectDifferences_ReCheck()
		{
			// the caller should have already reviewed all diffs
			Assert.AreEqual(0, Differences.Count);

			// re-init our output list, for a fresh start
			Differences.Clear();

			base.DetectDifferences(new DummyProgressDlg());
		}

		/// --------------------------------------------------------------------------------
		/// <summary>
		/// Create an instance of the Book Merger's VerseIteratorForSetOfStTexts
		///   class, for testing.
		/// </summary>
		/// <param name="list">the given IStText list to create an iterator for</param>
		/// --------------------------------------------------------------------------------
		public void CreateVerseIteratorForSetOfStTexts(List<IStText> list)
		{
			if (m_VerseIteratorForSetOfStTexts != null)
				m_VerseIteratorForSetOfStTexts.Dispose();
			m_VerseIteratorForSetOfStTexts = new VerseIteratorForSetOfStTexts(list);
		}

		/// --------------------------------------------------------------------------------
		/// <summary>
		/// Exposes the NextVerseInStText method of the VerseIteratorForSetOfStTexts, for testing
		/// </summary>
		/// <returns>the next ScrVerse in the set of StTexts</returns>
		/// --------------------------------------------------------------------------------
		public ScrVerse NextVerseInSet()
		{
			return m_VerseIteratorForSetOfStTexts.NextVerse();
		}

		/// --------------------------------------------------------------------------------
		/// <summary>
		/// Create an instance of the Book Merger's VerseIterator class, for testing.
		/// </summary>
		/// <param name="txt">the given IStText to create an iterator for</param>
		/// --------------------------------------------------------------------------------
		public void CreateVerseIteratorForStText(IStText txt)
		{
			if (m_VerseIteratorForStText != null)
				m_VerseIteratorForStText.Dispose();
			m_VerseIteratorForStText = new VerseIteratorForStText(txt);
		}

		/// --------------------------------------------------------------------------------
		/// <summary>
		/// Exposes the NextVerseInStText method of the VerseIterator, for testing
		/// </summary>
		/// <returns>the next ScrVerse in the IStText</returns>
		/// --------------------------------------------------------------------------------
		public ScrVerse NextVerseInStText()
		{
			return m_VerseIteratorForStText.NextVerse();
		}

		/// --------------------------------------------------------------------------------
		/// <summary>
		/// Calls FirstVerseForStText in the book merger.
		/// </summary>
		/// <param name="text">IStText from which we want to get the first verse.</param>
		/// --------------------------------------------------------------------------------
		public ScrVerse CallFirstVerseForStText(IStText text)
		{
			ScrVerse prevVerse = null;
			using (VerseIteratorForStText iterator = new VerseIteratorForStText(text))
				return FirstVerseForStText(iterator, ref prevVerse);
		}

		/// --------------------------------------------------------------------------------
		/// <summary>
		/// Sets a list of differences to be used in the BookMerger for test purposes.
		/// </summary>
		/// --------------------------------------------------------------------------------
		public List<Difference> TestDiffList
		{
			set
			{
				Differences.Clear();
				Differences.AddRange(value);
				if (DiffHashTable == null)
				{
					m_allDifferences = Differences;
					m_diffHashTable = BuildDiffLookupTable();
				}
			}
		}
	}
	#endregion

	/// <summary/>
	[TestFixture]
	public class BookMergerTestsBase: ScrInMemoryLcmTestBase
	{
		#region Member variables
		/// <summary/>
		protected IScrBook m_genesis;
		/// <summary/>
		protected IScrBook m_genesisRevision;
		/// <summary/>
		protected DummyBookMerger m_bookMerger;
		#endregion

		#region Setup
		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Create Genesis and a revision of Genesis, and create the BookMerger.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		[SetUp]
		public override void TestSetup()
		{
			base.TestSetup();

			// init the DummyBookMerger
			Debug.Assert(m_bookMerger == null, "m_bookMerger is not null.");
			m_bookMerger = new DummyBookMerger(Cache, m_genesisRevision);
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Shuts down the cache.
		/// </summary>
		/// <remarks>This method is called after each test</remarks>
		/// ------------------------------------------------------------------------------------
		[TearDown]
		public override void TestTearDown()
		{
			m_bookMerger.Dispose();
			m_bookMerger = null;
			m_genesis = null;
			m_genesisRevision = null;

			base.TestTearDown();
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Overridden to only create a book with no content, heading, title, etc.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		protected override void CreateTestData()
		{
			m_genesis = AddBookToMockedScripture(1, "Genesis");
			m_genesisRevision = AddArchiveBookToMockedScripture(1, "Genesis");

			//Philemon
			IScrBook book = AddBookToMockedScripture(57, "Philemon");
			AddTitleToMockedBook(book, "Philemon");

			IScrSection section = AddSectionToMockedBook(book);
			AddSectionHeadParaToSection(section, "Paul tells people", "Section Head");
			IScrTxtPara para = AddParaToMockedSectionContent(section, "Paragraph");
			AddRunToMockedPara(para, "1", "Chapter Number");
			AddRunToMockedPara(para, "1", "Verse Number");
			AddRunToMockedPara(para, "and the earth was without form and void and darkness covered the face of the deep", null);

			IScrSection section2 = AddSectionToMockedBook(book);
			AddSectionHeadParaToSection(section2, "Paul tells people more", "Section Head");
			IScrTxtPara para2 = AddParaToMockedSectionContent(section2, "Paragraph");
			AddRunToMockedPara(para2, "2", "Chapter Number");
			AddRunToMockedPara(para2, "1", "Verse Number");
			AddRunToMockedPara(para2, "paul expounds on the nature of reality", null);
			IScrTxtPara para3 = AddParaToMockedSectionContent(section2, "Paragraph");
			AddRunToMockedPara(para3, "2", "Verse Number");
			AddRunToMockedPara(para3, "the existentialists are all wrong", null);

			//Jude
			IScrBook jude = AddBookToMockedScripture(65, "Jude");
			IScrSection judeSection = AddSectionToMockedBook(jude);
			AddTitleToMockedBook(jude, "Jude");
			AddSectionHeadParaToSection(judeSection, "Introduction", "Section Head");
			IScrTxtPara judePara = AddParaToMockedSectionContent(judeSection, "Intro Paragraph");
			AddRunToMockedPara(judePara, "The Letter from Jude was written to warn against" +
				" false teachers who claimed to be believers. In this brief letter, which is similar in" +
				" content to 2 Peter the writer encourages his readers to fight on for the faith which" +
				" once and for all God has given to his people.", null);
		}
		#endregion

	}
}
