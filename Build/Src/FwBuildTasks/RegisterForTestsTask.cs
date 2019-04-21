// ---------------------------------------------------------------------------------------------
#region // Copyright (c) 2012, SIL International. All Rights Reserved.
// <copyright from='2012' to='2012' company='SIL International'>
//		Copyright (c) 2012, SIL International. All Rights Reserved.
//
//		Distributable under the terms of either the Common Public License or the
//		GNU Lesser General Public License, as specified in the LICENSING.txt file.
// </copyright>
#endregion
// ---------------------------------------------------------------------------------------------
using System.IO;
using Microsoft.Build.Framework;
using Microsoft.Build.Utilities;

namespace SIL.FieldWorks.Build.Tasks
{
	/// ----------------------------------------------------------------------------------------
	/// <summary>
	/// Register the unmanaged dll
	/// </summary>
	/// ----------------------------------------------------------------------------------------
	public class RegisterForTests: Task
	{
		public RegisterForTests()
		{
			RegisterTypeLibrary = true;
		}

		/// <summary>
		/// Gets or sets the name and path of the DLL that should be registered
		/// </summary>
		[Required]
		public string Dll { get; set; }

		/// <summary>
		/// Whether to process the type library in addition to the objects and interfaces.
		/// Defaults to <c>true</c>.
		/// </summary>
		public bool RegisterTypeLibrary { get; set; }

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Executes the task.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		public override bool Execute()
		{
			Log.LogMessage(MessageImportance.Normal, "Registering {0}", Path.GetFileName(Dll));

			if (File.Exists(Dll))
			{
				using (var regHelper = new RegHelper(Log, "win32"))
				{
					return regHelper.Register(Dll, false, RegisterTypeLibrary);
				}
			}

			return true;
		}

	}
}
