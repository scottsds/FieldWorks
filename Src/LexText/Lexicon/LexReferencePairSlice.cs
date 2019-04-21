// Copyright (c) 2015 SIL International
// This software is licensed under the LGPL, version 2.1 or later
// (http://www.gnu.org/licenses/lgpl-2.1.html)

using System.Diagnostics;

using SIL.LCModel;
using SIL.FieldWorks.Common.Framework.DetailControls;
using XCore;
using SIL.Utils;

namespace SIL.FieldWorks.XWorks.LexEd
{
	/// <summary>
	/// LexReferencePairSlice is used to support selecting
	/// of a Sense or Entry tree.
	/// </summary>
	public class LexReferencePairSlice : CustomAtomicReferenceSlice, ILexReferenceSlice
	{
		/// <summary>
		/// Initializes a new instance of the <see cref="LexReferencePairSlice"/> class.
		/// Constructor must be public (and with no arguments) for creation by reflection
		/// based on mention in XML configuration files.
		/// </summary>
		public LexReferencePairSlice()
			: base(new LexReferencePairLauncher())
		{
		}

		/// <summary>
		/// Override method to add suitable control.
		/// </summary>
		public override void FinishInit()
		{
			CheckDisposed();
			Debug.Assert(m_cache != null);
			Debug.Assert(m_configurationNode != null);

			base.FinishInit();

			var hvoDisplayParent = XmlUtils.GetMandatoryIntegerAttributeValue(
				m_configurationNode, "hvoDisplayParent");
			((LexReferencePairLauncher)Control).DisplayParent = hvoDisplayParent != 0
				? m_cache.ServiceLocator.GetObject(hvoDisplayParent) : null;
		}

		#region ILexReferenceSlice Members

		public override bool HandleDeleteCommand(Command cmd)
		{
			CheckDisposed();
			((LexReferenceMultiSlice)m_parentSlice).DeleteReference(GetObjectForMenusToOperateOn() as ILexReference);
			return true; // delete was done
		}

		/// <summary>
		/// This method is called when the user selects "Add Reference" or "Replace Reference" under the
		/// dropdown menu for a lexical relation
		/// </summary>
		public override void HandleLaunchChooser()
		{
			CheckDisposed();
			((LexReferencePairLauncher)Control).LaunchChooser();
		}

		public override void HandleEditCommand()
		{
			CheckDisposed();
			((LexReferenceMultiSlice)m_parentSlice).EditReferenceDetails(GetObjectForMenusToOperateOn() as ILexReference);
		}

		#endregion
	}
}
