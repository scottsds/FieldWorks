// Copyright (c) 2015 SIL International
// This software is licensed under the LGPL, version 2.1 or later
// (http://www.gnu.org/licenses/lgpl-2.1.html)

using System;
using System.Collections.Generic;
using System.Xml;
using SIL.LCModel.Core.Cellar;
using SIL.LCModel.Core.Text;
using SIL.LCModel.Core.KernelInterfaces;
using SIL.LCModel.Application;
using SIL.LCModel.Infrastructure;
using SIL.Utils;
using HvoFlidKey = SIL.LCModel.Application.HvoFlidKey;
using HvoFlidWSKey = SIL.LCModel.Application.HvoFlidWSKey;

namespace SIL.FieldWorks.Common.Controls
{
	/// <summary>
	/// A 'Decorator' class for the FDO ISilDataAccess implementation.
	/// This class allows for caching the 'fake' flids used in a browse view
	/// (i.e., flides used for the check box and selection, etc.).
	/// </summary>
	public class XMLViewsDataCache : DomainDataByFlidDecoratorBase
	{
		// Fake flids for this cache should be in the range 90000000 to 99999999.
		// NB: For any fke flid added and used, be sure the override methods are updated.
		// For instance, the "get_IsPropInCache" method needs to knwo about all of them,
		// but other methods can be selective, based on the type of data is ion the flid.

		/// <summary>
		/// This item controls the status of the check box in the bulk edit view, and thus, whether an item is included
		/// in any bulk edit operations. HOWEVER, we don't cache this value for all objects in the list. The VC has a default
		/// state, DefaultChecked, which controls whether an item is selected if it has no value cached for ktagItemSelected.
		/// The value of this property should therefore always be accessed through BrowseViewer.GetCurrentCheckedValue().
		/// The one exception is when the check box is actually displayed by the VC; this uses the simple value in the cache,
		/// which is OK because a line in LoadDataFor makes sure there is a value cached for any item that is visible.
		/// </summary>
		public const int ktagItemSelected = 90000000;

		/// <summary>
		/// This is used to store a flag indicating whether a preview should be shown for a given root object if it is selected:
		/// that is, it is true if bulk edit is allowed to change this item and the current settings will actually change it.
		/// It is not used in multi-column previews, since currently in that mode we may presume that a preview should be shown
		/// if a non-empty alternate value is stored.
		/// </summary>
		public const int ktagItemEnabled = 90000001;
		// if ktagActiveColumn has a value for m_hvoRoot, then if the check box is on
		// for a given row, the indicated cell is highlighted with a pale blue
		// background, and the ktagAlternateValue property is displayed for those cells.
		// So that no action is required for default behavior, ktagActiveColumn uses
		// 1-based indexing, so zero means no active column.
		internal const int ktagActiveColumn = 90000002;
		/// <summary>
		///This is the tag that is used to store and retrieve the value of the property being bulk edited that should be shown
		/// in the Preview, when not doing a multi-column preview.
		/// </summary>
		public const int ktagAlternateValue = 90000003;
		internal const int ktagTagMe = 90000004;

		// This group support Rapid Data Entry views (XmlBrowseRDEView).
		internal const int ktagEditColumnBase = 91000000;
		internal const int ktagEditColumnLim = 91000100;  // arbitrary max

		/// <summary>
		/// Used for multi column preview such as in assigning phonological features to phonemes.
		/// A preview may be stored for each column using ktagAlternateValueMultiBase + column index.
		/// Don't use values between this and the limit for other things.
		/// </summary>
		public const int ktagAlternateValueMultiBase = 92000000;
		internal const int ktagAlternateValueMultiBaseLim = 92000100;

		// Stores override values (when value is different from DefaultSelected) for ktagItemSelected.
		private readonly Dictionary<int, int> m_selectedCache;

		private readonly Dictionary<HvoFlidKey, int> m_integerCache = new Dictionary<HvoFlidKey, int>();
		private readonly Dictionary<HvoFlidWSKey, ITsString> m_mlStringCache = new Dictionary<HvoFlidWSKey, ITsString>();
		private readonly Dictionary<HvoFlidKey, ITsString> m_stringCache = new Dictionary<HvoFlidKey, ITsString>();

		/// <summary>
		/// This virtual flid needs special treatment, as we need to maintain any separators
		/// (semicolons) typed by the user, but also need to feed the string through to the
		/// virtual property handler for storing/creating new reversal index entries.
		/// See FWR-376.
		/// </summary>
		internal int m_tagReversalEntriesBulkText;

		/// <summary>
		/// The main constructor.
		/// </summary>
		public XMLViewsDataCache(ISilDataAccessManaged domainDataByFlid, bool defaultSelected, Dictionary<int, int> selectedItems)
			: base(domainDataByFlid)
		{
			DefaultSelected = defaultSelected;
			m_selectedCache = selectedItems;
			SetOverrideMdc(new XmlViewsMdc(domainDataByFlid.MetaDataCache as IFwMetaDataCacheManaged));
			m_tagReversalEntriesBulkText = domainDataByFlid.MetaDataCache.GetFieldId("LexSense", "ReversalEntriesBulkText", false);
		}

		/// <summary>
		/// Simpler constructor supplies default args
		/// </summary>
		internal XMLViewsDataCache(ISilDataAccessManaged domainDataByFlid, XmlNode nodeSpec)
			: this(domainDataByFlid, XmlUtils.GetOptionalBooleanAttributeValue(nodeSpec, "defaultChecked", true),
			new Dictionary<int, int>())
		{
		}

		/// <summary>
		/// True if ktagSelected should return 1 for items not previously set; false to return 0.
		/// </summary>
		internal bool DefaultSelected { get; set; }

		// The cache that controls ktagSelected along with DefaultSelected. Should only be used to save
		// for creating a new instance later.
		internal Dictionary<int, int> SelectedCache { get { return m_selectedCache; } }

		/// <summary>
		/// Override to support fake integer properties.
		/// </summary>
		/// <param name="hvo"></param>
		/// <param name="tag"></param>
		/// <returns></returns>
		public override int get_IntProp(int hvo, int tag)
		{
			switch (tag)
			{
				default:
					return base.get_IntProp(hvo, tag);
				case ktagActiveColumn: // Fall through
				case ktagItemEnabled: // Fall through
					int result;
					if (m_integerCache.TryGetValue(new HvoFlidKey(hvo,  tag), out result))
						return result;
					return 0;
				case ktagItemSelected:
					return GetItemSelectedValue(hvo);
			}
		}

		private int GetItemSelectedValue(int hvo)
		{
			int sel;
			if (m_selectedCache.TryGetValue(hvo, out sel))
				return sel;
			return DefaultSelected ? 1 : 0;
		}

		/// <summary>
		/// Override to work with fake flids.
		/// </summary>
		/// <param name="hvo"></param>
		/// <param name="tag"></param>
		/// <returns></returns>
		public override int get_ObjectProp(int hvo, int tag)
		{
			switch (tag)
			{
				default:
					return base.get_ObjectProp(hvo, tag);
				case ktagTagMe:
					return hvo;
			}
		}

		/// <summary>
		/// Override to work with fake flid.
		/// </summary>
		/// <param name="hvo"></param>
		/// <param name="tag"></param>
		/// <returns></returns>
		public override int get_VecSize(int hvo, int tag)
		{
			return tag == ktagTagMe ? -1 : base.get_VecSize(hvo, tag);
		}

		/// <summary>
		/// Override to support fake integer properties.
		/// </summary>
		/// <param name="hvo"></param>
		/// <param name="tag"></param>
		/// <param name="n"></param>
		public override void SetInt(int hvo, int tag, int n)
		{
			switch (tag)
			{
				default:
					base.SetInt(hvo, tag, n);
					break;
				case ktagActiveColumn:
				case ktagItemEnabled:
					{
						var key = new HvoFlidKey(hvo,  tag);
						int oldVal;
						if (m_integerCache.TryGetValue(key, out oldVal) && oldVal == n)
							return; // unchanged, avoid especially PropChanged.
						m_integerCache[key] = n;
						SendPropChanged(hvo, tag, 0, 0, 0);
					}
					break;
				case ktagItemSelected:
					{
						if (GetItemSelectedValue(hvo) == n)
							return; // unchanged, avoid especially PropChanged.
						m_selectedCache[hvo] = n;
						SendPropChanged(hvo, tag, 0, 0, 0);
					}
					break;
			}
		}

		/// <summary>
		/// Override to support some fake flids.
		/// </summary>
		public override bool get_IsPropInCache(int hvo, int tag, int cpt, int ws)
		{
			switch (tag)
			{
				default:
					if (tag == m_tagReversalEntriesBulkText &&
						m_mlStringCache.ContainsKey(new HvoFlidWSKey(hvo, tag, ws)))
					{
							return true;
					}
					if (tag >= ktagAlternateValueMultiBase && tag < ktagAlternateValueMultiBaseLim)
						return m_stringCache.ContainsKey(new HvoFlidKey(hvo, tag));
					return base.get_IsPropInCache(hvo, tag, cpt, ws);
				case ktagTagMe:
					return true; // hvo can always be itself.
				case ktagAlternateValue:
					return m_stringCache.ContainsKey(new HvoFlidKey(hvo,  tag));
				case ktagActiveColumn: // Fall through
				case ktagItemEnabled: // Fall through
					return m_integerCache.ContainsKey(new HvoFlidKey(hvo, tag));
				case ktagItemSelected:
					return true; // we have a default, this is effectively always in the cache.
			}
		}

		/// <summary>
		/// We understand how to get a multistring for any hvo in the tagEditColumn range.
		/// We also handle the virtual property LexEntry.ReversalEntriesBulkText in a
		/// special way.  (See FWR-376.)
		/// </summary>
		public override ITsString get_MultiStringAlt(int hvo, int tag, int ws)
		{
			ITsString result1 = null;
			if (tag < ktagEditColumnBase || tag > ktagEditColumnLim)
			{
				result1 = base.get_MultiStringAlt(hvo, tag, ws);
				if (tag != m_tagReversalEntriesBulkText)
					return result1;
			}
			ITsString result;
			if (m_mlStringCache.TryGetValue(new HvoFlidWSKey(hvo, tag, ws), out result))
				return result;
			if (tag == m_tagReversalEntriesBulkText && result1 != null)
				return result1;
			return TsStringUtils.EmptyString(ws);
		}

		/// <summary>
		/// Store a value, without generating PropChanged calls.
		/// </summary>
		public void CacheMultiString(int hvo, int tag, int ws, ITsString val)
		{
			m_mlStringCache[new HvoFlidWSKey(hvo, tag, ws)] = val;
		}

		/// <summary>
		/// Override to handle props in the tagEditColumn range.  We also handle the virtual
		/// property LexEntry.ReversalEntriesBulkText in a special way.  (See FWR-376.)
		/// </summary>
		public override void SetMultiStringAlt(int hvo, int tag, int ws, ITsString tss)
		{
			if (tag < ktagEditColumnBase || tag > ktagEditColumnLim)
			{
				base.SetMultiStringAlt(hvo, tag, ws, tss);
				// Keep a local copy.
				if (tag == m_tagReversalEntriesBulkText)
					CacheMultiString(hvo, tag, ws, tss);
				return;
			}
			CacheMultiString(hvo, tag, ws, tss);
			SendPropChanged(hvo, tag, ws, 0, 0);
		}

		/// <summary>
		/// Override to handle ktagAlternateValue.
		/// </summary>
		public override ITsString get_StringProp(int hvo, int tag)
		{
			if ((tag == ktagAlternateValue) || (tag >= ktagAlternateValueMultiBase && tag < ktagAlternateValueMultiBaseLim))
			{
				ITsString result;
				if (m_stringCache.TryGetValue(new HvoFlidKey(hvo, tag), out result))
					return result;
				// Try to find a sensible WS from existing data, avoiding a crash if possible.
				// See FWR-3598.
				ITsString tss = null;
				foreach (var x in m_stringCache.Keys)
				{
					tss = m_stringCache[x];
					if (x.Flid == tag)
						break;
				}
				if (tss == null)
				{
					foreach (HvoFlidWSKey x in m_mlStringCache.Keys)
					{
						return TsStringUtils.EmptyString(x.Ws);
					}
				}
				if (tss != null)
				{
					var ws = TsStringUtils.GetWsOfRun(tss, 0);
					return TsStringUtils.EmptyString(ws);
				}
				// Enhance JohnT: might be desirable to return empty string rather than crashing,
				// but as things stand, we don't know what would be a sensible WS.
				throw new InvalidOperationException("trying to read a preview value not previously cached");
			}
			return base.get_StringProp(hvo, tag);
		}

		/// <summary>
		/// Override to handle ktagAlternateValue.
		/// </summary>
		public override void SetString(int hvo, int tag, ITsString _tss)
		{
			if ((tag == ktagAlternateValue) || (tag >= ktagAlternateValueMultiBase && tag < ktagAlternateValueMultiBaseLim))
			{
				int oldLen = 0;
				ITsString oldVal;
				if (m_stringCache.TryGetValue(new HvoFlidKey(hvo, tag), out oldVal))
					oldLen = oldVal.Length;
				m_stringCache[new HvoFlidKey(hvo, tag)] = _tss;
				SendPropChanged(hvo, tag, 0, _tss.Length, oldLen);
				return;
			}
			base.SetString(hvo, tag, _tss);
		}

		/// <summary>
		/// Remove any ktagAlternateValueMultiBase values for this hvo
		/// </summary>
		/// <param name="hvo"></param>
		/// <param name="tag"></param>
		public void RemoveMultiBaseStrings(int hvo, int tag)
		{
			m_stringCache.Remove(new HvoFlidKey(hvo, tag));
		}

	}

	class XmlViewsMdc : LcmMetaDataCacheDecoratorBase
	{
		public XmlViewsMdc(IFwMetaDataCacheManaged metaDataCache) : base(metaDataCache)
		{
		}

		public override void AddVirtualProp(string bstrClass, string bstrField, int luFlid, int type)
		{
			throw new NotImplementedException();
		}

		// So far, this is the only query that needs to know about the virtual props.
		// It may not even need to know about all of these.
		public override string GetFieldName(int flid)
		{
			switch (flid)
			{
				case XMLViewsDataCache.ktagTagMe: return "Me";
				case XMLViewsDataCache.ktagActiveColumn: return "ActiveColumn";
				case XMLViewsDataCache.ktagAlternateValue: return "AlternateValue";
				case XMLViewsDataCache.ktagItemEnabled: return "ItemEnabled";
				case XMLViewsDataCache.ktagItemSelected: return "ItemSelected";
			}
			// Paste operations currently require the column to have some name.
			if (flid >= XMLViewsDataCache.ktagEditColumnBase && flid < XMLViewsDataCache.ktagEditColumnLim)
				return "RdeColumn" + (flid - XMLViewsDataCache.ktagEditColumnBase);
			if (flid >= XMLViewsDataCache.ktagAlternateValueMultiBase && flid < XMLViewsDataCache.ktagAlternateValueMultiBaseLim)
				return "PhonFeatColumn" + (flid - XMLViewsDataCache.ktagAlternateValueMultiBase);
			return base.GetFieldName(flid);
		}

		public override int GetFieldType(int luFlid)
		{
			// This is a bit arbitrary. Technically, the form column isn't formattable, while the one shadowing
			// Definition could be. But pretending all are Unicode just means Collect Words can't do formatting
			// of definitions, while allowing it in the Form could lead to crashes when we copy to the real field.
			if (luFlid >= XMLViewsDataCache.ktagEditColumnBase && luFlid < XMLViewsDataCache.ktagEditColumnLim)
				return (int)CellarPropertyType.Unicode;
			return base.GetFieldType(luFlid);
		}
	}
}
