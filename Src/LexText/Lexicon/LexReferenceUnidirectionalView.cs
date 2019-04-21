// Copyright (c) 2015-2017 SIL International
// This software is licensed under the LGPL, version 2.1 or later
// (http://www.gnu.org/licenses/lgpl-2.1.html)

using SIL.LCModel;
using SIL.FieldWorks.Common.ViewsInterfaces;
using SIL.FieldWorks.Common.Framework.DetailControls;
using SIL.LCModel.Core.KernelInterfaces;
using System.Collections.Generic;

namespace SIL.FieldWorks.XWorks.LexEd
{
	/// <summary>
	/// Summary description for LexReferenceUnidirectionalView.
	/// </summary>
	public class LexReferenceUnidirectionalView : VectorReferenceView
	{
		public LexReferenceUnidirectionalView()
		{
			// This call is required by the Windows.Forms Form Designer.
			InitializeComponent();
		}

		protected override VectorReferenceVc CreateVectorReferenceVc()
		{
			return new LexReferenceUnidirectionalVc(m_cache, m_rootFlid, m_displayNameProperty, m_displayWs);
		}

		protected override void Delete()
		{
			Delete(LexEdStrings.ksUndoDeleteRef, LexEdStrings.ksRedoDeleteRef);
		}

		protected override void UpdateTimeStampsIfNeeded(int[] hvos)
		{
#if WANTPORTMULTI
			for (int i = 0; i < hvos.Length; ++i)
			{
				ICmObject cmo = ICmObject.CreateFromDBObject(m_cache, hvos[i]);
				(cmo as ICmObject).UpdateTimestampForVirtualChange();
			}
#endif
		}

		/// <summary>
		/// Put the hidden item back into the list of visible items to make the list that should be stored in the property.
		/// </summary>
		/// <param name="items"></param>
		protected override void AddHiddenItems(List<ICmObject> items)
		{
			var allItems = base.GetVisibleItemList();
			if (allItems.Count != 0)
				items.Insert(0, allItems[0]);
		}

		/// <summary>
		/// In a unidirectional view the FIRST item is hidden.
		/// </summary>
		protected override List<ICmObject> GetVisibleItemList()
		{
			var result = base.GetVisibleItemList();
			result.RemoveAt(0);
			return result;
		}

		#region Component Designer generated code
		/// <summary>
		/// The Superclass handles everything except our Name property.
		/// </summary>
		private void InitializeComponent()
		{
			this.Name = "LexReferenceUnidirectionalView";
		}
		#endregion
	}

	/// <summary>
	///  View constructor for creating the view details.
	/// </summary>
	public class LexReferenceUnidirectionalVc : VectorReferenceVc
	{
		/// <summary>
		/// Constructor for the Vector Reference View Constructor Class.
		/// </summary>
		public LexReferenceUnidirectionalVc(LcmCache cache, int flid, string displayNameProperty, string displayWs)
			: base(cache, flid, displayNameProperty, displayWs)
		{
		}

		/// <summary>
		/// Calling vwenv.AddObjVec() in Display() and implementing DisplayVec() seems to
		/// work better than calling vwenv.AddObjVecItems() in Display().  Theoretically
		/// this should not be case, but experience trumps theory every time.  :-) :-(
		/// </summary>
		public override void DisplayVec(IVwEnv vwenv, int hvo, int tag, int frag)
		{
			ISilDataAccess da = vwenv.DataAccess;
			int count = da.get_VecSize(hvo, tag);
			// Unidirectional consist of everything FOLLOWING the first element which is the owning root.
			for (int i = 1; i < count; ++i)
			{
				vwenv.AddObj(da.get_VecItem(hvo, tag, i), this,
					VectorReferenceView.kfragTargetObj);
				vwenv.AddSeparatorBar();
			}
		}
	}
}
