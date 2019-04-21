// Copyright (c) 2014-2017 SIL International
// This software is licensed under the LGPL, version 2.1 or later
// (http://www.gnu.org/licenses/lgpl-2.1.html)

using System.Collections.Generic;
using System.Xml;
using SIL.FieldWorks.Common.FwUtils;
using SIL.LCModel;

namespace SIL.FieldWorks.XWorks
{
	/// <summary>
	/// This interface is used in the conversion of xml configuration from layout and parts files into another form. i.e. TreeNodes
	/// </summary>
	public interface ILayoutConverter
	{
		/// <summary>
		/// This method is called when the entire layout has been converted a tree of LayoutTreeNodes
		/// </summary>
		void AddDictionaryTypeItem(XmlNode layoutNode, List<XmlDocConfigureDlg.LayoutTreeNode> oldNodes);

		/// <summary>
		/// Returns the configuration nodes for all layout types
		/// </summary>
		IEnumerable<XmlNode> GetLayoutTypes();

		/// <summary/>
		LcmCache Cache { get; }

		/// <summary/>
		StringTable StringTable { get; }

		/// <summary/>
		LayoutLevels LayoutLevels { get; }

		/// <summary/>
		void ExpandWsTaggedNodes(string sWsTag);

		/// <summary/>
		void SetOriginalIndexForNode(XmlDocConfigureDlg.LayoutTreeNode mainLayoutNode);

		/// <summary/>
		XmlNode GetLayoutElement(string className, string layoutName);

		/// <summary/>
		XmlNode GetPartElement(string className, string sRef);

		/// <summary/>
		void BuildRelationTypeList(XmlDocConfigureDlg.LayoutTreeNode ltn);

		/// <summary/>
		void BuildEntryTypeList(XmlDocConfigureDlg.LayoutTreeNode ltn, string layoutName);

		void LogConversionError(string errorLog);
	}
}