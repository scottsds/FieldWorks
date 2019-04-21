﻿// Copyright (c) 2015 SIL International
// This software is licensed under the LGPL, version 2.1 or later
// (http://www.gnu.org/licenses/lgpl-2.1.html)

using System;
using System.Collections.Generic;
using SIL.LCModel.Core.Text;
using SIL.LCModel.Core.KernelInterfaces;
using SIL.FieldWorks.Common.ViewsInterfaces;
using SIL.LCModel;
using SIL.LCModel.DomainServices;
using SIL.FieldWorks.LexText.Controls;
using XCore;

namespace SIL.FieldWorks.XWorks.MorphologyEditor
{
	/// <summary>
	/// This view constructor is intended to be extended by particular rule formula view
	/// constructors. It handles the display of phonological contexts, such as <c>PhSequenceContext</c>,
	/// <c>PhIterationContext</c>, <c>PhSimpleContextNC</c>, <c>PhSimpleContextSeg</c>, <c>PhSimpleContextNC</c>, etc., for
	/// rule formulas.
	/// </summary>
	public abstract class RuleFormulaVcBase : PatternVcBase
	{
		public const int kfragContext = 100;
		public const int kfragFeatNC = 101;
		public const int kfragFeats = 102;
		public const int kfragFeature = 103;
		public const int kfragPlusVariable = 104;
		public const int kfragMinusVariable = 105;
		public const int kfragFeatureLine = 106;
		public const int kfragPlusVariableLine = 107;
		public const int kfragMinusVariableLine = 108;
		public const int kfragIterCtxtMax = 109;
		public const int kfragIterCtxtMin = 110;
		public const int kfragNC = 111;
		public const int kfragTerminalUnit = 112;

		// variant frags
		public const int kfragXVariable = 113;

		// fake flids
		public const int ktagFeature = -200;
		public const int ktagVariable = -201;
		public const int ktagXVariable = -202;

		private static readonly string[] VariableNames = { "α", "β", "γ", "δ", "ε", "ζ", "η", "θ", "ι", "κ", "λ", "μ", "ν", "ξ",
														   "ο", "π", "ρ", "σ", "τ", "υ", "φ", "χ", "ψ", "ω" };

		protected ITsString m_infinity;
		protected ITsString m_x;

		protected RuleFormulaVcBase(LcmCache cache, PropertyTable propertyTable)
			: base(cache, propertyTable)
		{
			int userWs = m_cache.DefaultUserWs;
			m_propertyTable = propertyTable;
			m_infinity = TsStringUtils.MakeString("\u221e", userWs);
			m_x = TsStringUtils.MakeString("X", userWs);
		}

		/// <summary>
		/// Gets the maximum number of lines for context cells.
		/// </summary>
		/// <value>The max number of lines.</value>
		protected abstract int GetMaxNumLines();

		/// <summary>
		/// Gets the index of the specified feature constraint. This is used to ensure that the same
		/// variable is used for a feature constraint across the entire rule.
		/// </summary>
		/// <param name="var">The variable.</param>
		/// <returns></returns>
		protected abstract int GetVarIndex(IPhFeatureConstraint var);

		public override void Display(IVwEnv vwenv, int hvo, int frag)
		{
			switch (frag)
			{
				case kfragContext:
					var ctxtOrVar = m_cache.ServiceLocator.GetInstance<IPhContextOrVarRepository>().GetObject(hvo);
					bool isOuterIterCtxt = false;
					// are we inside an iteration context? this is important since we only open a context pile if we are not
					// in an iteration context, since an iteration context does it for us
					if (vwenv.EmbeddingLevel > 0)
					{
						int outerHvo, outerTag, outerIndex;
						vwenv.GetOuterObject(vwenv.EmbeddingLevel - 1, out outerHvo, out outerTag, out outerIndex);
						var outerObj = m_cache.ServiceLocator.GetObject(outerHvo);
						isOuterIterCtxt = outerObj.ClassID == PhIterationContextTags.kClassId;
					}

					switch (ctxtOrVar.ClassID)
					{
						case PhSequenceContextTags.kClassId:
							var seqCtxt = (IPhSequenceContext) ctxtOrVar;
							if (seqCtxt.MembersRS.Count > 0)
							{
								vwenv.AddObjVecItems(PhSequenceContextTags.kflidMembers, this, kfragContext);
							}
							else
							{
								OpenSingleLinePile(vwenv, GetMaxNumLines(), false);
								vwenv.Props = m_bracketProps;
								vwenv.AddProp(PhSequenceContextTags.kflidMembers, this, kfragEmpty);
								CloseSingleLinePile(vwenv, false);
							}
							break;

						case PhSimpleContextNCTags.kClassId:
							var ncCtxt = (IPhSimpleContextNC) ctxtOrVar;
							if (ncCtxt.FeatureStructureRA != null && ncCtxt.FeatureStructureRA.ClassID == PhNCFeaturesTags.kClassId)
							{
								// Natural class simple context with a feature-based natural class
								var natClass = (IPhNCFeatures) ncCtxt.FeatureStructureRA;

								int numLines = GetNumLines(ncCtxt);
								if (numLines == 0)
								{
									if (!isOuterIterCtxt)
										OpenSingleLinePile(vwenv, GetMaxNumLines());

									vwenv.AddProp(ktagInnerNonBoundary, this, kfragLeftBracket);
									vwenv.AddProp(PhSimpleContextNCTags.kflidFeatureStructure, this, kfragQuestions);
									vwenv.AddProp(ktagInnerNonBoundary, this, kfragRightBracket);

									if (!isOuterIterCtxt)
										CloseSingleLinePile(vwenv);
								}
								else if (numLines == 1)
								{
									if (!isOuterIterCtxt)
										OpenSingleLinePile(vwenv, GetMaxNumLines());

									// use normal brackets for a single line context
									vwenv.AddProp(ktagInnerNonBoundary, this, kfragLeftBracket);

									// special consonant and vowel natural classes only display the abbreviation
									if (natClass.Abbreviation.AnalysisDefaultWritingSystem.Text == "C"
										|| natClass.Abbreviation.AnalysisDefaultWritingSystem.Text == "V")
									{
										vwenv.AddObjProp(PhSimpleContextNCTags.kflidFeatureStructure, this, kfragNC);
									}
									else
									{
										if (natClass.FeaturesOA != null && natClass.FeaturesOA.FeatureSpecsOC.Count > 0)
											vwenv.AddObjProp(PhSimpleContextNCTags.kflidFeatureStructure, this, kfragFeatNC);
										else if (ncCtxt.PlusConstrRS.Count > 0)
											vwenv.AddObjVecItems(PhSimpleContextNCTags.kflidPlusConstr, this, kfragPlusVariable);
										else
											vwenv.AddObjVecItems(PhSimpleContextNCTags.kflidMinusConstr, this, kfragMinusVariable);
									}
									vwenv.AddProp(ktagInnerNonBoundary, this, kfragRightBracket);

									if (!isOuterIterCtxt)
										CloseSingleLinePile(vwenv);
								}
								else
								{
									// multiline context

									// left bracket pile
									int maxNumLines = GetMaxNumLines();
									vwenv.Props = m_bracketProps;
									vwenv.set_IntProperty((int)FwTextPropType.ktptMarginLeading, (int)FwTextPropVar.ktpvMilliPoint, PileMargin);
									vwenv.OpenInnerPile();
									AddExtraLines(maxNumLines - numLines, ktagLeftNonBoundary, vwenv);
									vwenv.AddProp(ktagLeftNonBoundary, this, kfragLeftBracketUpHook);
									for (int i = 1; i < numLines - 1; i++)
										vwenv.AddProp(ktagLeftNonBoundary, this, kfragLeftBracketExt);
									vwenv.AddProp(ktagLeftBoundary, this, kfragLeftBracketLowHook);
									vwenv.CloseInnerPile();

									// feature and variable pile
									vwenv.set_IntProperty((int)FwTextPropType.ktptAlign, (int)FwTextPropVar.ktpvEnum, (int)FwTextAlign.ktalLeft);
									vwenv.OpenInnerPile();
									AddExtraLines(maxNumLines - numLines, vwenv);
									vwenv.AddObjProp(PhSimpleContextNCTags.kflidFeatureStructure, this, kfragFeatNC);
									vwenv.AddObjVecItems(PhSimpleContextNCTags.kflidPlusConstr, this, kfragPlusVariable);
									vwenv.AddObjVecItems(PhSimpleContextNCTags.kflidMinusConstr, this, kfragMinusVariable);
									vwenv.CloseInnerPile();

									// right bracket pile
									vwenv.Props = m_bracketProps;
									if (!isOuterIterCtxt)
										vwenv.set_IntProperty((int)FwTextPropType.ktptMarginTrailing, (int)FwTextPropVar.ktpvMilliPoint, PileMargin);
									vwenv.OpenInnerPile();
									AddExtraLines(maxNumLines - numLines, ktagRightNonBoundary, vwenv);
									vwenv.AddProp(ktagRightNonBoundary, this, kfragRightBracketUpHook);
									for (int i = 1; i < numLines - 1; i++)
										vwenv.AddProp(ktagRightNonBoundary, this, kfragRightBracketExt);
									vwenv.AddProp(ktagRightBoundary, this, kfragRightBracketLowHook);
									vwenv.CloseInnerPile();
								}
							}
							else
							{
								// natural class context with segment-based natural class
								if (!isOuterIterCtxt)
									OpenSingleLinePile(vwenv, GetMaxNumLines());

								vwenv.AddProp(ktagInnerNonBoundary, this, kfragLeftBracket);
								if (ncCtxt.FeatureStructureRA != null)
									vwenv.AddObjProp(PhSimpleContextNCTags.kflidFeatureStructure, this, kfragNC);
								else
									vwenv.AddProp(PhSimpleContextNCTags.kflidFeatureStructure, this, kfragQuestions);
								vwenv.AddProp(ktagInnerNonBoundary, this, kfragRightBracket);

								if (!isOuterIterCtxt)
									CloseSingleLinePile(vwenv);
							}
							break;

						case PhIterationContextTags.kClassId:
							var iterCtxt = (IPhIterationContext) ctxtOrVar;
							if (iterCtxt.MemberRA != null)
							{
								int numLines = GetNumLines(iterCtxt.MemberRA as IPhSimpleContext);
								if (numLines > 1)
								{
									vwenv.AddObjProp(PhIterationContextTags.kflidMember, this, kfragContext);
									DisplayIterCtxt(numLines, vwenv);
								}
								else
								{
									OpenSingleLinePile(vwenv, GetMaxNumLines());
									if (iterCtxt.MemberRA.ClassID == PhSimpleContextNCTags.kClassId)
									{
										vwenv.AddObjProp(PhIterationContextTags.kflidMember, this, kfragContext);
									}
									else
									{
										vwenv.AddProp(ktagInnerNonBoundary, this, kfragLeftParen);
										vwenv.AddObjProp(PhIterationContextTags.kflidMember, this, kfragContext);
										vwenv.AddProp(ktagInnerNonBoundary, this, kfragRightParen);
									}
									DisplayIterCtxt(1, vwenv);
									// Views doesn't handle selection properly when we have an inner pile with strings on either side,
									// so we don't add a zero-width space at the end
									CloseSingleLinePile(vwenv, false);
								}
							}
							else
							{
								OpenSingleLinePile(vwenv, GetMaxNumLines());
								vwenv.AddProp(PhIterationContextTags.kflidMember, this, kfragQuestions);
								CloseSingleLinePile(vwenv);
							}
							break;

						case PhSimpleContextSegTags.kClassId:
							if (!isOuterIterCtxt)
								OpenSingleLinePile(vwenv, GetMaxNumLines());

							var segCtxt = (IPhSimpleContextSeg) ctxtOrVar;
							if (segCtxt.FeatureStructureRA != null)
								vwenv.AddObjProp(PhSimpleContextSegTags.kflidFeatureStructure, this, kfragTerminalUnit);
							else
								vwenv.AddProp(PhSimpleContextSegTags.kflidFeatureStructure, this, kfragQuestions);

							if (!isOuterIterCtxt)
								CloseSingleLinePile(vwenv);
							break;

						case PhSimpleContextBdryTags.kClassId:
							if (!isOuterIterCtxt)
								OpenSingleLinePile(vwenv, GetMaxNumLines());

							var bdryCtxt = (IPhSimpleContextBdry) ctxtOrVar;
							if (bdryCtxt.FeatureStructureRA != null)
								vwenv.AddObjProp(PhSimpleContextBdryTags.kflidFeatureStructure, this, kfragTerminalUnit);
							else
								vwenv.AddProp(PhSimpleContextBdryTags.kflidFeatureStructure, this, kfragQuestions);

							if (!isOuterIterCtxt)
								CloseSingleLinePile(vwenv);
							break;

						case PhVariableTags.kClassId:
							OpenSingleLinePile(vwenv, GetMaxNumLines());
							vwenv.AddProp(ktagXVariable, this, kfragXVariable);
							CloseSingleLinePile(vwenv);
							break;
					}
					break;

				case kfragNC:
					int ncWs = WritingSystemServices.ActualWs(m_cache, WritingSystemServices.kwsFirstAnal, hvo,
						PhNaturalClassTags.kflidAbbreviation);
					if (ncWs != 0)
					{
						vwenv.AddStringAltMember(PhNaturalClassTags.kflidAbbreviation, ncWs, this);
					}
					else
					{
						ncWs = WritingSystemServices.ActualWs(m_cache, WritingSystemServices.kwsFirstAnal, hvo,
							PhNaturalClassTags.kflidName);
						if (ncWs != 0)
							vwenv.AddStringAltMember(PhNaturalClassTags.kflidName, ncWs, this);
						else
							vwenv.AddProp(PhNaturalClassTags.kflidAbbreviation, this, kfragQuestions);
					}
					break;

				case kfragTerminalUnit:
					int tuWs = WritingSystemServices.ActualWs(m_cache, WritingSystemServices.kwsFirstVern,
						hvo, PhTerminalUnitTags.kflidName);
					if (tuWs != 0)
						vwenv.AddStringAltMember(PhTerminalUnitTags.kflidName, tuWs, this);
					else
						vwenv.AddProp(PhTerminalUnitTags.kflidName, this, kfragQuestions);
					break;

				case kfragFeatNC:
					vwenv.AddObjProp(PhNCFeaturesTags.kflidFeatures, this, kfragFeats);
					break;

				case kfragFeats:
					vwenv.AddObjVecItems(FsFeatStrucTags.kflidFeatureSpecs, this, kfragFeature);
					break;

				case kfragFeature:
					vwenv.AddProp(ktagFeature, this, kfragFeatureLine);
					break;

				case kfragPlusVariable:
					vwenv.AddProp(ktagVariable, this, kfragPlusVariableLine);
					break;

				case kfragMinusVariable:
					vwenv.AddProp(ktagVariable, this, kfragMinusVariableLine);
					break;
			}
		}

		public override ITsString DisplayVariant(IVwEnv vwenv, int tag, int frag)
		{
			// we use display variant to display literal strings that are editable
			ITsString tss;
			switch (frag)
			{
				case kfragFeatureLine:
					var value = m_cache.ServiceLocator.GetInstance<IFsClosedValueRepository>().GetObject(vwenv.CurrentObject());
					tss = CreateFeatureLine(value);
					break;

				case kfragPlusVariableLine:
				case kfragMinusVariableLine:
					var var = m_cache.ServiceLocator.GetInstance<IPhFeatureConstraintRepository>().GetObject(vwenv.CurrentObject());
					tss = CreateVariableLine(var, frag == kfragPlusVariableLine);
					break;

				case kfragIterCtxtMax:
					// if the max value is -1, it indicates that it is infinite
					int i = m_cache.DomainDataByFlid.get_IntProp(vwenv.CurrentObject(), tag);
					tss = i == -1 ? m_infinity : TsStringUtils.MakeString(Convert.ToString(i), m_cache.DefaultUserWs);
					break;

				case kfragXVariable:
					tss = m_x;
					break;

				default:
					tss = base.DisplayVariant(vwenv, tag, frag);
					break;
			}
			return tss;
		}

		public override ITsString UpdateProp(IVwSelection vwsel, int hvo, int tag, int frag, ITsString tssVal)
		{
			return tssVal;
		}

		ITsString CreateFeatureLine(IFsClosedValue value)
		{
			ITsIncStrBldr featLine = TsStringUtils.MakeIncStrBldr();
			featLine.AppendTsString(value.ValueRA != null ? value.ValueRA.Abbreviation.BestAnalysisAlternative : m_questions);
			featLine.Append(" ");
			featLine.AppendTsString(value.FeatureRA != null ? value.FeatureRA.Abbreviation.BestAnalysisAlternative : m_questions);
			return featLine.GetString();
		}

		ITsString CreateVariableLine(IPhFeatureConstraint var, bool polarity)
		{
			int varIndex = GetVarIndex(var);
			if (varIndex == -1)
				return m_questions;

			ITsIncStrBldr varLine = TsStringUtils.MakeIncStrBldr();
			if (!polarity)
				varLine.AppendTsString(TsStringUtils.MakeString("-", m_cache.DefaultUserWs));
			varLine.AppendTsString(TsStringUtils.MakeString(VariableNames[varIndex], m_cache.DefaultUserWs));
			varLine.Append(" ");
			varLine.AppendTsString(var.FeatureRA == null ? m_questions : var.FeatureRA.Abbreviation.BestAnalysisAlternative);
			return varLine.GetString();
		}

		void DisplayIterCtxt(int numLines, IVwEnv vwenv)
		{
			int superOffset = 0;
			if (numLines == 1)
			{
				// if the inner context is a single line, then make the min value a subscript and the max value a superscript.
				// I tried to use the Views subscript and superscript properties, but they added extra space so that it would
				// have the same line height of a normal character, which is not what I wanted, so I compute the size myself
				int fontHeight = GetFontHeight(m_cache.DefaultUserWs);
				int superSubHeight = (fontHeight * 2) / 3;
				vwenv.set_IntProperty((int)FwTextPropType.ktptFontSize, (int)FwTextPropVar.ktpvMilliPoint, superSubHeight);
				vwenv.set_IntProperty((int)FwTextPropType.ktptLineHeight, (int)FwTextPropVar.ktpvMilliPoint, -superSubHeight);
				superOffset = superSubHeight / 2;
			}
			else
			{
				vwenv.set_IntProperty((int)FwTextPropType.ktptMarginTrailing, (int)FwTextPropVar.ktpvMilliPoint, PileMargin);
			}
			vwenv.OpenInnerPile();
			if (numLines == 1)
				vwenv.set_IntProperty((int)FwTextPropType.ktptOffset, (int)FwTextPropVar.ktpvMilliPoint, superOffset);
			vwenv.OpenParagraph();
			vwenv.AddProp(PhIterationContextTags.kflidMaximum, this, kfragIterCtxtMax);
			vwenv.CloseParagraph();
			AddExtraLines(numLines - 2, vwenv);
			vwenv.set_IntProperty((int)FwTextPropType.ktptOffset, (int)FwTextPropVar.ktpvMilliPoint, 0);
			vwenv.OpenParagraph();
			vwenv.AddIntProp(PhIterationContextTags.kflidMinimum);
			vwenv.CloseParagraph();
			vwenv.CloseInnerPile();
		}

		/// <summary>
		/// Gets the maximum number of lines to display the specified sequence of simple contexts.
		/// </summary>
		/// <param name="seq">The sequence.</param>
		/// <returns></returns>
		protected int GetNumLines(IEnumerable<IPhSimpleContext> seq)
		{
			int maxNumLines = 1;
			foreach (IPhSimpleContext ctxt in seq)
			{
				int numLines = GetNumLines(ctxt);
				if (numLines > maxNumLines)
					maxNumLines = numLines;
			}
			return maxNumLines;
		}

		/// <summary>
		/// Gets the number of lines needed to display the specified context or variable.
		/// </summary>
		/// <param name="ctxtOrVar">The context or variable.</param>
		/// <returns></returns>
		protected int GetNumLines(IPhContextOrVar ctxtOrVar)
		{
			if (ctxtOrVar == null)
				return 1;

			switch (ctxtOrVar.ClassID)
			{
				case PhSequenceContextTags.kClassId:
					var seqCtxt = (IPhSequenceContext) ctxtOrVar;
					int maxNumLines = 1;
					foreach (IPhPhonContext cur in seqCtxt.MembersRS)
					{
						int numLines = GetNumLines(cur);
						if (numLines > maxNumLines)
							maxNumLines = numLines;
					}
					return maxNumLines;

				case PhIterationContextTags.kClassId:
					var iterCtxt = (IPhIterationContext) ctxtOrVar;
					return GetNumLines(iterCtxt.MemberRA);

				case PhSimpleContextNCTags.kClassId:
					int numFeats = 0;
					var ncCtxt = (IPhSimpleContextNC) ctxtOrVar;
					if (ncCtxt.FeatureStructureRA != null && ncCtxt.FeatureStructureRA.ClassID == PhNCFeaturesTags.kClassId)
					{
						var natClass = (IPhNCFeatures) ncCtxt.FeatureStructureRA;
						if (natClass.FeaturesOA != null)
							numFeats = natClass.FeaturesOA.FeatureSpecsOC.Count;
					}
					return ncCtxt.PlusConstrRS.Count + ncCtxt.MinusConstrRS.Count + numFeats;
			}
			return 1;
		}

		/// <summary>
		/// Gets the width of the specified context or variable.
		/// </summary>
		/// <param name="ctxtOrVar">The context or variable.</param>
		/// <param name="vwenv">The vwenv.</param>
		/// <returns></returns>
		protected int GetWidth(IPhContextOrVar ctxtOrVar, IVwEnv vwenv)
		{
			if (ctxtOrVar == null)
				return 0;

			switch (ctxtOrVar.ClassID)
			{
				case PhSequenceContextTags.kClassId:
					var seqCtxt = (IPhSequenceContext) ctxtOrVar;
					int totalLen = 0;
					foreach (IPhPhonContext cur in seqCtxt.MembersRS)
						totalLen += GetWidth(cur, vwenv);
					return totalLen;

				case PhIterationContextTags.kClassId:
					return GetIterCtxtWidth(ctxtOrVar as IPhIterationContext, vwenv) + (PileMargin * 2);

				case PhVariableTags.kClassId:
					return GetStrWidth(m_x, null, vwenv) + (PileMargin * 2);

				default:
					return GetSimpleCtxtWidth(ctxtOrVar as IPhSimpleContext, vwenv) + (PileMargin * 2);
			}
		}

		int GetIterCtxtWidth(IPhIterationContext ctxt, IVwEnv vwenv)
		{
			if (ctxt.MemberRA != null)
			{
				int len = GetSimpleCtxtWidth(ctxt.MemberRA as IPhSimpleContext, vwenv);
				int numLines = GetNumLines(ctxt.MemberRA);
				if (numLines > 1)
				{
					len += GetMinMaxWidth(ctxt, null, vwenv);
				}
				else
				{
					if (ctxt.MemberRA.ClassID != PhSimpleContextNCTags.kClassId)
					{
						len += GetStrWidth(m_leftParen, null, vwenv);
						len += GetStrWidth(m_rightParen, null, vwenv);
					}
					int fontHeight = GetFontHeight(m_cache.DefaultUserWs);
					int superSubHeight = (fontHeight * 2) / 3;
					ITsPropsBldr tpb = TsStringUtils.MakePropsBldr();
					tpb.SetIntPropValues((int)FwTextPropType.ktptFontSize, (int)FwTextPropVar.ktpvMilliPoint, superSubHeight);
					len += GetMinMaxWidth(ctxt, tpb.GetTextProps(), vwenv);
				}
				return len;
			}

			return GetStrWidth(m_questions, null, vwenv);
		}

		int GetMinMaxWidth(IPhIterationContext ctxt, ITsTextProps props, IVwEnv vwenv)
		{
			var userWs = m_cache.DefaultUserWs;
			int minWidth = GetStrWidth(TsStringUtils.MakeString(Convert.ToString(ctxt.Minimum), userWs),
				props, vwenv);
			ITsString maxStr = ctxt.Maximum == -1 ? m_infinity : TsStringUtils.MakeString(Convert.ToString(ctxt.Maximum), userWs);
			int maxWidth = GetStrWidth(maxStr, props, vwenv);
			return Math.Max(minWidth, maxWidth);
		}

		int GetSimpleCtxtWidth(IPhSimpleContext ctxt, IVwEnv vwenv)
		{
			if (ctxt == null)
				return 0;

			switch (ctxt.ClassID)
			{
				case PhSimpleContextBdryTags.kClassId:
					var bdryCtxt = (IPhSimpleContextBdry) ctxt;
					return GetTermUnitWidth(bdryCtxt.FeatureStructureRA, vwenv);

				case PhSimpleContextSegTags.kClassId:
					var segCtxt = (IPhSimpleContextSeg) ctxt;
					return GetTermUnitWidth(segCtxt.FeatureStructureRA, vwenv);

				case PhSimpleContextNCTags.kClassId:
					return GetNCCtxtWidth(ctxt as IPhSimpleContextNC, vwenv);
			}
			return 0;
		}

		int GetTermUnitWidth(IPhTerminalUnit tu, IVwEnv vwenv)
		{
			if (tu == null)
				return GetStrWidth(m_questions, null, vwenv);
			else
				return GetStrWidth(tu.Name.BestVernacularAlternative, null, vwenv);
		}

		int GetNCCtxtWidth(IPhSimpleContextNC ctxt, IVwEnv vwenv)
		{
			if (ctxt.FeatureStructureRA != null && ctxt.FeatureStructureRA.ClassID == PhNCFeaturesTags.kClassId)
			{
				int numLines = GetNumLines(ctxt);
				if (numLines == 1)
				{
					int len;
					if (ctxt.FeatureStructureRA.Abbreviation.UserDefaultWritingSystem.Text == "C"
						|| ctxt.FeatureStructureRA.Abbreviation.UserDefaultWritingSystem.Text == "V")
					{
						len = GetStrWidth(ctxt.FeatureStructureRA.Abbreviation.BestAnalysisAlternative, null, vwenv);
					}
					else
					{
						len = GetNCFeatsWidth(ctxt, vwenv);
					}
					len += GetStrWidth(m_leftBracket, null, vwenv);
					len += GetStrWidth(m_rightBracket, null, vwenv);
					return len;
				}
				else
				{
					int len = GetNCFeatsWidth(ctxt, vwenv);
					len += GetStrWidth(m_leftBracketUpHook, m_bracketProps, vwenv);
					len += GetStrWidth(m_rightBracketUpHook, m_bracketProps, vwenv);
					return len;
				}
			}
			else
			{
				int len = GetStrWidth(ctxt.FeatureStructureRA == null ? m_questions : ctxt.FeatureStructureRA.Abbreviation.BestAnalysisAlternative, null, vwenv);
				len += GetStrWidth(m_leftBracket, null, vwenv);
				len += GetStrWidth(m_rightBracket, null, vwenv);
				return len;
			}
		}

		int GetNCFeatsWidth(IPhSimpleContextNC ctxt, IVwEnv vwenv)
		{
			int maxLen = 0;
			IPhNCFeatures natClass = ctxt.FeatureStructureRA as IPhNCFeatures;
			if (natClass != null && natClass.FeaturesOA != null)
			{
				foreach (IFsFeatureSpecification spec in natClass.FeaturesOA.FeatureSpecsOC)
				{
					IFsClosedValue curVal = spec as IFsClosedValue;
					ITsString featLine = CreateFeatureLine(curVal);
					int len = GetStrWidth(featLine, null, vwenv);
					if (len > maxLen)
						maxLen = len;
				}
			}

			int plusLen = GetVariablesWidth(ctxt, vwenv, true);
			if (plusLen > maxLen)
				maxLen = plusLen;

			int minusLen = GetVariablesWidth(ctxt, vwenv, false);
			if (minusLen > maxLen)
				maxLen = minusLen;
			return maxLen;
		}

		int GetVariablesWidth(IPhSimpleContextNC ctxt, IVwEnv vwenv, bool polarity)
		{
			ILcmReferenceSequence<IPhFeatureConstraint> vars = polarity ? ctxt.PlusConstrRS : ctxt.MinusConstrRS;
			int maxLen = 0;
			foreach (IPhFeatureConstraint var in vars)
			{
				ITsString varLine = CreateVariableLine(var, polarity);
				int len = GetStrWidth(varLine, null, vwenv);
				if (len > maxLen)
					maxLen = len;
			}
			return maxLen;
		}

		protected int GetStrWidth(ITsString tss, ITsTextProps props, IVwEnv vwenv)
		{
			int dmpx, dmpy;
			vwenv.get_StringWidth(tss, props, out dmpx, out dmpy);
			return dmpx;
		}
	}
}
