﻿// Copyright (c) 2015 SIL International
// This software is licensed under the LGPL, version 2.1 or later
// (http://www.gnu.org/licenses/lgpl-2.1.html)

using SIL.LCModel.Core.KernelInterfaces;
using SIL.LCModel;
using XCore;

namespace SIL.FieldWorks.Common.Widgets
{
	/// <summary>
	/// Encapsulates the accessors that controls need for setting up text boxes
	/// with stylesheets, so that such controls can use WritingSystemAndStylesheetHelper
	/// to setup this information.
	/// </summary>
	public interface IWritingSystemAndStylesheet
	{
		/// <summary></summary>
		System.Drawing.Font Font { get; set; }
		/// <summary></summary>
		IVwStylesheet StyleSheet { get; set; }
		/// <summary></summary>
		int WritingSystemCode { get; set; }
		/// <summary></summary>
		ILgWritingSystemFactory WritingSystemFactory { get; set; }
	}

	/// <summary>
	/// helps controls setup WritingSystem[Factory] and Stylesheet related functionality
	/// </summary>
	public static class WritingSystemAndStylesheetHelper
	{
		/// <summary>
		/// setup the given control's WritingSystem[Factory] and Stylesheet related functionality
		/// </summary>
		/// <param name="propertyTable"></param>
		/// <param name="control"></param>
		/// <param name="cache"></param>
		/// <param name="wsDefault">used to set WritingSytemCode and Font for the control</param>
		public static void SetupWritingSystemAndStylesheetInfo(PropertyTable propertyTable, IWritingSystemAndStylesheet control,
			LcmCache cache, int wsDefault)
		{
			control.WritingSystemFactory = cache.WritingSystemFactory;
			control.WritingSystemCode = wsDefault;
			control.Font = new System.Drawing.Font(cache.ServiceLocator.WritingSystemManager.Get(wsDefault).DefaultFontName, 10);
			control.StyleSheet = FontHeightAdjuster.StyleSheetFromPropertyTable(propertyTable);
		}
	}

}
