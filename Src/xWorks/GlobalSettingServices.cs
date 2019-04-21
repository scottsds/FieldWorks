// Copyright (c) 2015 SIL International
// This software is licensed under the LGPL, version 2.1 or later
// (http://www.gnu.org/licenses/lgpl-2.1.html)

using SIL.LCModel;
using SIL.LCModel.DomainImpl;
using XCore;

namespace SIL.FieldWorks.XWorks
{
	/// <summary>
	/// Global setting services provides a simple API for persisting and restoring global settings that affect all windows.
	/// The intended use is that each window, when it saves its settings, passes its property table to SaveSettings
	/// so that any global settings can be recorded. The first window to open (on a particular database) should call RestoreSettings.
	/// </summary>
	public static class GlobalSettingServices
	{
		private const string khomographconfiguration = "HomographConfiguration";

		/// <summary>
		/// Save any appropriate settings to the property table
		/// </summary>
		public static void SaveSettings(ILcmServiceLocator services, PropertyTable propertyTable)
		{
			var hc = services.GetInstance<HomographConfiguration>();
			propertyTable.SetProperty(khomographconfiguration, hc.PersistData, true);
		}

		/// <summary>
		/// Restore any appropriate settings which have values in the property table
		/// </summary>
		public static void RestoreSettings(ILcmServiceLocator services, PropertyTable propertyTable)
		{
			var hcSettings = propertyTable.GetStringProperty(khomographconfiguration, null);
			if (hcSettings != null)
			{
				var hc = services.GetInstance<HomographConfiguration>();
				hc.PersistData = hcSettings;
			}
		}
	}
}
