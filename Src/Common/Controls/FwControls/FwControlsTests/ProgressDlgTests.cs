// Copyright (c) 2003-2017 SIL International
// This software is licensed under the LGPL, version 2.1 or later
// (http://www.gnu.org/licenses/lgpl-2.1.html)

using System;
using System.ComponentModel;
using System.Threading;
using NUnit.Framework;
using SIL.FieldWorks.Common.FwUtils;
using SIL.LCModel.Utils;

namespace SIL.FieldWorks.Common.Controls
{
	#region DummyProgressDlg
	/// ----------------------------------------------------------------------------------------
	/// <summary>
	///
	/// </summary>
	/// ----------------------------------------------------------------------------------------
	public class DummyProgressDlg : ProgressDialogWithTask
	{
		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Initializes a new instance of the <see cref="DummyProgressDlg"/> class.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		public DummyProgressDlg(ISynchronizeInvoke synchronizeInvoke)
			: base(synchronizeInvoke)
		{
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Gets a value indicating wether or not the cancel button is visible.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		public bool IsCancelVisible
		{
			get
			{
				CheckDisposed();
				return AllowCancel;
			}
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Simulates pressing the cancel button.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		public void SimulatePressCancel()
		{
			ReflectionHelper.CallMethod(m_progressDialog, "btnCancel_Click", null, EventArgs.Empty);
		}
	}
	#endregion

	#region ProgressDlgTests
	/// ---------------------------------------------------------------------------------------
	/// <summary>
	/// Summary description for ProgressDlgTests.
	/// </summary>
	/// ---------------------------------------------------------------------------------------
	[TestFixture]
	public class ProgressDlgTests
	{
		private ThreadHelper m_threadHelper;
		private DummyProgressDlg m_dlg;
		private Timer m_timer;

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Set up the test
		/// </summary>
		/// ------------------------------------------------------------------------------------
		[SetUp]
		public void Setup()
		{
			m_threadHelper = new ThreadHelper();
			m_dlg = new DummyProgressDlg(m_threadHelper) {Maximum = 10};
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Teardowns this instance.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		[TearDown]
		public void Teardown()
		{
			if (m_timer != null)
			{
				m_timer.Dispose();
				m_timer = null;
			}

			if (m_dlg != null)
			{
				m_dlg.Dispose();
				m_dlg = null;
			}

			if (m_threadHelper != null)
			{
				m_threadHelper.Dispose();
				m_threadHelper = null;
			}

		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// This is a timer callback that gets fired to cancel the dialog.
		/// </summary>
		/// <param name="state">Minnesota, Illinois, etc.</param>
		/// ------------------------------------------------------------------------------------
		private void CancelDialog(object state)
		{
			if (m_dlg == null)
				return;

			m_dlg.SimulatePressCancel();
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Initializes a timer and kicks it off.
		/// </summary>
		/// <param name="delay">How many milliseconds until the timer delegate gets fired.
		/// </param>
		/// ------------------------------------------------------------------------------------
		private void StartTimer(long delay)
		{
			// Wait for the simulated cancel button, but if we don't get it after 'delay'
			// milliseconds, give up and close it when the following timer runs out.
			m_timer = new Timer(CancelDialog, null, delay, Timeout.Infinite);
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// The task to do in the background. We just increment the progress and sleep in
		/// between.
		/// </summary>
		/// <param name="progressDlg">The progress dialog.</param>
		/// <param name="parameters">The parameters. Not used.</param>
		/// <returns>Current progress</returns>
		/// ------------------------------------------------------------------------------------
		private static object BackgroundTask(IThreadedProgress progressDlg, object[] parameters)
		{
			int i = 0;
			for (; i < 10; i++)
			{
				if (progressDlg.Canceled)
					break;

				progressDlg.Step(1);
				Thread.Sleep(1000);
			}

			return i;
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Test the progress dialog box with a cancel button.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		[Test]
		[NUnit.Framework.Category("DesktopRequired")]
		public void TestWithCancel()
		{
			StartTimer(2500);

			var nProgress = (int) m_dlg.RunTask(false, BackgroundTask);

			Assert.Less(nProgress, 10);
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Test the progress dialog and verifies the cancel button doesn't show when there
		/// isn't a Cancel delegate specified. Also verifies the count is correct at the end,
		/// although that's not an incredibly exciting test.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		[Test]
		[NUnit.Framework.Category("DesktopRequired")]
		public void TestWithoutCancel()
		{
			var nProgress = (int) m_dlg.RunTask(false, BackgroundTask);

			Assert.AreEqual(10, nProgress);
		}
	}
	#endregion
}
