﻿// Copyright (c) 2014-2016 SIL International
// This software is licensed under the LGPL, version 2.1 or later
// (http://www.gnu.org/licenses/lgpl-2.1.html)

using System;
using System.Drawing;
using System.Linq;
using System.Windows.Forms;
using SIL.Linq;
using SIL.FieldWorks.Common.FwUtils;
using XCore;

namespace SIL.FieldWorks.XWorks
{
	public partial class DictionaryConfigurationManagerDlg : Form
	{

		private string m_helpTopic;
		private readonly HelpProvider m_helpProvider;
		private readonly IHelpTopicProvider m_helpTopicProvider;
		private readonly ToolTip m_toolTip;

		public DictionaryConfigurationManagerDlg(IHelpTopicProvider helpTopicProvider)
		{
			InitializeComponent();

			m_toolTip = new ToolTip();
			m_toolTip.SetToolTip(copyButton, xWorksStrings.DuplicateViewToolTip);
			m_toolTip.SetToolTip(removeButton, xWorksStrings.DeleteViewTooltip);
			m_toolTip.SetToolTip(resetButton, xWorksStrings.ResetViewTooltip);
			m_toolTip.SetToolTip(exportButton, xWorksStrings.ExportSelected);
			m_toolTip.SetToolTip(importButton, xWorksStrings.ImportView);

			m_helpTopicProvider = helpTopicProvider;

			// Allow renaming via the keyboard
			configurationsListView.KeyUp += ConfigurationsListViewKeyUp;
			// Make the Configuration selection more obvious when the control loses focus (LT-15450).
			configurationsListView.LostFocus += OnLostFocus;
			configurationsListView.GotFocus += OnGotFocus;

			m_helpProvider = new HelpProvider { HelpNamespace = m_helpTopicProvider.HelpFile };
			m_helpProvider.SetHelpKeyword(this, m_helpTopicProvider.GetHelpString(HelpTopic));
			m_helpProvider.SetHelpNavigator(this, HelpNavigator.Topic);
			m_helpProvider.SetShowHelp(this, true);
		}

		private void OnGotFocus(object sender, EventArgs eventArgs)
		{
			if (configurationsListView.Items.Count < 1)
				return;
			var nonBoldFont = new Font(configurationsListView.Items[0].Font, FontStyle.Regular);
			configurationsListView.Items.Cast<ListViewItem>().ForEach(item => item.Font = nonBoldFont);
		}

		private void OnLostFocus(object sender, EventArgs eventArgs)
		{
			if (configurationsListView.Items.Count < 1)
				return;
			var boldFont = new Font(configurationsListView.Items[0].Font, FontStyle.Bold);
			configurationsListView.SelectedItems.Cast<ListViewItem>().ForEach(item => { item.Font = boldFont; });

		}

		private void ConfigurationsListViewKeyUp(object sender, KeyEventArgs e)
		{
			// Match Windows Explorer behaviour: allow renaming from the keyboard by pressing F2 or through the
			// "context menu" (since there is no context menu, go straight to rename from the "Application" key)
			if ((e.KeyCode == Keys.F2 || e.KeyCode == Keys.Apps) && configurationsListView.SelectedItems.Count == 1)
				configurationsListView.SelectedItems[0].BeginEdit();
		}

		internal string HelpTopic
		{
			get { return m_helpTopic ?? (m_helpTopic = "khtpDictConfigManager"); }
			set
			{
				if (string.IsNullOrEmpty(value))
					return;
				m_helpTopic = value;
				m_helpProvider.SetHelpKeyword(this, m_helpTopicProvider.GetHelpString(HelpTopic));
			}
		}

		public string ConfigurationGroupText { set { configurationsGroupBox.Text = value; } }

		private void helpButton_Click(object sender, EventArgs e)
		{
			ShowHelp.ShowHelpTopic(m_helpTopicProvider, HelpTopic);
		}
	}
}
