// Copyright (c) 2003-2013 SIL International
// This software is licensed under the LGPL, version 2.1 or later
// (http://www.gnu.org/licenses/lgpl-2.1.html)
//
// File: AtomicReferenceView.cs
// Responsibility: Steve McConnel
// Last reviewed:
//
// <remarks>
// borrowed and hacked from PhoneEnvReferenceView
// </remarks>

using System;
using SIL.LCModel.Core.Cellar;
using SIL.LCModel.Core.Text;
using SIL.FieldWorks.Common.Controls;
using SIL.LCModel.Core.KernelInterfaces;
using SIL.LCModel;
using SIL.LCModel.Application;
using SIL.LCModel.Infrastructure;
using SIL.FieldWorks.Common.ViewsInterfaces;

namespace SIL.FieldWorks.Common.Framework.DetailControls
{
	/// <summary>
	/// Main class for displaying the AtomicReferenceSlice.
	/// </summary>
	public class PossibilityAtomicReferenceView : AtomicReferenceView
	{
		#region Constants and data members

		public const int kflidFake = -2222;

		private SdaDecorator m_sda;

		#endregion // Constants and data members

		#region Construction, initialization, and disposal

		protected override void SetRootBoxObj()
		{
			if (m_rootObj != null && m_rootObj.IsValidObject)
			{
				int hvo = m_cache.DomainDataByFlid.get_ObjectProp(m_rootObj.Hvo, m_rootFlid);
				if (hvo != 0)
				{
					var obj = m_cache.ServiceLocator.GetInstance<ICmObjectRepository>().GetObject(hvo);
					ObjectLabel label = ObjectLabel.CreateObjectLabel(m_cache, obj, m_displayNameProperty, m_displayWs);
					m_sda.Tss = label.AsTss;
				}
				else
				{
					var list = (ICmPossibilityList) m_rootObj.ReferenceTargetOwner(m_rootFlid);
					int ws = list.IsVernacular ? m_cache.ServiceLocator.WritingSystems.DefaultVernacularWritingSystem.Handle
						: m_cache.ServiceLocator.WritingSystems.DefaultAnalysisWritingSystem.Handle;
					if (list.PossibilitiesOS.Count > 0)
					{
						ObjectLabel label = ObjectLabel.CreateObjectLabel(m_cache, list.PossibilitiesOS[0], m_displayNameProperty, m_displayWs);
						ws = label.AsTss.get_WritingSystem(0);
					}
					m_sda.Tss = TsStringUtils.EmptyString(ws);
				}
			}

			base.SetRootBoxObj();
		}

		#endregion // Construction, initialization, and disposal

		#region RootSite required methods

		protected override ISilDataAccess GetDataAccess()
		{
			m_sda = new SdaDecorator((ISilDataAccessManaged) m_cache.DomainDataByFlid);
			return m_sda;
		}

		public override void SetReferenceVc()
		{
			CheckDisposed();
			m_atomicReferenceVc = new PossibilityAtomicReferenceVc(m_cache, m_rootFlid, m_displayNameProperty);
		}

		#endregion // RootSite required methods

		#region other overrides

		protected override void EnsureDefaultSelection()
		{
			if (RootBox != null)
				RootBox.MakeSimpleSel(true, true, true, true);
		}

		protected override void HandleSelectionChange(IVwRootBox rootb, IVwSelection vwselNew)
		{
			// do nothing
		}

		#endregion

		private class SdaDecorator : DomainDataByFlidDecoratorBase
		{
			public SdaDecorator(ISilDataAccessManaged domainDataByFlid)
				: base(domainDataByFlid)
			{
				SetOverrideMdc(new MdcDecorator((IFwMetaDataCacheManaged) domainDataByFlid.MetaDataCache));
			}

			public ITsString Tss { get; set; }

			public override ITsString get_StringProp(int hvo, int tag)
			{
				if (tag == kflidFake)
					return Tss;
				return base.get_StringProp(hvo, tag);
			}

			public override void SetString(int hvo, int tag, ITsString tss)
			{
				if (tag == kflidFake)
				{
					Tss = tss;
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

	#region AtomicReferenceVc class

	/// <summary>
	///  View constructor for creating the view details.
	/// </summary>
	public class PossibilityAtomicReferenceVc : AtomicReferenceVc
	{
		private string m_textStyle;

		public PossibilityAtomicReferenceVc(LcmCache cache, int flid, string displayNameProperty)
			: base(cache, flid, displayNameProperty)
		{
		}

		public override void Display(IVwEnv vwenv, int hvo, int frag)
		{
			switch (frag)
			{
				case AtomicReferenceView.kFragAtomicRef:
					// Display a paragraph with a single item.
					vwenv.OpenParagraph();		// vwenv.OpenMappedPara();
					if (!string.IsNullOrEmpty(TextStyle))
						vwenv.set_StringProperty((int)FwTextPropType.ktptNamedStyle, TextStyle);
					vwenv.AddStringProp(PossibilityAtomicReferenceView.kflidFake, this);
					vwenv.CloseParagraph();
					break;
				default:
					throw new ArgumentException(
						"Don't know what to do with the given frag.", "frag");
			}
		}
	}

	#endregion // AtomicReferenceVc class
}
