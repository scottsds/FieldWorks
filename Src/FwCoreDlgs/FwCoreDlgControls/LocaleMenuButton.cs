// Copyright (c) 2015-2017 SIL International
// This software is licensed under the LGPL, version 2.1 or later
// (http://www.gnu.org/licenses/lgpl-2.1.html)

using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.Drawing;
using System.Linq;
using System.Windows.Forms;
using Icu;
using SIL.LCModel.Core.Text;
using SIL.FieldWorks.Common.FwUtils;
using SIL.FieldWorks.Resources;
using SIL.LCModel.Utils;

namespace SIL.FieldWorks.FwCoreDlgControls
{
		/// <summary>
	/// Summary description for LocaleMenuButton.
	/// </summary>
	public class LocaleMenuButton : Button
	{
		private System.ComponentModel.Container components;

		// Key - language ID.
		// Value - collection of locales starting with that language id.
		private Dictionary<string, List<LocaleMenuItemData>> m_locales;

		// Items are LocaleMenuItemData; an entry is added for each mainmenu
		// (name and ID are taken from the unmarked locale for that language);
		// an entry is also added for each locale where there is no unmarked
		// locale for its language.
		private List<LocaleMenuItemData> m_mainItems;

		// Key - MenuItem
		// Value - LocaleMenuItemData
		private Dictionary<ToolStripMenuItem, LocaleMenuItemData> m_itemData;

		private string m_selectedLocale;
		// ID of locale to use for getting display names.
		private string m_displayLocaleId;

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Initializes a new instance of the <see cref="LocaleMenuButton"/> class.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		public LocaleMenuButton()
		{
			components = new Container();
			Image = ResourceHelper.ButtonMenuArrowIcon;
			ImageAlign = ContentAlignment.MiddleRight;
		}

		/// <summary>
		/// Check to see if the object has been disposed.
		/// All public Properties and Methods should call this
		/// before doing anything else.
		/// </summary>
		public void CheckDisposed()
		{
			if (IsDisposed)
				throw new ObjectDisposedException(String.Format("'{0}' in use after being disposed.", GetType().Name));
		}

		/// <summary>
		/// Clean up any resources being used.
		/// </summary>
		protected override void Dispose(bool disposing)
		{
			Debug.WriteLineIf(!disposing, "****** Missing Dispose() call for " + GetType() + ". ****** ");
			// Must not be run more than once.
			if (IsDisposed)
				return;

			if (disposing)
			{
				if (components != null)
					components.Dispose();
			}
			base.Dispose(disposing);
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Gets or sets the selected locale id.
		/// </summary>
		/// <value>The selected locale id.</value>
		/// ------------------------------------------------------------------------------------
		public string SelectedLocaleId
		{
			get
			{
				CheckDisposed();
				return m_selectedLocale;
			}
			set
			{
				CheckDisposed();

				// This gets called during initialization with null.
				if (value == null)
					return; // Probably initialization.

				string displayName;
				if (TryGetDisplayName(value, out displayName))
				{
					Text = displayName;
					m_selectedLocale = value;
				}
				else
				{
					// Client should make sure it is a valid id. Failing this, make a warning.
					Text = FwCoreDlgControls.kstidError;
				}
			}
		}

		private bool TryGetDisplayName(string locale, out string displayName)
		{
			try
			{
				displayName = new Locale().GetDisplayName(locale);
				return true;
			}
			catch
			{
				displayName = string.Empty;
				return false;
			}
		}

		/// <summary>
		/// Determine whether the specified locale is a custom one the user is allowed to modify.
		/// </summary>
		/// <param name="localeId"></param>
		/// <returns></returns>
		public bool IsCustomLocale(string localeId)
		{
			using (var rbroot = new ResourceBundle(null, "en"))
			using (var rbCustom = rbroot["Custom"])
			using (var rbCustomLocales = rbCustom["LocalesAdded"])
			{
				if (rbCustomLocales.GetStringContents().Contains(localeId))
					return true;
			}

			// Next, check if ICU knows about this locale. If ICU doesn't know about it, it is considered custom.
			string displayNameIgnored;
			if (TryGetDisplayName(localeId, out displayNameIgnored))
				return false;
			return true;
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// We are going to be used to choose a 'similar locale' for the specified
		/// locale. If this is a standard, built-in locale, disable the button and
		/// set its text to 'built-in'. If it's a custom locale, enable the button,
		/// and if the text used to be 'built-in' (that is, the previous target locale
		/// was built-in), change it to 'None'.
		/// </summary>
		/// <param name="localeName">Name of the locale.</param>
		/// ------------------------------------------------------------------------------------
		public void SetupForSimilarLocale(string localeName)
		{
			CheckDisposed();

			if (IsCustomLocale(localeName))
			{
				// Since it's a custom locale the user is allowed to control it.
				// If it used to say "built in" change it to "none", otherwise,
				// they are changing from one custom locale to another, leave the
				// current selection alone.
				if (Text == FwCoreDlgControls.kstidBuiltIn)
					Text = FwCoreDlgControls.kstid_None;
				Enabled = true;
			}
			else
			{
				Text = FwCoreDlgControls.kstidBuiltIn;
				Enabled = false;
			}
		}

		/// <summary>
		/// The locale to use for getting display names. If this is left blank the
		/// system defaut locale is used.
		/// </summary>
		public string DisplayLocaleId
		{
			get
			{
				CheckDisposed();
				return m_displayLocaleId;
			}
			set
			{
				CheckDisposed();
				m_displayLocaleId = value;
			}
		}

		/// <summary>Event that occurs when the user chooses a locale.</summary>
		public event EventHandler LocaleSelected;

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Handle the LocaleSelected event; by default just calls delegates.
		/// </summary>
		/// <param name="ea">The <see cref="T:System.EventArgs"/> instance containing the event data.</param>
		/// ------------------------------------------------------------------------------------
		protected virtual void OnLocaleSelected(EventArgs ea)
		{
			if (LocaleSelected != null)
				LocaleSelected(this, ea);
		}

		/// <summary>
		/// Get a dictionary, keyed by language ID, with values being a list of locale IDs and names.
		/// This allows "en-GB" and "en-US" to be grouped together under "English" in a menu, for example.
		/// </summary>
		/// <param name="displayLocale">Locale ID in which to display the locale names (e.g., if this is "fr", then the "en" locale will have the display name "anglais").</param>
		/// <returns>A dictionary whose keys are language IDs (2- or 3-letter ISO codes) and whose values are a list of the IcuIdAndName objects that GetLocaleIdsAndNames returns.</returns>
		private static IDictionary<string, IList<IcuIdAndName>> GetLocalesByLanguage(
			string displayLocale = null)
		{
			var result = new Dictionary<string, IList<IcuIdAndName>>();
			foreach (var locale in Locale.AvailableLocales)
			{
				var name = locale.GetDisplayName(displayLocale);
				IList<IcuIdAndName> entries;
				if (!result.TryGetValue(locale.Language, out entries))
				{
					entries = new List<IcuIdAndName>();
					result[locale.Language] = entries;
				}
				entries.Add(new IcuIdAndName(locale.Id, name));
			}
			return result;
		}


		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Raises the click event.
		/// </summary>
		/// <param name="e">The <see cref="T:System.EventArgs"/> instance containing the event data.</param>
		/// ------------------------------------------------------------------------------------
		protected override void OnClick(EventArgs e)
		{
			var menu = components.ContextMenuStrip("contextMenu");

			// Create the various collections we use in the process of assembling
			// the menu.
			m_locales = new Dictionary<string, List<LocaleMenuItemData>>();
			m_mainItems = new List<LocaleMenuItemData>();
			m_itemData = new Dictionary<ToolStripMenuItem, LocaleMenuItemData>();

			var localeDataByLanguage = GetLocalesByLanguage(m_displayLocaleId);
			foreach (var localeData in localeDataByLanguage)
			{
				string langid = localeData.Key;
				var items = localeData.Value;
				m_locales[langid] = items.Select(idAndName => new LocaleMenuItemData(idAndName.Id, idAndName.Name)).ToList();
			}

			// Generate the secondary items. For each key in m_locales,
			// 1. If there is just one item in the List and its id is equal to the key,
			//		then this language has only one locale,
			//		the unmodified one that appears in the main list.
			// 2. If there is more than one item and one of them has an id equal to the key,
			//		make a single item in main list. It is a copy of the item whose id is
			//		equal to the key, that is, the most basic locale for this language.
			//		It has the arraylist for mainItems.
			// 3. Otherwise, we have a list of locales for which there is no basic locale
			//		whose id is equal to the language id. In this case, we make an item
			//		for each thing in the List, whether many or just one.
			foreach (KeyValuePair<string, List<LocaleMenuItemData>> kvp in m_locales)
			{
				string langid = kvp.Key;
				List<LocaleMenuItemData> items = kvp.Value;
				LocaleMenuItemData lmdRootItem = items.FirstOrDefault(lmd => lmd.m_id == langid);
				// See if there is an item in the array list that matches the langid
				if (lmdRootItem == null)
				{
					// case 3
					foreach(LocaleMenuItemData lmd in items)
					{
						m_mainItems.Add(lmd);
					}
				}
				else
				{
					if (items.Count == 1)
					{
						// case 1
						var lmdMenu = new LocaleMenuItemData(lmdRootItem.m_id,lmdRootItem.m_displayName);
						m_mainItems.Add(lmdMenu);
					}
					else
					{
						// case 2
						var lmdMenu = new LocaleMenuItemData(lmdRootItem.m_id,lmdRootItem.m_displayName) {m_subitems = items};
						m_mainItems.Add(lmdMenu);
					}
				}
			}

			// Sort the items in each menu.
			m_mainItems.Sort();
			var NoneData = new LocaleMenuItemData(FwCoreDlgControls.kstid_None, FwCoreDlgControls.kstid_None);
			var NoneItem = new ToolStripMenuItem(NoneData.m_displayName, null, ItemClickHandler);
			menu.Items.Add(NoneItem); // This goes strictly at the beginning, irrespective of sorting

			foreach (LocaleMenuItemData lmd in m_mainItems)
			{
				var mi = new ToolStripMenuItem(lmd.m_displayName, null, ItemClickHandler);
				menu.Items.Add(mi);
				m_itemData[mi] = lmd;
				if (lmd.m_subitems != null)
				{
					mi.DropDownOpened += mi_Popup;
					lmd.m_subitems.Sort();
					// To make the system realize this item is a submenu, we have to
					// add at least one item. To save time and space, we don't add the others
					// until it pops up.
					LocaleMenuItemData lmdSub = lmd.m_subitems[0];
					var miSub = new ToolStripMenuItem(lmdSub.m_displayName, null, ItemClickHandler);
					mi.DropDownItems.Add(miSub);
					m_itemData[miSub] = lmdSub;

					// Turns out popup events don't happen in a .NET submenu, only for the top-level
					// menu. DavidO has a workaround that should soon be checked in. In the meantime,
					// generate the submenu at once. This may actually be fast enough to keep permanently.
					mi_Popup(mi, new EventArgs());
				}
			}

			if (MiscUtils.IsUnix)
				menu.ShowWithOverflow(this, new Point(0, Height));
			else
				menu.Show(this, new Point(0, Height));

			base.OnClick(e); // Review JohnT: is this useful or harmful or neither? MS recommends it.
		}

		private void ItemClickHandler(Object sender, EventArgs e)
		{
			var mi = (ToolStripMenuItem) sender;
			if (mi.Text == FwCoreDlgControls.kstid_None)
			{
				m_selectedLocale = null;
				Text = FwCoreDlgControls.kstid_None;
				OnLocaleSelected(new EventArgs());
				return;
			}
			LocaleMenuItemData lmd = m_itemData[mi];
			m_selectedLocale = lmd.m_id;
			Text = lmd.m_displayName;
			OnLocaleSelected(new EventArgs());
		}

		private void mi_Popup(object sender, EventArgs e)
		{
			var miBase = sender as ToolStripMenuItem;
			Debug.Assert(miBase != null);
			if (miBase.DropDownItems.Count > 1)
				return; // already popped up, has items.
			LocaleMenuItemData lmd = m_itemData[miBase];
			LocaleMenuItemData lmdFirst = lmd.m_subitems[0];
			foreach (LocaleMenuItemData lmdSub in lmd.m_subitems)
			{
				// Skip the first item as it was added earlier.
				if (lmdSub == lmdFirst)
					continue;
				var miSub = new ToolStripMenuItem(lmdSub.m_displayName, null, ItemClickHandler);
				miBase.DropDownItems.Add(miSub);
				m_itemData[miSub] = lmdSub;
			}
		}
	}

	internal class LocaleMenuItemData : IComparable<LocaleMenuItemData>
	{
		internal LocaleMenuItemData(string id, string displayName)
		{
			m_id = id;
			m_displayName = displayName;
		}
		// Locale id, as returned by Locale.getName. For submenus, this is the id
		// of the language, which is also the id of the base locale.
		internal string m_id;
		internal string m_displayName; // corresponding display name, from Locale.getDisplayName.

		internal List<LocaleMenuItemData> m_subitems;

		public int CompareTo(LocaleMenuItemData obj)
		{
			Debug.Assert(obj != null);
			return m_displayName.CompareTo(obj.m_displayName);
		}
	}

	#region	OverFlowContextMenuStrip
	/// <summary>
	/// Extends ContextMenuStrip with ShowWithOverflow extension method so that menus greater than screen height
	/// have an overflow button.
	/// This is only needed on mono as .NET ContextMenuStrip implements scrolling.
	/// </summary>
	internal static class OverflowContextMenuStrip
	{
		/// <summary>
		/// version of show that places all controls that don't fit in an overflow submenu.
		/// </summary>
		public static void ShowWithOverflow(this ContextMenuStrip contextMenu, Control control, Point position)
		{
			int maxHeight = Screen.GetWorkingArea(control).Height;

			CalculateOverflow(contextMenu, contextMenu.Items, maxHeight);

			contextMenu.Show(control, position);
		}

		private static void CalculateOverflow(ContextMenuStrip contextMenu,
			ToolStripItemCollection items, int maxHeight)
		{
			int height = contextMenu.Padding.Top;
			contextMenu.PerformLayout();
			int totalItems = items.Count;
			int overflowIndex = 0;
			bool overflowNeeded = false;
			// only examine up to last but one item.
			for (; overflowIndex < totalItems - 2; ++overflowIndex)
			{
				ToolStripItem current = items[overflowIndex];
				ToolStripItem next = items[overflowIndex + 1];
				if (!current.Available)
					continue;

				height += GetTotalHeight(current);

				if (height + GetTotalHeight(next) + contextMenu.Padding.Bottom > maxHeight)
				{
					overflowNeeded = true;
					break;
				}
			}

			if (overflowNeeded)
			{
				// Don't dispose overflow here because that will prevent it from working.
				ToolStripMenuItem overflow = new ToolStripMenuItem(FwCoreDlgControls.kstid_More, Images.arrowright);
				overflow.ImageScaling = ToolStripItemImageScaling.None;
				overflow.ImageAlign = ContentAlignment.MiddleCenter;
				overflow.DisplayStyle = ToolStripItemDisplayStyle.ImageAndText;

				for (int i = totalItems - 1; i >= overflowIndex; i--)
				{
					ToolStripItem item = items[i];
					items.RemoveAt(i);
					overflow.DropDown.Items.Insert(0, item);
				}

				CalculateOverflow(contextMenu, overflow.DropDownItems, maxHeight);

				if (overflow.DropDown.Items.Count > 0)
					items.Add(overflow);
				else
					overflow.Dispose();
			}
		}

		internal static int GetTotalHeight(ToolStripItem item)
		{
			return item.Padding.Top + item.Margin.Top + item.Height + item.Margin.Bottom + item.Padding.Bottom;
		}
	}
	#endregion
}
