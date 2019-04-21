// Copyright (c) 2015 SIL International
// This software is licensed under the LGPL, version 2.1 or later
// (http://www.gnu.org/licenses/lgpl-2.1.html)

using System.Collections.Generic;
using System.Linq;
using System.Xml;
using SIL.LCModel.Core.Text;
using SIL.LCModel.Core.WritingSystems;
using SIL.FieldWorks.Common.Controls;
using SIL.LCModel.Core.KernelInterfaces;
using SIL.FieldWorks.Common.Widgets;
using SIL.LCModel;
using SIL.FieldWorks.LexText.Controls;
using XCore;

namespace SIL.FieldWorks.XWorks.LexEd
{
	/// <summary/>
	public class ReversalEntryGoDlg : BaseGoDlg
	{
		private IReversalIndex m_reveralIndex;
		private readonly HashSet<int> m_FilteredReversalEntryHvos = new HashSet<int>();

		public ReversalEntryGoDlg()
		{
			SetHelpTopic("khtpFindReversalEntry");
			InitializeComponent();
		}

		/// <summary>
		/// Gets or sets the reversal index.
		/// </summary>
		/// <value>The reversal index.</value>
		public IReversalIndex ReversalIndex
		{
			get
			{
				CheckDisposed();
				return m_reveralIndex;
			}

			set
			{
				CheckDisposed();
				m_reveralIndex = value;
			}
		}

		public ICollection<int> FilteredReversalEntryHvos
		{
			get
			{
				CheckDisposed();
				return m_FilteredReversalEntryHvos;
			}
		}

		protected override string PersistenceLabel
		{
			get { return "ReversalEntryGo"; }
		}

		protected override void InitializeMatchingObjects(LcmCache cache)
		{
			var xnWindow = m_propertyTable.GetValue<XmlNode>("WindowConfiguration");
			XmlNode configNode = xnWindow.SelectSingleNode("controls/parameters/guicontrol[@id=\"matchingReversalEntries\"]/parameters");

			var searchEngine = (ReversalEntrySearchEngine)SearchEngine.Get(m_mediator, m_propertyTable, "ReversalEntryGoSearchEngine-" + m_reveralIndex.Hvo,
				() => new ReversalEntrySearchEngine(cache, m_reveralIndex));
			searchEngine.FilteredEntryHvos = m_FilteredReversalEntryHvos;

			m_matchingObjectsBrowser.Initialize(cache, FontHeightAdjuster.StyleSheetFromPropertyTable(m_propertyTable), m_mediator, m_propertyTable, configNode,
				searchEngine, m_cache.ServiceLocator.WritingSystemManager.Get(m_reveralIndex.WritingSystem));

			// start building index
			var wsObj = (CoreWritingSystemDefinition) m_cbWritingSystems.SelectedItem;
			if (wsObj != null)
			{
				ITsString tss = TsStringUtils.EmptyString(wsObj.Handle);
				var field = new SearchField(ReversalIndexEntryTags.kflidReversalForm, tss);
				m_matchingObjectsBrowser.SearchAsync(new[] { field });
			}
		}

		private class ReversalEntrySearchEngine : ReversalEntryGoSearchEngine
		{
			public ICollection<int> FilteredEntryHvos { private get; set; }

			public ReversalEntrySearchEngine(LcmCache cache, IReversalIndex revIndex) : base(cache, revIndex) {}

			protected override IEnumerable<int> FilterResults(IEnumerable<int> results)
			{
				return results == null ? null : results.Where(hvo => !FilteredEntryHvos.Contains(hvo));
			}
		}

		protected override void LoadWritingSystemCombo()
		{
			m_cbWritingSystems.Items.Add(m_cache.ServiceLocator.WritingSystemManager.Get(m_reveralIndex.WritingSystem));
		}

		public override void SetDlgInfo(LcmCache cache, WindowParams wp, Mediator mediator, PropertyTable propertyTable)
		{
			SetDlgInfo(cache, wp, mediator, propertyTable, cache.ServiceLocator.WritingSystemManager.GetWsFromStr(m_reveralIndex.WritingSystem));
		}

		public override void SetDlgInfo(LcmCache cache, WindowParams wp, Mediator mediator, PropertyTable propertyTable, string form)
		{
			SetDlgInfo(cache, wp, mediator, propertyTable, form, cache.ServiceLocator.WritingSystemManager.GetWsFromStr(m_reveralIndex.WritingSystem));
		}

		protected override void ResetMatches(string searchKey)
		{
			if (m_oldSearchKey == searchKey)
				return; // Nothing new to do, so skip it.

			// disable Go button until we rebuild our match list.
			m_btnOK.Enabled = false;
			m_oldSearchKey = searchKey;

			var wsObj = (CoreWritingSystemDefinition) m_cbWritingSystems.SelectedItem;
			if (wsObj == null)
				return;

			if (m_oldSearchKey != string.Empty || searchKey != string.Empty)
				StartSearchAnimation();

			ITsString tss = TsStringUtils.MakeString(searchKey, wsObj.Handle);
			var field = new SearchField(ReversalIndexEntryTags.kflidReversalForm, tss);
			m_matchingObjectsBrowser.SearchAsync(new[] { field });
		}

		#region Windows Form Designer generated code
		/// <summary>
		/// Required method for Designer support - do not modify
		/// the contents of this method with the code editor.
		/// </summary>
		private void InitializeComponent()
		{
			System.ComponentModel.ComponentResourceManager resources = new System.ComponentModel.ComponentResourceManager(typeof(ReversalEntryGoDlg));
			this.m_panel1.SuspendLayout();
			((System.ComponentModel.ISupportInitialize)(this.m_tbForm)).BeginInit();
			((System.ComponentModel.ISupportInitialize)(this.m_fwTextBoxBottomMsg)).BeginInit();
			this.SuspendLayout();
			//
			// m_btnOK
			//
			resources.ApplyResources(this.m_btnOK, "m_btnOK");
			//
			// m_btnInsert
			//
			resources.ApplyResources(this.m_btnInsert, "m_btnInsert");
			//
			// m_cbWritingSystems
			//
			resources.ApplyResources(this.m_cbWritingSystems, "m_cbWritingSystems");
			//
			// m_objectsLabel
			//
			resources.ApplyResources(this.m_objectsLabel, "m_objectsLabel");
			//
			// ReversalEntryGoDlg
			//
			resources.ApplyResources(this, "$this");
			this.m_helpProvider.SetHelpNavigator(this, ((System.Windows.Forms.HelpNavigator)(resources.GetObject("$this.HelpNavigator"))));
			this.Name = "ReversalEntryGoDlg";
			this.m_helpProvider.SetShowHelp(this, ((bool)(resources.GetObject("$this.ShowHelp"))));
			this.m_panel1.ResumeLayout(false);
			((System.ComponentModel.ISupportInitialize)(this.m_tbForm)).EndInit();
			((System.ComponentModel.ISupportInitialize)(this.m_fwTextBoxBottomMsg)).EndInit();
			this.ResumeLayout(false);
			this.PerformLayout();

		}
		#endregion
	}
}
