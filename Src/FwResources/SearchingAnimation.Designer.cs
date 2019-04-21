// Copyright (c) 2015 SIL International
// This software is licensed under the LGPL, version 2.1 or later
// (http://www.gnu.org/licenses/lgpl-2.1.html)

using SIL.PlatformUtilities;

namespace SIL.FieldWorks.Resources
{
	partial class SearchingAnimation
	{
		/// <summary>
		/// Required designer variable.
		/// </summary>
		private System.ComponentModel.IContainer components = null;

		/// <summary>
		/// Clean up any resources being used.
		/// </summary>
		/// <param name="disposing">true if managed resources should be disposed; otherwise, false.</param>
		protected override void Dispose(bool disposing)
		{
			System.Diagnostics.Debug.WriteLineIf(!disposing, "****** Missing Dispose() call for " + GetType().Name + ". ****** ");
			if (disposing && (components != null))
			{
				components.Dispose();
			}
			base.Dispose(disposing);
		}

		#region Component Designer generated code

		/// <summary>
		/// Required method for Designer support - do not modify
		/// the contents of this method with the code editor.
		/// </summary>
		private void InitializeComponent()
		{
			System.ComponentModel.ComponentResourceManager resources = null;
			if (!Platform.IsMono)
			{
				// TODO-Linux FWNX-222: work around mono giflib inadequacies
				resources = new System.ComponentModel.ComponentResourceManager(typeof(SearchingAnimation));
			}

			this.pictureBox1 = new System.Windows.Forms.PictureBox();
			((System.ComponentModel.ISupportInitialize)(this.pictureBox1)).BeginInit();
			this.SuspendLayout();
			//
			// pictureBox1
			//
			this.pictureBox1.Dock = System.Windows.Forms.DockStyle.Fill;

			if (Platform.IsMono)
				this.pictureBox1.Image = new System.Drawing.Bitmap(32,23);
			else
			{
				// TODO-Linux FWNX-222: work around mono giflib inadequacies
				this.pictureBox1.Image = ((System.Drawing.Image)(resources.GetObject("pictureBox1.Image")));
			}

			this.pictureBox1.Location = new System.Drawing.Point(0, 0);
			this.pictureBox1.Name = "pictureBox1";
			this.pictureBox1.Size = new System.Drawing.Size(48, 50);
			this.pictureBox1.TabIndex = 0;
			this.pictureBox1.TabStop = false;
			//
			// SearcjAnimation
			//
			this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
			this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
			this.Controls.Add(this.pictureBox1);
			this.Name = "SearcjAnimation";
			this.Size = new System.Drawing.Size(48, 50);
			((System.ComponentModel.ISupportInitialize)(this.pictureBox1)).EndInit();
			this.ResumeLayout(false);

		}

		#endregion

		private System.Windows.Forms.PictureBox pictureBox1;
	}
}
