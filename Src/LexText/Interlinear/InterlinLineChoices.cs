// Copyright (c) 2015-2018 SIL International
// This software is licensed under the LGPL, version 2.1 or later
// (http://www.gnu.org/licenses/lgpl-2.1.html)
// NOTE: whenever this class is updated to include the tagging line(s), InterlinClipboardHelper
// (in InterlinDocView.cs) has to be fixed.  It currently hacks up a solution for a single
// tagging line when it thinks it needs to do so.
// Of course, it might be better implemented overall by a CollectorEnv approach...
using System;
using System.Collections;
using System.Collections.Generic;
using SIL.LCModel;
using SIL.LCModel.DomainServices;
using SIL.FieldWorks.FdoUi;
using SIL.LCModel.Utils;
using System.Drawing;
using System.Diagnostics;
using System.Linq;
using System.Text;
using SIL.LCModel.Core.Cellar;
using SIL.LCModel.Core.Text;
using SIL.LCModel.Core.WritingSystems;
using SIL.FieldWorks.Common.Controls;
using SIL.LCModel.Core.KernelInterfaces;
using SIL.LCModel.Infrastructure;
using XCore;

namespace SIL.FieldWorks.IText
{
	/// <summary>
	/// This class specifies the fields to display in an interlinear text, and which writing systems
	/// to display, where relevant. It has an indexer, and item(n) is the spec of what to show as
	/// the nth line. This class also has the knowledge about allowed orders.
	/// </summary>
	public class InterlinLineChoices : ICloneable
	{
		Color kMorphLevelColor = Color.Purple;
		Color kWordLevelColor = Color.Blue;
		private List<InterlinLineSpec> m_allLineSpecs = new List<InterlinLineSpec>();
		internal List<LineOption> m_allLineOptions = new List<LineOption>();
		internal List<InterlinLineSpec> m_specs = new List<InterlinLineSpec>();
		internal int m_wsDefVern; // The default vernacular writing system.
		internal int m_wsDefAnal; // The default analysis writing system.
		internal ILangProject m_proj;	// provides more ws info.
		internal LcmCache m_cache;
		Dictionary<int, string> m_fieldNames = new Dictionary<int, string>();
		InterlinMode m_mode = InterlinMode.Analyze;

		public InterlinLineChoices(ILangProject proj, int defaultVernacularWs, int defaultAnalysisWs)
			: this(proj, defaultVernacularWs, defaultAnalysisWs, InterlinMode.Analyze)
		{
		}

		public InterlinLineChoices(LcmCache cache, int defaultVernacularWs, int defaultAnalysisWs)
			: this(cache, defaultVernacularWs, defaultAnalysisWs, InterlinMode.Analyze)
		{
		}

		public InterlinLineChoices(LcmCache cache, int defaultVernacularWs, int defaultAnalysisWs, InterlinMode mode)
		{
			m_cache = cache;
			this.Mode = mode;
			UpdateFieldNamesFromLines(mode);
			m_wsDefVern = defaultVernacularWs;
			m_wsDefAnal = defaultAnalysisWs == WritingSystemServices.kwsAnal ? m_cache.DefaultAnalWs : defaultAnalysisWs;
			AllLineOptions = LineOptions(mode).ToList();
		}

		public InterlinLineChoices(ILangProject proj, int defaultVernacularWs, int defaultAnalysisWs, InterlinMode mode)
			: this(proj.Cache, defaultVernacularWs, defaultAnalysisWs, mode)
		{
			m_proj = proj; // Not used any more. TODO: remove, and modify callers.
		}

		/// <summary>
		/// The mode that the configured lines are in.
		/// If the mode changes, we'll reinitialze the fieldname (label) info.
		/// </summary>
		public InterlinMode Mode
		{
			get { return m_mode; }
			set
			{
				if (m_mode == value)
					return;
				m_mode = value;
				// recompute labels for flids
				UpdateFieldNamesFromLines(m_mode);
			}
		}

		internal List<LineOption> AllLineOptions
		{
			get { return m_allLineOptions; }
			set
			{
				m_allLineOptions = value;

				// AllLineOptions and AllLineSpecs will be identical
				// On a set of AllLineOptions, AllLineSpecs will also be updated.
				var newLineSpecs = new List<InterlinLineSpec>();
				foreach (var option in value)
				{
					newLineSpecs.Add(CreateSpec(option.Flid, 0));
				}
				m_allLineSpecs = newLineSpecs;
			}
		}

		internal List<InterlinLineSpec> AllLineSpecs => m_allLineSpecs;

		/// <summary>
		/// Count previous occurrences of the flid at the specified index.
		/// </summary>
		/// <param name="index"></param>
		/// <returns></returns>
		public int PreviousOccurrences(int index)
		{
			int prev = 0;
			for (int i = 0; i < index; i++)
				if (this[i].Flid == this[index].Flid)
					prev++;
			return prev;
		}

		public static InterlinLineChoices DefaultChoices(ILangProject proj, int vern, int analysis)
		{
			return DefaultChoices(proj, vern, analysis, InterlinMode.Analyze);
		}

		public enum InterlinMode
		{
			Analyze,
			Gloss,
			GlossAddWordsToLexicon,
			Chart
		}

		public static InterlinLineChoices DefaultChoices(ILangProject proj, int vern, int analysis, InterlinMode mode)
		{
			InterlinLineChoices result = new InterlinLineChoices(proj, vern, analysis, mode);
			switch (mode)
			{
				case InterlinMode.Analyze:
					result.SetStandardState();
					break;
				case InterlinMode.Chart:
					result.SetStandardChartState();
					break;
				case InterlinMode.Gloss:
				case InterlinMode.GlossAddWordsToLexicon:
					result.SetStandardGlossState();
					break;
			}
			return result;
		}

		public void SetStandardChartState()
		{
			m_specs.Clear();
			Add(InterlinLineChoices.kflidWord);
			Add(InterlinLineChoices.kflidWordGloss);
			Add(InterlinLineChoices.kflidMorphemes);
			Add(InterlinLineChoices.kflidLexGloss);
			Add(InterlinLineChoices.kflidLexEntries);
			Add(InterlinLineChoices.kflidLexPos);
		}

		public void SetStandardState()
		{
			m_specs.Clear();
			Add(InterlinLineChoices.kflidWord); // 0
			Add(InterlinLineChoices.kflidMorphemes); // 1
			Add(InterlinLineChoices.kflidLexEntries); // 2
			Add(InterlinLineChoices.kflidLexGloss); // 3
			Add(InterlinLineChoices.kflidLexPos); // 4
			Add(InterlinLineChoices.kflidWordGloss); // 5
			Add(InterlinLineChoices.kflidWordPos); // 6
			Add(InterlinLineChoices.kflidFreeTrans); // 7
		}

		public void SetStandardGlossState()
		{
			m_specs.Clear();
			Add(InterlinLineChoices.kflidWord); // 0
			Add(InterlinLineChoices.kflidWordGloss); // 5
			Add(InterlinLineChoices.kflidWordPos); // 6
			Add(InterlinLineChoices.kflidFreeTrans); // 7
		}

		public string Persist(ILgWritingSystemFactory wsf)
		{
			StringBuilder builder = new StringBuilder();
			builder.Append(this.GetType().Name);
			foreach (InterlinLineSpec spec in m_specs)
			{
				builder.Append(",");
				builder.Append(spec.Flid);
				builder.Append("%");
				builder.Append(wsf.GetStrFromWs(spec.WritingSystem));
			}
			return builder.ToString();
		}

		/// <remarks>The typical value for defAnalysis is LgWritingSystemTags.kwsVernInParagraph</remarks>
		public static InterlinLineChoices Restore(string data, ILgWritingSystemFactory wsf, ILangProject proj, int defVern, int defAnalysis, InterlinMode mode = InterlinMode.Analyze, PropertyTable propertyTable = null, string configPropName = "")
		{
			Debug.Assert(defVern != 0);
			Debug.Assert(defAnalysis != 0);

			InterlinLineChoices result;
			string[] parts = data.Split(',');

			switch(parts[0])
			{
				case "InterlinLineChoices":
					result = new InterlinLineChoices(proj, defVern, defAnalysis, mode);
					break;
				case "EditableInterlinLineChoices":
					result = new EditableInterlinLineChoices(proj, defVern, defAnalysis);
					break;
				default:
					throw new Exception("Unrecognised type of InterlinLineChoices: " + parts[0]);
			}
			for (int i = 1; i < parts.Length; i++)
			{
				string[] flidAndWs = parts[i].Split('%');
				if (flidAndWs.Length != 2)
					throw new Exception("Unrecognized InterlinLineSpec: " + parts[i]);
				int flid = Int32.Parse(flidAndWs[0]);
				int ws = wsf.GetWsFromStr(flidAndWs[1]);
				// Some virtual Ids such as -61 and 103 create standard items. so, we need to add those items always
				if (flid <= ComplexConcPatternVc.kfragFeatureLine || ((IFwMetaDataCacheManaged)proj.Cache.MetaDataCacheAccessor).FieldExists(flid))
					result.Add(flid, ws);
				else
				{
					if (propertyTable != null && !string.IsNullOrEmpty(configPropName))
					{
						data = data.Replace("," + flid + "%", "");
						propertyTable.SetProperty(configPropName, data, false);
						propertyTable.SetPropertyPersistence(configPropName, true);
					}
				}
			}
			return result;
		}

		public int Count
		{
			get { return m_specs.Count; }
		}

		public bool HaveMorphemeLevel
		{
			get
			{
				for (int i = 0; i < m_specs.Count; i++)
				{
					if (this[i].MorphemeLevel)
						return true;
				}
				return false;
			}
		}

		/// <summary>
		/// Answer true if it is valid for the field indicated in spec2 to immediately
		/// follow the one indicated in spec1. By default any order is OK. This routine is
		/// not responsible for the restrictions related to keeping word level stuff before
		/// freeforms and morpheme-level stuff together.
		/// </summary>
		/// <param name="spec1"></param>
		/// <param name="spec2"></param>
		/// <returns></returns>
		internal virtual bool CanFollow(InterlinLineSpec spec1, InterlinLineSpec spec2)
		{
			return true;
		}

		public virtual int Add(InterlinLineSpec spec)
		{
			bool fGotMorpheme = HaveMorphemeLevel;
			for (int i = m_specs.Count - 1; i >= 0; i--)
			{
				if (this[i].Flid == spec.Flid)
				{
					// It's always OK (and optimal) to insert a new occurrence of the same
					// flid right after the last existing one.
					m_specs.Insert(i + 1, spec);
					return i + 1;
				}
			}
			for (int i = m_specs.Count - 1; i >= 0; i--)
			{
				if (CanFollow(this[i], spec))
				{
					int firstMorphemeIndex = FirstMorphemeIndex;
					// Even if otherwise OK, if we're inserting something morpheme level
					// and there's already morpheme-level stuff present it must follow
					// the existing morpheme-level stuff.
					if (fGotMorpheme && spec.MorphemeLevel && i >= firstMorphemeIndex &&
						(!this[i].MorphemeLevel ||
						spec.Flid == kflidMorphemes ||
						spec.Flid == kflidLexEntries && this[i].Flid != kflidMorphemes))
					{
						continue;
					}
					// And word-level annotations can't follow freeform ones.
					if (spec.WordLevel && !this[i].WordLevel)
						continue;
					m_specs.Insert(i + 1, spec);
					return i + 1;
				}
			}
			m_specs.Insert(0, spec); // can't follow anything, put first.
			return 0;
		}

		/// <summary>
		/// Answer true if it is OK to change the writing system of the specified field.
		/// By default this is always OK.
		/// </summary>
		/// <param name="index"></param>
		/// <returns></returns>
		public virtual bool OkToChangeWritingSystem(int index)
		{
			return true;
		}

		/// <summary>
		/// Call before calling Remove. True and null message indicates no problem to
		/// remove this field.
		/// If it returns a true, display a message box indicating the Remove is not
		/// possible, using the returned message.
		/// If it returns false and a message, there is a warning, display it and
		/// allow the user to possibly cancel.
		/// </summary>
		/// <param name="spec"></param>
		/// <returns></returns>
		public virtual bool OkToRemove(InterlinLineSpec spec, out string message)
		{
			if (m_specs.Count == 1)
			{
				message = ITextStrings.ksNeedOneField;
				return false;
			}
			message = null;
			return true;
		}

		internal bool OkToRemove(int choiceIndex)
		{
			return OkToRemove(this[choiceIndex]);
		}

		internal bool OkToRemove(InterlinLineSpec spec)
		{
			string message;
			return OkToRemove(spec, out message);
		}

		/// <summary>
		/// Remove the specified field (and any dependents, with warning).
		/// If there are dependents, this will interact with the user to ask whether to
		/// go ahead.
		/// </summary>
		/// <param name="spec"></param>
		public virtual void Remove(InterlinLineSpec spec)
		{
			m_specs.Remove(spec);
			Debug.Assert(m_specs.Count > 0);
		}


		/// <summary>
		/// Removes the Line Choice specified by a flid and writing system, then returns if the Remove was successful
		/// </summary>
		public bool Remove(int flid, int ws)
		{
			var spec = m_specs.Find(x => x.Flid == flid && x.WritingSystem == ws);
			if (spec == null)
				return false;
			if (OkToRemove(spec))
			{
				m_specs.Remove(spec);
				return true;
			}
			return false;
		}

		/// These constants are defined for brevity here and convenience in testing. They use real field
		/// IDs where that is possible. The names correspond to what we see by default in the dialog.
		public const int kflidWord = WfiWordformTags.kflidForm;
		/// <summary></summary>
		public const int kflidMorphemes = WfiMorphBundleTags.kflidMorph;
		/// <summary>
		/// We get the lex entry by following the owner of the morpheme. So rather arbitrarily we
		/// use that constant to identify that field.
		/// </summary>
		public const int kflidLexEntries = (int)CmObjectFields.kflidCmObject_Owner;
		public const int kflidLexGloss = WfiMorphBundleTags.kflidSense;
		public const int kflidLexPos = WfiMorphBundleTags.kflidMsa;
		public const int kflidWordGloss = WfiGlossTags.kflidForm;
		public const int kflidWordPos = WfiAnalysisTags.kflidCategory;
		public const int kflidFreeTrans = InterlinVc.ktagSegmentFree;
		public const int kflidLitTrans = InterlinVc.ktagSegmentLit;
		public const int kflidNote = InterlinVc.ktagSegmentNote;

		private LineOption[] UpdateFieldNamesFromLines(InterlinMode mode)
		{
			LineOption[] options = LineOptions(mode);
			m_fieldNames.Clear();
			foreach (LineOption opt in options)
				m_fieldNames[opt.Flid] = opt.ToString();
			return options;
		}

		/// <summary>
		/// Get the standard list of lines. Also updates the member variable storing the line names.
		/// </summary>
		internal LineOption[] LineOptions()
		{
			return UpdateFieldNamesFromLines(m_mode);
		}

		private LineOption[] LineOptions(InterlinMode mode)
		{
			var customLineOptions = GetCustomLineOptions(mode);

			if (mode == InterlinMode.Chart)
			{
				return new[]
				{
					new LineOption(kflidWord, ITextStrings.ksWord),
					new LineOption(kflidWordGloss, ITextStrings.ksWordGloss),
					new LineOption(kflidMorphemes, ITextStrings.ksMorphemes),
					new LineOption(kflidLexGloss, ITextStrings.ksGloss),
					new LineOption(kflidLexEntries, ITextStrings.ksLexEntries),
					new LineOption(kflidLexPos, ITextStrings.ksGramInfo)
				}.Union(customLineOptions).ToArray();
			}

			return new[] {
				 new LineOption(kflidWord, ITextStrings.ksWord),
				 new LineOption(kflidMorphemes, ITextStrings.ksMorphemes),
				 new LineOption(kflidLexEntries, ITextStrings.ksLexEntries),
				 new LineOption(kflidLexGloss, ITextStrings.ksLexGloss),
				 new LineOption(kflidLexPos, ITextStrings.ksGramInfo),
				 new LineOption(kflidWordGloss,
					mode == InterlinMode.GlossAddWordsToLexicon ? ITextStrings.ksLexWordGloss : ITextStrings.ksWordGloss),
				 new LineOption(kflidWordPos,
					mode == InterlinMode.GlossAddWordsToLexicon ? ITextStrings.ksLexWordCat : ITextStrings.ksWordCat),
				 new LineOption(kflidFreeTrans, ITextStrings.ksFreeTranslation),
				 new LineOption(kflidLitTrans, ITextStrings.ksLiteralTranslation),
				 new LineOption(kflidNote, ITextStrings.ksNote)
			}.Union(customLineOptions).ToArray();
		}

		private List<LineOption> GetCustomLineOptions(InterlinMode mode)
		{
			var customLineOptions = new List<LineOption>();
			switch (mode)
			{
				case InterlinMode.Analyze:
				case InterlinMode.Chart:
				case InterlinMode.Gloss:
					if (m_cache != null)
					{
						var classId = m_cache.MetaDataCacheAccessor.GetClassId("Segment");
						var mdc = (IFwMetaDataCacheManaged)m_cache.MetaDataCacheAccessor;
						foreach (int flid in mdc.GetFields(classId, false, (int)CellarPropertyTypeFilter.All))
						{
							if (!mdc.IsCustom(flid))
								continue;
							customLineOptions.Add(new LineOption(flid, mdc.GetFieldLabel(flid)));
						}
					}
					break;
			}

			return customLineOptions;
		}

		/// <summary>
		///
		/// </summary>
		/// <param name="flid"></param>
		/// <returns></returns>
		/// <exception cref="KeyNotFoundException">Thrown if the key is not found in the Dictionary.</exception>
		public string LabelFor(int flid)
		{
			return m_fieldNames[flid];
		}

		internal int LabelRGBFor(int choiceIndex)
		{
			return LabelRGBFor(this[choiceIndex]);
		}

		internal int LabelRGBFor(InterlinLineSpec spec)
		{
			return (int)CmObjectUi.RGB(LabelColorFor(spec));
		}

		internal Color LabelColorFor(InterlinLineSpec spec)
		{
			if (spec.MorphemeLevel)
				return kMorphLevelColor;
			else if (spec.WordLevel && spec.Flid != InterlinLineChoices.kflidWord)
				return kWordLevelColor;
			else
				return SystemColors.ControlText;
		}

		// Find where the spec is in your collection.
		public int IndexOf(InterlinLineSpec spec)
		{
			return m_specs.IndexOf(spec);
		}

		/// <summary>
		/// Add the specified flid (in the appropriate default writing system).
		/// </summary>
		/// <param name="flid"></param>
		/// <returns>the index where the new field was inserted</returns>
		public int Add(int flid)
		{
			return Add(flid, 0);
		}

		/// <summary>
		/// Answer the index of the first item that is at the morpheme level (or -1 if none)
		/// </summary>
		public int FirstMorphemeIndex
		{
			get
			{
				for (int i = 0; i < Count; i++)
					if (this[i].MorphemeLevel)
						return i;
				return -1;
			}
		}

		/// <summary>
		/// Answer the index of the first item that is at the lex entry level (or -1 if none)
		/// </summary>
		public int FirstLexEntryIndex
		{
			get
			{
				for (int i = 0; i < Count; i++)
					if (this[i].LexEntryLevel)
						return i;
				return -1;
			}
		}

		/// <summary>
		/// Answer the index of the first item that is at the morpheme level (or Count if none).
		/// (Returning Count if none makes it easy to loop from FirstFreeformIndex to Count
		/// to get them all.)
		/// </summary>
		public int FirstFreeformIndex
		{
			get
			{
				for (int i = 0; i < Count; i++)
					if (!this[i].WordLevel)
						return i;
				return Count;
			}
		}

		/// <summary>
		/// Answer the index of the last item that is at the morpheme level (or -1 if none)
		/// </summary>
		public int LastMorphemeIndex
		{
			get
			{
				for (int i = Count - 1; i >= 0; i--)
					if (this[i].MorphemeLevel)
						return i;
				return -1;
			}
		}

		/// <summary>
		/// Answer true if the spec at index is the first one that has its flid.
		/// (This is used to decide whether to display a pull-down icon in the sandbox.)
		/// </summary>
		/// <param name="index"></param>
		/// <returns></returns>
		public bool IsFirstOccurrenceOfFlid(int index)
		{
			int flid = this[index].Flid;
			for (int i = 0; i < index; i++)
				if (this[i].Flid == flid)
					return false;
			return true;
		}

		/// <summary>
		/// Answer an array list of integers, the writing systems we care about for the specified flid.
		/// Note that some of these may be magic; one is returned for each occurrence of flid.
		/// </summary>
		/// <param name="flid"></param>
		/// <returns></returns>
		public List<int> WritingSystemsForFlid(int flid)
		{
			return WritingSystemsForFlid(flid, false);
		}

		/// <summary>
		/// Get the writing system for the given flid.
		/// </summary>
		/// <param name="flid"></param>
		/// <param name="fGetDefaultForMissing">if true, provide the default writing system for the given flid.</param>
		/// <returns></returns>
		public List<int> WritingSystemsForFlid(int flid, bool fGetDefaultForMissing)
		{
			List<int> result = new List<int>();
			foreach (InterlinLineSpec spec in m_specs)
			{
				if (spec.Flid == flid && result.IndexOf(spec.WritingSystem) < 0)
					result.Add(spec.WritingSystem);
			}
			if (fGetDefaultForMissing && result.Count == 0)
			{
				InterlinLineSpec newSpec = CreateSpec(flid, 0);
				result.Add(newSpec.WritingSystem);
			}
			return result;
		}

		/// <summary>
		/// Answer the number of times the specified flid is displayed (typically using different writing systems).
		/// </summary>
		/// <param name="flid"></param>
		/// <returns></returns>
		public int RepetitionsOfFlid(int flid)
		{
			int result = 0;
			foreach (InterlinLineSpec spec in m_specs)
			{
				if (spec.Flid == flid)
				{
					result++;
				}
			}
			return result;
		}

		/// <summary>
		/// A list of integers representing the writing systems we care about for the view.
		/// </summary>
		public List<int> WritingSystems
		{
			get
			{
				List<int> result = new List<int>();
				foreach (InterlinLineSpec spec in m_specs)
				{
					if (result.IndexOf(spec.WritingSystem) < 0)
						result.Add(spec.WritingSystem);
				}
				return result;
			}
		}

		/// <summary>
		/// Answer an array of the writing systems to display for the field at index,
		/// and any subsequent fields with the same flid.
		/// </summary>
		/// <param name="index"></param>
		/// <returns></returns>
		public int[] AdjacentWssAtIndex(int index, int hvo)
		{
			int first = index;
			int lim = index + 1;
			while (lim < Count && this[lim].Flid == this[first].Flid)
				lim++;
			int[] result = new int[lim - first];
			for (int i = first; i < lim; i++)
			{
				var wsId = this[i].WritingSystem;
				if (wsId < 0) // if this is a magic writing system
				{
					wsId = WritingSystemServices.ActualWs(m_cache, wsId, hvo, this[i].Flid);
				}
				result[i - first] = wsId;
			}
			return result;
		}
		/// <summary>
		/// Answer an array list of integers, the writing systems we care about for the specified flid,
		/// except the one specified.
		/// </summary>
		/// <param name="flid"></param>
		/// <param name="wsToOmit">the ws to omit. use 0 remove the default ws for this flid</param>
		/// <returns></returns>
		public List<int> OtherWritingSystemsForFlid(int flid, int wsToOmit)
		{
			if (wsToOmit == 0)
			{
				// eliminate the default writing system for this flid.
				InterlinLineSpec specDefault = CreateSpec(flid, 0);
				wsToOmit = specDefault.WritingSystem;
			}
			List<int> result = new List<int>();
			foreach (InterlinLineSpec spec in m_specs)
			{
				if (spec.Flid == flid && result.IndexOf(spec.WritingSystem) < 0 && spec.WritingSystem != wsToOmit)
					result.Add(spec.WritingSystem);
			}
			return result;
		}

		public bool IsDefaultSpec(InterlinLineSpec spec)
		{
			return spec.SameSpec(CreateSpec(spec.Flid, 0));
		}

		/// <summary>
		/// Add the specified flid (in the appropriate default writing system).
		/// </summary>
		/// <param name="flid"></param>
		/// <param name="ws">If zero, supply the default ws for the field; otherwise
		/// use the one supplied.</param>
		/// <returns>the integer where inserted</returns>
		public int Add(int flid, int wsRequested)
		{
			InterlinLineSpec spec = CreateSpec(flid, wsRequested);
			return Add(spec);
		}

		/// <summary>
		/// returns the main spec for this flid, searching for the first one that cant be removed.
		/// then searches for the first default spec.
		/// then simply the first spec.
		/// </summary>
		/// <param name="specFlid"></param>
		/// <returns>null, if no primary spec is found.</returns>
		public InterlinLineSpec GetPrimarySpec(int specFlid)
		{
			List<InterlinLineSpec> matchingSpecs = this.ItemsWithFlids(new int[] { specFlid });
			// should we consider creating a default spec instead?
			if (matchingSpecs.Count == 0)
				return null;
			// search for the first matching spec that we can't remove.
			foreach (InterlinLineSpec spec in matchingSpecs)
			{
				if (!OkToRemove(spec))
					return spec;
			}

			// search for the first spec that is a default spec.
			foreach (InterlinLineSpec spec in matchingSpecs)
			{
				if (IsDefaultSpec(spec))
					return spec;
			}

			// lastly return the first matchingSpec
			return matchingSpecs[0];
		}

		/// <summary>
		///
		/// </summary>
		/// <param name="flid"></param>
		/// <param name="wsRequested">If zero, supply the default ws for the field; otherwise
		/// use the one supplied.</param>
		/// <returns></returns>
		internal InterlinLineSpec CreateSpec(int flid, int wsRequested)
		{
			int ws = 0;
			bool fMorphemeLevel = false;
			bool fWordLevel = true;
			int flidString = 0;
			ColumnConfigureDialog.WsComboContent comboContent = ColumnConfigureDialog.WsComboContent.kwccAnalysis; // The usual choice
			switch (flid)
			{
				case kflidWord:
					comboContent = ColumnConfigureDialog.ChooseComboContent(m_cache, m_wsDefVern, "vernacular");
					ws = m_wsDefVern;
					break; // vern, not interlin, word
				case kflidLexEntries:
				case kflidMorphemes:
					fMorphemeLevel = true;
					comboContent = ColumnConfigureDialog.ChooseComboContent(m_cache, m_wsDefVern, "vernacular");
					flidString = MoFormTags.kflidForm;
					ws = m_wsDefVern;
					break; // vern, morpheme
				case kflidLexGloss:
					fMorphemeLevel = true;
					ws = WritingSystemServices.kwsFirstAnal;
					flidString = LexSenseTags.kflidGloss;
					comboContent = ColumnConfigureDialog.WsComboContent.kwccBestAnalysis;
					break; // analysis, morpheme
				case kflidLexPos:
					fMorphemeLevel = true;
					// getting to the string takes a couple of levels
					// so just do it when we have the actual hvos.
					flidString = -1;
					ws = WritingSystemServices.kwsFirstAnal;
					comboContent = ColumnConfigureDialog.WsComboContent.kwccBestAnalysis;
					break; // analysis, morpheme
				case kflidWordGloss:
					ws = m_wsDefAnal;
					break; // not morpheme-level
				case kflidWordPos:
					ws = WritingSystemServices.kwsFirstAnal;
					flidString = CmPossibilityTags.kflidAbbreviation;
					comboContent = ColumnConfigureDialog.WsComboContent.kwccBestAnalysis;
					break; // not morpheme-level
				case kflidFreeTrans:
				case kflidLitTrans:
					ws = m_wsDefAnal;
					fWordLevel = false;
					break;
				case kflidNote:
					comboContent = ColumnConfigureDialog.WsComboContent.kwccVernAndAnal;
					ws = m_wsDefAnal;
					fWordLevel = false;
					break;
				default:
					var mdc = (IFwMetaDataCacheManaged) m_cache.MetaDataCacheAccessor;
					if (mdc.FieldExists(flid))
					{
						if (!mdc.IsCustom(flid))
						{
							throw new Exception("Adding unknown field to interlinear");
						}

						ws = mdc.GetFieldWs(flid);
					}
					fWordLevel = false;
					comboContent = ColumnConfigureDialog.WsComboContent.kwccAnalAndVern;
					break;
			}
			InterlinLineSpec spec = new InterlinLineSpec();
			spec.ComboContent = comboContent;
			spec.Flid = flid;
			spec.WritingSystem = wsRequested == 0 ? ws : wsRequested;
			spec.MorphemeLevel = fMorphemeLevel;
			spec.WordLevel = fWordLevel;
			spec.StringFlid = flidString;
			return spec;
		}

		public InterlinLineSpec this[int index]
		{
			get { return m_specs[index]; }
		}

		public IEnumerator GetEnumerator()
		{
			return m_specs.GetEnumerator();
		}

		/// <summary>
		/// Return the index of the (first) spec with the specified flid and ws, if any.
		/// First tries to match the ws exactly, and then will see if we can find it in
		/// collections referred to by a magic value.
		/// </summary>
		/// <param name="flid"></param>
		/// <param name="ws"></param>
		/// <returns>-1 if not found.</returns>
		internal int IndexOf(int flid, int ws)
		{
			int index = -1;
			if (ws > 0)
			{
				// first try to find an exact match.
				 index = IndexOf(flid, ws, true);
			}
			if (index == -1)
				index = IndexOf(flid, ws, false);
			return index;
		}

		/// <summary>
		///
		/// </summary>
		/// <param name="flid"></param>
		/// <param name="ws"></param>
		/// <param name="fExact">if true, see if we can find a line choice that matches the exact writing system given.
		/// if false, we'll try to see if a line choice refers to a collection (via magic value) that contains the ws.</param>
		/// <returns></returns>
		internal int IndexOf(int flid, int ws, bool fExact)
		{
			for (int i = 0; i < m_specs.Count; i++)
			{
				if (this[i].Flid == flid && MatchingWritingSystem(this[i].WritingSystem, ws, fExact))
					return i;
			}
			return -1;
		}

		/// <summary>
		///
		/// </summary>
		/// <param name="wsConfig"></param>
		/// <param name="ws"></param>
		/// <param name="fExact"></param>
		/// <returns></returns>
		private bool MatchingWritingSystem(int wsConfig, int ws, bool fExact)
		{
			LcmCache cache = m_cache;
			if (wsConfig == ws)
				return true;
			if (fExact)
				return false;
			if (m_proj == null)
				return false;
			CoreWritingSystemDefinition wsObj = m_cache.ServiceLocator.WritingSystemManager.Get(ws);
			switch (wsConfig)
			{
				case WritingSystemServices.kwsAnal:
					return ws == cache.DefaultAnalWs;

				case WritingSystemServices.kwsPronunciation:
					return ws == cache.DefaultPronunciationWs;

				case WritingSystemServices.kwsVern:
					return ws == cache.DefaultVernWs;

				case WritingSystemServices.kwsAnals:
				case WritingSystemServices.kwsFirstAnal:
					return m_cache.ServiceLocator.WritingSystems.AnalysisWritingSystems.Contains(wsObj);

				case WritingSystemServices.kwsAnalVerns:
				case WritingSystemServices.kwsFirstAnalOrVern:
				case WritingSystemServices.kwsVernAnals:
				case WritingSystemServices.kwsFirstVernOrAnal:
					return m_cache.ServiceLocator.WritingSystems.AnalysisWritingSystems.Contains(wsObj) ||
						m_cache.ServiceLocator.WritingSystems.VernacularWritingSystems.Contains(wsObj);

				case WritingSystemServices.kwsFirstVern:
				case WritingSystemServices.kwsVernInParagraph:
				case WritingSystemServices.kwsVerns:
					return m_cache.ServiceLocator.WritingSystems.VernacularWritingSystems.Contains(wsObj);

				case WritingSystemServices.kwsFirstPronunciation:
				case WritingSystemServices.kwsPronunciations:
				case WritingSystemServices.kwsReversalIndex:
				default:
					return false;
			}
		}

		/// <summary>
		/// Return the index of the (first) spec with the specified flid
		/// </summary>
		/// <param name="flid"></param>
		/// <param name="ws"></param>
		/// <returns>-1 if not found.</returns>
		public int IndexOf(int flid)
		{
			for (int i = 0; i < m_specs.Count; i++)
			{
				if (this[i].Flid == flid)
					return i;
			}
			return -1;
		}

		/// <summary>
		/// Return a collection containing any line which has any of the specified flids.
		/// </summary>
		/// <param name="flids"></param>
		/// <returns></returns>
		internal List<InterlinLineSpec> ItemsWithFlids(int[] flids)
		{
			return ItemsWithFlids(flids, null);
		}

		internal List<InterlinLineSpec> ItemsWithFlids(int[] flids, int[] wsList)
		{
			Debug.Assert(wsList == null || wsList.Length == flids.Length,
				"wsList should be empty or match the same item count in flids.");
			List<InterlinLineSpec> result = new List<InterlinLineSpec>();
			for (int i = 0; i < m_specs.Count; i++)
			{
				for (int j = 0; j < flids.Length; j++)
				{
					if (this[i].Flid == flids[j] &&
						(wsList == null || this[i].WritingSystem == wsList[j]))
					{
						result.Add(this[i]);
					}
				}
			}
			return result;
		}

		/// <summary>
		/// Answer where line n should move up to (if it can move up).
		/// If it can't answer -1.
		/// </summary>
		/// <param name="n"></param>
		/// <returns></returns>
		internal int WhereToMoveUpTo(int n)
		{
			if (n == 0)
				return -1; // first line can't move up!
			InterlinLineSpec spec = this[n];
			// Can't move up at all if it's a freeform and the previous line is not.
			if (!spec.WordLevel && this[n - 1].WordLevel)
				return -1;
			int newPos = n - 1; // default place to put it.
			// If it is not morpheme level and a morpheme-level precedes it, must move
			// past all of them. Same treatment for notes if there is more than one ws (one per line)
			if (!spec.MorphemeLevel && this[newPos].MorphemeLevel)
				for (; newPos > 0 && this[newPos - 1].MorphemeLevel; newPos--) {}
			if (spec.Flid != kflidNote && this[newPos].Flid == kflidNote)
				for (; newPos > 0 && this[newPos - 1].Flid == kflidNote; newPos--) {}
			// If it can't go here it just can't move.
			if (newPos > 0 && !CanFollow(this[newPos - 1], spec))
				return -1;
			if (!CanFollow(spec, this[newPos]))
				return -1;
			return newPos;
		}

		/// <summary>
		/// Answer true if the item at line n can be moved up a line.
		/// </summary>
		/// <param name="n"></param>
		/// <returns></returns>
		public virtual bool OkToMoveUp(int n)
		{
			return WhereToMoveUpTo(n) >= 0;
		}

		/// <summary>
		/// Moves a choice line up to the position n.
		/// Morpheme lines, word lines and
		/// </summary>
		/// <param name="n"></param>
		public void MoveUp(int n)
		{
			int dest = WhereToMoveUpTo(n);
			if (dest < 0)
				return;
			InterlinLineSpec spec = this[n];
			// If this was the first morpheme field, move the others too.
			bool fMoveMorphemeGroup = spec.MorphemeLevel && !this[n - 1].MorphemeLevel;
			bool fMoveNoteGroup	= spec.Flid == kflidNote && this[n - 1].Flid != kflidNote;
			m_specs.RemoveAt(n);
			m_specs.Insert(dest, spec);
			if (fMoveMorphemeGroup || fMoveNoteGroup)
			{
				for (int i = n + 1; i < Count && ((fMoveMorphemeGroup && this[i].MorphemeLevel) ||
												  (fMoveNoteGroup && this[i].Flid == kflidNote)); i++)
				{
					InterlinLineSpec specT = this[i];
					m_specs.RemoveAt(i);
					m_specs.Insert(dest + i - n, specT);
				}
			}
		}

		/// <summary>
		/// Answer true if the item at line n can be moved down a line.
		/// Currently this is true if the following line can be moved up.
		/// </summary>
		/// <param name="n"></param>
		/// <returns></returns>
		public virtual bool OkToMoveDown(int n)
		{
			if (n > Count - 2)
				return false;
			return OkToMoveUp(n + 1);
		}

		/// <summary>
		/// Currently, moving line n down is the same as moving line n+1 up.
		/// </summary>
		/// <param name="n"></param>
		public void MoveDown(int n)
		{
			if (n > Count - 2)
				return;
			MoveUp(n + 1);
		}
		#region ICloneable Members

		public object Clone()
		{
			InterlinLineChoices result = base.MemberwiseClone() as InterlinLineChoices;
			// We need a deep clone of the specs, because not only may we reorder the
			// list and add items, but we may alter items, e.g., by setting the WS.
			result.m_specs = new List<InterlinLineSpec>(m_specs.Count);
			foreach (InterlinLineSpec spec in m_specs)
				result.m_specs.Add(spec.Clone() as InterlinLineSpec);
			return result;
		}

		#endregion
	}

	/// <summary>
	/// This is a subclass of InterlinLineChoices used for editable interlinear views. It has more
	/// restrictions on allowed orders.
	/// </summary>
	public class EditableInterlinLineChoices : InterlinLineChoices
	{
		public EditableInterlinLineChoices(ILangProject proj, int defaultVernacularWs, int defaultAnalysisWs)
			: base(proj, defaultVernacularWs, defaultAnalysisWs)
		{
		}
		public new static InterlinLineChoices DefaultChoices(ILangProject proj, int vern, int analysis)
		{
			InterlinLineChoices result = new EditableInterlinLineChoices(proj, vern, analysis);
			result.SetStandardState();
			return result;
		}
		/// <summary>
		/// Overridden to ensure that if the requested field depends on something, that something is
		/// there or gets added.
		/// </summary>
		/// <param name="flid"></param>
		/// <returns></returns>
		//public override int Add(InterlinLineSpec spec)
		//{
		//	int flid = spec.Flid;
		//	if (flid == kflidLexEntries && IndexOf(kflidMorphemes, m_wsDefVern) < 0)
		//	{
		//		// Must have morpheme breakdown, too.
		//		Add(kflidMorphemes);
		//	}
		//	if ((flid == kflidLexGloss
		//		|| flid == kflidLexPos)
		//		&& IndexOf(kflidLexEntries, m_wsDefVern) < 0)
		//	{
		//		// Must have default lex entries line, too.
		//		Add(kflidLexEntries);
		//	}
		//	return base.Add(spec);
		//}

		/// <summary>
		/// Answer true if it is OK to change the writing system of the specified field.
		/// This is not allowed if it is one of the special fields and is the first
		/// occurrence of the default writing system.
		/// </summary>
		/// <param name="index"></param>
		/// <returns></returns>
		public override bool OkToChangeWritingSystem(int index)
		{
			int flid = this[index].Flid;
			if (flid == kflidLexPos)
				return true; // We now allow the user to select the ws for Lex Grammatical Info.
			if (flid != kflidWord && flid != kflidMorphemes && flid != kflidLexEntries)
				return true; // Not a field we care about.
			if (this[index].WritingSystem != m_wsDefVern)
				return true; // Not a Ws we care about.
			if (IndexOf(flid) != index)
				return true; // Not the instance we care about.
			return false;
		}

		public override bool OkToRemove(InterlinLineSpec spec, out string message)
		{
			if (!base.OkToRemove(spec, out message))
				return false;
			if (spec.Flid == kflidWord && spec.WritingSystem == m_wsDefVern &&
				ItemsWithFlids(new int[] {kflidWord}, new int[] {m_wsDefVern}).Count < 2)
			{
				message = ITextStrings.ksNeedWordLine;
				return false;
			}
			if (FindDependents(spec).Count > 0)
			{
				// Enhance JohnT: get the names and include them in the message.
				message = ITextStrings.ksHidesDependentLinesAlso;
				// OK to go ahead if the user wishes, return true.
			}
			return true;
		}


		/// <summary>
		/// Overridden to prevent removing the Words line and to remove dependents of the line being removed
		/// (after warning the user).
		/// </summary>
		/// <param name="spec"></param>
		public override void Remove(InterlinLineSpec spec)
		{
			List<InterlinLineSpec> dependents = new List<InterlinLineSpec>();
			dependents = FindDependents(spec);
			foreach (InterlinLineSpec depSpec in dependents)
				m_specs.Remove(depSpec);
			base.Remove(spec);
		}

		private List<InterlinLineSpec> FindDependents(InterlinLineSpec spec)
		{
			List<InterlinLineSpec> dependents = new List<InterlinLineSpec>();
			return dependents;
		}
	}

	internal class LineOption
	{
		int m_flid;
		string m_label;

		public LineOption(int flid, string label)
		{
			m_flid = flid;
			m_label = label;
		}

		public override string ToString()
		{
			return m_label;
		}

		public int Flid
		{
			get { return m_flid; }
		}

		public string Label
		{
			get { return m_label; }
			set { m_label = value; }
		}
	}

	/// <summary>
	/// The specification of what to show on one interlinear line.
	/// Indicates what line (typically a field of a wordform, bundle, or segment)
	/// and which it is a field of.
	/// MorphemeLevel annotations are always also Word level.
	/// </summary>
	public class InterlinLineSpec : ICloneable
	{
		int m_flid;
		int m_ws;
		bool m_fMorpheme;
		bool m_fWord;
		int m_flidString; // the string property to use with m_ws
		ColumnConfigureDialog.WsComboContent m_comboContent;
		ITsString m_tssWsLabel;

		public InterlinLineSpec()
		{
		}

		/// <summary>
		/// Compare the public property getters to the given spec.
		/// </summary>
		/// <param name="spec"></param>
		/// <returns>true if all the public getter values match.</returns>
		public bool SameSpec(InterlinLineSpec spec)
		{
			return ReflectionHelper.HaveSamePropertyValues(this, spec);
		}

		public int Flid
		{
			get { return m_flid; }
			set { m_flid = value; }
		}

		public ColumnConfigureDialog.WsComboContent ComboContent
		{
			get { return m_comboContent; }
			set { m_comboContent = value; }
		}

		/// <summary>
		/// The flid referring to the string associated with the WritingSystem.
		/// </summary>
		public int StringFlid
		{
			get
			{
				if (m_flidString == 0)
					return m_flid;
				return m_flidString;
			}
			set { m_flidString = value; }
		}

		/// <summary>
		/// Could be a magic writing system
		/// </summary>
		public int WritingSystem
		{
			get { return m_ws; }
			set
			{
				if (m_ws == value)
					return;
				m_ws = value;
				m_tssWsLabel = null;
			}
		}

		/// <summary>
		/// Indicate whether WritingSystem is a magic value.
		/// </summary>
		public bool IsMagicWritingSystem
		{
			get { return WritingSystem < 0; }
		}

		/// <summary>
		/// Get the actual ws of the WritingSystem based on the given hvo, if it refers to a non-empty alternative;
		/// otherwise, get the actual ws for wsFallback
		/// </summary>
		public int GetActualWs(LcmCache cache, int hvo, int wsFallback)
		{
			if (StringFlid == -1)
			{
				// we depend upon someone else to determine the ws.
				return 0;
			}

			int wsActual;
			ITsString dummy;
		// While displaying the morph bundle, we need the ws used by the Form. If we can't get the Form from the MoForm, we
		// substitute with the Form stored in the WfiMorphBundle. But our system incorrectly assumes that this object
		// is a MoForm, so we specify that if our object is a WfiMorphBundle, use the relevant flid.
		int flid = cache.ServiceLocator.GetInstance<ICmObjectRepository>().GetObject(hvo) is IWfiMorphBundle ? WfiMorphBundleTags.kflidForm : StringFlid;
		WritingSystemServices.TryWs(cache, WritingSystem, wsFallback, hvo, flid, out wsActual, out dummy);
			return wsActual;
		}

		public bool LexEntryLevel
		{
			get { return MorphemeLevel && Flid != InterlinLineChoices.kflidMorphemes; }
		}

		public bool MorphemeLevel
		{
			get { return m_fMorpheme; }
			set
			{
				m_fMorpheme = value;
				if (value)
					m_fWord = true;
			}
		}

		public bool WordLevel
		{
			get { return m_fWord; }
			set
			{
				m_fWord = value;
				if (!value)
					m_fMorpheme = false;
			}
		}

		#region ICloneable Members

		public object Clone()
		{
			return MemberwiseClone();
		}

		public ITsString WsLabel(LcmCache cache)
		{
			if (m_tssWsLabel == null)
			{
				string label;
				if (m_ws == WritingSystemServices.kwsFirstAnal || m_ws == WritingSystemServices.kwsAnal)
				{
					label = ITextStrings.ksBstAn;
				}
				else if (m_ws == WritingSystemServices.kwsFirstVern || m_ws == WritingSystemServices.kwsVern)
				{
					label = ITextStrings.ksBstVn;
				}
				else if (m_ws == WritingSystemServices.kwsVernInParagraph)
				{
					label = ITextStrings.ksBaselineAbbr;
				}
				else
				{
					CoreWritingSystemDefinition wsAnalysis = cache.ServiceLocator.WritingSystemManager.Get(m_ws);
					label = wsAnalysis.Abbreviation;
				}

				ITsStrBldr tsb = TsStringUtils.MakeStrBldr();
				tsb.Replace(0, tsb.Length, label, WsListManager.LanguageCodeTextProps(cache.DefaultUserWs));
				m_tssWsLabel = tsb.GetString();
			}

			return m_tssWsLabel;
		}

		#endregion
	}
}
