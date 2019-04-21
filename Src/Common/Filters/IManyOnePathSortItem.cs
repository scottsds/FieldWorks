﻿// Copyright (c) 2015 SIL International
// This software is licensed under the LGPL, version 2.1 or later
// (http://www.gnu.org/licenses/lgpl-2.1.html)

using SIL.LCModel;

namespace SIL.FieldWorks.Filters
{
	/// <summary>
	/// A ManyOnePathSortItem stores the information we need to work with an item in a browse view.
	/// This includes the ID of the item, and a path indicating how we got from one of the
	/// root items for the browse view to the item.
	/// This path is empty when sorting by columns containing simple (or very complex)
	/// properties of the original objects, but may be more complex when sorting by columns
	/// containing related objects, especially ones in many:1 relation with the original.
	/// </summary>
	public interface IManyOnePathSortItem
	{
		ICmObject RootObjectUsing(LcmCache cache);
		int RootObjectHvo { get; }
		int KeyObject { get; }
		ICmObject KeyObjectUsing(LcmCache cache);
		int PathLength { get; }
		int PathObject(int index);
		int PathFlid(int index);
		// This is in the interface only for internal use.
		string PersistData(ICmObjectRepository repo);
	}
}
