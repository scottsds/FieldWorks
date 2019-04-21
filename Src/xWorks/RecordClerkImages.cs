// Copyright (c) 2003-2017 SIL International
// This software is licensed under the LGPL, version 2.1 or later
// (http://www.gnu.org/licenses/lgpl-2.1.html)

using System;
using System.Windows.Forms;

namespace SIL.FieldWorks.XWorks
{
	/// <summary>
	/// Summary description for ImageHolder.
	/// </summary>
	public class RecordClerkImages : UserControl
	{
		public System.Windows.Forms.ImageList buttonImages;
		private System.ComponentModel.IContainer components;

		/// -----------------------------------------------------------------------------------
		/// <summary>
		/// Initializes a new instance of the <see cref="RecordClerkImages"/> class.
		/// </summary>
		/// -----------------------------------------------------------------------------------
		public RecordClerkImages()
		{
			// This call is required by the Windows.Forms Form Designer.
			InitializeComponent();

			// TODO: Add any initialization after the InitForm call

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

		/// -----------------------------------------------------------------------------------
		/// <summary>
		/// Clean up any resources being used.
		/// </summary>
		/// <param name="disposing"><c>true</c> to release both managed and unmanaged
		/// resources; <c>false</c> to release only unmanaged resources.
		/// </param>
		/// -----------------------------------------------------------------------------------
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

		#region Component Designer generated code
		/// -----------------------------------------------------------------------------------
		/// <summary>
		/// Required method for Designer support - do not modify
		/// the contents of this method with the code editor.
		/// </summary>
		/// -----------------------------------------------------------------------------------
		private void InitializeComponent()
		{
			this.components = new System.ComponentModel.Container();
			System.ComponentModel.ComponentResourceManager resources = new System.ComponentModel.ComponentResourceManager(typeof(RecordClerkImages));
			this.buttonImages = new System.Windows.Forms.ImageList(this.components);
			this.SuspendLayout();
			//
			// buttonImages
			//
			this.buttonImages.ImageStream = ((System.Windows.Forms.ImageListStreamer)(resources.GetObject("buttonImages.ImageStream")));
			this.buttonImages.TransparentColor = System.Drawing.Color.Magenta;
			this.buttonImages.Images.SetKeyName(0, "");
			this.buttonImages.Images.SetKeyName(1, "");
			this.buttonImages.Images.SetKeyName(2, "");
			this.buttonImages.Images.SetKeyName(3, "");
			this.buttonImages.Images.SetKeyName(4, "sendReceive16x16.png");
			this.buttonImages.Images.SetKeyName(5, "SendReceiveGetArrow16x16.png");
			this.buttonImages.Images.SetKeyName(6, "chorus16.png");
			this.buttonImages.Images.SetKeyName(7, "sendReceiveFirst16x16.png");
			//
			// RecordClerkImages
			//
			this.Name = "RecordClerkImages";
			this.ResumeLayout(false);

		}
		#endregion
	}
}
