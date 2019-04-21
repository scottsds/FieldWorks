﻿// Copyright (c) 2015 SIL International
// This software is licensed under the LGPL, version 2.1 or later
// (http://www.gnu.org/licenses/lgpl-2.1.html)

using System;
using System.Windows.Forms;
using SIL.FieldWorks.Common.FwUtils;
using SIL.LCModel;
using SIL.LCModel.DomainServices;

namespace SIL.FieldWorks.FwCoreDlgs
{
	/// <summary>
	/// Provides a common place for calls which utilize FDO's SharedBackendServices
	/// </summary>
	public static class SharedBackendServicesHelper
	{
		/// <summary>
		/// Display a warning indicating that it may be dangerous to change things in the dialog that
		/// is about to open when there are other applications using this project. The warning should only
		/// be shown if, in fact, other applications currently have this project open. Return true to continue,
		/// false to cancel opening the dialog.
		/// </summary>
		/// <returns></returns>
		public static bool WarnOnOpeningSingleUserDialog(LcmCache cache)
		{
			if (!SharedBackendServices.AreMultipleApplicationsConnected(cache))
				return true;
			string msg = Strings.ksWarnOnOpeningSingleAppDialog.Replace("\\n", Environment.NewLine);
			return ThreadHelper.ShowMessageBox(null, msg, Strings.ksOtherAppsUsingProjectCaption,
				MessageBoxButtons.OKCancel, MessageBoxIcon.Information) == DialogResult.OK;
		}

		/// <summary>
		/// Display a warning indicating that it may be dangerous to change things that the user has just
		/// asked to change when other applications are using this project. The warning should only be shown
		/// if, in fact, other applications currently have this project open. Return true to continue, false
		/// to discard the changes. This is typically called in response to clicking an OK button in a dialog
		/// which changes dangerous user settings.
		/// </summary>
		/// <returns></returns>
		public static bool WarnOnConfirmingSingleUserChanges(LcmCache cache)
		{
			if (!SharedBackendServices.AreMultipleApplicationsConnected(cache))
				return true;
			string msg = Strings.ksWarnOnConfirmingSingleAppChanges.Replace("\\n", Environment.NewLine);
			return ThreadHelper.ShowMessageBox(null, msg, Strings.ksNotAdvisableOtherAppsUsingProjectCaption,
				MessageBoxButtons.YesNo, MessageBoxIcon.Warning) == DialogResult.Yes;
		}
	}
}
