﻿// Copyright (c) 2012-2018 SIL International
// This software is licensed under the LGPL, version 2.1 or later
// (http://www.gnu.org/licenses/lgpl-2.1.html)

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Xml;
using System.Xml.Linq;
using Microsoft.Build.Utilities;

namespace FwBuildTasks
{
	public class GenerateFwTargets : Task
	{
		public override bool Execute()
		{
			try
			{
				var gen = new CollectTargets(Log);
				gen.Generate();
				return true;
			}
			catch (CollectTargets.StopTaskException)
			{
				return false;
			}
		}
	}

	/// <summary>
	/// Collect projects from the FieldWorks repository tree, and generate a targets file
	/// for MSBuild (Mono xbuild).
	/// </summary>
	public class CollectTargets
	{
		public class StopTaskException : Exception
		{
			public StopTaskException(Exception innerException) : base(null, innerException)
			{
			}
		}

		private readonly string m_fwroot;
		private readonly Dictionary<string, string> m_mapProjFile = new Dictionary<string, string>();
		private readonly Dictionary<string, List<string>> m_mapProjDepends = new Dictionary<string, List<string>>();
		private TaskLoggingHelper Log { get; }
		private XmlDocument m_csprojFile;
		private XmlNamespaceManager m_namespaceMgr;
		private Dictionary<string, int> m_timeoutMap;

		public CollectTargets(TaskLoggingHelper log)
		{
			Log = log;
			// Get the parent directory of the running program.  We assume that
			// this is the root of the FieldWorks repository tree.
			var fwrt = BuildUtils.GetAssemblyFolder();
			while (!Directory.Exists(Path.Combine(fwrt, "Build")) || !Directory.Exists(Path.Combine(fwrt, "Src")))
			{
				fwrt = Path.GetDirectoryName(fwrt);
				if (fwrt == null)
				{
					Log.LogError("Error pulling the working folder from the running assembly.");
					break;
				}
			}
			m_fwroot = fwrt;
		}

		/// <summary>
		/// Scan all the known csproj files under FWROOT for references, and then
		/// create msbuild target files accordingly.
		/// </summary>
		public void Generate()
		{
			var infoSrc = new DirectoryInfo(Path.Combine(m_fwroot, "Src"));
			CollectInfo(infoSrc);
			// These projects from Lib had nant targets.  They really should be under Src.
			var infoEth = new DirectoryInfo(Path.Combine(m_fwroot, "Lib/src/Ethnologue"));
			CollectInfo(infoEth);
			var infoScr2 = new DirectoryInfo(Path.Combine(m_fwroot, "Lib/src/ScrChecks"));
			CollectInfo(infoScr2);
			var infoObj = new DirectoryInfo(Path.Combine(m_fwroot, "Lib/src/ObjectBrowser"));
			CollectInfo(infoObj);
			WriteTargetFiles();
		}

		/// <summary>
		/// Recursively scan the directory for csproj files.
		/// </summary>
		private void CollectInfo(DirectoryInfo dirInfo)
		{
			if (dirInfo == null || !dirInfo.Exists)
				return;
			foreach (var fi in dirInfo.GetFiles())
			{
				if (fi.Name.EndsWith(".csproj") && fi.Exists)
					ProcessCsProjFile(fi.FullName);
			}
			foreach (var diSub in dirInfo.GetDirectories())
				CollectInfo(diSub);
		}

		/// <summary>
		/// Extract the reference information from a csproj file.
		/// </summary>
		private void ProcessCsProjFile(string filename)
		{
			if (filename.Contains("Src/LexText/Extensions/") || filename.Contains("Src\\LexText\\Extensions\\"))
				return; // Skip the extensions -- they're either obsolete or nonstandard.
			var project = Path.GetFileNameWithoutExtension(filename);
			if (project == "ICSharpCode.SharpZLib" ||
				project == "VwGraphicsReplayer" ||
				project == "SfmStats" ||
				project == "ConvertSFM")
			{
				return; // Skip these apps - they are are sample or support apps
			}
			if (m_mapProjFile.ContainsKey(project) || m_mapProjDepends.ContainsKey(project))
			{
				Log.LogWarning("Project '{0}' has already been found elsewhere!", project);
				return;
			}
			m_mapProjFile.Add(project, filename);
			List<string> dependencies = new List<string>();
			using (var reader = new StreamReader(filename))
			{
				int lineNumber = 0;
				while (!reader.EndOfStream)
				{
					lineNumber++;
					var line = reader.ReadLine().Trim();
					try
					{
						if (line.Contains("<Reference Include="))
						{
							// line is similar to
							// <Reference Include="BasicUtils, Version=4.1.1.0, Culture=neutral, processorArchitecture=MSIL">
							var tmp = line.Substring(line.IndexOf('"') + 1);
							// NOTE: we assume that the name of the assembly is the same as the name of the project
							var projectName = tmp.Substring(0, tmp.IndexOf('"'));
							var i0 = projectName.IndexOf(',');
							if (i0 >= 0)
								projectName = projectName.Substring(0, i0);
							//Console.WriteLine("{0} [R]: ref0 = '{1}'; ref1 = '{2}'", filename, ref0, ref1);
							dependencies.Add(projectName);
						}
						else if (line.Contains("<ProjectReference Include="))
						{
							// line is similar to
							// <ProjectReference Include="..\HermitCrab\HermitCrab.csproj">
							var tmp = line.Substring(line.IndexOf('"') + 1);
							// NOTE: we assume that the name of the assembly is the same as the name of the project
							var projectName = tmp.Substring(0, tmp.IndexOf('"'));
							// Unfortunately we can't use File.GetFileNameWithoutExtension(projectName)
							// here: we use the same .csproj file on both Windows and Linux
							// and so it contains backslashes in the name which is a valid
							// character on Linux.
							var i0 = projectName.LastIndexOfAny(new[] {'\\', '/'});
							if (i0 >= 0)
								projectName = projectName.Substring(i0 + 1);
							projectName = projectName.Replace(".csproj", "");
							//Console.WriteLine("{0} [PR]: ref0 = '{1}'; ref1 = '{2}'", filename, ref0, ref1);
							dependencies.Add(projectName);
						}
					}
					catch (ArgumentOutOfRangeException e)
					{
						Log.LogError("GenerateFwTargets", null, null,
							filename, lineNumber, 0, 0, 0,
							"Error reading project references. Invalid XML file?");
						throw new StopTaskException(e);
					}
				}
				reader.Close();
			}
			m_mapProjDepends.Add(project, dependencies);
		}

		private void LoadProjectFile(string projectFile)
		{
			try
			{
				m_csprojFile = new XmlDocument();
				m_csprojFile.Load(projectFile);
				m_namespaceMgr = new XmlNamespaceManager(m_csprojFile.NameTable);
				m_namespaceMgr.AddNamespace("c", "http://schemas.microsoft.com/developer/msbuild/2003");
			}
			catch (XmlException e)
			{
				Log.LogError("GenerateFwTargets", null, null,
					projectFile, 0, 0, 0, 0, "Error reading project references. Invalid XML file?");

				throw new StopTaskException(e);
			}
		}

		/// <summary>
		/// Gets the name of the assembly as defined in the .csproj file.
		/// </summary>
		/// <returns>The assembly name with extension.</returns>
		private string AssemblyName
		{
			get
			{
				var name = m_csprojFile.SelectSingleNode("/c:Project/c:PropertyGroup/c:AssemblyName",
					m_namespaceMgr);
				var type = m_csprojFile.SelectSingleNode("/c:Project/c:PropertyGroup/c:OutputType",
					m_namespaceMgr);
				string extension = ".dll";
				if (type.InnerText == "WinExe" || type.InnerText == "Exe")
					extension = ".exe";
				return name.InnerText + extension;
			}
		}

		/// <summary>
		/// Gets property groups for the different configurations from the project file
		/// </summary>
		private XmlNodeList ConfigNodes
		{
			get
			{
				return m_csprojFile.SelectNodes("/c:Project/c:PropertyGroup[c:DefineConstants]",
					m_namespaceMgr);
			}
		}

		private string GetProjectSubDir(string project)
		{
			var projectSubDir = Path.GetDirectoryName(m_mapProjFile[project]);
			projectSubDir = projectSubDir.Substring(m_fwroot.Length);
			projectSubDir = projectSubDir.Replace("\\", "/");
			if (projectSubDir.StartsWith("/Src/"))
				projectSubDir = projectSubDir.Substring(5);
			else if (projectSubDir.StartsWith("/Lib/src/"))
				projectSubDir = projectSubDir.Substring(9);
			else if (projectSubDir.StartsWith("/"))
				projectSubDir = projectSubDir.Substring(1);
			if (Path.DirectorySeparatorChar != '/')
				projectSubDir = projectSubDir.Replace('/', Path.DirectorySeparatorChar);
			return projectSubDir;
		}

		private static bool IsMono
		{
			get
			{
				return Type.GetType("Mono.Runtime") != null;
			}
		}

		[DllImport("__Internal", EntryPoint = "mono_get_runtime_build_info")]
		private static extern string GetMonoVersion();

		/// <summary>
		/// Gets the version of the currently running Mono (e.g.
		/// "5.0.1.1 (2017-02/5077205 Thu May 25 09:16:53 UTC 2017)"), or the empty string
		/// on Windows.
		/// </summary>
		private static string MonoVersion => IsMono ? GetMonoVersion() : string.Empty;

		/// <summary>
		/// Used the collected information to write the needed target files.
		/// </summary>
		private void WriteTargetFiles()
		{
			var targetsFile = Path.Combine(m_fwroot, "Build/FieldWorks.targets");
			try
			{
				// Write all the C# targets and their dependencies.
				using (var writer = new StreamWriter(targetsFile))
				{
					writer.WriteLine("<?xml version=\"1.0\" encoding=\"utf-8\"?>");
					writer.WriteLine("<!-- This file is automatically generated by the Setup target.  DO NOT EDIT! -->");
					writer.WriteLine("<!-- Unfortunately, the new one is generated after the old one has been read. -->");
					writer.WriteLine("<!-- 'msbuild /t:refreshTargets' generates this file and does nothing else. -->");
					var toolsVersion = !IsMono || int.Parse(MonoVersion.Substring(0, 1)) >= 5 ? "15.0" : "14.0";
					writer.WriteLine("<Project xmlns=\"http://schemas.microsoft.com/developer/msbuild/2003\" ToolsVersion=\"{0}\">", toolsVersion);
					writer.WriteLine();
					foreach (var project in m_mapProjFile.Keys)
					{
						LoadProjectFile(m_mapProjFile[project]);

						var isTestProject = project.EndsWith("Tests") || project == "TestManager" || project == "ProjectUnpacker";

						// <Choose> to define DefineConstants
						writer.WriteLine("\t<Choose>");
						var otherwiseBldr = new StringBuilder();
						var otherwiseAdded = false;
						var configs = new Dictionary<string, string>();
						foreach (XmlNode node in ConfigNodes)
						{
							var condition = node.Attributes["Condition"].InnerText;
							var tmp = condition.Substring(condition.IndexOf("==") + 2).Trim().Trim('\'');
							var configuration = tmp.Substring(0, tmp.IndexOf("|"));

							// Add configuration only once even if same configuration is contained
							// for multiple platforms, e.g. for AnyCpu and x64.
							if (configs.ContainsKey(configuration))
							{
								if (configs[configuration] != node.SelectSingleNode("c:DefineConstants", m_namespaceMgr).InnerText.Replace(";", " "))
								{
									Log.LogError("Configuration {0} for project {1} is defined several times " +
										"but contains differing values for DefineConstants.", configuration, project);
								}
								continue;
							}
							configs.Add(configuration, node.SelectSingleNode("c:DefineConstants", m_namespaceMgr).InnerText.Replace(";", " "));

							writer.WriteLine("\t\t<When Condition=\" '$(config-capital)' == '{0}' \">", configuration);
							writer.WriteLine("\t\t\t<PropertyGroup>");
							writer.WriteLine("\t\t\t\t<{0}Defines>{1} CODE_ANALYSIS</{0}Defines>",
								project, configs[configuration]);
							writer.WriteLine("\t\t\t</PropertyGroup>");
							writer.WriteLine("\t\t</When>");
							if (condition.Contains("Debug") && !otherwiseAdded)
							{
								otherwiseBldr.AppendLine("\t\t<Otherwise>");
								otherwiseBldr.AppendLine("\t\t\t<PropertyGroup>");
								otherwiseBldr.AppendLine(string.Format("\t\t\t\t<{0}Defines>{1} CODE_ANALYSIS</{0}Defines>", project,
									node.SelectSingleNode("c:DefineConstants", m_namespaceMgr).InnerText.Replace(";", " ")));
								otherwiseBldr.AppendLine("\t\t\t</PropertyGroup>");
								otherwiseBldr.AppendLine("\t\t</Otherwise>");
								otherwiseAdded = true;
							}
						}
						writer.Write(otherwiseBldr.ToString());
						writer.WriteLine("\t</Choose>");
						writer.WriteLine();

						writer.Write("\t<Target Name=\"{0}\"", project);
						var bldr = new StringBuilder();
						bldr.Append("Initialize"); // ensure the output directories and version files exist.
						if (project == "ParatextImportTests" || project == "FwCoreDlgsTests")
						{
							// The ParatextImportTests and FwCoreDlgsTests require that the ScrChecks.dll be in DistFiles/Editorial Checks.
							// We don't discover that dependency because it's not a reference (LT-13777).
							bldr.Append(";ScrChecks");
						}
						var dependencies = m_mapProjDepends[project];
						dependencies.Sort();
						foreach (var dep in dependencies)
						{
							if (m_mapProjFile.ContainsKey(dep))
								bldr.AppendFormat(";{0}", dep);
						}
						writer.Write(" DependsOnTargets=\"{0}\"", bldr);

						if (project == "MigrateSqlDbs")
						{
							writer.Write(" Condition=\"'$(OS)'=='Windows_NT'\"");
						}
						if (project.StartsWith("ManagedVwWindow"))
						{
							writer.Write(" Condition=\"'$(OS)'=='Unix'\"");
						}
						writer.WriteLine(">");

						// <MsBuild> task
						writer.WriteLine("\t\t<MSBuild Projects=\"{0}\"", m_mapProjFile[project].Replace(m_fwroot, "$(fwrt)"));
						writer.WriteLine("\t\t\tTargets=\"$(msbuild-target)\"");
						writer.WriteLine("\t\t\tProperties=\"$(msbuild-props);IntermediateOutputPath=$(dir-fwobj){0}{1}{0};DefineConstants=$({2}Defines);$(warningsAsErrors);WarningLevel=4;LcmArtifactsDir=$(LcmArtifactsDir)\"",
							Path.DirectorySeparatorChar, GetProjectSubDir(project), project);
						writer.WriteLine("\t\t\tToolsVersion=\"{0}\"/>", toolsVersion);
						// <Clouseau> verification task
						writer.WriteLine("\t\t<Clouseau Condition=\"'$(Configuration)' == 'Debug'\" AssemblyPathname=\"$(dir-outputBase)/{0}\"/>", AssemblyName);

						if (isTestProject)
						{
							// <NUnit> task
							writer.WriteLine("\t\t<Message Text=\"Running unit tests for {0}\" />", project);
							writer.WriteLine("\t\t<NUnit Condition=\"'$(action)'=='test'\"");
							writer.WriteLine("\t\t\tAssemblies=\"$(dir-outputBase)/{0}.dll\"", project);
							writer.WriteLine("\t\t\tToolPath=\"$(fwrt)/Bin/NUnit/bin\"");
							writer.WriteLine("\t\t\tWorkingDirectory=\"$(dir-outputBase)\"");
							writer.WriteLine("\t\t\tOutputXmlFile=\"$(dir-outputBase)/{0}.dll-nunit-output.xml\"", project);
							writer.WriteLine("\t\t\tForce32Bit=\"$(useNUnit-x86)\"");
							writer.WriteLine("\t\t\tExcludeCategory=\"$(excludedCategories)\"");
							// Don't continue on error. NUnit returns 0 even if there are failed tests.
							// A non-zero return code means a configuration error or that NUnit crashed
							// - we shouldn't ignore those.
							//writer.WriteLine("\t\t\tContinueOnError=\"true\"");
							writer.WriteLine("\t\t\tFudgeFactor=\"$(timeoutFudgeFactor)\"");
							writer.WriteLine("\t\t\tTimeout=\"{0}\">", TimeoutForProject(project));
							writer.WriteLine("\t\t\t<Output TaskParameter=\"FailedSuites\" ItemName=\"FailedSuites\"/>");
							writer.WriteLine("\t\t</NUnit>");
							writer.WriteLine("\t\t<Message Text=\"Finished building {0}.\" Condition=\"'$(action)'!='test'\"/>", project);
							writer.WriteLine("\t\t<Message Text=\"Finished building {0} and running tests.\" Condition=\"'$(action)'=='test'\"/>", project);
							// Generate dotCover task
							GenerateDotCoverTask(writer, new[] {project}, $"{project}.coverage.xml");
						}
						else
						{
							writer.WriteLine("\t\t<Message Text=\"Finished building {0}.\"/>", project);
						}
						writer.WriteLine("\t</Target>");
						writer.WriteLine();
					}
					writer.Write("\t<Target Name=\"allCsharp\" DependsOnTargets=\"");
					bool first = true;
					foreach (var project in m_mapProjFile.Keys)
					{
						// These projects are experimental.
						// These projects weren't built by nant normally.
						if (project == "FxtExe")
						{
							continue;
						}
						if (first)
							writer.Write(project);
						else
							writer.Write(";{0}", project);
						first = false;
					}
					writer.WriteLine("\"/>");
					writer.WriteLine();
					writer.Write("\t<Target Name=\"allCsharpNoTests\" DependsOnTargets=\"");
					first = true;
					foreach (var project in m_mapProjFile.Keys)
					{
						// These projects are experimental.
						// These projects weren't built by nant normally.
						if (project == "FxtExe" ||
							project.EndsWith("Tests") || // These are tests.
							project == "ProjectUnpacker") // This is only used in tests.
						{
							continue;
						}
						if (first)
							writer.Write(project);
						else
							writer.Write(";{0}", project);
						first = false;
					}
					writer.WriteLine("\"/>");

					writer.WriteLine("</Project>");
					writer.Flush();
					writer.Close();
				}
				Console.WriteLine("Created {0}", targetsFile);
			}
			catch (Exception e)
			{
				var badFile = targetsFile + ".bad";
				File.Move(targetsFile, badFile);
				Console.WriteLine("Failed to Create FieldWorks.targets bad result stored in {0}", badFile);
				throw new StopTaskException(e);
			}
		}

		private static void GenerateDotCoverTask(StreamWriter writer, IEnumerable<string> projects, string outputXml)
		{
			string assemblyList = projects.Aggregate("",
				(current, proj) => current + string.Format("$(dir-outputBase)/{0}.dll;", proj));
			writer.WriteLine("\t\t<Message Text=\"Running coverage analysis for {0}\" Condition=\"'$(action)'=='cover'\"/>",
				string.Join(", ", projects));
			writer.WriteLine("\t\t<GenerateTestCoverageReport Condition=\"'$(action)'=='cover'\"");
			writer.WriteLine("\t\t\tAssemblies=\"" + assemblyList + "\"");
			writer.WriteLine("\t\t\tNUnitConsoleExe=\"$(fwrt)/Bin/NUnit/bin/nunit-console-x86.exe\"");
			writer.WriteLine("\t\t\tDotCoverExe=\"$(DOTCOVER_HOME)/dotcover.exe\"");
			writer.WriteLine("\t\t\tWorkingDirectory=\"$(dir-outputBase)\"");
			writer.WriteLine("\t\t\tOutputXmlFile=\"$(dir-outputBase)/{0}\"/>", outputXml);
		}

		/// <summary>
		/// Return the timeout for running the tests in the given test project.
		/// </summary>
		/// <remarks>
		/// The timings for projects are now found in the fw/Build/TestTimeoutValues.xml file and can be changed there
		/// without having to rebuild the FwBuildTasks dll.  Values in XML are in seconds, which we convert to milliseconds here.
		/// </remarks>
		int TimeoutForProject(string project)
		{
			if (m_timeoutMap == null)
			{
				var timeoutDocument = XDocument.Load(Path.Combine(m_fwroot, "Build", "TestTimeoutValues.xml"));
				m_timeoutMap = new Dictionary<string, int>();
				var testTimeoutValuesElement = timeoutDocument.Root;
				m_timeoutMap["default"] = int.Parse(testTimeoutValuesElement.Attribute("defaultTimeLimit").Value);
				foreach (var timeoutElement in timeoutDocument.Root.Descendants("TimeoutGroup"))
				{
					var timeout = int.Parse(timeoutElement.Attribute("timeLimit").Value);
					foreach (var projectElement in timeoutElement.Descendants("Project"))
					{
						m_timeoutMap[projectElement.Attribute("name").Value] = timeout;
					}
				}
			}
			return (m_timeoutMap.ContainsKey(project) ? m_timeoutMap[project] : m_timeoutMap["default"])*1000;
		}
	}
}
