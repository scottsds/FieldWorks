// Copyright (c) 2010-2017 SIL International
// This software is licensed under the LGPL, version 2.1 or later
// (http://www.gnu.org/licenses/lgpl-2.1.html)

using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Drawing;
using System.Linq;
using System.Windows.Forms;

using SIL.LCModel.Core.WritingSystems;
using SIL.FieldWorks.Common.Controls;
using SIL.FieldWorks.Common.FwUtils;
using SIL.FieldWorks.Common.RootSites;
using SIL.LCModel;
using SIL.LCModel.Infrastructure;
using SIL.FieldWorks.FwCoreDlgs;

namespace SIL.FieldWorks.LexText.Controls
{
	/// ----------------------------------------------------------------------------------------
	/// <summary>
	///
	/// </summary>
	/// ----------------------------------------------------------------------------------------
	public partial class AddWritingSystemButton : Button
	{
		LcmCache m_cache;
		private HashSet<string> m_existingWsIds;
		public event EventHandler WritingSystemAdded;
		CoreWritingSystemDefinition m_wsNew;
		private IHelpTopicProvider m_helpTopicProvider;
		private IApp m_app;

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Initializes a new instance of the <see cref="AddWritingSystemButton"/> class.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		public AddWritingSystemButton()
		{
			InitializeComponent();
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Constructor
		/// </summary>
		/// ------------------------------------------------------------------------------------
		public AddWritingSystemButton(IContainer container)
		{
			container.Add(this);
			InitializeComponent();
		}

		/// <summary>
		/// Check to see if the object has been disposed.
		/// All public Properties and Methods should call this
		/// before doing anything else.
		/// </summary>
		public void CheckDisposed()
		{
			if (IsDisposed)
				throw new ObjectDisposedException(String.Format("'{0}' in use after being disposed.", GetType().Name));
		}

		/// <summary>
		/// Initialize for adding new writing systems during import.
		/// </summary>
		/// <param name="cache">primary FDO data cache</param>
		/// <param name="helpTopicProvider">The help topic provider.</param>
		/// <param name="app">The app.</param>
		/// <param name="wss">The writing systems already displayed.</param>
		public void Initialize(LcmCache cache, IHelpTopicProvider helpTopicProvider, IApp app,
			 IEnumerable<CoreWritingSystemDefinition> wss)
		{
			CheckDisposed();
			m_cache = cache;
			m_helpTopicProvider = helpTopicProvider;
			m_app = app;
			m_existingWsIds = new HashSet<string>(wss.Select(ws => ws.Id).ToList());
		}

		/// <summary>
		/// Initialize for adding new writing systems.
		/// </summary>
		internal void Initialize(LcmCache cache, IHelpTopicProvider helpTopicProvider, IApp app)
		{
			Initialize(cache, helpTopicProvider, m_app, cache.ServiceLocator.WritingSystems.AllWritingSystems);
		}

		protected override void OnPaint(PaintEventArgs e)
		{
			base.OnPaint(e);

			// add the 'drop down' arrow to the text
			using (var p = new Pen(SystemColors.ControlText, 1))
			{
				int x = Width - 14; // 7 wide at top, and 7 in from right boundry
				int y = Height / 2 - 2; // up 2 past the mid point
				// 4 lines: len 7, len 5, len 3 and len 1
				e.Graphics.DrawLine(p, x, y, x + 7, y);
				e.Graphics.DrawLine(p, x + 1, y + 1, x + 1 + 5, y + 1);
				e.Graphics.DrawLine(p, x + 2, y + 2, x + 2 + 3, y + 2);
				e.Graphics.DrawLine(p, x + 3, y + 3, x + 3 + 1, y + 3);
			}
		}

		protected override void OnClick(EventArgs e)
		{
			base.OnClick(e);

			m_wsNew = null;

			// show the menu to select which type of writing system to create
			var mnuAddWs = components.ContextMenu("mnuAddWs");

				// look like the "Add" button on the WS properties dlg
				List<CoreWritingSystemDefinition> xmlWs = GetOtherWritingSystems();
				var xmlWsV = new MenuItem[xmlWs.Count + 2]; // one for Vernacular
				var xmlWsA = new MenuItem[xmlWs.Count + 2]; // one for Analysis
				for (int i = 0; i < xmlWs.Count; i++)
				{
					CoreWritingSystemDefinition ws = xmlWs[i];
					xmlWsV[i] = new MenuItem(ws.DisplayLabel, mnuAddWS_Vern);
					xmlWsA[i] = new MenuItem(ws.DisplayLabel, mnuAddWS_Anal);
					xmlWsV[i].Tag = ws;
					xmlWsA[i].Tag = ws;
				}
				xmlWsV[xmlWs.Count] = new MenuItem("-");
				xmlWsV[xmlWs.Count + 1] = new MenuItem(LexTextControls.ks_DefineNew_, mnuAddWS_Vern);
				xmlWsA[xmlWs.Count] = new MenuItem("-");
				xmlWsA[xmlWs.Count + 1] = new MenuItem(LexTextControls.ks_DefineNew_, mnuAddWS_Anal);

				// have to have separate lists
				mnuAddWs.MenuItems.Add(LexTextControls.ks_VernacularWS, xmlWsV);
				mnuAddWs.MenuItems.Add(LexTextControls.ks_AnalysisWS, xmlWsA);

				mnuAddWs.Show(this, new Point(0, Height));
			}

		private List<CoreWritingSystemDefinition> GetOtherWritingSystems()
		{
			return m_cache.ServiceLocator.WritingSystemManager.OtherWritingSystems.
				Where(ws => !m_existingWsIds.Contains(ws.Id)).OrderBy(ws => ws.DisplayLabel).ToList();
		}

		private void mnuAddWS_Vern(object sender, EventArgs e)
		{
			CommonAddWS(false, (sender as MenuItem));
		}

		private void mnuAddWS_Anal(object sender, EventArgs e)
		{
			CommonAddWS(true, (sender as MenuItem));
		}

		private void CommonAddWS(bool isAnalysis, MenuItem selectedMI)
		{
			CoreWritingSystemDefinition ws = null;

			if (selectedMI.Text == LexTextControls.ks_DefineNew_)
			{
				IEnumerable<CoreWritingSystemDefinition> newWritingSystems;
				if (WritingSystemPropertiesDialog.ShowNewDialog(FindForm(), m_cache, m_cache.ServiceLocator.WritingSystemManager,
					m_cache.ServiceLocator.WritingSystems, m_helpTopicProvider, m_app, true, null,
					out newWritingSystems))
				{
					ws = newWritingSystems.First();
				}
			}
			else
			{
				ws = selectedMI.Tag as CoreWritingSystemDefinition;
			}

			if (ws != null)
			{
				m_wsNew = ws;
				// now add the ws to the FDO list for it
				NonUndoableUnitOfWorkHelper.Do(m_cache.ActionHandlerAccessor, () =>
				{
					// Add a global writing system to the local writing system store.  (Replace
					// does this if there's nothing to replace.)
					if (m_wsNew.Handle == 0)
						m_cache.ServiceLocator.WritingSystemManager.Replace(m_wsNew);
					if (isAnalysis)
					{
						m_cache.ServiceLocator.WritingSystems.AnalysisWritingSystems.Add(m_wsNew);
						if (!m_cache.ServiceLocator.WritingSystems.CurrentAnalysisWritingSystems.Contains(m_wsNew))
							m_cache.ServiceLocator.WritingSystems.CurrentAnalysisWritingSystems.Add(m_wsNew);
					}
					else
					{
						m_cache.ServiceLocator.WritingSystems.VernacularWritingSystems.Add(m_wsNew);
						if (!m_cache.ServiceLocator.WritingSystems.CurrentVernacularWritingSystems.Contains(m_wsNew))
							m_cache.ServiceLocator.WritingSystems.CurrentVernacularWritingSystems.Add(m_wsNew);
					}
					ProgressDialogWithTask.ImportTranslatedListsForWs(this.FindForm(), m_cache, m_wsNew.IcuLocale);
				});
				if (WritingSystemAdded != null)
					WritingSystemAdded(this, new EventArgs());
			}
		}

		/// <summary>
		/// Get the new writing system added by clicking this button and following the popup menus.
		/// </summary>
		public CoreWritingSystemDefinition NewWritingSystem
		{
			get
			{
				CheckDisposed();
				return m_wsNew;
			}
		}
	}
}
