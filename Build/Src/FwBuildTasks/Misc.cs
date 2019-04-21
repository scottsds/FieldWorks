// Copyright (c) 2015 SIL International
// This software is licensed under the LGPL, version 2.1 or later
// (http://www.gnu.org/licenses/lgpl-2.1.html)

using System;
using System.IO;
using System.Security.Principal;
using Microsoft.Build.Framework;
using Microsoft.Build.Utilities;

namespace FwBuildTasks
{
	/// <summary>
	/// Return the parent directory of the required input attribute.
	/// </summary>
	public class ParentDirectory : Task
	{
		public override bool Execute()
		{
			Value = Path.GetDirectoryName(CurrentDirectory);
			return true;
		}

		[Required]
		public string CurrentDirectory { get; set; }

		[Output]
		public string Value { get; set; }
	}

	/// <summary>
	/// Return the CPU architecture of the current system.  (This is really only used on
	/// linux at the moment.)
	/// </summary>
	public class CpuArchitecture : Task
	{
		public override bool Execute()
		{
			if (Environment.OSVersion.Platform == System.PlatformID.Unix)
			{
				System.Diagnostics.Process proc = new System.Diagnostics.Process();
				proc.StartInfo.UseShellExecute = false;
				proc.StartInfo.RedirectStandardOutput = true;
				proc.StartInfo.FileName = "/usr/bin/arch";
				proc.Start();
				Value = proc.StandardOutput.ReadToEnd().TrimEnd();
				proc.WaitForExit();
			}
			else
			{
				var arch = Environment.GetEnvironmentVariable("arch");
				Value = !string.IsNullOrEmpty(arch) && (arch.ToLower() == "x64" || arch.ToLower() == "win64") ? "x64" : "x86";
			}
			return true;
		}

		[Output]
		public string Value { get; set; }
	}

	/// <summary>
	/// Mono doesn't implement CombinePaths, so here's a simplified replacement.
	/// </summary>
	public class PathCombine : Task
	{
		public override bool Execute()
		{
			Value = Path.Combine(BasePath, SubPath);
			return true;
		}

		[Required]
		public string BasePath { get; set; }

		[Required]
		public string SubPath { get; set; }

		[Output]
		public string Value { get; set; }
	}

	/// <summary>
	/// Return the name of the current computer.
	/// </summary>
	public class ComputerName : Task
	{
		public override bool Execute()
		{
			Value = Environment.MachineName;
			if (string.IsNullOrEmpty(Value))
				Value = ".";
			return true;
		}

		[Output]
		public string Value { get; set; }
	}

	/// <summary>
	/// Set an environment variable for the current process.
	/// </summary>
	public class SetEnvVar : Task
	{
		[Required]
		public string Variable { get; set; }

		/// <summary>
		/// Value might be empty, so it can't be [Required].
		/// </summary>
		public string Value { get; set; }

		public override bool Execute()
		{
			Environment.SetEnvironmentVariable(Variable, Value);
			if (Value == null)
				Log.LogMessage(MessageImportance.Low, "SetEnvVar: '{0}' set to null", Variable);
			else
				Log.LogMessage(MessageImportance.Low, "SetEnvVar: '{0}' set to '{1}'", Variable, Value);
			return true;
		}
	}

	/// <summary>
	/// Check whether the user has administrative privilege.
	/// </summary>
	public class CheckAdminPrivilege : Task
	{
		[Output]
		public bool UserIsAdmin { get; set; }

		public override bool Execute()
		{
			WindowsIdentity id = WindowsIdentity.GetCurrent();
			WindowsPrincipal p = new WindowsPrincipal(id);
			UserIsAdmin = p.IsInRole(WindowsBuiltInRole.Administrator);
			return true;
		}
	}

	/// <summary>
	/// Gets the path to a special folder.
	/// </summary>
	/// <remarks>In theory we could use $([System.Environment]::GetFolderPath(SpecialFolder.LocalApplicationData))
	/// instead. However, property functions aren't implemented in xbuild yet.</remarks>
	public class GetSpecialFolderPath: Task
	{
		[Required]
		public string Folder { get; set; }

		[Output]
		public string Path { get; set; }

		public override bool Execute()
		{
			var folder = (Environment.SpecialFolder)Enum.Parse(typeof(Environment.SpecialFolder), Folder);
			Path = Environment.GetFolderPath(folder);
			return true;
		}
	}
}
