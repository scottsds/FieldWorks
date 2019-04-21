// Copyright (c) 2007-2013 SIL International
// This software is licensed under the LGPL, version 2.1 or later
// (http://www.gnu.org/licenses/lgpl-2.1.html)
//
// File: LexImport.cs
// Responsibility: FLEx Team

using System;
using System.Diagnostics;
using System.IO;
using System.Xml;
using System.Xml.Xsl;
using System.Collections.Generic;
using System.Text.RegularExpressions;
using SIL.FieldWorks.Common.FwUtils;
using SIL.LCModel;
using SIL.LCModel.Application.ApplicationServices;
using SIL.LCModel.Utils;

namespace SIL.FieldWorks.LexText.Controls
{
	/// ----------------------------------------------------------------------------------------
	/// <summary>
	///
	/// </summary>
	/// ----------------------------------------------------------------------------------------
	public class LexImport
	{
		private string m_sPhase2XSLT;
		private string m_sPhase3XSLT;
		private string m_sPhase4XSLT;
		private string m_sBuildPhase2XSLT;
		private LcmCache m_cache;
		private string m_sTempDir;
		private string m_sPhase4Output;
		private bool m_fDisplayImportReport;
		private string m_sPhase1HtmlReport;
		private string m_sPhase1FileName;

		public delegate void ErrorHandler(object sender, string message, string caption);
		public event ErrorHandler Error;

		public static readonly string s_sPhase1FileName = "Phase1Output.xml";
		public static readonly string s_sPhase2FileName = "Phase2Output.xml";
		public static readonly string s_sPhase3FileName = "Phase3Output.xml";
		public static readonly string s_sPhase4FileName = "Phase4Output.xml";

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Initializes a new instance of the <see cref="LexImport"/> class.
		/// </summary>
		/// <param name="cache">The FDO cache.</param>
		/// <param name="tempDir">The temp dir.</param>
		/// <param name="transformDir">The transform dir.</param>
		/// ------------------------------------------------------------------------------------
		public LexImport(LcmCache cache, string tempDir, string transformDir)
		{
			m_cache = cache;
			m_sTempDir = tempDir;

			// XSLT files
			m_sPhase2XSLT = Path.Combine(tempDir, "Phase2.xsl"); // needs to be in temp dir also sense it's created
			m_sPhase3XSLT = Path.Combine(transformDir, "Phase3.xsl");
			m_sPhase4XSLT = Path.Combine(transformDir, "Phase4.xsl");
			m_sBuildPhase2XSLT = Path.Combine(transformDir, "BuildPhase2XSLT.xsl");
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// does the import
		/// </summary>
		/// <param name="dlg">The progress dialog.</param>
		/// <param name="parameters">The parameters: 1) runToCompletion, 2) last step,
		/// 3) start phase, 4) database file name, 5) number of entries, 6) true to display
		/// the import report, 7) name of phase 1 HTML report file, 8) name of phase 1 file,
		/// 9) true to create entries for missing link targets.
		/// </param>
		/// <returns><c>true</c> if import was successful, otherwise <c>false</c>.</returns>
		/// ------------------------------------------------------------------------------------
		public object Import(IThreadedProgress dlg, object[] parameters)
		{
			Debug.Assert(parameters.Length == 9);
			bool runToCompletion = (bool)parameters[0];
			int lastStep = (int)parameters[1];
			int startPhase = (int)parameters[2];
			string databaseFileName = (string)parameters[3];
			int cEntries = (int)parameters[4];
			m_fDisplayImportReport = (bool)parameters[5];
			m_sPhase1HtmlReport = (string)parameters[6];
			m_sPhase1FileName = (string)parameters[7];
			var fCreateMissingLinks = (bool) parameters[8];

			string sErrorMsg = LexTextControls.ksTransformProblem_X;
			bool fAttemptedXml = false;
			string processedInputFile = databaseFileName;
			string sPhase1Output = Path.Combine(m_sTempDir, s_sPhase1FileName);
			string sPhase2Output = Path.Combine(m_sTempDir, s_sPhase2FileName);
			string sPhase3Output = Path.Combine(m_sTempDir, s_sPhase3FileName);
			m_sPhase4Output = Path.Combine(m_sTempDir, s_sPhase4FileName);

			XmlImportData xid = null;
			try
			{
				// if starting with a phase file, rename the phase file with the input file
				switch (startPhase)
				{
					case 1:
						sPhase1Output = databaseFileName;
						break;
					case 2:
						sPhase2Output = databaseFileName;
						break;
					case 3:
						sPhase3Output = databaseFileName;
						break;
					case 4:
						m_sPhase4Output = databaseFileName;
						break;
					default:
						break; // no renaming needed
				}

				if (startPhase < 2)
				{
					dlg.Title = String.Format(LexTextControls.ksImportingXEntriesFromY,
						cEntries, processedInputFile);
					dlg.Message = String.Format(LexTextControls.ksPhase1ofX_Preview, lastStep);
					sErrorMsg = LexTextControls.ksTransformProblemPhase1_X;
					DoTransform(m_sBuildPhase2XSLT, sPhase1Output, m_sPhase2XSLT);
				}
				dlg.Step(10);
				if (dlg.Canceled)
					return false;

				sErrorMsg = LexTextControls.ksTransformProblemPhase2_X;
				dlg.Message = String.Format(LexTextControls.ksPhase2ofX, lastStep);
				if (startPhase < 2)
					DoTransform(m_sPhase2XSLT, sPhase1Output, sPhase2Output);
				dlg.Step(10);
				if (dlg.Canceled)
					return false;

				sErrorMsg = LexTextControls.ksTransformProblemPhase3_X;
				dlg.Message = String.Format(LexTextControls.ksPhase3ofX, lastStep);
				if (startPhase < 3)
					DoTransform(m_sPhase3XSLT, sPhase2Output, sPhase3Output);
				dlg.Step(10);
				if (dlg.Canceled)
					return false;

				sErrorMsg = LexTextControls.ksTransformProblemPhase4_X;
				dlg.Message = String.Format(LexTextControls.ksPhase4ofX, lastStep);
				if (startPhase < 4)
					DoTransform(m_sPhase4XSLT, sPhase3Output, m_sPhase4Output);
				dlg.Step(20);
				if (dlg.Canceled)
					return false;

				if (runToCompletion)
				{
					sErrorMsg = LexTextControls.ksXmlParsingProblem5_X;
					dlg.Message = LexTextControls.ksPhase5of5_LoadingData;
					if (dlg.Canceled)
						return false;
					dlg.Step(1);
					// There's no way to cancel from here on out.
					dlg.AllowCancel = false;
					xid = new XmlImportData(m_cache, fCreateMissingLinks);
					fAttemptedXml = true;
					if (startPhase == 4 && processedInputFile.Length == 0)
						processedInputFile = m_sPhase4Output;
					xid.ImportData(m_sPhase4Output, dlg);
					sErrorMsg = LexTextControls.ksLogFileProblem5_X;
					ProcessLogFile(processedInputFile, startPhase, xid);
					return true;
				}
			}
			catch (Exception ex)
			{
				Debug.WriteLine("Error: " + ex.Message);

				ReportError(string.Format(sErrorMsg, ex.Message), LexTextControls.ksUnhandledError);

				if (fAttemptedXml)
				{
					// We want to see the log file even (especially) if the Xml blows up.
					ProcessLogFile(processedInputFile, startPhase, xid);
				}
			}

			return false;
		}

		private void DoTransform(string xsl, string xml, string output)
		{
			// Create the XslTransform and load the stylesheet.
			XslCompiledTransform xslt = new XslCompiledTransform();
			xslt.Load(xsl, System.Xml.Xsl.XsltSettings.TrustedXslt, null);

			// Create an XmlReader for input to the transform.
			using (XmlReader reader = XmlReader.Create(xml))
			{
				// Create an XmlTextWriter to output the result of the transform.
				using (XmlTextWriter writer = new XmlTextWriter(output, System.Text.Encoding.UTF8))
				{
					// Do NOT set Formatting.Indented. It can insert spurious white space, for example,
					// when the first child of a Custom element in phase 2 is an InFieldMarker, it inserts
					// a newline before it, which becomes part of the content of the string, with bad consequences
					// (LT-LT-13607)
					// NO! don't do this!!  writer.Formatting = Formatting.Indented;

					// Transform the file.
					xslt.Transform(reader, writer);
					writer.Close();
				}
			}
		}

		internal static string GetHtmlJavaScript()
		{
			// create a string for storing the jscript html code for showing the link
			string sRootDir = FwDirectoryFinder.CodeDirectory;
			if (!sRootDir.EndsWith("\\"))
				sRootDir += "\\";

			// TODO-Linux: this isn't portable this way
			string sNewLine = Environment.NewLine;
			string zedit = "\"" + sRootDir + "zedit.exe\"";
			zedit = zedit.Replace(@"\", @"\\");
			string script = @"<script>" + sNewLine +
				@"function exec (command) {" + sNewLine +
				@"command = '" + zedit + " ' + command" + sNewLine +
				@"//alert(command) // look at the command" + sNewLine +
				@"if (document.layers && navigator.javaEnabled()) {" + sNewLine +
				@"window._command = command;" + sNewLine +
				@"window.oldOnError = window.onerror;" + sNewLine +
				@"window.onerror = function (err) {" + sNewLine +
				@"if (err.indexOf (""User didn't grant"") != -1) {" + sNewLine +
				@"alert('command execution of ' + window._command + ' disallowed by user.'); " + sNewLine +
				@"return true;" + sNewLine +
				@"}" + sNewLine +
				@"else return false;" + sNewLine +
				@"}" + sNewLine +
				@"netscape.security.PrivilegeManager.enablePrivilege('UniversalExecAccess');" + sNewLine +
				@"java.lang.Runtime.getRuntime().exec(command);" + sNewLine +
				@"window.onerror = window.oldOnError;" + sNewLine +
				@"}" + sNewLine +
				@"else if (document.compatMode) {" + sNewLine +
				@"window.oldOnError = window.onerror;" + sNewLine +
				@"window._command = command;" + sNewLine +
				@"window.onerror = function (err) {" + sNewLine +
				@"if (err.indexOf('utomation') != -1) {" + sNewLine +
				@"alert('command execution of ' + window._command + ' disallowed by user.'); " + sNewLine +
				@"return true;" + sNewLine +
				@"}" + sNewLine +
				@"else return false;" + sNewLine +
				@"};" + sNewLine +
				@"var wsh = new ActiveXObject('WScript.Shell');" + sNewLine +
				@"if (wsh)" + sNewLine +
				@"wsh.Run(command);" + sNewLine +
				@"window.onerror = window.oldOnError;" + sNewLine +
				@"}" + sNewLine +
				@"}" + sNewLine +
				@"</script>" + sNewLine;
			return script;
		}

		private void ProcessLogFile(string processedInputFile, int startPhase, XmlImportData xid)
		{
			string inputFileName = processedInputFile;
			if (startPhase > 0)
			{
				inputFileName = m_sPhase1FileName;
				inputFileName = inputFileName.Replace("1", startPhase.ToString());
			}
			ProcessPhase4Log(inputFileName, xid);

			if (m_fDisplayImportReport)
			{
				string sHtmlFile = Path.Combine(m_sTempDir, "ImportLog.htm");
				using (Process.Start(sHtmlFile))
				{
				}
			}
		}

		private void ProcessPhase4Log(string inputFileName, XmlImportData xid)
		{
			string sLogFile = m_sPhase4Output;
			int ich = m_sPhase4Output.LastIndexOf('.');
			if (ich != -1)
				sLogFile = m_sPhase4Output.Remove(ich, sLogFile.Length - ich);
			sLogFile += "-Import.log";
			StreamReader sr;
			try
			{
				sr = File.OpenText(sLogFile);
			}
			catch
			{
				return;
			}
			try
			{
				string sHtmlFile = Path.Combine(m_sTempDir, "ImportLog.htm");
				using (StreamWriter sw = File.CreateText(sHtmlFile))
				{
					sw.WriteLine("<html>");
					sw.WriteLine("<head>");
					sw.WriteLine("<meta http-equiv=\"Content-Type\" content=\"text/html; charset=UTF-8\"/>");
					string sHeadInfo = String.Format(LexTextControls.ksImportLogForX, inputFileName);
					sw.WriteLine(String.Format("  <title>{0}</title>", sHeadInfo));
					// add the script
					string script = GetHtmlJavaScript();
					sw.WriteLine(script);
					// done adding the java script for jumping to errors
					sw.WriteLine("</head>");
					sw.WriteLine("<body>");
					sw.WriteLine(String.Format("<h2>{0}</h2>", sHeadInfo));
					sw.WriteLine(String.Format("<h3>{0}</h3>", LexTextControls.ksMessagesFromPreview));
					sw.WriteLine(m_sPhase1HtmlReport);
					string sInput;
					// LT-1901 : make a first pass through the log file and put all "Warning:" errors together
					bool bWarningFound = false;	// none yet
					string sWarning_ = LexTextControls.ksWarning_;	// localized version of "Warning:"
					string sInfo_ = LexTextControls.ksInfo_;		// localized version of "Info:"
					while ((sInput = sr.ReadLine()) != null)
					{
						ich = sInput.IndexOf(sWarning_);
						if (ich == -1)
							ich = sInput.IndexOf("Warning:");	// in case warning message not localized...
						if (ich != -1)
						{
							if (!bWarningFound)	// first time put out the header
							{
								bWarningFound = true;
								sw.WriteLine(String.Format("<p><h3>{0}</h3>",
									LexTextControls.ksMayBeBugInImport));
								sw.WriteLine("<ul>");
							}

							// Need to quote any occurrences of <, >, or & in the message text.
							sInput = sInput.Replace("&", "&amp;");
							sInput = sInput.Replace("<", "&lt;");
							sInput = sInput.Replace(">", "&gt;");
							sw.WriteLine("<li>" + sInput);
						}
					}
					sr.Dispose();
					sr = File.OpenText(sLogFile);

					if (bWarningFound)
						sw.WriteLine("</ul>");

					sw.WriteLine(String.Format("<p><h3>{0}</h3>",
						LexTextControls.ksMessagesFromLoading));
					string sTiming = null;
					sw.WriteLine("<ul>");

					string sPath = m_sPhase4Output.Replace("\\", "\\\\");
					List<string> rgsCreated = xid.CreatedForMessages;
					List<Regex> rgxMsgs = new List<Regex>();
					foreach (string sMsg in rgsCreated)
					{
						string sRegex = "^" + sMsg + "$";
						sRegex = sRegex.Replace("{0}", sPath);
						sRegex = sRegex.Replace("{1}", "[0-9]+");
						sRegex = sRegex.Replace("{2}", "[^\"]+");
						Regex xMsg = new Regex(sRegex);
						rgxMsgs.Add(xMsg);
					}

					string sElapsedTimeMsg = xid.ElapsedTimeMsg;
					ich = sElapsedTimeMsg.IndexOf("{0:F1}");
					Debug.Assert(ich >= 0);
					sElapsedTimeMsg = sElapsedTimeMsg.Substring(0, ich);
					// Print the Info: messages together, save everything else for a later loop.
					List<string> rgsNotInfo = new List<string>();
					while ((sInput = sr.ReadLine()) != null)
					{
						// warning msgs were already handled, so don't show them again
						ich = sInput.IndexOf(sWarning_);
						if (ich == -1)
							ich = sInput.IndexOf("Warning:");	// in case warning message not localized...
						if (ich != -1)
							continue;
						ich = sInput.IndexOf(m_sPhase4Output + ":");
						if (ich != -1)
						{
							int ichNumber = ich + m_sPhase4Output.Length + 1;
							ich = sInput.IndexOf(": ", ichNumber);
							if (ich != -1)
							{
								// Need to quote any occurrences of <, >, or & in the message text.
								string sOutput = sInput.Remove(0, ich + 2);
								sOutput = sOutput.Replace("&", "&amp;");
								sOutput = sOutput.Replace("<", "&lt;");
								sOutput = sOutput.Replace(">", "&gt;");
								if (AnyMsgMatches(rgxMsgs, sInput))
								{
									string sNumber = sInput.Substring(ichNumber, ich - ichNumber);
									int hvo;
									if (Int32.TryParse(sNumber, out hvo))
									{
										// Convert text between ichBegin and ichEnd into a link to the Flex entry.
										string sLinkRef = LinkRef(hvo);
										if (sLinkRef != null)
										{
											int ichBegin = sOutput.IndexOf('"') + 1;
											int ichEnd = sOutput.IndexOf('"', ichBegin);
											Debug.Assert(ichBegin > 0 && ichEnd > ichBegin);
											sOutput = sOutput.Insert(ichEnd, "</a>");
											sOutput = sOutput.Insert(ichBegin, String.Format("<a href=\"{0}\">", sLinkRef));
										}
									}
								}
								sOutput = sOutput.Insert(0, "<li>");
								if (sOutput.IndexOf(sInfo_) >= 0 || sOutput.IndexOf("Info:") >= 0)
									sw.WriteLine(sOutput);
								else
									rgsNotInfo.Add(sOutput);
							}
						}
						else
						{
							ich = sInput.IndexOf(sElapsedTimeMsg);
							if (ich != -1)
								sTiming = sInput;
						}
					}
					sr.Close();
					for (int i = 0; i < rgsNotInfo.Count; ++i)
						sw.WriteLine(rgsNotInfo[i]);
					sw.WriteLine("</ul>");
					if (sTiming != null)
						sw.WriteLine("<p>" + sTiming);
					sw.WriteLine("</body>");
					sw.WriteLine("</html>");
					sw.Close();
				}
			}
			finally
			{
				sr.Dispose();
			}
		}

		private bool AnyMsgMatches(List<Regex> rgxCreated, string sInput)
		{
			foreach (Regex xMsg in rgxCreated)
			{
				if (xMsg.IsMatch(sInput))
					return true;
			}
			return false;
		}

		internal string LinkRef(int hvo)
		{
			ICmObjectRepository repo = m_cache.ServiceLocator.GetInstance<ICmObjectRepository>();
			if (!repo.IsValidObjectId(hvo))
				return null;
			Guid guid = repo.GetObject(hvo).Guid;
			FwLinkArgs link = new FwLinkArgs("lexiconEdit", guid);
			return link.ToString();
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Reports an error.
		/// </summary>
		/// <param name="message">The message.</param>
		/// <param name="caption">The caption.</param>
		/// ------------------------------------------------------------------------------------
		private void ReportError(string message, string caption)
		{
			if (Error != null)
				Error(this, message, caption);
		}
	}
}
