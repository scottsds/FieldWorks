﻿// Copyright (c) 2015 SIL International
// This software is licensed under the LGPL, version 2.1 or later
// (http://www.gnu.org/licenses/lgpl-2.1.html)

using System.Diagnostics;
using SIL.LCModel;
using SIL.LCModel.DomainServices;
using SIL.LCModel.Infrastructure;
using SIL.FieldWorks.FdoUi;
using SIL.FieldWorks.FwCoreDlgs;

namespace SIL.FieldWorks.IText
{
	/// <summary>
	/// This class is a wrapper that makes WfiWordformServices.MergeDuplicateAnalyses available in Tools/Utilities.
	/// An entry in UtilityCatalogInclude (in DistFiles/Language Explorer/Configuration) causes an instance to
	/// be created by reflection when the dialog is initialized.
	/// </summary>
	public class DuplicateAnalysisFixer : IUtility
	{
		public string Label
		{
			get { return ITextStrings.ksMergeDuplicateAnalyses; }
		}

		/// <summary>
		/// This is what is actually shown in the dialog as the ID of the task.
		/// </summary>
		public override string ToString()
		{
			return Label;
		}

		private UtilityDlg m_dlg;

		/// <summary>
		/// Sets the utility dialog. NOTE: The caller is responsible for disposing the dialog!
		/// </summary>
		public UtilityDlg Dialog
		{
			set { m_dlg = value; }
		}

		public void LoadUtilities()
		{
			Debug.Assert(m_dlg != null);
			m_dlg.Utilities.Items.Add(this);
		}

		public void OnSelection()
		{
			Debug.Assert(m_dlg != null);
			m_dlg.WhenDescription = ITextStrings.ksUseMergeAnalysesWhen;
			m_dlg.WhatDescription = ITextStrings.ksMergeAnalysesAttemptsTo;
			m_dlg.RedoDescription = ITextStrings.ksMergeAnalysesWarning;
		}

		/// <summary>
		/// This actually makes the fix.
		/// </summary>
		public void Process()
		{
			Debug.Assert(m_dlg != null);
			var cache = m_dlg.PropTable.GetValue<LcmCache>("cache");
			UndoableUnitOfWorkHelper.Do(ITextStrings.ksUndoMergeAnalyses, ITextStrings.ksRedoMergeAnalyses,
				cache.ActionHandlerAccessor,
				() => WfiWordformServices.MergeDuplicateAnalyses(cache, new ProgressBarWrapper(m_dlg.ProgressBar)));
		}
	}
}
