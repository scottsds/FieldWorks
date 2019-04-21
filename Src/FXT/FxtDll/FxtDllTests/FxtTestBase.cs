// Copyright (c) 2003-2017 SIL International
// This software is licensed under the LGPL, version 2.1 or later
// (http://www.gnu.org/licenses/lgpl-2.1.html)

using System;
using System.Diagnostics;
using System.IO;
using System.Xml;
using System.Xml.Xsl;

using NUnit.Framework;

using SIL.FieldWorks.Common.FwUtils;
using SIL.LCModel;
using SIL.LCModel.Utils;

namespace SIL.FieldWorks.Common.FXT
{
	/// <summary>
	/// Summary description for FxtTestBase.
	/// </summary>
	public class FxtTestBase : MemoryOnlyBackendProviderTestBase
	{
		/// <summary>Time when <see cref="startClock"/> was called</summary>
		protected DateTime m_dtstart;
		/// <summary>Timespan between the call of <see cref="startClock"/> and
		/// <see cref="stopClock"/></summary>
		protected TimeSpan m_tsTimeSpan;
		/// <summary>
		/// Location of XML result control files
		/// </summary>
		protected string m_sExpectedResultsPath;

		/// <summary>
		/// any filters that we want, for example, to only output items which satisfy their constraint.
		/// </summary>
		protected IFilterStrategy[] m_filters;

		/// <summary>
		/// start timing something.  Follow up with stopClock() and writeTimeSpan()
		/// </summary>
		protected void startClock()
		{
			m_dtstart = DateTime.Now;
		}
		/// <summary>
		/// stop timing something.  Follow up with writeTimeSpan ()
		/// </summary>
		protected void stopClock()
		{
			m_tsTimeSpan = new TimeSpan(DateTime.Now.Ticks - m_dtstart.Ticks);
		}
		/// <summary>
		/// write out a timespan in seconds and fractions of seconds to the debugger output window.
		/// </summary>
		/// <param name="sLabel"></param>
		protected void writeTimeSpan(string sLabel)
		{
			Console.WriteLine(sLabel + " " + m_tsTimeSpan.TotalSeconds.ToString() + " Seconds");
			Debug.WriteLine("");
			Debug.WriteLine("");
			Debug.WriteLine("");
			Debug.WriteLine(sLabel + " " + m_tsTimeSpan.TotalSeconds.ToString() + " Seconds");
		}

		[TestFixtureSetUp]
		public virtual void Init()
		{
			m_sExpectedResultsPath = Path.Combine(FwDirectoryFinder.SourceDirectory,
															Path.Combine("FXT",
															Path.Combine("FxtDll",
															Path.Combine("FxtDllTests", "ExpectedResults"))));
		}

		protected void DoDump (string databaseName, string label, string fxtPath, string answerPath)
		{
			DoDump(databaseName, label, fxtPath, answerPath, false);
		}
		protected void DoDump (string databaseName, string label, string fxtPath, string answerPath, bool outputGuids)
		{
			DoDump(databaseName, label, fxtPath, answerPath, outputGuids, false);
		}

		protected void DoDump (string databaseName, string label, string fxtPath, string answerPath, bool outputGuids, bool bApplyTransform)
		{
			XDumper dumper = PrepareDumper(databaseName, fxtPath, outputGuids);
			string outputPath = FileUtils.GetTempFile("xml");
			PerformDump(dumper, outputPath, databaseName, label);
			if(answerPath!=null)
				FileAssert.AreEqual(answerPath, outputPath);
		}

		protected static void PerformTransform(string xsl, string inputPath, string sTransformedResultPath)
		{
			XslCompiledTransform transformer = new XslCompiledTransform();
			transformer.Load(xsl);
			TextWriter writer = null;
			try
			{
				//writer = File.CreateText(sTransformedResultPath);
				//XmlDocument inputDOM = new XmlDocument();
				//inputDOM.Load(inputPath);
				//transformer.Transform(inputDOM, null, writer);
				transformer.Transform(inputPath, sTransformedResultPath);
			}
			finally
			{
				if (writer != null)
					writer.Close();
			}
		}

		protected void PerformDump(XDumper dumper, string outputPath, string databaseName, string label)
		{
			startClock();
			dumper.Go(Cache.LanguageProject, File.CreateText(outputPath), m_filters);
			stopClock();
			writeTimeSpan(databaseName+": " + label);
		}

		protected XDumper PrepareDumper(string databaseName, string fxtPath, bool doOutputGuids)
		{
			XDumper dumper = new XDumper(Cache);
			dumper.FxtDocument = new XmlDocument();
			dumper.FxtDocument.Load(fxtPath);
			dumper.OutputGuids= doOutputGuids;
			return dumper;
		}

	}
}
