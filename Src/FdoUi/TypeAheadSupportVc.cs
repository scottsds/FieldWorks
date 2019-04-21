// Copyright (c) 2015 SIL International
// This software is licensed under the LGPL, version 2.1 or later
// (http://www.gnu.org/licenses/lgpl-2.1.html)

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Drawing;
using System.Windows.Forms;
using SIL.LCModel.Core.Cellar;
using SIL.LCModel.Core.Text;
using SIL.LCModel.Core.KernelInterfaces;
using SIL.LCModel;
using SIL.FieldWorks.Common.ViewsInterfaces;
using SIL.FieldWorks.Common.RootSites;
using SIL.LCModel.Utils;

namespace SIL.FieldWorks.FdoUi
{
	/// <summary>
	/// TypeAheadSupportVc is a view constructor used to display a (typically real) object reference property
	/// in a mode that allows the user to type part of an identifying string in order to make a selection.
	///
	/// Typical call code:
	///
	/// // Member variable of main VC, typically.
	/// TypeAheadSupportVc m_tasvc;
	///
	/// // (In constructor of main VC, or similar place.)
	/// m_tasvc = new TypeAheadSupportVc((int)LexSense.LexSenseTags.kflidSemanticDomain, m_cache);
	/// m_tasvc.InitXXX(...); // Optional, choose any appropriate method if further Init required.
	///
	/// // (As part of Display method or similar, where type-ahead property is wanted.
	/// m_tasvc.Insert(IVwEnv vwenv, int hvo); // atomic
	/// m_tasbc.InsertSeq(IVwEnv vwenv, int hvo); // sequence (or collection) -- not yet fully implemented? At least not tested.
	///
	/// To make things work properly, the client must also override OnKeyPress and (after checking that no
	/// update is in progress) arrange to call
	/// m_tasvc.OnKeyPress(EditingHelper, e, ModifierKeys, m_vwGraphics). If this returns true, the normal call
	/// to base.OnKeyPress should be omitted.
	///			protected override void OnKeyPress(KeyPressEventArgs e)
	///			{
	///				if (DataUpdateMonitor.IsUpdateInProgress(DataAccess))
	///					return; //throw this event away
	///				using (new HoldGraphics(this))
	///				{
	///					if (m_vc.TasVc.OnKeyPress(EditingHelper, e, ModifierKeys, m_vwGraphics))
	///						return;
	///				}
	///				base.OnKeyPress(e);
	///			}
	///
	/// The client should also override SelectionChanged and (among any other behavior)
	/// calle m_tasvc.SelectionChanged(rootb, sel). This is used to expand any selection that covers more than one
	/// item in the sequence to cover the whole of the items partly selected.
	///
	/// Finally, the client should override OnLoseFocus and (among any other behavior) call m_tasvc.LoseFocus(rootb);
	/// Similarly OnGotFocus(rootb).
	/// </summary>
	public class TypeAheadSupportVc : FwBaseVc
	{
		// Top-level fragment used in the Display method.
		public const int kfragName = 3039;

		internal const int kBaseFakeObj = 500000000; // Object ids greater than this are presumed fake.
		int m_tag; // The (typically real) property we're editing.
		int m_virtualTagObj = 0; // The virtual property corresponding to m_tag.
		int m_clid; // class that has property m_tag (and to which m_virtualTagObj is added).
		string m_className; // name of m_clid.
		string m_fieldName;
		readonly CellarPropertyType m_type; // the type (refAtomic, refSeq, or refColl) of m_tag
		ISilDataAccess m_sda;  // m_cache.DomainDataByFlid; use separate var in interests of minimising need for LcmCache.
		int m_taTagName = 0; // Virtual prop for type-ahead name property.
		int m_snTagName = 0; // Virtual prop for shortname property.
		// HVO of the object that has the type-ahead name. This is set by SelectionChanged when it decides that
		// type-ahead is relevant to the current selection, changed if typing selects a different object.
		int m_hvoTa;
		// Hvo of object that has m_hvoTa in prop m_virtualTagObj.
		private int m_hvoParent;
		int m_ihvoTa; // index of m_hvoTa in prop m_virtualTagObj of m_hvoParent, or -1 if atomic.
		// For performance reasons the default lookup caches the shortnames of the possible values for property m_tag of m_hvoParent.
		// This variable should be cleared to empty any time m_hvoParent is changed, unless it is known that the list
		// for this property does not depend on the particular parent.
		List<string> m_shortnames = new List<string>();
		bool m_fPossibilitiesDependOnObject = true;
		bool m_fInSelectionChanged = false;
		bool m_fGotFocus = false; // True if we have focus.

		public TypeAheadSupportVc(int tag, LcmCache cache)
		{
			m_tag = tag;
			IFwMetaDataCache mdc = cache.DomainDataByFlid.MetaDataCache;
			m_clid = mdc.GetOwnClsId(m_tag);
			m_className = mdc.GetClassName(m_clid);
			m_fieldName = mdc.GetFieldName(m_tag);
			m_type = (CellarPropertyType)(mdc.GetFieldType(m_tag) & (int)CellarPropertyTypeFilter.VirtualMask);
			m_sda = cache.DomainDataByFlid;
			Cache = cache;
		}

		/// <summary>
		/// Insert a display of the relevant property for the indicated object, which is atomic.
		/// </summary>
		/// <param name="vwenv"></param>
		/// <param name="hvo"></param>
		public void Insert(IVwEnv vwenv, int hvo)
		{
			vwenv.AddObjProp(FakePropTag, this, kfragName);
		}

		/// <summary>
		/// You may set this to false if the possibilities do not depend on which object
		/// we are doing type-ahead for (resulting in performance improvement).
		/// (Setting it again will also force reloading the list if it changes for some other reason.)
		/// </summary>
		public bool PossibilitiesDependOnObject
		{
			get { return m_fPossibilitiesDependOnObject; }
			set
			{
				m_fPossibilitiesDependOnObject = value;
				m_shortnames.Clear(); // usually redundant, but play safe.
			}
		}

		/// <summary>
		/// Insert a display of the relevant property for the indicated object, which is a sequence or collection.
		/// </summary>
		/// <param name="vwenv"></param>
		/// <param name="hvo"></param>
		public void InsertSeq(IVwEnv vwenv, int hvo)
		{
			vwenv.AddObjVecItems(FakePropTag, this, kfragName);
		}

		/// <summary>
		/// Receives a notification that the selection in the root box has changed.
		///
		/// </summary>
		/// <param name="rootb"></param>
		/// <param name="sel"></param>
		public void SelectionChanged(IVwRootBox rootb, IVwSelection sel)
		{
			if (m_fInSelectionChanged)
				return;
			try
			{
				m_fInSelectionChanged = true; // suppress recursive calls.
				int hvoParent;
				int ihvo;
				int hvoSel = SelectedObject(rootb, sel, out hvoParent, out ihvo);
				if (hvoSel == m_hvoTa && hvoParent == m_hvoParent)
					return;
				int hvoTaOld = m_hvoTa;
				m_hvoTa = hvoSel; // must change before PropChange converting old one back.
				if (hvoTaOld != 0)
				{
					// Convert current object back to normal name. We replace the object with itself (without actually changing anything)
					// to force a redisplay, with different results because it is no longer the current object.
					m_sda.PropChanged(null, (int)PropChangeType.kpctNotifyAll, m_hvoParent, m_virtualTagObj, m_ihvoTa, 1, 1);
				}
				if (hvoSel != 0)
				{
					ParentObject = hvoParent;
					m_ihvoTa = ihvo;
					// Ensure the initial tag name matches the real one.
					(m_sda as IVwCacheDa).CacheStringProp(hvoSel, m_taTagName, m_sda.get_StringProp(hvoSel, m_snTagName));
					SwitchTagAndFixSel(m_taTagName, rootb);
				}
				else
				{
					ParentObject = m_ihvoTa = 0;
				}
			}
			finally
			{
				m_fInSelectionChanged = false;
			}
		}

		/// <summary>
		/// Get/Set the parent object we are doing type-ahead for.
		/// </summary>
		protected int ParentObject
		{
			get { return m_hvoParent; }
			set
			{
				if (m_hvoParent == value)
					return;
				m_hvoParent = value;
				if (m_fPossibilitiesDependOnObject)
					m_shortnames.Clear();
			}
		}

		// Regenerate the display of object m_ihvo of property m_virtualTagObj of object m_hvoParent,
		// assuming that the current selection is within it and that it will be displayed using
		// newTagName.
		private void SwitchTagAndFixSel(int newTagName, IVwRootBox rootb)
		{
			IVwSelection sel = rootb.Selection;
			int cvsli = 0;
			// Get selection information to determine where the user is typing.
			int ihvoRoot = 0;
			int tagTextProp = 0;
			int cpropPrevious = 0;
			int ichAnchor = 0;
			int ichEnd = 0;
			int ihvoEnd = 0;
			int ws = 0;
			ITsTextProps ttp = null;
			bool fAssocPrev = false;
			SelLevInfo[] rgvsli = null;
			if (sel != null)
			{
				// Next step will destroy selection. Save info to install new one.
				cvsli = sel.CLevels(false) - 1;
				// Get selection information to determine where the user is typing.
				rgvsli = SelLevInfo.AllTextSelInfo(sel, cvsli,
					out ihvoRoot, out tagTextProp, out cpropPrevious, out ichAnchor, out ichEnd,
					out ws, out fAssocPrev, out ihvoEnd, out ttp);
			}
			// This needs to be done even if we don't have a selection.
			m_sda.PropChanged(null, (int)PropChangeType.kpctNotifyAll, m_hvoParent, m_virtualTagObj, m_ihvoTa, 1, 1);
			if (sel != null) // nb old sel is now invalid, but should be OK to compare to null.
			{
				// Make a new selection. The critical difference is that, whereas tagTextProp was the other name property,
				// now that this is the active object the property displayed for its name is newTagName.
				try
				{
					rootb.MakeTextSelection(ihvoRoot, cvsli, rgvsli, newTagName, cpropPrevious, ichAnchor, ichEnd, ws,
						fAssocPrev, ihvoEnd, null, true);
				}
				catch
				{
					// Eat any exceptions.
				}
			}
		}

		public void OnLostFocus(IVwRootBox rootb)
		{
			m_fGotFocus = false;
			if (m_hvoTa != 0)
			{
				SwitchTagAndFixSel(m_snTagName, rootb);
			}
		}

		public void OnGotFocus(IVwRootBox rootb)
		{
			m_fGotFocus = true;
			if (m_hvoTa != 0)
			{
				SwitchTagAndFixSel(m_taTagName, rootb);
			}
		}

		/// <summary>
		/// Determine whether the current selection is at a place suitable for type-ahead. If not, answer 0.
		/// (In this case the values of hvoParent and ihvo should not be relied on.)
		/// If so, indicate the object whose property may be set by type-ahead (hvoParent), the object
		/// in the relevant property that is selected (return result), and its index within the property
		/// (0 if atomic).
		/// </summary>
		/// <param name="rootb"></param>
		/// <param name="sel"></param>
		/// <param name="hvoParent"></param>
		/// <param name="ihvo"></param>
		/// <returns></returns>
		private int SelectedObject(IVwRootBox rootb, IVwSelection sel, out int hvoParent, out int ihvo)
		{
			hvoParent = 0;
			ihvo = 0;
			if (rootb == null) // If we don't have a root box, can't do anything interesting.
				return 0;
			if (sel == null) // nothing interesting to do without a selection, either.
				return 0;
			ITsString tssA, tssE;
			int ichA, ichE, hvoObjA, hvoObjE, tagA, tagE, ws;
			bool fAssocPrev;
			// Enhance JohnT: what we're really trying to do here is confirm that the selection is
			// all in one string property. We could readily have a method in the selection interface to tell us that.
			sel.TextSelInfo(false, out tssA, out ichA, out fAssocPrev, out hvoObjA, out tagA, out ws);
			if (tagA != m_taTagName && tagA != m_snTagName)
				return 0; // selection not anchored in any sort of type-ahead name property.
			sel.TextSelInfo(true, out tssE, out ichE, out fAssocPrev, out hvoObjE, out tagE, out ws);
			int cch = tssA.Length;
			// To do our type-ahead trick, both ends of the seleciton must be in the same string property.
			// Also, we want the selection to extend to the end of the name.
			// Enhance JohnT: if we do a popup window, it may not matter whether the selection extends
			// to the end; just show items that match.
			if (tagE != tagA || hvoObjE != hvoObjA || cch != tssE.Length || Math.Max(ichA, ichE) != cch)
				return 0; // not going to attempt type-ahead behavior
			int clev = sel.CLevels(false);
			if (clev < 2)
				return 0; // can't be our property.
			int tagParent, cpropPrevious;
			IVwPropertyStore vps;
			sel.PropInfo(false, 1, out hvoParent, out tagParent, out ihvo, out cpropPrevious, out vps);
			if (tagParent != m_virtualTagObj)
				return 0; // not our virtual property!
			return hvoObjA;
		}
		/// <summary>
		/// Return the 'tag' or 'flid' that is used to identify the fake property (ref atomic or ref sequence)
		/// that is to be displayed in place of the one this VC was created for.
		/// </summary>
		public int FakePropTag
		{
			get
			{
				if (m_virtualTagObj == 0)
					InitDefault();
				return m_virtualTagObj;
			}
		}

		/// <summary>
		/// This is the default initialization of the VC. It is adequate if
		///		1. A list of possible values for the real property can be obtained from FDO by calling ReferenceTargetCandidates
		///		Enhance JohnT: support the following also:
		///		2. The signature of the real property is CmPossibility (or a subclass), and the actual
		///		list can be obtained from the ListRootId.
		/// </summary>
		public void InitDefault()
		{
			// Enhance: if a ListRootId is specified, use it.
		}

		/// <summary>
		/// All our display method does is to display the name of each item in the fake virtual property.
		/// If it is the active object we are editing, at the relevant position in the relevant owner,
		/// we display the name using the special marker property.
		/// </summary>
		/// <param name="vwenv"></param>
		/// <param name="hvo"></param>
		/// <param name="frag"></param>
		public override void Display(IVwEnv vwenv, int hvo, int frag)
		{
			int hvoParent, tag, ihvo;
			switch(frag)
			{
			case kfragName:
				vwenv.GetOuterObject(vwenv.EmbeddingLevel - 1, out hvoParent, out tag, out ihvo);
				if (m_fGotFocus && hvo == m_hvoTa && hvoParent == m_hvoParent && ihvo == m_ihvoTa)
					vwenv.AddStringProp(m_taTagName, this);
				else
					vwenv.AddStringProp(m_snTagName, this);
				break;
			default:
				throw new Exception("Unexpected fragment ID in TypeAheadSupportVc");
			}
		}

		/// <summary>
		/// This is the real guts of type-ahead. It is called by the client whenever a key is pressed.
		/// It returns true if it handled the key press, which it does if the current selection
		/// is in a type-ahead name property.
		/// </summary>
		/// <param name="ehelp"></param>
		/// <param name="e"></param>
		/// <param name="modifiers"></param>
		/// <param name="vwGraphics"></param>
		/// <returns></returns>
		public virtual bool OnKeyPress(EditingHelper ehelp, KeyPressEventArgs e, Keys modifiers, IVwGraphics vwGraphics)
		{
			IVwRootBox rootb = ehelp.Callbacks.EditedRootBox;
			if (rootb == null) // If we don't have a root box, can't do anything interesting.
				return false;
			IVwSelection sel = rootb.Selection;
			if (sel == null) // nothing interesting to do without a selection, either.
				return false;
			ITsString tssA, tssE;
			int ichA, ichE, hvoObjA, hvoObjE, tagA, tagE, ws;
			bool fAssocPrev;
			// Enhance JohnT: what we're really trying to do here is confirm that the selection is
			// all in one string property. We could readily have a method in the selection interface to tell us that.
			sel.TextSelInfo(false, out tssA, out ichA, out fAssocPrev, out hvoObjA, out tagA, out ws);
			if (tagA != m_taTagName)
				return false; // selection not anchored in a type-ahead name property.
			sel.TextSelInfo(true, out tssE, out ichE, out fAssocPrev, out hvoObjE, out tagE, out ws);
			int cch = tssA.Length;
			// To do our type-ahead trick, both ends of the seleciton must be in the same string property.
			// Also, we want the selection to extend to the end of the name.
			// Enhance JohnT: poupu list may not depend on selection extending to end.
			if (tagE != m_taTagName || hvoObjE != hvoObjA || cch != tssE.Length || Math.Max(ichA, ichE) != cch)
				return false; // not going to attempt type-ahead behavior
			// if the key pressed is a backspace or del, prevent smart completion,
			// otherwise we are likely to put back what the user deleted.
			// Review JohnT: do arrow keys come through here? What do we do if so?
			int charT = Convert.ToInt32(e.KeyChar);
			if (charT == (int)Keys.Back || charT == (int)Keys.Delete)
				return false; // normal key handling will just delete selection. // Review: should backspace delete one more?
			// OK, we're in a type-ahead situation. First step is to let normal editing take place.
			ehelp.OnKeyPress(e, modifiers);
			e.Handled = true;
			// Now see what we have. Note that our old selection is no longer valid.
			sel = rootb.Selection;
			if (sel == null)
				return true; // can't be smart, but we already did the keypress.

			int cvsli = sel.CLevels(false);
			// CLevels includes the string prop itself, but AllTextSelInfo does not need it.
			cvsli--;
			// Get selection information to determine where the user is typing.
			int ihvoObj;
			int tagTextProp;
			int cpropPrevious, ichAnchor, ichEnd, ihvoEnd;
			ITsTextProps ttp;
			SelLevInfo[] rgvsli = SelLevInfo.AllTextSelInfo(sel, cvsli,
				out ihvoObj, out tagTextProp, out cpropPrevious, out ichAnchor, out ichEnd,
				out ws, out fAssocPrev, out ihvoEnd, out ttp);
			if (tagTextProp != m_taTagName || ichAnchor != ichEnd || ihvoEnd != -1 || cvsli < 1)
				return true; // something bizarre happened, but keypress is done.
			int hvoLeaf = rgvsli[0].hvo;

			// Get the parent object we will modify.
			// (This would usually work, but not if the parent object is the root of the whole display,
			// as in a simple atomic ref type ahead slice.
			//int hvoParent = rgvsli[1].hvo; // object whose reference property we are setting.)
			int tagParent, cpropPreviousDummy, ihvo;
			IVwPropertyStore vps;
			int hvoParent;
			sel.PropInfo(false, 1, out hvoParent, out tagParent, out ihvo, out cpropPreviousDummy, out vps);

			if (hvoParent != m_hvoParent)
				return true; // another bizarre unexpected event.
			// This is what the name looks like after the keypress.
			ITsString tssTyped = m_sda.get_StringProp(hvoLeaf, m_taTagName);
			// Get the substitute. This is where the actual type-ahead behavior happens. Sets hvoNewRef to 0 if no match.
			ICmObject objNewRef;
			ITsString tssLookup = Lookup(tssTyped, out objNewRef);
			int hvoNewRef = (objNewRef != null) ? objNewRef.Hvo : 0;
			IVwCacheDa cda = m_sda as IVwCacheDa;
			if (hvoNewRef == 0 && tssTyped.Length > 0)
			{
				// No match...underline string in red squiggle.
				ITsStrBldr bldr = tssLookup.GetBldr();
				bldr.SetIntPropValues(0, tssLookup.Length, (int) FwTextPropType.ktptUnderline,
					(int) FwTextPropVar.ktpvEnum, (int) FwUnderlineType.kuntSquiggle);
				bldr.SetIntPropValues(0, tssLookup.Length, (int) FwTextPropType.ktptUnderColor,
					(int) FwTextPropVar.ktpvDefault, (int)ColorUtil.ConvertColorToBGR(Color.Red));
				tssLookup = bldr.GetString();
			}

			// Don't rely on sel from here on.
			if (hvoNewRef != hvoLeaf)
			{
				m_hvoTa = hvoNewRef; // Before we replace in the prop, so it gets displayed using special ta prop.
				switch (m_type)
				{
				case CellarPropertyType.ReferenceAtomic:
					if (m_hvoParent != 0) // I think it always is, except when loss of focus during debugging causes problems.
					{
						// If nothing matched, set the real property to null and the fake one to kbaseFakeObj.
						// Otherwise set both to the indicated object.
						m_sda.SetObjProp(m_hvoParent, m_tag, hvoNewRef); // Review: do we want to set the real thing yet?
						m_sda.PropChanged(null, (int)PropChangeType.kpctNotifyAll, m_hvoParent, m_tag, 0, 1, 1);
						if (hvoNewRef == 0)
							hvoNewRef = m_hvoTa = kBaseFakeObj; // use in fake so we can display something.
						cda.CacheObjProp(m_hvoParent, m_virtualTagObj, hvoNewRef); // Change the fake property
						m_sda.PropChanged(null, (int)PropChangeType.kpctNotifyAll, m_hvoParent, m_virtualTagObj, 0, 1, 1);
					}
					break;
				case CellarPropertyType.ReferenceSequence:
				case CellarPropertyType.ReferenceCollection:
					// Several cases, depending on whether we got a match and whether hvoLeaf is the dummy object
					// 1. match on dummy: insert appropriate real object, change dummy name to empty.
					// 2. match on non-dummy: replace old object with new
					// 3: non-match: do nothing. (Even if not looking at the fake object, we'll go on using the
					// actual object as a base for the fake name, since it's displayed only for the active position.)
					if (hvoNewRef == 0)
						break; // case 3
					if (hvoLeaf == kBaseFakeObj)
					{ // case 1
						// The fake object goes back to being an empty name at the end of the list.
						ITsStrBldr bldr = tssLookup.GetBldr();
						bldr.ReplaceTsString(0, bldr.Length, null); // makes an empty string in correct ws.
						cda.CacheStringProp(kBaseFakeObj, m_taTagName, bldr.GetString());
						// Insert the new object before the fake one in fake prop and at end of real seq.
						// Include the fake object in the replace to get it redisplayed also.
						cda.CacheReplace(m_hvoParent, m_virtualTagObj, m_ihvoTa, m_ihvoTa + 1, new int[] {hvoNewRef, kBaseFakeObj}, 2);
						m_sda.PropChanged(null, (int)PropChangeType.kpctNotifyAll, m_hvoParent, m_virtualTagObj, m_ihvoTa, 2, 1);
						m_sda.Replace(m_hvoParent, m_tag, m_ihvoTa, m_ihvoTa, new int[] {hvoNewRef}, 1);
						m_sda.PropChanged(null, (int)PropChangeType.kpctNotifyAll, m_hvoParent, m_tag, m_ihvoTa, 1, 0);
					}
					else
					{ // case 2
						// Replace the object being edited with the indicated one in both props.
						cda.CacheReplace(m_hvoParent, m_virtualTagObj, m_ihvoTa, m_ihvoTa + 1, new int[] {hvoNewRef}, 1);
						m_sda.PropChanged(null, (int)PropChangeType.kpctNotifyAll, m_hvoParent, m_virtualTagObj, m_ihvoTa, 1, 1);
						m_sda.Replace(m_hvoParent, m_tag, m_ihvoTa, m_ihvoTa + 1, new int[] {hvoNewRef}, 1);
						m_sda.PropChanged(null, (int)PropChangeType.kpctNotifyAll, m_hvoParent, m_tag, m_ihvoTa, 1, 1);
					}
					break;
				default:
					throw new Exception("unsupported property type for type-ahead chooser");
				}
			}
			cda.CacheStringProp(hvoNewRef, m_taTagName, tssLookup);
			m_sda.PropChanged(null, (int)PropChangeType.kpctNotifyAll, hvoNewRef, m_taTagName, 0, tssLookup.Length, tssTyped.Length);
			// Make a new selection, typically the range that is the bit added to the typed string.
			// no change is needed to rgvsli because it's the same object index in the same property of the same parent.
			sel = rootb.MakeTextSelection(ihvoObj, cvsli, rgvsli, m_taTagName, cpropPrevious, ichAnchor,
				tssLookup.Length, ws, true, -1, null, true);
			return true;
		}

		/// <summary>
		/// Given the string the user typed, generate the best available guess as to which of the options
		/// he intended. Set hvoNew to 0 (and return tssTyped) if it doesn't match any option.
		/// It is assumed that he is setting an object in property m_tag of object m_hvoParent.
		/// </summary>
		/// <param name="tssTyped"></param>
		/// <returns></returns>
		protected virtual ITsString Lookup(ITsString tssTyped, out ICmObject objNew)
		{
			var parent = m_cache.ServiceLocator.GetInstance<ICmObjectRepository>().GetObject(m_hvoParent);
			string sTyped = tssTyped == null ? "" : tssTyped.Text;
			if (sTyped == null)
				sTyped = "";
			int cchTyped = sTyped.Length;
			if (cchTyped == 0)
			{
				// Otherwise we'd match the first item and arbitrarily insert it when the user backspaces to
				// nothing. Seems better to wait till at least one letter is typed to try to match.
				objNew = null;
				return tssTyped;
			}

			int ipossibility = -1;
			foreach(ICmObject obj in parent.ReferenceTargetCandidates(m_tag))
			{
				ipossibility ++;
				string key = null;
				if (ipossibility < m_shortnames.Count)
					key = m_shortnames[ipossibility]; // Use the cache as far as it goes
				else
				{
					key = obj.ShortName;
					m_shortnames.Add(key); // extend the cache for next time.
				}
				if (sTyped.Length < key.Length && key.Substring(0, cchTyped) == sTyped)
				{
					objNew = obj;
					ITsStrBldr bldr = tssTyped.GetBldr();
					bldr.Replace(0, bldr.Length, key, null);
					// Clear any underlining left over from previous bad value.
					bldr.SetIntPropValues(0, bldr.Length, (int) FwTextPropType.ktptUnderline,
						-1, -1);
					bldr.SetIntPropValues(0, bldr.Length, (int) FwTextPropType.ktptUnderColor,
						-1, -1);
					return bldr.GetString(); // Same ws as input, contents replaced.
				}
			}
			objNew = null;

			return tssTyped;
		}
	}
}
