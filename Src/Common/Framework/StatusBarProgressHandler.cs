// Copyright (c) 2006-2017 SIL International
// This software is licensed under the LGPL, version 2.1 or later
// (http://www.gnu.org/licenses/lgpl-2.1.html)

using System;
using System.ComponentModel;
using System.Windows.Forms;
using SIL.LCModel.Utils;

namespace SIL.FieldWorks.Common.Framework
{
	/// ----------------------------------------------------------------------------------------
	/// <summary>
	/// Handles displaying progress in the status bar.
	/// </summary>
	/// ----------------------------------------------------------------------------------------
	public class StatusBarProgressHandler: IProgress, IDisposable
	{
		event CancelEventHandler IProgress.Canceling
		{
			add { throw new NotImplementedException(); }
			remove { throw new NotImplementedException(); }
		}

		private readonly ToolStripStatusLabel m_label;
		private readonly ToolStripProgressBar m_progressBar;
		private Control m_control;

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Initializes a new instance of the <see cref="StatusBarProgressHandler"/> class.
		/// </summary>
		/// <param name="label">The label that will display the message.</param>
		/// <param name="progressBar">The progress bar.</param>
		/// ------------------------------------------------------------------------------------
		public StatusBarProgressHandler(ToolStripStatusLabel label,
			ToolStripProgressBar progressBar)
		{
			m_label = label;
			m_progressBar = progressBar;

			// Create a (invisible) control for multithreading purposes. We have to do this
			// because ToolStripStatusLabel and ToolStripProgressBar don't derive from Control
			// and so don't provide an implementation of Invoke.
			m_control = new Control();
			m_control.CreateControl();
		}

		#region IProgress Members

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Sets a Message
		/// </summary>
		/// <value></value>
		/// <returns>A System.String</returns>
		/// ------------------------------------------------------------------------------------
		public string Message
		{
			get
			{
				if (m_label != null)
				{
					if (m_control.InvokeRequired)
						return (string) m_control.Invoke((Func<string>) (() => m_label.Text));
					return m_label.Text;
				}
				return null;
			}

			set
			{
				if (m_label != null)
				{
					if (m_control.InvokeRequired)
						m_control.BeginInvoke((Action<string>) (s => m_label.Text = s), value);
					else
						m_label.Text = value;
				}
			}
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Sets a Position
		/// </summary>
		/// <value></value>
		/// <returns>A System.Int32</returns>
		/// ------------------------------------------------------------------------------------
		public int Position
		{
			get
			{
				if (m_progressBar != null)
				{
					if (m_control.InvokeRequired)
						return (int) m_control.Invoke((Func<int>) (() => m_progressBar.Value));
					return m_progressBar.Value;
				}
				return -1;
			}

			set
			{
				if (m_progressBar != null)
				{
					if (m_control.InvokeRequired)
						m_control.BeginInvoke((Action<int>) (i => m_progressBar.Value = i), value);
					else
						m_progressBar.Value = value;
				}
			}
		}

		/// <summary>
		/// Gets or sets the minimum value of the progress bar.
		/// </summary>
		/// <value>The minimum.</value>
		public int Minimum
		{
			get
			{
				if (m_progressBar != null)
				{
					if (m_control.InvokeRequired)
						return (int) m_control.Invoke((Func<int>) (() => m_progressBar.Minimum));
					return m_progressBar.Minimum;
				}
				return -1;
			}

			set
			{
				if (m_progressBar != null)
				{
					if (m_control.InvokeRequired)
						m_control.BeginInvoke((Action<int>) (i => m_progressBar.Minimum = i), value);
					else
						m_progressBar.Minimum = value;
				}
			}
		}

		/// <summary>
		/// Gets or sets the maximum value of the progress bar.
		/// </summary>
		/// <value>The maximum.</value>
		public int Maximum
		{
			get
			{
				if (m_progressBar != null)
				{
					if (m_control.InvokeRequired)
						return (int)m_control.Invoke((Func<int>)(() => m_progressBar.Maximum));
					return m_progressBar.Maximum;
				}
				return -1;
			}

			set
			{
				if (m_progressBar != null)
				{
					if (m_control.InvokeRequired)
						m_control.BeginInvoke((Action<int>)(i => m_progressBar.Maximum = i), value);
					else
						m_progressBar.Maximum = value;
				}
			}
		}

		/// <summary>
		/// Gets or sets a value indicating whether the task has been canceled.
		/// </summary>
		/// <value><c>true</c> if canceled; otherwise, <c>false</c>.</value>
		public bool Canceled
		{
			get { return false; }
		}

		/// <summary>
		/// Gets an object to be used for ensuring that required tasks are invoked on the main
		/// UI thread.
		/// </summary>
		public ISynchronizeInvoke SynchronizeInvoke
		{
			get { return m_progressBar.Control; }
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Gets the progress as a form (used for message box owners, etc).
		/// </summary>
		/// ------------------------------------------------------------------------------------
		public Form Form
		{
			get { return m_progressBar.Control.FindForm(); }
		}

		/// <summary>
		/// Gets or sets a value indicating whether this progress is indeterminate.
		/// </summary>
		public bool IsIndeterminate
		{
			get { return m_progressBar.Style == ProgressBarStyle.Marquee; }
			set { m_progressBar.Style = value ? ProgressBarStyle.Marquee : ProgressBarStyle.Blocks; }
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Gets or sets a value indicating whether the opertation executing on the separate thread
		/// can be cancelled by a different thread (typically the main UI thread).
		/// </summary>
		/// ------------------------------------------------------------------------------------
		public bool AllowCancel
		{
			get { return false; }
			set { throw new NotImplementedException(); }
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Member Step
		/// </summary>
		/// <param name="nStepAmt">nStepAmt</param>
		/// ------------------------------------------------------------------------------------
		public void Step(int nStepAmt)
		{
			if (m_progressBar == null)
				return;

			if (m_control.InvokeRequired)
			{
				m_control.BeginInvoke((Action<int>) Step, nStepAmt);
			}
			else
			{
				if (nStepAmt == 0)
					m_progressBar.PerformStep();
				else
				{
					if (m_progressBar.Value + nStepAmt > m_progressBar.Maximum)
					{
						m_progressBar.Value = m_progressBar.Minimum +
							(m_progressBar.Value + nStepAmt - m_progressBar.Maximum);
					}
					else
						m_progressBar.Value += nStepAmt;
				}
			}
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Sets a StepSize
		/// </summary>
		/// <value></value>
		/// <returns>A System.Int32</returns>
		/// ------------------------------------------------------------------------------------
		public int StepSize
		{
			get
			{
				if (m_progressBar != null)
				{
					if (m_control.InvokeRequired)
						return (int) m_control.Invoke((Func<int>) (() => m_progressBar.Step));
					return m_progressBar.Step;
				}
				return -1;
			}

			set
			{
				if (m_progressBar != null)
				{
					if (m_control.InvokeRequired)
						m_control.BeginInvoke((Action<int>) (i => m_progressBar.Step = i), value);
					else
						m_progressBar.Step = value;
				}
			}
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Sets a Title
		/// </summary>
		/// <value></value>
		/// <returns>A System.String</returns>
		/// ------------------------------------------------------------------------------------
		public string Title
		{
			get { throw new NotImplementedException(); }
			set { throw new NotImplementedException(); }
		}

		#endregion

		#region Disposing stuff

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// This method throws an ObjectDisposedException if IsDisposed returns
		/// true.  This is the case where a method or property in an object is being
		/// used but the object itself is no longer valid.
		/// This method should be added to all public properties and methods of this
		/// object and all other objects derived from it (extensive).
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
		/// Add the public property for knowing if the object has been disposed of yet
		/// </summary>
		/// <value></value>
		/// ------------------------------------------------------------------------------------
		public bool IsDisposed { get; private set; }

		#region IDisposable Members

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Performs application-defined tasks associated with freeing, releasing, or
		/// resetting unmanaged resources.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		public void Dispose()
		{
			Dispose(true);
			GC.SuppressFinalize(this);
		}

		#endregion

#if DEBUG
		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Releases unmanaged resources and performs other cleanup operations before the
		/// <see cref="T:SIL.FieldWorks.Common.Framework.StatusBarProgressHandler"/> is
		/// reclaimed by garbage collection.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		~StatusBarProgressHandler()
		{
			Dispose(false);
		}
#endif

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Dispose method
		/// </summary>
		/// <param name="fFromDispose">set to <c>true</c> if it is save to access managed
		/// member variables, <c>false</c> if only unmanaged member variables should be
		/// accessed.</param>
		/// ------------------------------------------------------------------------------------
		protected virtual void Dispose(bool fFromDispose)
		{
			System.Diagnostics.Debug.WriteLineIf(!fFromDispose, "****************** Missing Dispose() call for " + GetType().Name + ". ******************");

			if (fFromDispose)
			{
				if (m_control != null)
					m_control.Dispose();
			}
			m_control = null;

			IsDisposed = true;
		}
		#endregion
	}
}
