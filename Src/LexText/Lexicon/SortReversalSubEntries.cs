﻿// Copyright (c) 2015 SIL International
// This software is licensed under the LGPL, version 2.1 or later
// (http://www.gnu.org/licenses/lgpl-2.1.html)

using System;
using System.Collections.Generic;
using System.Windows.Forms;
using SIL.FieldWorks.Common.COMInterfaces;
using SIL.FieldWorks.FDO;
using SIL.FieldWorks.FDO.Infrastructure;
using SIL.FieldWorks.FwCoreDlgs;
using SIL.FieldWorks.Language;

namespace SIL.FieldWorks.XWorks.LexEd
{
	class SortReversalSubEntries : IUtility
	{
		public UtilityDlg Dialog { set; private get; }

		public string Label { get { return LexEdStrings.SortReversalSubentries_Label; } }

		public void LoadUtilities()
		{
			Dialog.Utilities.Items.Add(this);
		}

		public void OnSelection()
		{
			Dialog.WhenDescription = LexEdStrings.ksWhenToSortReversalSubentries;
			Dialog.WhatDescription = LexEdStrings.ksWhatIsSortReversalSubentries;
			Dialog.RedoDescription = LexEdStrings.ksGenericUtilityCannotUndo;
		}

		public void Process()
		{
			var cache = (FdoCache)Dialog.Mediator.PropertyTable.GetValue("cache");
			NonUndoableUnitOfWorkHelper.DoSomehow(cache.ActionHandlerAccessor, () =>
			{
				SortReversalSubEntriesInPlace(cache);
				MessageBox.Show(Dialog, LexEdStrings.SortReversalSubEntries_CompletedContent, LexEdStrings.SortReversalSubEntries_CompletedTitle, MessageBoxButtons.OK, MessageBoxIcon.Information);
			});
		}

		internal static void SortReversalSubEntriesInPlace(FdoCache cache)
		{
			var allReversalIndexes = cache.ServiceLocator.GetInstance<IReversalIndexRepository>().AllInstances();
			foreach(var reversalIndex in allReversalIndexes)
			{
				var ws = reversalIndex.WritingSystem;
				foreach(var reversalIndexEntry in reversalIndex.EntriesOC)
				{
					if(reversalIndexEntry.SubentriesOS.Count > 0)
					{
						var subEntryArray = reversalIndexEntry.SubentriesOS.ToArray();
						Array.Sort(subEntryArray, new ReversalSubEntryIcuComparer(cache, ws));
						for(var i = 0; i < subEntryArray.Length; ++i)
							reversalIndexEntry.SubentriesOS.Insert(i, subEntryArray[i]);
					}
				}
			}
		}

		internal class ReversalSubEntryIcuComparer : IComparer<IReversalIndexEntry>
		{
			private int m_ws;
			private ManagedLgIcuCollator m_collator;

			public ReversalSubEntryIcuComparer(FdoCache cache, string ws)
			{
				m_collator = new ManagedLgIcuCollator();
				m_ws = cache.WritingSystemFactory.GetWsFromStr(ws);
				m_collator.Open(ws);
			}

			public int Compare(IReversalIndexEntry x, IReversalIndexEntry y)
			{
				var xString = x.ReversalForm.get_String(m_ws);
				var yString = y.ReversalForm.get_String(m_ws);
				if(m_collator == null)
				{
					return string.Compare(xString.Text, yString.Text, StringComparison.OrdinalIgnoreCase);
				}
				return m_collator.Compare(xString.Text, yString.Text, LgCollatingOptions.fcoIgnoreCase);
			}
		}
	}
}