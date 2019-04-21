// Copyright (c) 2006-2013 SIL International
// This software is licensed under the LGPL, version 2.1 or later
// (http://www.gnu.org/licenses/lgpl-2.1.html)

using System;
using System.Collections.Generic;
using System.Diagnostics;
using SIL.FieldWorks.Resources;
using SIL.LCModel;
using SIL.LCModel.DomainServices;
using SIL.LCModel.Infrastructure;
using SIL.Reporting;
using SIL.LCModel.Core.Scripture;

namespace ParatextImport
{
	/// <summary>
	/// Manages info and stuff for doing import in such a way that it can be undone.
	/// </summary>
	public class UndoImportManager
	{
		#region Data members
		// saved version for immediate merge (contains books from original)
		// saved version into which to put selected imported books.
		private int m_lastBookAddedToImportedBooks;
		private int m_hMark;
		private readonly IScripture m_scr;
		private IScrBookAnnotations m_annotations;

		#endregion

		#region Constructor
		/// <summary>
		/// Initializes a new instance of the <see cref="UndoImportManager"/> class.
		/// </summary>
		/// <param name="cache">The cache.</param>
		public UndoImportManager(LcmCache cache)
		{
			Cache = cache;
			m_scr = Cache.LanguageProject.TranslatedScriptureOA;
			ImportedBooks = new Dictionary<int, bool>();
		}
		#endregion

		#region Public methods
		/// <summary>
		/// This must be called before starting import.
		/// </summary>
		public void StartImportingFiles()
		{
			Debug.Assert(Cache.DomainDataByFlid.GetActionHandler() != null);
			Debug.Assert(Cache.DomainDataByFlid.GetActionHandler().CurrentDepth == 0);
			m_hMark = Cache.DomainDataByFlid.GetActionHandler().Mark();
			var actionHandler = Cache.ActionHandlerAccessor;
			actionHandler.BeginUndoTask("Create saved version", "Create saved version");
			BackupVersion = GetOrCreateVersion(Properties.Resources.kstidSavedVersionDescriptionOriginal);
		}

		/// <summary>
		/// Adds the new book (to the saved version...create it if need be) for which the
		/// vernacular is about to be imported.
		/// </summary>
		/// <param name="nCanonicalBookNumber">The canonical book number.</param>
		/// <param name="description">Description to use for the newly created imported version
		/// if necessary.</param>
		/// <param name="title">The title of the newly created book.</param>
		/// <returns>The newly created book (which has been added to the imported version)</returns>
		public IScrBook AddNewBook(int nCanonicalBookNumber, string description, out IStText title)
		{
			if (ImportedVersion == null)
			{
				ImportedVersion = GetOrCreateVersion(description);
			}
			var existingBook = SetCurrentBook(nCanonicalBookNumber, true);

			if (existingBook != null)
			{
				if (m_lastBookAddedToImportedBooks == 0)
				{
					// We've been asked to create a book we have already imported (typically
					// reading multiple independent SF files).
					title = existingBook.TitleOA;
					return existingBook;
				}

				// Replace any previous book with the one we're about to import.
				ImportedVersion.BooksOS.Remove(existingBook);
			}

			var newScrBook = Cache.ServiceLocator.GetInstance<IScrBookFactory>().Create(ImportedVersion.BooksOS, nCanonicalBookNumber, out title);
			return newScrBook;
		}

		/// <summary>
		/// Called when we are about to import a book but are not importing the main translation.
		/// </summary>
		/// <param name="nCanonicalBookNumber"></param>
		/// <param name="fMakeBackup">This should be true if we are importing a back
		/// translation.</param>
		/// <returns>The version of the book in the imported version if available; otherwise
		/// the current version of the book (in which case a backup will be made first if
		/// <c>fMakeBackup</c> is <c>true</c>.</returns>
		public IScrBook PrepareBookNotImportingVern(int nCanonicalBookNumber, bool fMakeBackup)
		{
			var isvBook = SetCurrentBook(nCanonicalBookNumber, false);
			if (isvBook != null && ImportedBooks.ContainsKey(nCanonicalBookNumber))
				return isvBook;

			var cvBook = m_scr.FindBook(nCanonicalBookNumber);
			if (cvBook != null && fMakeBackup)
			{
				// Replace any existing book with the imported one.
				var oldBook = BackupVersion.FindBook(nCanonicalBookNumber);
				if (oldBook != null)
				{
					BackupVersion.BooksOS.Remove(oldBook);
				}
				BackupVersion.AddBookCopy(cvBook);
			}

			return cvBook;
		}

		/// <summary>
		/// Creator of this object MUST call this after import is done, whether or not it
		/// succeeded.
		/// </summary>
		/// <param name="fRollbackLastSequence"><c>false</c> if import completed normally or was
		/// stopped by user after importing one or more complete books. <c>true</c> if an error
		/// occurred during import or user cancelled import in the middle of a book.</param>
		public void DoneImportingFiles(bool fRollbackLastSequence)
		{
			if (Cache.ActionHandlerAccessor.CurrentDepth == 0)
			{
				Logger.WriteEvent("DoneImportingFiles called when no UOW is in progress");
				Debug.Fail("DoneImportingFiles called when no UOW is in progress");
				return;
			}
			if (fRollbackLastSequence)
			{
				Cache.ActionHandlerAccessor.Rollback(0);
				ImportedBooks.Remove(m_lastBookAddedToImportedBooks);
			}
			else
			{
				Cache.ActionHandlerAccessor.EndUndoTask();
			}

			Cache.ServiceLocator.WritingSystemManager.Save();
		}

		/// <summary>
		/// Removes the saved version for backups of any overwritten books if it is empty.
		/// </summary>
		public void RemoveEmptyBackupSavedVersion()
		{
			if (BackupVersion == null || !BackupVersion.IsValidObject || BackupVersion.BooksOS.Count != 0)
			{
				return;
			}
			using (var uow = new UndoableUnitOfWorkHelper(Cache.ActionHandlerAccessor, "Remove saved version"))
			{
				m_scr.ArchivedDraftsOC.Remove(BackupVersion);
				uow.RollBack = false;
			}
			BackupVersion = null;
		}

		/// <summary>
		/// Remove the version we made specially for importing,
		/// if doing an import just for updating FLEx texts from Paratext (LT-15945).
		///
		/// This is because Paratext import generates a new draft each time, and this can
		/// produce lots of duplication when several people are doing it using S/R.
		/// </summary>
		public void RemoveImportedVersion()
		{
			if (ImportedVersion != null && ImportedVersion.IsValidObject)
			{
				// No need to localize, this should not be seen by the user.
				UndoableUnitOfWorkHelper.Do("Remove temp version", "Restore temp version", Cache.ActionHandlerAccessor, () => m_scr.ArchivedDraftsOC.Remove(ImportedVersion));
				ImportedVersion = null;
			}
		}

		/// <summary>
		/// When really, truly done with all merging etc, collapse all the things we can Undo
		/// for the import into a single Undo Item.
		/// </summary>
		/// <returns>True if some actions were collapsed, false otherwise</returns>
		public bool CollapseAllUndoActions()
		{
			var import = Properties.Resources.kstidImport;
			var undo = string.Format(ResourceHelper.GetResourceString("kstidUndoFrame"), import);
			var redo = string.Format(ResourceHelper.GetResourceString("kstidRedoFrame"), import);
			return Cache.DomainDataByFlid.GetActionHandler().CollapseToMark(m_hMark, undo, redo);
		}

		/// <summary>
		/// Undoes the entire import. Presumably there was either nothing in the file, or the
		/// user canceled during the first book.
		/// </summary>
		public void UndoEntireImport()
		{
			if (CollapseAllUndoActions())
			{
				Cache.ActionHandlerAccessor.Undo();
			}
			ImportedBooks.Clear();
			ImportedVersion = null;
			BackupVersion = null;
		}

		/// <summary>
		/// Inserts a Scriture annotation for the book currently being imported.
		/// </summary>
		/// <param name="bcvStartReference">The starting BCV reference.</param>
		/// <param name="bcvEndReference">The ending BCV reference.</param>
		/// <param name="obj">The object being annotated (either a paragraph or a ScrBook)</param>
		/// <param name="bldr">The paragraph builder containing the guts of the annotation
		/// description</param>
		/// <param name="guidNoteType">The GUID representing the CmAnnotationDefn to use for
		/// the type</param>
		/// <returns>The newly created annotation</returns>
		public IScrScriptureNote InsertNote(int bcvStartReference, int bcvEndReference, ICmObject obj, StTxtParaBldr bldr, Guid guidNoteType)
		{
			return m_annotations.InsertImportedNote(bcvStartReference, bcvEndReference, obj, obj, guidNoteType, bldr);
		}
		#endregion

		#region Non-private properties
		/// <summary>
		/// Gets the cache.
		/// </summary>
		protected LcmCache Cache { get; }

		/// <summary>
		/// Gets the canonical numbers of the books that were imported.
		/// </summary>
		public Dictionary<int, bool> ImportedBooks { get; }

		/// <summary>
		/// Gets the version we use to back up originals of merged or overwritten books,
		/// creating a new one if necessary.
		/// </summary>
		public IScrDraft BackupVersion { get; private set; }

		/// <summary>
		/// The saved version into which we are putting new books imported.
		/// </summary>
		public IScrDraft ImportedVersion { get; private set; }

		#endregion

		#region Private methods
		/// <summary>
		/// Gets or creates an imported ScrDraft with the specified description.
		/// </summary>
		/// <param name="description">The description of the draft to get.</param>
		private IScrDraft GetOrCreateVersion(string description)
		{
			return Cache.ServiceLocator.GetInstance<IScrDraftRepository>().GetDraft(description, ScrDraftType.ImportedVersion) ??
			            Cache.ServiceLocator.GetInstance<IScrDraftFactory>().Create(description, ScrDraftType.ImportedVersion);
		}

		/// <summary>
		/// Sets the current book, particularly picking the right set of annotations
		/// to add new ones to. Also (and more conspicuously) ends the current Undo task
		/// and makes a new one for importing the new book. Should therefore be called
		/// BEFORE setting up the Undo action for the creation of the book.
		/// </summary>
		/// <param name="nCanonicalBookNumber">The canonical book number.</param>
		/// <param name="fVernacular">if set to <c>true</c> currently importing the vernacular.
		/// </param>
		/// <returns>The existing book in the current imported version, if any; otherwise
		/// <c>null</c></returns>
		/// <remarks>If importing annotations and/or BTs without importing the vernacular, the
		/// importer is responsible for calling this directly.</remarks>
		private IScrBook SetCurrentBook(int nCanonicalBookNumber, bool fVernacular)
		{
			if (nCanonicalBookNumber <= 0 || nCanonicalBookNumber > BCVRef.LastBook)
			{
				throw new ArgumentOutOfRangeException(nameof(nCanonicalBookNumber), nCanonicalBookNumber, @"Expected a canonical book number.");
			}

			var actionHandler = Cache.DomainDataByFlid.GetActionHandler();

			// We want a new undo task for each new book, except the first one
			if (ImportedBooks.Count > 0)
			{
				actionHandler.EndUndoTask();
			}

			if (actionHandler.CurrentDepth == 0)
			{
				// No need to use localizable string from resources because the user will never
				// see these labels because we collapse to a single undo task when the import
				// completes.
				actionHandler.BeginUndoTask("Undo Import Book " + nCanonicalBookNumber, "Redo Import Book " + nCanonicalBookNumber);
			}
			if (ImportedBooks.ContainsKey(nCanonicalBookNumber))
			{
				m_lastBookAddedToImportedBooks = 0;
			}
			else
			{
				m_lastBookAddedToImportedBooks = nCanonicalBookNumber;
				ImportedBooks[nCanonicalBookNumber] = fVernacular;
			}

			m_annotations = m_scr.BookAnnotationsOS[nCanonicalBookNumber - 1];

			return ImportedVersion?.FindBook(nCanonicalBookNumber);
		}
		#endregion
	}
}
