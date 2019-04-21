// SilSidePane, Copyright 2008-2016 SIL International. All rights reserved.
// SilSidePane is licensed under the Code Project Open License (CPOL), <http://www.codeproject.com/info/cpol10.aspx>.
// Derived from OutlookBar v2 2005 <http://www.codeproject.com/KB/vb/OutlookBar.aspx>, Copyright 2007 by Star Vega.
// Changed in 2008 and 2009 by SIL International to convert to C# and add more functionality.

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Windows.Forms;
using SIL.LCModel.Utils;

namespace SIL.SilSidePane
{
	/// <summary>
	/// Item area in a sidepane which uses a list of small icons next to text labels.
	/// </summary>
	internal class ListViewItemArea : ListView, IItemArea
	{
		private List<Item> _items;
		private Item _currentItem;
		private ImageList _smallImageList;
		private ImageList _largeImageList;

		public ListViewItemArea()
		{
			_items = new List<Item>();

			_smallImageList = new ImageList();
			_largeImageList = new ImageList();

			base.ItemSelectionChanged += HandleWidgetSelectionChanged;
			base.View = View.List;
			base.Name = "sidepane_listview";
			base.Dock = DockStyle.Fill;
			base.MultiSelect = false;
			base.Tag = null;
			base.SmallImageList = _smallImageList;
			base.LargeImageList = _largeImageList;
			base.HideSelection = false;
			if (MiscUtils.IsUnix)
				base.LabelWrap = false;		// Fix FWNX-739 as best we can (no ellipsis when trimming like in Windows).
		}

		protected override void Dispose(bool disposing)
		{
			Debug.WriteLineIf(!disposing, "******* Missing Dispose() call for " + GetType() + ". *******");
			base.Dispose(disposing);
		}

		#region IItemArea members
		public event ItemAreaItemClickedEventHandler ItemClicked;

		public void Add(Item item)
		{
			var widget = new ListViewItem
				{
					Name = item.Name,
					Text = item.Text,
					Tag = item,
				};

			if (item.Icon != null)
			{
				_smallImageList.Images.Add(item.Icon);
				_largeImageList.Images.Add(item.Icon);
				// Set widget icon to the one we just added
				widget.ImageIndex = _smallImageList.Images.Count - 1;
			}

			item.UnderlyingWidget = widget;

			base.Items.Add(widget);
			_items.Add(item);
		}

		public new List<Item> Items
		{
			get { return _items; }
		}

		public Item CurrentItem
		{
			get { return _currentItem; }
		}

		public void SelectItem(Item item)
		{
			var widget = item.UnderlyingWidget as ListViewItem;
			Debug.Assert(widget != null, "item.UnderlyingWidget as ListViewItem is null");
			if (widget == null)
				return;

			widget.Selected = true;
		}

		public Control AsControl()
		{
			return this;
		}
		#endregion IItemArea members

		/// <summary>
		/// Handles when the selection of which widget (ListViewItem in the ListView)
		/// in this item area is changed.
		/// </summary>
		void HandleWidgetSelectionChanged(object sender, ListViewItemSelectionChangedEventArgs e)
		{
			if (e == null)
				return;

			// Event fires twice when switching from one item to another. Only handle second event.
			if (!e.IsSelected)
				return;

			var widget = e.Item;
			if (widget == null)
				return;

			var item = widget.Tag as Item;

			_currentItem = item;
			if (!_clicked)
				ItemClicked(item);
		}

		private bool _clicked;

		protected override void OnMouseDown(MouseEventArgs e)
		{
			base.OnMouseDown(e);
			_clicked = true;
		}

		protected override void OnMouseUp(MouseEventArgs e)
		{
			base.OnMouseUp(e);
			if (_clicked)
			{
				ItemClicked(_currentItem);
				_clicked = false;
			}
		}

		protected override void OnMouseLeave(System.EventArgs e)
		{
			base.OnMouseLeave(e);
			if (_clicked)
			{
				ItemClicked(_currentItem);
				_clicked = false;
			}
		}

		#region Overrides of ListView

		/// <param name="e">An <see cref="T:System.EventArgs"/> that contains the event data. </param>
		protected override void OnSizeChanged(EventArgs e)
		{
			base.OnSizeChanged(e);

			// This fixes LT-583 (in part)
			if (SelectedItems.Count > 0)
				SelectedItems[0].EnsureVisible();
			else if (Items.Count > 0)
				EnsureVisible(0);

			// This is a really bad hack, but it seems to be the only way that I know of to get around
			// this bug in XP. EnsureVisible doesn't seem to work when we get in to this weird state
			// in XP, so we go ahead and rebuild the entire list.
			if (TopItem != null || base.Items.Count < 1)
				return;

			ListViewItem selected = null;
			if (SelectedItems.Count > 0)
				selected = SelectedItems[0];
			var items = new ListViewItem[Items.Count];
			for (var i = 0; i < base.Items.Count; i++)
				items[i] = base.Items[i];
			base.Items.Clear();
			base.Items.AddRange(items);
			if (selected == null)
				return;
			selected.Selected = true;
			selected.EnsureVisible();
		}

		#endregion
	}
}
