// SilSidePane, Copyright 2008-2016 SIL International. All rights reserved.
// SilSidePane is licensed under the Code Project Open License (CPOL), <http://www.codeproject.com/info/cpol10.aspx>.
// Derived from OutlookBar v2 2005 <http://www.codeproject.com/KB/vb/OutlookBar.aspx>, Copyright 2007 by Star Vega.
// Changed in 2008 and 2009 by SIL International to convert to C# and add more functionality.

using System.Drawing.Drawing2D;
using System.Windows.Forms;

namespace SIL.SilSidePane
{
	/// <summary>
	/// Color-themed panel
	/// </summary>
	internal class OutlookBarSubButtonPanel : Panel
	{
		/// <summary></summary>
		public OutlookBarSubButtonPanel()
		{
			DoubleBuffered = true;
			ResizeRedraw = true;
			AutoScroll = true;
			BorderStyle = BorderStyle.None;
		}

		protected override void Dispose(bool disposing)
		{
			System.Diagnostics.Debug.WriteLineIf(!disposing, "******* Missing Dispose() call for " + GetType() + ". *******");
			base.Dispose(disposing);
		}

		/// <summary></summary>
		protected override void OnPaint(PaintEventArgs e)
		{
			using (LinearGradientBrush br = new LinearGradientBrush(ClientRectangle,
				ProfessionalColors.ToolStripGradientMiddle,
				ProfessionalColors.ToolStripGradientEnd, LinearGradientMode.Vertical))
			{
				e.Graphics.FillRectangle(br, ClientRectangle);
			}
		}

		/// <summary></summary>
		protected override void OnScroll(ScrollEventArgs se)
		{
			base.OnScroll(se);
			Invalidate();
		}
	}
}