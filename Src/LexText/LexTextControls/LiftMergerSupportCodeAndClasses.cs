// Copyright (c) 2015-2017 SIL International
// This software is licensed under the LGPL, version 2.1 or later
// (http://www.gnu.org/licenses/lgpl-2.1.html)

using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Windows.Forms;
using System.Xml;
using SIL.Lift.Parsing;
using SIL.LCModel.Core.Cellar;
using SIL.LCModel.Core.Text;
using SIL.LCModel.Core.WritingSystems;
using SIL.LCModel.Core.KernelInterfaces;
using SIL.FieldWorks.Common.FwUtils;
using SIL.LCModel;
using SIL.LCModel.DomainServices;
using SIL.LCModel.Utils;
using SIL.Utils;
using SIL.WritingSystems;

namespace SIL.FieldWorks.LexText.Controls
{
	/// <summary>
	/// This class is called by the LiftParser, as it encounters each element of a lift file.
	/// There is at least one other ILexiconMerger implementation, used in WeSay.
	/// </summary>
	public partial class FlexLiftMerger : ILexiconMerger<LiftObject, CmLiftEntry, CmLiftSense, CmLiftExample>
	{

		//===========================================================================
		#region Constructors and other initialization methods
		private void InitializePossibilityMaps()
		{
			if (m_cache.LangProject.PartsOfSpeechOA != null)
				InitializePossibilityMap(m_cache.LangProject.PartsOfSpeechOA.PossibilitiesOS, m_dictPos);
			InitializeMorphTypes();
			if (m_cache.LangProject.LexDbOA.ComplexEntryTypesOA != null)
				InitializePossibilityMap(m_cache.LangProject.LexDbOA.ComplexEntryTypesOA.PossibilitiesOS,
										 m_dictComplexFormType);
			if (m_cache.LangProject.LexDbOA.VariantEntryTypesOA != null)
				InitializePossibilityMap(m_cache.LangProject.LexDbOA.VariantEntryTypesOA.PossibilitiesOS,
										 m_dictVariantType);
			if (m_cache.LangProject.SemanticDomainListOA != null)
			{
				InitializePossibilityMap(m_cache.LangProject.SemanticDomainListOA.PossibilitiesOS,
										 m_dictSemDom);
				EnhancePossibilityMapForWeSay(m_cache.LangProject.SemanticDomainListOA.PossibilitiesOS,
											  m_dictSemDom);
			}
			if (m_cache.LangProject.TranslationTagsOA != null)
				InitializePossibilityMap(m_cache.LangProject.TranslationTagsOA.PossibilitiesOS,
										 m_dictTransType);
			if (m_cache.LangProject.AnthroListOA != null)
				InitializePossibilityMap(m_cache.LangProject.AnthroListOA.PossibilitiesOS,
										 m_dictAnthroCode);
			if (m_cache.LangProject.MorphologicalDataOA != null)
			{
				if (m_cache.LangProject.MorphologicalDataOA.ProdRestrictOA != null)
				{
					InitializePossibilityMap(m_cache.LangProject.MorphologicalDataOA.ProdRestrictOA.PossibilitiesOS,
											 m_dictExceptFeats);
				}
			}
			if (m_cache.LangProject.LexDbOA.DialectLabelsOA != null)
				InitializePossibilityMap(m_cache.LangProject.LexDbOA.DialectLabelsOA.PossibilitiesOS,
										 m_dictDialect);
			if (m_cache.LangProject.LexDbOA.DomainTypesOA != null)
				InitializePossibilityMap(m_cache.LangProject.LexDbOA.DomainTypesOA.PossibilitiesOS,
										 m_dictDomainType);
			if (m_cache.LangProject.LexDbOA.PublicationTypesOA != null)
				InitializePossibilityMap(m_cache.LangProject.LexDbOA.PublicationTypesOA.PossibilitiesOS,
										m_dictPublicationTypes);
			if (m_cache.LangProject.LexDbOA.SenseTypesOA != null)
				InitializePossibilityMap(m_cache.LangProject.LexDbOA.SenseTypesOA.PossibilitiesOS,
										 m_dictSenseType);
			if (m_cache.LangProject.StatusOA != null)
				InitializePossibilityMap(m_cache.LangProject.StatusOA.PossibilitiesOS,
										 m_dictStatus);
			if (m_cache.LangProject.LexDbOA.UsageTypesOA != null)
				InitializePossibilityMap(m_cache.LangProject.LexDbOA.UsageTypesOA.PossibilitiesOS,
										 m_dictUsageType);
			if (m_cache.LangProject.LocationsOA != null)
				InitializePossibilityMap(m_cache.LangProject.LocationsOA.PossibilitiesOS,
										 m_dictLocation);
			if (m_cache.LangProject.LexDbOA.LanguagesOA != null)
				InitializePossibilityMap(m_cache.LangProject.LexDbOA.LanguagesOA.PossibilitiesOS,
										 m_dictLanguage);
			if (m_cache.LangProject.PhonologicalDataOA != null)
			{
				foreach (IPhEnvironment env in m_cache.LangProject.PhonologicalDataOA.EnvironmentsOS)
				{
					// More than one environment may have the same string representation.  This
					// is unfortunate, but it does happen.
					string s = env.StringRepresentation.Text;
					if (!String.IsNullOrEmpty(s))
					{
						List<IPhEnvironment> rgenv;
						if (m_dictEnvirons.TryGetValue(s, out rgenv))
						{
							rgenv.Add(env);
						}
						else
						{
							rgenv = new List<IPhEnvironment>();
							rgenv.Add(env);
							m_dictEnvirons.Add(s, rgenv);
						}
					}
				}
			}
			if (m_cache.LangProject.LexDbOA.ReferencesOA != null)
				InitializePossibilityMap(m_cache.LangProject.LexDbOA.ReferencesOA.PossibilitiesOS,
										 m_dictLexRefTypes);
		}

		private void InitializePossibilityMap(ILcmOwningSequence<ICmPossibility> possibilities,
			Dictionary<string, ICmPossibility> dict)
		{
			if (possibilities == null)
				return;
			int ws;
			foreach (ICmPossibility poss in possibilities)
			{
				for (int i = 0; i < poss.Abbreviation.StringCount; ++i)
				{
					ITsString tss = poss.Abbreviation.GetStringFromIndex(i, out ws);
					AddToPossibilityMap(tss, poss, dict);
				}
				for (int i = 0; i < poss.Name.StringCount; ++i)
				{
					ITsString tss = poss.Name.GetStringFromIndex(i, out ws);
					AddToPossibilityMap(tss, poss, dict);
				}
				InitializePossibilityMap(poss.SubPossibilitiesOS, dict);
			}
		}

		private static void AddToPossibilityMap(ITsString tss, ICmPossibility poss, Dictionary<string, ICmPossibility> dict)
		{
			if (tss.Length > 0)
			{
				string s = tss.Text.Normalize();
				if (!dict.ContainsKey(s))
					dict.Add(s, poss);
				s = s.ToLowerInvariant();
				if (!dict.ContainsKey(s))
					dict.Add(s, poss);
			}
		}

		private void InitializeReversalPOSMaps()
		{
			foreach (IReversalIndex ri in m_cache.LangProject.LexDbOA.ReversalIndexesOC)
			{
				var dict = new Dictionary<string, ICmPossibility>();
				if (ri.PartsOfSpeechOA != null)
					InitializePossibilityMap(ri.PartsOfSpeechOA.PossibilitiesOS, dict);
				Debug.Assert(!string.IsNullOrEmpty(ri.WritingSystem));
				int handle = m_cache.ServiceLocator.WritingSystemManager.GetWsFromStr(ri.WritingSystem);
				if (m_dictWsReversalPos.ContainsKey(handle))
				{
					// REVIEW: SHOULD WE LOG A WARNING HERE?  THIS SHOULD NEVER HAPPEN!
					// (BUT IT HAS AT LEAST ONCE IN A 5.4.1 PROJECT)
				}
				else
				{
					m_dictWsReversalPos.Add(handle, dict);
				}
			}
		}

		/// <summary>
		/// WeSay stores Semantic Domain values as "abbr name", so fill in keys like that
		/// for lookup during import.
		/// </summary>
		/// <param name="possibilities"></param>
		/// <param name="dict"></param>
		private void EnhancePossibilityMapForWeSay(ILcmOwningSequence<ICmPossibility> possibilities,
			Dictionary<string, ICmPossibility> dict)
		{
			foreach (ICmPossibility poss in possibilities)
			{
				for (int i = 0; i < poss.Abbreviation.StringCount; ++i)
				{
					int ws;
					ITsString tssAbbr = poss.Abbreviation.GetStringFromIndex(i, out ws);
					if (tssAbbr.Length > 0)
					{
						ITsString tssName = poss.Name.get_String(ws);
						if (tssName.Length > 0)
						{
							string sAbbr = tssAbbr.Text;
							string sName = tssName.Text;
							string sKey = String.Format("{0} {1}", sAbbr, sName);
							if (!dict.ContainsKey(sKey))
								dict.Add(sKey, poss);
							sKey = sKey.ToLowerInvariant();
							if (!dict.ContainsKey(sKey))
								dict.Add(sKey, poss);
						}
					}
				}
				EnhancePossibilityMapForWeSay(poss.SubPossibilitiesOS, dict);
			}
		}

		#endregion //Constructors and other initialization methods

		//===========================================================================

		#region String matching, merging, extracting, etc.
		/// <summary>
		/// Merge in a form that may need to have morphtype markers stripped from it.
		/// forms is safe-XML
		/// </summary>
		private void MergeInAllomorphForms(LiftMultiText forms, ITsMultiString tsm,
			int clsidForm, Guid guidEntry, int flid)
		{
			// If we're keeping only the imported data, erase any existing data that isn't
			// overwritten by imported data.
			Dictionary<int, string> multi;
			if (m_msImport == MergeStyle.MsKeepOnlyNew)
				multi = GetAllUnicodeAlternatives(tsm);
			else
				multi = new Dictionary<int, string>();
			AddNewWsToVernacular();
			foreach (string key in forms.Keys)
			{
				int wsHvo = GetWsFromLiftLang(key);
				// LiftMultiText parameter may have come in with escaped characters which need to be
				// converted to plain text before merging with existing entries
				string form = XmlUtils.DecodeXml(forms[key].Text);
				if (wsHvo > 0 && !String.IsNullOrEmpty(form))
				{
					multi.Remove(wsHvo);
					bool fUpdate = false;
					if (!m_fCreatingNewEntry && m_msImport == MergeStyle.MsKeepOld)
					{
						ITsString tssOld = tsm.get_String(wsHvo);
						if (tssOld == null || tssOld.Length == 0)
							fUpdate = true;
					}
					else
					{
						fUpdate = true;
					}
					if (fUpdate)
					{
						string sAllo = form;
						if (IsVoiceWritingSystem(wsHvo))
						{
							string sPath = Path.Combine(Path.GetDirectoryName(m_sLiftFile),
								String.Format("audio{0}{1}", Path.DirectorySeparatorChar, form));
							CopyFileToLinkedFiles(form, sPath, LcmFileHelper.ksMediaDir);
						}
						else
						{
							sAllo = StripAlloForm(form, clsidForm, guidEntry, flid);
						}
						tsm.set_String(wsHvo, TsStringUtils.MakeString(sAllo, wsHvo));
					}
				}
			}
			foreach (int ws in multi.Keys)
				tsm.set_String(ws, null);
		}

		private string StripAlloForm(string form, int clsidForm, Guid guidEntry, int flid)
		{
			int clsid;
			// Strip any affix/clitic markers from the form before storing it.
			FindMorphType(ref form, out clsid, guidEntry, flid);
			if (clsidForm != 0 && clsid != clsidForm)
			{
				// complain about varying morph types??
			}
			return form;
		}

		/// <summary>
		/// Answer true if tsm already matches forms, in all the alternatives that would be set by MergeMultiString.
		/// <param name="tsm"></param>
		/// <param name="forms">safe-XML</param>
		/// </summary>
		private bool MatchMultiString(ITsMultiString tsm, LiftMultiText forms)
		{
			foreach (string key in forms.Keys)
			{
				int wsHvo = GetWsFromLiftLang(key);
				if (wsHvo <= 0)
					continue;
				var tssWanted = CreateTsStringFromLiftString(forms[key], wsHvo);
				var tssActual = tsm.get_String(wsHvo);
				if (!tssActual.Equals(tssWanted))
					return false;
			}
			return true;
		}

		/// <summary>
		/// Merge in a Multi(Ts)String type value.
		/// </summary>
		/// <param name="tsm"></param>
		/// <param name="flid"></param>
		/// <param name="forms">safe-XML</param>
		/// <param name="guidObj"></param>
		private void MergeInMultiString(ITsMultiString tsm, int flid, LiftMultiText forms, Guid guidObj)
		{
			// If we're keeping only the imported data, erase any existing data that isn't
			// overwritten by imported data.
			Dictionary<int, ITsString> multi;
			if (m_msImport == MergeStyle.MsKeepOnlyNew)
				multi = GetAllTsStringAlternatives(tsm);
			else
				multi = new Dictionary<int, ITsString>();
			if (forms != null && forms.Keys != null)
			{
				foreach (string key in forms.Keys)
				{
					int wsHvo = GetWsFromLiftLang(key);
					if (wsHvo > 0)
					{
						multi.Remove(wsHvo);
						if (!m_fCreatingNewEntry &&
							!m_fCreatingNewSense &&
							m_msImport == MergeStyle.MsKeepOld)
						{
							ITsString tssOld = tsm.get_String(wsHvo);
							if (tssOld != null && tssOld.Length != 0)
								continue;
						}
						ITsString tss = CreateTsStringFromLiftString(forms[key], wsHvo);
						tsm.set_String(wsHvo, tss);
						if (tss.RunCount == 1 && IsVoiceWritingSystem(wsHvo))
						{
							string sPath = Path.Combine(Path.GetDirectoryName(m_sLiftFile),
								String.Format("audio{0}{1}", Path.DirectorySeparatorChar, tss.Text));
							CopyFileToLinkedFiles(tss.Text, sPath, LcmFileHelper.ksMediaDir);
						}
					}
				}
			}
			foreach (int ws in multi.Keys)
				tsm.set_String(ws, null);
		}

		/// <summary>
		/// Make a TsString from the (safe-XML) input LiftString
		/// </summary>
		/// <param name="liftstr">safe-XML</param>
		/// <param name="wsHvo"></param>
		/// <returns></returns>
		private ITsString CreateTsStringFromLiftString(LiftString liftstr, int wsHvo)
		{
			ITsStrBldr tsb = TsStringUtils.MakeStrBldr();
			// LiftString parameter may have come in with escaped characters which need to be
			// converted to plain text before comparing with existing entries
			var convertSafeXmlToText = XmlUtils.DecodeXml(liftstr.Text);
			tsb.Replace(0, tsb.Length, convertSafeXmlToText, TsStringUtils.MakeProps(null, wsHvo));
			int wsSpan;
			// TODO: handle nested spans.
			foreach (LiftSpan span in liftstr.Spans)
			{
				ITsPropsBldr tpb = TsStringUtils.MakePropsBldr();
				if (String.IsNullOrEmpty(span.Lang))
					wsSpan = wsHvo;
				else
					wsSpan = GetWsFromLiftLang(span.Lang);
				tpb.SetIntPropValues((int)FwTextPropType.ktptWs, 0, wsSpan);
				if (!String.IsNullOrEmpty(span.Class))
					tpb.SetStrPropValue((int)FwTextPropType.ktptNamedStyle, span.Class);
				if (!String.IsNullOrEmpty(span.LinkURL))
				{
					string linkPath = FileUtils.StripFilePrefix(span.LinkURL);
					if (MiscUtils.IsUnix)
						linkPath = linkPath.TrimStart('/');
					string sPath = Path.Combine(Path.GetDirectoryName(m_sLiftFile), linkPath);
					if (linkPath.StartsWith("others" + '/') || linkPath.StartsWith("others" + "\\")
						|| linkPath.StartsWith("others" + Path.DirectorySeparatorChar))
					{
						linkPath = CopyFileToLinkedFiles(linkPath.Substring("others/".Length), sPath,
							LcmFileHelper.ksOtherLinkedFilesDir);
					}
					char chOdt = Convert.ToChar((int)FwObjDataTypes.kodtExternalPathName);
					string sRef = chOdt.ToString() + linkPath;
					tpb.SetStrPropValue((int)FwTextPropType.ktptObjData, sRef);
				}
				tsb.SetProperties(span.Index, span.Index + span.Length, tpb.GetTextProps());
			}
			return tsb.GetString();
		}

		/// <summary>
		/// Merge in a MultiUnicode type value.
		/// <param name="tsm"></param>
		/// <param name="flid"></param>
		/// <param name="forms">safe XML</param>
		/// <param name="guidObj"></param>
		/// </summary>
		private void MergeInMultiUnicode(ITsMultiString tsm, int flid, LiftMultiText forms, Guid guidObj)
		{
			// If we're keeping only the imported data, erase any existing data that isn't
			// overwritten by imported data.
			Dictionary<int, string> multi;
			if (m_msImport == MergeStyle.MsKeepOnlyNew)
				multi = GetAllUnicodeAlternatives(tsm);
			else
				multi = new Dictionary<int, string>();
			if (forms != null && forms.Keys != null)
			{
				foreach (string key in forms.Keys)
				{
					int wsHvo = GetWsFromLiftLang(key);
					if (wsHvo > 0)
					{
						multi.Remove(wsHvo);
						// LiftMultiText parameter may have come in with escaped characters which need to be
						// converted to plain text before merging with existing entries
						string sText = XmlUtils.DecodeXml(forms[key].Text);
						if (!m_fCreatingNewEntry && !m_fCreatingNewSense && m_msImport == MergeStyle.MsKeepOld)
						{
							ITsString tss = tsm.get_String(wsHvo);
							if (tss == null || tss.Length == 0)
								tsm.set_String(wsHvo, TsStringUtils.MakeString(sText, wsHvo));
						}
						else
						{
							tsm.set_String(wsHvo, TsStringUtils.MakeString(sText, wsHvo));
						}
					}
				}
			}
			foreach (int ws in multi.Keys)
				tsm.set_String(ws, null);
		}

		Dictionary<string, int> m_mapLangWs = new Dictionary<string, int>();
		private bool m_fAddNewWsToVern;
		private bool m_fAddNewWsToAnal;

		private void AddNewWsToAnalysis()
		{
			m_fAddNewWsToVern = false;
			m_fAddNewWsToAnal = true;
		}

		private void AddNewWsToVernacular()
		{
			m_fAddNewWsToVern = true;
			m_fAddNewWsToAnal = false;
		}

		private void AddNewWsToBothVernAnal()
		{
			m_fAddNewWsToVern = true;
			m_fAddNewWsToAnal = true;
		}

		private void IgnoreNewWs()
		{
			m_fAddNewWsToVern = false;
			m_fAddNewWsToAnal = false;
		}

		public int GetWsFromLiftLang(string key)
		{
			int hvo;
			if (m_mapLangWs.TryGetValue(key, out hvo))
				return hvo;
			CoreWritingSystemDefinition ws;
			if (!WritingSystemServices.FindOrCreateSomeWritingSystem(m_cache, FwDirectoryFinder.TemplateDirectory, key,
				m_fAddNewWsToAnal, m_fAddNewWsToVern, out ws))
			{
				m_addedWss.Add(ws);
				// Use the LDML file if it's available.  Look in the current location first, then look
				// in the old location.
				string ldmlFile = Path.Combine(Path.Combine(m_sLiftDir, "WritingSystems"), key + ".ldml");
				if (!File.Exists(ldmlFile))
					ldmlFile = Path.Combine(m_sLiftDir, key + ".ldml");
				if (File.Exists(ldmlFile) && key == ws.Id)
				{
					string id = ws.Id;
					var adaptor = new LdmlDataMapper(new WritingSystemFactory());
					adaptor.Read(ldmlFile, ws);
					ws.Id = id;
					ws.ForceChanged();
				}
			}
			m_mapLangWs.Add(key, ws.Handle);
			// If FindOrCreate had to get creative, the WS ID may not match the input identifier. We want both the
			// original and actual keys in the map.
			if (!m_mapLangWs.ContainsKey(ws.Id))
				m_mapLangWs.Add(ws.Id, ws.Handle);
			return ws.Handle;
		}

		/// <summary>
		/// Both arguments must be in safe-XML form
		/// </summary>
		/// <param name="mtCurrent"></param>
		/// <param name="mtNew"></param>
		private void MergeLiftMultiTexts(LiftMultiText mtCurrent, LiftMultiText mtNew)
		{
			foreach (string key in mtNew.Keys)
			{
				if (mtCurrent.ContainsKey(key))
				{
					if (m_fCreatingNewEntry || m_fCreatingNewSense || m_msImport != MergeStyle.MsKeepOld)
						mtCurrent.Add(key, mtNew[key]);
				}
				else
				{
					mtCurrent.Add(key, mtNew[key]);
				}
			}
		}

		/// <summary>
		/// Make a TsString out of the first value in the (safe-XML) contents
		/// </summary>
		/// <param name="contents">safe-XML</param>
		/// <returns></returns>
		private ITsString GetFirstLiftTsString(LiftMultiText contents)
		{
			if (contents != null && !contents.IsEmpty)
			{
				int ws = GetWsFromLiftLang(contents.FirstValue.Key);
				return CreateTsStringFromLiftString(contents.FirstValue.Value, ws);
			}
			else
			{
				return null;
			}
		}

		private void SetStringsFromLiftContents(LiftMultiText contents, ITsMultiString destination)
		{
			if(contents != null && !contents.IsEmpty)
			{
				foreach(KeyValuePair<string, LiftString> keyValuePair in contents)
				{
					var ws = GetWsFromLiftLang(keyValuePair.Key);
					destination.set_String(ws, CreateTsStringFromLiftString(keyValuePair.Value, ws));
				}
			}
		}

		HashSet<Tuple<string, string, string, ICmObject>> m_reportedMergeProblems = new HashSet<Tuple<string, string, string, ICmObject>>();

		/// <summary>
		/// Fill in any missing alternatives from the LIFT data.
		/// </summary>
		/// <param name="contents"></param>
		/// <param name="destination"></param>
		private void MergeStringsFromLiftContents(LiftMultiText contents, ITsMultiString destination, string attr, ICmObject obj)
		{
			if (contents != null && !contents.IsEmpty)
			{
				foreach (KeyValuePair<string, LiftString> keyValuePair in contents)
				{
					var ws = GetWsFromLiftLang(keyValuePair.Key);
					var liftString = CreateTsStringFromLiftString(keyValuePair.Value, ws);
					var ourString = destination.get_String(ws);
					if (string.IsNullOrEmpty(ourString.Text))
					{
						destination.set_String(ws, liftString);
					}
					else if (liftString.Text.Normalize() != ourString.Text.Normalize()) // ignore the very unlikely case of more subtle differences we can't report
					{
						// Fairly typically this is called more than once on the same object...not quite sure why. Simplest thing is not to make
						// the same report repeatedly.
						var key = Tuple.Create(liftString.Text, ourString.Text, attr, obj);
						if (!m_reportedMergeProblems.Contains(key))
						{
							m_reportedMergeProblems.Add(key);
							m_rgErrorMsgs.Add(string.Format(LexTextControls.ksNonMatchingRelation, liftString.Text, ourString.Text, attr,
								obj.ShortName));
						}
					}
				}
			}
		}

		public bool TsStringIsNullOrEmpty(ITsString tss)
		{
			return tss == null || tss.Length == 0;
		}

		private bool StringsConflict(string sOld, string sNew)
		{
			if (String.IsNullOrEmpty(sOld))
				return false;
			if (sNew == null)
				return false;
			string sNewNorm = CustomIcu.GetIcuNormalizer(FwNormalizationMode.knmNFD).Normalize(sNew);
			string sOldNorm = CustomIcu.GetIcuNormalizer(FwNormalizationMode.knmNFD).Normalize(sOld);
			return sNewNorm != sOldNorm;
		}

		private bool StringsConflict(ITsString tssOld, ITsString tssNew)
		{
			if (TsStringIsNullOrEmpty(tssOld))
				return false;
			if (tssNew == null)
				return false;
			ITsString tssNewNorm = tssNew.get_NormalizedForm(FwNormalizationMode.knmNFD);
			ITsString tssOldNorm = tssOld.get_NormalizedForm(FwNormalizationMode.knmNFD);
			return !tssOldNorm.Equals(tssNewNorm);
		}

		/// <summary>
		///
		/// </summary>
		/// <param name="tsm"></param>
		/// <param name="lmt">safe-XML</param>
		/// <param name="fStripMarkers"></param>
		/// <param name="guidEntry"></param>
		/// <param name="flid"></param>
		/// <returns></returns>
		private bool MultiUnicodeStringsConflict(ITsMultiString tsm, LiftMultiText lmt, bool fStripMarkers,
			Guid guidEntry, int flid)
		{
			if (tsm == null || lmt == null || lmt.IsEmpty)
				return false;
			foreach (string key in lmt.Keys)
			{
				int wsHvo = GetWsFromLiftLang(key);
				if (wsHvo < 0)
					continue;		// Should never happen!
				string sNew = XmlUtils.DecodeXml(lmt[key].Text);
				if (fStripMarkers)
					sNew = StripAlloForm(sNew, 0, guidEntry, flid);
				ITsString tssOld = tsm.get_String(wsHvo);
				if (tssOld == null || tssOld.Length == 0)
					continue;
				string sOld = tssOld.Text;
				string sNewNorm = CustomIcu.GetIcuNormalizer(FwNormalizationMode.knmNFD).Normalize(sNew);
				string sOldNorm = CustomIcu.GetIcuNormalizer(FwNormalizationMode.knmNFD).Normalize(sOld);
				if (sNewNorm != sOldNorm)
					return true;
			}
			return false;
		}

		/// <summary>
		///
		/// </summary>
		/// <param name="tsm"></param>
		/// <param name="lmt">safe XML</param>
		/// <returns></returns>
		private bool MultiTsStringsConflict(ITsMultiString tsm, LiftMultiText lmt)
		{
			if (tsm == null || lmt == null || lmt.IsEmpty)
				return false;
			foreach (string key in lmt.Keys)
			{
				int wsHvo = GetWsFromLiftLang(key);
				if (wsHvo < 0)
					continue;		// Should never happen!
				ITsString tssNew = CreateTsStringFromLiftString(lmt[key], wsHvo);
				ITsString tss = tsm.get_String(wsHvo);
				if (tss == null || tss.Length == 0)
					continue;
				ITsString tssOld = tss;
				ITsString tssOldNorm = tssOld.get_NormalizedForm(FwNormalizationMode.knmNFD);
				ITsString tssNewNorm = tssNew.get_NormalizedForm(FwNormalizationMode.knmNFD);
				if (!tssOldNorm.Equals(tssNewNorm))
					return true;
			}
			return false;
		}

		/// <summary>
		///
		/// </summary>
		/// <param name="tsm"></param>
		/// <param name="lmt">safe XML</param>
		/// <param name="fStripMarkers"></param>
		/// <param name="guidEntry"></param>
		/// <param name="flid"></param>
		/// <returns></returns>
		private int MultiUnicodeStringMatches(ITsMultiString tsm, LiftMultiText lmt, bool fStripMarkers,
			Guid guidEntry, int flid)
		{
			if (tsm == null && (lmt == null || lmt.IsEmpty))
				return 1;
			if (tsm == null || lmt == null || lmt.IsEmpty)
				return 0;
			int cMatches = 0;
			foreach (string key in lmt.Keys)
			{
				int wsHvo = GetWsFromLiftLang(key);
				if (wsHvo < 0)
					continue;		// Should never happen!
				ITsString tssOld = tsm.get_String(wsHvo);
				if (tssOld == null || tssOld.Length == 0)
					continue;
				string sOld = tssOld.Text;
				string sNew = XmlUtils.DecodeXml(lmt[key].Text);
				if (fStripMarkers)
					sNew = StripAlloForm(sNew, 0, guidEntry, flid);
				string sNewNorm = CustomIcu.GetIcuNormalizer(FwNormalizationMode.knmNFD).Normalize(sNew);
				string sOldNorm = CustomIcu.GetIcuNormalizer(FwNormalizationMode.knmNFD).Normalize(sOld);
				if (sNewNorm == sOldNorm)
					++cMatches;
			}
			return cMatches;
		}

		/// <summary>
		///
		/// </summary>
		/// <param name="tsm"></param>
		/// <param name="lmt">safe-XML</param>
		/// <returns></returns>
		private int MultiTsStringMatches(ITsMultiString tsm, LiftMultiText lmt)
		{
			if (tsm == null && (lmt == null || lmt.IsEmpty))
				return 1;
			if (tsm == null || lmt == null || lmt.IsEmpty)
				return 0;
			int cMatches = 0;
			foreach (string key in lmt.Keys)
			{
				int wsHvo = GetWsFromLiftLang(key);
				if (wsHvo < 0)
					continue;
				ITsString tss = tsm.get_String(wsHvo);
				if (tss == null || tss.Length == 0)
					continue;
				ITsString tssOld = tss;
				ITsString tssNew = CreateTsStringFromLiftString(lmt[key], wsHvo);
				ITsString tssOldNorm = tssOld.get_NormalizedForm(FwNormalizationMode.knmNFD);
				ITsString tssNewNorm = tssNew.get_NormalizedForm(FwNormalizationMode.knmNFD);
				if (tssOldNorm.Equals(tssNewNorm))
					++cMatches;
			}
			return cMatches;
		}

		/// <summary>
		///
		/// </summary>
		/// <param name="contents">safe XML</param>
		/// <param name="tsm"></param>
		/// <returns></returns>
		private bool SameMultiUnicodeContent(LiftMultiText contents, ITsMultiString tsm)
		{
			foreach (string key in contents.Keys)
			{
				int ws = GetWsFromLiftLang(key);
				string sNew = contents[key].Text;
				ITsString tssOld = tsm.get_String(ws);
				if (String.IsNullOrEmpty(sNew) && (tssOld == null || tssOld.Length == 0))
					continue;
				if (String.IsNullOrEmpty(sNew) || (tssOld == null || tssOld.Length == 0))
					return false;
				// LiftMultiText parameter may have come in with escaped characters which need to be
				// converted to plain text before comparing with existing entries
				string sNewNorm = XmlUtils.DecodeXml(CustomIcu.GetIcuNormalizer(FwNormalizationMode.knmNFD).Normalize(sNew));
				string sOldNorm = CustomIcu.GetIcuNormalizer(FwNormalizationMode.knmNFD).Normalize(tssOld.Text);
				if (sNewNorm != sOldNorm)
					return false;
			}
			// TODO: check whether all strings in mua are found in contents?
			return true;
		}

		/// <summary>
		/// Check whether any of the given unicode values match in any of the writing
		/// systems.
		/// </summary>
		/// <param name="sVal">value to match against</param>
		/// <param name="tsmAbbr">accessor for abbreviation (or null)</param>
		/// <param name="tsmName">accessor for name</param>
		/// <returns></returns>
		private bool HasMatchingUnicodeAlternative(string sVal, ITsMultiString tsmAbbr,
			ITsMultiString tsmName)
		{
			int ws;
			if (tsmAbbr != null)
			{
				for (int i = 0; i < tsmAbbr.StringCount; ++i)
				{
					ITsString tss = tsmAbbr.GetStringFromIndex(i, out ws);
					// TODO: try tss.Text.Equals(sVal, StringComparison.InvariantCultureIgnoreCase)
					if (tss.Length > 0 && tss.Text.ToLowerInvariant() == sVal)
						return true;
				}
			}
			if (tsmName != null)
			{
				for (int i = 0; i < tsmName.StringCount; ++i)
				{
					ITsString tss = tsmName.GetStringFromIndex(i, out ws);
					// TODO: try tss.Text.Equals(sVal, StringComparison.InvariantCultureIgnoreCase)
					if (tss.Length > 0 && tss.Text.ToLowerInvariant() == sVal)
						return true;
				}
			}
			return false;
		}

		/// <summary>
		/// Write the string as HTML, interpreting the string properties as best we can.
		/// </summary>
		/// <param name="tss">The string.</param>
		/// <param name="cache">The cache.</param>
		/// <returns></returns>
		public string TsStringAsHtml(ITsString tss, LcmCache cache)
		{
			StringBuilder sb = new StringBuilder();
			int crun = tss.RunCount;
			for (int irun = 0; irun < crun; ++irun)
			{
				int iMin = tss.get_MinOfRun(irun);
				int iLim = tss.get_LimOfRun(irun);
				string sLang = null;
				string sDir = null;
				string sFont = null;
				ITsTextProps ttp = tss.get_Properties(irun);
				int nVar;
				int ws = ttp.GetIntPropValues((int)FwTextPropType.ktptWs, out nVar);
				if (ws > 0)
				{
					CoreWritingSystemDefinition wsObj = GetExistingWritingSystem(ws);
					sLang = wsObj.Id;
					sDir = wsObj.RightToLeftScript ? "RTL" : "LTR";
					sFont = wsObj.DefaultFontName;
				}
				int nSuperscript = ttp.GetIntPropValues((int)FwTextPropType.ktptSuperscript, out nVar);
				switch (nSuperscript)
				{
					case (int)FwSuperscriptVal.kssvSuper:
						sb.Append("<sup");
						break;
					case (int)FwSuperscriptVal.kssvSub:
						sb.Append("<sub");
						break;
					default:
						sb.Append("<span");
						break;
				}
				if (!String.IsNullOrEmpty(sLang))
					sb.AppendFormat(" lang=\"{0}\"", sLang);
				if (!String.IsNullOrEmpty(sDir))
					sb.AppendFormat(" dir=\"{0}\"", sDir);
				if (!String.IsNullOrEmpty(sFont))
					sb.AppendFormat(" style=\"font-family: '{0}', serif\"", sFont);
				sb.Append(">");
				sb.Append(tss.Text.Substring(iMin, iLim - iMin));
				switch (nSuperscript)
				{
					case (int)FwSuperscriptVal.kssvSuper:
						sb.Append("</sup>");
						break;
					case (int)FwSuperscriptVal.kssvSub:
						sb.Append("</sub>");
						break;
					default:
						sb.Append("</span>");
						break;
				}
			}
			return sb.ToString();
		}
		#endregion // String matching, merging, extracting, etc.

		//===========================================================================

		#region Storing LIFT import residue...
		private XmlDocument FindOrCreateResidue(ICmObject cmo, string sId, int flid)
		{
			LiftResidue res;
			if (!m_dictResidue.TryGetValue(cmo.Hvo, out res))
			{
				res = CreateLiftResidue(cmo.Hvo, flid, sId);
				m_dictResidue.Add(cmo.Hvo, res);
			}
			else if (!String.IsNullOrEmpty(sId))
			{
				EnsureIdSet(res.Document.FirstChild, sId);
			}
			return res.Document;
		}

		/// <summary>
		/// This creates a new LiftResidue object with an empty XML document (empty except for
		/// the enclosing &lt;lift-residue&gt; element, that is).
		/// As a side-effect, it moves any existing LIFT residue for LexEntry or LexSense from
		/// ImportResidue to LiftResidue.
		/// </summary>
		/// <param name="hvo"></param>
		/// <param name="flid"></param>
		/// <param name="sId"></param>
		/// <returns></returns>
		private LiftResidue CreateLiftResidue(int hvo, int flid, string sId)
		{
			string sResidue = null;
			// The next four lines move any existing LIFT residue from ImportResidue to LiftResidue.
			if (flid == LexEntryTags.kflidLiftResidue)
				ExtractLIFTResidue(m_cache, hvo, LexEntryTags.kflidImportResidue, flid);
			else if (flid == LexSenseTags.kflidLiftResidue)
				ExtractLIFTResidue(m_cache, hvo, LexSenseTags.kflidImportResidue, flid);
			if (String.IsNullOrEmpty(sId))
				sResidue = "<lift-residue></lift-residue>";
			else
				sResidue = String.Format("<lift-residue id=\"{0}\"></lift-residue>", XmlUtils.MakeSafeXmlAttribute(sId));
			XmlDocument xd = new XmlDocument();
			xd.PreserveWhitespace = true;
			xd.LoadXml(sResidue);
			return new LiftResidue(flid, xd);
		}

		private void EnsureIdSet(XmlNode xn, string sId)
		{
			XmlAttribute xa = xn.Attributes["id"];
			if (xa == null)
			{
				xa = xn.OwnerDocument.CreateAttribute("id");
				xa.Value = XmlUtils.MakeSafeXmlAttribute(sId);
				xn.Attributes.Append(xa);
			}
			else if (String.IsNullOrEmpty(xa.Value))
			{
				xa.Value = XmlUtils.MakeSafeXmlAttribute(sId);
			}
		}

		/// <summary>
		/// Scan ImportResidue for XML looking string inserted by LIFT import.  If any is found,
		/// move it from ImportResidue to LiftResidue.
		/// </summary>
		/// <returns>string containing any LIFT import residue found in ImportResidue</returns>
		private static string ExtractLIFTResidue(LcmCache cache, int hvo, int flidImportResidue,
			int flidLiftResidue)
		{
			Debug.Assert(flidLiftResidue != 0);
			ITsString tssImportResidue = cache.MainCacheAccessor.get_StringProp(hvo, flidImportResidue);
			string sImportResidue = tssImportResidue == null ? null : tssImportResidue.Text;
			if (String.IsNullOrEmpty(sImportResidue))
				return null;
			if (sImportResidue.Length < 13)
				return null;
			int idx = sImportResidue.IndexOf("<lift-residue");
			if (idx >= 0)
			{
				string sLiftResidue = sImportResidue.Substring(idx);
				int idx2 = sLiftResidue.IndexOf("</lift-residue>");
				if (idx2 >= 0)
				{
					idx2 += 15;
					if (sLiftResidue.Length > idx2)
						sLiftResidue = sLiftResidue.Substring(0, idx2);
				}
				int cch = sLiftResidue.Length;
				cache.MainCacheAccessor.set_UnicodeProp(hvo, flidImportResidue, sImportResidue.Remove(idx, cch));
				cache.MainCacheAccessor.set_UnicodeProp(hvo, flidLiftResidue, sLiftResidue);
				return sLiftResidue;
			}
			else
			{
				return null;
			}
		}

		private void StoreFieldAsResidue(ICmObject extensible, LiftField field)
		{
			XmlDocument xdResidue = FindOrCreateResidue(extensible);
			if (xdResidue != null)
			{
				string sXml = CreateXmlForField(field);
				InsertResidueContent(xdResidue, sXml);
			}
			else
			{
				Debug.WriteLine(String.Format("Need LiftResidue for {0}: <field type='{1}'>",
					extensible.GetType().Name, field.Type));
			}
		}

		private XmlDocument FindOrCreateResidue(ICmObject extensible)
		{
			// chaining if..else if instead of switch deals easier with matching superclasses.
			if (extensible is ILexEntry)
				return FindOrCreateResidue(extensible, null, LexEntryTags.kflidLiftResidue);
			else if (extensible is ILexSense)
				return FindOrCreateResidue(extensible, null, LexSenseTags.kflidLiftResidue);
			else if (extensible is ILexEtymology)
				return FindOrCreateResidue(extensible, null, LexEtymologyTags.kflidLiftResidue);
			else if (extensible is ILexExampleSentence)
				return FindOrCreateResidue(extensible, null, LexExampleSentenceTags.kflidLiftResidue);
			else if (extensible is ILexPronunciation)
				return FindOrCreateResidue(extensible, null, LexPronunciationTags.kflidLiftResidue);
			else if (extensible is ILexReference)
				return FindOrCreateResidue(extensible, null, LexReferenceTags.kflidLiftResidue);
			else if (extensible is IMoForm)
				return FindOrCreateResidue(extensible, null, MoFormTags.kflidLiftResidue);
			else if (extensible is IMoMorphSynAnalysis)
				return FindOrCreateResidue(extensible, null, MoMorphSynAnalysisTags.kflidLiftResidue);
			else
				return null;
		}

		/// <summary>
		///
		/// </summary>
		/// <param name="field">field with content safe XML</param>
		/// <returns>safe XML (with some XML structure)</returns>
		private string CreateXmlForField(LiftField field)
		{
			StringBuilder bldr = new StringBuilder();
			bldr.AppendFormat("<field type=\"{0}\"", field.Type);
			AppendXmlDateAttributes(bldr, field.DateCreated, field.DateModified);
			bldr.AppendLine(">");
			AppendXmlForMultiText(bldr, field.Content, "form");
			foreach (LiftTrait trait in field.Traits)
				bldr.Append(CreateXmlForTrait(trait));
			bldr.AppendLine("</field>");
			return bldr.ToString();
		}

		/// <summary>
		/// Append content to bldr
		/// </summary>
		/// <param name="bldr">safe XML</param>
		/// <param name="content">safe XML</param>
		/// <param name="tagXml"></param>
		private void AppendXmlForMultiText(StringBuilder bldr, LiftMultiText content, string tagXml)
		{
			if (content == null)
				return; // probably shouldn't happen in a fully functional system, but...
			foreach (string lang in content.Keys)
			{
				LiftString str = content[lang]; // safe XML
				bldr.AppendFormat("<{0} lang=\"{1}\"><text>", tagXml, lang);
				int idxPrev = 0;
				foreach (LiftSpan span in str.Spans)
				{
					if (idxPrev < span.Index)
						bldr.Append(str.Text.Substring(idxPrev, span.Index - idxPrev));
					// TODO: handle nested spans.
					bool fSpan = AppendSpanElementIfNeeded(bldr, span, lang);
					bldr.Append(str.Text.Substring(span.Index, span.Length));
					if (fSpan)
						bldr.Append("</span>");
					idxPrev = span.Index + span.Length;
				}
				if (idxPrev < str.Text.Length)
					bldr.Append(str.Text.Substring(idxPrev, str.Text.Length - idxPrev));
				bldr.AppendFormat("</text></{0}>", tagXml);
				bldr.AppendLine();
			}
		}

		/// <summary>
		/// Lift import may contain tabs, newlines, or characters that need escaping to be valid XML.
		/// newlines are converted to \u2028, tabs to spaces.
		/// </summary>
		/// <param name="input"></param>
		/// <returns></returns>
		internal static string ConvertToSafeFieldXmlContent(string input)
		{
			return XmlUtils.ConvertMultiparagraphToSafeXml(input.Replace('\t', ' '));
		}

		private bool AppendSpanElementIfNeeded(StringBuilder bldr, LiftSpan span, string lang)
		{
			bool fSpan = false;
			if (!String.IsNullOrEmpty(span.Class))
			{
				bldr.AppendFormat("<span class=\"{0}\"", XmlUtils.MakeSafeXmlAttribute(span.Class));
				fSpan = true;
			}
			if (!String.IsNullOrEmpty(span.LinkURL))
			{
				if (!fSpan)
				{
					bldr.Append("<span");
					fSpan = true;
				}
				bldr.AppendFormat(" href=\"{0}\"", XmlUtils.MakeSafeXmlAttribute(span.LinkURL));
			}
			if (!String.IsNullOrEmpty(span.Lang) && span.Lang != lang)
			{
				if (!fSpan)
				{
					bldr.Append("<span");
					fSpan = true;
				}
				bldr.AppendFormat(" lang=\"{0}\"", XmlUtils.MakeSafeXmlAttribute(span.Lang));
			}
			if (fSpan)
				bldr.Append(">");
			return fSpan;
		}

		private void StoreNoteAsResidue(ICmObject extensible, CmLiftNote note)
		{
			XmlDocument xdResidue = FindOrCreateResidue(extensible);
			if (xdResidue != null)
			{
				string sXml = CreateXmlForNote(note);
				InsertResidueContent(xdResidue, sXml);
			}
			else
			{
				Debug.WriteLine(String.Format("Need LiftResidue for {0}: <note type='{1}'>",
					extensible.GetType().Name, note.Type));
			}
		}

		private string CreateXmlForNote(CmLiftNote note)
		{
			StringBuilder bldr = new StringBuilder();
			bldr.Append("<note");
			if (!String.IsNullOrEmpty(note.Type))
				bldr.AppendFormat(" type=\"{0}\"", XmlUtils.MakeSafeXmlAttribute(note.Type));
			AppendXmlDateAttributes(bldr, note.DateCreated, note.DateModified);
			bldr.AppendLine(">");
			AppendXmlForMultiText(bldr, note.Content, "form");
			foreach (LiftField field in note.Fields)
				bldr.Append(CreateXmlForField(field));
			foreach (LiftTrait trait in note.Traits)
				bldr.Append(CreateXmlForTrait(trait));
			bldr.AppendLine("</note>");
			return bldr.ToString();
		}

		/// <summary>
		///
		/// </summary>
		/// <param name="trait">trait with parts NOT safe-XML</param>
		/// <returns>XML (text is safe XML)</returns>
		private string CreateXmlForTrait(LiftTrait trait)
		{
			StringBuilder bldr = new StringBuilder();
			bldr.AppendFormat("<trait name=\"{0}\" value=\"{1}\"",
				XmlUtils.MakeSafeXmlAttribute(trait.Name),
				XmlUtils.MakeSafeXmlAttribute(trait.Value));
			if (!String.IsNullOrEmpty(trait.Id))
				bldr.AppendFormat(" id=\"{0}\"", XmlUtils.MakeSafeXmlAttribute(trait.Id));
			if (trait.Annotations != null && trait.Annotations.Count > 0)
			{
				bldr.AppendLine(">");
				foreach (LiftAnnotation ann in trait.Annotations)
					bldr.Append(CreateXmlForAnnotation(ann));
				bldr.AppendLine("</trait>");
			}
			else
			{
				bldr.AppendLine("/>");
			}
			return bldr.ToString();
		}

		/// <summary>
		/// Return an XML string (content safe)
		/// </summary>
		/// <param name="ann">safe XML fields</param>
		/// <returns></returns>
		private string CreateXmlForAnnotation(LiftAnnotation ann)
		{
			StringBuilder bldr = new StringBuilder();
			bldr.AppendFormat("<annotation name=\"{0}\" value=\"{1}\"", ann.Name, ann.Value);
			if (!String.IsNullOrEmpty(ann.Who))
				bldr.AppendFormat(" who=\"{0}\"", ann.Who);
			DateTime when = ann.When;
			if (IsDateSet(when))
				bldr.AppendFormat(" when=\"{0}\"", when.ToUniversalTime().ToString(LiftDateTimeFormat));
			bldr.AppendLine(">");
			AppendXmlForMultiText(bldr, ann.Comment, "form");
			bldr.AppendLine("</annotation>");
			return bldr.ToString();
		}

		private string CreateXmlForPhonetic(CmLiftPhonetic phon)
		{
			StringBuilder bldr = new StringBuilder();
			bldr.Append("<pronunciation");
			AppendXmlDateAttributes(bldr, phon.DateCreated, phon.DateModified);
			bldr.AppendLine(">");
			AppendXmlForMultiText(bldr, phon.Form, "form");
			foreach (LiftUrlRef url in phon.Media)
				bldr.Append(CreateXmlForUrlRef(url, "media"));
			foreach (LiftField field in phon.Fields)
				bldr.Append(CreateXmlForField(field));
			foreach (LiftTrait trait in phon.Traits)
				bldr.Append(CreateXmlForTrait(trait));
			foreach (LiftAnnotation ann in phon.Annotations)
				bldr.Append(CreateXmlForAnnotation(ann));
			bldr.AppendLine("</pronunciation>");
			return bldr.ToString();
		}

		/// <summary>
		///
		/// </summary>
		/// <param name="bldr">string builder collecting safe XML</param>
		/// <param name="created"></param>
		/// <param name="modified"></param>
		private void AppendXmlDateAttributes(StringBuilder bldr, DateTime created, DateTime modified)
		{
			if (IsDateSet(created))
				bldr.AppendFormat(" dateCreated=\"{0}\"", ConvertToSafeFieldXmlContent(created.ToUniversalTime().ToString(LiftDateTimeFormat)));
			if (IsDateSet(modified))
				bldr.AppendFormat(" dateModified=\"{0}\"", ConvertToSafeFieldXmlContent(modified.ToUniversalTime().ToString(LiftDateTimeFormat)));
		}

		private string CreateXmlForUrlRef(LiftUrlRef url, string tag)
		{
			StringBuilder bldr = new StringBuilder();
			bldr.AppendFormat("<{0} href=\"{1}\">", tag, url.Url);
			bldr.AppendLine();
			AppendXmlForMultiText(bldr, url.Label, "form");
			bldr.AppendFormat("</{0}>", tag);
			bldr.AppendLine();
			return bldr.ToString();
		}

		private string CreateXmlForRelation(CmLiftRelation rel)
		{
			StringBuilder bldr = new StringBuilder();
			bldr.AppendFormat("<relation type=\"{0}\" ref=\"{1}\"", rel.Type, rel.Ref);
			if (rel.Order >= 0)
				bldr.AppendFormat(" order=\"{0}\"", rel.Order);
			AppendXmlDateAttributes(bldr, rel.DateCreated, rel.DateModified);
			bldr.AppendLine(">");
			AppendXmlForMultiText(bldr, rel.Usage, "usage");
			foreach (LiftField field in rel.Fields)
				bldr.Append(CreateXmlForField(field));
			foreach (LiftTrait trait in rel.Traits)
				bldr.Append(CreateXmlForTrait(trait));
			foreach (LiftAnnotation ann in rel.Annotations)
				bldr.Append(CreateXmlForAnnotation(ann));
			bldr.AppendLine("</relation>");
			return bldr.ToString();
		}

		private string CreateRelationResidue(CmLiftRelation rel)
		{
			if (rel.Usage != null || rel.Fields.Count > 0 || rel.Traits.Count > 0 ||
				rel.Annotations.Count > 0)
			{
				StringBuilder bldr = new StringBuilder();
				AppendXmlForMultiText(bldr, rel.Usage, "usage");
				foreach (LiftField field in rel.Fields)
					bldr.Append(CreateXmlForField(field));
				foreach (LiftTrait trait in rel.Traits)
					bldr.Append(CreateXmlForTrait(trait));
				foreach (LiftAnnotation ann in rel.Annotations)
					bldr.Append(CreateXmlForAnnotation(ann));
				return bldr.ToString();
			}
			else
			{
				return null;
			}
		}

		private void StoreTraitAsResidue(ICmObject extensible, LiftTrait trait)
		{
			XmlDocument xdResidue = FindOrCreateResidue(extensible);
			if (xdResidue != null)
			{
				string sXml = CreateXmlForTrait(trait);
				InsertResidueContent(xdResidue, sXml);
			}
			else
			{
				Debug.WriteLine(String.Format("Need LiftResidue for {0}: <trait name='{1}' value='{2}'>",
					extensible.GetType().Name, trait.Name, trait.Value));
			}
		}

		private void StoreResidue(ICmObject extensible, List<string> rgsResidue)
		{
			if (rgsResidue.Count == 0)
				return;
			XmlDocument xdResidue = FindOrCreateResidue(extensible);
			if (xdResidue != null)
			{
				foreach (string sXml in rgsResidue)
					InsertResidueContent(xdResidue, sXml);
			}
			else
			{
				Debug.WriteLine(String.Format("Need LiftResidue for {0}: {1}...",
					extensible.GetType().Name, rgsResidue[0]));
			}
		}

		private void StoreResidue(ICmObject extensible, string sResidueXml)
		{
			if (String.IsNullOrEmpty(sResidueXml))
				return;
			XmlDocument xdResidue = FindOrCreateResidue(extensible);
			if (xdResidue != null)
			{
				InsertResidueContent(xdResidue, sResidueXml);
			}
			else
			{
				Debug.WriteLine(String.Format("Need LiftResidue for {0}: {1}",
					extensible.GetType().Name, sResidueXml));
			}
		}

		private void StoreResidueFromVariant(ICmObject extensible, CmLiftVariant var)
		{
			XmlDocument xdResidue = FindOrCreateResidue(extensible);
			if (xdResidue != null)
			{
				// traits have already been handled.
				InsertResidueAttribute(xdResidue, "ref", var.Ref);
				StoreDatesInResidue(extensible, var);
				foreach (LiftAnnotation ann in var.Annotations)
				{
					string sXml = CreateXmlForAnnotation(ann);
					InsertResidueContent(xdResidue, sXml);
				}
				foreach (CmLiftPhonetic phon in var.Pronunciations)
				{
					string sXml = CreateXmlForPhonetic(phon);
					InsertResidueContent(xdResidue, sXml);
				}
				foreach (CmLiftRelation rel in var.Relations)
				{
					string sXml = CreateXmlForRelation(rel);
					InsertResidueContent(xdResidue, sXml);
				}
				if (!String.IsNullOrEmpty(var.RawXml) &&
					String.IsNullOrEmpty(var.Ref) &&
					var.Pronunciations.Count == 0 &&
					var.Relations.Count == 0)
				{
					XmlDocument xdoc = new XmlDocument();
					xdoc.PreserveWhitespace = true;
					xdoc.LoadXml(var.RawXml);
					string sRef = XmlUtils.GetOptionalAttributeValue(xdoc.FirstChild, "ref");
					InsertResidueAttribute(xdResidue, "ref", sRef);
					foreach (XmlNode node in xdoc.FirstChild.SelectNodes("pronunciation"))
						InsertResidueContent(xdResidue, node.OuterXml + Environment.NewLine);
					foreach (XmlNode node in xdoc.FirstChild.SelectNodes("relation"))
						InsertResidueContent(xdResidue, node.OuterXml + Environment.NewLine);
				}
			}
			else
			{
				Debug.WriteLine(String.Format("Need LiftResidue for {0}: <variant...>",
					extensible.GetType().Name));
			}
		}

		private void StoreEtymologyAsResidue(ICmObject extensible, CmLiftEtymology ety)
		{
			XmlDocument xdResidue = FindOrCreateResidue(extensible);
			if (xdResidue != null)
			{
				string sXml = CreateXmlForEtymology(ety);
				InsertResidueContent(xdResidue, sXml);
			}
			else
			{
				Debug.WriteLine(String.Format("Need LiftResidue for {0}: <etymology...>",
					extensible.GetType().Name));
			}
		}

		private string CreateXmlForEtymology(CmLiftEtymology ety)
		{
			StringBuilder bldr = new StringBuilder();
			bldr.AppendFormat("<etymology source=\"{0}\" type=\"{1}\"", ety.Source, ety.Type);
			AppendXmlDateAttributes(bldr, ety.DateCreated, ety.DateModified);
			bldr.AppendLine(">");
			Debug.Assert(ety.Form.Count < 2);
			AppendXmlForMultiText(bldr, ety.Form, "form");
			AppendXmlForMultiText(bldr, ety.Gloss, "gloss");
			foreach (LiftField field in ety.Fields)
				bldr.Append(CreateXmlForField(field));
			foreach (LiftTrait trait in ety.Traits)
				bldr.Append(CreateXmlForTrait(trait));
			foreach (LiftAnnotation ann in ety.Annotations)
				bldr.Append(CreateXmlForAnnotation(ann));
			bldr.AppendLine("</etymology>");
			return bldr.ToString();
		}

		private void StoreDatesInResidue(ICmObject extensible, LiftObject obj)
		{
			if (IsDateSet(obj.DateCreated) || IsDateSet(obj.DateModified))
			{
				XmlDocument xdResidue = FindOrCreateResidue(extensible);
				if (xdResidue != null)
				{
					InsertResidueDate(xdResidue, "dateCreated", obj.DateCreated);
					InsertResidueDate(xdResidue, "dateModified", obj.DateModified);
				}
				else
				{
					Debug.WriteLine(String.Format("Need LiftResidue for {0}: <etymology...>",
						extensible.GetType().Name));
				}
			}
		}

		private void InsertResidueAttribute(XmlDocument xdResidue, string sName, string sValue)
		{
			if (!String.IsNullOrEmpty(sValue))
			{
				XmlAttribute xa = xdResidue.FirstChild.Attributes[sName];
				if (xa == null)
				{
					xa = xdResidue.CreateAttribute(sName);
					xdResidue.FirstChild.Attributes.Append(xa);
				}
				xa.Value = sValue;
			}
		}

		private void InsertResidueDate(XmlDocument xdResidue, string sAttrName, DateTime dt)
		{
			if (IsDateSet(dt))
			{
				InsertResidueAttribute(xdResidue, sAttrName,
					dt.ToUniversalTime().ToString(LiftDateTimeFormat));
			}
		}

		private static void InsertResidueContent(XmlDocument xdResidue, string sXml)
		{
			XmlParserContext context = new XmlParserContext(null, null, null, XmlSpace.None);
			using (XmlReader reader = new XmlTextReader(sXml, XmlNodeType.Element, context))
			{
				XmlNode xn = xdResidue.ReadNode(reader);
				if (xn != null)
				{
					xdResidue.FirstChild.AppendChild(xn);
					xn = xdResidue.ReadNode(reader); // add trailing newline
					if (xn != null)
						xdResidue.FirstChild.AppendChild(xn);
				}
			}
		}

		public bool IsDateSet(DateTime dt)
		{
			return dt != default(DateTime) && dt != m_defaultDateTime;
		}

		private void StoreAnnotationsAndDatesInResidue(ICmObject extensible, LiftObject obj)
		{
			// unknown fields and traits have already been stored as residue.
			if (obj.Annotations.Count > 0 || IsDateSet(obj.DateCreated) || IsDateSet(obj.DateModified))
			{
				XmlDocument xdResidue = FindOrCreateResidue(extensible);
				if (xdResidue != null)
				{
					StoreDatesInResidue(extensible, obj);
					foreach (LiftAnnotation ann in obj.Annotations)
					{
						string sXml = CreateXmlForAnnotation(ann);
						InsertResidueContent(xdResidue, sXml);
					}
				}
				else
				{
					Debug.WriteLine(String.Format("Need LiftResidue for {0}: <{1}...>",
						extensible.GetType().Name), obj.XmlTag);
				}
			}
		}

		private void StoreExampleResidue(ICmObject extensible, CmLiftExample expl)
		{
			// unknown notes have already been stored as residue.
			if (expl.Fields.Count + expl.Traits.Count + expl.Annotations.Count == 0)
				return;
			XmlDocument xdResidue = FindOrCreateResidue(extensible);
			if (xdResidue != null)
			{
				StoreDatesInResidue(extensible, expl);
				foreach (LiftAnnotation ann in expl.Annotations)
				{
					string sXml = CreateXmlForAnnotation(ann);
					InsertResidueContent(xdResidue, sXml);
				}
			}
			else
			{
				Debug.WriteLine(String.Format("Need LiftResidue for {0}: <example...>",
					extensible.GetType().Name));
			}
		}
		#endregion // Storing LIFT import residue...

		#region Methods for processing LIFT header elements
		/// <summary>
		///
		/// </summary>
		/// <param name="sName"></param>
		/// <param name="lmtDesc">lmtDesc is safe-XML</param>
		/// <param name="clid"></param>
		/// <param name="possListGuid"></param>
		/// <returns></returns>
		private int FindOrCreateCustomField(string sName, LiftMultiText lmtDesc, int clid, out Guid possListGuid)
		{
			var sClass = m_cache.MetaDataCacheAccessor.GetClassName(clid);
			var sTag = String.Format("{0}-{1}", sClass, sName);
			var flid = 0;
			possListGuid = Guid.Empty;
			if (m_dictCustomFlid.TryGetValue(sTag, out flid))
			{
				m_CustomFieldNamesToPossibilityListGuids.TryGetValue(sTag, out possListGuid);
				return flid;
			}
			var sDesc = String.Empty; // safe-XML
			string sSpec = null; // safe-XML
			if (lmtDesc != null)
			{
				LiftString lstr; // safe-XML
				if (lmtDesc.TryGetValue("en", out lstr))
					sDesc = lstr.Text;
				if (lmtDesc.TryGetValue("qaa-x-spec", out lstr))
					sSpec = lstr.Text;
				if (String.IsNullOrEmpty(sSpec) && !String.IsNullOrEmpty(sDesc) && sDesc.StartsWith("Type=kcpt"))
				{
					sSpec = sDesc;
					sDesc = String.Empty;
				}
			}
			var type = CellarPropertyType.MultiUnicode;
			var wsSelector = WritingSystemServices.kwsAnalVerns;
			var clidDst = 0;
			if (!String.IsNullOrEmpty(sSpec))
			{
				string sDstCls;
				var rgsDef = sSpec.Split(new char[] { ';' }, StringSplitOptions.RemoveEmptyEntries);
				for (var i = 0; i < rgsDef.Length; i++)
				{
					var str = rgsDef[i].TrimStart(' ');
					rgsDef[i] = str;
				}
				type = GetCustomFieldType(rgsDef);
				if (type == CellarPropertyType.Nil)
					type = CellarPropertyType.MultiUnicode;
				wsSelector = GetCustomFieldWsSelector(rgsDef);
				clidDst = GetCustomFieldDstCls(rgsDef, out sDstCls);
				possListGuid = GetCustomFieldPossListGuid(rgsDef);
			}
			foreach (var fd in FieldDescription.FieldDescriptors(m_cache))
			{
				if (fd.Custom != 0 && fd.Name == sName && fd.Class == clid)
				{
					if (String.IsNullOrEmpty(sSpec))
					{
						// Fieldworks knows about a field with this label, but the file doesn't. Assume the project's definition of it is valid.
						m_dictCustomFlid.Add(sTag, fd.Id);
						possListGuid = fd.ListRootId;
						m_CustomFieldNamesToPossibilityListGuids.Add(sTag, possListGuid);
						return fd.Id;
					}
					else
					{
						// The project and the file both specify type information for this field. See whether they match (near enough).
						var fOk = CheckForCompatibleTypes(type, fd);
						if (!fOk)
						{
							// log error.
							return 0;
						}
						m_dictCustomFlid.Add(sTag, fd.Id);
						possListGuid = fd.ListRootId;
						m_CustomFieldNamesToPossibilityListGuids.Add(sTag, possListGuid);
						return fd.Id; // field with same label and type information exists already.
					}
				}
			}
			switch (type)
			{
				case CellarPropertyType.Boolean:
				case CellarPropertyType.Integer:
				case CellarPropertyType.Numeric:
				case CellarPropertyType.Float:
				case CellarPropertyType.Time:
				case CellarPropertyType.Guid:
				case CellarPropertyType.Image:
				case CellarPropertyType.GenDate:
				case CellarPropertyType.Binary:
				case CellarPropertyType.String:
					clidDst = -1;
					break;
				case CellarPropertyType.Unicode:
				case CellarPropertyType.MultiString:
				case CellarPropertyType.MultiUnicode:
					if (wsSelector == 0)
						wsSelector = WritingSystemServices.kwsAnalVerns;		// we need a WsSelector value!
					clidDst = -1;
					break;
				case CellarPropertyType.OwningAtomic:
				case CellarPropertyType.ReferenceAtomic:
				case CellarPropertyType.OwningCollection:
				case CellarPropertyType.ReferenceCollection:
				case CellarPropertyType.OwningSequence:
				case CellarPropertyType.ReferenceSequence:
					break;
				default:
					type = CellarPropertyType.MultiUnicode;
					if (wsSelector == 0)
						wsSelector = WritingSystemServices.kwsAnalVerns;
					clidDst = -1;
					break;
			}
			var fdNew = new FieldDescription(m_cache)
			{
				Type = type,
				Class = clid,
				Name = sName,
				Userlabel = sName,
				HelpString = XmlUtils.DecodeXml(sDesc),
				WsSelector = wsSelector,
				DstCls = clidDst,
				ListRootId = possListGuid
			};
			fdNew.UpdateCustomField();
			//Clear the data so that when the descriptions are next requested the up to date data is used
			FieldDescription.ClearDataAbout();
			m_dictCustomFlid.Add(sTag, fdNew.Id);
			m_CustomFieldNamesToPossibilityListGuids.Add(sTag, possListGuid);
			m_rgnewCustomFields.Add(fdNew);
			return fdNew.Id;
		}

		private Guid GetCustomFieldPossListGuid(IEnumerable<string> rgsDef)
		{
			string possListName = null;
			var guidToReturn = Guid.Empty;
			foreach (string sDef in rgsDef)
			{
				if (sDef.StartsWith("range="))
				{
					possListName = sDef.Substring(6);
					if (m_rangeNamesToPossibilityListGuids.TryGetValue(possListName, out guidToReturn))
						return guidToReturn;
				}
			}
			return guidToReturn;
		}

		private static bool CheckForCompatibleTypes(CellarPropertyType type, FieldDescription fd)
		{
			if (fd.Type == type)
				return true;
			if (fd.Type == CellarPropertyType.Binary && type == CellarPropertyType.Image)
				return true;
			if (fd.Type == CellarPropertyType.Image && type == CellarPropertyType.Binary)
				return true;
			if (fd.Type == CellarPropertyType.OwningCollection && type == CellarPropertyType.OwningSequence)
				return true;
			if (fd.Type == CellarPropertyType.OwningSequence && type == CellarPropertyType.OwningCollection)
				return true;
			if (fd.Type == CellarPropertyType.ReferenceCollection && type == CellarPropertyType.ReferenceSequence)
				return true;
			if (fd.Type == CellarPropertyType.ReferenceSequence && type == CellarPropertyType.ReferenceCollection)
				return true;
			return false;
		}

		// arguments may be safeXML; but since they must be slightly decorated versions of enumeration members it doesn't matter.
		private CellarPropertyType GetCustomFieldType(string[] rgsDef)
		{
			foreach (string sDef in rgsDef)
			{
				if (sDef.StartsWith("Type="))
				{
					var sValue = sDef.Substring(5);
					if (sValue.StartsWith("kcpt"))
						sValue = sValue.Substring(4);
					return (CellarPropertyType)Enum.Parse(typeof(CellarPropertyType), sValue, true);
				}
			}
			return CellarPropertyType.Nil;
		}

		private int GetCustomFieldWsSelector(string[] rgsDef)
		{
			foreach (string sDef in rgsDef)
			{
				if (sDef.StartsWith("WsSelector="))
				{
					string sValue = sDef.Substring(11);
					// Do NOT use WritingSystemServices.GetMagicWsIdFromName...that's a different set of names (LT-12275)
					int ws = GetLiftExportMagicWsIdFromName(sValue);
					if (ws == 0)
						ws = GetWsFromStr(sValue);
					return ws;
				}
			}
			return 0;
		}

		/// <summary>
		/// Method MUST be consistent with LiftExporter.GetLiftExportMagicWsNameFromId.
		/// Change only with great care...this affects how we can import existing LIFT files.
		/// </summary>
		/// <param name="name"></param>
		/// <returns></returns>
		private int GetLiftExportMagicWsIdFromName(string name)
		{
			switch (name)
			{
				case "kwsAnal":
					return WritingSystemServices.kwsAnal;
				case "kwsVern":
					return WritingSystemServices.kwsVern;
				case "kwsAnals":
					return WritingSystemServices.kwsAnals;
				case "kwsVerns":
					return WritingSystemServices.kwsVerns;
				case "kwsAnalVerns":
					return WritingSystemServices.kwsAnalVerns;
				case "kwsVernAnals":
					return WritingSystemServices.kwsVernAnals;
			}
			return 0;
		}

		private int GetCustomFieldDstCls(string[] rgsDef, out string sValue)
		{
			sValue = null;
			foreach (string sDef in rgsDef)
			{
				if (sDef.StartsWith("DstCls="))
				{
					sValue = sDef.Substring(7);
					return (int)m_cache.MetaDataCacheAccessor.GetClassId(sValue);
				}
			}
			return 0;
		}

		/// <summary>
		///
		/// </summary>
		/// <param name="id"></param>
		/// <param name="guidAttr"></param>
		/// <param name="parent"></param>
		/// <param name="description">safe-XML</param>
		/// <param name="label">safe-XML</param>
		/// <param name="abbrev">safe-XML</param>
		private void ProcessAnthroItem(string id, string guidAttr, string parent,
			LiftMultiText description, LiftMultiText label, LiftMultiText abbrev)
		{
			ICmPossibility poss = FindExistingPossibility(id, guidAttr, label, abbrev, m_dictAnthroCode,
				m_cache.LangProject.AnthroListOA);
			if (poss == null)
			{
				int hvo = FindAbbevOrLabelInDict (abbrev, label, m_dictAnthroCode);
				if (hvo <= 0) {
					ICmObject caiParent = null;
					if (!String.IsNullOrEmpty (parent) && m_dictAnthroCode.ContainsKey (parent))
						caiParent = m_dictAnthroCode [parent];
					else
						caiParent = m_cache.LangProject.AnthroListOA;
					ICmAnthroItem cai = CreateNewCmAnthroItem (guidAttr, caiParent);
					SetNewPossibilityAttributes (id, description, label, abbrev, cai);
					m_dictAnthroCode [id] = cai;
					m_rgnewAnthroCode.Add (cai);
				}
			}
		}

		/// <summary>
		///
		/// </summary>
		/// <param name="abbrev">safe-XML</param>
		/// <param name="label">safe-XML</param>
		/// <param name="dict">dictionary from non-safe-XML abbreviations and labels to possibilities</param>
		/// <returns></returns>
		private static int FindAbbevOrLabelInDict(LiftMultiText abbrev, LiftMultiText label,
			Dictionary<string, ICmPossibility> dict)
		{
			var normalizer = CustomIcu.GetIcuNormalizer(FwNormalizationMode.knmNFD);
			if (abbrev != null && abbrev.Keys != null)
			{
				foreach (string key in abbrev.Keys)
				{
					var text = normalizer.Normalize(XmlUtils.DecodeXml(abbrev[key].Text));
					if (dict.ContainsKey(text))
						return dict[text].Hvo;
				}
			}
			if (label != null && label.Keys != null)
			{
				foreach (string key in label.Keys)
				{
					var text = normalizer.Normalize(XmlUtils.DecodeXml(label[key].Text));
					if (dict.ContainsKey(text))
						return dict[text].Hvo;
				}
			}
			return 0;
		}

		/// <summary>
		///
		/// </summary>
		/// <param name="id"></param>
		/// <param name="guidAttr"></param>
		/// <param name="parent"></param>
		/// <param name="description">safe-XmL</param>
		/// <param name="label">safe-XML</param>
		/// <param name="abbrev">safe-XML</param>
		private void ProcessSemanticDomain(string id, string guidAttr, string parent,
			LiftMultiText description, LiftMultiText label, LiftMultiText abbrev)
		{
			ICmPossibility poss = GetPossibilityForGuidIfExisting(id, guidAttr, m_dictSemDom);
			if (poss == null)
			{
				ICmObject csdParent = null;
				if (!String.IsNullOrEmpty(parent) && m_dictSemDom.ContainsKey(parent))
					csdParent = m_dictSemDom[parent];
				else
					csdParent = m_cache.LangProject.SemanticDomainListOA;
				ICmSemanticDomain csd = CreateNewCmSemanticDomain(guidAttr, csdParent);
				SetNewPossibilityAttributes(id, description, label, abbrev, csd);
				m_dictSemDom[id] = csd;
				m_rgnewSemDom.Add(csd);
			}
		}

		/// <summary>
		///
		/// </summary>
		/// <param name="id"></param>
		/// <param name="guidAttr"></param>
		/// <param name="parent"></param>
		/// <param name="description">safe-XML</param>
		/// <param name="label">safe-XML</param>
		/// <param name="abbrev">safe-XML</param>
		/// <param name="dict"></param>
		/// <param name="rgNew"></param>
		/// <param name="list"></param>
		private void ProcessPossibility(string id, string guidAttr, string parent,
			LiftMultiText description, LiftMultiText label, LiftMultiText abbrev,
			Dictionary<string, ICmPossibility> dict, List<ICmPossibility> rgNew, ICmPossibilityList list, bool isCustom = false)
		{
			ICmPossibility poss = FindExistingPossibility(id, guidAttr, label, abbrev, dict, list);
			if (poss == null)
			{
				ICmObject possParent = null;
				if (!String.IsNullOrEmpty(parent) && dict.ContainsKey(parent))
					possParent = dict[parent];
				else
					possParent = list;
				poss = isCustom ? CreateNewCustomPossibility(guidAttr, possParent) : CreateNewCmPossibility(guidAttr, possParent);
				SetNewPossibilityAttributes(id, description, label, abbrev, poss);
				dict[id] = poss;
				rgNew.Add(poss);
			}
		}

		/// <summary>
		/// To Process Publications
		/// </summary>
		/// <param name="id"></param>
		/// <param name="guidAttr"></param>
		/// <param name="parent"></param>
		/// <param name="description">safe-XML</param>
		/// <param name="label">safe-XML</param>
		/// <param name="abbrev">safe-XML</param>
		/// <param name="dict"></param>
		/// <param name="rgNew"></param>
		/// <param name="list"></param>
		/// <param name="isCustom"></param>
		private void ProcessPossibilityPublications(string id, string guidAttr, string parent,
			LiftMultiText description, LiftMultiText label, LiftMultiText abbrev,
			Dictionary<string, ICmPossibility> dict, List<ICmPossibility> rgNew, ICmPossibilityList list, bool isCustom = false)
		{
			ICmPossibility poss = FindExistingPossibility(id, guidAttr, label, abbrev, dict, list);
			if (poss == null)
			{
				ICmObject possParent = null;
				if (!String.IsNullOrEmpty(parent) && dict.ContainsKey(parent))
					possParent = dict[parent];
				else
					possParent = list;
				poss = isCustom ? CreateNewCustomPossibility(guidAttr, possParent) : CreateNewCmPossibility(guidAttr, possParent);
				SetNewPossibilityAttributes(id, description, label, abbrev, poss);
				dict[id] = poss;
				rgNew.Add(poss);
			}
			else
			{
				SetNewPossibilityAttributes(id, description, label, abbrev, poss);
			}
		}

		/// <summary>
		///
		/// </summary>
		/// <param name="id"></param>
		/// <param name="guidAttr"></param>
		/// <param name="parent"></param>
		/// <param name="description">safe-XML</param>
		/// <param name="label">safe-XML</param>
		/// <param name="abbrev">safe-XML</param>
		/// <param name="dict"></param>
		/// <param name="rgNew"></param>
		/// <param name="list"></param>
		private void ProcessLocation(string id, string guidAttr, string parent,
			LiftMultiText description, LiftMultiText label, LiftMultiText abbrev)
		{
			var dict = m_dictLocation;
			var list = m_cache.LangProject.LocationsOA;
			var poss = (ICmLocation)FindExistingPossibility(id, guidAttr, label, abbrev, dict, list);
			if (poss == null)
			{
				var cmLocationFactory = m_cache.ServiceLocator.GetInstance<ICmLocationFactory>();
				ICmObject possParent = null;
				if (!String.IsNullOrEmpty(parent) && dict.ContainsKey(parent))
				{
					var parentLocation = (ICmLocation)dict[parent];
					if (!String.IsNullOrEmpty(guidAttr))
					{
						Guid guid = (Guid)m_gconv.ConvertFrom(guidAttr);
						poss = cmLocationFactory.Create(guid, parentLocation);
					}
					else
					{
						poss = cmLocationFactory.Create();
						parentLocation.SubPossibilitiesOS.Add(poss);
					}
				}
				else
				{
					if (!String.IsNullOrEmpty(guidAttr))
					{
						Guid guid = (Guid)m_gconv.ConvertFrom(guidAttr);
						poss = cmLocationFactory.Create(guid, list);
					}
					else
					{
						poss = cmLocationFactory.Create();
						list.PossibilitiesOS.Add(poss);
					}
				}
				SetNewPossibilityAttributes(id, description, label, abbrev, poss);
				dict[id] = poss;
				m_rgnewLocation.Add(poss);
			}
		}

		/// <summary>
		///
		/// </summary>
		/// <param name="id"></param>
		/// <param name="guidAttr"></param>
		/// <param name="parent"></param>
		/// <param name="description">safe-XML</param>
		/// <param name="label">safe-XML</param>
		/// <param name="abbrev">safe-XML</param>
		/// <param name="dict"></param>
		/// <param name="rgNew"></param>
		/// <param name="list"></param>
		private void ProcessPerson(string id, string guidAttr, string parent,
			LiftMultiText description, LiftMultiText label, LiftMultiText abbrev,
			Dictionary<string, ICmPossibility> dict, List<ICmPossibility> rgNew, ICmPossibilityList list)
		{
			var person = FindExistingPossibility(id, guidAttr, label, abbrev, dict, list);
			if (person == null)
			{
				ICmObject possParent = null;
				if (!String.IsNullOrEmpty(parent) && dict.ContainsKey(parent))
					possParent = dict[parent];
				else
					possParent = list;
				person = CreateNewCmPerson(guidAttr, possParent);
				SetNewPossibilityAttributes(id, description, label, abbrev, person);
				dict[id] = person;
				rgNew.Add(person);
			}
		}

		/// <summary>
		///
		/// </summary>
		/// <param name="id"></param>
		/// <param name="description">safe-XML</param>
		/// <param name="label">safe-XML</param>
		/// <param name="abbrev">safe-XML</param>
		/// <param name="poss"></param>
		private void SetNewPossibilityAttributes(string id, LiftMultiText description, LiftMultiText label,
			LiftMultiText abbrev, ICmPossibility poss)
		{
			IgnoreNewWs();
			if (label.Count > 0)
				MergeInMultiUnicode(poss.Name, CmPossibilityTags.kflidName, label, poss.Guid);
			else
				poss.Name.AnalysisDefaultWritingSystem = TsStringUtils.MakeString(id, m_cache.DefaultAnalWs);
			MergeInMultiUnicode(poss.Abbreviation, CmPossibilityTags.kflidAbbreviation, abbrev, poss.Guid);
			MergeInMultiString(poss.Description, CmPossibilityTags.kflidDescription, description, poss.Guid);
		}

		/// <summary>
		///
		/// </summary>
		/// <param name="id"></param>
		/// <param name="guidAttr"></param>
		/// <param name="parent"></param>
		/// <param name="description">safe-XML</param>
		/// <param name="label">safe-XML</param>
		/// <param name="abbrev">safe-XML</param>
		private void ProcessPartOfSpeech(string id, string guidAttr, string parent,
			LiftMultiText description, LiftMultiText label, LiftMultiText abbrev)
		{
			ICmPossibility poss = FindExistingPossibility(id, guidAttr, label, abbrev, m_dictPos,
				m_cache.LangProject.PartsOfSpeechOA);
			if (poss == null)
			{
				ICmObject posParent = null;
				if (!String.IsNullOrEmpty(parent) && m_dictPos.ContainsKey(parent))
					posParent = m_dictPos[parent];
				else
					posParent = m_cache.LangProject.PartsOfSpeechOA;
				IPartOfSpeech pos = CreateNewPartOfSpeech(guidAttr, posParent);
				SetNewPossibilityAttributes(id, description, label, abbrev, pos);
				m_dictPos[id] = pos;
				// Try to find this in the category catalog list, so we can add in more information.
				EticCategory cat = FindMatchingEticCategory(label);
				if (cat != null)
					AddEticCategoryInfo(cat, pos);
				m_rgnewPos.Add(pos);
			}
		}

		private void AddEticCategoryInfo(EticCategory cat, IPartOfSpeech pos)
		{
			if (cat != null)
			{
				pos.CatalogSourceId = cat.Id;
				foreach (string lang in cat.MultilingualName.Keys)
				{
					int ws = GetWsFromStr(lang);
					if (ws > 0)
					{
						ITsString tssName = pos.Name.get_String(ws);
						if (tssName == null || tssName.Length == 0)
							pos.Name.set_String(ws, cat.MultilingualName[lang]);
					}
				}
				foreach (string lang in cat.MultilingualAbbrev.Keys)
				{
					int ws = GetWsFromStr(lang);
					if (ws > 0)
					{
						ITsString tssAbbrev = pos.Abbreviation.get_String(ws);
						if (tssAbbrev == null || tssAbbrev.Length == 0)
							pos.Abbreviation.set_String(ws, cat.MultilingualAbbrev[lang]);
					}
				}
				foreach (string lang in cat.MultilingualDesc.Keys)
				{
					int ws = GetWsFromStr(lang);
					if (ws > 0)
					{
						ITsString tss = pos.Description.get_String(ws);
						if (tss == null || tss.Length == 0)
							pos.Description.set_String(ws, cat.MultilingualDesc[lang]);
					}
				}
			}
		}

		/// <summary>
		///
		/// </summary>
		/// <param name="label">safe-XML</param>
		/// <returns></returns>
		private EticCategory FindMatchingEticCategory(LiftMultiText label)
		{
			foreach (EticCategory cat in m_rgcats)
			{
				int cMatch = 0;
				int cDiffer = 0;
				foreach (string lang in label.Keys)
				{
					string sName = label[lang].Text;
					string sCatName;
					if (cat.MultilingualName.TryGetValue(lang, out sCatName))
					{
						if (sName.ToLowerInvariant() == sCatName.ToLowerInvariant())
							++cMatch;
						else
							++cDiffer;
					}
				}
				if (cMatch > 0 && cDiffer == 0)
					return cat;
			}
			return null;
		}

		/// <summary>
		///
		/// </summary>
		/// <param name="id"></param>
		/// <param name="guidAttr"></param>
		/// <param name="parent"></param>
		/// <param name="description">safe-XML</param>
		/// <param name="label">safe-XML</param>
		/// <param name="abbrev">safe-XML</param>
		private void ProcessMorphType(string id, string guidAttr, string parent, LiftMultiText description, LiftMultiText label, LiftMultiText abbrev)
		{
			ICmPossibility poss = FindExistingPossibility(id, guidAttr, label, abbrev, m_dictMmt,
				m_cache.LangProject.LexDbOA.MorphTypesOA);
			if (poss == null)
			{
				ICmObject mmtParent = null;
				if (!String.IsNullOrEmpty(parent) && m_dictPos.ContainsKey(parent))
					mmtParent = m_dictMmt[parent];
				else
					mmtParent = m_cache.LangProject.LexDbOA.MorphTypesOA;
				IMoMorphType mmt = CreateNewMoMorphType(guidAttr, mmtParent);
				SetNewPossibilityAttributes(id, description, label, abbrev, mmt);
				m_dictMmt[id] = mmt;
				m_rgnewMmt.Add(mmt);
			}
		}

		/// <summary>
		///
		/// </summary>
		/// <param name="id"></param>
		/// <param name="guidAttr"></param>
		/// <param name="label">safe-XML</param>
		/// <param name="abbrev">safe-XML</param>
		/// <param name="dict"></param>
		/// <param name="list"></param>
		/// <returns></returns>
		private ICmPossibility FindExistingPossibility(string id, string guidAttr, LiftMultiText label,
			LiftMultiText abbrev, Dictionary<string, ICmPossibility> dict, ICmPossibilityList list)
		{
			ICmPossibility poss = GetPossibilityForGuidIfExisting(id, guidAttr, dict);
			if (poss == null)
			{
				poss = FindMatchingPossibility(list.PossibilitiesOS, label, abbrev);
				if (poss != null)
				{
					dict[id] = poss;
					// For the moment, we won't try to update any information in
					// existing items.
				}
			}
			return poss;
		}

		private ICmPossibility GetPossibilityForGuidIfExisting(string id, string guidAttr, Dictionary<string, ICmPossibility> dict)
		{
			ICmPossibility poss = null;
			if (!String.IsNullOrEmpty(guidAttr))
			{
				Guid guid = (Guid)m_gconv.ConvertFrom(guidAttr);
				ICmObject cmo = GetObjectForGuid(guid);
				if (cmo != null && cmo is ICmPossibility)
				{
					poss = cmo as ICmPossibility;
					dict[id] = poss;
					// For the moment, we won't try to update any information in
					// existing items.
				}
			}
			return poss;
		}

		/// <summary>
		///
		/// </summary>
		/// <param name="possibilities"></param>
		/// <param name="label">safe-XML</param>
		/// <param name="abbrev">safe-XML</param>
		/// <returns></returns>
		ICmPossibility FindMatchingPossibility(ILcmOwningSequence<ICmPossibility> possibilities,
			LiftMultiText label, LiftMultiText abbrev)
		{
			IgnoreNewWs();
			foreach (ICmPossibility item in possibilities)
			{
				if (HasMatchingUnicodeAlternative(item.Name, label) &&
					HasMatchingUnicodeAlternative(item.Abbreviation, abbrev))
				{
					return item;
				}
				ICmPossibility poss = FindMatchingPossibility(item.SubPossibilitiesOS, label, abbrev);
				if (poss != null)
					return poss;
			}
			return null;
		}

		/// <summary>
		///
		/// </summary>
		/// <param name="tsm"></param>
		/// <param name="text">safe-XML</param>
		/// <returns></returns>
		private bool HasMatchingUnicodeAlternative(ITsMultiString tsm, LiftMultiText text)
		{
			if (text != null && text.Keys != null)
			{
				foreach (string key in text.Keys)
				{
					int wsHvo = GetWsFromLiftLang(key);
					string sValue = CustomIcu.GetIcuNormalizer(FwNormalizationMode.knmNFD)
						.Normalize(XmlUtils.DecodeXml(text[key].Text));
					ITsString tssAlt = tsm.get_String(wsHvo);
					if (String.IsNullOrEmpty(sValue) || (tssAlt == null || tssAlt.Length == 0))
						continue;
					if (sValue.ToLowerInvariant() == tssAlt.Text.ToLowerInvariant())
						return true;
				}
				return false;
			}
			return true;		// no data at all -- assume match (!!??)
		}


		/// <summary>
		///
		/// </summary>
		/// <param name="id"></param>
		/// <param name="label">safe-XML</param>
		/// <param name="abbrev">safe-XML</param>
		/// <param name="description">safe-XML</param>
		private void VerifyOrCreateWritingSystem(string id, LiftMultiText label,
			LiftMultiText abbrev, LiftMultiText description)
		{
			// This finds or creates a writing system for the given key.
			int handle = GetWsFromLiftLang(id);
			Debug.Assert(handle >= 1);
			CoreWritingSystemDefinition ws = GetExistingWritingSystem(handle);

			if (m_msImport != MergeStyle.MsKeepOld || string.IsNullOrEmpty(ws.Abbreviation))
			{
				if (abbrev.Count > 0)
					ws.Abbreviation = XmlUtils.DecodeXml(abbrev.FirstValue.Value.Text);
			}
			LanguageSubtag languageSubtag = ws.Language;
			if (m_msImport != MergeStyle.MsKeepOld || string.IsNullOrEmpty(languageSubtag.Name))
			{
				if (label.Count > 0)
					ws.Language = new LanguageSubtag(languageSubtag, XmlUtils.DecodeXml(label.FirstValue.Value.Text));
			}
		}

		/// <summary>
		///
		/// </summary>
		/// <param name="range"></param>
		/// <param name="id"></param>
		/// <param name="guidAttr"></param>
		/// <param name="parent"></param>
		/// <param name="description">safe-XML</param>
		/// <param name="label">safe-XML</param>
		/// <param name="abbrev">safe-XML</param>
		private void ProcessSlotDefinition(string range, string id, string guidAttr, string parent,
			LiftMultiText description, LiftMultiText label, LiftMultiText abbrev)
		{
			IgnoreNewWs();
			int idx = range.IndexOf("-slot");
			if (idx < 0)
				idx = range.IndexOf("-Slots");
			string sOwner = range.Substring(0, idx);
			ICmPossibility owner = null;
			if (m_dictPos.ContainsKey(sOwner))
				owner = m_dictPos[sOwner];
			if (owner == null)
				owner = FindMatchingPossibility(sOwner.ToLowerInvariant(),
					m_cache.LangProject.PartsOfSpeechOA.PossibilitiesOS, m_dictPos);
			if (owner == null)
				return;
			IPartOfSpeech posOwner = owner as IPartOfSpeech;
			IMoInflAffixSlot slot = null;
			foreach (IMoInflAffixSlot slotT in posOwner.AffixSlotsOC)
			{

				if (HasMatchingUnicodeAlternative(slotT.Name, label))
				{
					slot = slotT;
					break;
				}
			}
			if (slot == null)
			{
				slot = CreateNewMoInflAffixSlot();
				posOwner.AffixSlotsOC.Add(slot);
				AddNewWsToAnalysis();
				MergeInMultiUnicode(slot.Name, MoInflAffixSlotTags.kflidName, label, slot.Guid);
				MergeInMultiString(slot.Description, MoInflAffixSlotTags.kflidDescription, description, slot.Guid);
				m_rgnewSlots.Add(slot);
				// TODO: How to handle "Optional" field.
			}
		}

		/// <summary>
		///
		/// </summary>
		/// <param name="range"></param>
		/// <param name="id"></param>
		/// <param name="guidAttr"></param>
		/// <param name="sParent"></param>
		/// <param name="description">safe-XML</param>
		/// <param name="label">safe-XML</param>
		/// <param name="abbrev">safe-XML</param>
		private void ProcessInflectionClassDefinition(string range, string id, string guidAttr,
			string sParent, LiftMultiText description, LiftMultiText label, LiftMultiText abbrev)
		{
			IgnoreNewWs();
			int idx = range.IndexOf("-infl-class");
			if (idx < 0)
				idx = range.IndexOf("-InflClasses");
			string sOwner = range.Substring(0, idx);
			ICmPossibility owner = null;
			if (m_dictPos.ContainsKey(sOwner))
				owner = m_dictPos[sOwner];
			if (owner == null)
				owner = FindMatchingPossibility(sOwner.ToLowerInvariant(),
					m_cache.LangProject.PartsOfSpeechOA.PossibilitiesOS, m_dictPos);
			if (owner == null)
				return;
			Dictionary<string, IMoInflClass> dictSlots = null;
			if (!m_dictDictSlots.TryGetValue(sOwner, out dictSlots))
			{
				dictSlots = new Dictionary<string, IMoInflClass>();
				m_dictDictSlots[sOwner] = dictSlots;
			}
			IPartOfSpeech posOwner = owner as IPartOfSpeech;
			IMoInflClass infl = null;
			IMoInflClass inflParent = null;
			if (!String.IsNullOrEmpty(sParent))
			{
				if (dictSlots.ContainsKey(sParent))
				{
					inflParent = dictSlots[sParent];
				}
				else
				{
					inflParent = FindMatchingInflectionClass(sParent, posOwner.InflectionClassesOC, dictSlots);
				}
			}
			else
			{
				foreach (IMoInflClass inflT in posOwner.InflectionClassesOC)
				{
					if (HasMatchingUnicodeAlternative(inflT.Name, label) &&
						HasMatchingUnicodeAlternative(inflT.Abbreviation, abbrev))
					{
						infl = inflT;
						break;
					}
				}
			}
			if (infl == null)
			{
				infl = CreateNewMoInflClass();
				if (inflParent == null)
					posOwner.InflectionClassesOC.Add(infl);
				else
					inflParent.SubclassesOC.Add(infl);
				MergeInMultiUnicode(infl.Abbreviation, MoInflClassTags.kflidAbbreviation, abbrev, infl.Guid);
				MergeInMultiUnicode(infl.Name, MoInflClassTags.kflidName, label, infl.Guid);
				MergeInMultiString(infl.Description, MoInflClassTags.kflidDescription, description, infl.Guid);
				dictSlots[id] = infl;
			}
		}

		private IMoInflClass FindMatchingInflectionClass(string parent,
			ILcmOwningCollection<IMoInflClass> collection, Dictionary<string, IMoInflClass> dict)
		{
			foreach (IMoInflClass infl in collection)
			{
				if (HasMatchingUnicodeAlternative(parent.ToLowerInvariant(), infl.Abbreviation, infl.Name))
				{
					dict[parent] = infl;
					return infl;
				}
				IMoInflClass inflT = FindMatchingInflectionClass(parent, infl.SubclassesOC, dict);
				if (inflT != null)
					return inflT;
			}
			return null;
		}

		private ICmPossibility FindMatchingPossibility(string sVal,
			ILcmOwningSequence<ICmPossibility> possibilities,
			Dictionary<string, ICmPossibility> dict)
		{
			foreach (ICmPossibility poss in possibilities)
			{
				if (HasMatchingUnicodeAlternative(sVal, poss.Abbreviation, poss.Name))
				{
					if (dict != null)
						dict.Add(sVal, poss);
					return poss;
				}
				ICmPossibility possT = FindMatchingPossibility(sVal, poss.SubPossibilitiesOS, dict);
				if (possT != null)
					return possT;
			}
			return null;
		}
		#endregion // Methods for processing LIFT header elements

		#region Process Guids in import data

		/// <summary>
		/// As sense elements often don't have explict guid attributes in LIFT files,
		/// the parser generates new Guid values for them.  We want to always use the
		/// old guid values if we can, so we try to get a guid from the id value if
		/// one exists.  (In fact, WeSay appears to put out only the guid as the id
		/// value.  Flex puts out the default analysis gloss followed by the guid.)
		/// See LT-8840 for what happens if we depend of the Guid value provided by
		/// the parser.
		/// </summary>
		/// <param name="info"></param>
		/// <returns></returns>
		private CmLiftSense CreateLiftSenseFromInfo(Extensible info, LiftObject owner)
		{
			Guid guidInfo = info.Guid;
			info.Guid = Guid.Empty;
			Guid guid = GetGuidInExtensible(info);
			if (guid == Guid.Empty)
				guid = guidInfo;
			return new CmLiftSense(info, guid, owner, this);
		}

		private GuidConverter GuidConv
		{
			get { return m_gconv; }
		}

		private Guid GetGuidInExtensible(Extensible info)
		{
			if (info.Guid == Guid.Empty)
			{
				string sGuid = FindGuidInString(info.Id);
				if (!String.IsNullOrEmpty(sGuid))
					return (Guid)GuidConv.ConvertFrom(sGuid);
				else
					return Guid.NewGuid();
			}
			else
			{
				return info.Guid;
			}
		}

		/// <summary>
		/// Find and return a substring like "ebc06013-3cf8-4091-9436-35aa2c4ffc34", or null
		/// if nothing looks like a guid.
		/// </summary>
		/// <param name="sId"></param>
		/// <returns></returns>
		private string FindGuidInString(string sId)
		{
			if (String.IsNullOrEmpty(sId) || sId.Length < 36)
				return null;
			Match matchGuid = m_regexGuid.Match(sId);
			if (matchGuid.Success)
				return sId.Substring(matchGuid.Index, matchGuid.Length);
			else
				return null;
		}

		private ICmObject GetObjectFromTargetIdString(string targetId)
		{
			if (m_mapIdObject.ContainsKey(targetId))
				return m_mapIdObject[targetId];
			string sGuid = FindGuidInString(targetId);
			if (!String.IsNullOrEmpty(sGuid))
			{
				Guid guidTarget = (Guid)GuidConv.ConvertFrom(sGuid);
				return GetObjectForGuid(guidTarget);
			}
			return null;
		}
		#endregion // Process Guids in import data

		#region Methods for handling relation links
		/// <summary>
		/// This isn't really a relation link, but it needs to be done at the end of the
		/// import process.
		/// </summary>
		private void ProcessMissingFeatStrucTypeFeatures()
		{
			foreach (IFsFeatStrucType type in m_mapFeatStrucTypeMissingFeatureAbbrs.Keys)
			{
				List<string> rgsAbbr = m_mapFeatStrucTypeMissingFeatureAbbrs[type];
				List<IFsFeatDefn> rgfeat = new List<IFsFeatDefn>(rgsAbbr.Count);
				for (int i = 0; i < rgsAbbr.Count; ++i)
				{
					string sAbbr = rgsAbbr[i];
					IFsFeatDefn feat;
					if (m_mapIdFeatDefn.TryGetValue(sAbbr, out feat))
						rgfeat.Add(feat);
					else
						break;
				}
				if (rgfeat.Count == rgsAbbr.Count)
				{
					type.FeaturesRS.Clear();
					for (int i = 0; i < rgfeat.Count; ++i)
						type.FeaturesRS.Add(rgfeat[i]);
				}
			}
			m_mapFeatStrucTypeMissingFeatureAbbrs.Clear();
		}

		/// <summary>
		/// After all the entries (and senses) have been imported, then the relations among
		/// them can be set since all the target ids can be resolved.
		/// This is also an opportunity to delete unwanted objects if we're keeping only
		/// the imported data.
		/// </summary>
		public void ProcessPendingRelations(IProgress progress)
		{
			var lexRefRepo = m_cache.ServiceLocator.GetInstance<ILexReferenceRepository>();
			var lexEntryRefRepo = m_cache.ServiceLocator.GetInstance<ILexEntryRefRepository>();
			// Collection of the lexical references from before the import. Items remaining here may be removed for MergeStyle.MsKeepOnlyNew
			var originalLexEntryRefs = new List<ILexEntryRef>(lexEntryRefRepo.AllInstances());
			var originalLexRefs = new List<ILexReference>(lexRefRepo.AllInstances());
			// relationMap is used to group collection relations from the lift file into a structure useful for creating
			// correct LexRefType and LexReference objects in our model.
			// The key is the relationType string(eg. Synonym), The value is a list of groups of references.
			//		in detail, the value holds a pair(tuple) containing a set of the ids(hvos) involved in the group
			//		and a set of all the PendingRelation objects which have those ids.
			var relationMap = new Dictionary<string, List<Tuple<HashSet<int>, HashSet<PendingRelation>>>>();
			if (m_mapFeatStrucTypeMissingFeatureAbbrs.Count > 0)
				ProcessMissingFeatStrucTypeFeatures();
			if (m_rgPendingRelation.Count > 0)
			{
				progress.Message = String.Format(LexTextControls.ksProcessingRelationLinks, m_rgPendingRelation.Count);
				progress.Position = 0;
				progress.Minimum = 0;
				progress.Maximum = m_rgPendingRelation.Count;
				// First pass, ignore "minorentry" and "subentry", since those should be
				// installed by "main".  (The first two are backreferences to the third.)
				// Also ignore reverse tree relation references on the first pass.
				// Also collect more information about collection type relations without
				// storing anything in the database yet.
				m_rgPendingTreeTargets.Clear();
				for (var i = 0; i < m_rgPendingRelation.Count;)
				{
					var relation = CollectRelationMembers(i);
					if (relation == null || relation.Count == 0)
					{
						++i;
					}
					else
					{
						i += relation.Count;
						ProcessRelation(originalLexRefs, relation, relationMap);
					}
					progress.Position = i;
				}
			}

			StorePendingCollectionRelations(originalLexRefs, progress, relationMap);
			StorePendingTreeRelations(originalLexRefs, progress);
			StorePendingLexEntryRefs(originalLexEntryRefs, progress);
			// We can now store residue everywhere since any bogus relations have been added
			// to residue.
			progress.Message = LexTextControls.ksWritingAccumulatedResidue;
			WriteAccumulatedResidue();

			// If we're keeping only the imported data, erase any unused entries or senses.
			if (m_msImport == MergeStyle.MsKeepOnlyNew)
			{
				progress.Message = LexTextControls.ksDeletingUnwantedEntries;
				GatherUnwantedObjects(originalLexEntryRefs, originalLexRefs); //at this point any LexRefs which haven't been matched are dead.
				DeleteUnwantedObjects();
			}
			// Now that the relations have all been set, it's safe to set the entry
			// modification times.
			progress.Message = LexTextControls.ksSettingEntryModificationTimes;
			foreach (PendingModifyTime pmt in m_rgPendingModifyTimes)
				pmt.SetModifyTime();
		}

		private void StorePendingLexEntryRefs(List<ILexEntryRef> originalLexEntryRefs, IProgress progress)
		{
			// Now create the LexEntryRef type links.
			if (m_rgPendingLexEntryRefs.Count > 0)
			{
				progress.Message = String.Format(LexTextControls.ksStoringLexicalEntryReferences,
					m_rgPendingLexEntryRefs.Count);
				progress.Position = 0;
				progress.Minimum = 0;
				progress.Maximum = m_rgPendingLexEntryRefs.Count;
				var missingRefs = new List<Tuple<string, string>>();
				for (int i = 0; i < m_rgPendingLexEntryRefs.Count; )
				{
					List<PendingLexEntryRef> rgRefs = CollectLexEntryRefMembers(i);
					if (rgRefs == null || rgRefs.Count == 0)
					{
						++i;
					}
					else
					{
						ProcessLexEntryRefs(originalLexEntryRefs, rgRefs, missingRefs);
						i += rgRefs.Count;
					}
					progress.Position = i;
				}
				if (missingRefs.Count > 0)
				{
					var bldr = new StringBuilder();
					bldr.Append("The LIFT file you are importing has entries with 'Component' or 'Variant' references to lexical entries that ");
					bldr.Append("were not exported to the LIFT file. ");
					bldr.Append("Therefore, these references (components or variants) will be excluded from this import.  ");
					bldr.AppendLine();
					bldr.AppendLine();
					bldr.Append("This is probably a result of doing a Filtered Lexicon LIFT export. Instead, a Full Lexicon LIFT export should been done ");
					bldr.Append("to correct this problem, followed by another LIFT import.  ");
					bldr.AppendLine();
					bldr.AppendLine();
					bldr.AppendLine("Form\t\t:\tReference ID");
					foreach (var missingRefPair in missingRefs)
					{
						bldr.AppendFormat("{0}\t\t:\t{1}{2}", missingRefPair.Item1, missingRefPair.Item2, Environment.NewLine);
					}
					bldr.AppendLine();
					MessageBoxUtils.Show(bldr.ToString(), LexTextControls.ksProblemImporting,
									MessageBoxButtons.OK, MessageBoxIcon.Warning);
				}
			}
		}

		private void StorePendingTreeRelations(List<ILexReference> originalLexRefs, IProgress progress)
		{
			if (m_rgPendingTreeTargets.Count > 0)
			{
				progress.Message = String.Format(LexTextControls.ksSettingTreeRelationLinks,
					m_rgPendingTreeTargets.Count);
				progress.Position = 0;
				progress.Minimum = 0;
				progress.Maximum = m_rgPendingTreeTargets.Count;
				for (var i = 0; i < m_rgPendingTreeTargets.Count; ++i)
				{
					ProcessRemainingTreeRelation(originalLexRefs, m_rgPendingTreeTargets[i]);
					progress.Position = i + 1;
				}
			}
		}

		private void ProcessRemainingTreeRelation(List<ILexReference> originalLexRefs, PendingRelation rel)
		{
			Debug.Assert(rel.Target != null);
			if (rel.Target == null)
				return;
			var sType = rel.RelationType;
			Debug.Assert(!rel.IsSequence);
			var lrt = FindLexRefType(sType, false);
			if (!TreeRelationAlreadyExists(originalLexRefs, lrt, rel))
			{
				ILexReference lr = CreateNewLexReference();
				lrt.MembersOC.Add(lr);
				if(ObjectIsFirstInRelation(rel.RelationType, lrt))
				{
					lr.TargetsRS.Add(rel.Target);
					lr.TargetsRS.Add(rel.CmObject);
				}
				else
				{
					lr.TargetsRS.Add(rel.CmObject);
					lr.TargetsRS.Add(rel.Target);
				}
				StoreRelationResidue(lr, rel);
			}
		}

		private void ProcessRelation(List<ILexReference> originalLexRefs, List<PendingRelation> rgRelation,
			Dictionary<string, List<Tuple<HashSet<int>, HashSet<PendingRelation>>>> uniqueRelations)
		{
			if (rgRelation == null || rgRelation.Count == 0 || rgRelation[0] == null)
				return;
			switch (rgRelation[0].RelationType)
			{
				case "main":
				case "minorentry":
				case "subentry":
				case "_component-lexeme":
					// These should never get this far...
					Debug.Assert(rgRelation[0].RelationType == "Something else...");
					break;
				default:
					StoreLexReference(originalLexRefs, rgRelation, uniqueRelations);
					break;
			}
		}

		/// <summary>
		/// This method will process the m_rgPendingLexEntryRefs and put all the ones which belong
		/// in the same LexEntryRef into a collection. It will return when it encounters an item which
		/// belongs in a different LexEntryRef.
		/// </summary>
		/// <param name="i">The index into m_rgPendingLexEntryRefs from which to start building the like item collection</param>
		/// <returns>A List containing all the PendingLexEntryRefs which belong in the same LexEntryRef</returns>
		private List<PendingLexEntryRef> CollectLexEntryRefMembers(int i)
		{
			if (i < 0 || i >= m_rgPendingLexEntryRefs.Count)
				return null;
			List<PendingLexEntryRef> rgRefs = new List<PendingLexEntryRef>();
			PendingLexEntryRef prev = null;
			int hvo = m_rgPendingLexEntryRefs[i].ObjectHvo;
			string sEntryType = m_rgPendingLexEntryRefs[i].EntryType;
			string sMinorEntryCondition = m_rgPendingLexEntryRefs[i].MinorEntryCondition;
			DateTime dateCreated = m_rgPendingLexEntryRefs[i].DateCreated;
			DateTime dateModified = m_rgPendingLexEntryRefs[i].DateModified;
			//string sResidue = m_rgPendingLexEntryRefs[i].Residue; // cs 219
			while (i < m_rgPendingLexEntryRefs.Count)
			{
				PendingLexEntryRef pend = m_rgPendingLexEntryRefs[i];
				// If the object, entry type (in an old LIFT file), or minor entry condition
				// (in an old LIFT file) has changed, we're into another LexEntryRef.
				if (pend.ObjectHvo != hvo || pend.EntryType != sEntryType ||
					pend.MinorEntryCondition != sMinorEntryCondition ||
					pend.DateCreated != dateCreated || pend.DateModified != dateModified)
				{
					break;
				}
				// The end of the components of a LexEntryRef may be marked only by a sudden
				// drop in the order value (which starts at 0 and increments by 1 steadily, or
				// is set to -1 when there's only one).
				if (prev != null && pend.Order < prev.Order)
					break;
				//If we have a different set of traits from the previous relation we belong in a new ref.
				if(prev != null)
				{
					if(prev.ComplexFormTypes.Count != pend.ComplexFormTypes.Count)
					{
						break;
					}
					if (!prev.ComplexFormTypes.ContainsCollection(pend.ComplexFormTypes))
						break;
				}
				pend.Target = GetObjectFromTargetIdString(m_rgPendingLexEntryRefs[i].TargetId);
				rgRefs.Add(pend);
				prev = pend;
				++i;
			}
			return rgRefs;
		}

		/// <summary>
		/// A LexEntryRef is matched if it is has same type, summary and hideMinorEntry value
		/// and if the collections all intersect.
		/// </summary>
		/// <param name="ler"></param>
		/// <param name="refType"></param>
		/// <param name="complexEntryTypes"></param>
		/// <param name="variantEntryTypes"></param>
		/// <param name="summary">safe-XML</param>
		/// <param name="componentLexemes"></param>
		/// <param name="primaryLexemes"></param>
		/// <returns></returns>
		private bool MatchLexEntryRef(ILexEntryRef ler, int refType, List<ILexEntryType> complexEntryTypes,
			List<ILexEntryType> variantEntryTypes, LiftMultiText summary,
			List<ICmObject> componentLexemes, List<ICmObject> primaryLexemes)
		{
			if (ler.RefType != refType)
				return false;
			AddNewWsToAnalysis();
			if (summary != null && !MatchMultiString(ler.Summary, summary))
				return false;
			if ((complexEntryTypes.Count() != 0 || ler.ComplexEntryTypesRS.Count != 0)
				&& complexEntryTypes.Intersect(ler.ComplexEntryTypesRS).Count() == 0)
			{
				return false;
			}
			if ((variantEntryTypes.Count() != 0 || ler.VariantEntryTypesRS.Count != 0)
				&& variantEntryTypes.Intersect(ler.VariantEntryTypesRS).Count() == 0)
			{
				return false;
			}
			if ((componentLexemes.Count() != 0 || ler.ComponentLexemesRS.Count != 0)
				&& componentLexemes.Intersect(ler.ComponentLexemesRS).Count() == 0)
			{
				return false;
			}
			if ((primaryLexemes.Count() != 0 || ler.PrimaryLexemesRS.Count != 0)
				&& primaryLexemes.Intersect(ler.PrimaryLexemesRS).Count() == 0)
			{
				return false;
			}
			return true;
		}

		private void ProcessLexEntryRefs(List<ILexEntryRef> originalRefs, List<PendingLexEntryRef> rgRefs, List<Tuple<string, string>> missingRefs)
		{
			if (rgRefs.Count == 0)
				return;
			ILexEntry le = null;
			ICmObject target = null;
			//"main" is no longer used as a relationType in LIFT export but is handled here in case
			//an older LIFT file is being imported.
			//The current LIFT export will have rgRefs[0].RelationType == "_component-lexeme"
			if (rgRefs.Count == 1 && rgRefs[0].RelationType == "main")
			{
				target = rgRefs[0].CmObject;
				string sRef = rgRefs[0].TargetId;
				ICmObject cmo;
				if (!String.IsNullOrEmpty(sRef) && m_mapIdObject.TryGetValue(sRef, out cmo))
				{
					Debug.Assert(cmo is ILexEntry);
					le = cmo as ILexEntry;
				}
				else
				{
					// log error message about invalid link in <relation type="main" ref="...">.
					InvalidRelation bad = new InvalidRelation(rgRefs[0], m_cache, this);
					if (!m_rgInvalidRelation.Contains(bad))
						m_rgInvalidRelation.Add(bad);
				}
			}
			else
			{
				Debug.Assert(rgRefs[0].CmObject is ILexEntry);
				le = rgRefs[0].CmObject as ILexEntry;
			}
			if (le == null)
				return;
			// Adjust HideMinorEntry for using old LIFT file.
			if (rgRefs[0].HideMinorEntry == 0 && rgRefs[0].ExcludeAsHeadword)
				rgRefs[0].HideMinorEntry = 1;
			// See if we can find a matching variant that already exists.
			var complexEntryTypes = new List<ILexEntryType>();
			var variantEntryTypes = new List<ILexEntryType>();
			int refType = DetermineLexEntryTypes(rgRefs, complexEntryTypes, variantEntryTypes);
			var componentLexemes = new List<ICmObject>();
			var primaryLexemes = new List<ICmObject>();
			for (int i = 0; i < rgRefs.Count; ++i)
			{
				PendingLexEntryRef pend = rgRefs[i];
				//This is handling the historical LIFT export data where relationType was "main"
				if (pend.RelationType == "main" && i == 0 && target != null)
				{
					componentLexemes.Add(target);
					primaryLexemes.Add(target);
				}
				else if (pend.Target != null)
					// pend.RelationType == "_component-lexeme" is now the default and there should be a non-null Target, however
					//when the LIFT file was produced by a partial export vs. full export the compontent/variant contained
					//in the lexEntry might be referencing another lexEntry that was not part of the export. In this case Target will be null.
				{
					componentLexemes.Add(pend.Target);
					//With compontents, for example a compound word, often one of the components is considered the
					//primary lexeme and the others are not.
					if (pend.IsPrimary || pend.RelationType == "main")
						primaryLexemes.Add(pend.Target);
				}
				else if (!string.IsNullOrEmpty(pend.TargetId))
					// pend.Target == null
					//If there is a partial LIFT export "Filtered Lexicon LIFT 0.13 XML" we can encounter a LexEntryRef that has a null target.
					//For example if the word 'unbelieving' has Components un- believe -ing then these three components will be included
					//in the <entry> 'unbelieving' as relations <relation type="_component-lexeme".../> when doing a LIFT export.
					//However, for a partial export where'un-' 'believe' and '-ing' are not included in the export, the reference	s to these found in
					//the lexEntry 'unbelieving' are invalid and therefore pend.Target will be null for each of these.
					//Therefore they are removed from this lexEntry on importing the LIFT file.
					//We should however warn the user that this data is not being imported and that they should do a FULL export to ensure this data
					//is imported correctly.
					// One exception: a LexEntryRef with NO components is exported as a relation with an EMPTY ref attribute.
					// This is not a problem, it just represents incomplete original data.
				{
					var form = "<empty form>";
					if (rgRefs[0].LexemeForm != null)
					{
						form = XmlUtils.DecodeXml(rgRefs[0].LexemeForm.FirstValue.Value.Text);
					}
					missingRefs.Add(new Tuple<string, string>(form, pend.TargetId));
				}
			}
			if (complexEntryTypes.Count == 0 && variantEntryTypes.Count == 0 && rgRefs[0].RelationType == "BaseForm" && componentLexemes.Count == 1
				&& primaryLexemes.Count == 0)
			{
				// A BaseForm relation from WeSay, with none of our lexical relation stuff implemented.
				// The baseform should be considered primary.
				primaryLexemes.Add(componentLexemes[0]);
				complexEntryTypes.Add(FindOrCreateComplexFormType("BaseForm"));
				refType = LexEntryRefTags.krtComplexForm;
			}
			ILexEntryRef ler = null;
			LiftMultiText summary = null;
			if (rgRefs[0].Summary != null)
				summary = rgRefs[0].Summary.Content;
			foreach (var candidate in le.EntryRefsOS)
			{
				if (MatchLexEntryRef(candidate, refType, complexEntryTypes, variantEntryTypes,
					summary, componentLexemes, primaryLexemes))
				{
					ler = ler ?? candidate;
					originalRefs.Remove(candidate);
				}
			}

			if (ler == null)
			{
				// no match, make a new one with the required properties.
				ler = CreateNewLexEntryRef();

				le.EntryRefsOS.Add(ler);
				ler.RefType = refType;
				foreach (var item in complexEntryTypes)
					ler.ComplexEntryTypesRS.Add(item);
				foreach (var item in variantEntryTypes)
					ler.VariantEntryTypesRS.Add(item);
				try
				{
					foreach (var item in componentLexemes)
						ler.ComponentLexemesRS.Add(item);
					foreach (var item in primaryLexemes)
						ler.PrimaryLexemesRS.Add(item);
				}
				catch (Exception error)
				{
					var bldr = new StringBuilder();
					bldr.Append("Something went wrong while FieldWorks was attempting to import the LIFT file.");
					bldr.AppendLine();
					bldr.Append(error.Message);
					bldr.AppendLine();
					if (rgRefs[0].LexemeForm != null)
					{
						bldr.AppendFormat("CmLiftEntry.LexicalForm is:    {0}", XmlUtils.DecodeXml(rgRefs[0].LexemeForm.FirstValue.Value.Text));
						bldr.AppendLine();
						bldr.AppendFormat("RelationType is:     {0}", rgRefs[0].RelationType);
					}
					MessageBox.Show(bldr.ToString(), LexTextControls.ksProblemImporting,
									MessageBoxButtons.OK, MessageBoxIcon.Warning);
				}

				ler.HideMinorEntry = rgRefs[0].HideMinorEntry;
				AddNewWsToAnalysis();
				if (summary != null)
					MergeInMultiString(ler.Summary, LexEntryRefTags.kflidSummary, summary, ler.Guid);
			}
			else // Adjust collection contents if necessary
			{
				AdjustCollectionContents(complexEntryTypes, ler.ComplexEntryTypesRS, ler);
				AdjustCollectionContents(variantEntryTypes, ler.VariantEntryTypesRS, ler);
				AdjustCollectionContents(componentLexemes, ler.ComponentLexemesRS, ler);
				AdjustCollectionContents(primaryLexemes, ler.PrimaryLexemesRS, ler);
			}

			// Create an empty sense if a complex form came in without a sense.  See LT-9153.
			if (le.SensesOS.Count == 0 &&
				(ler.ComplexEntryTypesRS.Count > 0 || ler.PrimaryLexemesRS.Count > 0))
			{
				bool fNeedNewId;
				CreateNewLexSense(Guid.Empty, le, out fNeedNewId);
				EnsureValidMSAsForSenses(le);
			}
		}

		/// <summary>
		/// This method will set the contents of the given ReferenceSequence(second param) to the union with the list (first param)
		/// </summary>
		/// <typeparam name="T"></typeparam>
		/// <param name="complexEntryTypes"></param>
		/// <param name="referenceCollection"></param>
		/// <param name="lexEntryRef"></param>
		private void AdjustCollectionContents<T>(List<T> complexEntryTypes, ILcmReferenceSequence<T> referenceCollection, ILexEntryRef lexEntryRef) where T : class, ICmObject
		{
			AdjustCollectionContents(complexEntryTypes, referenceCollection,
									 lexEntryRef.VariantEntryTypesRS.Count == 0 ? LexTextControls.ksComplexFormType : LexTextControls.ksVariantType, lexEntryRef.Owner);
		}
		private void AdjustCollectionContents<T>(List<T> complexEntryTypes, ILcmReferenceSequence<T> referenceCollection, ILexReference lexEntryRef) where T : class, ICmObject
		{
			AdjustCollectionContents(complexEntryTypes, referenceCollection,
									 lexEntryRef.TypeAbbreviation(m_cache.DefaultVernWs, lexEntryRef), referenceCollection.First());
		}

		private void AdjustCollectionContents<T>(List<T> complexEntryTypes, ILcmReferenceSequence<T> referenceCollection, string typeName, ICmObject owner) where T : class, ICmObject
		{
			if (referenceCollection.Count != complexEntryTypes.Count)
			{
				//add an error message for intersecting sets which do not have a subset-superset relationship.
				if(!complexEntryTypes.ContainsCollection(referenceCollection) && !referenceCollection.ContainsCollection(complexEntryTypes))
				{
					foreach (var newItem in complexEntryTypes)
					{
						var col = new CombinedCollection(owner, m_cache, this)
									{
										TypeName = typeName,
										BadValue = newItem is ILexEntry
													? ((ILexEntry) newItem).HeadWordForWs(m_cache.DefaultVernWs).Text
													: ((ILexSense) newItem).OwnerOfClass<ILexEntry>().HeadWordForWs(m_cache.DefaultVernWs).Text
									};
						m_combinedCollections.Add(col);
					}
				}
				referenceCollection.Replace(0, referenceCollection.Count, complexEntryTypes);
			}
		}

		/// <summary>
		/// Answer the RefType that a LexEntryRef should have to match the input. Set the two lists to the required
		/// values for the indicated properties.
		/// </summary>
		private int DetermineLexEntryTypes(List<PendingLexEntryRef> rgRefs, List<ILexEntryType> complexEntryTypes,
			List<ILexEntryType> variantEntryTypes)
		{
			int result = LexEntryRefTags.krtVariant; // default in an unitialized LexEntryRef
			complexEntryTypes.Clear();
			variantEntryTypes.Clear();
			List<string> rgsComplexFormTypes = rgRefs[0].ComplexFormTypes;
			List<string> rgsVariantTypes = rgRefs[0].VariantTypes;
			string sOldEntryType = rgRefs[0].EntryType;
			string sOldCondition = rgRefs[0].MinorEntryCondition;
			// A trait name complex-form-type or variant-type can be used with an unspecified value
			// to indicate that this reference type is either complex or variant (more options in future).
			if (rgsComplexFormTypes.Count > 0)
				result = LexEntryRefTags.krtComplexForm;
			if (rgsVariantTypes.Count > 0)
				result = LexEntryRefTags.krtVariant;
			if (rgsComplexFormTypes.Count > 0 && rgsVariantTypes.Count > 0)
			{
				// TODO: Complain to the user that he's getting ahead of the programmers!
			}
			foreach (string sType in rgsComplexFormTypes)
			{
				if (!String.IsNullOrEmpty(sType))
				{
					ILexEntryType let = FindOrCreateComplexFormType(sType);
					complexEntryTypes.Add(let);
				}
			}
			foreach (string sType in rgsVariantTypes)
			{
				if (!String.IsNullOrEmpty(sType))
				{
					ILexEntryType let = FindOrCreateVariantType(sType);
					variantEntryTypes.Add(let);
				}
			}
			if (complexEntryTypes.Count == 0 &&
				variantEntryTypes.Count == 0 &&
				!String.IsNullOrEmpty(sOldEntryType))
			{
				if (sOldEntryType == "Derivation")
					sOldEntryType = "Derivative";
				else if (sOldEntryType == "derivation")
					sOldEntryType = "derivative";
				else if (sOldEntryType == "Inflectional Variant")
					sOldEntryType = "Irregularly Inflected Form";
				else if (sOldEntryType == "inflectional variant")
					sOldEntryType = "irregularly inflected form";
				ILexEntryType letComplex = FindComplexFormType(sOldEntryType);
				if (letComplex == null)
				{
					ILexEntryType letVar = FindVariantType(sOldEntryType);
					if (letVar == null && sOldEntryType.ToLowerInvariant() != "main entry")
					{
						if (String.IsNullOrEmpty(sOldCondition))
						{
							letComplex = FindOrCreateComplexFormType(sOldEntryType);
							complexEntryTypes.Add(letComplex);
							result = LexEntryRefTags.krtComplexForm;
						}
						else
						{
							letVar = FindOrCreateVariantType(sOldEntryType);
						}
					}
					if (letVar != null)
					{
						if (String.IsNullOrEmpty(sOldCondition))
						{
							variantEntryTypes.Add(letVar);
						}
						else
						{
							ILexEntryType subtype = null;
							foreach (ICmPossibility poss in letVar.SubPossibilitiesOS)
							{
								ILexEntryType sub = poss as ILexEntryType;
								if (sub != null &&
									(sub.Name.AnalysisDefaultWritingSystem.Text == sOldCondition ||
									 sub.Abbreviation.AnalysisDefaultWritingSystem.Text == sOldCondition ||
									 sub.ReverseAbbr.AnalysisDefaultWritingSystem.Text == sOldCondition))
								{
									subtype = sub;
									break;
								}
							}
							if (subtype == null)
							{
								subtype = CreateNewLexEntryType();
								letVar.SubPossibilitiesOS.Add(subtype);
								subtype.Name.set_String(m_cache.DefaultAnalWs, sOldCondition);
								subtype.Abbreviation.set_String(m_cache.DefaultAnalWs, sOldCondition);
								subtype.ReverseAbbr.set_String(m_cache.DefaultAnalWs, sOldCondition);
								m_rgnewVariantType.Add(subtype);
							}
							variantEntryTypes.Add(subtype);
						}
						result = LexEntryRefTags.krtVariant;
					}
				}
				else
				{
					complexEntryTypes.Add(letComplex);
					result = LexEntryRefTags.krtComplexForm;
				}
			}
			return result;
		}

		private void StoreLexReference(ICollection<ILexReference> refsAsYetUnmatched, List<PendingRelation> rgRelation,
			Dictionary<string, List<Tuple<HashSet<int>, HashSet<PendingRelation>>>> uniqueRelations)
		{
			// Store any relations with unrecognized targets in residue, removing them from the
			// list.
			for (var i = 0; i < rgRelation.Count; ++i)
			{
				if (rgRelation[i].Target == null)
				{
					StoreResidue(rgRelation[i].CmObject, rgRelation[i].AsResidueString());
				}
			}
			for (var i = rgRelation.Count - 1; i >= 0; --i)
			{
				if (rgRelation[i].Target == null)
				{
					rgRelation.RemoveAt(i);
				}
			}
			if (rgRelation.Count == 0)
			{
				return;
			}
			// Store the list of relations appropriately as a LexReference with a proper type.
			var sType = rgRelation[0].RelationType;
			var lrt = FindLexRefType(sType, rgRelation[0].Order != -1);
			switch (lrt.MappingType)
			{
				case (int)LexRefTypeTags.MappingTypes.kmtEntryAsymmetricPair:
				case (int)LexRefTypeTags.MappingTypes.kmtEntryOrSenseAsymmetricPair:
				case (int)LexRefTypeTags.MappingTypes.kmtSenseAsymmetricPair:
					StoreAsymmetricPairRelations(refsAsYetUnmatched, lrt, rgRelation, ObjectIsFirstInRelation(rgRelation[0].RelationType, lrt));
					break;
				case (int)LexRefTypeTags.MappingTypes.kmtEntryPair:
				case (int)LexRefTypeTags.MappingTypes.kmtEntryOrSensePair:
				case (int)LexRefTypeTags.MappingTypes.kmtSensePair:
					StorePairRelations(refsAsYetUnmatched, lrt, rgRelation);
					break;
				case (int)LexRefTypeTags.MappingTypes.kmtEntryCollection:
				case (int)LexRefTypeTags.MappingTypes.kmtEntryOrSenseCollection:
				case (int)LexRefTypeTags.MappingTypes.kmtSenseCollection:
					CollapseCollectionRelationPairs(rgRelation, uniqueRelations);
					break;
				case (int)LexRefTypeTags.MappingTypes.kmtEntryOrSenseSequence:
				case (int)LexRefTypeTags.MappingTypes.kmtEntrySequence:
				case (int)LexRefTypeTags.MappingTypes.kmtSenseSequence:
					StoreSequenceRelation(refsAsYetUnmatched, lrt, rgRelation);
					break;
				case (int)LexRefTypeTags.MappingTypes.kmtEntryOrSenseTree:
				case (int)LexRefTypeTags.MappingTypes.kmtEntryTree:
				case (int)LexRefTypeTags.MappingTypes.kmtSenseTree:
				case (int)LexRefTypeTags.MappingTypes.kmtEntryOrSenseUnidirectional:
				case (int)LexRefTypeTags.MappingTypes.kmtEntryUnidirectional:
				case (int)LexRefTypeTags.MappingTypes.kmtSenseUnidirectional:
					StoreTreeRelation(refsAsYetUnmatched, lrt, rgRelation);
					break;
			}
		}

		private ILexRefType FindLexRefType(string sType, bool isSequence)
		{
			ICmPossibility lrt = GetRefTypeByNameOrReverseName(sType);
			if(lrt == null)
			{

				//This section is for older lift files where the type information wasn't written out well enough, new files will work better, but this is better than nothing
				var newLexRefType = CreateNewLexRefType(null, null);
				m_cache.LangProject.LexDbOA.ReferencesOA.PossibilitiesOS.Add(newLexRefType);
				newLexRefType.Name.set_String(m_cache.DefaultAnalWs, sType);
				if((String.IsNullOrEmpty(m_sLiftProducer) || m_sLiftProducer.StartsWith("WeSay")) && (sType == "BaseForm"))
				{
					newLexRefType.Abbreviation.set_String(m_cache.DefaultAnalWs, "base");
					newLexRefType.ReverseName.set_String(m_cache.DefaultAnalWs, "Derived Forms");
					newLexRefType.ReverseAbbreviation.set_String(m_cache.DefaultAnalWs, "deriv");
					newLexRefType.MappingType = (int)LexRefTypeTags.MappingTypes.kmtEntryTree;
				}
				else
				{
					newLexRefType.Abbreviation.set_String(m_cache.DefaultAnalWs, sType);
					if(isSequence)
						newLexRefType.MappingType = (int)LexRefTypeTags.MappingTypes.kmtEntryOrSenseSequence;
					else
						newLexRefType.MappingType = (int)LexRefTypeTags.MappingTypes.kmtEntryOrSenseCollection;
				}
				lrt = newLexRefType;
				m_dictLexRefTypes.Add(sType.ToLowerInvariant(), lrt);
				m_rgnewLexRefTypes.Add(lrt);
			}
			return lrt as ILexRefType;
		}

		private ICmPossibility GetRefTypeByNameOrReverseName(string sType)
		{
			ICmPossibility lrt;
			var normalizedTypeName = sType.Normalize().ToLowerInvariant();
			m_dictLexRefTypes.TryGetValue(normalizedTypeName, out lrt);
			if(lrt == null)
			{
				foreach(ILexRefType dictLexRefType in m_dictLexRefTypes.Values)
				{
					if(dictLexRefType.ReverseName.OccursInAnyAlternative(normalizedTypeName))
					{
						return dictLexRefType;
					}
				}
				lrt = m_rgnewLexRefTypes.FirstOrDefault(x => x.Name.OccursInAnyAlternative(normalizedTypeName)) ??
						m_rgnewLexRefTypes.FirstOrDefault(x => ((ILexRefType)x).ReverseName.OccursInAnyAlternative(normalizedTypeName));
			}
			return lrt;
		}

		private void StoreAsymmetricPairRelations(ICollection<ILexReference> refsAsYetUnmatched, ILexRefType lrt, List<PendingRelation> rgRelation, bool fFirst)
		{
			for(var i = 0; i < rgRelation.Count; ++i)
			{
				Debug.Assert(rgRelation[i].Target != null);
				if (rgRelation[i].Target == null)
					continue;
				if(AsymmetricPairRelationAlreadyExists(refsAsYetUnmatched, lrt, rgRelation[i], fFirst))
					continue;
				var lr = CreateNewLexReference();
				lrt.MembersOC.Add(lr);
				if(fFirst)
				{
					lr.TargetsRS.Add(rgRelation[i].CmObject);
					lr.TargetsRS.Add(rgRelation[i].Target);
				}
				else
				{
					lr.TargetsRS.Add(rgRelation[i].Target);
					lr.TargetsRS.Add(rgRelation[i].CmObject);
				}
				StoreRelationResidue(lr, rgRelation[i]);
			}
		}

		/// <summary>
		/// Attempts to match the pending relation to any existing AsymmetricPairRelations
		/// </summary>
		/// <param name="refsAsYetUnmatched">The collection of relations prior to the lift merge operation, matched relations will be removed</param>
		/// <param name="lrt"></param>
		/// <param name="rel"></param>
		/// <param name="isFirst"></param>
		/// <returns></returns>
		private static bool AsymmetricPairRelationAlreadyExists(ICollection<ILexReference> refsAsYetUnmatched, ILexRefType lrt, PendingRelation rel,
			bool isFirst)
		{
			var hvo1 = rel.CmObject == null ? 0 : rel.ObjectHvo;
			var hvo2 = rel.TargetHvo;
			foreach(var lr in lrt.MembersOC)
			{
				if(lr.TargetsRS.Count != 2)
					continue; // SHOULD NEVER HAPPEN!!
				var hvoA = lr.TargetsRS[0].Hvo;
				var hvoB = lr.TargetsRS[1].Hvo;
				if(isFirst)
				{
					if(hvoA == hvo1 && hvoB == hvo2)
					{
						refsAsYetUnmatched.Remove(lr);
						return true;
					}
				}
				else
				{
					if(hvoA == hvo2 && hvoB == hvo1)
					{
						refsAsYetUnmatched.Remove(lr);
						return true;
					}
				}
			}
			return false;
		}

		private void StorePairRelations(ICollection<ILexReference> refsAsYetUnmatched, ILexRefType lrt, List<PendingRelation> rgRelation)
		{
			for(var i = 0; i < rgRelation.Count; ++i)
			{
				Debug.Assert(rgRelation[i].Target != null);
				if(rgRelation[i].Target == null)
				{
					continue;
				}
				if(PairRelationAlreadyExists(refsAsYetUnmatched, lrt, rgRelation[i]))
				{
					continue;
				}
				ILexReference lr = CreateNewLexReference();
				lrt.MembersOC.Add(lr);
				lr.TargetsRS.Add(rgRelation[i].CmObject);
				lr.TargetsRS.Add(rgRelation[i].Target);
				StoreRelationResidue(lr, rgRelation[i]);
			}
		}

		private static bool PairRelationAlreadyExists(ICollection<ILexReference> refsAsYetUnmatched, ILexRefType lrt, PendingRelation rel)
		{
			var hvo1 = rel.CmObject == null ? 0 : rel.ObjectHvo;
			var hvo2 = rel.TargetHvo;
			foreach(var lr in lrt.MembersOC)
			{
				if(lr.TargetsRS.Count != 2)
					continue; // SHOULD NEVER HAPPEN!!
				var hvoA = lr.TargetsRS[0].Hvo;
				var hvoB = lr.TargetsRS[1].Hvo;
				if((hvoA == hvo1 && hvoB == hvo2) || (hvoA == hvo2 && hvoB == hvo1))
				{
					refsAsYetUnmatched.Remove(lr);
					return true;
				}
			}
			return false;
		}

		/// <summary>
		/// Removes duplicate and mirrored relations from Collections
		/// </summary>
		/// <param name="rgRelation"></param>
		/// <param name="uniqueRelations">see comment on </param>
		private static void CollapseCollectionRelationPairs(IEnumerable<PendingRelation> rgRelation,
			IDictionary<string, List<Tuple<HashSet<int>, HashSet<PendingRelation>>>> uniqueRelations)
		{
			//for every pending relation in this list
			foreach(var rel in rgRelation)
			{
				Debug.Assert(rel.Target != null);
				if(rel.Target == null)
					continue;
				List<Tuple<HashSet<int>, HashSet<PendingRelation>>> relationsForType;
				uniqueRelations.TryGetValue(rel.RelationType, out relationsForType);
				if(relationsForType != null)
				{
					bool foundGroup = false;
					//for every group of relations identified so far (relations which share target or object ids)
					foreach(var refs in relationsForType)
					{
						var fAdd = true;
						//If this item belongs to an existing group
						if(refs.Item1.Contains(rel.ObjectHvo) || refs.Item1.Contains(rel.TargetHvo))
						{
							foundGroup = true;
							foreach(var pend in refs.Item2)
							{
								//test if we have added this relation or a mirror of it.
								if(pend.IsSameOrMirror(rel))
								{
									fAdd = false;
									break;
								}
							}
							if(fAdd) //add it into the group if it wasn't already here.
							{
								refs.Item1.Add(rel.ObjectHvo);
								refs.Item1.Add(rel.TargetHvo);
								refs.Item2.Add(rel);
								continue;
							}
						}
					}
					//if this is a brand new relation for this type, build it
					if(!foundGroup)
					{
						relationsForType.Add(new Tuple<HashSet<int>, HashSet<PendingRelation>>(
							new HashSet<int> {rel.ObjectHvo, rel.TargetHvo}, new HashSet<PendingRelation> {rel}));
					}
				}
				else //First relation that we are processing, create the dictionary with this relation as our initial data.
				{
					var relData = new List<Tuple<HashSet<int>, HashSet<PendingRelation>>>
					{
						new Tuple<HashSet<int>, HashSet<PendingRelation>>(new HashSet<int> {rel.TargetHvo, rel.ObjectHvo},
							new HashSet<PendingRelation> {rel})
					};
					uniqueRelations[rel.RelationType] = relData;
				}
			}
		}

		private void StorePendingCollectionRelations(ICollection<ILexReference> originalLexRefs, IProgress progress,
			Dictionary<string, List<Tuple<HashSet<int>, HashSet<PendingRelation>>>> relationMap)
		{
			progress.Message = string.Format(LexTextControls.ksSettingCollectionRelationLinks, m_rgPendingCollectionRelations.Count);
			progress.Minimum = 0;
			progress.Maximum = relationMap.Count;
			progress.Position = 0;
			foreach(var typeCollections in relationMap) //for each relationType
			{
				string sType = typeCollections.Key;
				foreach(var collection in typeCollections.Value) //for each grouping of relations for that type
				{
					var incomingRelationIDs = collection.Item1;
					var incomingRelations = collection.Item2;
					ILexRefType lrt = FindLexRefType(sType, false);
					if (IsAnExistingCollectionAnExactMatch(originalLexRefs, lrt, incomingRelationIDs))
					{
						continue;
					}
					List<ICmObject> oldItems;
					var lr = FindExistingSequence(lrt, incomingRelations, out oldItems);
					if(lr == null)
					{
						lr = CreateNewLexReference();
						lrt.MembersOC.Add(lr);
						foreach(int hvo in incomingRelationIDs)
							lr.TargetsRS.Add(GetObjectForId(hvo));
						foreach(var relation in incomingRelations)
							StoreRelationResidue(lr, relation);
					}
					else
					{
						originalLexRefs.Remove(lr);
						ReplaceLexRefContents(lr, incomingRelationIDs);
					}
				}
				progress.Position++;
			}
		}

		/// <summary>
		/// This method is necessary to prevent duplication of sets of relations due to the fact they are replicated in
		/// all the members of the relation in the LIFT file.
		/// </summary>
		/// <param name="originalLexRefs">Collection of the lexical references from before the import. Items remaining here may be removed for MergeStyle.MsKeepOnlyNew</param>
		/// <param name="lrt">The LexRefType to inspect for duplicate collections</param>
		/// <param name="checkTargets">The set of ids to look for</param>
		/// <returns></returns>
		private static bool IsAnExistingCollectionAnExactMatch(ICollection<ILexReference> originalLexRefs, ILexRefType lrt,
			HashSet<int> checkTargets)
		{
			foreach (var lr in lrt.MembersOC)
			{
				if(lr.TargetsRS.Count != checkTargets.Count)
					continue;
				//build a set of the hvos in the lexical relation
				var currentSet = lr.TargetsRS.Select(cmo => cmo.Hvo).ToList();
				//for every object in the target sequence of the LexReference
				if(currentSet.ContainsCollection(checkTargets))
				{
					originalLexRefs.Remove(lr);
					return true; // got an exact match. If not, keep trying.
				}
			}
			return false;
		}

		/// <summary>
		/// This method will adjust two collections (which share some content) based off new data from the lift file.
		/// </summary>
		/// <param name="lr">The ILexReference to adjust</param>
		/// <param name="setRelation">The Set of hvo's to use in the adjusted reference</param>
		private void ReplaceLexRefContents(ILexReference lr, IEnumerable<int> setRelation)
		{
			var currentSet = new List<int>();
			//for every object in the target sequence of the LexReference
			foreach (var cmo in lr.TargetsRS)
			{
				currentSet.Add(cmo.Hvo);
			}
			var intersectors = currentSet.Intersect(setRelation);
			if(intersectors.Count() == 0) //the two sets are unrelated, and shouldn't be merged
			{
				return;
			}
			//If the sets intersect, but did not have a subset-superset relationship then we might be doing
			//something the user did not expect or want, so log it for them.
			if (!intersectors.ContainsCollection(setRelation) && !intersectors.ContainsCollection(currentSet))
			{
				CombinedCollection conflictingData;
				foreach(var item in currentSet)
				{
					if (!intersectors.Contains(item))
					{
						conflictingData = new CombinedCollection(GetObjectForId(intersectors.First()), m_cache, this);
						conflictingData.TypeName = lr.TypeAbbreviation(m_cache.DefaultUserWs, GetObjectForId(item));
						conflictingData.BadValue = GetObjectForId(item).DeletionTextTSS.Text;
						m_combinedCollections.Add(conflictingData);
					}
				}
			}
			var otherObjects = new List<ICmObject>();
			foreach (var hvo in setRelation)
			{
				otherObjects.Add(GetObjectForId(hvo));
			}
			AdjustCollectionContents(otherObjects, lr.TargetsRS, lr);
		}

		private void StoreSequenceRelation(ICollection<ILexReference> refsAsYetUnmatched, ILexRefType lrt,
													  List<PendingRelation> rgRelation)
		{
			if(IsAnExistingSequenceAnExactMatch(refsAsYetUnmatched, lrt, rgRelation))
			{
				return;
			}
			List<ICmObject> oldItems;
			var lr = FindExistingSequence(lrt, rgRelation, out oldItems);
			if(lr == null) //This is a new relation, add all the targets
			{
				lr = CreateNewLexReference();
				lrt.MembersOC.Add(lr);
				for(var i = 0; i < rgRelation.Count; ++i)
				{
					lr.TargetsRS.Add(GetObjectForId(rgRelation[i].TargetHvo));
				}
			}
			else //Reusing an old relation, replace the contents of the Targets sequence
			{
				var targetList = new List<ICmObject>();
				for(var i = 0; i < rgRelation.Count; ++i)
				{
					targetList.Add(GetObjectForId(rgRelation[i].TargetHvo));
				}
				lr.TargetsRS.Replace(0, lr.TargetsRS.Count, targetList);
				refsAsYetUnmatched.Remove(lr);
			}
			StoreRelationResidue(lr, rgRelation[0]);
		}

		/// <summary>
		/// Tests if there is an existing sequence in the LexRefType which exactly matches the pendingRelation
		/// in contents and order.
		/// </summary>
		/// <param name="refsAsYetUnmatched"></param>
		/// <param name="lrt">the ILexRefType to check</param>
		/// <param name="rgRelation">An ordered list of relations from the LIFT file</param>
		/// <returns>true if this sequence already exists in the system</returns>
		private static bool IsAnExistingSequenceAnExactMatch(ICollection<ILexReference> refsAsYetUnmatched, ILexRefType lrt, IList<PendingRelation> rgRelation)
		{
			foreach(var lr in lrt.MembersOC)
			{
				if(lr.TargetsRS.Count != rgRelation.Count)
				{
					continue;
				}
				var sequenceMatched = true;
				for(var i = 0; i < rgRelation.Count; ++i)
				{
					if(lr.TargetsRS[i].Hvo != rgRelation[i].TargetHvo)
					{
						sequenceMatched = false;
						break;
					}
				}
				if(sequenceMatched)
				{
					refsAsYetUnmatched.Remove(lr);
					return true;
				}
			}
			return false;
		}

		/// <summary>
		/// This method will find and return an ILexReference in the given LexRefType if it has any overlap with the given collection
		/// of relations
		/// </summary>
		/// <param name="lrt"></param>
		/// <param name="rgRelation"></param>
		/// <param name="oldItems"></param>
		/// <returns></returns>
		private static ILexReference FindExistingSequence(ILexRefType lrt, IEnumerable<PendingRelation> rgRelation, out List<ICmObject> oldItems)
		{
			oldItems = new List<ICmObject>();
			foreach (var lr in lrt.MembersOC)
			{
				foreach (var cmObject in lr.TargetsRS)
				{
					oldItems.Add(cmObject);
				}
				foreach (var pendingRelation in rgRelation)
				{
					if (oldItems.Contains(pendingRelation.Target))
					{
						return lr;
					}
				}
				oldItems.Clear();
			}
			oldItems = null; // old items aren't actually old items if we didn't find a match
			return null;
		}

		private void StoreTreeRelation(ICollection<ILexReference> refsAsYetUnmatched, ILexRefType lrt, List<PendingRelation> rgRelation)
		{
			if (TreeRelationAlreadyExists(refsAsYetUnmatched, lrt, rgRelation))
			{
				return;
			}
			if(ObjectIsFirstInRelation(rgRelation[0].RelationType, lrt))
			{
				var lr = CreateNewLexReference();
				lrt.MembersOC.Add(lr);
				lr.TargetsRS.Add(rgRelation[0].CmObject);
				for (int i = 0; i < rgRelation.Count; ++i)
					lr.TargetsRS.Add(GetObjectForId(rgRelation[i].TargetHvo));
				StoreRelationResidue(lr, rgRelation[0]);
			}
			else
			{
				foreach (var pendingRelation in rgRelation)
				{
					m_rgPendingTreeTargets.Add(pendingRelation);
				}
			}
		}

		private void StoreRelationResidue(ILexReference lr, PendingRelation pend)
		{
			var sResidue = pend.Residue;
			if (!String.IsNullOrEmpty(sResidue) ||
				IsDateSet(pend.DateCreated) || IsDateSet(pend.DateModified))
			{
				var bldr = new StringBuilder();
				bldr.Append("<lift-residue");
				AppendXmlDateAttributes(bldr, pend.DateCreated, pend.DateModified);
				bldr.AppendLine(">");
				if (!String.IsNullOrEmpty(sResidue))
					bldr.Append(sResidue);
				bldr.Append("</lift-residue>");
				lr.LiftResidue = bldr.ToString();
			}
		}

	   /// <summary>
	   /// This method will test if a TreeRelation already exists in a lex reference of the given ILexRefType
	   /// related to the given list of pending relations.
	   /// If it does then those relations which are not yet included will be added to the matching ILexReference.
	   /// </summary>
	   /// <param name="originalLexRefs"></param>
	   /// <param name="lrt"></param>
	   /// <param name="rgRelation"></param>
	   /// <returns>true if a match was found</returns>
	   private bool TreeRelationAlreadyExists(ICollection<ILexReference> originalLexRefs, ILexRefType lrt, List<PendingRelation> rgRelation)
		{
		   foreach(var lr in lrt.MembersOC) //for every potential reference
		   {
			   if(lr.TargetsRS.Count == 0) //why this should be I don't know, probably due to some defect elsewhere.
			   {
				   continue;
			   }
			   var firstTargetHvo = lr.TargetsRS.First().Hvo;
			   if(firstTargetHvo == rgRelation[0].ObjectHvo) //if the target of the first relation is the first item in the list
			   {
					// Store the original contents of the LexReference to track removals
					var relationsBeforeAdditions = new List<ICmObject>(lr.TargetsRS.AsEnumerable());
				   foreach(var pendingRelation in rgRelation)
				   {
					   var pendingObj = GetObjectForId(pendingRelation.TargetHvo);
					   if(firstTargetHvo == pendingRelation.ObjectHvo
							&& HasMatchingUnicodeAlternative(pendingRelation.RelationType.ToLowerInvariant(),
																		lrt.Abbreviation, lrt.Name))
					   {
						   if(!lr.TargetsRS.Contains(pendingObj))
							{
								lr.TargetsRS.Add(pendingObj); // Add each item which is not yet in this list
							}
							else
							{
								relationsBeforeAdditions.Remove(pendingObj); // The object was found in the relation
							}
					   }
					   else
					   {
							relationsBeforeAdditions.Remove(pendingObj); // The pending relation wasn't for this LexRef, so don't store pendingObj for removal
						   m_rgPendingTreeTargets.Add(pendingRelation);
					   }
				   }
				   originalLexRefs.Remove(lr);
					// Consider removing every object that wasn't matched from the tree relationship
				   foreach(var unusedObject in relationsBeforeAdditions)
				   {
						// Check the LexReference for validity as removing an object could trigger its deletion.
						// We will receive the relation with the head of the tree and the relation with the children in separate method calls.
						// Skipping the removal of an unusedObject when the Hvo matches the CmObject that owns the relations prevents removing the head
						// when processing the children.
						if(lr.IsValidObject && unusedObject.Hvo != rgRelation[0].CmObject.Hvo)
						{
							lr.TargetsRS.Remove(unusedObject);
						}
				   }
				   return true;
			   }
		   }
		   return false;
		}

		/// <summary>
		/// This method will test if a TreeRelation already exists in a lex reference of the given ILexRefType
		/// related to the given PendingRelation.
		/// If it does then the relation will be added to the matching ILexReference if necessary.
		/// </summary>
		/// <param name="originalLexRefs"></param>
		/// <param name="lrt"></param>
		/// <param name="rel"></param>
		/// <returns>true if a match was found</returns>
		private bool TreeRelationAlreadyExists(ICollection<ILexReference> originalLexRefs, ILexRefType lrt,
															PendingRelation rel)
		{
			//The object who contains the other end of this relation, i.e. the entry with the Part
			//may not have been processed, so if we are dealing with the Whole we need to make sure that
			//the part is present.
			if(!ObjectIsFirstInRelation(rel.RelationType, lrt))
			{
				foreach(var lr in lrt.MembersOC)
				{
					if(lr.TargetsRS.Count == 0 || lr.TargetsRS[0].Hvo != rel.TargetHvo)
					{
						continue;
					}
					var pendingObj = GetObjectForId(rel.ObjectHvo);
					if(!lr.TargetsRS.Contains(pendingObj))
					{
						lr.TargetsRS.Add(pendingObj);
					}
					originalLexRefs.Remove(lr);
					return true;
				}
			}
			else
			{
				foreach(var lr in lrt.MembersOC)
				{
					if(lr.TargetsRS.Count == 0 || lr.TargetsRS[0].Hvo != rel.ObjectHvo)
					{
						continue;
					}
					var pendingObj = GetObjectForId(rel.TargetHvo);
					if(!lr.TargetsRS.Contains(pendingObj))
					{
						lr.TargetsRS.Add(pendingObj);
					}
					originalLexRefs.Remove(lr);
					return true;
				}
			}
			return false;
		}

		/// <summary>
		/// This method will return true if the given type is the name, or main part of this reference type.
		/// as opposed to the ReversalName i.e. in a Part/Whole relation Part would return true, where Whole would
		/// return false.
		/// </summary>
		/// <param name="sType"></param>
		/// <param name="lrt"></param>
		/// <returns></returns>
		private bool ObjectIsFirstInRelation(string sType, ILexRefType lrt)
		{
			if (HasMatchingUnicodeAlternative(sType.ToLowerInvariant(), lrt.Abbreviation, lrt.Name))
				return true;
			else
				return false;
		}

		private List<PendingRelation> CollectRelationMembers(int i)
		{
			if (i < 0 || i >= m_rgPendingRelation.Count)
				return null;
			var relation = new List<PendingRelation>();
			PendingRelation prev = null;
			var hvo = m_rgPendingRelation[i].ObjectHvo;
			var sType = m_rgPendingRelation[i].RelationType;
			var dateCreated = m_rgPendingRelation[i].DateCreated;
			var dateModified = m_rgPendingRelation[i].DateModified;
			var sResidue = m_rgPendingRelation[i].Residue;
			bool needSort = false;
			while (i < m_rgPendingRelation.Count)
			{
				PendingRelation pend = m_rgPendingRelation[i];
				// If the object or relation type (or residue) has changed, we're into another
				// lexical relation.
				if (pend.ObjectHvo != hvo || pend.RelationType != sType ||
					pend.DateCreated != dateCreated || pend.DateModified != dateModified ||
					pend.Residue != sResidue)
				{
					break;
				}
				// If the sequence items are out of order we must sort them.
				if (prev != null && pend.Order < prev.Order)
					needSort = true;
				pend.Target = GetObjectFromTargetIdString(m_rgPendingRelation[i].TargetId);
				relation.Add(pend);	// We handle missing/unrecognized targets later.
				prev = pend;
				++i;
			}
			// In the (typically unusual) case that the elements of the relation are out of order,
			// sort them. We prefer to use the Linq OrderBy method rather than the Sort method
			// of List because it is stable (that is, if some objects have the same Order we will
			// at least keep ones with the same Order value in their original order).
			// Enhance JohnT: it is possible that items appear out of order because they were exported
			// from two LexReferenceType objects with the same name (pend.RelationType) on the same
			// source object (pend.ObjectHvo). We can't currently recreate this situation on import.
			// See LT-15389.
			if (needSort)
				return relation.OrderBy(pend => pend.Order).ToList();
			return relation;
		}

		private void GatherUnwantedObjects(List<ILexEntryRef> unusedLexEntryRefs, IEnumerable<ILexReference> unusedLexRefs)
		{
			foreach(var lexRef in unusedLexRefs)
			{
				m_deletedObjects.Add(lexRef.Hvo);
			}
			foreach(var lexEntryRef in unusedLexEntryRefs)
			{
				m_deletedObjects.Add(lexEntryRef.Hvo);
			}
			foreach(var le in m_cache.LangProject.LexDbOA.Entries)
			{
				if(!m_setUnchangedEntry.Contains(le.Guid) &&
					!m_setChangedEntry.Contains(le.Guid))
				{
					m_deletedObjects.Add(le.Hvo);
				}
			}
		}

		private void DeleteUnwantedObjects()
		{
			if (m_deletedObjects.Count > 0)
			{
				DeleteObjects(m_deletedObjects);
				DeleteOrphans();
			}
		}

		/// <summary>
		/// This pretends to replace CmObject.DeleteObjects() in the old system.
		/// </summary>
		/// <param name="deletedObjects"></param>
		private void DeleteObjects(HashSet<int> deletedObjects)
		{
			foreach (int hvo in deletedObjects)
			{
				try
				{
					ICmObject cmo = GetObjectForId(hvo);
					int hvoOwner = cmo.Owner == null ? 0 : cmo.Owner.Hvo;
					int flid = cmo.OwningFlid;
					m_cache.MainCacheAccessor.DeleteObjOwner(hvoOwner, hvo, flid, -1);
				}
				catch
				{
				}
			}
		}

		/// <summary>
		/// This replaces CmObject.DeleteOrphanedObjects(m_cache, false, null); in the
		/// old system, which used SQL extensively.  I'm not sure where this should go in
		/// the new system, or if it was used anywhere else.
		/// </summary>
		private void DeleteOrphans()
		{
			var orphans = new HashSet<int>();
			// Look for LexReference objects that have lost all their targets.
			ILexReferenceRepository repoLR = m_cache.ServiceLocator.GetInstance<ILexReferenceRepository>();
			foreach (ILexReference lr in repoLR.AllInstances())
			{
				if (lr.TargetsRS.Count == 0)
					orphans.Add(lr.Hvo);
			}
			DeleteObjects(orphans);
			orphans.Clear();
			// Look for MSAs that are not used by any senses.
			IMoMorphSynAnalysisRepository repoMsa = m_cache.ServiceLocator.GetInstance<IMoMorphSynAnalysisRepository>();
			foreach (IMoMorphSynAnalysis msa in repoMsa.AllInstances())
			{
				ILexEntry le = msa.Owner as ILexEntry;
				if (le == null)
					continue;
				bool fUsed = false;
				foreach (ILexSense ls in le.AllSenses)
				{
					if (ls.MorphoSyntaxAnalysisRA == msa)
					{
						fUsed = true;
						break;
					}
				}
				if (!fUsed)
					orphans.Add(msa.Hvo);
			}
			DeleteObjects(orphans);
			orphans.Clear();
			// Look for WfiAnalysis objects that are not targeted by a human CmAgentEvaluation
			// and which do not own a WfiMorphBundle with a set Msa value.
			IWfiAnalysisRepository repoWA = m_cache.ServiceLocator.GetInstance<IWfiAnalysisRepository>();
			ICmAgent cmaHuman = GetObjectForGuid(CmAgentTags.kguidAgentDefUser) as ICmAgent;
			Debug.Assert(cmaHuman != null);
			foreach (IWfiAnalysis wa in repoWA.AllInstances())
			{
				if (wa.GetAgentOpinion(cmaHuman as ICmAgent) == Opinions.noopinion)
				{
					bool fOk = false;
					foreach (IWfiMorphBundle wmb in wa.MorphBundlesOS)
					{
						if (wmb.MsaRA != null)
						{
							fOk = true;
							break;
						}
					}
					if (!fOk)
						orphans.Add(wa.Hvo);
				}
			}
			DeleteObjects(orphans);
			orphans.Clear();

			// Update WfiMorphBundle.Form and WfiMorphBundle.Msa as needed.
			IWfiMorphBundleRepository repoWMB = m_cache.ServiceLocator.GetInstance<IWfiMorphBundleRepository>();
			foreach (IWfiMorphBundle mb in repoWMB.AllInstances())
			{
				if (mb.Form.StringCount == 0 && mb.MorphRA == null && mb.MsaRA == null && mb.SenseRA == null)
				{
					IWfiAnalysis wa = mb.Owner as IWfiAnalysis;
					IWfiWordform wf = wa.Owner as IWfiWordform;
					ITsString tssWordForm = wf.Form.get_String(m_cache.DefaultVernWs);
					if (tssWordForm != null && tssWordForm.Length > 0)
						mb.Form.set_String(m_cache.DefaultVernWs, tssWordForm.Text);
				}
				if (mb.MsaRA == null && mb.SenseRA != null)
				{
					mb.MsaRA = mb.SenseRA.MorphoSyntaxAnalysisRA;
				}
			}
			// Look for MoMorphAdhocProhib objects that don't have any Morphemes (MSA targets)
			IMoMorphAdhocProhibRepository repoMAP = m_cache.ServiceLocator.GetInstance<IMoMorphAdhocProhibRepository>();
			foreach (IMoMorphAdhocProhib map in repoMAP.AllInstances())
			{
				if (map.MorphemesRS.Count == 0)
					orphans.Add(map.Hvo);
			}
			DeleteObjects(orphans);
			orphans.Clear();
		}
		#endregion // Methods for handling relation links


		//=======================================================================================
		#region Methods for getting or creating model objects

		internal ICmObject GetObjectForId(int hvo)
		{
			if (m_repoCmObject == null)
				m_repoCmObject = m_cache.ServiceLocator.GetInstance<ICmObjectRepository>();
			try
			{
				return m_repoCmObject.GetObject(hvo);
			}
			catch (KeyNotFoundException)
			{
				return null;
			}
		}

		internal ICmObject GetObjectForGuid(Guid guid)
		{
			if (m_repoCmObject == null)
				m_repoCmObject = m_cache.ServiceLocator.GetInstance<ICmObjectRepository>();
			if (m_repoCmObject.IsValidObjectId(guid))
				return m_repoCmObject.GetObject(guid);
			else
				return null;
		}

		internal CoreWritingSystemDefinition GetExistingWritingSystem(int handle)
		{
			return m_cache.ServiceLocator.WritingSystemManager.Get(handle);
		}


		internal IMoMorphType GetExistingMoMorphType(Guid guid)
		{
			if (m_repoMoMorphType == null)
				m_repoMoMorphType = m_cache.ServiceLocator.GetInstance<IMoMorphTypeRepository>();
			return m_repoMoMorphType.GetObject(guid);
		}

		internal ICmAnthroItem CreateNewCmAnthroItem(string guidAttr, ICmObject owner)
		{
			if (m_factCmAnthroItem == null)
				m_factCmAnthroItem = m_cache.ServiceLocator.GetInstance<ICmAnthroItemFactory>();
			if (!String.IsNullOrEmpty(guidAttr))
			{
				Guid guid = (Guid)m_gconv.ConvertFrom(guidAttr);
				if (owner is ICmAnthroItem)
					return m_factCmAnthroItem.Create(guid, owner as ICmAnthroItem);
				else
					return m_factCmAnthroItem.Create(guid, owner as ICmPossibilityList);
			}
			else
			{
				ICmAnthroItem cai = m_factCmAnthroItem.Create();
				if (owner is ICmAnthroItem)
					(owner as ICmAnthroItem).SubPossibilitiesOS.Add(cai);
				else
					(owner as ICmPossibilityList).PossibilitiesOS.Add(cai);
				return cai;
			}
		}

		internal ICmAnthroItem CreateNewCmAnthroItem()
		{
			if (m_factCmAnthroItem == null)
				m_factCmAnthroItem = m_cache.ServiceLocator.GetInstance<ICmAnthroItemFactory>();
			return m_factCmAnthroItem.Create();
		}

		internal ICmSemanticDomain CreateNewCmSemanticDomain()
		{
			if (m_factCmSemanticDomain == null)
				m_factCmSemanticDomain = m_cache.ServiceLocator.GetInstance<ICmSemanticDomainFactory>();
			return m_factCmSemanticDomain.Create();
		}

		internal ICmSemanticDomain CreateNewCmSemanticDomain(string guidAttr, ICmObject owner)
		{
			if (m_factCmSemanticDomain == null)
				m_factCmSemanticDomain = m_cache.ServiceLocator.GetInstance<ICmSemanticDomainFactory>();
			if (!String.IsNullOrEmpty(guidAttr))
			{
				Guid guid = (Guid)m_gconv.ConvertFrom(guidAttr);
				if (owner is ICmSemanticDomain)
					return m_factCmSemanticDomain.Create(guid, owner as ICmSemanticDomain);
				else
					return m_factCmSemanticDomain.Create(guid, owner as ICmPossibilityList);
			}
			else
			{
				ICmSemanticDomain csd = m_factCmSemanticDomain.Create();
				if (owner is ICmSemanticDomain)
					(owner as ICmSemanticDomain).SubPossibilitiesOS.Add(csd);
				else
					(owner as ICmPossibilityList).PossibilitiesOS.Add(csd);
				return csd;
			}
		}

		internal IMoStemAllomorph CreateNewMoStemAllomorph()
		{
			if (m_factMoStemAllomorph == null)
				m_factMoStemAllomorph = m_cache.ServiceLocator.GetInstance<IMoStemAllomorphFactory>();
			return m_factMoStemAllomorph.Create();
		}

		internal IMoAffixAllomorph CreateNewMoAffixAllomorph()
		{
			if (m_factMoAffixAllomorph == null)
				m_factMoAffixAllomorph = m_cache.ServiceLocator.GetInstance<IMoAffixAllomorphFactory>();
			return m_factMoAffixAllomorph.Create();
		}

		internal ILexPronunciation CreateNewLexPronunciation()
		{
			if (m_factLexPronunciation == null)
				m_factLexPronunciation = m_cache.ServiceLocator.GetInstance<ILexPronunciationFactory>();
			return m_factLexPronunciation.Create();
		}

		internal ICmMedia CreateNewCmMedia()
		{
			if (m_factCmMedia == null)
				m_factCmMedia = m_cache.ServiceLocator.GetInstance<ICmMediaFactory>();
			return m_factCmMedia.Create();
		}

		internal ILexEtymology CreateNewLexEtymology()
		{
			if (m_factLexEtymology == null)
				m_factLexEtymology = m_cache.ServiceLocator.GetInstance<ILexEtymologyFactory>();
			return m_factLexEtymology.Create();
		}

		internal ILexSense CreateNewLexSense(Guid guid, ICmObject owner, out bool fNeedNewId)
		{
			Debug.Assert(owner != null);
			Debug.Assert(owner is ILexEntry || owner is ILexSense);
			if (m_factLexSense == null)
				m_factLexSense = m_cache.ServiceLocator.GetInstance<ILexSenseFactory>();
			fNeedNewId = false;
			ILexSense ls = null;
			if (guid != Guid.Empty && GuidIsNotInUse(guid))
			{
				if (owner is ILexEntry)
					ls = m_factLexSense.Create(guid, owner as ILexEntry);
				else
					ls = m_factLexSense.Create(guid, owner as ILexSense);

			}
			if (ls == null)
			{
				ls = m_factLexSense.Create();
				if (owner is ILexEntry)
					(owner as ILexEntry).SensesOS.Add(ls);
				else
					(owner as ILexSense).SensesOS.Add(ls);
				fNeedNewId = guid != Guid.Empty;
			}
			return ls;
		}

		private bool GuidIsNotInUse(Guid guid)
		{
			if (m_deletedGuids.Contains(guid))
				return false;
			if (m_repoCmObject == null)
				m_repoCmObject = m_cache.ServiceLocator.GetInstance<ICmObjectRepository>();
			return !m_repoCmObject.IsValidObjectId(guid);
		}

		private ILexEntry CreateNewLexEntry(Guid guid, out bool fNeedNewId)
		{
			if (m_factLexEntry == null)
				m_factLexEntry = m_cache.ServiceLocator.GetInstance<ILexEntryFactory>();
			fNeedNewId = false;
			ILexEntry le = null;
			if (guid != Guid.Empty && GuidIsNotInUse(guid))
				le = m_factLexEntry.Create(guid, m_cache.LangProject.LexDbOA);
			if (le == null)
			{
				le = m_factLexEntry.Create();
				fNeedNewId = guid != Guid.Empty;
			}
			return le;
		}

		internal IMoInflClass CreateNewMoInflClass()
		{
			if (m_factMoInflClass == null)
				m_factMoInflClass = m_cache.ServiceLocator.GetInstance<IMoInflClassFactory>();
			return m_factMoInflClass.Create();
		}

		internal IMoInflAffixSlot CreateNewMoInflAffixSlot()
		{
			if (m_factMoInflAffixSlot == null)
				m_factMoInflAffixSlot = m_cache.ServiceLocator.GetInstance<IMoInflAffixSlotFactory>();
			return m_factMoInflAffixSlot.Create();
		}

		internal ILexExampleSentence CreateNewLexExampleSentence(Guid guid, ILexSense owner)
		{
			Debug.Assert(owner != null);
			if (m_factLexExampleSentence == null)
				m_factLexExampleSentence = m_cache.ServiceLocator.GetInstance<ILexExampleSentenceFactory>();
			if (guid != Guid.Empty && GuidIsNotInUse(guid))
			{
				return m_factLexExampleSentence.Create(guid, owner);
			}
			else
			{
				ILexExampleSentence les = m_factLexExampleSentence.Create();
				owner.ExamplesOS.Add(les);
				return les;
			}
		}

		internal ICmTranslation CreateNewCmTranslation(ILexExampleSentence les, ICmPossibility type)
		{
			if (m_factCmTranslation == null)
				m_factCmTranslation = m_cache.ServiceLocator.GetInstance<ICmTranslationFactory>();
			bool fNoType = type == null;
			if (fNoType)
			{
				ICmObject obj;
				if (m_repoCmObject.TryGetObject(LangProjectTags.kguidTranFreeTranslation, out obj))
					type = obj as ICmPossibility;
				if (type == null)
					type = FindOrCreateTranslationType("Free translation");
			}
			ICmTranslation trans = m_factCmTranslation.Create(les, type);
			if (fNoType)
				trans.TypeRA = null;
			return trans;
		}

		internal ILexEntryType CreateNewLexEntryType()
		{
			if (m_factLexEntryType == null)
				m_factLexEntryType = m_cache.ServiceLocator.GetInstance<ILexEntryTypeFactory>();
			return m_factLexEntryType.Create();
		}

		/// <summary>
		/// Will create a new ref type using the guid if one is passed. The type will be owned by the owner guid if one is passed.
		/// </summary>
		/// <param name="guid">can be null</param>
		/// <param name="owner">can be null</param>
		/// <returns></returns>
		internal ILexRefType CreateNewLexRefType(string guid, string owner)
		{
			if(m_factLexRefType == null)
			{
				m_factLexRefType = m_cache.ServiceLocator.GetInstance<ILexRefTypeFactory>();
			}
			var cleanOwner = owner != null ? owner.ToLowerInvariant() : null;
			return m_factLexRefType.Create(String.IsNullOrEmpty(guid) ? Guid.Empty : new Guid(guid),
													 m_cache.ServiceLocator.GetAllInstances<ILexRefType>().FirstOrDefault(x => x.Guid.ToString().ToLowerInvariant() == cleanOwner));
		}

		internal ICmPossibility CreateNewCmPossibility()
		{
			if (m_factCmPossibility == null)
				m_factCmPossibility = m_cache.ServiceLocator.GetInstance<ICmPossibilityFactory>();
			return m_factCmPossibility.Create();
		}

		internal ICmPossibility CreateNewCmPossibility(string guidAttr, ICmObject owner)
		{
			if (m_factCmPossibility == null)
				m_factCmPossibility = m_cache.ServiceLocator.GetInstance<ICmPossibilityFactory>();
			if (!string.IsNullOrEmpty(guidAttr))
			{
				Guid guid = (Guid)m_gconv.ConvertFrom(guidAttr);
				if (owner is ICmPossibility)
					return m_factCmPossibility.Create(guid, owner as ICmPossibility);
				else
					return m_factCmPossibility.Create(guid, owner as ICmPossibilityList);
			}
			else
			{
				ICmPossibility csd = m_factCmPossibility.Create();
				if (owner is ICmPossibility)
					(owner as ICmPossibility).SubPossibilitiesOS.Add(csd);
				else
					(owner as ICmPossibilityList).PossibilitiesOS.Add(csd);
				return csd;
			}
		}

		private ICmPossibility CreateNewCustomPossibility(string guidAttr, ICmObject owner)
		{
			var customPossibilityFactory = m_cache.ServiceLocator.GetInstance<ICmCustomItemFactory>();
			if (!string.IsNullOrEmpty(guidAttr))
			{
				Guid guid = (Guid)m_gconv.ConvertFrom(guidAttr);
				if (owner is ICmPossibility)
					return customPossibilityFactory.Create(guid, owner as ICmCustomItem);
				else
					return customPossibilityFactory.Create(guid, owner as ICmPossibilityList);
			}
			else
			{
				var csd = customPossibilityFactory.Create();
				var item = owner as ICmCustomItem;
				if (item != null)
					item.SubPossibilitiesOS.Add(csd);
				else
					(owner as ICmPossibilityList).PossibilitiesOS.Add(csd);
				return csd;
			}
		}

		internal ICmLocation CreateNewCmLocation()
		{
			if (m_factCmLocation == null)
				m_factCmLocation = m_cache.ServiceLocator.GetInstance<ICmLocationFactory>();
			return m_factCmLocation.Create();
		}

		internal IMoStemMsa CreateNewMoStemMsa()
		{
			if (m_factMoStemMsa == null)
				m_factMoStemMsa = m_cache.ServiceLocator.GetInstance<IMoStemMsaFactory>();
			return m_factMoStemMsa.Create();
		}

		internal IMoUnclassifiedAffixMsa CreateNewMoUnclassifiedAffixMsa()
		{
			if (m_factMoUnclassifiedAffixMsa == null)
				m_factMoUnclassifiedAffixMsa = m_cache.ServiceLocator.GetInstance<IMoUnclassifiedAffixMsaFactory>();
			return m_factMoUnclassifiedAffixMsa.Create();
		}

		internal IMoDerivStepMsa CreateNewMoDerivStepMsa()
		{
			if (m_factMoDerivStepMsa == null)
				m_factMoDerivStepMsa = m_cache.ServiceLocator.GetInstance<IMoDerivStepMsaFactory>();
			return m_factMoDerivStepMsa.Create();
		}

		internal IMoDerivAffMsa CreateNewMoDerivAffMsa()
		{
			if (m_factMoDerivAffMsa == null)
				m_factMoDerivAffMsa = m_cache.ServiceLocator.GetInstance<IMoDerivAffMsaFactory>();
			return m_factMoDerivAffMsa.Create();
		}

		internal IMoInflAffMsa CreateNewMoInflAffMsa()
		{
			if (m_factMoInflAffMsa == null)
				m_factMoInflAffMsa = m_cache.ServiceLocator.GetInstance<IMoInflAffMsaFactory>();
			return m_factMoInflAffMsa.Create();
		}

		internal ICmPicture CreateNewCmPicture()
		{
			if (m_factCmPicture == null)
				m_factCmPicture = m_cache.ServiceLocator.GetInstance<ICmPictureFactory>();
			return m_factCmPicture.Create();
		}

		internal IReversalIndexEntry CreateNewReversalIndexEntry()
		{
			if (m_factReversalIndexEntry == null)
				m_factReversalIndexEntry = m_cache.ServiceLocator.GetInstance<IReversalIndexEntryFactory>();
			return m_factReversalIndexEntry.Create();
		}

		internal IReversalIndex FindOrCreateReversalIndex(int ws)
		{
			if (m_repoReversalIndex == null)
				m_repoReversalIndex = m_cache.ServiceLocator.GetInstance<IReversalIndexRepository>();
			return m_repoReversalIndex.FindOrCreateIndexForWs(ws);
		}

		internal IPartOfSpeech CreateNewPartOfSpeech()
		{
			if (m_factPartOfSpeech == null)
				m_factPartOfSpeech = m_cache.ServiceLocator.GetInstance<IPartOfSpeechFactory>();
			return m_factPartOfSpeech.Create();
		}

		internal IPartOfSpeech CreateNewPartOfSpeech(string guidAttr, ICmObject owner)
		{
			Debug.Assert(owner != null);
			Debug.Assert(owner is ICmPossibilityList || owner is IPartOfSpeech);
			if (m_factPartOfSpeech == null)
				m_factPartOfSpeech = m_cache.ServiceLocator.GetInstance<IPartOfSpeechFactory>();
			if (!String.IsNullOrEmpty(guidAttr))
			{
				Guid guid = (Guid)m_gconv.ConvertFrom(guidAttr);
				if (owner is IPartOfSpeech)
					return m_factPartOfSpeech.Create(guid, owner as IPartOfSpeech);
				else
					return m_factPartOfSpeech.Create(guid, owner as ICmPossibilityList);
			}
			else
			{
				IPartOfSpeech csd = m_factPartOfSpeech.Create();
				if (owner is IPartOfSpeech)
					(owner as IPartOfSpeech).SubPossibilitiesOS.Add(csd);
				else
					(owner as ICmPossibilityList).PossibilitiesOS.Add(csd);
				return csd;
			}
		}

		internal IMoMorphType CreateNewMoMorphType(string guidAttr, ICmObject owner)
		{
			Debug.Assert(owner != null);
			Debug.Assert(owner is ICmPossibilityList || owner is IMoMorphType);
			if (m_factMoMorphType == null)
				m_factMoMorphType = m_cache.ServiceLocator.GetInstance<IMoMorphTypeFactory>();
			if (!String.IsNullOrEmpty(guidAttr))
			{
				Guid guid = (Guid)m_gconv.ConvertFrom(guidAttr);
				if (owner is IMoMorphType)
					return m_factMoMorphType.Create(guid, owner as IMoMorphType);
				else
					return m_factMoMorphType.Create(guid, owner as ICmPossibilityList);
			}
			else
			{
				IMoMorphType csd = m_factMoMorphType.Create();
				if (owner is IMoMorphType)
					(owner as IMoMorphType).SubPossibilitiesOS.Add(csd);
				else
					(owner as ICmPossibilityList).PossibilitiesOS.Add(csd);
				return csd;
			}
		}

		private IPhEnvironment CreateNewPhEnvironment()
		{
			if (m_factPhEnvironment == null)
				m_factPhEnvironment = m_cache.ServiceLocator.GetInstance<IPhEnvironmentFactory>();
			return m_factPhEnvironment.Create();
		}

		private ILexReference CreateNewLexReference()
		{
			if (m_factLexReference == null)
				m_factLexReference = m_cache.ServiceLocator.GetInstance<ILexReferenceFactory>();
			return m_factLexReference.Create();
		}

		private ILexEntryRef CreateNewLexEntryRef()
		{
			if (m_factLexEntryRef == null)
				m_factLexEntryRef = m_cache.ServiceLocator.GetInstance<ILexEntryRefFactory>();
			return m_factLexEntryRef.Create();
		}

		internal ICmPerson CreateNewCmPerson()
		{
			if (m_factCmPerson == null)
				m_factCmPerson = m_cache.ServiceLocator.GetInstance<ICmPersonFactory>();
			return m_factCmPerson.Create();
		}

		internal ICmPerson CreateNewCmPerson(string guidAttr, ICmObject owner)
		{
			if (!(owner is ICmPossibilityList))
				throw new ArgumentException("Person should be in the People list", "owner");
			if (m_factCmPerson == null)
				m_factCmPerson = m_cache.ServiceLocator.GetInstance<ICmPersonFactory>();
			if (!String.IsNullOrEmpty(guidAttr))
			{
				Guid guid = (Guid)m_gconv.ConvertFrom(guidAttr);
				return m_factCmPerson.Create(guid, owner as ICmPossibilityList);
			}
			else
			{
				ICmPerson csd = m_factCmPerson.Create();
				if (owner is ICmPossibility)
					(owner as ICmPossibility).SubPossibilitiesOS.Add(csd);
				else
					(owner as ICmPossibilityList).PossibilitiesOS.Add(csd);
				return csd;
			}
		}

		private int GetWsFromStr(string sWs)
		{
			return m_cache.ServiceLocator.WritingSystemManager.GetWsFromStr(sWs);
		}
		#endregion // Methods for getting or creating model objects

		//==============================================================================
		#region internal classes
		internal abstract class ConflictingData
		{
			protected string m_sType;
			protected string m_sField;
			protected FlexLiftMerger m_merger;

			protected ConflictingData(string sType, string sField, FlexLiftMerger merger)
			{
				m_sType = sType;
				m_sField = sField;
				m_merger = merger;
			}
			public string ConflictType
			{
				get { return m_sType; }
			}
			public string ConflictField
			{
				get { return m_sField; }
			}
			public abstract string OrigHtmlReference();
			public abstract string DupHtmlReference();

		}

		internal static string LinkRef(ILexEntry le)
		{
			FwLinkArgs link = new FwLinkArgs("lexiconEdit", le.Guid);
			return XmlUtils.MakeSafeXmlAttribute(link.ToString());
		}

		internal class ConflictingEntry : ConflictingData
		{
			private ILexEntry m_leOrig;
			private ILexEntry m_leNew;

			public ConflictingEntry(string sField, ILexEntry leOrig, FlexLiftMerger merger)
				: base(LexTextControls.ksEntry, sField, merger)
			{
				m_leOrig = leOrig;
			}
			public override string OrigHtmlReference()
			{
				//"<a href=\"{0}\">{1}</a>", LinkRef(m_leOrig), HtmlString(m_leOrig.Headword)
				return String.Format("<a href=\"{0}\">{1}</a>", LinkRef(m_leOrig),
					m_merger.TsStringAsHtml(m_leOrig.HeadWord, m_leOrig.Cache));
			}

			public override string DupHtmlReference()
			{
				//"<a href=\"{0}\">{1}</a>", LinkRef(m_leNew), HtmlString(m_leNew.Headword)
				return String.Format("<a href=\"{0}\">{1}</a>", LinkRef(m_leNew),
					m_merger.TsStringAsHtml(m_leNew.HeadWord, m_leNew.Cache));
			}
			public ILexEntry DupEntry
			{
				set { m_leNew = value; }
			}
		}

		internal class ConflictingSense : ConflictingData
		{
			private ILexSense m_lsOrig;
			private ILexSense m_lsNew;
			public ConflictingSense(string sField, ILexSense lsOrig, FlexLiftMerger merger)
				: base(LexTextControls.ksSense, sField, merger)
			{
				m_lsOrig = lsOrig;
			}
			public override string OrigHtmlReference()
			{
				return String.Format("<a href=\"{0}\">{1}</a>",
					LinkRef(m_lsOrig.Entry),
					m_merger.TsStringAsHtml(OwnerOutlineName(m_lsOrig), m_lsOrig.Cache));
			}

			public override string DupHtmlReference()
			{
				return String.Format("<a href=\"{0}\">{1}</a>",
					LinkRef(m_lsNew.Entry),
					m_merger.TsStringAsHtml(OwnerOutlineName(m_lsNew), m_lsNew.Cache));
			}
			public ILexSense DupSense
			{
				set { m_lsNew = value; }
			}

			private ITsString OwnerOutlineName(ILexSense m_lsOrig)
			{
				return m_lsOrig.OwnerOutlineNameForWs(m_lsOrig.Cache.DefaultVernWs);
			}
		}

		/// <summary>
		/// This is the base class for pending error reports.
		/// </summary>
		class PendingErrorReport
		{
			protected Guid m_guid;
			protected int m_flid;
			int m_ws;
			protected LcmCache m_cache;
			private FlexLiftMerger m_merger;

			internal PendingErrorReport(Guid guid, int flid, int ws, LcmCache cache, FlexLiftMerger merger)
			{
				m_guid = guid;
				m_flid = flid;
				m_ws = ws;
				m_cache = cache;
				m_merger = merger;
			}

			internal virtual string FieldName
			{
				get
				{
					// TODO: make this more informative and user-friendly.
					return m_cache.MetaDataCacheAccessor.GetFieldName((int)m_flid);
				}
			}

			private ILexEntry Entry()
			{
				ICmObject cmo = m_merger.GetObjectForGuid(m_guid);
				if (cmo is ILexEntry)
				{
					return cmo as ILexEntry;
				}
				else
				{
					return cmo.OwnerOfClass<ILexEntry>();
				}
			}

			internal string EntryHtmlReference()
			{
				ILexEntry le = Entry();
				if (le == null)
					return String.Empty;
				else
					return String.Format("<a href=\"{0}\">{1}</a>",
						LinkRef(le),
						m_merger.TsStringAsHtml(le.HeadWord, m_cache));

			}

			internal string WritingSystem
			{
				get
				{
					if (m_ws > 0)
					{
						CoreWritingSystemDefinition ws = m_merger.GetExistingWritingSystem(m_ws);
						return ws.DisplayLabel;
					}
					else
					{
						return null;
					}
				}
			}

			public override bool Equals(object obj)
			{
				PendingErrorReport that = obj as PendingErrorReport;
				if (that != null && m_flid == that.m_flid && m_guid == that.m_guid &&
					m_ws == that.m_ws)
				{
					if (m_cache != null && that.m_cache != null)
					{
						return m_cache == that.m_cache;
					}
					else
					{
						return m_cache == null && that.m_cache == null;
					}
				}
				return false;
			}

			public override int GetHashCode()
			{
				return m_flid + m_ws + m_guid.GetHashCode() + (m_cache == null ? 0 : m_cache.GetHashCode());
			}
		}

		/// <summary>
		/// This class stores the data needed to construct a warning message to the user
		/// that some imported data is actually invalid.
		/// </summary>
		class InvalidData : PendingErrorReport
		{
			string m_sMsg;
			string m_sValue;

			public InvalidData(string sMsg, Guid guid, int flid, string val, int ws, LcmCache cache, FlexLiftMerger merger)
				: base(guid, flid, ws, cache, merger)
			{
				m_sMsg = sMsg;
				m_sValue = val;
			}

			internal string ErrorMessage
			{
				get { return m_sMsg; }
			}

			internal string BadValue
			{
				get { return m_sValue; }
			}

			public override bool Equals(object obj)
			{
				InvalidData that = obj as InvalidData;
				return that != null && m_sMsg == that.m_sMsg && m_sValue == that.m_sValue &&
					base.Equals(obj);
			}

			public override int GetHashCode()
			{
				return base.GetHashCode() + (m_sMsg == null ? 0 : m_sMsg.GetHashCode()) +
					(m_sValue == null ? 0 : m_sValue.GetHashCode());
			}
		}
		List<InvalidData> m_rgInvalidData = new List<InvalidData>();

		/// <summary>
		/// This class stores the data needed to construct a warning message to the user
		/// that a relation element in the imported file is invalid.
		/// </summary>
		class InvalidRelation : PendingErrorReport
		{
			PendingLexEntryRef m_pendRef;

			public InvalidRelation(PendingLexEntryRef pend, LcmCache cache, FlexLiftMerger merger)
				: base(pend.CmObject.Guid, 0, 0, cache, merger)
			{
				m_pendRef = pend;
			}

			internal string TypeName
			{
				get { return m_pendRef.RelationType; }
			}

			internal string BadValue
			{
				get { return m_pendRef.TargetId; }
			}

			internal string ErrorMessage
			{
				get
				{
					if (m_pendRef.CmObject is ILexEntry)
						return LexTextControls.ksEntryInvalidRef;

					Debug.Assert(m_pendRef is ILexSense);
					return String.Format(LexTextControls.ksSenseInvalidRef,
						((ILexSense)m_pendRef.CmObject).OwnerOutlineNameForWs(
						m_cache.ServiceLocator.WritingSystems.DefaultVernacularWritingSystem.Handle).Text);
				}
			}

			internal override string FieldName
			{
				get { return String.Empty; }
			}
		}
		List<InvalidRelation> m_rgInvalidRelation = new List<InvalidRelation>();

		 /// <summary>
		/// This class stores the data needed to construct a warning message to the user
		/// that a relation element in the imported file is invalid.
		/// </summary>
		class CombinedCollection : PendingErrorReport
		{

			private ICmObject _cmObject;
			public CombinedCollection(ICmObject owner, LcmCache cache, FlexLiftMerger merger)
				: base(owner.Guid, 0, 0, cache, merger)
			{
				_cmObject = owner;
			}

			internal string TypeName
			{
				get; set;
			}

			internal string BadValue
			{
				get; set;
			}

			internal string ErrorMessage
			{
				get { return String.Format(LexTextControls.ksAddedToCombinedCollection, BadValue, TypeName, EntryHtmlReference()); }
			}
		}
		List<CombinedCollection> m_combinedCollections = new List<CombinedCollection>();

		/// <summary>
		/// This class stores the information for one range element from a *-feature-value range.
		/// This is used only if the corresponding IFsClosedFeature object cannot be found.
		/// </summary>
		internal class PendingFeatureValue
		{
			readonly string m_featId;
			readonly string m_id;
			readonly string m_catalogId;
			readonly bool m_fShowInGloss;
			readonly LiftMultiText m_abbrev; // safe XML
			readonly LiftMultiText m_label; // safe XML
			readonly LiftMultiText m_description; // safe XML
			readonly Guid m_guidLift;

			/// <summary>
			///
			/// </summary>
			/// <param name="featId"></param>
			/// <param name="id"></param>
			/// <param name="description">safe XML</param>
			/// <param name="label">safe XML</param>
			/// <param name="abbrev">safe XML</param>
			/// <param name="catalogId"></param>
			/// <param name="fShowInGloss"></param>
			/// <param name="guidLift"></param>
			internal PendingFeatureValue(string featId, string id, LiftMultiText description,
				LiftMultiText label, LiftMultiText abbrev, string catalogId, bool fShowInGloss,
				Guid guidLift)
			{
				m_featId = featId;
				m_id = id;
				m_catalogId = catalogId;
				m_fShowInGloss = fShowInGloss;
				m_abbrev = abbrev;
				m_label = label;
				m_description = description;
				m_guidLift = guidLift;
			}
			internal string FeatureId
			{
				get { return m_featId; }
			}
			internal string Id
			{
				get { return m_id; }
			}
			internal string CatalogId
			{
				get { return m_catalogId; }
			}
			internal bool ShowInGloss
			{
				get { return m_fShowInGloss; }
			}
			internal LiftMultiText Abbrev // safe XML
			{
				get { return m_abbrev; }
			}
			internal LiftMultiText Label // safe XML
			{
				get { return m_label; }
			}
			internal LiftMultiText Description // safe XML
			{
				get { return m_description; }
			}
			internal Guid LiftGuid
			{
				get { return m_guidLift; }
			}
		}

		readonly List<PendingFeatureValue> m_rgPendingSymFeatVal = new List<PendingFeatureValue>();

		/// <summary>
		/// This stores information for a relation that will be set later because the
		/// target object may not have been imported yet.
		/// </summary>
		internal class PendingRelation
		{
			ICmObject m_cmo;
			ICmObject m_cmoTarget;
			readonly CmLiftRelation m_rel;
			string m_sResidue;

			public PendingRelation(ICmObject obj, CmLiftRelation rel, string sResidue)
			{
				m_cmo = obj;
				m_cmoTarget = null;
				m_rel = rel;
				m_sResidue = sResidue;
			}

			public ICmObject CmObject
			{
				get { return m_cmo; }
			}

			public int ObjectHvo
			{
				get { return m_cmo == null ? 0 : m_cmo.Hvo; }
			}

			public string RelationType
			{
				get { return m_rel.Type; }
			}

			public string TargetId
			{
				get { return m_rel.Ref; }
			}

			public ICmObject Target
			{
				get { return m_cmoTarget; }
				set { m_cmoTarget = value; }
			}

			public int TargetHvo
			{
				get { return m_cmoTarget == null ? 0 : m_cmoTarget.Hvo; }
			}

			public string Residue
			{
				get { return m_sResidue; }
			}

			public DateTime DateCreated
			{
				get { return m_rel.DateCreated; }
			}

			public DateTime DateModified
			{
				get { return m_rel.DateModified; }
			}

			internal bool IsSameOrMirror(PendingRelation rel)
			{
				if (rel == this)
					return true;
				if (rel.RelationType != RelationType)
					return false;
				if (rel.ObjectHvo == ObjectHvo && rel.Target == Target)
					return true;
				if (rel.ObjectHvo == TargetHvo && rel.Target == CmObject)
					return true;
				return false;
			}

			internal void MarkAsProcessed()
			{
				m_cmo = null;
			}

			internal bool HasBeenProcessed()
			{
				return m_cmo == null;
			}

			internal bool IsSequence
			{
				get { return m_rel.Order >= 0; }
			}

			internal int Order
			{
				get { return m_rel.Order; }
			}

			public override string ToString()
			{
				return String.Format("PendingRelation: type=\"{0}\", order={1}, target={2}, objHvo={3}",
					m_rel.Type, m_rel.Order, (m_cmoTarget == null ? 0 : m_cmoTarget.Hvo),
					(m_cmo == null ? 0 : m_cmo.Hvo));
			}

			internal string AsResidueString()
			{
				if (m_sResidue == null)
					m_sResidue = String.Empty;
				if (IsSequence)
				{
					return String.Format("<relation type=\"{0}\" ref=\"{1}\" order=\"{2}\"/>{3}",
						XmlUtils.MakeSafeXmlAttribute(m_rel.Type),
						XmlUtils.MakeSafeXmlAttribute(m_rel.Ref),
						m_rel.Order, Environment.NewLine);
				}
				return String.Format("<relation type=\"{0}\" ref=\"{1}\"/>{2}",
					XmlUtils.MakeSafeXmlAttribute(m_rel.Type),
					XmlUtils.MakeSafeXmlAttribute(m_rel.Ref), Environment.NewLine);
			}
		}
		readonly List<PendingRelation> m_rgPendingRelation = new List<PendingRelation>();
		readonly List<PendingRelation> m_rgPendingTreeTargets = new List<PendingRelation>();
		readonly LinkedList<PendingRelation> m_rgPendingCollectionRelations = new LinkedList<PendingRelation>();

		/// <summary>
		///
		/// </summary>
		internal class PendingLexEntryRef
		{
			readonly ICmObject m_cmo;
			ICmObject m_cmoTarget;
			readonly CmLiftRelation m_rel;
			readonly List<string> m_rgsComplexFormTypes = new List<string>();
			readonly List<string> m_rgsVariantTypes = new List<string>();
			bool m_fIsPrimary;
			int m_nHideMinorEntry;
			private LiftMultiText m_lexemeForm; // safe XML

			string m_sResidue;
			// preserve trait values from older LIFT files based on old FieldWorks model
			readonly string m_sEntryType;
			readonly string m_sMinorEntryCondition;
			bool m_fExcludeAsHeadword;
			LiftField m_summary; // safe-XML

			public PendingLexEntryRef(ICmObject obj, CmLiftRelation rel, CmLiftEntry entry)
			{
				m_cmo = obj;
				m_rel = rel;
				m_sResidue = null;
				m_sEntryType = null;
				m_sMinorEntryCondition = null;
				m_fExcludeAsHeadword = false;
				m_summary = null;
				if (entry != null)
				{
					m_sEntryType = entry.EntryType;
					m_sMinorEntryCondition = entry.MinorEntryCondition;
					m_fExcludeAsHeadword = entry.ExcludeAsHeadword;
					ProcessRelationData();
					m_lexemeForm = entry.LexicalForm;
				}
			}

			private void ProcessRelationData()
			{
				List<LiftTrait> knownTraits = new List<LiftTrait>();
				foreach (LiftTrait trait in m_rel.Traits)
				{
					switch (trait.Name)
					{
						case "complex-form-type":
							m_rgsComplexFormTypes.Add(trait.Value);
							knownTraits.Add(trait);
							break;
						case "variant-type":
							m_rgsVariantTypes.Add(trait.Value);
							knownTraits.Add(trait);
							break;
						case "hide-minor-entry":
							Int32.TryParse(trait.Value, out m_nHideMinorEntry);
							knownTraits.Add(trait);
							break;
						case "is-primary":
							m_fIsPrimary = (trait.Value.ToLowerInvariant() == "true");
							m_fExcludeAsHeadword = m_fIsPrimary;
							knownTraits.Add(trait);
							break;
					}
				}
				foreach (LiftTrait trait in knownTraits)
					m_rel.Traits.Remove(trait);
				List<LiftField> knownFields = new List<LiftField>();
				foreach (LiftField field in m_rel.Fields)
				{
					if (field.Type == "summary")
					{
						m_summary = field; // this had better be safe-XML
						knownFields.Add(field);
					}
				}
				foreach (LiftField field in knownFields)
					m_rel.Fields.Remove(field);
			}

			public ICmObject CmObject
			{
				get { return m_cmo; }
			}

			public int ObjectHvo
			{
				get { return m_cmo == null ? 0 : m_cmo.Hvo; }
			}

			public string RelationType
			{
				get { return m_rel.Type; }
			}

			public string TargetId
			{
				get { return m_rel.Ref; }
			}

			public int TargetHvo
			{
				get { return m_cmoTarget == null ? 0 : m_cmoTarget.Hvo; }
			}

			public ICmObject Target
			{
				get { return m_cmoTarget; }
				set { m_cmoTarget = value; }
			}

			public string Residue
			{
				get { return m_sResidue; }
				set { m_sResidue = value; }
			}

			public DateTime DateCreated
			{
				get { return m_rel.DateCreated; }
			}

			public DateTime DateModified
			{
				get { return m_rel.DateModified; }
			}

			internal int Order
			{
				get { return m_rel.Order; }
			}

			public string EntryType
			{
				get { return m_sEntryType; }
			}

			public string MinorEntryCondition
			{
				get { return m_sMinorEntryCondition; }
			}

			public bool ExcludeAsHeadword
			{
				get { return m_fExcludeAsHeadword; }
			}

			public List<string> ComplexFormTypes
			{
				get { return m_rgsComplexFormTypes; }
			}

			public List<string> VariantTypes
			{
				get { return m_rgsVariantTypes; }
			}

			public bool IsPrimary
			{
				get { return m_fIsPrimary; }
				set { m_fIsPrimary = value; }
			}

			public int HideMinorEntry
			{
				get { return m_nHideMinorEntry; }
				set { m_nHideMinorEntry = value; }
			}

			/// <summary>
			/// safe-XML
			/// </summary>
			public LiftField Summary
			{
				get { return m_summary; }
			}

			/// <summary>
			/// This is used to better error reporting to the user when the default vernacular of the LIFT import file
			/// does not match the project doing the import.
			/// safe-XML
			/// </summary>
			public LiftMultiText LexemeForm
			{
				get { return m_lexemeForm; }
			}
		}
		readonly List<PendingLexEntryRef> m_rgPendingLexEntryRefs = new List<PendingLexEntryRef>();

		internal class PendingModifyTime
		{
			readonly ILexEntry m_le;
			readonly DateTime m_dt;

			public PendingModifyTime(ILexEntry le, DateTime dt)
			{
				m_le = le;
				m_dt = dt;
			}

			public void SetModifyTime()
			{
				if (!AreDatesInSameSecond(m_le.DateModified.ToUniversalTime(), m_dt.ToUniversalTime()))
				{
					m_le.DateModified = m_dt;
				}
			}
		}
		readonly List<PendingModifyTime> m_rgPendingModifyTimes = new List<PendingModifyTime>();

		private int m_cEntriesAdded;
		private int m_cSensesAdded;
		private int m_cEntriesDeleted;
		private DateTime m_dtStart;		// when import started
		/// <summary>
		/// This stores the information for one object's LIFT import residue.
		/// </summary>
		class LiftResidue
		{
			readonly int m_flid;
			readonly XmlDocument m_xdoc;

			public LiftResidue(int flid, XmlDocument xdoc)
			{
				m_flid = flid;
				m_xdoc = xdoc;
			}

			public int Flid
			{
				get { return m_flid; }
			}

			public XmlDocument Document
			{
				get { return m_xdoc; }
			}
		}
		readonly Dictionary<int, LiftResidue> m_dictResidue = new Dictionary<int, LiftResidue>();

		#endregion //internal classes
	}

	#region Category catalog class
	public class EticCategory
	{
		string m_id;
		string m_parent;
		readonly Dictionary<string, string> m_dictName = new Dictionary<string, string>();	// term
		readonly Dictionary<string, string> m_dictAbbrev = new Dictionary<string, string>();	// abbrev
		readonly Dictionary<string, string> m_dictDesc = new Dictionary<string, string>();	// def

		public string Id
		{
			get { return m_id; }
			set { m_id = value; }
		}
		public string ParentId
		{
			get { return m_parent; }
			set { m_parent = value; }
		}
		/// <summary>
		/// Dictionary mapping WS ids to non-safe-XML strings.
		/// </summary>
		public Dictionary<string, string> MultilingualName
		{
			get { return m_dictName; }
		}
		/// <summary>
		///
		/// </summary>
		/// <param name="lang"></param>
		/// <param name="name">non-safe-XML</param>
		public void SetName(string lang, string name)
		{
			if (m_dictName.ContainsKey(lang))
				m_dictName[lang] = name;
			else
				m_dictName.Add(lang, name);
		}
		public Dictionary<string, string> MultilingualAbbrev
		{
			get { return m_dictAbbrev; }
		}
		/// <summary>
		///
		/// </summary>
		/// <param name="lang"></param>
		/// <param name="abbrev">safe-XmL</param>
		public void SetAbbrev(string lang, string abbrev)
		{
			if (m_dictAbbrev.ContainsKey(lang))
				m_dictAbbrev[lang] = abbrev;
			else
				m_dictAbbrev.Add(lang, abbrev);
		}

		/// <summary>
		/// Values are non-safe XML
		/// </summary>
		public Dictionary<string, string> MultilingualDesc
		{
			get { return m_dictDesc; }
		}

		/// <summary>
		///
		/// </summary>
		/// <param name="lang"></param>
		/// <param name="desc">non-safe-XML</param>
		public void SetDesc(string lang, string desc)
		{
			if (m_dictDesc.ContainsKey(lang))
				m_dictDesc[lang] = desc;
			else
				m_dictDesc.Add(lang, desc);
		}
	}
	#endregion // Category catalog class

	public class LdmlFileBackup
	{
		/// <summary>
		/// Copy a complete directory, including all contents recursively.
		/// Everything in out put will be writeable, even if some input files are read-only.
		/// </summary>
		/// <param name="sourcePath"></param>
		/// <param name="targetPath"></param>
		public static void CopyDirectory(string sourcePath, string targetPath)
		{
			CopyDirectory(new DirectoryInfo(sourcePath), new DirectoryInfo(targetPath));
		}

		private static void CopyDirectory(DirectoryInfo source, DirectoryInfo target)
		{
			// Check if the target directory exists, if not, create it.
			if (Directory.Exists(target.FullName) == false)
			{
				Directory.CreateDirectory(target.FullName);
			}

			// Copy each file into its new directory.
			foreach (FileInfo fi in source.GetFiles())
			{
				var destFileName = Path.Combine(target.ToString(), fi.Name);
				fi.CopyTo(destFileName, true);
				File.SetAttributes(destFileName, FileAttributes.Normal); // don't want to copy readonly property.
			}

			// Copy each subdirectory using recursion.
			foreach (DirectoryInfo diSourceSubDir in source.GetDirectories())
			{
				DirectoryInfo nextTargetSubDir = target.CreateSubdirectory(diSourceSubDir.Name);
				CopyDirectory(diSourceSubDir, nextTargetSubDir);
			}
		}

		/// <summary>
		/// Delete all files in a directory and all subfolders
		/// </summary>
		/// <param name="sourcePath"></param>
		public static void DeleteDirectory(string sourcePath)
		{
			DeleteDirectory(new DirectoryInfo(sourcePath));
		}
		/// <summary>
		/// Delete all files in a directory and all subfolders
		/// </summary>
		/// <param name="source"></param>
		private static void DeleteDirectory(DirectoryInfo source)
		{
			foreach (DirectoryInfo diSourceSubDir in source.GetDirectories())
			{
				DeleteDirectory(diSourceSubDir);
			}
			foreach (FileInfo fi in source.GetFiles())
			{
				fi.Delete();
			}
		}
	}

	#region LIFT model classes

	/// <summary>
	/// This class implements "phonetic" from the LIFT standard.
	/// It corresponds to LexPronunciation in the FieldWorks model.
	/// </summary>
	public class CmLiftPhonetic : LiftObject, ICmLiftObject
	{
		public CmLiftPhonetic()
		{
			Media = new List<LiftUrlRef>();
		}

		public ICmObject CmObject { get; set; }

		public LiftMultiText Form { get; set; } // safe XML

		public List<LiftUrlRef> Media { get; private set; }

		public override string XmlTag
		{
			get { return "pronunciation"; }
		}
	}

	/// <summary>
	/// This class implements "note" from the LIFT standard.
	/// It doesn't really correspond to any CmObject in the FieldWorks model.
	/// </summary>
	public class CmLiftNote : LiftObject
	{
		public CmLiftNote()
		{
		}
		/// <summary>
		///
		/// </summary>
		/// <param name="type"></param>
		/// <param name="contents">safe XML</param>
		public CmLiftNote(string type, LiftMultiText contents)
		{
			Type = type;
			Content = contents;
		}

		public string Type { get; set; }

		public LiftMultiText Content { get; set; } // safe XML

		public override string XmlTag
		{
			get { return "note"; }
		}
	}

	/// <summary>
	/// This class implements "sense" from the LIFT standard.
	/// It corresponds to LexSense from the FieldWorks model.
	/// </summary>
	public class CmLiftSense : LiftObject, ICmLiftObject
	{
		public CmLiftSense()
		{
			Subsenses = new List<CmLiftSense>();
			Illustrations = new List<LiftUrlRef>();
			Reversals = new List<CmLiftReversal>();
			Examples = new List<CmLiftExample>();
			Notes = new List<CmLiftNote>();
			Relations = new List<CmLiftRelation>();
		}

		public CmLiftSense(Extensible info, Guid guid, LiftObject owner, FlexLiftMerger merger)
		{
			Subsenses = new List<CmLiftSense>();
			Illustrations = new List<LiftUrlRef>();
			Reversals = new List<CmLiftReversal>();
			Examples = new List<CmLiftExample>();
			Notes = new List<CmLiftNote>();
			Relations = new List<CmLiftRelation>();
			Id = info.Id;
			Guid = guid;
			if (guid == Guid.Empty)
				CmObject = null;
			else
				CmObject = merger.GetObjectForGuid(guid);
			DateCreated = info.CreationTime;
			DateModified = info.ModificationTime;
			Owner = owner;
		}

		public ICmObject CmObject { get; set; }

		public int Order { get; set; }

		public LiftGrammaticalInfo GramInfo { get; set; }

		public LiftMultiText Gloss { get; set; }  // safe-XML

		public LiftMultiText Definition { get; set; } // safe-XML

		public List<CmLiftRelation> Relations { get; private set; }

		public List<CmLiftNote> Notes { get; private set; }

		public List<CmLiftExample> Examples { get; private set; }

		public List<CmLiftReversal> Reversals { get; private set; }

		public List<LiftUrlRef> Illustrations { get; private set; }

		public List<CmLiftSense> Subsenses { get; private set; }

		public LiftObject Owner { get; private set; }

		public CmLiftEntry OwningEntry
		{
			get
			{
				LiftObject owner;
				for (owner = Owner; owner is CmLiftSense; owner = (owner as CmLiftSense).Owner)
				{
				}
				return owner as CmLiftEntry;
			}
		}

		public override string XmlTag
		{
			get { return "sense/subsense"; }
		}
	}

	/// <summary>
	/// This class implements "reversal" from the LIFT standard.
	/// It roughly corresponds to ReversalIndexEntry in the FieldWorks model.
	/// </summary>
	public class CmLiftReversal : LiftObject, ICmLiftObject
	{
		public ICmObject CmObject { get; set; }

		public string Type { get; set; }

		public LiftMultiText Form { get; set; } // safe XML

		public CmLiftReversal Main { get; set; }

		public LiftGrammaticalInfo GramInfo { get; set; }

		public override string XmlTag
		{
			get { return "reversal"; }
		}
	}

	public interface ICmLiftObject
	{
		ICmObject CmObject { get; set; }
	}

	/// <summary>
	/// This class implements "example" from the LIFT standard.
	/// It corresponds to LexExampleSentence in the FieldWorks model.
	/// </summary>
	public class CmLiftExample : LiftObject, ICmLiftObject
	{
		public CmLiftExample()
		{
			Notes = new List<CmLiftNote>();
			Translations = new List<LiftTranslation>();
		}

		public ICmObject CmObject { get; set; }

		public string Source { get; set; }

		public LiftMultiText Content { get; set; } // safe XML

		public List<LiftTranslation> Translations { get; private set; }

		public List<CmLiftNote> Notes { get; private set; }

		public override string XmlTag
		{
			get { return "example"; }
		}
	}

	/// <summary>
	/// This class implements "variant" from the LIFT standard.  (It represents an allomorph, not what
	/// FieldWorks understands to be a Variant.)
	/// It corresponds to MoForm (or one of its subclasses) in the FieldWorks model.
	/// </summary>
	public class CmLiftVariant : LiftObject, ICmLiftObject
	{
		public CmLiftVariant()
		{
			Relations = new List<CmLiftRelation>();
			Pronunciations = new List<CmLiftPhonetic>();
		}

		public ICmObject CmObject { get; set; }

		public string Ref { get; set; }

		public LiftMultiText Form { get; set; } // safe XML

		public List<CmLiftPhonetic> Pronunciations { get; private set; }

		public List<CmLiftRelation> Relations { get; private set; }

		public string RawXml { get; set; }

		public override string XmlTag
		{
			get { return "variant"; }
		}
	}

	/// <summary>
	/// This class implements "relation" from the LIFT standard.
	/// It relates to LexRelation or LexEntryRef in the FieldWorks model.
	/// </summary>
	public class CmLiftRelation : LiftObject, ICmLiftObject
	{
		public CmLiftRelation()
		{
			Order = -1;
		}

		public ICmObject CmObject { get; set; }

		public string Type { get; set; }

		public string Ref { get; set; }

		public int Order { get; set; }

		public LiftMultiText Usage { get; set; } // safe XML

		public override string XmlTag
		{
			get { return "relation"; }
		}
	}

	/// <summary>
	/// This class implements "etymology" from the LIFT standard.
	/// It corresponds to LexEtymology in the FieldWorks model.
	/// </summary>
	public class CmLiftEtymology : LiftObject, ICmLiftObject
	{
		public string Type { get; set; }

		public string Source { get; set; }

		public LiftMultiText Gloss { get; set; } // safe XML

		public LiftMultiText Form { get; set; } // safe XML

		public ICmObject CmObject { get; set; }

		public override string XmlTag
		{
			get { return "etymology"; }
		}
	}

	/// <summary>
	/// This class implements "entry" from the LIFT standard.
	/// It corresponds to LexEntry in the FieldWorks model.
	/// </summary>
	public class CmLiftEntry : LiftObject, ICmLiftObject
	{
		public CmLiftEntry()
		{
			Etymologies = new List<CmLiftEtymology>();
			Relations = new List<CmLiftRelation>();
			Notes = new List<CmLiftNote>();
			Senses = new List<CmLiftSense>();
			Variants = new List<CmLiftVariant>();
			Pronunciations = new List<CmLiftPhonetic>();
			Order = 0;
			DateDeleted = DateTime.MinValue;
		}
		public CmLiftEntry(Extensible info, Guid guid, int order, FlexLiftMerger merger)
		{
			Etymologies = new List<CmLiftEtymology>();
			Relations = new List<CmLiftRelation>();
			Notes = new List<CmLiftNote>();
			Senses = new List<CmLiftSense>();
			Variants = new List<CmLiftVariant>();
			Pronunciations = new List<CmLiftPhonetic>();
			Id = info.Id;
			Guid = guid;
			if (guid == Guid.Empty)
				CmObject = null;
			else
				CmObject = merger.GetObjectForGuid(guid);
			DateCreated = info.CreationTime;
			DateModified = info.ModificationTime;
			DateDeleted = DateTime.MinValue;
			Order = order;
		}

		public ICmObject CmObject { get; set; }

		public int Order { get; set; }

		public DateTime DateDeleted { get; set; }

		public LiftMultiText LexicalForm { get; set; } // safe-XML

		public LiftMultiText CitationForm { get; set; } // safe-XML

		public List<CmLiftPhonetic> Pronunciations { get; private set; }

		public List<CmLiftVariant> Variants { get; private set; }

		public List<CmLiftSense> Senses { get; private set; }

		public List<CmLiftNote> Notes { get; private set; }

		public List<CmLiftRelation> Relations { get; private set; }

		public List<CmLiftEtymology> Etymologies { get; private set; }

		public override string XmlTag
		{
			get { return "entry"; }
		}

		public string EntryType { get; set; }

		public string MinorEntryCondition { get; set; }

		public bool ExcludeAsHeadword { get; set; }
	}
	#endregion // LIFT model classes
}
