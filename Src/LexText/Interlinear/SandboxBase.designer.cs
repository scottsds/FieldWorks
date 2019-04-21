// Copyright (c) 2015 SIL International
// This software is licensed under the LGPL, version 2.1 or later
// (http://www.gnu.org/licenses/lgpl-2.1.html)

using System;

namespace SIL.FieldWorks.IText
{
	partial class SandboxBase
	{
		/// <summary>
		/// Required designer variable.
		/// </summary>
		private System.ComponentModel.IContainer components = null;

		/// -----------------------------------------------------------------------------------
		/// <summary>
		/// Clean up any resources being used.
		/// </summary>
		/// -----------------------------------------------------------------------------------
		protected override void Dispose(bool disposing)
		{
			System.Diagnostics.Debug.WriteLineIf(!disposing, "****** Missing Dispose() call for " + GetType().Name + ". ******");

			// Must not be run more than once.
			if (IsDisposed)
				return;

			if (disposing)
			{
				if (m_propertyTable != null)
				{
					m_propertyTable.SetProperty("FirstControlToHandleMessages", null, true);
				}
			}

			base.Dispose(disposing);

			if (disposing)
			{
				if (components != null)
					components.Dispose();
				if (m_editMonitor != null)
					m_editMonitor.Dispose();
				if (m_vc != null)
					m_vc.Dispose();
				if (m_caches != null)
					m_caches.Dispose();
				DisposeComboHandler();
				if (m_caHandler != null)
				{
					m_caHandler.AnalysisChosen -= new EventHandler(Handle_AnalysisChosen);
					m_caHandler.Dispose();
				}
			}
			m_stylesheet = null;
			m_caches = null;
			// StringCaseStatus m_case; // Enum, which is a value type, and value types can't be set to null.
			m_ComboHandler = null; // handles most kinds of combo box.
			m_caHandler = null; // handles the one on the base line.

			m_editMonitor = null;
			m_vc = null;
			m_propertyTable = null;
			if (m_rawWordform != null)
			{
				m_rawWordform = null;
			}
			if (m_tssWordform != null)
			{
				m_tssWordform = null;
			}
		}

		#region Component Designer generated code

		/// <summary>
		/// Required method for Designer support - do not modify
		/// the contents of this method with the code editor.
		/// </summary>
		private void InitializeComponent()
		{
			components = new System.ComponentModel.Container();
			this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
		}

		#endregion
	}
}
