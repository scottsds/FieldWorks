﻿// Copyright (c) 2015 SIL International
// This software is licensed under the LGPL, version 2.1 or later
// (http://www.gnu.org/licenses/lgpl-2.1.html)

using System.Text.RegularExpressions;
using System.Xml;
using SIL.Xml;

namespace SIL.FieldWorks.Common.FwUtils
{
	/// <summary>
	/// Class for utilities used to determine/search/build keys for layout configurations.
	/// Used by Inventory, LayoutCache, LayoutMerger and XmlDocConfigureDlg
	/// </summary>
	public class LayoutKeyUtils
	{
		/// <summary>
		/// This marks the beginning of a tag added to layout names (and param values) when a
		/// node in the tree is copied and has subnodes (the user duplicated part of a view).
		/// This tag will be a suffix on the duplicated node's 'param' attribute and any descendant
		/// layout's 'name' attribute (and their part refs' 'param' attributes).
		/// </summary>
		public const char kcMarkNodeCopy = '%';
		/// <summary>
		/// This marks the beginning of a tag added to layout names (and param values) when an
		/// entire top-level layout type is copied (i.e. this suffix is added when a user makes
		/// a named copy of an entire layout; e.g. 'My Lexeme-based Dictionary').
		/// </summary>
		public const char kcMarkLayoutCopy = '#';
		/// <summary>
		/// This marks the beginning of a tag added to layout names (and param values) for a language
		/// specific reversal index.
		/// </summary>
		public const char kcMarkReversalIndex = '-';

		/// <summary>
		/// A defect in some configuration saving code related to either the hideConfig attribute or children of sublayouts
		/// would use _ as the duplicate identifier on param attributes.
		/// </summary>
		public const char kcMarkNodeCopyBug = '_';

		private const string NameAttr = "name";
		private const string LabelAttr = "label";
		private const string ParamAttr = "param";
		private const string LabelRegExString = @" \(\d\)";

		/// <summary>
		/// Look for signs of user named view or duplicated node in this user config key's name attribute.
		/// If found, return the extra part suffixed to the name as well as a new key array
		/// with the original (unmodified) name that should match the newMaster.
		/// </summary>
		/// <param name="keyAttributes"></param>
		/// <param name="keyVals">oldConfigured key values with (probably) suffixed material on the name</param>
		/// <param name="stdKeyVals">key values that should match the newMaster version</param>
		/// <returns>The extra part of the layout name after the standard name that is
		/// due to either (1) the user copying an entire view, or (2) the user duplicating an element of a view.</returns>
		/// <remarks>This method assumes that it is called only with duplicated data</remarks>
		public static string GetSuffixedPartOfNamedViewOrDuplicateNode(string[] keyAttributes, string[] keyVals,
			out string[] stdKeyVals)
		{
			stdKeyVals = keyVals.Clone() as string[];
			if (keyAttributes.Length > 2 && keyAttributes[2] == NameAttr && stdKeyVals.Length > 2)
			{
				var userModifiedName = stdKeyVals[2];
				var index = userModifiedName.IndexOfAny(new[] { kcMarkLayoutCopy, kcMarkNodeCopy, kcMarkReversalIndex });
				var bugSuffixIndex = userModifiedName.IndexOf(kcMarkNodeCopyBug);
				if (index > 0 || bugSuffixIndex > 0)
				{
					// In data such as "publishStem_AsPara#selar491" the '_' does not indicate the beginning
					// of the suffix. Other valid situations are "publishCfCd%01_1.0" and the buggy situation "publishRootSubEntryType_1"
					// So only use the '_' to trim the string if the other correct suffix markers aren't found
					var adjustedIndex = index > 0 ? index : bugSuffixIndex;
					stdKeyVals[2] = userModifiedName.Substring(0, adjustedIndex);
					return userModifiedName.Substring(adjustedIndex);
				}
			}
			return string.Empty;
		}

		/// <summary>
		/// Look at the part ref label for a possible duplicated node suffix to put on the new label.
		/// Will be of the format "(x)" where x is a digit, except there could be more than one
		/// separated by spaces.
		/// </summary>
		/// <param name="partRefNode"></param>
		/// <returns></returns>
		public static string GetPossibleLabelSuffix(XmlNode partRefNode)
		{
			var label = partRefNode.GetOptionalStringAttribute(LabelAttr, string.Empty);
			if (string.IsNullOrEmpty(label))
				return string.Empty;
			var regexp = new Regex(LabelRegExString);
			var match = regexp.Match(label);
			// if there's a match, we want everything after the index
			return match.Success ? label.Substring(match.Index) : string.Empty;
		}

		/// <summary>
		/// Look at the part ref param attribute for a possible duplicated node suffix to copy to
		/// the new param attribute.
		/// Will be of the format "%0x" where x is a digit.
		/// </summary>
		/// <param name="partRefNode"></param>
		/// <returns></returns>
		public static string GetPossibleParamSuffix(XmlNode partRefNode)
		{
			var param = partRefNode.GetOptionalStringAttribute(ParamAttr, string.Empty);
			if (string.IsNullOrEmpty(param))
				return string.Empty;
			var index = param.IndexOfAny(new [] {kcMarkNodeCopy, kcMarkNodeCopyBug});
			if (index > 0)
				return param.Substring(index);
			return string.Empty;
		}
	}
}
