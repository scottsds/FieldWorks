// Copyright (c) 2011-2018 SIL International
// This software is licensed under the LGPL, version 2.1 or later
// (http://www.gnu.org/licenses/lgpl-2.1.html)
using System;
using System.IO;
using Gtk;

namespace SIL.FieldWorks.Common.Controls.FileDialog.Linux
{
	internal class OpenFileDialogLinux: FileDialogLinux, IOpenFileDialog
	{
		public OpenFileDialogLinux()
		{
			Action = FileChooserAction.Open;
			LocalReset();
		}

		#region IOpenFileDialog implementation
		public Stream OpenFile()
		{
			return new FileStream(FileName, FileMode.Open);
		}
		#endregion

		protected override void ReportFileNotFound(string fileName)
		{
			ShowMessageBox(string.Format(FileDialogStrings.FileNotFoundOpen, Environment.NewLine), ButtonsType.Ok, MessageType.Warning,
				fileName);
		}

		private void LocalReset()
		{
			Title = FileDialogStrings.TitleOpen;
		}

		public override void Reset()
		{
			base.Reset();
			LocalReset();
		}
	}
}
