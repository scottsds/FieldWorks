﻿// Copyright (c) 2015 SIL International
// This software is licensed under the LGPL, version 2.1 or later
// (http://www.gnu.org/licenses/lgpl-2.1.html)

using System.Windows.Forms;
using System.Linq;
using SIL.FieldWorks.Common.Framework.DetailControls;
using SIL.LCModel;
using SIL.FieldWorks.LexText.Controls;

namespace SIL.FieldWorks.XWorks.LexEd
{
	/// <summary>
	///
	/// </summary>
	public class RecordReferenceVectorLauncher : VectorReferenceLauncher
	{
		protected override void HandleChooser()
		{
			using (var dlg = new RecordGoDlg())
			{
				var wp = new WindowParams { m_title = LexEdStrings.ksIdentifyRecord, m_btnText = LexEdStrings.ks_Add };
				dlg.SetDlgInfo(m_cache, wp, m_mediator, m_propertyTable);
				dlg.SetHelpTopic(Slice.GetChooserHelpTopicID());
				if (dlg.ShowDialog(FindForm()) == DialogResult.OK)
					AddItem(dlg.SelectedObject);
			}
		}

		public override void AddItem(ICmObject obj)
		{
			if (!Targets.Contains(obj))
				AddItem(obj, LexEdStrings.ksUndoAddRef, LexEdStrings.ksRedoAddRef);
		}
	}
}
