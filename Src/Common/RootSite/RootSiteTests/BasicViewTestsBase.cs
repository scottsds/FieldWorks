// Copyright (c) 2003-2018 SIL International
// This software is licensed under the LGPL, version 2.1 or later
// (http://www.gnu.org/licenses/lgpl-2.1.html)

using System.Diagnostics;
using NUnit.Framework;
using SIL.LCModel;
using SIL.LCModel.DomainServices;
using SIL.PlatformUtilities;

namespace SIL.FieldWorks.Common.RootSites
{
	/// ----------------------------------------------------------------------------------------
	/// <summary>
	/// Base class for tests that use <see cref="DummyBasicView"/>
	/// </summary>
	/// ----------------------------------------------------------------------------------------
	public class BasicViewTestsBase : ScrInMemoryLcmTestBase
	{
		/// <summary>The draft form</summary>
		protected DummyBasicView m_basicView;
		/// <summary></summary>
		protected int m_hvoRoot;
		/// <summary>Derived class needs to initialize this with something useful</summary>
		protected int m_flidContainingTexts;
		/// <summary>Fragment for view constructor</summary>
		protected int m_frag = 1;

		/// ------------------------------------------------------------------------------------
		/// <summary>
		///
		/// </summary>
		/// <returns></returns>
		/// ------------------------------------------------------------------------------------
		protected virtual DummyBasicView CreateDummyBasicView()
		{
			return new DummyBasicView(m_hvoRoot, m_flidContainingTexts);
		}

		/// -----------------------------------------------------------------------------------
		/// <summary>
		/// Create a new basic view
		/// </summary>
		/// -----------------------------------------------------------------------------------
		[SetUp]
		public override void TestSetup()
		{
			base.TestSetup();

			var styleSheet = new LcmStyleSheet();
			styleSheet.Init(Cache, Cache.LangProject.Hvo, LangProjectTags.kflidStyles);

			Debug.Assert(m_basicView == null, "m_basicView is not null.");
			//if (m_basicView != null)
			//	m_basicView.Dispose();
			m_basicView = CreateDummyBasicView();
			m_basicView.Cache = Cache;
			m_basicView.Visible = false;
			m_basicView.StyleSheet = styleSheet;
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Shuts down the view
		/// </summary>
		/// <remarks>This method is called after each test</remarks>
		/// ------------------------------------------------------------------------------------
		[TearDown]
		public override void  TestTearDown()
		{
			// FdoTestBase::TestTeardown uses m_actionHandler which seems to
			// require its associated RootBox to have a valid root site.
			// This m_basicView needs to be disposed after FdoTestBase::TestTeardown is called.
			base.TestTearDown();

			if (m_basicView != null)
				m_basicView.Dispose();
			m_basicView = null;
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Set up the test form.
		/// </summary>
		/// <param name="display"></param>
		/// ------------------------------------------------------------------------------------
		protected virtual void ShowForm(DummyBasicViewVc.DisplayType display)
		{
			// TODO-Linux: This value works better, given mono differences. Possibly look into this further.
			int height = Platform.IsMono ? 300 : 307 - 25;

			ShowForm(display, height);
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Set up the test form.
		/// </summary>
		/// <param name="display"></param>
		/// <param name="height"></param>
		/// ------------------------------------------------------------------------------------
		protected virtual void ShowForm(DummyBasicViewVc.DisplayType display, int height)
		{
			Assert.IsTrue(m_flidContainingTexts != 0, "Need to initialize m_flidContainingTexts");

			m_basicView.DisplayType = display;

			// We don't actually want to show it, but we need to force the view to create the root
			// box and lay it out so that various test stuff can happen properly.
			m_basicView.Width = 300;
			m_basicView.Height = height;
			m_basicView.MakeRoot(m_hvoRoot, m_flidContainingTexts, m_frag);
			m_basicView.CallLayout();
		}
	}
}
