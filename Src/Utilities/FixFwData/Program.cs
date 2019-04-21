﻿// Copyright (c) 2011-2013 SIL International
// This software is licensed under the LGPL, version 2.1 or later
// (http://www.gnu.org/licenses/lgpl-2.1.html)
//
// File: Program.cs
// Responsibility: FLEx team

using System;
using System.ComponentModel;
using SIL.LCModel.FixData;
using SIL.Reporting;
using SIL.LCModel.Utils;
using SIL.Windows.Forms.HotSpot;

namespace FixFwData
{
	class Program
	{
		private static int Main(string[] args)
		{
			SetUpErrorHandling();
			var pathname = args[0];
			using (var prog = new NullProgress())
			{
				var data = new FwDataFixer(pathname, prog, logger, counter);
				data.FixErrorsAndSave();
			}
			if (errorsOccurred)
				return 1;
			return 0;
		}

		private static bool errorsOccurred;
		private static int errorCount;

		private static void logger(string description, bool errorFixed)
		{
			Console.WriteLine(description);
			errorsOccurred = true;
			if (errorFixed)
				++errorCount;
		}

		private static int counter()
		{
			return errorCount;
		}

		private static void SetUpErrorHandling()
		{
			using (new HotSpotProvider())
			{
				ErrorReport.EmailAddress = "flex_errors@sil.org";
				ErrorReport.AddStandardProperties();
				ExceptionHandler.Init();
			}
		}

		private sealed class NullProgress : IProgress, IDisposable
		{
			public event CancelEventHandler Canceling;

			public void Step(int amount)
			{
				if (Canceling != null)
				{
					// don't do anything -- this just shuts up the compiler about the
					// event handler never being used.
				}
			}

			public string Title { get; set; }

			public string Message
			{
				get { return null; }
				set { Console.Out.WriteLine(value); }
			}

			public int Position { get; set; }
			public int StepSize { get; set; }
			public int Minimum { get; set; }
			public int Maximum { get; set; }
			public ISynchronizeInvoke SynchronizeInvoke { get; private set; }
			public bool IsIndeterminate
			{
				get { return false; }
				set { }
			}

			public bool AllowCancel
			{
				get { return false; }
				set { }
			}
			#region Clouseau required cruft
#if DEBUG
			/// <summary/>
			~NullProgress()
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
				System.Diagnostics.Debug.WriteLineIf(!fDisposing, "****** Missing Dispose() call for " + GetType() + ". *******");
			}
			#endregion
		}
	}
}
