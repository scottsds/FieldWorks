// Copyright (c) 2015 SIL International
// This software is licensed under the LGPL, version 2.1 or later
// (http://www.gnu.org/licenses/lgpl-2.1.html)

using System.Xml;
using SIL.LCModel.Core.Text;
using SIL.LCModel.Core.WritingSystems;
using SIL.LCModel;
using SIL.FieldWorks.Common.Widgets;
using SIL.FieldWorks.LexText.Controls;
using SIL.FieldWorks.Common.Controls;
using SIL.LCModel.Core.KernelInterfaces;

namespace SIL.FieldWorks.XWorks.MorphologyEditor
{
	/// <summary>
	/// Summary description for WordformGoDlg.
	/// </summary>
	public class WordformGoDlg : BaseGoDlg
	{
		#region	Data members

		protected int m_oldSearchWs;

		#endregion

		#region Construction, Initialization, and Disposal

		public WordformGoDlg()
		{
			SetHelpTopic("khtpFindWordform");
			InitializeComponent();
		}

		/// <summary>
		/// Just load current vernacular
		/// </summary>
		protected override void LoadWritingSystemCombo()
		{
			foreach (CoreWritingSystemDefinition ws in m_cache.ServiceLocator.WritingSystems.CurrentVernacularWritingSystems)
				m_cbWritingSystems.Items.Add(ws);
		}

		#endregion Construction, Initialization, and Disposal

		#region Other methods

		protected override void InitializeMatchingObjects(LcmCache cache)
		{
			var xnWindow = m_propertyTable.GetValue<XmlNode>("WindowConfiguration");
			var configNode = xnWindow.SelectSingleNode("controls/parameters/guicontrol[@id=\"WordformsBrowseView\"]/parameters");

			SearchEngine searchEngine = SearchEngine.Get(m_mediator, m_propertyTable, "WordformGoSearchEngine", () => new WordformGoSearchEngine(cache));

			m_matchingObjectsBrowser.Initialize(cache, FontHeightAdjuster.StyleSheetFromPropertyTable(m_propertyTable), m_mediator, m_propertyTable, configNode,
				searchEngine);

			// start building index
			var wsObj = (CoreWritingSystemDefinition) m_cbWritingSystems.SelectedItem;
			if (wsObj != null)
			{
				ITsString tssForm = TsStringUtils.EmptyString(wsObj.Handle);
				var field = new SearchField(WfiWordformTags.kflidForm, tssForm);
				m_matchingObjectsBrowser.SearchAsync(new[] { field });
			}
		}

		/// <summary>
		/// Reset the list of matching items.
		/// </summary>
		/// <param name="searchKey"></param>
		protected override void ResetMatches(string searchKey)
		{
			var wsObj = (CoreWritingSystemDefinition) m_cbWritingSystems.SelectedItem;
			int wsSelHvo = wsObj != null ? wsObj.Handle : 0;

			string form;
			int vernWs;
			if (!GetSearchKey(wsSelHvo, searchKey, out form, out vernWs))
			{
				var ws = TsStringUtils.GetWsAtOffset(m_tbForm.Tss, 0);
				if (!GetSearchKey(ws, searchKey, out form, out vernWs))
					return;
				wsSelHvo = ws;
			}

			if (m_oldSearchKey == searchKey && m_oldSearchWs == wsSelHvo)
				return; // Nothing new to do, so skip it.

			if (m_oldSearchKey != string.Empty || searchKey != string.Empty)
				StartSearchAnimation();

			// disable Go button until we rebuild our match list.
			m_btnOK.Enabled = false;
			m_oldSearchKey = searchKey;
			m_oldSearchWs = wsSelHvo;

			ITsString tssForm = TsStringUtils.MakeString(form ?? string.Empty, vernWs);
			var field = new SearchField(WfiWordformTags.kflidForm, tssForm);
			m_matchingObjectsBrowser.SearchAsync(new[] { field });
		}

		private bool GetSearchKey(int ws, string searchKey, out string form, out int vernWs)
		{
			form = null;
			vernWs = 0;

			if (m_vernHvos.Contains(ws))
			{
				vernWs = ws;
				form = searchKey;
			}
			else
			{
				return false;
			}

			return true;
		}
		#endregion

		#region Windows Form Designer generated code
		/// <summary>
		/// Required method for Designer support - do not modify
		/// the contents of this method with the code editor.
		/// </summary>
		private void InitializeComponent()
		{
			System.ComponentModel.ComponentResourceManager resources = new System.ComponentModel.ComponentResourceManager(typeof(WordformGoDlg));
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
			// m_objectsLabel
			//
			resources.ApplyResources(this.m_objectsLabel, "m_objectsLabel");
			//
			// WordformGoDlg
			//
			resources.ApplyResources(this, "$this");
			this.m_helpProvider.SetHelpNavigator(this, ((System.Windows.Forms.HelpNavigator)(resources.GetObject("$this.HelpNavigator"))));
			this.Name = "WordformGoDlg";
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
