﻿// Copyright (c) 2015 SIL International
// This software is licensed under the LGPL, version 2.1 or later
// (http://www.gnu.org/licenses/lgpl-2.1.html)

using System.Drawing;
using System.Windows.Forms;
using SIL.FieldWorks.Resources;

namespace SIL.FieldWorks.FwCoreDlgControls
{
	/// <summary>
	/// This control simply draws the blue circle that FieldWorks often uses as an indication that a popup menu is available.
	/// </summary>
	public partial class BlueCircleButton : Control
	{
		private Image m_blueCircle;
		/// <summary>
		/// Stupid mandatory comment.
		/// </summary>
		public BlueCircleButton()
		{
			InitializeComponent();

			m_blueCircle = ResourceHelper.BlueCircleDownArrowForView;
			Height = m_blueCircle.Height + 3;
			Width = m_blueCircle.Width + 3;
			Cursor = Cursors.Arrow;
		}

		/// <summary>
		/// Stupid mandatory comment.
		/// </summary>
		/// <param name="pe"></param>
		protected override void OnPaint(PaintEventArgs pe)
		{
			pe.Graphics.DrawImage(m_blueCircle, 0, 0);
		}
	}
}
