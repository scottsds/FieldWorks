// Copyright (c) 2003-2013 SIL International
// This software is licensed under the LGPL, version 2.1 or later
// (http://www.gnu.org/licenses/lgpl-2.1.html)
//
// File: DummyBasicView.cs
// Responsibility: Eberhard Beilharz

using System;
using System.Drawing;
using System.Runtime.InteropServices;
using System.Windows.Forms;
using SIL.LCModel.Core.Text;
using SIL.LCModel.Core.KernelInterfaces;
using SIL.LCModel;
using SIL.FieldWorks.Common.ViewsInterfaces;

namespace SIL.FieldWorks.Common.RootSites
{
	/// ---------------------------------------------------------------------------------------
	/// <summary>
	/// Implementation of a basic view for testing, similar to DraftView
	/// </summary>
	/// ---------------------------------------------------------------------------------------
	public class DummyBasicView : RootSite
	{
		#region Testing EditingHelper
		/// ------------------------------------------------------------------------------------
		/// <summary>
		///
		/// </summary>
		/// ------------------------------------------------------------------------------------
		public class DummyEditingHelper: RootSiteEditingHelper
		{
			internal IVwSelection m_mockedSelection = null;
			internal bool m_fOverrideGetParaPropStores = false;

			/// -----------------------------------------------------------------------------------
			/// <summary>
			/// Construct one.
			/// </summary>
			/// <param name="cache"></param>
			/// <param name="callbacks"></param>
			/// -----------------------------------------------------------------------------------
			public DummyEditingHelper(LcmCache cache, IEditingCallbacks callbacks)
				: base(cache, callbacks)
			{
			}

			/// -----------------------------------------------------------------------------------
			/// <summary>
			/// Overriden so that it works if we don't display the view
			/// </summary>
			/// <returns>Returns <c>true</c> if pasting is possible.</returns>
			/// -----------------------------------------------------------------------------------
			public override bool CanPaste()
			{
				CheckDisposed();

				bool fVisible = Control.Visible;
				Control.Visible = true;
				bool fReturn = base.CanPaste();
				Control.Visible = fVisible;
				return fReturn;
			}

			/// --------------------------------------------------------------------------------
			/// <summary>
			/// Gets the selection from the root box that is currently being edited (can be null).
			/// </summary>
			/// --------------------------------------------------------------------------------
			public override IVwSelection RootBoxSelection
			{
				get { return m_mockedSelection ?? base.RootBoxSelection; }
			}

			/// ------------------------------------------------------------------------------------
			/// <summary>
			/// Gets an array of property stores, one for each paragraph in the given selection.
			/// </summary>
			/// <param name="vwsel">The selection.</param>
			/// <param name="vqvps">The property stores.</param>
			/// ------------------------------------------------------------------------------------
			protected override void GetParaPropStores(IVwSelection vwsel,
				out IVwPropertyStore[] vqvps)
			{
				if (m_fOverrideGetParaPropStores)
					vqvps = new IVwPropertyStore[1];
				else
					base.GetParaPropStores(vwsel, out vqvps);
			}

			/// ------------------------------------------------------------------------------------
			/// <summary>
			/// Gets the caption props.
			/// </summary>
			/// ------------------------------------------------------------------------------------
			public override ITsTextProps CaptionProps
			{
				get
				{
					CheckDisposed();
					ITsPropsBldr bldr = TsStringUtils.MakePropsBldr();
					bldr.SetStrPropValue((int)FwTextPropType.ktptNamedStyle, "Figure caption");
					return bldr.GetTextProps();
				}
			}
		}
		#endregion

		#region Data members
		/// <summary></summary>
		protected System.ComponentModel.IContainer components;
		/// <summary></summary>
		protected VwBaseVc m_basicViewVc;
		/// <summary></summary>
		protected SelectionHelper m_SelectionHelper;

		///// <summary>HVO of dummy root object</summary>
		//public const int kHvoRoot = 1001;
		/// <summary>Text for the first and third test paragraph (English)</summary>
		internal const string kFirstParaEng = "This is the first test paragraph.";
		/// <summary>Text for the second and fourth test paragraph (English).</summary>
		/// <remarks>This text needs to be shorter than the text for the first para!</remarks>
		internal const string kSecondParaEng = "This is the 2nd test paragraph.";
		private int m_hvoRoot;
		private int m_flid;

		private bool m_fSkipLayout = false;
		private DummyBasicViewVc.DisplayType m_displayType;
		private Point m_scrollPosition = new Point(0,0);
		#endregion

		#region Constructor, Dispose, InitializeComponent

		/// <summary>
		/// Initializes a new instance of the DraftView class
		/// </summary>
		public DummyBasicView() : base(null)
		{
			// This call is required by the Windows.Forms Form Designer.
			InitializeComponent();
		}

		/// <summary>
		/// Initializes a new instance of the DraftView class
		/// </summary>
		public DummyBasicView(int hvoRoot, int flid) : base(null)
		{
			m_hvoRoot = hvoRoot;
			m_flid = flid;

			// This call is required by the Windows.Forms Form Designer.
			InitializeComponent();
		}

		/// <summary>
		/// Clean up any resources being used.
		/// </summary>
		protected override void Dispose(bool disposing)
		{
			// Must not be run more than once.
			if (IsDisposed)
				return;

			base.Dispose(disposing);

			if (disposing)
			{
				if (components != null)
				{
					components.Dispose();
				}
				var disposable = m_basicViewVc as IDisposable;
				if (disposable != null)
					disposable.Dispose();
			}
			m_basicViewVc = null;
			m_SelectionHelper = null;
		}

		#region Component Designer generated code
		/// <summary>
		/// Required method for Designer support - do not modify
		/// the contents of this method with the code editor.
		/// </summary>
		private void InitializeComponent()
		{
			this.SuspendLayout();
			//
			// DummyBasicView
			//
			this.Name = "DummyBasicView";
			this.ResumeLayout(false);

		}
		#endregion
		#endregion

		#region Event handling methods
		/// -----------------------------------------------------------------------------------
		/// <summary>
		/// Activates the view
		/// </summary>
		/// -----------------------------------------------------------------------------------
		public virtual void ActivateView()
		{
			CheckDisposed();

			PerformLayout();
			Show();
			Focus();
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Simulate a scroll to end
		/// </summary>
		/// ------------------------------------------------------------------------------------
		public override void ScrollToEnd()
		{
			CheckDisposed();

			base.ScrollToEnd();
			m_rootb.MakeSimpleSel(false, true, false, true);
			PerformLayout();
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Simulate scrolling to top
		/// </summary>
		/// ------------------------------------------------------------------------------------
		public override void ScrollToTop()
		{
			CheckDisposed();

			base.ScrollToTop();
			// The actual DraftView code for handling Ctrl-Home doesn't contain this method call.
			// The call to CallOnExtendedKey() in OnKeyDown() handles setting the IP.
			m_rootb.MakeSimpleSel(true, true, false, true);
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Calls the OnKeyDown method.
		/// </summary>
		/// <param name="e">The <see cref="T:System.Windows.Forms.KeyEventArgs"/> instance
		/// containing the event data.</param>
		/// ------------------------------------------------------------------------------------
		public void CallOnKeyDown(KeyEventArgs e)
		{
			base.OnKeyDown(e);
		}
		#endregion

		#region Overrides of Control methods
		/// -----------------------------------------------------------------------------------
		/// <summary>
		/// Focus got set to the draft view
		/// </summary>
		/// <param name="e">The event data</param>
		/// -----------------------------------------------------------------------------------
		protected override void OnGotFocus(EventArgs e)
		{
			base.OnGotFocus(e);

			if (DesignMode || !m_fRootboxMade)
				return;

			if (m_SelectionHelper != null)
			{
				m_SelectionHelper.SetSelection(this);
				m_SelectionHelper = null;
			}
		}

		/// <summary>
		/// For AdjustScrollRangeTestHScroll we need the dummy to allow horizontal scrolling.
		/// </summary>
		protected override bool WantHScroll
		{
			get
			{
				return true;
			}
		}

		/// -----------------------------------------------------------------------------------
		/// <summary>
		/// Recompute the layout
		/// </summary>
		/// <param name="levent"></param>
		/// -----------------------------------------------------------------------------------
		protected override void OnLayout(LayoutEventArgs levent)
		{
			if (!m_fSkipLayout)
				base.OnLayout(levent);
		}
		#endregion

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Call the OnLayout methods
		/// </summary>
		/// ------------------------------------------------------------------------------------
		public virtual void CallLayout()
		{
			CheckDisposed();

			OnLayout(new LayoutEventArgs(this, string.Empty));
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Add English paragraphs
		/// </summary>
		/// ------------------------------------------------------------------------------------
		public void MakeEnglishParagraphs()
		{
			CheckDisposed();

			ILgWritingSystemFactory wsf = m_cache.WritingSystemFactory;
			int wsEng = wsf.GetWsFromStr("en");
			AddParagraphsToLangProj(wsEng, kFirstParaEng, kSecondParaEng);
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Adds paragraphs to the database
		/// </summary>
		/// <param name="ws"></param>
		/// <param name="firstPara"></param>
		/// <param name="secondPara"></param>
		/// ------------------------------------------------------------------------------------
		private void AddParagraphsToLangProj(int ws, string firstPara, string secondPara)
		{
			var servLoc = Cache.ServiceLocator;
			var txt = servLoc.GetInstance<ITextFactory>().Create();
			//Cache.LanguageProject.TextsOC.Add(txt);
			var text = servLoc.GetInstance<IStTextFactory>().Create();
			txt.ContentsOA = text;
			var stTxtParaFactory = servLoc.GetInstance<IStTxtParaFactory>();
			var para1 = stTxtParaFactory.Create();
			text.ParagraphsOS.Add(para1);
			var para2 = stTxtParaFactory.Create();
			text.ParagraphsOS.Add(para2);

			ITsString tss = TsStringUtils.MakeString(firstPara, ws);
			para1.Contents = tss;

			tss = TsStringUtils.MakeString(secondPara, ws);
			para2.Contents = tss;
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Exposes the OnKillFocus method to testing.
		/// </summary>
		/// <param name="newWindow">The new window.</param>
		/// ------------------------------------------------------------------------------------
		public void KillFocus(Control newWindow)
		{
			CheckDisposed();

			OnKillFocus(newWindow, true);
		}

		#region Overrides of RootSite
		/// -----------------------------------------------------------------------------------
		/// <summary>
		/// Since we don't really show this window, it doesn't have a working AutoScrollPosition;
		/// but to test making the selection visible, we have to remember what the view tries to
		/// change it to.
		/// </summary>
		/// -----------------------------------------------------------------------------------
		public override Point ScrollPosition
		{
			get
			{
				CheckDisposed();

				return (m_group == null || this == m_group.ScrollingController ?
					m_scrollPosition : m_group.ScrollingController.ScrollPosition);
			}
			set
			{
				CheckDisposed();

				if (m_group == null || this == m_group.ScrollingController)
					m_scrollPosition = new Point(-value.X, -value.Y);
				else
					m_group.ScrollingController.ScrollPosition = value;
			}
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Provides direct access to the base's MakeRoot() method.
		/// </summary>
		/// <remarks>This is needed in derived classes. See
		/// Test\AcceptanceTests\Common\RootSite\DummyDraftView.cs</remarks>
		/// ------------------------------------------------------------------------------------
		protected void BaseMakeRoot()
		{
			base.MakeRoot();
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Makes a root box and initializes it with appropriate data
		/// </summary>
		/// <param name="hvoRoot">Hvo of the root object</param>
		/// <param name="flid">Flid in which hvoRoot contains a sequence of StTexts</param>
		/// ------------------------------------------------------------------------------------
		public void MakeRoot(int hvoRoot, int flid)
		{
			CheckDisposed();

			MakeRoot(hvoRoot, flid, 1);
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Makes a root box and initializes it with appropriate data
		/// </summary>
		/// <param name="hvoRoot">Hvo of the root object</param>
		/// <param name="flid">Flid in which hvoRoot contains a sequence of StTexts</param>
		/// <param name="frag">Fragment for view constructor</param>
		/// ------------------------------------------------------------------------------------
		public void MakeRoot(int hvoRoot, int flid, int frag)
		{
			CheckDisposed();

			if (m_cache == null || DesignMode)
				return;

			base.MakeRoot();

			// Set up a new view constructor.
			m_basicViewVc = CreateVc(flid);
			m_basicViewVc.DefaultWs = m_cache.ServiceLocator.WritingSystems.DefaultVernacularWritingSystem.Handle;

			m_rootb.DataAccess = m_cache.DomainDataByFlid;
			m_rootb.SetRootObject(hvoRoot, m_basicViewVc, frag, m_styleSheet);

			m_fRootboxMade = true;
			m_dxdLayoutWidth = kForceLayout;
			// Don't try to draw until we get OnSize and do layout.

			// Added this to keep from Asserting if the user tries to scroll the draft window
			// before clicking into it to place the insertion point.
			try
			{
				m_rootb.MakeSimpleSel(true, true, false, true);
			}
			catch (COMException)
			{
				// We ignore failures since the text window may be empty, in which case making a
				// selection is impossible.
			}
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Creates the view constructor.
		/// </summary>
		/// <param name="flid">The flid.</param>
		/// <returns>The view constructor</returns>
		/// ------------------------------------------------------------------------------------
		protected virtual VwBaseVc CreateVc(int flid)
		{
			return new DummyBasicViewVc(m_displayType, flid);
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Makes a root box and initializes it with appropriate data
		/// </summary>
		/// ------------------------------------------------------------------------------------
		public override void MakeRoot()
		{
			CheckDisposed();

			MakeRoot(m_hvoRoot, m_flid); //, DummyBasicViewVc.kflidTestDummy);
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Creates a new DummyEditingHelper
		/// </summary>
		/// ------------------------------------------------------------------------------------
		protected override EditingHelper  CreateEditingHelper()
		{
			return new DummyEditingHelper(m_cache, this);
		}
		#endregion

		#region Properties
		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Gets or sets the type of boxes to display: lazy or non-lazy or both
		/// </summary>
		/// ------------------------------------------------------------------------------------
		public DummyBasicViewVc.DisplayType DisplayType
		{
			get
			{
				CheckDisposed();
				return m_displayType;
			}
			set
			{
				CheckDisposed();
				m_displayType = value;
			}
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Gets the draft view's selection helper object
		/// </summary>
		/// ------------------------------------------------------------------------------------
		public SelectionHelper SelectionHelper
		{
			get
			{
				CheckDisposed();
				return m_SelectionHelper;
			}
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Gets or sets a flag if OnLayout should be skipped.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		public bool SkipLayout
		{
			get
			{
				CheckDisposed();
				return m_fSkipLayout;
			}
			set
			{
				CheckDisposed();
				m_fSkipLayout = value;
			}
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Gets the view constructor.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		public VwBaseVc ViewConstructor
		{
			get
			{
				CheckDisposed();
				return m_basicViewVc;
			}
		}
		#endregion


		/// ------------------------------------------------------------------------------------
		/// <summary>
		///
		/// </summary>
		/// <param name="e"></param>
		/// ------------------------------------------------------------------------------------
		public void CallRootSiteOnKeyDown(KeyEventArgs e)
		{
			CheckDisposed();

			this.OnKeyDown(e);
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Check for presence of proper paragraph properties.
		/// </summary>
		/// <param name="vwsel">[out] The selection</param>
		/// <param name="hvoText">[out] The HVO</param>
		/// <param name="tagText">[out] The tag</param>
		/// <param name="vqvps">[out] The paragraph properties</param>
		/// <param name="ihvoAnchor">[out] Start index of selection</param>
		/// <param name="ihvoEnd">[out] End index of selection</param>
		/// <returns>Return <c>false</c> if neither selection nor paragraph property. Otherwise
		/// return <c>true</c>.</returns>
		/// ------------------------------------------------------------------------------------
		public bool IsParagraphProps(out IVwSelection vwsel, out int hvoText,
			out int tagText, out IVwPropertyStore[] vqvps, out int ihvoAnchor, out int ihvoEnd)
		{
			CheckDisposed();

			vwsel = null;
			hvoText = 0;
			tagText = 0;
			vqvps = null;
			ihvoAnchor = 0;
			ihvoEnd = 0;

			return EditingHelper.IsParagraphProps(out vwsel, out hvoText, out tagText, out vqvps,
				out ihvoAnchor, out ihvoEnd);
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Get the view selection and paragraph properties.
		/// </summary>
		/// <param name="vwsel">[out] The selection</param>
		/// <param name="hvoText">[out] The HVO</param>
		/// <param name="tagText">[out] The tag</param>
		/// <param name="vqvps">[out] The paragraph properties</param>
		/// <param name="ihvoFirst">[out] Start index of selection</param>
		/// <param name="ihvoLast">[out] End index of selection</param>
		/// <param name="vqttp">[out] The style rules</param>
		/// <returns>Return false if there is neither a selection nor a paragraph property.
		/// Otherwise return true.</returns>
		/// ------------------------------------------------------------------------------------
		public bool GetParagraphProps(out IVwSelection vwsel, out int hvoText,
			out int tagText, out IVwPropertyStore[] vqvps, out int ihvoFirst, out int ihvoLast,
			out ITsTextProps[] vqttp)
		{
			CheckDisposed();
			return EditingHelper.GetParagraphProps(out vwsel, out hvoText, out tagText,
				out vqvps, out ihvoFirst, out ihvoLast, out vqttp);
		}

		/// -----------------------------------------------------------------------------------
		/// <summary>
		/// Handle a key press.
		/// </summary>
		/// <param name="keyChar">The pressed character key</param>
		/// -----------------------------------------------------------------------------------
		public void HandleKeyPress(char keyChar)
		{
			CheckDisposed();

			using (new HoldGraphics(this))
			{
				EditingHelper.HandleKeyPress(keyChar, ModifierKeys);
			}
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Provides access to <see cref="SimpleRootSite.GetCoordRects"/>.
		/// </summary>
		/// <param name="rcSrcRoot"></param>
		/// <param name="rcDstRoot"></param>
		/// ------------------------------------------------------------------------------------
		public new void GetCoordRects(out Rectangle rcSrcRoot, out Rectangle rcDstRoot)
		{
			CheckDisposed();

			rcSrcRoot = Rectangle.Empty;
			rcDstRoot = Rectangle.Empty;
			base.GetCoordRects(out rcSrcRoot, out rcDstRoot);
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Provides access to <see cref="SimpleRootSite.AdjustScrollRange1"/>.
		/// </summary>
		/// <param name="dxdSize"></param>
		/// <param name="dxdPosition"></param>
		/// <param name="dydSize"></param>
		/// <param name="dydPosition"></param>
		/// <returns></returns>
		/// ------------------------------------------------------------------------------------
		public bool AdjustScrollRange(int dxdSize, int dxdPosition, int dydSize,
			int dydPosition)
		{
			CheckDisposed();

			return base.AdjustScrollRange1(dxdSize, dxdPosition, dydSize, dydPosition);
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Provides access to <see cref="ScrollableControl.VScroll"/>
		/// </summary>
		/// ------------------------------------------------------------------------------------
		public new bool VScroll
		{
			get
			{
				CheckDisposed();
				return base.VScroll;
			}
			set
			{
				CheckDisposed();
				base.VScroll = value;
			}
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Provides access to <see cref="ScrollableControl.HScroll"/>
		/// </summary>
		/// ------------------------------------------------------------------------------------
		public new bool HScroll
		{
			get
			{
				CheckDisposed();
				return base.HScroll;
			}
			set
			{
				CheckDisposed();
				base.HScroll = value;
			}
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Gets the height of the selection.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		public int SelectionHeight
		{
			get
			{
				CheckDisposed();

				int nLineHeight = 0;
				using(new HoldGraphics(this))
				{
					Rectangle rcSrcRoot;
					Rectangle rcDstRoot;
					GetCoordRects(out rcSrcRoot, out rcDstRoot);

					Rect rdIP;
					Rect rdSecondary;
					bool fSplit;
					bool fEndBeforeAnchor;
					IVwSelection vwsel = m_rootb.Selection;
					if (vwsel != null)
					{
						vwsel.Location(m_graphicsManager.VwGraphics, rcSrcRoot, rcDstRoot, out rdIP,
							out rdSecondary, out fSplit, out fEndBeforeAnchor);
						nLineHeight = rdIP.bottom - rdIP.top;
					}
				}

				return nLineHeight;
			}
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Gets the width of the selection.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		public int SelectionWidth
		{
			get
			{
				CheckDisposed();

				int nSelWidth = 0;
				using(new HoldGraphics(this))
				{
					Rectangle rcSrcRoot;
					Rectangle rcDstRoot;
					GetCoordRects(out rcSrcRoot, out rcDstRoot);

					Rect rdIP;
					Rect rdSecondary;
					bool fSplit;
					bool fEndBeforeAnchor;
					IVwSelection vwsel = m_rootb.Selection;
					if (vwsel != null)
					{
						vwsel.Location(m_graphicsManager.VwGraphics, rcSrcRoot, rcDstRoot, out rdIP,
							out rdSecondary, out fSplit, out fEndBeforeAnchor);
						nSelWidth = rdIP.right - rdIP.left;
					}
				}

				return nSelWidth;
			}
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Provides access to EditingHelper.ApplyStyle
		/// </summary>
		/// <param name="sStyleToApply"></param>
		/// ------------------------------------------------------------------------------------
		public void ApplyStyle(string sStyleToApply)
		{
			CheckDisposed();

			EditingHelper.ApplyStyle(sStyleToApply);
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Provides access to <see cref="SimpleRootSite.OnMouseDown"/>
		/// </summary>
		/// <param name="e"></param>
		/// ------------------------------------------------------------------------------------
		public void CallOnMouseDown(MouseEventArgs e)
		{
			CheckDisposed();

			base.OnMouseDown(e);
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Provides access to <see cref="SimpleRootSite.OnMouseUp"/>
		/// </summary>
		/// <param name="e"></param>
		/// ------------------------------------------------------------------------------------
		public void CallOnMouseUp(MouseEventArgs e)
		{
			CheckDisposed();

			base.OnMouseUp(e);
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		///
		/// </summary>
		/// <param name="rootb"></param>
		/// ------------------------------------------------------------------------------------
		public void SetRootBox(IVwRootBox rootb)
		{
			CheckDisposed();

			m_rootb = rootb;
		}
	}
}
