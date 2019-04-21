﻿// Copyright (c) 2015 SIL International
// This software is licensed under the LGPL, version 2.1 or later
// (http://www.gnu.org/licenses/lgpl-2.1.html)

using System;
using System.Collections.Generic;
using System.Linq;
using SIL.LCModel.Core.Text;
using SIL.FieldWorks.Common.Controls;
using SIL.LCModel.Core.KernelInterfaces;
using SIL.LCModel;

namespace SIL.FieldWorks.XWorks.LexEd
{
	/// <summary>
	/// This is the search engine for ReversalEntryGoDlg.
	/// </summary>
	internal class ReversalEntryGoSearchEngine : SearchEngine
	{
		private readonly IReversalIndex m_reversalIndex;
		private readonly IReversalIndexEntryRepository m_revEntryRepository;

		public ReversalEntryGoSearchEngine(LcmCache cache, IReversalIndex reversalIndex)
			: base(cache, SearchType.Prefix)
		{
			m_reversalIndex = reversalIndex;
			m_revEntryRepository = Cache.ServiceLocator.GetInstance<IReversalIndexEntryRepository>();
		}

		protected override IEnumerable<ITsString> GetStrings(SearchField field, ICmObject obj)
		{
			var rie = (IReversalIndexEntry) obj;

			int ws = field.String.get_WritingSystemAt(0);
			switch (field.Flid)
			{
				case ReversalIndexEntryTags.kflidReversalForm:
					var form = rie.ReversalForm.StringOrNull(ws);
					if (form != null && form.Length > 0)
						yield return form;
					break;

				default:
					throw new ArgumentException("Unrecognized field.", "field");
			}
		}

		protected override IList<ICmObject> GetSearchableObjects()
		{
			return m_reversalIndex.AllEntries.Cast<ICmObject>().ToArray();
		}

		protected override bool IsIndexResetRequired(int hvo, int flid)
		{
			switch (flid)
			{
				case ReversalIndexTags.kflidEntries:
					return hvo == m_reversalIndex.Hvo;
				case ReversalIndexEntryTags.kflidReversalForm:
					return m_revEntryRepository.GetObject(hvo).ReversalIndex == m_reversalIndex;
			}
			return false;
		}

		protected override bool IsFieldMultiString(SearchField field)
		{
			switch (field.Flid)
			{
				case ReversalIndexEntryTags.kflidReversalForm:
					return true;
			}

			throw new ArgumentException("Unrecognized field.", "field");
		}
	}
}
