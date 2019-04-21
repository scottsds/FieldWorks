﻿// Copyright (c) 2015 SIL International
// This software is licensed under the LGPL, version 2.1 or later
// (http://www.gnu.org/licenses/lgpl-2.1.html)

using SIL.FieldWorks.Common.FwUtils;
using SIL.LCModel;

namespace SIL.FieldWorks.XWorks.MorphologyEditor
{
	public class AffixRuleFormulaSlice : RuleFormulaSlice
	{
		public AffixRuleFormulaSlice()
		{
		}

		AffixRuleFormulaControl AffixRuleFormulaControl
		{
			get
			{
				return Control as AffixRuleFormulaControl;
			}
		}

		public override void FinishInit()
		{
			CheckDisposed();
			Control = new AffixRuleFormulaControl(m_configurationNode);
		}

		public bool OnDisplayMappingSetFeatures(object commandObject, ref XCore.UIItemDisplayProperties display)
		{
			CheckDisposed();
			bool enable = AffixRuleFormulaControl.IsIndexCurrent;
			display.Enabled = enable;
			display.Visible = enable;
			return true;
		}

		public bool OnMappingSetFeatures(object args)
		{
			CheckDisposed();
			AffixRuleFormulaControl.SetMappingFeatures();
			return true;
		}

		public bool OnDisplayMappingSetNaturalClass(object commandObject, ref XCore.UIItemDisplayProperties display)
		{
			CheckDisposed();
			bool enable = AffixRuleFormulaControl.IsIndexCurrent;
			display.Enabled = enable;
			display.Visible = enable;
			return true;
		}

		public bool OnMappingSetNaturalClass(object args)
		{
			CheckDisposed();
			AffixRuleFormulaControl.SetMappingNaturalClass();
			return true;
		}

		public bool OnDisplayMappingJumpToNaturalClass(object commandObject, ref XCore.UIItemDisplayProperties display)
		{
			CheckDisposed();
			bool enable = AffixRuleFormulaControl.IsNCIndexCurrent;
			display.Enabled = enable;
			display.Visible = enable;
			return true;
		}

		public bool OnMappingJumpToNaturalClass(object args)
		{
			CheckDisposed();
			IMoModifyFromInput mapping = RuleFormulaControl.CurrentObject as IMoModifyFromInput;
			Mediator.PostMessage("FollowLink", new FwLinkArgs("naturalClassedit", mapping.ModificationRA.Guid));
			return true;
		}

		public virtual bool OnDisplayMappingJumpToPhoneme(object commandObject, ref XCore.UIItemDisplayProperties display)
		{
			CheckDisposed();
			bool enable = AffixRuleFormulaControl.IsPhonemeCurrent;
			display.Enabled = enable;
			display.Visible = enable;
			return true;
		}

		public virtual bool OnMappingJumpToPhoneme(object args)
		{
			CheckDisposed();
			IMoInsertPhones mapping = RuleFormulaControl.CurrentObject as IMoInsertPhones;
			Mediator.PostMessage("FollowLink", new FwLinkArgs("phonemeEdit", mapping.ContentRS[0].Guid));
			return true;
		}
	}
}
