// Copyright (c) 2015 SIL International
// This software is licensed under the LGPL, version 2.1 or later
// (http://www.gnu.org/licenses/lgpl-2.1.html)

using System.Diagnostics;
using System.Windows.Forms;

using SIL.FieldWorks.Common.Framework.DetailControls;
using SIL.FieldWorks.Common.FwUtils;
using SIL.LCModel;
using SIL.FieldWorks.LexText.Controls;

namespace SIL.FieldWorks.XWorks.MorphologyEditor
{
	public class AdhocCoProhibAtomicLauncher : AtomicReferenceLauncher
	{
		private System.ComponentModel.IContainer components = null;

		public AdhocCoProhibAtomicLauncher()
		{
			// This call is required by the Windows Form Designer.
			InitializeComponent();

			// TODO: Add any initialization after the InitializeComponent call
		}

		/// <summary>
		/// Clean up any resources being used.
		/// </summary>
		protected override void Dispose( bool disposing )
		{
			//Debug.WriteLineIf(!disposing, "****************** " + GetType().Name + " 'disposing' is false. ******************");
			// Must not be run more than once.
			if (IsDisposed)
				return;

			if( disposing )
			{
				if (components != null)
				{
					components.Dispose();
				}
			}
			base.Dispose( disposing );
		}


		/// <summary>
		/// Override method to handle launching of a chooser for selecting lexical entries.
		/// </summary>
		protected override void HandleChooser()
		{
			Form frm = FindForm();
			WaitCursor wc = null;
			BaseGoDlg dlg = null;
			try
			{
				if (frm != null)
					wc = new WaitCursor(frm);
				if (m_obj is IMoAlloAdhocProhib)
					dlg = new LinkAllomorphDlg();
				else
				{
					Debug.Assert(m_obj is IMoMorphAdhocProhib);
					dlg = new LinkMSADlg();
				}
				Debug.Assert(dlg != null);
				dlg.SetDlgInfo(m_cache, null, m_mediator, m_propertyTable);
				if (dlg.ShowDialog(frm) == DialogResult.OK)
					AddItem(dlg.SelectedObject);
			}
			finally
			{
				if (wc != null)
					wc.Dispose();
				if (dlg != null)
					dlg.Dispose();
			}
		}

		#region Designer generated code
		/// <summary>
		/// Required method for Designer support - do not modify
		/// the contents of this method with the code editor.
		/// </summary>
		private void InitializeComponent()
		{
			components = new System.ComponentModel.Container();
		}
		#endregion
	}
}
