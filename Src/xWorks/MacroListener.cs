﻿// Copyright (c) 2015 SIL International
// This software is licensed under the LGPL, version 2.1 or later
// (http://www.gnu.org/licenses/lgpl-2.1.html)

using System;
using System.Collections.Generic;
using System.Windows.Forms;
using System.Xml;
using SIL.LCModel.Core.KernelInterfaces;
using SIL.FieldWorks.Common.ViewsInterfaces;
using SIL.LCModel;
using SIL.LCModel.Infrastructure;
using XCore;
using SIL.Utils;

namespace SIL.FieldWorks.XWorks
{
	/// <summary>
	/// The MacroListener class allows FLEx to have user-defined macros anywhere there is a view-based selection.
	/// To create a macro, make an Assembly with a name starting with "Macro" that implements IFlexMacro, build it,
	/// and drop the DLL in the FieldWorks root directory.
	/// </summary>
	public class MacroListener : IxCoreColleague
	{
		private Mediator m_mediator;
		private PropertyTable m_propertyTable;

		/// <summary>
		/// Standard member for IXCoreColleague. The configurationParameters are not currently used.
		/// </summary>
		/// <param name="mediator"></param>
		/// <param name="propertyTable"></param>
		/// <param name="configurationParameters"></param>
		public void Init(Mediator mediator, PropertyTable propertyTable, XmlNode configurationParameters)
		{
			m_mediator = mediator;
			m_propertyTable = propertyTable;
			m_mediator.AddColleague(this);
		}

		/// <summary>
		/// Standard member for IXCoreColleague. The only object known to this colleague to handle messages is itself.
		/// </summary>
		/// <returns></returns>
		public IxCoreColleague[] GetMessageTargets()
		{
			return new IxCoreColleague[] {this};
		}

		/// <summary>
		/// Nothing to do by way of disposing, so this can be called any time.
		/// </summary>
		public bool ShouldNotCall
		{
			get { return false; }
		}

		/// <summary>
		/// High priority colleague, this is implementing a direct user command, not some background task.
		/// </summary>
		public int Priority
		{
			get { return (int) ColleaguePriority.High; }
		}

		// Number of distinct macros we support.
		// Note that just increasing this won't help. You will also need to add new commands and menu items to the
		// XML configuration, typically Main.xml, with appropriate key parameters. Even then some work will be needed in GetMacroIndex
		// to remove the assumption that slots are assigned to consecutive keys.
		private const int MacroCount = 11;

		private IFlexMacro[] m_macros;

		/// <summary>
		/// the macros; positions correspond to function keys, 0 => F2; unimplemented are null.
		/// </summary>
		internal IFlexMacro[] Macros
		{
			get
			{
				if (m_macros == null)
				{
					var macroImplementors = DynamicLoader.GetPlugins<IFlexMacro>("Macro*.dll");
					m_macros = AssignMacrosToSlots(macroImplementors);
				}
				return m_macros;
			}
			set { m_macros = value; } // for testing
		}

		internal IFlexMacro[] AssignMacrosToSlots(List<IFlexMacro> macroImplementors)
		{
			IFlexMacro[] macros = new IFlexMacro[MacroCount];
			var conflicts = new List<IFlexMacro>();
			// Put each at its preferred key if possible
			foreach (var macro in macroImplementors)
			{
				int index = GetMacroIndex(macro.PreferredFunctionKey);
				if (macros[index] == null)
					macros[index] = macro;
				else
					conflicts.Add(macro);
			}
			// Put any conflicts in remaining slots; if too many, arbitrary ones will be left out.
			foreach (var macro in conflicts)
			{
				for (int index = 0; index < MacroCount; index++)
				{
					if (macros[index] == null)
					{
						macros[index] = macro;
						break;
					}
				}
			}
			return macros;
		}

		private int GetMacroIndex(Keys key)
		{
			int result = key - Keys.F2;
			if (result < 0 || result >= MacroCount)
				throw new ArgumentException("Key assigned to macro must currently be between F2 and F12");
			return result;
		}

		/// <summary>
		/// Get the active selection in the mediator's main window. This determines what we will apply the command to.
		/// </summary>
		/// <returns></returns>
		private IVwSelection GetSelection()
		{
			var window = m_propertyTable.GetValue<FwXWindow>("window");
			if (window == null || !(window.ActiveView is IVwRootSite))
				return null;
			var rootBox = ((IVwRootSite) window.ActiveView).RootBox;
			if (rootBox == null)
				return null; // paranoia
			return rootBox.Selection;
		}

		/// <summary>
		/// Invoked by reflection when the appropriate menu command is executed. Which one is indicated by the paramters node.
		/// </summary>
		/// <param name="commandObject"></param>
		/// <returns>true (always) to indicate that we handled this command</returns>
		public bool OnMacro(object commandObject)
		{
			return DoMacro(commandObject, GetSelection());
		}

		// The body of OnMacro is isolated so we don't have to fake all the objects needed to get the (typically mock) selection in testing.
		internal bool DoMacro(object commandObject, IVwSelection sel)
		{
			var macro = GetMacro(commandObject);
			if (macro == null)
				return true; // Paranoia, it should be disabled.

			int ichA, hvoA, flid, ws, ichE, start, length;
			ICmObject obj;
			if (!SafeToDoMacro(sel, out obj, out flid, out ws, out start, out length) || !macro.Enabled(obj, flid, ws, start, length))
				return true;
			string commandName = macro.CommandName;
			// We normally let undo and redo be localized independently, but we compromise in the interests of making macros
			// easier to create.
			string undo = string.Format(xWorksStrings.ksUndoMacro, commandName);
			string redo = string.Format(xWorksStrings.ksRedoMacro, commandName);
			UndoableUnitOfWorkHelper.Do(undo, redo, obj.Cache.ActionHandlerAccessor,
				() => macro.RunMacro(obj, flid, ws, start, length));
			return true;
		}

		internal bool SafeToDoMacro(IVwSelection sel, out ICmObject obj, out int flid, out int ws, out int start, out int length)
		{
			start = flid = ws = length = 0; // defaults so we can return early.
			obj = null;
			if (sel == null || !(sel.SelType == VwSelType.kstText))
				return false;
			ITsString dummy;
			int hvoA, hvoE, flidE, ichA, ichE, wsE;
			bool fAssocPrev;
			sel.TextSelInfo(false, out dummy, out ichA, out fAssocPrev, out hvoA, out flid, out ws);
			sel.TextSelInfo(true, out dummy, out ichE, out fAssocPrev, out hvoE, out flidE, out wsE);
			// for safety require selection to be in a single property.
			if (hvoA != hvoE || flid != flidE || ws != wsE)
				return false;
			var cache = m_propertyTable.GetValue<LcmCache>("cache");
			obj = cache.ServiceLocator.ObjectRepository.GetObject(hvoA);
			start = Math.Min(ichA, ichE);
			length = Math.Max(ichA, ichE) - start;
			return true;
		}

		private IFlexMacro GetMacro(object commandObject)
		{
			var command = (Command) commandObject;
			var paramNode = XmlUtils.GetFirstNonCommentChild(command.ConfigurationNode);
			if (paramNode == null)
				throw new ArgumentException("macro configuration must have params node");
			int index = XmlUtils.GetMandatoryIntegerAttributeValue(paramNode, "key") - 2;
			if (index < 0 || index >= MacroCount)
				throw new ArgumentException("macro configuration must specify a key between 2 and " + (MacroCount + 1));
			var macro = Macros[index];
			return macro;
		}

		/// <summary>
		/// Invoked by reflection when displaying the appropriate menu item. Which one is indicated by the paramters node.
		/// This method is responsible to decide whether to display the command, whether to enable it, and what the text of the menu
		/// item should be.
		/// </summary>
		/// <param name="commandObject"></param>
		/// <param name="display"></param>
		/// <returns>true (always) to indicate that we handled this command</returns>
		public bool OnDisplayMacro(object commandObject, ref UIItemDisplayProperties display)
		{
			return DoDisplayMacro(commandObject, display, GetSelection());
		}

		// The body of OnDisplayMacro is isolated so we don't have to fake all the objects needed to get the (typically mock) selection in testing.
		internal bool DoDisplayMacro(object commandObject, UIItemDisplayProperties display, IVwSelection sel)
		{
			var macro = GetMacro(commandObject);
			int ichA, hvoA, flid, ws, ichE, start, length;
			ICmObject obj;
			if (macro == null)
			{
				display.Enabled = display.Visible = false;
				return true;
			}
			display.Visible = true;
			display.Text = macro.CommandName;
			if (!SafeToDoMacro(sel, out obj, out flid, out ws, out start, out length))
			{
				display.Enabled = false;
				return true;
			}
			display.Enabled = macro.Enabled(obj, flid, ws, start, length);

			return true;
		}
	}
}
