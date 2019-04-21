﻿// Copyright (c) 2013-2017 SIL International
// This software is licensed under the LGPL, version 2.1 or later
// (http://www.gnu.org/licenses/lgpl-2.1.html)

using System;
using System.Windows.Forms;
using Microsoft.Win32;
using SIL.FieldWorks.Common.RootSites;
using SIL.Reporting;
using SIL.Utils;
using XCore;

namespace SIL.FieldWorks.XWorks
{
	/// ------------------------------------------------------------------------------------
	/// <summary>
	/// This class allows for exceptions in SIL dlls to be reported in FieldWorks
	/// using the normal FieldWorks error handling system and reporting dialogs. When you
	/// create a Palaso dialog, create an instance of this class, passing it the dialog.
	/// </summary>
	/// ------------------------------------------------------------------------------------
	class SilErrorReportingAdapter : IErrorReporter, IDisposable
	{
		private Form m_parentForm;
		private RegistryKey m_registryKey;
		private string m_supportEmailAddress;

		internal SilErrorReportingAdapter(Form parentForm, PropertyTable propertyTable)
		{
			m_parentForm = parentForm;
			m_registryKey = propertyTable.GetValue<IApp>("App").SettingsKey;
			m_supportEmailAddress = propertyTable.GetValue<IFeedbackInfoProvider>("FeedbackInfoProvider").SupportEmailAddress;
		}

		public void ReportFatalException(Exception e)
		{
			throw e; // I think this will ultimately show the green-screen (unless something catches it)
		}

		public ErrorResult NotifyUserOfProblem(IRepeatNoticePolicy policy, string alternateButton1Label,
			ErrorResult resultIfAlternateButtonPressed, string message)
		{
			return policy.ShouldShowMessage(message) &&
				ErrorReporter.ReportException(new Exception(message), m_registryKey, m_supportEmailAddress, m_parentForm, false) ?
				ErrorResult.Abort : ErrorResult.Ignore;
		}

		public void ReportNonFatalException(Exception exception, IRepeatNoticePolicy policy)
		{
			if (policy.ShouldShowErrorReportDialog(exception))
				ErrorReporter.ReportException(exception, m_registryKey, m_supportEmailAddress, m_parentForm, false);
		}

		public void ReportNonFatalExceptionWithMessage(Exception error, string message, params object[] args)
		{
			ErrorReporter.ReportException(new Exception(string.Format(message, args), error),
				m_registryKey, m_supportEmailAddress, m_parentForm, false);
		}

		public void ReportNonFatalMessageWithStackTrace(string message, params object[] args)
		{
			ErrorReporter.ReportException(new Exception(string.Format(message, args)), m_registryKey,
				m_supportEmailAddress, m_parentForm, false);
		}

		public void ReportFatalMessageWithStackTrace(string message, object[] args)
		{
			ErrorReporter.ReportException(new Exception(string.Format(message, args)), m_registryKey,
				m_supportEmailAddress, m_parentForm, true);
		}

		public void CheckDisposed()
		{
			if (IsDisposed)
				throw new ObjectDisposedException(string.Format("'{0}' in use after being disposed.", GetType().Name));
		}

		public bool IsDisposed { get; private set; }

		/// <summary>
		/// Finalizer, in case client doesn't dispose it.
		/// Force Dispose(false) if not already called (i.e. m_isDisposed is true)
		/// </summary>
		/// <remarks>
		/// In case some clients forget to dispose it directly.
		/// </remarks>
		~SilErrorReportingAdapter()
		{
			Dispose(false);
			// The base class finalizer is called automatically.
		}

		/// <summary>
		///
		/// </summary>
		/// <remarks>Must not be virtual.</remarks>
		public void Dispose()
		{
			Dispose(true);
			// This object will be cleaned up by the Dispose method.
			// Therefore, you should call GC.SupressFinalize to
			// take this object off the finalization queue
			// and prevent finalization code for this object
			// from executing a second time.
			GC.SuppressFinalize(this);
		}

		/// <summary>
		/// Executes in two distinct scenarios.
		///
		/// 1. If disposing is true, the method has been called directly
		/// or indirectly by a user's code via the Dispose method.
		/// Both managed and unmanaged resources can be disposed.
		///
		/// 2. If disposing is false, the method has been called by the
		/// runtime from inside the finalizer and you should not reference (access)
		/// other managed objects, as they already have been garbage collected.
		/// Only unmanaged resources can be disposed.
		/// </summary>
		/// <param name="disposing"></param>
		/// <remarks>
		/// If any exceptions are thrown, that is fine.
		/// If the method is being done in a finalizer, it will be ignored.
		/// If it is thrown by client code calling Dispose,
		/// it needs to be handled by fixing the bug.
		///
		/// If subclasses override this method, they should call the base implementation.
		/// </remarks>
		protected virtual void Dispose(bool disposing)
		{
			System.Diagnostics.Debug.WriteLineIf(!disposing, "****** Missing Dispose() call for " + GetType().Name + ". ****** ");
			if (IsDisposed)
				return;

			if (disposing)
				m_registryKey.Dispose();

			m_parentForm = null;
			m_registryKey = null;
			m_supportEmailAddress = null;

			IsDisposed = true;
		}
	}
}
