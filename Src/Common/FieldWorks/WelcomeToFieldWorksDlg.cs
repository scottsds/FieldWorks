// Copyright (c) 2003-2017 SIL International
// This software is licensed under the LGPL, version 2.1 or later
// (http://www.gnu.org/licenses/lgpl-2.1.html)

using System;
using System.ComponentModel;
using System.Drawing;
using System.Windows.Forms;
using SIL.FieldWorks.Common.FwUtils;
using SIL.Reporting;
using SIL.LCModel.Utils;

namespace SIL.FieldWorks
{
	/// ----------------------------------------------------------------------------------------
	/// <summary>
	/// Dialog presenting multiple options for how to begin a FLEx session
	/// </summary>
	/// ----------------------------------------------------------------------------------------
	internal partial class WelcomeToFieldWorksDlg : Form
	{
		private string m_helpTopic = "khtpWelcomeToFieldworks";
		private readonly HelpProvider helpProvider;

		/// ------------------------------------------------------------------------------------
		/// <summary>
		///
		/// </summary>
		/// ------------------------------------------------------------------------------------
		public enum ButtonPress
		{
			/// <summary></summary>
			Open,
			/// <summary></summary>
			New,
			/// <summary></summary>
			Restore,
			/// <summary></summary>
			Exit,
			/// <summary>
			/// Receive a project through S/R LiftBridge or FLExBridge
			/// </summary>
			Receive,
			/// <summary>
			/// Import an SFM data set into a new FLEx project
			/// </summary>
			Import,
			/// <summary>
			/// Clicked the Sample/LastEdited project link
			/// </summary>
			Link,
		}

		#region Construction, Initialization and Deconstruction

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Initializes a new instance of the <see cref="WelcomeToFieldWorksDlg"/> class.
		/// </summary>
		/// <param name="helpTopicProvider">Help topic provider</param>
		/// <param name="exception">Exception that was thrown if the previously requested
		/// project could not be opened.</param>
		/// <param name="showReportingRow">True (usually only on the first run) when we want to show the first-time warning about
		/// sending google analytics information</param>
		/// ------------------------------------------------------------------------------------
		public WelcomeToFieldWorksDlg(IHelpTopicProvider helpTopicProvider, StartupException exception, bool showReportingRow)
		{
			InitializeComponent();
			AccessibleName = GetType().Name;
			var fullAppName = Properties.Resources.kstidFLEx;
			SetCheckboxText = fullAppName;  // Setter uses the app name in a format string.

			if (exception == null || !exception.ReportToUser)
			{
				Text = fullAppName;
				Logger.WriteEvent("Opening 'Welcome to FieldWorks' dialog");
			}
			else
			{
				m_helpTopic = "khtpUnableToOpenProject";
				Text = Properties.Resources.kstidUnableToOpenProjectCaption;
				m_lblProjectLoadError.Text = exception.Message;
				Logger.WriteEvent("Opening 'Unable to Open Project' dialog");
			}

			if (!showReportingRow)
			{
				reportingInfoLayout.Visible = false;
			}

			m_helpTopicProvider = helpTopicProvider;
			helpProvider = new HelpProvider();
			helpProvider.HelpNamespace = FwDirectoryFinder.CodeDirectory + m_helpTopicProvider.GetHelpString("UserHelpFile");
			helpProvider.SetHelpKeyword(this, m_helpTopicProvider.GetHelpString(m_helpTopic));
			helpProvider.SetHelpNavigator(this, HelpNavigator.Topic);
			receiveButton.Enabled = FLExBridgeHelper.IsFlexBridgeInstalled();
		}

		public bool OpenLastProjectCheckboxIsChecked
		{
			get { return alwaysOpenLastProjectCheckBox.Checked; }
			set { alwaysOpenLastProjectCheckBox.Checked = value; }
		}

		public void SetFirstOrLastProjectText(bool firstTimeOpening)
		{
			m_sampleOrLastProjectLinkLabel.Text = firstTimeOpening ? Properties.Resources.ksOpenSampleProject :
					Properties.Resources.ksOpenLastEditedProject;
		}

		public string ProjectLinkUiName
		{
			get { return m_openSampleOrLastProjectLink.Text; }
			set { m_openSampleOrLastProjectLink.Text = value; }
		}

		private string SetCheckboxText
		{
			set { alwaysOpenLastProjectCheckBox.Text = string.Format(Properties.Resources.ksWelcomeDialogCheckboxText, value); }
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Clean up any resources being used.
		/// </summary>
		/// <param name="disposing"><c>true</c> to release both managed and unmanaged
		/// resources; <c>false</c> to release only unmanaged resources.
		/// </param>
		/// ------------------------------------------------------------------------------------
		protected override void Dispose( bool disposing )
		{
			System.Diagnostics.Debug.WriteLineIf(!disposing, "****** Missing Dispose() call for " + GetType().Name + ". ****** ");
			// Must not be run more than once.
			if (IsDisposed)
				return;

			if( disposing )
			{
				if(components != null)
				{
					components.Dispose();
				}
			}
			base.Dispose( disposing );
		}

		/// <summary>
		/// Check to see if the object has been disposed.
		/// All public Properties and Methods should call this
		/// before doing anything else.
		/// </summary>
		public void CheckDisposed()
		{
			if (IsDisposed)
				throw new ObjectDisposedException(String.Format("'{0}' in use after being disposed.", GetType().Name));
		}
		#endregion

		#region Properties
		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Gets the button that was pressed
		/// </summary>
		/// ------------------------------------------------------------------------------------
		public ButtonPress DlgResult
		{
			get
			{
				CheckDisposed();
				return m_dlgResult;
			}
		}
		#endregion

		#region Overriden methods
		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Log the dialog result
		/// </summary>
		/// <param name="e"></param>
		/// ------------------------------------------------------------------------------------
		protected override void OnClosing(CancelEventArgs e)
		{
			Logger.WriteEvent("Closing dialog: " + m_dlgResult);
			base.OnClosing (e);
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// When the dialog is loaded, make sure it gets focused.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		protected override void OnShown(EventArgs e)
		{
			base.OnShown(e);
			// This dialog may be created when no other forms are active. Calling Activate will
			// make sure that the dialog comes up visible and activated.
			Activate();

			if (MiscUtils.IsUnix)
				ReLayoutCorrectly();
		}

		/// <summary>
		/// Adjust dialog so that the height of the main FlowLayoutPanel is the right value to
		/// contain the displayed controls.
		/// A better solution will be to fix Mono FlowLayoutPanel to not include
		/// non-Visible controls in the FlowLayoutPanel height calculation, if that is what
		/// it is doing when it AutoSizes.
		/// </summary>
		private void ReLayoutCorrectly()
		{
			var shrunkWidth = this.mainVerticalLayout.Width;
			this.mainVerticalLayout.AutoSize = false;
			this.mainVerticalLayout.Width = shrunkWidth;

			var heightOfVisibleControls = 0;
			foreach (Control control in this.mainVerticalLayout.Controls)
			{
				if (control.Visible == false)
					continue;
				heightOfVisibleControls += control.Height;
				heightOfVisibleControls += control.Margin.Top;
				heightOfVisibleControls += control.Margin.Bottom;
			}
			this.mainVerticalLayout.Height = heightOfVisibleControls;
		}
		#endregion

		#region Button click handlers
		/// ------------------------------------------------------------------------------------
		/// <summary>
		///
		/// </summary>
		/// <param name="sender"></param>
		/// <param name="e"></param>
		/// ------------------------------------------------------------------------------------
		private void m_btnOpen_Click(object sender, EventArgs e)
		{
			m_dlgResult = ButtonPress.Open;
			Hide();
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		///
		/// </summary>
		/// <param name="sender"></param>
		/// <param name="e"></param>
		/// ------------------------------------------------------------------------------------
		private void m_btnNew_Click(object sender, EventArgs e)
		{
			m_dlgResult = ButtonPress.New;
			Hide();
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		///
		/// </summary>
		/// <param name="sender"></param>
		/// <param name="e"></param>
		/// ------------------------------------------------------------------------------------
		private void m_btnRestore_Click(object sender, EventArgs e)
		{
			m_dlgResult = ButtonPress.Restore;
			Hide();
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		///
		/// </summary>
		/// <param name="sender"></param>
		/// <param name="e"></param>
		/// ------------------------------------------------------------------------------------
		private void m_btnExit_Click(object sender, EventArgs e)
		{
			m_dlgResult = ButtonPress.Exit;
			Hide();
		}

		private void m_openProjectLink_LinkClicked(object sender, LinkLabelLinkClickedEventArgs e)
		{
			m_dlgResult = ButtonPress.Link;
			Hide();
		}

		private void Receive_Click(object sender, EventArgs e)
		{
			m_dlgResult = ButtonPress.Receive;
			Hide();
		}

		private void Import_Click(object sender, EventArgs e)
		{
			m_dlgResult = ButtonPress.Import;
			Hide();
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Open the context-sensitive help for this dialog.
		/// </summary>
		/// <param name="sender"></param>
		/// <param name="e"></param>
		/// ------------------------------------------------------------------------------------
		private void m_btnHelp_Click(object sender, EventArgs e)
		{
			ShowHelp.ShowHelpTopic(m_helpTopicProvider, m_helpTopic);
		}

		#endregion

		internal void ShowErrorLabelHideLink()
		{
			m_sampleOrLastProjectLinkLabel.Visible = false;
			m_openSampleOrLastProjectLink.Visible = false;
			m_lblProjectLoadError.Visible = true;
			if(!string.IsNullOrEmpty(m_lblProjectLoadError.Text))
				Icon = SystemIcons.Exclamation;
		}

		internal void ShowLinkHideErrorLabel()
		{
			m_lblProjectLoadError.Visible = false;
			m_sampleOrLastProjectLinkLabel.Visible = true;
			m_openSampleOrLastProjectLink.Visible = true;
		}
	}
}
