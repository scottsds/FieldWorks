// Copyright (c) 2003-2017 SIL International
// This software is licensed under the LGPL, version 2.1 or later
// (http://www.gnu.org/licenses/lgpl-2.1.html)

using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.IO;
using System.Reflection;
using System.Windows.Forms;
using System.Xml;
using SIL.LCModel.Utils;
using SIL.Utils;

namespace XCore
{

	/// <summary>
	/// Summary description for HtmlViewer.
	/// </summary>
	/// <remarks>
	/// IxCoreContentControl includes IxCoreColleague now,
	/// so only IxCoreContentControl needs to be declared here.
	/// </remarks>
	public class HtmlViewer : XCoreUserControl, IxCoreContentControl
	{
		#region Data Members
		/// <summary>
		/// The control that shows the HTML data.
		/// </summary>
		protected HtmlControl m_htmlControl;
		/// <summary>
		/// Mediator that passes off messages.
		/// </summary>
		protected Mediator m_mediator;
		/// <summary>
		/// Property table that stores all manner of objects.
		/// </summary>
		protected PropertyTable m_propertyTable;
		/// <summary>
		/// Required designer variable.
		/// </summary>
		private Container components = null;

		#endregion // Data Members

		#region Construction and disposal
		/// -----------------------------------------------------------------------------------
		/// <summary>
		/// Initializes a new instance of the <see cref="HtmlViewer"/> class.
		/// </summary>
		/// -----------------------------------------------------------------------------------
		public HtmlViewer()
		{
			// This call is required by the Windows.Forms Form Designer.
			InitializeComponent();

			m_htmlControl = new HtmlControl();
			m_htmlControl.Dock = DockStyle.Fill;
			Controls.Add(m_htmlControl);

			AccNameDefault = "HtmlViewer";	// default accessibility name
		}

		/// -----------------------------------------------------------------------------------
		/// <summary>
		/// Clean up any resources being used.
		/// </summary>
		/// <param name="disposing"><c>true</c> to release both managed and unmanaged
		/// resources; <c>false</c> to release only unmanaged resources.
		/// </param>
		/// -----------------------------------------------------------------------------------
		protected override void Dispose(bool disposing)
		{
			//Debug.WriteLineIf(!disposing, "****************** " + GetType().Name + " 'disposing' is false. ******************");
			// Must not be run more than once.
			if (IsDisposed)
				return;

			if (disposing)
			{
				if (components != null)
					components.Dispose();
				if (m_mediator != null)
					m_mediator.RemoveColleague(this);
			}
			m_mediator = null;

			base.Dispose(disposing);
		}
		#endregion // Construction and disposal

		#region IxCoreColleague implementation

		/// <summary>
		/// Initialize.
		/// </summary>
		/// <param name="mediator"></param>
		/// <param name="propertyTable"></param>
		/// <param name="configurationParameters"></param>
		public virtual void Init(Mediator mediator, PropertyTable propertyTable, XmlNode configurationParameters)
		{
			CheckDisposed();

			m_mediator = mediator;
			m_propertyTable = propertyTable;
			m_configurationParameters = configurationParameters;
			mediator.AddColleague(this);
			string urlAttr = XmlUtils.GetMandatoryAttributeValue(m_configurationParameters, "URL");
			var uri = new Uri(GetInstallSubDirectory(urlAttr));
			m_htmlControl.URL = uri.AbsoluteUri;

		}

		/// <summary>
		/// Return an array of all of the objects which should
		/// 1) be queried when looking for someone to deliver a message to
		/// 2) be potential recipients of a broadcast
		/// </summary>
		/// <returns>An array of IxCoreColleague objects. Here it is just 'this'.</returns>
		public IxCoreColleague[] GetMessageTargets()
		{
			CheckDisposed();

			return new IxCoreColleague[]{this};
		}

		/// <summary>
		/// Should not be called if disposed.
		/// </summary>
		public bool ShouldNotCall
		{
			get { return IsDisposed; }
		}

		/// <summary>
		/// Mediator message handling Priority
		/// </summary>
		public int Priority
		{
			get { return (int)ColleaguePriority.Medium; }
		}

		#endregion // IxCoreColleague implementation

		#region IxCoreContentControl implementation

		/// <summary>
		/// From IxCoreContentControl
		/// </summary>
		/// <returns>true if ok to go away</returns>
		public bool PrepareToGoAway()
		{
			CheckDisposed();

			return true;
		}

		public string AreaName
		{
			get
			{
				CheckDisposed();

				return XmlUtils.GetOptionalAttributeValue( m_configurationParameters, "area", "unknown");
			}
		}

		#endregion // IxCoreContentControl implementation

		#region IxCoreCtrlTabProvider implementation

		public Control PopulateCtrlTabTargetCandidateList(List<Control> targetCandidates)
		{
			if (targetCandidates == null)
				throw new ArgumentNullException("targetCandidates");

			targetCandidates.Add(this);

			return ContainsFocus ? this : null;
		}

		#endregion  IxCoreCtrlTabProvider implementation

		protected string GetInstallSubDirectory(string subDirectory)
		{
			Debug.Assert(subDirectory != null);

			string retval = subDirectory.Trim();
			if (retval.StartsWith(@"\") || retval.StartsWith("/"))
				retval = retval.Remove(0, 1);
			string asmPathname = Assembly.GetExecutingAssembly().CodeBase;
			asmPathname = FileUtils.StripFilePrefix(asmPathname);
			string asmPath = asmPathname.Substring(0, asmPathname.LastIndexOf("/", StringComparison.Ordinal) + 1);
			string possiblePath = Path.Combine(asmPath, retval);
			if (File.Exists(possiblePath))
				retval = possiblePath;
			// Implicit 'else' assumes it to be a full path,
			// but not in the install folder structure.
			// Sure hope the caller can handle it.
			return retval;
		}

		#region Component Designer generated code
		/// -----------------------------------------------------------------------------------
		/// <summary>
		/// Required method for Designer support - do not modify
		/// the contents of this method with the code editor.
		/// </summary>
		/// -----------------------------------------------------------------------------------
		private void InitializeComponent()
		{
			//
			// HtmlViewer
			//
			this.Name = "HtmlViewer";
			this.Size = new System.Drawing.Size(600, 320);

		}
		#endregion
	}
}
