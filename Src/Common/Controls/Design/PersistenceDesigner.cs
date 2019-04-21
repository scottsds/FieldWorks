// Copyright (c) 2002-2013 SIL International
// This software is licensed under the LGPL, version 2.1 or later
// (http://www.gnu.org/licenses/lgpl-2.1.html)
//
// File: PersistenceDesigner.cs
// Responsibility: EberhardB
// Last reviewed:
//
// <remarks>
// Implementation of PersistenceDesigner. This allows to set the parent property when the
// psersistence component is inserted in design view.
// </remarks>
//
// -------------------------------------------------------------------------------------------
using System;
using System.Windows.Forms;
using System.ComponentModel;
using System.ComponentModel.Design;
using System.Reflection;

namespace SIL.FieldWorks.Common.Controls.Design
{
	/// <summary>
	/// Set the Parent property at design time. This allows simple dropping of the component
	/// on a form/control.
	/// </summary>
	public class PersistenceDesigner: ComponentDesigner
	{

		/// <summary>
		/// Set the Parent property to the control where we are hosted. If we aren't hosted
		/// in a control, do nothing.
		/// </summary>
		/// <param name="c"></param>
		public override void Initialize(IComponent c)
		{
			base.Initialize(c);

			var host = (IDesignerHost)GetService(typeof(IDesignerHost));
			var control = host?.RootComponent as Control;

			if (control == null)
				return;

			// We invoke the Parent property by reflection. This makes the compile time
			// dependenceis easier to handle. For the compiler we are independant of
			// FwControls.
			Type t = Type.GetType("SIL.FieldWorks.Common.Controls.Persistence");

			t.InvokeMember("Parent",
				BindingFlags.DeclaredOnly | BindingFlags.Public |
				BindingFlags.SetProperty | BindingFlags.Instance, null, c, new object[] { control });

		}

		/// <summary/>
		protected override void Dispose(bool disposing)
		{
			System.Diagnostics.Debug.WriteLineIf(!disposing, "****** Missing Dispose() call for " + GetType() + ". ******");
			base.Dispose(disposing);
		}
	}
}
