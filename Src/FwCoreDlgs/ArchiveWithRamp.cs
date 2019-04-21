﻿// Copyright (c) 2015 SIL International
// This software is licensed under the LGPL, version 2.1 or later
// (http://www.gnu.org/licenses/lgpl-2.1.html)

using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Windows.Forms;
using SIL.LCModel;
using SIL.FieldWorks.FwCoreDlgs.BackupRestore;
using SIL.LCModel.DomainServices.BackupRestore;
using SIL.FieldWorks.Common.FwUtils;

namespace SIL.FieldWorks.FwCoreDlgs
{
	/// ------------------------------------------------------------------------------------
	public partial class ArchiveWithRamp : Form
	{
		private readonly List<string> m_filesToArchive = new List<string>();
		private readonly LcmCache m_cache;
		private readonly IHelpTopicProvider m_helpTopicProvider;
		private string m_lastBackupFile;

		/// ------------------------------------------------------------------------------------
		public ArchiveWithRamp(LcmCache cache,
			IHelpTopicProvider helpTopicProvider)
		{
			m_cache = cache;
			m_helpTopicProvider = helpTopicProvider;
			InitializeComponent();

			get_Last_Backup();
		}

		private void m_archive_Click(object sender, EventArgs e)
		{
			// did the user select the FieldWorks backup file to archive?
			if (m_fieldWorksBackup.Checked)
			{
				if (m_rbNewBackup.Checked)
				{
					using (BackupProjectDlg dlg = new BackupProjectDlg(m_cache, m_helpTopicProvider))
					{
						if ((dlg.ShowDialog(this) == DialogResult.OK)
							&& (!string.IsNullOrEmpty(dlg.BackupFilePath)))
						{
							m_filesToArchive.Add(dlg.BackupFilePath);
						}
						else
						{
							DialogResult = DialogResult.None;
							return;
						}
					}
				}
				else
				{
					m_filesToArchive.Add(m_lastBackupFile);
				}
			}

			// other files would go here, if there were an option to archive them.

			// close the dialog
			DialogResult = DialogResult.OK;
		}

		/// ------------------------------------------------------------------------------------
		public List<string> FilesToArchive { get { return m_filesToArchive;  }}

		private void get_Last_Backup()
		{
			BackupFileRepository backups = new BackupFileRepository(FwDirectoryFinder.DefaultBackupDirectory);
			var projName = backups.AvailableProjectNames.FirstOrDefault(s => s == m_cache.ProjectId.Name);

			if (!string.IsNullOrEmpty(projName))
			{
				var fileDate = backups.GetAvailableVersions(projName).FirstOrDefault();
				if (fileDate != default(DateTime))
				{
					var backup = backups.GetBackupFile(projName, fileDate, true);

					if (backup != null)
					{
						m_lblMostRecentBackup.Text = fileDate.ToString(Thread.CurrentThread.CurrentCulture);
						m_lastBackupFile = backup.File;
						return;
					}
				}
			}

			// no backup found if you are here
			m_rbNewBackup.Checked = true;
			m_rbExistingBackup.Visible = false;
			m_lblMostRecentBackup.Visible = false;
		}

		private void m_help_Click(object sender, EventArgs e)
		{
			ShowHelp.ShowHelpTopic(m_helpTopicProvider, "khtpArchiveWithRamp");
		}
	}
}
