﻿// Copyright (c) 2015-2017 SIL International
// This software is licensed under the LGPL, version 2.1 or later
// (http://www.gnu.org/licenses/lgpl-2.1.html)

using System;
using System.Collections.Generic;
using SIL.LCModel.Core.KernelInterfaces;
using SIL.LCModel.Application;
using SIL.LCModel.Utils;

namespace SIL.FieldWorks.Common.Controls
{
	/// <summary>
	/// This decorator modifies certain sequence properties by filtering from them items not in a master list.
	/// It is not currently used (as of LT-10260) since the new DictionaryPublicationDecorator achieves the purpose
	/// more effectively. However for now I'm keeping the source code in case we find a reason again to hide
	/// references to filtered-out items. To enable it, uncomment stuff in XmlSeqView.GetSda().
	/// </summary>
	public class FilterSdaDecorator : DomainDataByFlidDecoratorBase
	{
		private int m_mainFlid;
		private int m_hvoRoot;
		private Dictionary<int, ITestItem> m_filterFlids = new Dictionary<int, ITestItem>();
		private readonly HashSet<int> m_validHvos;

		/// <summary>
		/// Make one that wraps the specified cache and passes items in the specified property of the specified root object.
		/// </summary>
		public FilterSdaDecorator(ISilDataAccessManaged domainDataByFlid, int mainFlid, int hvoRoot)
			: base(domainDataByFlid)
		{
			m_mainFlid = mainFlid;
			m_hvoRoot = hvoRoot;
			int chvoReal = BaseSda.get_VecSize(m_hvoRoot, m_mainFlid);
			using (ArrayPtr arrayPtr = MarshalEx.ArrayToNative<int>(chvoReal))
			{
				BaseSda.VecProp(m_hvoRoot, m_mainFlid, chvoReal, out chvoReal, arrayPtr);
				m_validHvos = new HashSet<int>(MarshalEx.NativeToArray<int>(arrayPtr, chvoReal));
			}
		}

		/// <summary>
		/// Set the filter flids from a string that contains semi-colon-separated sequence of Class.Field strings.
		/// </summary>
		/// <param name="input"></param>
		public void SetFilterFlids(string input)
		{
			foreach (string field in input.Split(';'))
			{
				string[] parts = field.Trim().Split(':');
				if (parts.Length != 2)
				   throw new ArgumentException("Expected sequence of class.field:class.field;class.field:class.field but got " + input);
				int flidMain = Flid(parts[0]);
				int flidRel = Flid(parts[1]);
				m_filterFlids[flidMain] = new TestItemAtomicFlid(flidRel);
			}
		}

		int Flid(string field)
		{
			string[] parts = field.Trim().Split('.');
			if (parts.Length != 2)
				throw new ArgumentException("Expected class.field but got " + field);
			return(int)MetaDataCache.GetFieldId(parts[0], parts[1], true);

		}

		/// <summary>
		/// Override to filter the specified properties.
		/// </summary>
		public override int get_VecItem(int hvo, int tag, int index)
		{
			ITestItem tester;
			if (!m_filterFlids.TryGetValue(tag, out tester))
				return base.get_VecItem(hvo, tag, index);
			int chvoReal = BaseSda.get_VecSize(hvo, tag);
			using (ArrayPtr arrayPtr = MarshalEx.ArrayToNative<int>(chvoReal))
			{
				BaseSda.VecProp(hvo, tag, chvoReal, out chvoReal, arrayPtr);
				int[] candidates = MarshalEx.NativeToArray<int>(arrayPtr, chvoReal);
				int iresult = 0;
				for (int icandidate = 0; icandidate < candidates.Length; icandidate++)
				{
					if (tester.Test(candidates[icandidate], BaseSda, m_validHvos))
					{
						if (iresult == index)
							return candidates[icandidate];
						iresult++;
					}
				}
				throw new IndexOutOfRangeException("filtered vector does not contain that many items (wanted " + index +
												  " but have only " + iresult + ")");
			}
		}

		/// <summary>
		/// Override to filter the specified properties.
		/// </summary>
		public override int get_VecSize(int hvo, int tag)
		{
			ITestItem tester;
			if (!m_filterFlids.TryGetValue(tag, out tester))
				return base.get_VecSize(hvo, tag);
			int chvoReal = BaseSda.get_VecSize(hvo, tag);
			using (ArrayPtr arrayPtr = MarshalEx.ArrayToNative<int>(chvoReal))
			{
				BaseSda.VecProp(hvo, tag, chvoReal, out chvoReal, arrayPtr);
				int[] candidates = MarshalEx.NativeToArray<int>(arrayPtr, chvoReal);
				int iresult = 0;
				for (int icandidate = 0; icandidate < candidates.Length; icandidate++)
				{
					if (tester.Test(candidates[icandidate], BaseSda, m_validHvos))
						iresult++;
				}
				return iresult;
			}
		}

		/// <summary>
		/// Override to filter the specified properties.
		/// </summary>
		public override void VecProp(int hvo, int tag, int chvoMax, out int chvo, ArrayPtr rghvo)
		{
			ITestItem tester;
			if (!m_filterFlids.TryGetValue(tag, out tester))
			{
				base.VecProp(hvo, tag, chvoMax, out chvo, rghvo);
				return;
			}
			int chvoReal = BaseSda.get_VecSize(hvo, tag);
			using (ArrayPtr arrayPtr = MarshalEx.ArrayToNative<int>(chvoReal))
			{
				BaseSda.VecProp(hvo, tag, chvoReal, out chvoReal, arrayPtr);
				int[] candidates = MarshalEx.NativeToArray<int>(arrayPtr, chvoReal);
				int[] results = new int[chvoMax];
				int iresult = 0;
				for (int icandidate = 0; icandidate < candidates.Length; icandidate++)
				{
					if (tester.Test(candidates[icandidate], BaseSda, m_validHvos))
						results[iresult++] = candidates[icandidate];
				}
				chvo = iresult;
				MarshalEx.ArrayToNative(rghvo, chvoMax, results);
			}
		}
	}

	/// <summary>
	/// Test an item in a filtered property to see whether it should be included.
	/// </summary>
	interface ITestItem
	{
		bool Test(int hvo, ISilDataAccess sda, ISet<int> validHvos);
	}

	/// <summary>
	/// Test an item by following the specified flid (an atomic object property).
	/// Pass if the destination is in the validHvos set.
	/// </summary>
	class TestItemAtomicFlid : ITestItem
	{
		private int m_flid;
		public TestItemAtomicFlid(int flid)
		{
			m_flid = flid;
		}
		public bool Test(int hvo, ISilDataAccess sda, ISet<int> validHvos)
		{
			return validHvos.Contains(sda.get_ObjectProp(hvo, m_flid));
		}
	}
}
