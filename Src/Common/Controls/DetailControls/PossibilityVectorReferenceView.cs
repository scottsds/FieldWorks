// Copyright (c) 2003-2013 SIL International
// This software is licensed under the LGPL, version 2.1 or later
// (http://www.gnu.org/licenses/lgpl-2.1.html)

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Windows.Forms;
using SIL.FieldWorks.Common.Controls;
using SIL.FieldWorks.Common.Framework.DetailControls.Resources;
using SIL.LCModel;
using SIL.LCModel.Infrastructure;
using SIL.LCModel.Application;
using SIL.FieldWorks.Common.ViewsInterfaces;
using SIL.LCModel.Core.Cellar;
using SIL.LCModel.Core.Text;
using SIL.LCModel.Core.KernelInterfaces;

namespace SIL.FieldWorks.Common.Framework.DetailControls
{
	/// <summary>
	/// Main class for displaying the VectorReferenceSlice.
	/// </summary>
	public class PossibilityVectorReferenceView : VectorReferenceView
	{
		#region Constants and data members

		public const int khvoFake = -2333;
		public const int kflidFake = -2444;

		private int m_prevSelectedHvo;

		private SdaDecorator m_sda;

		#endregion // Constants and data members

		#region Construction, initialization, and disposal

		/// <summary>
		/// Reload the vector in the root box, presumably after it's been modified by a chooser.
		/// </summary>
		public override void ReloadVector()
		{
			CheckDisposed();
			int ws = 0;
			if (m_rootObj != null && m_rootObj.IsValidObject)
			{
				ILgWritingSystemFactory wsf = m_cache.WritingSystemFactory;
				int count = m_sda.get_VecSize(m_rootObj.Hvo, m_rootFlid);
				// This loop is mostly redundant now that the decorator will generate labels itself as needed.
				// It still serves the purpose of figuring out the WS that should be used for the 'fake' item where the user
				// is typing to select.
				for (int i = 0; i < count; ++i)
				{
					int hvo = m_sda.get_VecItem(m_rootObj.Hvo, m_rootFlid, i);
					Debug.Assert(hvo != 0);
					ws = m_sda.GetLabelFor(hvo).get_WritingSystem(0);
				}

				if (ws == 0)
				{
					var list = (ICmPossibilityList) m_rootObj.ReferenceTargetOwner(m_rootFlid);
					ws = list.IsVernacular ? m_cache.ServiceLocator.WritingSystems.DefaultVernacularWritingSystem.Handle
						 : m_cache.ServiceLocator.WritingSystems.DefaultAnalysisWritingSystem.Handle;
					if (list.PossibilitiesOS.Count > 0)
					{
						ObjectLabel label = ObjectLabel.CreateObjectLabel(m_cache, list.PossibilitiesOS[0], m_displayNameProperty, m_displayWs);
						ws = label.AsTss.get_WritingSystem(0);
					}
				}
			}

			if (ws == 0)
				ws = m_cache.ServiceLocator.WritingSystems.DefaultAnalysisWritingSystem.Handle;
			m_sda.Strings[khvoFake] = TsStringUtils.EmptyString(ws);
			base.ReloadVector();
		}

		#endregion // Construction, initialization, and disposal

		#region RootSite required methods

		protected override VectorReferenceVc CreateVectorReferenceVc()
		{
			return new PossibilityVectorReferenceVc(m_cache, m_rootFlid, m_displayNameProperty, m_displayWs);
		}

		protected override ISilDataAccess GetDataAccess()
		{
			if (m_sda == null)
			{
				m_sda = new SdaDecorator((ISilDataAccessManaged) m_cache.DomainDataByFlid, m_cache, m_displayNameProperty, m_displayWs);
				m_sda.Empty = TsStringUtils.EmptyString(m_cache.DefaultAnalWs);
			}
			return m_sda;
		}

		#endregion // RootSite required methods

		protected override void OnEnter(EventArgs e)
		{
			base.OnEnter(e);
			m_prevSelectedHvo = 0;
		}

		protected override void OnLeave(EventArgs e)
		{
			base.OnLeave(e);
			DeleteItem();
		}

		protected override void HandleSelectionChange(IVwRootBox rootb, IVwSelection vwselNew)
		{
			ICmObject selected = SelectedObject;
			int selectedHvo = 0;
			if (selected != null)
				selectedHvo = selected.Hvo;

			if (selectedHvo != m_prevSelectedHvo)
			{
				if (DeleteItem())
				{
					m_prevSelectedHvo = selectedHvo;
					SelectedObject = selected;
				}
				else
				{
					m_prevSelectedHvo = selectedHvo;
					vwselNew.ExtendToStringBoundaries();
				}
			}
		}

		protected override bool HandleRightClickOnObject(int hvo)
		{
			if(hvo != khvoFake)
			{
				return base.HandleRightClickOnObject(hvo);
			}
			else
			{
				return false;
			}
		}

		private bool DeleteItem()
		{
			if (m_prevSelectedHvo == 0)
				return false;

			ITsString tss = m_sda.get_StringProp(m_prevSelectedHvo, kflidFake);
			if (tss == null || tss.Length > 0)
				return false;

			if (m_rootObj.Hvo < 0)
				return false;    // already deleted.  See LT-15042.

			int[] hvosOld = m_sda.VecProp(m_rootObj.Hvo, m_rootFlid);
			for (int i = 0; i < hvosOld.Length; ++i)
			{
				if (hvosOld[i] == m_prevSelectedHvo)
				{
					RemoveObjectFromList(hvosOld, i, string.Format(DetailControlsStrings.ksUndoDeleteItem, m_rootFieldName),
						string.Format(DetailControlsStrings.ksRedoDeleteItem, m_rootFieldName));
					break;
				}
			}
			return true;
		}

		protected override void HandleKeyDown(KeyEventArgs e)
		{
		}

		protected override void HandleKeyPress(KeyPressEventArgs e)
		{
		}

		/// <summary>
		/// This class maintains a cache allowing possibility item display names to be looked up rather than computed after the
		/// first time they are used.
		/// </summary>
		internal class SdaDecorator : DomainDataByFlidDecoratorBase
		{
			private LcmCache Cache { get; set; }
			private string DisplayNameProperty { get; set; }
			private string DisplayWs { get; set; }
			private readonly Dictionary<int, ITsString> m_strings;
			/// <summary>
			/// The empty string displayed (hopefully temporarily) for any object we don't have a fake string for.
			/// </summary>
			public ITsString Empty;

			public SdaDecorator(ISilDataAccessManaged domainDataByFlid, LcmCache cache, string displayNameProperty, string displayWs)
				: base(domainDataByFlid)
			{
				SetOverrideMdc(new MdcDecorator((IFwMetaDataCacheManaged) domainDataByFlid.MetaDataCache));
				m_strings = new Dictionary<int, ITsString>();
				Cache = cache;
				DisplayNameProperty = displayNameProperty;
				DisplayWs = displayWs;
			}

			public IDictionary<int, ITsString> Strings
			{
				get { return m_strings; }
			}

			public ITsString GetLabelFor(int hvo)
			{
				ITsString value;
				if (m_strings.TryGetValue(hvo, out value))
					return value;
				Debug.Assert(Cache != null);
				var obj = Cache.ServiceLocator.GetInstance<ICmObjectRepository>().GetObject(hvo);
				Debug.Assert(obj != null);

				ObjectLabel label = ObjectLabel.CreateObjectLabel(Cache, obj, DisplayNameProperty, DisplayWs);
				ITsString tss = label.AsTss;
				if (tss == null)
					tss = TsStringUtils.EmptyString(Cache.DefaultUserWs);
				Strings[hvo] = tss;
				return tss; // never return null!
			}

			public override ITsString get_StringProp(int hvo, int tag)
			{
				if (tag == kflidFake)
					return GetLabelFor(hvo);
				return base.get_StringProp(hvo, tag);
			}

			public override void SetString(int hvo, int tag, ITsString tss)
			{
				if (tag == kflidFake)
				{
					m_strings[hvo] = tss;
					SendPropChanged(hvo, tag, 0, 0, 0);
				}
				else
				{
					base.SetString(hvo, tag, tss);
				}
			}
		}

		private class MdcDecorator : LcmMetaDataCacheDecoratorBase
		{
			public MdcDecorator(IFwMetaDataCacheManaged mdc)
				: base(mdc)
			{
			}

			public override void AddVirtualProp(string bstrClass, string bstrField, int luFlid, int type)
			{
				throw new NotImplementedException();
			}

			/// <summary>
			/// The virtual field store a TsString, so the fake flid returns a type of String.
			/// </summary>
			public override int GetFieldType(int luFlid)
			{
				return luFlid == kflidFake ?
					(int) CellarPropertyType.String : base.GetFieldType(luFlid);
			}
		}
	}

	#region VectorReferenceVc class

	/// <summary>
	///  View constructor for creating the view details.
	/// </summary>
	public class PossibilityVectorReferenceVc : VectorReferenceVc
	{
		public PossibilityVectorReferenceVc(LcmCache cache, int flid, string displayNameProperty, string displayWs)
			: base(cache, flid, displayNameProperty, displayWs)
		{
		}

		/// <summary>
		/// This is the basic method needed for the view constructor.
		/// </summary>
		public override void Display(IVwEnv vwenv, int hvo, int frag)
		{
			switch (frag)
			{
				case VectorReferenceView.kfragTargetVector:
					if (!string.IsNullOrEmpty(TextStyle))
					{
						vwenv.set_StringProperty((int)FwTextPropType.ktptNamedStyle, TextStyle);
					}
					vwenv.OpenParagraph();
					vwenv.AddObjVec(m_flid, this, frag);
					vwenv.CloseParagraph();
					break;
				case VectorReferenceView.kfragTargetObj:
					// Display one object by displaying the fake string property of that object which our special
					// private decorator stores for it.
					vwenv.AddStringProp(PossibilityVectorReferenceView.kflidFake, this);
					break;
				default:
					throw new ArgumentException(
						"Don't know what to do with the given frag.", "frag");
			}
		}

		/// <summary>
		/// Calling vwenv.AddObjVec() in Display() and implementing DisplayVec() seems to
		/// work better than calling vwenv.AddObjVecItems() in Display().  Theoretically
		/// this should not be case, but experience trumps theory every time.  :-) :-(
		/// </summary>
		public override void DisplayVec(IVwEnv vwenv, int hvo, int tag, int frag)
		{
			if (hvo == 0)
				return;

			ISilDataAccess da = vwenv.DataAccess;
			int count = da.get_VecSize(hvo, tag);
			for (int i = 0; i < count; ++i)
			{
				vwenv.AddObj(da.get_VecItem(hvo, tag, i), this, VectorReferenceView.kfragTargetObj);
				vwenv.AddSeparatorBar();
			}
			vwenv.AddObj(PossibilityVectorReferenceView.khvoFake, this, VectorReferenceView.kfragTargetObj);
		}
	}

	#endregion // VectorReferenceVc class
}
