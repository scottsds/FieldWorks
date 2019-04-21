// Copyright (c) 2011-2017 SIL International
// This software is licensed under the LGPL, version 2.1 or later
// (http://www.gnu.org/licenses/lgpl-2.1.html)

using System;
using System.Collections.Generic;

namespace SIL.FieldWorks.Common.FwUtils
{
	/// ----------------------------------------------------------------------------------------
	/// <summary>
	/// A set that collects objects for later disposal
	/// </summary>
	/// ----------------------------------------------------------------------------------------
	public class DisposableObjectsSet<T> : IDisposable where T : class
	{
		/// <summary/>
		protected readonly HashSet<IDisposable> m_ObjectsToDispose = new HashSet<IDisposable>();

		#region Disposable stuff
		#if DEBUG
		/// <summary/>
		~DisposableObjectsSet()
		{
			Dispose(false);
		}
		#endif

		/// <summary/>
		public bool IsDisposed { get; private set; }

		/// <summary/>
		public void Dispose()
		{
			Dispose(true);
			GC.SuppressFinalize(this);
		}

		/// <summary/>
		protected virtual void Dispose(bool fDisposing)
		{
			System.Diagnostics.Debug.WriteLineIf(!fDisposing, "****** Missing Dispose() call for " + GetType() + " *******");
			if (fDisposing && !IsDisposed)
			{
				// dispose managed and unmanaged objects
				DisposeAllObjects();
			}
			IsDisposed = true;
		}
		#endregion

		/// <summary>
		/// Disposes all objects in the set, but doesn't dispose the set itself.
		/// </summary>
		public void DisposeAllObjects()
		{
			foreach (var disposable in m_ObjectsToDispose)
				disposable.Dispose();
			m_ObjectsToDispose.Clear();
		}

		/// <summary>
		/// Registers an object for later disposal. If the object is already registered or
		/// doesn't implement IDisposable nothing happens.
		/// </summary>
		public void Add(T obj)
		{
			var disposable = obj as IDisposable;
			if (disposable == null)
				return;
			m_ObjectsToDispose.Add(disposable);
		}

		/// <summary>
		/// Returns <c>true</c> if obj is contained in the set.
		/// </summary>
		public bool Contains(T obj)
		{
			if (m_ObjectsToDispose.Count == 0)
				return false;

			var disposable = obj as IDisposable;
			if (disposable == null)
				return false;
			return m_ObjectsToDispose.Contains(disposable);
		}

		/// <summary>
		/// Removes an object from the set of objects that need to be disposed.
		/// If the object doesn't implement IDisposable or isn't registered
		/// for disposal, nothing happens.
		/// </summary>
		public bool Remove(T obj)
		{
			var disposable = obj as IDisposable;
			if (disposable == null)
				return false;
			return m_ObjectsToDispose.Remove(disposable);
		}
	}
}
