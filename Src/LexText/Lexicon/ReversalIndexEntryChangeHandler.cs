// Copyright (c) 2015 SIL International
// This software is licensed under the LGPL, version 2.1 or later
// (http://www.gnu.org/licenses/lgpl-2.1.html)

using System;
using System.Diagnostics;
using SIL.FieldWorks.Common.Framework;
using SIL.LCModel;

namespace SIL.FieldWorks.XWorks.LexEd
{
	/// <summary/>
	public class ReversalIndexEntryChangeHandler : IRecordChangeHandler
	{
		#region Data members

		/// <summary>Reversal entry being monitored for changes.</summary>
		protected IReversalIndexEntry m_rie;
		/// <summary>original citation form</summary>
		protected string m_originalForm;
		/// <summary></summary>
		protected IRecordListUpdater m_rlu;

		#endregion Data members

		#region IDisposable & Co. implementation
		// Region last reviewed: never

		/// <summary>
		/// Check to see if the object has been disposed.
		/// All public Properties and Methods should call this
		/// before doing anything else.
		/// </summary>
		public void CheckDisposed()
		{
			if (IsDisposed)
				throw new ObjectDisposedException(String.Format("'{0}' in use after being disposed.", GetType().Name));
		}

		/// <summary>
		/// See if the object has been disposed.
		/// </summary>
		public bool IsDisposed { get; private set; }

		/// <summary>
		/// Finalizer, in case client doesn't dispose it.
		/// Force Dispose(false) if not already called
		/// </summary>
		~ReversalIndexEntryChangeHandler()
		{
			Dispose(false);
			// The base class finalizer is called automatically.
		}

		/// <remarks>Must not be virtual.</remarks>
		public void Dispose()
		{
			Dispose(true);
			// This object will be cleaned up by the Dispose method.
			// Therefore, you should call GC.SupressFinalize to
			// take this object off the finalization queue
			// and prevent finalization code for this object
			// from executing a second time.
			GC.SuppressFinalize(this);
		}

		/// <summary>
		/// Executes in two distinct scenarios.
		///
		/// 1. If disposing is true, the method has been called directly
		/// or indirectly by a user's code via the Dispose method.
		/// Both managed and unmanaged resources can be disposed.
		///
		/// 2. If disposing is false, the method has been called by the
		/// runtime from inside the finalizer and you should not reference (access)
		/// other managed objects, as they already have been garbage collected.
		/// Only unmanaged resources can be disposed.
		/// </summary>
		/// <param name="disposing"></param>
		/// <remarks>
		/// If any exceptions are thrown, that is fine.
		/// If the method is being done in a finalizer, it will be ignored.
		/// If it is thrown by client code calling Dispose,
		/// it needs to be handled by fixing the bug.
		///
		/// If subclasses override this method, they should call the base implementation.
		/// </remarks>
		protected virtual void Dispose(bool disposing)
		{
			Debug.WriteLineIf(!disposing, "****** Missing Dispose() call for " + GetType().Name + ". ****** ");
			// Must not be run more than once.
			if (IsDisposed)
				return;

			if (disposing)
			{
				if (Disposed != null)
					Disposed(this, new EventArgs());

				// Dispose managed resources here.
				if (m_rlu != null)
					m_rlu.RecordChangeHandler = null;
			}

			// Dispose unmanaged resources here, whether disposing is true or false.
			m_rie = null;
			m_originalForm = null;
			m_rlu = null;

			IsDisposed = true;
		}

		#endregion IDisposable & Co. implementation

		#region IRecordChangeHandler implementation

		/// <summary>
		/// Let users know it is being disposed
		/// </summary>
		public event EventHandler Disposed;

		/// <summary>
		/// True, if the updater was not null in the Setup call, otherwise false.
		/// </summary>
		public bool HasRecordListUpdater
		{
			get { return m_rlu != null; }
		}

		/// <summary></summary>
		public void Setup(object record, IRecordListUpdater rlu, LcmCache cache)
		{
			CheckDisposed();

			Debug.Assert(record is IReversalIndexEntry);
			var rie = (IReversalIndexEntry)record;
			if (m_rlu == null && rlu != null && m_rie == rie)
			{
				m_rlu = rlu;
				m_rlu.RecordChangeHandler = this;
				m_rlu.UpdateList(true);
			}
			else
			{
				m_rie = rie;
				Debug.Assert(m_rie != null);
				var ws = m_rie.Services.WritingSystemManager.GetWsFromStr(m_rie.ReversalIndex.WritingSystem);
				m_originalForm = m_rie.ReversalForm.get_String(ws).Text;
				if (rlu != null)
				{
					m_rlu = rlu;
					m_rlu.RecordChangeHandler = this;
				}
			}
		}

		/// <summary>Handle possible homograph number changes:
		/// 1. Possibly remove homograph from original citation form.
		/// 2. Possibly add homograph for new citation form.
		/// </summary>
		public void Fixup(bool fRefreshList)
		{
			CheckDisposed();

			Debug.Assert(m_rie != null);

			if (!m_rie.IsValidObject)
			{
				// If our old entry isn't even valid any more, something has deleted it,
				// and whatever did so should have fixed up the list. We really don't want
				// to reload the whole thing if we don't need to (takes ages in a big lexicon),
				// so do nothing...JohnT
				return;
			}

			var ws = m_rie.Services.WritingSystemManager.GetWsFromStr(m_rie.ReversalIndex.WritingSystem);
			var currentForm = m_rie.ReversalForm.get_String(ws).Text;
			if (currentForm == m_originalForm)
				return; // No relevant changes, so do nothing.

			// Fix it so that another call will do the right thing.
			m_originalForm = currentForm;

			if (fRefreshList && m_rlu != null)
				m_rlu.UpdateList(false);
		}

		#endregion IRecordChangeHandler implementation
	}
}
