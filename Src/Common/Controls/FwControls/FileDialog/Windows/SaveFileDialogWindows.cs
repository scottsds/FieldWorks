// Copyright (c) 2011-2018 SIL International
// This software is licensed under the LGPL, version 2.1 or later
// (http://www.gnu.org/licenses/lgpl-2.1.html)
using System.IO;
using System.Windows.Forms;

namespace SIL.FieldWorks.Common.Controls.FileDialog.Windows
{
	internal class SaveFileDialogWindows: FileDialogWindows, ISaveFileDialog
	{
		public SaveFileDialogWindows()
		{
			m_dlg = new SaveFileDialog();
		}

		public bool CreatePrompt
		{
			get { return ((SaveFileDialog)m_dlg).CreatePrompt; }
			set { ((SaveFileDialog)m_dlg).CreatePrompt = value; }
		}

		public bool OverwritePrompt
		{
			get { return ((SaveFileDialog)m_dlg).OverwritePrompt; }
			set { ((SaveFileDialog)m_dlg).OverwritePrompt = value; }
		}

		public Stream OpenFile()
		{
			return ((SaveFileDialog)m_dlg).OpenFile();
		}
	}
}
