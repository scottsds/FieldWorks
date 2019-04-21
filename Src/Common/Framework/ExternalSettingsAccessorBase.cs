// Copyright (c) 2008-2013 SIL International
// This software is licensed under the LGPL, version 2.1 or later
// (http://www.gnu.org/licenses/lgpl-2.1.html)
//
// File: ExternalSettingsAccessorBase.cs
// Responsibility: FieldWorks Team

using System;
using System.Linq;
#if DEBUG
using System.Diagnostics;
#endif

using SIL.FieldWorks.Common.FwUtils;
using SIL.LCModel;
using SIL.Reporting;
using SIL.LCModel.Utils;
#if !DEBUG
using SIL.FieldWorks.Resources;
#endif

namespace SIL.FieldWorks.Common.Framework
{
	/// ----------------------------------------------------------------------------------------
	/// <summary>
	/// Base class for FieldWorks classes that process external "resource" files to load
	/// factory settings into databases.
	/// </summary>
	/// <typeparam name="T">The type of document returned by LoadDoc and accessed by GetVersion
	/// (e.g. XmlNode or class representing the contents of an XML file</typeparam>
	/// ----------------------------------------------------------------------------------------
	public abstract class ExternalSettingsAccessorBase<T>
	{
#if DEBUG
		private bool m_fVersionUpdated;
#endif

		#region Abstract properties
		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// The path where the settings file is found.
		/// For example, @"\Language Explorer\TeStyles.xml"
		/// </summary>
		/// ------------------------------------------------------------------------------------
		protected abstract string ResourceFilePathFromFwInstall { get; }

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// The name (no path, no extension) of the settings file.
		/// For example, "Testyles"
		/// </summary>
		/// ------------------------------------------------------------------------------------
		protected abstract string ResourceName { get; }

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Gets the LcmCache
		/// </summary>
		/// ------------------------------------------------------------------------------------
		protected abstract LcmCache Cache { get; }

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Gets the resource list in which the CmResources are owned.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		protected abstract ILcmOwningCollection<ICmResource> ResourceList { get; }

		#endregion

		#region Protected properties
		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Gets the name (no path) of the settings file. This is the resource name with the
		/// correct file extension appended.
		/// For example, "TeStyles.xml"
		/// If the external resource is not an XML file, override this to append an extension
		/// other than ".xml".
		/// </summary>
		/// ------------------------------------------------------------------------------------
		virtual protected string ResourceFileName
		{
			get { return ResourceName + ".xml"; }
		}
		#endregion

		#region Abstract and Virtual methods
		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Gets a GUID based on the version attribute node.
		/// </summary>
		/// <param name="document">The document.</param>
		/// <returns>A GUID based on the version attribute node</returns>
		/// ------------------------------------------------------------------------------------
		protected abstract Guid GetVersion(T document);

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Process the resources (e.g., create styles or add publication info).
		/// </summary>
		/// <param name="dlg">The progress dialog manager.</param>
		/// <param name="doc">The loaded document that has the settings.</param>
		/// ------------------------------------------------------------------------------------
		protected abstract void ProcessResources(IThreadedProgress dlg, T doc);
		#endregion

		#region Static methods
		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Throws an exception. Release mode overrides the message.
		/// </summary>
		/// <param name="message">The message to display (in debug mode)</param>
		/// ------------------------------------------------------------------------------------
		static protected void ReportInvalidInstallation(string message)
		{
			ReportInvalidInstallation(message, null);
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Throws an exception. Release mode overrides the message.
		/// </summary>
		/// <param name="message">The message to display (in debug mode)</param>
		/// <param name="e">Optional inner exception</param>
		/// ------------------------------------------------------------------------------------
		static protected void ReportInvalidInstallation(string message, Exception e)
		{
			Logger.WriteEvent(message); // This is so we get the actual error in release builds
#if !DEBUG
			message = ResourceHelper.GetResourceString("kstidInvalidInstallation");
#endif
			throw new InstallationException(message, e);
		}
		#endregion

		#region Protected methods
		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Loads the settings file.
		/// </summary>
		/// <returns>The loaded document</returns>
		/// ------------------------------------------------------------------------------------
		protected abstract T LoadDoc(string xmlDocument = null);

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Make sure the stylesheet for the specified object is current.
		/// </summary>
		/// <param name="progressDlg">The progress dialog if one is already up.</param>
		/// ------------------------------------------------------------------------------------
		public void EnsureCurrentResource(IThreadedProgress progressDlg)
		{
			var doc = LoadDoc();
			Guid newVersion;
			try
			{
				newVersion = GetVersion(doc);
			}
			catch (Exception e)
			{
				ReportInvalidInstallation(string.Format(
					FrameworkStrings.ksInvalidResourceFileVersion, ResourceFileName), e);
				newVersion = Guid.Empty;
			}

			// Re-load the factory settings if they are not at current version.
			if (IsResourceOutdated(ResourceName, newVersion))
			{
				ProcessResources(progressDlg, doc);
#if DEBUG
				Debug.Assert(m_fVersionUpdated);
#endif
			}
			else
				EnsureCurrentLocalizations(progressDlg);
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Subclasses can override this method to add or update localizations of the resource
		/// if needed. This method is only called if the main resource is already up-to-date
		/// (i.e., IsResourceOutdated returns <c>false</c> for ResourceName.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		protected virtual void EnsureCurrentLocalizations(IThreadedProgress progressDlg)
		{
			// Default is a no-op
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Gets the requested resource.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		private ICmResource GetResource(string resourceName)
		{
			return (from res in ResourceList.ToArray()
				where res.Name.Equals(resourceName)
				select res).FirstOrDefault();
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Determines whether the specified resource is out-of-date (or not created).
		/// </summary>
		/// <param name="resourceName">Name of the resource.</param>
		/// <param name="newVersion">The latest version (i.e., the version from the resource
		/// file).</param>
		/// ------------------------------------------------------------------------------------
		protected bool IsResourceOutdated(string resourceName, Guid newVersion)
		{
			// Get the current version of the settings used in this project.
			ICmResource resource = GetResource(resourceName);
			return (resource == null || newVersion != resource.Version);
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Sets the new resource version in the DB.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		protected void SetNewResourceVersion(Guid newVersion)
		{
			SetNewResourceVersion(ResourceName, newVersion);
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Sets the new version in the DB for the named resource.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		protected void SetNewResourceVersion(string name, Guid newVersion)
		{
			ICmResource resource = GetResource(name);
			if (resource == null)
			{
				// Resource does not exist yet. Add it to the collection.
				ICmResource newResource = Cache.ServiceLocator.GetInstance<ICmResourceFactory>().Create();
				ResourceList.Add(newResource);
				newResource.Name = name;
				newResource.Version = newVersion;
#if DEBUG
				m_fVersionUpdated = true;
#endif
				return;
			}

			resource.Version = newVersion;
#if DEBUG
			m_fVersionUpdated = true;
#endif
		}
		#endregion
	}
}
