﻿// Copyright (c) 2015-2017 SIL International
// This software is licensed under the LGPL, version 2.1 or later
// (http://www.gnu.org/licenses/lgpl-2.1.html)

using System;
using System.IO;
using System.Reflection;
using Microsoft.Win32;
using SIL.LCModel;
using SIL.LCModel.Utils;

namespace SIL.FieldWorks.ParatextLexiconPlugin
{
	internal static class ParatextLexiconPluginDirectoryFinder
	{
		private const string ProjectsDir = "ProjectsDir";
		private const string RootDataDir = "RootDataDir";
		private const string RootCodeDir = "RootCodeDir";
		private const string Projects = "Projects";
		private const string Templates = "Templates";
		private const string SilDir = "SIL";
		private const string FieldWorksDir = "FieldWorks";

		public static string ProjectsDirectory
		{
			get { return GetDirectory(ProjectsDir, Path.Combine(DataDirectory, Projects)); }
		}

		public static string ProjectsDirectoryLocalMachine
		{
			get { return GetDirectoryLocalMachine(ProjectsDir, Path.Combine(DataDirectoryLocalMachine, Projects)); }
		}

		public static string TemplateDirectory
		{
			get { return Path.Combine(CodeDirectory, Templates); }
		}

		public static ILcmDirectories LcmDirectories { get; } = new ParatextLexiconPluginLcmDirectories();

		public static string DataDirectory
		{
			get { return GetDirectory(RootDataDir, Path.Combine(LcmFileHelper.CommonApplicationData, SilDir, FieldWorksDir)); }
		}

		public static string DataDirectoryLocalMachine
		{
			get { return GetDirectoryLocalMachine(RootDataDir, Path.Combine(LcmFileHelper.CommonApplicationData, SilDir, FieldWorksDir)); }
		}

		public static string CodeDirectory
		{
			get
			{
				return GetDirectory(RootCodeDir, MiscUtils.IsUnix ? "/usr/share/fieldworks"
					: Path.GetDirectoryName(FileUtils.StripFilePrefix(Assembly.GetExecutingAssembly().CodeBase)));
			}
		}

		private static string GetDirectory(string registryValue, string defaultDir)
		{
			using (RegistryKey userKey = ParatextLexiconPluginRegistryHelper.FieldWorksRegistryKey)
			using (RegistryKey machineKey = ParatextLexiconPluginRegistryHelper.FieldWorksRegistryKeyLocalMachine)
			{
				var registryKey = userKey;
				if (userKey == null || userKey.GetValue(registryValue) == null)
				{
					registryKey = machineKey;
				}

				return GetDirectory(registryKey, registryValue, defaultDir);
			}
		}

		private static string GetDirectory(RegistryKey registryKey, string registryValue, string defaultDir)
		{
			string rootDir = (registryKey == null) ? null : registryKey.GetValue(registryValue, null) as string;

			if (string.IsNullOrEmpty(rootDir) && !string.IsNullOrEmpty(defaultDir))
				rootDir = defaultDir;
			if (string.IsNullOrEmpty(rootDir))
			{
				throw new ApplicationException();
			}
			// Hundreds of callers of this method are using Path.Combine with the results.
			// Combine only works with a root directory if it is followed by \ (e.g., c:\)
			// so we don't want to trim the \ in this situation.
			string dir = rootDir.TrimEnd(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar);
			return dir.Length > 2 ? dir : dir + Path.DirectorySeparatorChar;
		}

		private static string GetDirectoryLocalMachine(string registryValue, string defaultDir)
		{
			using (RegistryKey machineKey = ParatextLexiconPluginRegistryHelper.FieldWorksRegistryKeyLocalMachine)
			{
				return GetDirectory(machineKey, registryValue, defaultDir);
			}
		}

		private class ParatextLexiconPluginLcmDirectories : ILcmDirectories
		{
			string ILcmDirectories.ProjectsDirectory
			{
				get { return ProjectsDirectory; }
			}

			string ILcmDirectories.TemplateDirectory
			{
				get { return TemplateDirectory; }
			}
		}
	}
}
