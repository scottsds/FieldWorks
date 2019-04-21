﻿// Copyright (c) 2015 SIL International
// This software is licensed under the LGPL, version 2.1 or later
// (http://www.gnu.org/licenses/lgpl-2.1.html)

using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using SIL.Collections;
using SIL.LCModel.Core.Text;
using SIL.LCModel.Core.KernelInterfaces;
using SIL.LCModel;
using SIL.LCModel.DomainServices;
using SIL.LCModel.Infrastructure;
using SIL.Machine.Annotations;
using SIL.Machine.FeatureModel;

namespace SIL.FieldWorks.IText
{
	public class ComplexConcParagraphData : IAnnotatedData<ShapeNode>, IDeepCloneable<ComplexConcParagraphData>
	{
		private readonly Shape m_shape;
		private readonly IStTxtPara m_para;

		public ComplexConcParagraphData(SpanFactory<ShapeNode> spanFactory, FeatureSystem featSys, IStTxtPara para)
		{
			m_para = para;
			m_shape = new Shape(spanFactory, begin => new ShapeNode(spanFactory, FeatureStruct.New(featSys).Symbol("bdry").Symbol("paraBdry").Value));
			if (!GenerateShape(spanFactory, featSys))
			{
				// if there are any analyses that are out-of-sync with the baseline, we force a parse
				// and try again, somehow this can happen even though we have already parsed all
				// paragraphs that is out-of-date
				NonUndoableUnitOfWorkHelper.DoSomehow(m_para.Cache.ActionHandlerAccessor, () =>
				{
					using (var pp = new ParagraphParser(m_para.Cache))
					{
						pp.ForceParse(m_para);
					}
				});
				m_shape.Clear();
				if (!GenerateShape(spanFactory, featSys))
					throw new InvalidOperationException("A paragraph cannot be parsed properly.");
			}
		}

		private bool GenerateShape(SpanFactory<ShapeNode> spanFactory, FeatureSystem featSys)
		{
			m_shape.Add(FeatureStruct.New(featSys).Symbol("bdry").Symbol("wordBdry").Value);
			var typeFeat = featSys.GetFeature<SymbolicFeature>("type");
			var catFeat = featSys.GetFeature<SymbolicFeature>("cat");
			var inflFeat = featSys.GetFeature<ComplexFeature>("infl");
			var segments = new Dictionary<ISegment, List<Annotation<ShapeNode>>>();
			foreach (ISegment segment in m_para.SegmentsOS)
			{
				var annotations = new List<Annotation<ShapeNode>>();
				foreach (Tuple<IAnalysis, int, int> analysis in segment.GetAnalysesAndOffsets())
				{
					// check if analyses are out-of-sync with the baseline
					ITsString baselineStr = m_para.Contents.GetSubstring(analysis.Item2, analysis.Item3);
					ITsString formStr = analysis.Item1.GetForm(baselineStr.get_WritingSystemAt(0));
					if (!baselineStr.Text.Equals(formStr.Text, StringComparison.InvariantCultureIgnoreCase))
						return false;

					var wordform = analysis.Item1 as IWfiWordform;
					if (wordform != null)
					{
						var wordFS = new FeatureStruct();
						wordFS.AddValue(typeFeat, typeFeat.PossibleSymbols["word"]);
						foreach (int ws in wordform.Form.AvailableWritingSystemIds)
						{
							StringFeature strFeat;
							if (featSys.TryGetFeature(string.Format("form-{0}", ws), out strFeat))
								wordFS.AddValue(strFeat, wordform.Form.get_String(ws).Text);
						}
						ShapeNode node = m_shape.Add(wordFS);
						node.Annotation.Data = analysis;
						annotations.Add(node.Annotation);
					}
					else
					{
						if (analysis.Item1 is IPunctuationForm)
						{
							annotations.Add(null);
							continue;
						}

						FeatureStruct wordInflFS = null;
						IWfiAnalysis wanalysis = analysis.Item1.Analysis;
						ShapeNode analysisStart = null;
						foreach (IWfiMorphBundle mb in wanalysis.MorphBundlesOS)
						{
							var morphFS = new FeatureStruct();
							morphFS.AddValue(typeFeat, typeFeat.PossibleSymbols["morph"]);
							foreach (int ws in mb.Form.AvailableWritingSystemIds.Union(mb.MorphRA == null ? Enumerable.Empty<int>() : mb.MorphRA.Form.AvailableWritingSystemIds))
							{
								StringFeature strFeat;
								if (!featSys.TryGetFeature(string.Format("form-{0}", ws), out strFeat))
									continue;

								IEnumerable<string> forms = Enumerable.Empty<string>();
								ITsString mbForm = mb.Form.StringOrNull(ws);
								if (mbForm != null)
									forms = forms.Concat(mbForm.Text);
								if (mb.MorphRA != null)
								{
									ITsString morphForm = mb.MorphRA.Form.StringOrNull(ws);
									if (morphForm != null)
										forms = forms.Concat(morphForm.Text);
								}

								morphFS.AddValue(strFeat, forms.Distinct());
							}
							if (mb.SenseRA != null)
							{
								foreach (int ws in mb.SenseRA.Gloss.AvailableWritingSystemIds)
								{
									StringFeature strFeat;
									if (featSys.TryGetFeature(string.Format("gloss-{0}", ws), out strFeat))
										morphFS.AddValue(strFeat, mb.SenseRA.Gloss.get_String(ws).Text);
								}
							}

							if (mb.MorphRA != null)
							{
								var entry = (ILexEntry) mb.MorphRA.Owner;
								foreach (int ws in entry.LexemeFormOA.Form.AvailableWritingSystemIds)
								{
									StringFeature strFeat;
									if (featSys.TryGetFeature(string.Format("entry-{0}", ws), out strFeat))
										morphFS.AddValue(strFeat, entry.LexemeFormOA.Form.get_String(ws).Text);
								}
							}

							if (mb.MsaRA != null && mb.MsaRA.ComponentsRS != null)
							{
								FeatureSymbol[] catSymbols = GetHvoOfMsaPartOfSpeech(mb.MsaRA).Select(hvo => catFeat.PossibleSymbols[hvo.ToString(CultureInfo.InvariantCulture)]).ToArray();
								if (catSymbols.Length > 0)
									morphFS.AddValue(catFeat, catSymbols);
								var inflFS = GetFeatureStruct(featSys, mb.MsaRA);
								if (inflFS != null)
								{
									morphFS.AddValue(inflFeat, inflFS);
									if (wordInflFS == null)
										wordInflFS = inflFS.DeepClone();
									else
										wordInflFS.Union(inflFS);
								}
							}

							ShapeNode node = m_shape.Add(morphFS);
							if (analysisStart == null)
								analysisStart = node;
						}

						var wordFS = new FeatureStruct();
						wordFS.AddValue(typeFeat, typeFeat.PossibleSymbols["word"]);
						if (wanalysis.CategoryRA != null)
							wordFS.AddValue(catFeat, catFeat.PossibleSymbols[wanalysis.CategoryRA.Hvo.ToString(CultureInfo.InvariantCulture)]);
						if (wordInflFS != null && !wordInflFS.IsEmpty)
							wordFS.AddValue(inflFeat, wordInflFS);
						wordform = wanalysis.Wordform;
						foreach (int ws in wordform.Form.AvailableWritingSystemIds)
						{
							StringFeature strFeat;
							if (featSys.TryGetFeature(string.Format("form-{0}", ws), out strFeat))
								wordFS.AddValue(strFeat, wordform.Form.get_String(ws).Text);
						}
						var gloss = analysis.Item1 as IWfiGloss;
						if (gloss != null)
						{
							foreach (int ws in gloss.Form.AvailableWritingSystemIds)
							{
								StringFeature strFeat;
								if (featSys.TryGetFeature(string.Format("gloss-{0}", ws), out strFeat))
									wordFS.AddValue(strFeat, gloss.Form.get_String(ws).Text);
							}
						}
						Annotation<ShapeNode> ann;
						if (analysisStart != null)
						{
							ann = m_shape.Annotations.Add(analysisStart, m_shape.Last, wordFS);
							m_shape.Add(FeatureStruct.New(featSys).Symbol("bdry").Symbol("wordBdry").Value);
						}
						else
						{
							ShapeNode node = m_shape.Add(wordFS);
							ann = node.Annotation;
						}
						ann.Data = analysis;
						annotations.Add(ann);
					}
				}

				segments[segment] = annotations;
				m_shape.Add(FeatureStruct.New(featSys).Symbol("bdry").Symbol("segBdry").Value);
			}

			foreach (ITextTag tag in m_para.OwnerOfClass<IStText>().TagsOC)
			{
				// skip invalid tags
				// TODO: should these tags be cleaned up somewhere?
				if (tag.BeginAnalysisIndex >= tag.BeginSegmentRA.AnalysesRS.Count || tag.EndAnalysisIndex >= tag.EndSegmentRA.AnalysesRS.Count
					|| tag.BeginAnalysisIndex > tag.EndAnalysisIndex)
				{
					continue;
				}
				List<Annotation<ShapeNode>> beginSegment, endSegment;
				if (!segments.TryGetValue(tag.BeginSegmentRA, out beginSegment) || !segments.TryGetValue(tag.EndSegmentRA, out endSegment))
					continue;
				Annotation<ShapeNode> beginAnnotation = beginSegment[tag.BeginAnalysisIndex];
				Annotation<ShapeNode> endAnnotation = endSegment[tag.EndAnalysisIndex];
				ICmPossibility tagType = tag.TagRA;
				if (tagType == null || beginAnnotation == null || endAnnotation == null)
					continue; // guard against LT-14549 crash
				Annotation<ShapeNode> tagAnn = new Annotation<ShapeNode>(spanFactory.Create(beginAnnotation.Span.Start, endAnnotation.Span.End),
					FeatureStruct.New(featSys).Symbol("ttag").Symbol(tagType.Hvo.ToString(CultureInfo.InvariantCulture)).Value) { Data = tag };
				m_shape.Annotations.Add(tagAnn, false);
			}

			return true;
		}

		private ComplexConcParagraphData(ComplexConcParagraphData paraData)
		{
			m_para = paraData.m_para;
			m_shape = paraData.m_shape.DeepClone();
		}

		/// <summary>
		/// Get the hvo(s) for the Part of Speech for the various subclasses of MSA.
		/// N.B. If we add new subclasses or rearrange the class hierarchy, this will
		/// need to change.
		/// </summary>
		/// <param name="msa"></param>
		/// <returns></returns>
		private static IEnumerable<int> GetHvoOfMsaPartOfSpeech(IMoMorphSynAnalysis msa)
		{
			var result = new List<int>();
			ICmPossibility pos;
			var affMsa = msa as IMoInflAffMsa;
			if (affMsa != null)
			{
				pos = affMsa.PartOfSpeechRA;
				if (pos != null)
					result.Add(pos.Hvo);
			}
			var stemMsa = msa as IMoStemMsa;
			if (stemMsa != null)
			{
				pos = stemMsa.PartOfSpeechRA;
				if (pos != null)
					result.Add(pos.Hvo);
			}
			var derivAffMsa = msa as IMoDerivAffMsa;
			if (derivAffMsa != null)
			{
				var derivMsa = derivAffMsa;
				pos = derivMsa.ToPartOfSpeechRA;
				if (pos != null)
					result.Add(pos.Hvo);
				pos = derivMsa.FromPartOfSpeechRA;
				if (pos != null)
					result.Add(pos.Hvo);
			}
			var stepMsa = msa as IMoDerivStepMsa;
			if (stepMsa != null)
			{
				pos = stepMsa.PartOfSpeechRA;
				if (pos != null)
					result.Add(pos.Hvo);
			}
			var affixMsa = msa as IMoUnclassifiedAffixMsa;
			if (affixMsa != null)
			{
				pos = affixMsa.PartOfSpeechRA;
				if (pos != null)
					result.Add(pos.Hvo);
			}
			return result;
		}

		private static FeatureStruct GetFeatureStruct(FeatureSystem featSys, IMoMorphSynAnalysis msa)
		{
			IFsFeatStruc fs = null;
			var stemMsa = msa as IMoStemMsa;
			if (stemMsa != null)
			{
				fs = stemMsa.MsFeaturesOA;
			}
			else
			{
				var inflMsa = msa as IMoInflAffMsa;
				if (inflMsa != null)
				{
					fs = inflMsa.InflFeatsOA;
				}
				else
				{
					var dervMsa = msa as IMoDerivAffMsa;
					if (dervMsa != null)
						fs = dervMsa.ToMsFeaturesOA;
				}
			}

			if (fs != null && !fs.IsEmpty)
			{
				return GetFeatureStruct(featSys, fs);
			}

			return null;
		}

		private static FeatureStruct GetFeatureStruct(FeatureSystem featSys, IFsFeatStruc fs)
		{
			var featStruct = new FeatureStruct();
			foreach (IFsFeatureSpecification featSpec in fs.FeatureSpecsOC)
			{
				var complexVal = featSpec as IFsComplexValue;
				if (complexVal != null)
				{
					var cfs = complexVal.ValueOA as IFsFeatStruc;
					if (complexVal.FeatureRA != null && cfs != null && !cfs.IsEmpty)
						featStruct.AddValue(featSys.GetFeature(complexVal.FeatureRA.Hvo.ToString(CultureInfo.InvariantCulture)), GetFeatureStruct(featSys, cfs));
				}
				else
				{
					var closedVal = featSpec as IFsClosedValue;
					if (closedVal != null && closedVal.FeatureRA != null)
					{
						var symFeat = featSys.GetFeature<SymbolicFeature>(closedVal.FeatureRA.Hvo.ToString(CultureInfo.InvariantCulture));
						FeatureSymbol symbol;
						if (symFeat.PossibleSymbols.TryGetValue(closedVal.ValueRA.Hvo.ToString(CultureInfo.InvariantCulture), out symbol))
							featStruct.AddValue(symFeat, symbol);
					}
				}
			}
			return featStruct;
		}

		public Shape Shape
		{
			get { return m_shape; }
		}

		public IStTxtPara Paragraph
		{
			get { return m_para; }
		}

		public Span<ShapeNode> Span
		{
			get { return m_shape.Span; }
		}

		public AnnotationList<ShapeNode> Annotations
		{
			get { return m_shape.Annotations; }
		}

		public ComplexConcParagraphData DeepClone()
		{
			return new ComplexConcParagraphData(this);
		}
	}
}
