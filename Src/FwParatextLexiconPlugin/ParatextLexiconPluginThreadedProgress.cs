﻿// Copyright (c) 2015 SIL International
// This software is licensed under the LGPL, version 2.1 or later
// (http://www.gnu.org/licenses/lgpl-2.1.html)

using System;
using System.ComponentModel;
using SIL.LCModel.Utils;

namespace SIL.FieldWorks.ParatextLexiconPlugin
{
	internal class ParatextLexiconPluginThreadedProgress : IThreadedProgress
	{
		private readonly ISynchronizeInvoke m_synchronizeInvoke;

		public ParatextLexiconPluginThreadedProgress(ISynchronizeInvoke synchronizeInvoke)
		{
			m_synchronizeInvoke = synchronizeInvoke;
		}

		public void Step(int amount)
		{
			Position += amount * StepSize;
			//Progress.Mgr.Incr(amount * StepSize);
		}

		public string Title { get; set; }

		public string Message
		{
			//get { return Progress.Mgr.Text; }
			//set { Progress.Mgr.Text = value; }
			get; set;
		}

		public int Position
		{
			//get { return Progress.Mgr.Value; }
			//set { Progress.Mgr.Value = value; }
			get; set;
		}

		public int StepSize { get; set; }

		public int Minimum { get; set; }

		public int Maximum { get; set; }

		public ISynchronizeInvoke SynchronizeInvoke
		{
			get { return m_synchronizeInvoke; }
		}

		public bool IsIndeterminate { get; set; }

		public bool AllowCancel { get; set; }

		public bool IsCanceling
		{
			get { return false; }
		}

		public event CancelEventHandler Canceling;

		public object RunTask(Func<IThreadedProgress, object[], object> backgroundTask, params object[] parameters)
		{
			return RunTask(true, backgroundTask, parameters);
		}

		public object RunTask(bool fDisplayUi, Func<IThreadedProgress, object[], object> backgroundTask, params object[] parameters)
		{
			object result = backgroundTask(this, parameters);
			//if (Progress.Mgr.InProgress)
			//{
			//    result = backgroundTask(this, parameters);
			//}
			//else
			//{
			//    ProgressUtils.Execute(Title, AllowCancel ? CancelModes.Cancelable : CancelModes.NonCancelable, () =>
			//        {
			//            if (!IsIndeterminate)
			//                Progress.Mgr.Begin(Minimum, Maximum);

			//            result = backgroundTask(this, parameters);

			//            if (!IsIndeterminate)
			//                Progress.Mgr.End();
			//        });
			//}
			return result;
		}

		public bool Canceled
		{
			//get { return Progress.Mgr.Cancelled; }
			get; set;
		}
	}
}
