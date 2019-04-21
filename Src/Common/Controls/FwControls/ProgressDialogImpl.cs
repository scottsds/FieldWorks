// Copyright (c) 2007-2017 SIL International
// This software is licensed under the LGPL, version 2.1 or later
// (http://www.gnu.org/licenses/lgpl-2.1.html)

using System;
using System.ComponentModel;
using System.Drawing;
using System.Windows.Forms;
using SIL.LCModel.Utils;

namespace SIL.FieldWorks.Common.Controls
{
	/// ----------------------------------------------------------------------------------------
	/// <summary>
	/// This implements the real progress dialog that is visible to the user. This dialog is
	/// displayed as a modal dialog in the main thread so that it can respond to the user
	/// clicking cancel while the work we want to accomplish is done in a background thread.
	/// </summary>
	/// <remarks>This dialog shouldn't be used directly, only through ProgressDialogWithTask.
	/// If we want to update the progress dialog to update properly and to be responsive to
	/// the user, we have to continually process its messages from the message loop. Previously
	/// this was done by calling Application.DoEvents(), but this can easily cause reentrancy
	/// problems. The proper way to do it is to run the progress dialog on the main thread as
	/// a modal dialog, and do the work which we want to accomplish while showing the dialog in
	/// a background thread. ProgressDialogWithTask is a wrapper to accomplish that, and it
	/// also deals with the multi-threading issues when we want to update the progress dialog
	/// from the background thread.</remarks>
	/// ----------------------------------------------------------------------------------------
	internal partial class ProgressDialogImpl : Form, IProgress
	{
		#region Member variables
		/// <summary>
		/// Event handler for listening to whether or the cancel button is pressed.
		/// </summary>
		public event CancelEventHandler Canceling;
		/// <summary>
		/// If true, this allows the progress indicator to restart at 0 if it overflows.
		/// </summary>
		private bool m_fRestartable;

		#endregion

		#region Constructors
		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Initializes a new instance of the <see cref="ProgressDialogImpl"/> class.
		/// </summary>
		/// <param name="owner">The owner (which we also expect to be used as a parameter when
		/// Show() is called).</param>
		/// ------------------------------------------------------------------------------------
		public ProgressDialogImpl(Form owner)
		{
			InitializeComponent();

			Message = string.Empty;
			lblCancel.AutoSize = false;
			lblCancel.Text = string.Empty;
			m_fRestartable = false;
			if (owner == null)
				StartPosition = FormStartPosition.CenterScreen;
			else
			{
				//StartPosition = FormStartPosition.CenterParent;
				// Sadly, just doing CenterParent won't work in this case :-(
				Left = owner.Left + (owner.Width - Width) / 2;
				Top = owner.Top + (owner.Height - Height) / 2;
				Screen primaryScreen = Screen.FromControl(owner);
				Left = Math.Max(Left, primaryScreen.WorkingArea.Left);
				Top = Math.Max(Top, primaryScreen.WorkingArea.Top);
				if (Right > primaryScreen.WorkingArea.Right)
					Left -= (Right - primaryScreen.WorkingArea.Right);
				if (Bottom > primaryScreen.WorkingArea.Bottom)
					Top -= (Bottom - primaryScreen.WorkingArea.Bottom);
			}
		}
		#endregion

		#region Properties

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Gets or sets a message indicating progress status.
		/// </summary>
		/// <returns>The status message</returns>
		/// ------------------------------------------------------------------------------------
		public string Message
		{
			get
			{
				CheckDisposed();
				return lblStatusMessage.Text;
			}
			set
			{
				CheckDisposed();
				lblStatusMessage.Text = value;
			}
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Gets or sets the maximum number of steps or increments corresponding to a progress
		/// bar that's 100% filled.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		public int Maximum
		{
			get
			{
				CheckDisposed();
				return progressBar.Maximum;
			}
			set
			{
				CheckDisposed();
				progressBar.Maximum = value;
			}
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Gets or sets the minimum number of steps or increments corresponding to a progress
		/// bar that's 100% empty.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		public int Minimum
		{
			get
			{
				CheckDisposed();
				return progressBar.Minimum;
			}
			set
			{
				CheckDisposed();
				progressBar.Minimum = value;
			}
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Gets or sets a value indicating the number of steps (or increments) having been
		/// completed.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		public int Position
		{
			get
			{
				CheckDisposed();
				return progressBar.Value;
			}
			set
			{
				CheckDisposed();
				if (value < progressBar.Minimum)
					progressBar.Value = progressBar.Minimum;
				else if (m_fRestartable)
					progressBar.Value = (value > progressBar.Maximum) ? 0 : value;
				else
					progressBar.Value = (value > progressBar.Maximum) ? progressBar.Maximum : value;
			}
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Steps the specified step.
		/// </summary>
		/// <param name="step">The step.</param>
		/// ------------------------------------------------------------------------------------
		public void Step(int step)
		{
			CheckDisposed();
			Position += step;
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Get the title of the progress display window.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		public string Title
		{
			get
			{
				CheckDisposed();
				return Text;
			}
			set
			{
				CheckDisposed();
				Text = value;
			}
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Sets the size of the step.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		public int StepSize
		{
			get
			{
				CheckDisposed();
				return progressBar.Step;
			}
			set
			{
				CheckDisposed();
				progressBar.Step = value;
			}
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Gets or sets a value indicating whether or not the progress indicator can restart
		/// at zero if it goes beyond the end.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		public bool Restartable
		{
			get
			{
				CheckDisposed();

				return m_fRestartable;
			}
			set
			{
				CheckDisposed();

				m_fRestartable = value;
			}
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Gets or sets a value indicating whether or not the cancel button is visible.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		public bool AllowCancel
		{
			get
			{
				CheckDisposed();
				return btnCancel.Visible && lblCancel.Visible;
			}
			set
			{
				CheckDisposed();
				btnCancel.Visible = value;
				lblCancel.Visible = value;

				if (Visible && value)
				{
					// If the cancel button is showing then make sure that the user thinks he can
					// press it by changing the cursor from an hourglass to a busy cursor.
					Cursor = Cursors.AppStarting;
				}
			}
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Gets or sets the text on the cancel button.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		public string CancelButtonText
		{
			get
			{
				CheckDisposed();
				return btnCancel.Text;
			}
			set
			{
				CheckDisposed();
				btnCancel.Text = value;
			}
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Gets or sets a label for Canceling button.
		/// </summary>
		/// <returns>The status message</returns>
		/// ------------------------------------------------------------------------------------
		public string CancelLabelText
		{
			get
			{
				CheckDisposed();
				return lblCancel.Text;
			}
			set
			{
				CheckDisposed();
				lblCancel.Text = value;
				Size sz = new Size(lblCancel.Width, int.MaxValue);
				sz = TextRenderer.MeasureText(lblCancel.Text, lblCancel.Font, sz, TextFormatFlags.WordBreak);
				lblCancel.Height = sz.Height;
			}
		}

		/// <summary>
		/// Gets an object to be used for ensuring that required tasks are invoked on the main
		/// UI thread.
		/// </summary>
		public ISynchronizeInvoke SynchronizeInvoke
		{
			get
			{
				CheckDisposed();
				return this;
			}
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Gets the form  displaying the progress (used for message box owners, etc).
		/// </summary>
		/// ------------------------------------------------------------------------------------
		public Form Form
		{
			get
			{
				CheckDisposed();
				return this;
			}
		}

		/// <summary>
		/// Gets or sets a value indicating whether this progress is indeterminate.
		/// </summary>
		public bool IsIndeterminate
		{
			get
			{
				CheckDisposed();
				return progressBar.Style == ProgressBarStyle.Marquee;
			}
			set
			{
				CheckDisposed();
				progressBar.Style = value ? ProgressBarStyle.Marquee : ProgressBarStyle.Continuous;
			}
		}
		#endregion

		#region Misc. Methods
		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Check to see if the object has been disposed.
		/// All public Properties and Methods should call this
		/// before doing anything else.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		public void CheckDisposed()
		{
			if (IsDisposed)
			{
				throw new ObjectDisposedException(
					string.Format("'{0}' in use after being disposed.", GetType().Name));
			}
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// If no one has subscribed to the cancel event, then don't bother showing the cancel
		/// button.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		protected override void OnVisibleChanged(EventArgs e)
		{
			CheckDisposed();
			base.OnVisibleChanged(e);

			if (Visible)
			{
				// If the cancel button is showing then make sure that the user thinks he can
				// press it by changing the cursor from an hourglass to a busy cursor.
				Cursor = Cursors.AppStarting;
			}
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Calls subscribers to the cancel event.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		protected virtual void OnCancel()
		{
			CheckDisposed();
			if (Canceling != null)
			{
				var cea = new CancelEventArgs();
				Canceling(this, cea);
				btnCancel.Enabled = cea.Cancel;
			}
		}

		#endregion

		#region Event Handlers
		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Occurs when the cancel button is pressed.
		/// </summary>
		/// <param name="sender"></param>
		/// <param name="e"></param>
		/// ------------------------------------------------------------------------------------
		protected internal void btnCancel_Click(object sender, EventArgs e)
		{
			OnCancel();
		}

		#endregion
	}
}
