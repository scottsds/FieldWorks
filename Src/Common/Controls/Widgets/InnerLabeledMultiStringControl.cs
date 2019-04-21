// Copyright (c) 2015 SIL International
// This software is licensed under the LGPL, version 2.1 or later
// (http://www.gnu.org/licenses/lgpl-2.1.html)

using System.Collections.Generic;
using System.Diagnostics;
using System.Windows.Forms;		// controls and etc...
using SIL.LCModel.Core.WritingSystems;
using SIL.LCModel.Core.KernelInterfaces;
using SIL.LCModel;
using SIL.FieldWorks.Common.ViewsInterfaces;
using SIL.FieldWorks.Common.RootSites;
using SIL.LCModel.DomainServices;

namespace SIL.FieldWorks.Common.Widgets
{
	internal class InnerLabeledMultiStringControl : SimpleRootSite
	{
		LabeledMultiStringVc m_vc;
		LcmCache m_realCache; // real one we get writing system info from
		ISilDataAccess m_sda; // one actually used in the view.
		List<CoreWritingSystemDefinition> m_rgws;

		internal const int khvoRoot = -3045; // arbitrary but recognizeable numbers for debugging.
		internal const int kflid = 4554;

		public InnerLabeledMultiStringControl(LcmCache cache, int wsMagic)
		{
			m_realCache = cache;
			m_sda = new TextBoxDataAccess { WritingSystemFactory = cache.WritingSystemFactory };
			m_rgws = WritingSystemServices.GetWritingSystemList(cache, wsMagic, 0, false);

			AutoScroll = true;
			IsTextBox = true;	// range selection not shown when not in focus
		}

		public InnerLabeledMultiStringControl(LcmCache cache, List<CoreWritingSystemDefinition> wsList)
		{
			// Ctor for use with a non-standard list of wss (like available UI languages)
			m_realCache = cache;
			m_sda = new TextBoxDataAccess { WritingSystemFactory = cache.WritingSystemFactory };
			m_rgws = wsList;

			AutoScroll = true;
			IsTextBox = true;	// range selection not shown when not in focus
		}

		#region IDisposable override

		/// <summary>
		/// Executes in two distinct scenarios.
		///
		/// 1. If disposing is true, the method has been called directly
		/// or indirectly by a user's code via the Dispose method.
		/// Both managed and unmanaged resources can be disposed.
		///
		/// 2. If disposing is false, the method has been called by the
		/// runtime from inside the finalizer and you should not reference (access)
		/// other managed objects, as they already have been garbage collected.
		/// Only unmanaged resources can be disposed.
		/// </summary>
		/// <remarks>
		/// If any exceptions are thrown, that is fine.
		/// If the method is being done in a finalizer, it will be ignored.
		/// If it is thrown by client code calling Dispose,
		/// it needs to be handled by fixing the bug.
		///
		/// If subclasses override this method, they should call the base implementation.
		/// </remarks>
		protected override void Dispose(bool disposing)
		{
			Debug.WriteLineIf(!disposing, "****************** Missing Dispose() call for " + GetType().Name + " ******************");
			// Must not be run more than once.
			if (IsDisposed)
				return;

			// m_sda COM object block removed due to crash in Finializer thread LT-6124

			if (disposing)
			{
				// Dispose managed resources here.
			}

			// Dispose unmanaged resources here, whether disposing is true or false.
			m_sda = null;
			m_realCache = null;
			m_rgws = null;
			m_vc = null;

			base.Dispose(disposing);
		}

		#endregion IDisposable override

		/// <summary>
		/// Get the number of writing systems being displayed.
		/// </summary>
		public List<CoreWritingSystemDefinition> WritingSystems
		{
			get
			{
				CheckDisposed();
				return m_rgws;
			}
		}

		/// <summary></summary>
		public override void MakeRoot()
		{
			CheckDisposed();

			if (DesignMode)
				return;

			// The simple root site won't lay out properly until this is done.
			// It needs to be done before base.MakeRoot or it won't lay out at all ever!
			WritingSystemFactory = m_realCache.WritingSystemFactory;

			base.MakeRoot();

			m_rootb.DataAccess = m_sda;

			int wsUser = m_realCache.ServiceLocator.WritingSystemManager.UserWs;
			int wsEn = m_realCache.ServiceLocator.WritingSystemManager.GetWsFromStr("en");
			m_vc = new LabeledMultiStringVc(kflid, WritingSystems, wsUser, true, wsEn);

			// arg3 is a meaningless initial fragment, since this VC only displays one thing.
			m_rootb.SetRootObject(khvoRoot, m_vc, 1, m_styleSheet);
			m_dxdLayoutWidth = kForceLayout; // Don't try to draw until we get OnSize and do layout.
		}

		/// <summary>
		/// User pressed a key.
		/// </summary>
		protected override void OnKeyDown(KeyEventArgs e)
		{
			if (!m_editingHelper.HandleOnKeyDown(e))
				base.OnKeyDown(e);
			if (!e.Handled && (e.KeyCode == Keys.Up || e.KeyCode == Keys.Down))
			{
				MultiStringSelectionUtils.HandleUpDownArrows(e, RootBox, EditingHelper.CurrentSelection, WritingSystems, kflid);
			}
		}

		internal ITsString Value(int ws)
		{
			return m_sda.get_MultiStringAlt(khvoRoot, kflid, ws);
		}

		internal void SetValue(int ws, ITsString tss)
		{
			m_sda.SetMultiStringAlt(khvoRoot, kflid, ws, tss);
		}
	}
}
