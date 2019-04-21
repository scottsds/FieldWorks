// Copyright (c) 2015-2018 SIL International
// This software is licensed under the LGPL, version 2.1 or later
// (http://www.gnu.org/licenses/lgpl-2.1.html)

using System;
using System.Drawing;
using System.Windows.Forms;
using SIL.LCModel;

namespace LCMBrowser
{
	/// ----------------------------------------------------------------------------------------
	/// <summary>
	///
	/// </summary>
	/// ----------------------------------------------------------------------------------------
	public partial class ClassPropertySelector : Form
	{
		private string m_fmtMsg;
		private bool m_showCmObjProps = true;

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Initializes a new instance of the <see cref="T:ClassPropertySelector"/> class.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		public ClassPropertySelector()
		{
			InitializeComponent();
			m_fmtMsg = lblMsg.Text;
			m_showCmObjProps = LCMClassList.ShowCmObjectProperties;
			cboClass.Items.AddRange(LCMClassList.AllFDOClasses.ToArray());
			cboClass.SelectedIndex = 0;
			cboClass_SelectionChangeCommitted(null, null);
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Initializes a new instance of the <see cref="T:ClassPropertySelector"/> class.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		public ClassPropertySelector(ICmObject obj)	: this()
		{
			if (obj == null)
				return;

			foreach (LCMClass cls in cboClass.Items)
			{
				if (obj.GetType() == cls.ClassType)
				{
					cboClass.SelectedItem = cls;
					cboClass_SelectionChangeCommitted(null, null);
				}
			}
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Handles the SelectionChangeCommitted event of the cboClass control.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		private void cboClass_SelectionChangeCommitted(object sender, EventArgs e)
		{
			LCMClass clsProps = cboClass.SelectedItem as LCMClass;
			lblMsg.Text = string.Format(m_fmtMsg, clsProps.ClassName);
			gridProperties.CellValueChanged -= gridProperties_CellValueChanged;
			gridProperties.Rows.Clear();

			foreach (LCMClassProperty prop in clsProps.Properties)
			{
				bool fIsDisplayedCmObjProp =
					(m_showCmObjProps || !LCMClassList.IsCmObjectProperty(prop.Name));

				int i = gridProperties.Rows.Add(prop.Displayed && fIsDisplayedCmObjProp, prop.Name, prop);
				gridProperties.Rows[i].ReadOnly = !fIsDisplayedCmObjProp;
			}

			gridProperties.CellValueChanged += gridProperties_CellValueChanged;
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Handles the CellValueChanged event of the gridProperties control.
		/// </summary>
		/// <param name="sender">The source of the event.</param>
		/// <param name="e">The <see cref="T:System.Windows.Forms.DataGridViewCellEventArgs"/> instance containing the event data.</param>
		/// ------------------------------------------------------------------------------------
		void gridProperties_CellValueChanged(object sender, DataGridViewCellEventArgs e)
		{
			if (e.ColumnIndex == 0 && e.RowIndex >= 0)
			{
				DataGridViewCellCollection cells = gridProperties.Rows[e.RowIndex].Cells;
				LCMClassProperty prop = cells[2].Value as LCMClassProperty;
				prop.Displayed = (bool)cells[0].Value;
			}
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Handles the CellFormatting event of the gridProperties control.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		private void gridProperties_CellFormatting(object sender, DataGridViewCellFormattingEventArgs e)
		{
			if (e.ColumnIndex == 1 && e.RowIndex >= 0 && !m_showCmObjProps)
			{
				DataGridViewCellCollection cells = gridProperties.Rows[e.RowIndex].Cells;
				LCMClassProperty prop = cells[2].Value as LCMClassProperty;
				if (LCMClassList.IsCmObjectProperty(prop.Name))
					e.CellStyle.ForeColor = SystemColors.GrayText;
			}
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Handles the CellPainting event of the gridProperties control.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		private void gridProperties_CellPainting(object sender, DataGridViewCellPaintingEventArgs e)
		{
			DataGridViewPaintParts parts = e.PaintParts;
			parts &= ~DataGridViewPaintParts.Focus;
			e.Paint(e.CellBounds, parts);
			e.Handled = true;
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// When space is pressed and the current cell in the grid is a property name, then
		/// force the check value in the first column to toggle.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		private void gridProperties_KeyPress(object sender, KeyPressEventArgs e)
		{
			Point cell = gridProperties.CurrentCellAddress;
			if (e.KeyChar == (char)Keys.Space && cell.X == 1 && cell.Y >= 0 &&
				!gridProperties.Rows[cell.Y].ReadOnly)
			{
				gridProperties[0, cell.Y].Value = !(bool)gridProperties[0, cell.Y].Value;
			}
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Handles the Click event of the btnOK control.
		/// </summary>
		/// <param name="sender">The source of the event.</param>
		/// <param name="e">The <see cref="T:System.EventArgs"/> instance containing the event data.</param>
		/// ------------------------------------------------------------------------------------
		private void btnOK_Click(object sender, EventArgs e)
		{
			LCMClassList.Save();
			Close();
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Handles the Click event of the btnCancel control.
		/// </summary>
		/// <param name="sender">The source of the event.</param>
		/// <param name="e">The <see cref="T:System.EventArgs"/> instance containing the event data.</param>
		/// ------------------------------------------------------------------------------------
		private void btnCancel_Click(object sender, EventArgs e)
		{
			// This will blow away any changes.
			LCMClassList.Reset();
			Close();
		}
	}
}
