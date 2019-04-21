// Copyright (c) 2010-2017 SIL International
// This software is licensed under the LGPL, version 2.1 or later
// (http://www.gnu.org/licenses/lgpl-2.1.html)

using System;
using System.IO;
using Microsoft.Win32;
using SIL.LCModel.Utils;
using SIL.PlatformUtilities;

namespace SIL.FieldWorks.UnicodeCharEditor
{
	/// ----------------------------------------------------------------------------------------
	/// <summary>
	/// Summary description for LogFile.
	/// </summary>
	/// 	/// ----------------------------------------------------------------------------------------
	public static class LogFile
	{
		#region Static methods to interact with the logging
		///<summary>
		///</summary>
		private static LogFileImpl GetLogFile()
		{
			return SingletonsContainer.Get<LogFileImpl>();
		}

		///<summary>
		///</summary>
		///<param name="line"></param>
		public static void AddErrorLine(string line)
		{
			GetLogFile().AddLineX(line, true);
		}

		///<summary>
		///</summary>
		///<param name="line"></param>
		public static void AddLine(string line)
		{
			GetLogFile().AddLineX(line, false);
		}

		///<summary>
		///</summary>
		///<param name="line"></param>
		public static void AddVerboseLine(string line)
		{
			if (GetLogFile().VerboseLogging)
				GetLogFile().AddLineX("    (" + line + ")", false);
		}

		///<summary>
		///</summary>
		///<returns></returns>
		public static bool IsLogging()
		{
			return GetLogFile().Logging;
		}

		///<summary>
		///</summary>
		public static void Release()
		{
			if (!SingletonsContainer.Contains<LogFileImpl>())
				return;

			AddLine("----- LogFile Object Released -----");
			SingletonsContainer.Get<LogFileImpl>().Shutdown();
			SingletonsContainer.Remove(SingletonsContainer.Get<LogFileImpl>());
		}

		#endregion

		private sealed class LogFileImpl: IDisposable
		{
			#region private member variables
			private readonly string m_sFileName;
			private StreamWriter m_file;
			#endregion

			#region Properties
			public bool Logging { get; private set; }
			public bool VerboseLogging { get; private set; }
			#endregion

			#region public methods to do the work
			public LogFileImpl()
			{
				Logging = false;
				m_file = null;

				m_sFileName = "";
				try
				{
					// Try to find the key.
					using (RegistryKey regKey = Registry.LocalMachine.OpenSubKey(@"SOFTWARE\SIL\FieldWorks"))
					{
						if (regKey != null)
						{
							var useLogFile = (string)regKey.GetValue("InstallLanguageUseLog");

							if (useLogFile != null)
							{
								if (useLogFile.Substring(0, 1).ToUpperInvariant() == "T" ||	// true
									useLogFile.Substring(0, 1).ToUpperInvariant() == "Y" ||	// yes
									useLogFile == "1")								// 1
								{
									Logging = true;
									VerboseLogging = false;
								}

								if (useLogFile.Substring(0, 1).ToUpperInvariant() == "V")		// verbose
								{
									Logging = true;
									VerboseLogging = true;
								}

								if (Logging)	// logging is enabled
								{
									m_sFileName = (string)regKey.GetValue("InstallLanguageLog");
									if (m_sFileName != null)
										m_file = new StreamWriter(m_sFileName, true) { AutoFlush = true};
									else
									{
										Console.WriteLine(
											@"Need to specify InstallLanguageLog in HKLM\SOFTWARE\SIL\FieldWorks");
										Logging = false;
									}
								}
							}
							regKey.Close();
						}
					}
					AddLineX("----- LogFile Object Created -----", false);
				}
				catch (Exception e)
				{
					Console.WriteLine(@"An error occurred: '{0}'", e);
					m_sFileName = "";	// can't log with exception somewhere...
					Logging = false;
				}
			}

			public void AddLineX(string line, bool echoToStdError)
			{
				var dateStamp = string.Format("[{0}] ", DateTime.Now);

				//			// always log to the debug output window
				//			System.Diagnostics.Debug.Write(dateStamp, "Log");
				if (Platform.IsWindows)
				{
					// TODO-Linux: this breaks unit test: InstallLanguageTests.IcuTests.TestInstallLanguage_argumentParser
					// since System.Diagnostics.Debug goes to StdOut on Linux.
					System.Diagnostics.Debug.WriteLine(line);
				}

				if (!Logging)
					return;

				m_file.Write(dateStamp);
				m_file.WriteLine(line);

				if (echoToStdError)
				{
					Console.Error.Write(dateStamp);
					Console.Error.WriteLine(line);
				}
			}

			public void Shutdown()
			{
				if (Logging)
					m_file.Close();

				Dispose();
			}

			#region Disposable stuff
			#if DEBUG
			/// <summary/>
			~LogFileImpl()
			{
				Dispose(false);
			}
			#endif

			/// <summary/>
			public void Dispose()
			{
				Dispose(true);
				GC.SuppressFinalize(this);
			}

			/// <summary/>
			private void Dispose(bool fDisposing)
			{
				System.Diagnostics.Debug.WriteLineIf(!fDisposing, "****** Missing Dispose() call for " + GetType() + " *******");
				if (fDisposing)
				{
					// dispose managed and unmanaged objects
					if (m_file != null)
						m_file.Dispose();
				}
				m_file = null;
			}
			#endregion
			#endregion
		}
	}
}
