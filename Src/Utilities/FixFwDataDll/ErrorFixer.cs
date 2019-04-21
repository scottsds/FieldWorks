// Copyright (c) 2011-2013 SIL International
// This software is licensed under the LGPL, version 2.1 or later
// (http://www.gnu.org/licenses/lgpl-2.1.html)
//
// File: LinkFixer.cs
// Responsibility: mcconnel

using System.Collections.Generic;
using System.Diagnostics;
using SIL.LCModel;
using SIL.FieldWorks.FwCoreDlgs;
using System.Windows.Forms;
using System.IO;
using SIL.FieldWorks.Common.FwUtils;
using System.Text;
using SIL.FieldWorks.Common.Controls;
using SIL.LCModel.FixData;
using SIL.LCModel.Utils;

namespace SIL.FieldWorks.FixData
{
	/// ----------------------------------------------------------------------------------------
	/// <summary>
	/// Connect the error fixing code to the FieldWorks UtilityDlg facility.
	/// </summary>
	/// ----------------------------------------------------------------------------------------
	public class ErrorFixer : IUtility
	{
		private UtilityDlg m_dlg;

		/// <summary>
		/// Override method to return the Label property.
		/// </summary>
		public override string ToString()
		{
			return Label;
		}

		#region IUtility Members

		/// <summary>
		/// Set the UtilityDlg that invokes this utility.
		/// </summary>
		/// <remarks>
		/// This must be set, before calling any other property or method.
		/// </remarks>
		public UtilityDlg Dialog
		{
			set
			{
				Debug.Assert(value != null);
				Debug.Assert(m_dlg == null);
				m_dlg = value;
			}
		}

		/// <summary>
		/// Get the main label describing the utility.
		/// </summary>
		public string Label
		{
			get
			{
				Debug.Assert(m_dlg != null);
				return Strings.ksFindAndFixErrors;
			}
		}

		/// <summary>
		/// Load 0 or more items in the main utility dialog's list box.
		/// </summary>
		public void LoadUtilities()
		{
			Debug.Assert(m_dlg != null);
			m_dlg.Utilities.Items.Add(this);
		}

		/// <summary>
		/// When selected in the main utility dialog, fill in some more information there.
		/// </summary>
		public void OnSelection()
		{
			Debug.Assert(m_dlg != null);
			m_dlg.WhenDescription = Strings.ksUseThisWhen;
			m_dlg.WhatDescription = Strings.ksThisUtilityAttemptsTo;
			m_dlg.RedoDescription = Strings.ksCannotUndo;
		}

		/// <summary>
		/// Run the utility on command from the main utility dialog.
		/// </summary>
		public void Process()
		{
			Debug.Assert(m_dlg != null);
			using (var dlg = new FixErrorsDlg())
			{
				try
				{
					if (dlg.ShowDialog(m_dlg) == DialogResult.OK)
					{
						string pathname = Path.Combine(
							Path.Combine(FwDirectoryFinder.ProjectsDirectory, dlg.SelectedProject),
							dlg.SelectedProject + LcmFileHelper.ksFwDataXmlFileExtension);
						if (File.Exists(pathname))
						{
							using (new WaitCursor(m_dlg))
							{
								using (var progressDlg = new ProgressDialogWithTask(m_dlg))
								{
									string fixes = (string)progressDlg.RunTask(true, FixDataFile, pathname);
									if (fixes.Length > 0)
									{
										MessageBox.Show(fixes, Strings.ksErrorsFoundOrFixed);
										File.WriteAllText(pathname.Replace(LcmFileHelper.ksFwDataXmlFileExtension, "fixes"), fixes);
									}
								}
							}
						}
					}
				}
				catch
				{
				}
			}
		}

		private object FixDataFile(IProgress progressDlg, params object[] parameters)
		{
			string pathname = parameters[0] as string;
			StringBuilder bldr = new StringBuilder();

			FwDataFixer data = new FwDataFixer(pathname, progressDlg, LogErrors, ErrorCount);
			_errorsFixed = 0;
			_errors.Clear();
			data.FixErrorsAndSave();

			foreach (var err in _errors)
				bldr.AppendLine(err);
			return bldr.ToString();
		}

		private List<string> _errors = new List<string>();
		private int _errorsFixed = 0;
		private void LogErrors(string message, bool errorFixed)
		{
			_errors.Add(message);
			if (errorFixed)
				++_errorsFixed;
		}

		private int ErrorCount()
		{
			return _errorsFixed;
		}
		#endregion
	}
}
