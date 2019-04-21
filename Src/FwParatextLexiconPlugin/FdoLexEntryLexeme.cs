﻿// Copyright (c) 2015 SIL International
// This software is licensed under the LGPL, version 2.1 or later
// (http://www.gnu.org/licenses/lgpl-2.1.html)

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Paratext.LexicalContracts;
using SIL.LCModel.Core.WritingSystems;
using SIL.LCModel.Core.KernelInterfaces;
using SIL.LCModel;
using SIL.LCModel.DomainServices;
using SIL.LCModel.Infrastructure;

namespace SIL.FieldWorks.ParatextLexiconPlugin
{
	#region FdoLexEntryLexeme class
	/// <summary>
	/// </summary>
	internal class FdoLexEntryLexeme : Lexeme
	{
		private readonly LexemeKey m_key;
		private readonly FdoLexicon m_lexicon;

		public FdoLexEntryLexeme(FdoLexicon lexicon, LexemeKey key)
		{
			m_lexicon = lexicon;
			m_key = key;
		}

		public LexemeKey Key
		{
			get { return m_key; }
		}

		#region Lexeme Members

		public string Id
		{
			get { return m_key.Id; }
		}

		public LexemeType Type
		{
			get { return m_key.Type; }
		}

		public string LexicalForm
		{
			get { return m_key.LexicalForm; }
		}

		/// <summary>
		/// Gets a string that is suitable for display which may contain morphological abbreviations
		/// </summary>
		public string DisplayString
		{
			get
			{
				ILexEntry entry;
				if (!m_lexicon.TryGetEntry(m_key, out entry))
					return null;

				return StringServices.CitationFormWithAffixTypeStaticForWs(entry, m_lexicon.DefaultVernWs);
			}
		}

		public string CitationForm
		{
			get
			{
				ILexEntry entry;
				if (!m_lexicon.TryGetEntry(m_key, out entry))
					return null;

				ITsString tss = entry.CitationForm.StringOrNull(m_lexicon.DefaultVernWs);
				return tss == null ? null : tss.Text;
			}
		}

		public int HomographNumber
		{
			get
			{
				ILexEntry entry;
				if (!m_lexicon.TryGetEntry(m_key, out entry))
					return 0;

				return entry.HomographNumber;
			}
		}

		public IEnumerable<LexiconSense> Senses
		{
			get
			{
				ILexEntry entry;
				if (!m_lexicon.TryGetEntry(m_key, out entry))
					return Enumerable.Empty<LexiconSense>();

				if (entry.AllSenses.Count == 1 && entry.SensesOS[0].Gloss.StringCount == 0)
					return Enumerable.Empty<LexiconSense>();

				return entry.AllSenses.Select(s => new LexSenseLexiconSense(m_lexicon, m_key, s)).ToArray();
			}
		}

		public IEnumerable<LexicalRelation> LexicalRelations
		{
			get
			{
				ILexEntry entry;
				if (!m_lexicon.TryGetEntry(m_key, out entry))
					return Enumerable.Empty<LexicalRelation>();

				var relations = new List<LexicalRelation>();
				foreach (ILexReference lexRef in entry.LexEntryReferences.Union(entry.AllSenses.SelectMany(s => s.LexSenseReferences)))
				{
					ILexEntry parentEntry;
					string name = GetLexReferenceName(entry, lexRef, out parentEntry);
					foreach (ICmObject obj in lexRef.TargetsRS)
					{
						ILexEntry otherEntry = null;
						switch (obj.ClassID)
						{
							case LexEntryTags.kClassId:
								otherEntry = (ILexEntry) obj;
								break;
							case LexSenseTags.kClassId:
								otherEntry = obj.OwnerOfClass<ILexEntry>();
								break;
						}
						if (otherEntry != null && otherEntry != entry)
						{
							relations.Add(new FdoLexicalRelation(m_lexicon.GetEntryLexeme(otherEntry),
								parentEntry != null && parentEntry != entry && parentEntry != otherEntry ? Strings.ksOther : name));
						}
					}
				}

				return relations;
			}
		}

		public IEnumerable<string> AlternateForms
		{
			get
			{
				ILexEntry entry;
				if (!m_lexicon.TryGetEntry(m_key, out entry))
					return Enumerable.Empty<string>();

				var forms = new List<string>();
				foreach (IMoForm form in entry.AlternateFormsOS)
				{
					ITsString tss = form.Form.StringOrNull(m_lexicon.DefaultVernWs);
					if (tss != null)
						forms.Add(tss.Text.Normalize());
				}

				return forms;
			}
		}

		private string GetLexReferenceName(ILexEntry lexEntry, ILexReference lexRef, out ILexEntry parentEntry)
		{
			parentEntry = null;
			ILexRefType lexRefType = lexRef.OwnerOfClass<ILexRefType>();
			string name = lexRefType.ShortName;
			if (string.IsNullOrEmpty(name))
				name = lexRefType.Abbreviation.BestAnalysisAlternative.Text;
			var mappingType = (LexRefTypeTags.MappingTypes) lexRefType.MappingType;
			switch (mappingType)
			{
				case LexRefTypeTags.MappingTypes.kmtSenseTree:
				case LexRefTypeTags.MappingTypes.kmtEntryTree:
				case LexRefTypeTags.MappingTypes.kmtEntryOrSenseTree:
				case LexRefTypeTags.MappingTypes.kmtSenseAsymmetricPair: // Sense Pair with different Forward/Reverse names
				case LexRefTypeTags.MappingTypes.kmtEntryAsymmetricPair: // Entry Pair with different Forward/Reverse names
				case LexRefTypeTags.MappingTypes.kmtEntryOrSenseAsymmetricPair: // Entry or sense Pair with different Forward/Reverse names
					if (lexRef.TargetsRS.Count > 0)
					{
						ICmObject firstObj = lexRef.TargetsRS[0];
						ILexEntry firstEntry = null;
						switch (firstObj.ClassID)
						{
							case LexEntryTags.kClassId:
								firstEntry = (ILexEntry) firstObj;
								break;
							case LexSenseTags.kClassId:
								firstEntry = firstObj.OwnerOfClass<ILexEntry>();
								break;
						}

						if (firstEntry != lexEntry)
						{
							name = lexRefType.ReverseName.BestAnalysisAlternative.Text;
							if (string.IsNullOrEmpty(name))
								name = lexRefType.ReverseAbbreviation.BestAnalysisAlternative.Text;
						}

						if (mappingType == LexRefTypeTags.MappingTypes.kmtSenseTree
							|| mappingType == LexRefTypeTags.MappingTypes.kmtEntryTree
							|| mappingType == LexRefTypeTags.MappingTypes.kmtEntryOrSenseTree)
						{
							parentEntry = firstEntry;
						}
					}
					break;
			}
			return name.Normalize();
		}

		public LexiconSense AddSense()
		{
			LexiconSense sense = null;
			bool lexemeAdded = false;
			m_lexicon.UpdatingEntries = true;
			try
			{
				NonUndoableUnitOfWorkHelper.Do(m_lexicon.Cache.ActionHandlerAccessor, () =>
					{
						ILexEntry entry;
						if (!m_lexicon.TryGetEntry(m_key, out entry))
						{
							entry = m_lexicon.CreateEntry(m_key);
							lexemeAdded = true;
						}

						if (entry.AllSenses.Count == 1 && entry.SensesOS[0].Gloss.StringCount == 0)
						{
							// An empty sense exists (probably was created during a call to AddLexeme)
							sense = new LexSenseLexiconSense(m_lexicon, m_key, entry.SensesOS[0]);
						}
						else
						{
							ILexSense newSense = m_lexicon.Cache.ServiceLocator.GetInstance<ILexSenseFactory>().Create(
								entry, new SandboxGenericMSA(), (string)null);
							sense = new LexSenseLexiconSense(m_lexicon, m_key, newSense);
						}
					});
			}
			finally
			{
				m_lexicon.UpdatingEntries = false;
			}
			if (lexemeAdded)
				m_lexicon.OnLexemeAdded(this);
			m_lexicon.OnLexiconSenseAdded(this, sense);
			return sense;
		}

		public void RemoveSense(LexiconSense sense)
		{
			ILexEntry entry;
			if (!m_lexicon.TryGetEntry(m_key, out entry))
				return;

			NonUndoableUnitOfWorkHelper.Do(m_lexicon.Cache.ActionHandlerAccessor, () =>
				{
					var leSense = (LexSenseLexiconSense)sense;
					if (entry.AllSenses.Count == 1)
					{
						foreach (int ws in leSense.Sense.Gloss.AvailableWritingSystemIds)
							leSense.Sense.Gloss.set_String(ws, (ITsString) null);
					}
					else
					{
						leSense.Sense.Delete();
					}
				});
		}

		#endregion

		public override string ToString()
		{
			return DisplayString;
		}

		public override bool Equals(object obj)
		{
			var other = obj as FdoLexEntryLexeme;
			return other != null && m_key.Equals(other.m_key);
		}

		public override int GetHashCode()
		{
			return m_key.GetHashCode();
		}

		private class LexSenseLexiconSense : LexiconSense
		{
			private readonly FdoLexicon m_lexicon;
			private readonly LexemeKey m_lexemeKey;
			private readonly ILexSense m_lexSense;

			public LexSenseLexiconSense(FdoLexicon lexicon, LexemeKey lexemeKey, ILexSense lexSense)
			{
				m_lexicon = lexicon;
				m_lexemeKey = lexemeKey;
				m_lexSense = lexSense;
			}

			internal ILexSense Sense
			{
				get { return m_lexSense; }
			}

			public string Id
			{
				get
				{
					return m_lexSense.Guid.ToString();
				}
			}

			public string SenseNumber
			{
				get
				{
					return m_lexSense.LexSenseOutline.Text;
				}
			}

			public string Category
			{
				get
				{
					return m_lexSense.MorphoSyntaxAnalysisRA == null ? "" : m_lexSense.MorphoSyntaxAnalysisRA.PartOfSpeechForWsTSS(m_lexicon.Cache.DefaultAnalWs).Text;
				}
			}

			public IEnumerable<LanguageText> Definitions
			{
				get
				{
					IMultiString definition = m_lexSense.Definition;
					var defs = new List<LanguageText>();
					foreach (CoreWritingSystemDefinition ws in m_lexicon.Cache.ServiceLocator.WritingSystems.CurrentAnalysisWritingSystems)
					{
						ITsString tss = definition.StringOrNull(ws.Handle);
						if (tss != null)
							defs.Add(new FdoLanguageText(ws.Id, tss.Text.Normalize()));
					}
					return defs;
				}
			}

			public IEnumerable<LanguageText> Glosses
			{
				get
				{
					IMultiUnicode gloss = m_lexSense.Gloss;
					var glosses = new List<LanguageText>();
					foreach (CoreWritingSystemDefinition ws in m_lexicon.Cache.ServiceLocator.WritingSystems.CurrentAnalysisWritingSystems)
					{
						ITsString tss = gloss.StringOrNull(ws.Handle);
						if (tss != null)
							glosses.Add(new FdoLanguageText(ws.Id, tss.Text.Normalize()));
					}
					return glosses;
				}
			}

			public LanguageText AddGloss(string language, string text)
			{
				LanguageText lexGloss = null;
				NonUndoableUnitOfWorkHelper.Do(m_lexSense.Cache.ActionHandlerAccessor, () =>
					{
						CoreWritingSystemDefinition ws;
						if (!m_lexicon.Cache.ServiceLocator.WritingSystemManager.TryGet(language, out ws))
							throw new ArgumentException("The specified language is unrecognized.", "language");
						m_lexSense.Gloss.set_String(ws.Handle, text.Normalize(NormalizationForm.FormD));
						lexGloss = new FdoLanguageText(language, text);
					});
				m_lexicon.OnLexiconGlossAdded(new FdoLexEntryLexeme(m_lexicon, m_lexemeKey), this, lexGloss);
				return lexGloss;
			}

			public void RemoveGloss(string language)
			{
				NonUndoableUnitOfWorkHelper.Do(m_lexSense.Cache.ActionHandlerAccessor, () =>
					{
						CoreWritingSystemDefinition ws;
						if (!m_lexicon.Cache.ServiceLocator.WritingSystemManager.TryGet(language, out ws))
							throw new ArgumentException("The specified language is unrecognized.", "language");
						m_lexSense.Gloss.set_String(ws.Handle, (ITsString) null);
					});
			}

			public IEnumerable<LexiconSemanticDomain> SemanticDomains
			{
				get
				{
					return m_lexSense.SemanticDomainsRC.Select(sd => new FdoSemanticDomain(sd.ShortName.Normalize())).ToArray();
				}
			}

			public override bool Equals(object obj)
			{
				var other = obj as LexSenseLexiconSense;
				return other != null && m_lexSense == other.m_lexSense;
			}

			public override int GetHashCode()
			{
				return m_lexSense.GetHashCode();
			}
		}
	}
	#endregion
}
