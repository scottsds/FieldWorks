﻿// Copyright (c) 2015-2017 SIL International
// This software is licensed under the LGPL, version 2.1 or later
// (http://www.gnu.org/licenses/lgpl-2.1.html)

using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using SIL.LCModel.Core.Text;
using SIL.LCModel.Core.KernelInterfaces;
using SIL.LCModel;
using SIL.LCModel.Infrastructure;
using SIL.ObjectModel;
using SIL.LCModel.Utils;
using XCore;

namespace SIL.FieldWorks.Common.Controls
{
	/// <summary>Struct pairing a field ID with a TsString</summary>
	public struct SearchField
	{
		private readonly int m_flid;
		private readonly ITsString m_tss;

		/// <summary/>
		public SearchField(int flid, ITsString tss)
		{
			m_flid = flid;
			m_tss = tss;
		}

		/// <summary/>
		public int Flid { get { return m_flid; } }

		/// <summary/>
		public ITsString String { get { return m_tss; } }
	}

	/// <summary>
	/// An abstract class for performing indexing and searching asynchronously.
	/// </summary>
	public abstract class SearchEngine : DisposableBase, IVwNotifyChange
	{
		/// <summary>
		/// Gets the search engine.
		/// </summary>
		public static SearchEngine Get(Mediator mediator, PropertyTable propertyTable, string propName, Func<SearchEngine> searchEngineFactory)
		{
			var searchEngine = propertyTable.GetValue<SearchEngine>(propName);
			if (searchEngine == null)
			{
				searchEngine = searchEngineFactory();
				propertyTable.SetProperty(propName, searchEngine, true);
				propertyTable.SetPropertyDispose(propName, true);
				propertyTable.SetPropertyPersistence(propName, false);
			}
			return searchEngine;
		}

		private readonly LcmCache m_cache;
		private readonly StringSearcher<int> m_searcher;

		private IList<ICmObject> m_searchableObjs;
		private readonly Dictionary<Tuple<int, int>, int> m_indexObjPos;
		private readonly ConsumerThread<int, SearchField[]> m_thread;
		private readonly object m_syncRoot;
		private readonly SynchronizationContext m_synchronizationContext;

		/// <summary>
		/// Occurs when the search is completed.
		/// </summary>
		public event EventHandler<SearchCompletedEventArgs> SearchCompleted;

		/// <summary>
		/// Initializes a new instance of the <see cref="SearchEngine"/> class.
		/// </summary>
		protected SearchEngine(LcmCache cache, SearchType type)
		{
			m_cache = cache;
			m_searcher = new StringSearcher<int>(type, m_cache.ServiceLocator.WritingSystemManager);
			m_thread = new ConsumerThread<int, SearchField[]>(HandleWork);
			m_synchronizationContext = SynchronizationContext.Current;
			m_syncRoot = new object();
			m_indexObjPos = new Dictionary<Tuple<int, int>, int>();

			m_cache.DomainDataByFlid.AddNotification(this);

			m_thread.Start();
		}

		/// <summary>
		/// Override to dispose managed resources.
		/// </summary>
		protected override void DisposeManagedResources()
		{
			m_cache.DomainDataByFlid.RemoveNotification(this);

			m_thread.Stop();
			m_thread.Dispose();
		}

		/// <summary>
		/// Gets the searchable strings of an object.
		/// </summary>
		protected abstract IEnumerable<ITsString> GetStrings(SearchField field, ICmObject obj);

		/// <summary>
		/// Gets the searchable objects.
		/// </summary>
		protected abstract IList<ICmObject> GetSearchableObjects();

		/// <summary>
		/// Determines if an index reset is required.
		/// </summary>
		protected abstract bool IsIndexResetRequired(int hvo, int flid);

		/// <summary>
		/// Determines if the specified field is a multi-string or multi-unicode field.
		/// </summary>
		protected abstract bool IsFieldMultiString(SearchField field);

		/// <summary>
		/// Gets the cache.
		/// </summary>
		protected LcmCache Cache
		{
			get { return m_cache; }
		}

		/// <summary>
		/// Searches the specified fields asynchronously.
		/// </summary>
		public void SearchAsync(IEnumerable<SearchField> fields)
		{
			m_thread.EnqueueWork(fields.ToArray());
		}

		/// <summary>
		/// Searches the specified fields.
		/// N.B. This version is currently only used in testing.
		/// </summary>
		public IEnumerable<int> Search(IEnumerable<SearchField> fields)
		{
			return FilterResults(PerformSearch(fields.ToArray(), () => false));
		}

		/// <summary>
		/// Gets a value indicating whether the search engine is searching.
		/// </summary>
		public bool IsBusy
		{
			get { return !m_thread.IsIdle; }
		}

		/// <summary>
		/// Builds the search index.
		/// </summary>
		private int BuildIndex(int i, SearchField field, Func<bool> isSearchCanceled)
		{
			if (m_searchableObjs == null)
				m_searchableObjs = GetSearchableObjects();

			for (; i < m_searchableObjs.Count; i++)
			{
				if (isSearchCanceled())
					break;
				foreach (ITsString tss in GetStrings(field, m_searchableObjs[i]))
					m_searcher.Add(m_searchableObjs[i].Hvo, field.Flid, tss);
			}
			return i;
		}

		private static bool IsSearchCanceled(IQueueAccessor<int, SearchField[]> queue)
		{
			return queue.StopRequested || queue.HasWork;
		}

		private void HandleWork(IQueueAccessor<int, SearchField[]> queue)
		{
			SearchField[] work = queue.GetAllWorkItems().Last();

			if (IsSearchCanceled(queue))
				return;

			IEnumerable<int> results = PerformSearch(work, () => IsSearchCanceled(queue));

			if (results == null || IsSearchCanceled(queue))
				return;

			m_synchronizationContext.Post(OnSearchCompleted, new SearchCompletedEventArgs(work, FilterResults(results)));
		}

		/// <summary>
		/// If some objects need to be filtered out of the results (for instance the item we started from in the merge dialog)
		/// then this function can be used to do it.
		/// </summary>
		protected virtual IEnumerable<int> FilterResults(IEnumerable<int> results)
		{
			return results;
		}

		private IEnumerable<int> PerformSearch(IList<SearchField> fields, Func<bool> isSearchCanceled)
		{
			var results = new HashSet<int>();
			lock (m_syncRoot)
			{
				foreach (SearchField field in fields)
				{
					if (isSearchCanceled())
						return null;

					Tuple<int, int> key = IsFieldMultiString(field) ? Tuple.Create(field.Flid, field.String.get_WritingSystemAt(0)) : Tuple.Create(field.Flid, 0);
					int pos;
					if (!m_indexObjPos.TryGetValue(key, out pos))
					{
						pos = 0;
					}

					if (m_searchableObjs == null || pos < m_searchableObjs.Count)
					{
						// only use the IWorkerThreadReadHandler if we are executing on the worker thread
						if (SynchronizationContext.Current == m_synchronizationContext)
						{
							pos = BuildIndex(pos, field, isSearchCanceled);
						}
						else
						{
							using (new WorkerThreadReadHelper(m_cache.ServiceLocator.GetInstance<IWorkerThreadReadHandler>()))
								pos = BuildIndex(pos, field, isSearchCanceled);
						}
						m_indexObjPos[key] = pos;
					}

				}

				foreach (SearchField field in fields)
				{
					if (isSearchCanceled())
						return null;
					results.UnionWith(m_searcher.Search(field.Flid, field.String));
				}
			}

			if (isSearchCanceled())
				return null;

			return results;
		}

		void IVwNotifyChange.PropChanged(int hvo, int tag, int ivMin, int cvIns, int cvDel)
		{
			if (IsIndexResetRequired(hvo, tag))
			{
				lock (m_syncRoot)
				{
					m_searcher.Clear();
					m_indexObjPos.Clear();
					m_searchableObjs = null;
				}
			}
		}

		private void OnSearchCompleted(object e)
		{
			if (SearchCompleted != null)
				SearchCompleted(this, (SearchCompletedEventArgs) e);
		}
	}
}
