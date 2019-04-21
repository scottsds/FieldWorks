// Copyright (c) 2003-2017 SIL International
// This software is licensed under the LGPL, version 2.1 or later
// (http://www.gnu.org/licenses/lgpl-2.1.html)

using System;
using System.Globalization;
using System.Text;
using System.Xml;
using System.Diagnostics;
using System.IO;
using System.Collections.Generic;
using System.Linq;
using SIL.LCModel;
using SIL.LCModel.Utils;
using SIL.FieldWorks.Common.FwUtils;
using SIL.Utils;

namespace XCore
{
	/// <summary>
	/// Constructs a composite Xml document by gathering elements from files.
	/// An XPath may be used to identify the desired elements from each file.
	/// They all become top-level elements in the composite document, which may
	/// then be searched for desired elements.
	/// A list of key attributes may be supplied; if a node with the same values
	/// for all key attributes (or absence of a key attribute) is found in more
	/// than one file, the last encountered 'wins'.
	/// For example (square brakets used for angle ones)
	/// [layout class="LexSense" type="Detail" id="Advanced"]
	/// (here class, type, and id are listed as key attributes).
	///
	/// An element may also have an attribute 'base' which is used to create
	/// derived elements by a 'unification' process. For example, we might have
	/// [layout class="LexSense" type="Detail" id="MyAdvanced" base="Advanced"].
	///
	/// One of the key attributes is identified (by being placed last in the list of key
	/// attributes) as the one that 'base' must match; so here, we would list the
	/// key attributes as "class, type, id". The base element that a derived one
	/// modifies is found by matching all attributes except the last (and the element name)
	/// exactly as found in the derived node; the last key attribute of the base
	/// element must match the 'base' attribute of the derived element.
	///
	/// It is also possible for a derived node to 'override' the one it is based on:
	/// [layout class="LexSense" type="Detail" id="Advanced" base="Advanced"].
	/// This means that the new Advanced layout is unified with the existing one and
	/// then 'replaces' it, becoming the layout that will actually be used if the 'Advanced'
	/// one is requested.
	///
	/// If both occur (a derived layout called Advanced based on Advanced, and a layout
	/// with a different name based on Advanced), the layout with the different name
	/// is based on the overridden version of 'Advanced', no matter what order the
	/// elements were loaded.
	///
	/// It is an error to load an element (base or derived) whose key matches a
	/// previously-loaded override. (Among other things, this means that a base
	/// element must be loaded before its override.)
	///
	/// It is possible to override a derived element. However, all information needed
	/// to compute the effect of the derivation must be loaded before the override.
	///
	/// When derivation occurs, it must be possible to retrieve from the collection
	/// the base element, the derived element's original information, and the
	/// result of unifying the base with the derived information. The last of these
	/// (the 'unified' element) is the most important, and is stored in the main document
	/// and retrieved using GetElement. The original derived element (with the 'base' attribute)
	/// is stored separately and can be retrieved using GetDerived; similarly, the base
	/// is stored separately and may be retrieved using GetBase.
	///
	/// The current client has two instances of inventory, accessed by Inventories["parts"]
	/// and Inventories["layouts"].
	/// </summary>
	public class Inventory
	{
		#region Member data
		/// <summary>
		/// This does not represent any physical file on the disk,
		/// it is an in memory only document.
		/// </summary>
		protected XmlDocument m_mainDoc;
		protected XmlDocument m_baseDoc; // doc used to store replaced base elements
		// doc used to store alteration elements. (This is not the OUTPUT of the derivation/
		// unification process: that is stored in m_mainDoc. This is the node we read from
		// the file containing the specifications for the alteration.
		protected XmlDocument m_alterationsDoc;
		/// <summary>
		/// Set of template paths.
		/// </summary>
		protected HashSet<string> m_inventoryPaths;
		// The pattern used to find files to load into the inventory in a directory.
		// PersistOverrideElement assumes that it can strip off one character to get a useful
		// suffix for a file name (e.g., '*.fwlayout' is a typical pattern).
		protected string m_filePattern;
		protected string m_sDatabase = null;
		protected string m_projectPath;
		// An xpath defining the elements we want to load from the files.
		// PersistOverrideElement assumes that it follows the pattern /element1/element2/.../elementn/*
		// for example, '/LayoutInventory/*' or '/Parts/bin/*'.
		protected string m_xpathElementsWanted;
		// List of attribute names that must match for a node to be considered a replacement
		// of an existing node, keyed by element name.
		protected Dictionary<string, string[]> m_keyAttrs;
		// This table is used to implement the GetUnified method, which finds or creates
		// an element produced by unifying the children of two nodes. The key is a 'KeyValuePair'
		// in which the items are the two source XML nodes. The value is the unified XML node.
		protected Dictionary<Tuple<XmlNode, XmlNode>, XmlNode> m_unifiedNodes = new Dictionary<Tuple<XmlNode, XmlNode>, XmlNode>();
		Dictionary<GetElementKey, XmlNode> m_getElementTable = new Dictionary<GetElementKey, XmlNode>();
		static Dictionary<string, Inventory> s_inventories = new Dictionary<string, Inventory>();
		List<KeyValuePair<string, DateTime>> m_fileInfo;
		int m_version = 0; // Version number passed to LoadUserOverrides.
		// This is used to store layout nodes that have an attribute tagForWs="true".  These are
		// used in displaying Reversal Indexes, and need have separate versions generated for each
		// reversal index writing system.
		List<XmlNode> m_wsTaggedNodes = new List<XmlNode>();

		// Tracing variable - used to control when and what is output to the debug and trace listeners
		private TraceSwitch xmlInventorySwitch = new TraceSwitch("XML_Inventory", "", "Off");

		private IOldVersionMerger m_merger; // client-supplied merger for old-version user overrides.
		private string m_appName;
		#endregion

		/// <summary>
		/// This marks the beginning of a tag added to layout names (and param values) when a
		/// node in the tree is copied and has subnodes.
		/// </summary>
		public const char kcMarkNodeCopy = '%';
		/// <summary>
		/// This marks the beginning of a tag added to layout names (and param values) when an
		/// entire top-level layout type is copied.
		/// </summary>
		public const char kcMarkLayoutCopy = '#';

		/// <summary>
		/// Layout filename divider. In the case of user-created dictionary views there will
		/// be two of these; otherwise only one.
		/// </summary>
		public const string ksUnderscore = "_";

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Initializes a new instance of the <see cref="Inventory"/> class.
		/// </summary>
		/// <param name="customInventoryPath">A path to custom inventory files,
		/// or null, if defaults are fine.</param>
		/// <param name="filePattern">The pattern to select files from each directory</param>
		/// <param name="xpath">Identifies the elements we want to load.</param>
		/// <param name="keyAttrs">Initialize the KeyAttributes property</param>
		/// <param name="appName">Name of the application.</param>
		/// <param name="projectPath">Path of the project folder</param>
		/// ------------------------------------------------------------------------------------
		public Inventory(string customInventoryPath, string filePattern, string xpath,
			Dictionary<string, string[]> keyAttrs, string appName, string projectPath) :
			this(customInventoryPath != null ? new string[] { customInventoryPath } : null,
			filePattern, xpath, keyAttrs, appName, projectPath)
		{
		}

		/// -----------------------------------------------------------------------------------
		/// <summary>
		/// Initializes a new instance of the <see cref="Inventory"/> class, giving a list
		/// of directories to search after the default one.
		/// </summary>
		/// <param name="customInventoryPaths">A list of paths to custom inventory files,
		/// leave empty if defaults are fine.</param>
		/// <param name="filePattern">The pattern to select files from each directory</param>
		/// <param name="keyAttrs">Initialize the KeyAttributes property</param>
		/// <param name="xpath">Identifies the elements we want to load.</param>
		/// -----------------------------------------------------------------------------------
		public Inventory(string[] customInventoryPaths, string filePattern, string xpath,
			Dictionary<string, string[]> keyAttrs, string appName, String projectPath)
			:this (filePattern, xpath, keyAttrs, appName, projectPath)
		{
			int msStart = Environment.TickCount;
			m_inventoryPaths.Add(FwDirectoryFinder.GetCodeSubDirectory(@"Parts"));
			if (customInventoryPaths != null)
			{
				foreach (string customInventoryPath in customInventoryPaths)
					m_inventoryPaths.Add(FwDirectoryFinder.GetCodeSubDirectory(customInventoryPath));
			}
			LoadElements();
			if (xmlInventorySwitch.TraceInfo)
			{
				int msEnd = Environment.TickCount;
				string sInventory = (customInventoryPaths == null || customInventoryPaths.Length > 1) ? filePattern : customInventoryPaths[0];
				Debug.WriteLine("Initializing part inventory " + sInventory + " took " + (msEnd - msStart) + " ms", xmlInventorySwitch.DisplayName);
			}
		}

		/// -----------------------------------------------------------------------------------
		/// <summary>
		/// Initializes a new instance of the <see cref="Inventory"/> class, with no items.
		/// Caller should call LoadElements(input) to initialize. This is used for tests.
		/// </summary>
		/// <param name="filePattern">The pattern to select files from each directory</param>
		/// <param name="keyAttrs">Initialize the KeyAttributes property</param>
		/// <param name="xpath">Identifies the elements we want to load.</param>
		/// -----------------------------------------------------------------------------------
		public Inventory(string filePattern, string xpath,
			Dictionary<string, string[]> keyAttrs, string appName, String projectPath)
		{
			m_inventoryPaths = new HashSet<string>();
			m_filePattern = filePattern;
			m_keyAttrs = keyAttrs;
			m_xpathElementsWanted = xpath;
			m_appName = appName;
			m_projectPath = projectPath;
		}

		/// <summary>
		/// Provides a merge tool for safely creating valid current versions of user overrides
		/// belonging to an older version.
		/// </summary>
		public IOldVersionMerger Merger
		{
			get { return m_merger; }
			set { m_merger = value; }
		}
		/// <summary>
		/// Load overrides from the user's private directory. If version number does not match, ignore.
		/// </summary>
		public void LoadUserOverrides(int version, string sDatabase)
		{
			Debug.Assert(m_version == version || m_version == 0);
			m_version = version; // remember for reloads; currently we only support one version.
			Debug.Assert(m_sDatabase == null || m_sDatabase == sDatabase);
			m_sDatabase = sDatabase;
			string path = UserOverrideConfigurationSettingsPath;
			m_inventoryPaths.Add(path);
			if (Directory.Exists(path))
			{
				string sPattern;
				if (String.IsNullOrEmpty(sDatabase))
					sPattern = String.Format("default$${0}", m_filePattern);
				else
					sPattern = m_filePattern;
				AddElementsFromFiles(DirectoryUtils.GetOrderedFiles(path, sPattern), version, true);
			}
		}

		/// <summary>
		/// Delete override files from the user's private directory.  Ignore version numbers.
		/// </summary>
		public void DeleteUserOverrides(string sDatabase)
		{
			Debug.Assert(m_sDatabase == null || m_sDatabase == sDatabase);
			m_sDatabase = sDatabase;
			string path = UserOverrideConfigurationSettingsPath;
			if (Directory.Exists(path))
			{
				string sPattern;
				if (String.IsNullOrEmpty(sDatabase))
					sPattern = String.Format("default$${0}", m_filePattern);
				else
					sPattern = m_filePattern;
				string[] rgsFiles = DirectoryUtils.GetOrderedFiles(path, sPattern);
				foreach (string sFilename in rgsFiles)
				{
					// LT-11193 Don't delete user overrides if they are new dictionary views
					// (from the Manage Views dialog)
					if(sFilename.Split(new[] {ksUnderscore}, StringSplitOptions.RemoveEmptyEntries).Length > 1)
						continue;

					File.Delete(sFilename);
				}
			}
		}

		/// <summary>
		/// Get/set the name of the database associated with this Inventory.
		/// </summary>
		public string DatabaseName
		{
			get { return m_sDatabase; }
			set { m_sDatabase = value; }
		}

		/// <summary>
		/// Persist an override element by creating a file which LoadUserOverides will load.
		/// We may need to generalize this somewhat; currently it is only designed to
		/// handle the complexities of saving custom layouts.
		///
		/// 1. Assuming that m_xpathElementsWanted is something like /LayoutInventory/*,
		///    Make an XmlDocument that contains a LayoutInventory node containing element.
		///
		/// 2. Make a unique file name based on the name of the element and its identifying
		///    attributes. For example, if we have
		/// 			keyAttrs["layout"] = new string[] {"class", "type", "name" };
		///    and an element layout class="LexEntry" type="Detail" name="Full"
		///    and our m_filePattern is ".fwlayout"
		///    we want to make a file LexEntry.fwlayout.
		///   If this file already exists, we replace or append the element that matches
		///   the one we're given according to our key attributes.
		///
		///   However, if the name looks like "Full#Foo", the filename comes from the
		///   corresponding layoutType node with a layout name that ends with "#Foo".
		/// </summary>
		/// <param name="element"></param>
		public void PersistOverrideElement(XmlNode element)
		{
			string[] keyAttrs = m_keyAttrs[element.Name];
			if (element.Name == "layout")
			{
				string sVersion = XmlUtils.GetOptionalAttributeValue(element, "version");
				if (sVersion == null && m_version != 0)
					XmlUtils.AppendAttribute(element, "version", m_version.ToString());
			}
			string name = null;
			var layoutName = XmlUtils.GetOptionalAttributeValue(element, "name", "");
			var idxTag = layoutName.IndexOf(kcMarkLayoutCopy);
			XmlNode layoutType = null;
			if (idxTag > 0)
			{
				var tag = layoutName.Substring(idxTag);
				var idx = tag.IndexOf(LayoutKeyUtils.kcMarkNodeCopy);
				if (idx > 0)
					tag = tag.Remove(idx);
				Debug.Assert(m_mainDoc != null);
				XmlNode root = m_mainDoc["Main"];
				Debug.Assert(root != null);
				var nodes = root.SelectNodes("layoutType");
				if (nodes != null)
				{
					foreach (var node in nodes.OfType<XmlNode>())
					{
						var layoutNode = XmlUtils.GetMandatoryAttributeValue(node, "layout");
						if (layoutNode.EndsWith(tag))
						{
							layoutType = node;
							break;
						}
					}
					if (layoutType != null)
					{
						var label = XmlUtils.GetMandatoryAttributeValue(layoutType, "label");
						var className = XmlUtils.GetOptionalAttributeValue(layoutType.FirstChild, "class");
						name = String.Format("{0}_{1}", label, className);
					}
				}
			}
			if (String.IsNullOrEmpty(name))
				name = XmlUtils.GetMandatoryAttributeValue(element, keyAttrs[0]);
			string sDatabase = String.IsNullOrEmpty(m_sDatabase) ? "default$$" : "";
			string fileName = sDatabase + name + m_filePattern.Substring(1); // strip off leading *
			string path = Path.Combine(UserOverrideConfigurationSettingsPath, fileName);
			CreateDirectoryIfNonexistant(UserOverrideConfigurationSettingsPath);
			XmlDocument doc = null;
			string[] parentEltNames = GetParentElementNames();
			XmlNode parent = null; // where we will put new element

			// We expect to find or create a document that has one element
			if (File.Exists(path))
			{
				doc = new XmlDocument();
				doc.Load(path);
				parent = doc.DocumentElement;
				XmlNode current = parent;
				foreach (string eltName in parentEltNames)
				{
					if (current.Name != eltName)
					{
						parent = null;
						break;
					}
					parent = current;
					current = XmlUtils.GetFirstNonCommentChild(current);
				}
				if (parent != null)
				{
					// Remove any matching child
					foreach (XmlNode child in parent.ChildNodes)
					{
						if (child.Name != element.Name)
							continue;
						bool match = true;
						foreach (string attrName in keyAttrs)
						{
							if (XmlUtils.GetOptionalAttributeValue(child, attrName) !=
								XmlUtils.GetOptionalAttributeValue(element, attrName))
							{
								match = false;
								break;
							}
						}
						if (match)
						{
							parent.RemoveChild(child);
							break;
						}
					}
					// Remove any matching layoutType
					if (layoutType != null)
					{
						var layout = XmlUtils.GetMandatoryAttributeValue(layoutType, "layout");
						foreach (XmlNode child in parent.ChildNodes)
						{
							if (child.Name != layoutType.Name)
								continue;
							if (XmlUtils.GetOptionalAttributeValue(child, "layout") != layout)
								continue;
							parent.RemoveChild(child);
							break;
						}
					}
				}
			}
			if (parent == null)
			{
				// File does not exist, or has unexpected contents; overwrite.
				doc = new XmlDocument();

				doc.AppendChild(doc.CreateElement(parentEltNames[0]));
				parent = doc.DocumentElement;
				for (int i = 1; i < parentEltNames.Length; i++)
				{
					XmlNode child = doc.CreateElement(parentEltNames[i]);
					parent.AppendChild(child);
					parent = child;
				}
			}
			// One way or another we now have a parent node that doesn't contain an element
			// that conflicts with the new one.
			parent.AppendChild(doc.ImportNode(element, true));
			if (layoutType != null)
				parent.AppendChild(doc.ImportNode(layoutType, true));
			// OK, the document now contains the desired content. Write it out.
			using (var xwriter = new XmlTextWriter(path, Encoding.UTF8))
			{
				xwriter.Formatting = Formatting.Indented;
				doc.WriteTo(xwriter);
				xwriter.Close();
			}
			// Finally actually add it to the inventory, replacing any existing node of the same key.
			AddNodeToInventory(element);
		}

		private void CreateDirectoryIfNonexistant(string directory)
		{
			if (!Directory.Exists(directory))
				Directory.CreateDirectory(directory);
		}

		/// <summary>
		/// Add (or replace) the given layout type in the inventory.
		/// </summary>
		public void AddLayoutTypeToInventory(XmlNode layoutType)
		{
			Debug.Assert(m_mainDoc != null);
			XmlNode root = m_mainDoc["Main"];
			Debug.Assert(root != null);

			var layoutName = XmlUtils.GetMandatoryAttributeValue(layoutType, "layout");
			var nodes = root.SelectNodes("layoutType");
			if (nodes != null)
			{
				foreach (var xn in nodes.OfType<XmlNode>())
				{
					var layoutOld = XmlUtils.GetMandatoryAttributeValue(xn, "layout");
					if (layoutOld == layoutName)
					{
						root.ReplaceChild(m_mainDoc.ImportNode(layoutType, true), xn);
						return;
					}
				}
			}
			root.AppendChild(m_mainDoc.ImportNode(layoutType, true));
		}

		/// <summary>
		/// Return the list of layout types that the inventory knows about.
		/// </summary>
		public List<XmlNode> GetLayoutTypes()
		{
			Debug.Assert(m_mainDoc != null);
			XmlNode root = m_mainDoc["Main"];
			Debug.Assert(root != null);

			var retval = new List<XmlNode>();
			var nodes = root.SelectNodes("layoutType");
			if (nodes != null)
				retval.AddRange(nodes.OfType<XmlNode>());
			return retval;
		}

		/// <summary>
		/// Take the given node and add it to the parts/layouts inventory
		/// </summary>
		/// <param name="element"></param>
		public void AddNodeToInventory(XmlNode element)
		{
			AddNode(element, m_mainDoc["Main"]);
		}

		private string[] GetParentElementNames()
		{
			Debug.Assert(m_xpathElementsWanted[0] == '/');
			Debug.Assert(m_xpathElementsWanted.Substring(m_xpathElementsWanted.Length - 2, 2) == "/*");
			string[] result = m_xpathElementsWanted.Substring(1, m_xpathElementsWanted.Length - 3).Split('/');
			Debug.Assert(result.Length > 0);
			Debug.Assert(result[0].Length > 0);
			return result;
		}

		private string UserOverrideConfigurationSettingsPath
		{
			get { return LcmFileHelper.GetConfigSettingsDir(m_projectPath); }
		}

		/// <summary>
		/// Used to retrieve a shared inventory by name.
		/// </summary>
		static public Inventory GetInventory(string key, string sDatabase)
		{
			string sKey;
			if (!String.IsNullOrEmpty(sDatabase))
				sKey = String.Format("{0}${1}", key, sDatabase);
			else
				sKey = key;
			if (s_inventories.ContainsKey(sKey))
			{
				Inventory val = s_inventories[sKey];
				Debug.Assert(val.m_sDatabase == sDatabase);
				return val;
			}
			return null;
		}

		/// <summary>
		/// Used to remove a shared inventory by name.
		/// </summary>
		/// <param name="key"></param>
		/// <param name="sDatabase"></param>
		static public void RemoveInventory(string key, string sDatabase)
		{
			string sKey;
			if (!String.IsNullOrEmpty(sDatabase))
				sKey = String.Format("{0}${1}", key, sDatabase);
			else
				sKey = key;
			if (s_inventories.ContainsKey(sKey))
			{
				s_inventories.Remove(sKey);
			}
		}

		/// <summary>
		/// Used to set up a shared inventory.
		/// </summary>
		/// <param name="key">The key.</param>
		/// <param name="sDatabase">The database.</param>
		/// <param name="val">The val.</param>
		static public void SetInventory(string key, string sDatabase, Inventory val)
		{
			if (string.IsNullOrEmpty(key))
				throw new ArgumentException("Invalid key argument.");
			if (val == null)
				throw new ArgumentNullException("val");
			Debug.Assert(val.m_sDatabase == null || val.m_sDatabase == sDatabase);
			if (val.m_sDatabase == null)
				val.m_sDatabase = sDatabase;
			string sKey;
			if (!String.IsNullOrEmpty(sDatabase))
				sKey = String.Format("{0}${1}", key, sDatabase);
			else
				sKey = key;
			s_inventories[sKey] = val;
		}

		/// <summary>
		/// Get the root node that has the collected elements as direct children.
		/// </summary>
		public XmlNode Root
		{
			get
			{
				Debug.Assert(m_mainDoc != null);
				return m_mainDoc["Main"];
			}
		}

		/// <summary>
		/// List of attribute names that must match for a node to be considered a replacement
		/// of an existing node.		/// </summary>
		public Dictionary<string, string[]> KeyAttributes
		{
			get { return m_keyAttrs;}
		}

		/// <summary>
		/// Get the node (if any) that has the specified element name and the specified value
		/// for each of the attributes listed in KeyAttributes. If an item in attrvals is null,
		/// the specified attribute must not occur at all. If it's an empty string, it must
		/// occur with an empty value.
		///
		/// This is the main access point for clients wanting to USE a (possibly derived)
		/// element with a particular key.
		/// </summary>
		/// <param name="elementName"></param>
		/// <param name="attrvals"></param>
		/// <returns></returns>
		public XmlNode GetElement(string elementName, string[] attrvals)
		{
			XmlNode node = GetEltFromDoc(elementName, attrvals, m_mainDoc);
			if (node != null)
				return node;
			// If not found, there might be an alteration node for which we have not yet computed the
			// meaning. Do it now, and return the result.
			// We postponed computing the effect of alterations for two reasons:
			// 1. Saves memory and time for alterations that are never used.
			// 2. Ensures that an alteration based on an override gets based on the override,
			// even if it is loaded before the override.
			XmlNode alteration = GetEltFromDoc(elementName, attrvals, m_alterationsDoc);
			if (alteration == null)
				return null;
			return ApplyAlteration(elementName, attrvals, alteration);
		}

		/// <summary>
		/// Compute and save in m_mainDoc a node which represents the actual meaning of the given
		/// alteration node, which has the specified element name and keys.
		/// </summary>
		/// <param name="alteration"></param>
		/// <returns></returns>
		private XmlNode ApplyAlteration(string elementName, string[] attrvals, XmlNode alteration)
		{
			string baseName = XmlUtils.GetMandatoryAttributeValue(alteration, "base");
			string[] baseKey = (string[])attrvals.Clone();
			int cKeys = baseKey.Length;
			baseKey[cKeys - 1] = baseName;
			XmlNode baseNode = GetEltFromDoc(elementName, baseKey, m_mainDoc);
			XmlNode result = Unify(alteration, baseNode);
			m_mainDoc["Main"].AppendChild(result);
			// we may already have a 'miss' for this element cached, so update the table
			// 'result' may be null;
			var key = new GetElementKey(elementName, attrvals, m_mainDoc);
			m_getElementTable[key] = result;

			return result;
		}

		/// <summary>
		/// Unify two nodes.
		/// This creates a new node in m_mainDoc with the same element name as alteration
		/// (and baseNode, unless it is null).
		/// It is expected that baseNode matches alteration for all the key attributes
		/// specied for this element name.
		/// The unified node has all the same attributes as alteration, plus any attributes
		/// of baseNode that are not present in alteration.
		/// It also has children, created as follows:
		/// For each child of alteration, if there is a child of baseNode that has the
		/// same element name and the same key attributes appropriate to that element,
		/// the new child is produced by unifying those two nodes. Otherwise the node is
		/// simply copied.
		/// Also, each child of baseNode that does not get unified with a child of
		/// alteration is copied to the unified node.
		/// The order of child nodes is, by default, the order in which nodes appear in
		/// baseNode, followed by any nodes in alteration which don't unify.
		/// if alteration has the attribute/value reorder='true', then the order is the
		/// order in alteration, followed by base nodes that don't unify.
		/// </summary>
		/// <param name="alteration"></param>
		/// <param name="baseNode"></param>
		/// <returns></returns>
		private XmlNode Unify(XmlNode alteration, XmlNode baseNode)
		{
			// If we don't have a base node, make an exact copy of alteration.
			if (baseNode == null)
				return m_mainDoc.ImportNode(alteration, true);
			// And, likewise, if we don't have an alteration, make an exact copy of base.
			if (alteration == null)
				return m_mainDoc.ImportNode(baseNode, true);
			XmlNode unified = m_mainDoc.CreateNode(XmlNodeType.Element, alteration.Name, null);
			CopyAttributes(alteration, unified, false);
			CopyAttributes(baseNode, unified, true);

			UnifyChildren(alteration, baseNode, unified);

			return unified;
		}

		private void UnifyChildren(XmlNode alteration, XmlNode baseNode, XmlNode unified)
		{
			bool reorder = XmlUtils.GetOptionalBooleanAttributeValue(alteration, "reorder", false);
			XmlNodeList orderBy;
			HashSet<XmlNode> remainingOthers;
			XmlNodeList others;
			if (reorder)
			{
				orderBy = alteration.ChildNodes;
				others = baseNode.ChildNodes;
			}
			else
			{
				orderBy = baseNode.ChildNodes;
				others = alteration.ChildNodes;
			}
			remainingOthers = new HashSet<XmlNode>();
			foreach(XmlNode node in others)
				remainingOthers.Add(node);
			foreach(XmlNode item in orderBy)
			{
				XmlNode other = MatchAndRemove(remainingOthers, item);
				XmlNode newChild;
				if (reorder)
					newChild = Unify(item, other); // other is the base, item is the override.
				else
					newChild = Unify(other, item); // item is the base, other is the override
				unified.AppendChild(newChild);
			}
			foreach(XmlNode item in remainingOthers)
				unified.AppendChild(m_mainDoc.ImportNode(item, true));
		}

		private void CopyAttributes(XmlNode source, XmlNode dest, bool fIfNotPresent)
		{
			foreach(XmlAttribute attr in source.Attributes)
			{
				if (fIfNotPresent && dest.Attributes[attr.Name] != null)
					continue;
				XmlAttribute xa = m_mainDoc.CreateAttribute(attr.Name);
				dest.Attributes.Append(xa);
				xa.Value = attr.Value;
			}
		}

		/// <summary>
		/// If there is a node in remainingOthers which 'matches' item (in name and
		/// specified keys), remove and return it; otherwise return null.
		/// </summary>
		XmlNode MatchAndRemove(HashSet<XmlNode> remainingOthers, XmlNode target)
		{
			string elementName = target.Name;
			string[] keyAttrs = null;
			if (m_keyAttrs.ContainsKey(elementName))
				keyAttrs = m_keyAttrs[elementName];
			int ckeys = (keyAttrs == null ? 0 : keyAttrs.Length);
			string[] keyVals = new string[ckeys]; // keys to try to match for each item
			for (int i = 0; i < ckeys; i++)
				keyVals[i] = XmlUtils.GetOptionalAttributeValue(target, keyAttrs[i]);
			foreach (XmlNode item in remainingOthers)
			{
				if (item.Name != elementName)
					continue;
				// See if all the keys match
				int cMatchingAttrs = 0;
				for (; cMatchingAttrs < ckeys; cMatchingAttrs++)
				{
					if (XmlUtils.GetOptionalAttributeValue(item, keyAttrs[cMatchingAttrs]) != keyVals[cMatchingAttrs])
						break;
				}
				if (cMatchingAttrs == ckeys)
				{
					// Full match!
					remainingOthers.Remove(item);
					return item;
				}
			}
			return null;
		}

		/// <summary>
		/// This routine just takes an xpath, and returns the matching elements.
		/// </summary>
		/// <param name="xpath">xpath string</param>
		/// <returns></returns>
		public XmlNodeList GetElements(string xpath)
		{
			return m_mainDoc["Main"].SelectNodes(xpath);
		}

		/// <summary>
		/// This routine takes a set of arguments, typically with a less complete set of attr vals
		/// than GetElement, and returns all matching elements. Currently it is not guaranteed to
		/// find alterations.
		/// </summary>
		/// <param name="elementName"></param>
		/// <param name="attrvals"></param>
		/// <returns></returns>
		public XmlNodeList GetElements(string elementName, string[] attrvals)
		{
			// Create an xpath that will select a node with the same name and key attributes
			// (if any) as newNode. If some attributes are missing from newNode, they
			// must be missing from the matched node as well.
			string[] keyAttrs = null;
			if (m_keyAttrs.ContainsKey(elementName))
				keyAttrs = m_keyAttrs[elementName];
			if (keyAttrs == null)
				keyAttrs = new string[0];
			StringBuilder pathBldr = new StringBuilder(elementName);
			int numAttrs = Math.Min(keyAttrs.Length, attrvals.Length);
			if (numAttrs > 0)
			{
				pathBldr.Append("[");
				for(int i = 0; i < numAttrs; i++)
				{
					string attr = keyAttrs[i];
					if (i != 0)
						pathBldr.Append(" and ");
					string val = attrvals[i];
					if (val == null)
					{
						pathBldr.Append("not(@");
						pathBldr.Append(attr);
						pathBldr.Append(")");
					}
					else
					{
						pathBldr.Append("@");
						pathBldr.Append(attr);
						pathBldr.Append("='");
						pathBldr.Append(val);
						pathBldr.Append("'");
					}
				}
				pathBldr.Append("]");
			}

			return m_mainDoc["Main"].SelectNodes(pathBldr.ToString());
		}

		protected XmlNode GetEltFromDoc(string elementName, string[] attrvals, XmlDocument doc)
		{
			var key = new GetElementKey(elementName, attrvals, doc);
			XmlNode result = null;
			bool hasKey = m_getElementTable.ContainsKey(key);
			if (hasKey)
				result = m_getElementTable[key]; // May be null, even with the key.
			if (result == null && !hasKey)
			{
				result = GetEltFromDoc1(elementName, attrvals, doc);
				m_getElementTable[key] = result; // May still be null.
			}
			return result;
		}

		protected XmlNode GetEltFromDoc1(string elementName, string[] attrvals, XmlDocument doc)
		{
			// Create an xpath that will select a node with the same name and key attributes
			// (if any) as newNode. If some attributes are missing from newNode, they
			// must be missing from the matched node as well.
			string[] keyAttrs = null;
			if (m_keyAttrs.ContainsKey(elementName))
				keyAttrs = m_keyAttrs[elementName];
			if (keyAttrs == null)
				keyAttrs = new string[0];
			StringBuilder pathBldr = new StringBuilder(elementName);
			if (keyAttrs.Length > 0)
			{
				pathBldr.Append("[");
				for(int i = 0; i < keyAttrs.Length; i++)
				{
					string attr = keyAttrs[i];
					if (i != 0)
						pathBldr.Append(" and ");
					string val = attrvals[i];
					if (val == null)
					{
						pathBldr.Append("not(@");
						pathBldr.Append(attr);
						pathBldr.Append(")");
					}
					else
					{
						pathBldr.Append("@");
						pathBldr.Append(attr);
						if (val.Contains("'"))
						{
							pathBldr.Append("=\"");
							pathBldr.Append(val);
							pathBldr.Append("\"");
						}
						else
						{
							pathBldr.Append("='");
							pathBldr.Append(val);
							pathBldr.Append("'");
						}
					}
				}
				pathBldr.Append("]");
			}

			return doc["Main"].SelectSingleNode(pathBldr.ToString());
		}

		/// <summary>
		/// Get the node (if any) that has the specified element name and the specified value
		/// for each of the attributes listed in KeyAttributes and has the 'base' attribute.
		/// That is, this is the node that originally defined the specified element,
		/// typically containing overrides but not the complete definition of the element
		/// that will be looked up by GetElement.
		///
		/// Note that a key may produce a result in GetElement, but produce null here, if it
		/// was fully defined directly and not by derivation.
		///
		/// Review: possibly only things from the last directory loaded show up here?
		/// </summary>
		/// <param name="elementName"></param>
		/// <param name="attrvals"></param>
		/// <returns></returns>
		public XmlNode GetAlteration(string elementName, string[] attrvals)
		{
			return GetEltFromDoc(elementName, attrvals, m_alterationsDoc);
		}

		/// <summary>
		/// Get the node (if any) that was used as the base for the specified derived element.
		///
		/// Note that a key may produce a result in GetElement, but produce null here, if it
		/// was fully defined directly and not by derivation.
		///
		/// Exactly the keys that produce a value in GetDerived will produce one here.
		/// </summary>
		/// <param name="elementName"></param>
		/// <param name="attrvals"></param>
		/// <returns></returns>
		public XmlNode GetBase(string elementName, string[] attrvals)
		{
			XmlNode alteration = GetAlteration(elementName, attrvals);
			if (alteration == null)
				return null;
			string[] keyBase = (string[])attrvals.Clone();
			keyBase[keyBase.Length - 1] = XmlUtils.GetMandatoryAttributeValue(alteration, "base");
			// if the alteration is an override (key = id), the base node is saved in m_baseDoc,
			// otherwise it is just in the normal main document.
			string[] keyAttrs = m_keyAttrs[elementName];
			string id = XmlUtils.GetOptionalAttributeValue(alteration, keyAttrs[keyAttrs.Length - 1]);
			if (id == keyBase[keyBase.Length - 1])
			{
				// An override, the base should already be saved in m_baseDoc
				return GetEltFromDoc(elementName, keyBase, m_baseDoc);
			}
			else
			{
				// not an override, just an ordinary derived node.
				// Possibly the base is another derived node, not yet computed, so we need
				// to use the full GetElement to ensure that it gets created if needed.
				return GetElement(elementName, keyBase);
			}
		}

		/// <summary>
		/// Load the templates again. Useful when you are working on template writing,
		/// and don't want to have to restart the application to test something you have done.
		/// </summary>
		public void Reload()
		{
			LoadElements();
		}

		/// <summary>
		/// Reload if any of the files we depend on changed or new ones were added.
		/// </summary>
		public void ReloadIfChanges()
		{
			if (NoFilesChanged())
				return;
			Reload();
		}

		/// <summary>
		/// Get all of the elements in the given file that match our path.
		/// </summary>
		/// <param name="inventoryFilePath">Path to one inventory file.</param>
		/// <returns>A list of elements which should be inventory elements.</returns>
		protected XmlNodeList LoadOneInventoryFile(string inventoryFilePath)
		{
			m_fileInfo.Add(new KeyValuePair<string, DateTime>(inventoryFilePath, File.GetLastWriteTime(inventoryFilePath)));

			XmlDocument xdoc = new XmlDocument();
			try
			{
				xdoc.Load(inventoryFilePath);
			}
			catch(Exception error)
			{
				string x = string.Format(xCoreStrings.ErrorReadingXMLFile0, inventoryFilePath);
				throw new ApplicationException(x, error);

			}
			return xdoc.SelectNodes(m_xpathElementsWanted);
		}

		/// <summary>
		/// This method will save the given Nodes into the requested path, first it will remove all the existing nodes which match
		/// the type that this inventory is representing. Then it will add the new data to the parent node of the first match.
		/// </summary>
		/// <param name="inventoryFilePath"></param>
		/// <param name="newData"></param>
		protected void RefreshOneInventoryFile(string inventoryFilePath, List<XmlNode> newData)
		{
			XmlDocument xdoc = new XmlDocument();
			try
			{
				xdoc.Load(inventoryFilePath);
			}
			catch (Exception error)
			{
				string x = string.Format(xCoreStrings.ErrorReadingXMLFile0, inventoryFilePath);
				throw new ApplicationException(x, error);

			}
			var oldAndBusted = xdoc.SelectNodes(m_xpathElementsWanted);
			if (oldAndBusted != null && oldAndBusted.Count > 0)
			{
				//get the parent of the nodes in the path
				XmlElement root = oldAndBusted[0].ParentNode as XmlElement;
				// In Mono, changing the root element invalidates the XmlNodeList iterator
				// so that it quits the loop immediately after the first node is removed.
				// See FWNX-1057.  This results in evergrowing fwlayout files stored with
				// the project!  So we copy the list first into a form that won't be
				// invalidated.
				foreach (XmlNode match in new List<XmlNode>(oldAndBusted.Cast<XmlNode>()))
				{
					root.RemoveChild(match);
				}
				foreach (XmlNode newItem in newData)
				{
					root.AppendChild(root.OwnerDocument.ImportNode(newItem, true));
				}
				xdoc.Save(inventoryFilePath);
			}
		}

		/// <summary>
		/// Add custom files after the main loading.
		/// </summary>
		/// <param name="filePaths"></param>
		public void AddCustomFiles(string[] filePaths)
		{
			for(int i = 0; i < filePaths.Length; ++i)
			{
				string path = filePaths[i];
				if (m_inventoryPaths.Contains(path))
				{
					// Already in the path collection.
					filePaths[i] = null;
					continue;
				}
				if (Directory.Exists(path))
				{
					m_inventoryPaths.Add(path);
					AddElementsFromFiles(DirectoryUtils.GetOrderedFiles(path, m_filePattern));
				}
			}
		}

		protected void AddElementsFromFiles(string[] filePaths)
		{
			AddElementsFromFiles(filePaths, m_version, false);
		}

		/// <summary>
		/// Collect all of the elements up from an array of files.
		/// </summary>
		/// <param name="filePaths">Collection of pathnames to individual XDE template files.</param>
		/// <param name="loadUserOverRides">set to true if the version attribute needs to be added to elements in the configuration file.</param>
		/// <remarks>
		/// tests over in XMLVIews need access to this method.
		/// </remarks>
		public void AddElementsFromFiles(IEnumerable<string> filePaths, int version, bool loadUserOverRides)
		{
			Debug.Assert(filePaths != null);
			Debug.Assert(m_mainDoc != null);
			XmlNode root = m_mainDoc["Main"];

			foreach(string path in filePaths)
			{
				if (path == null)
					continue;
				var nodeList = LoadOneInventoryFile(path);
				bool wasMerged;
				List<XmlNode> cleanedNodes = MergeAndUpdateNodes(nodeList, version, out wasMerged, loadUserOverRides);
				LoadNodeList(cleanedNodes, version, root);
				if (wasMerged)
				{
					RefreshOneInventoryFile(path, cleanedNodes);
				}
			}
		}

		/// <summary>
		/// Collect all of the elements up from a string input (used in testing).
		/// </summary>
		protected void AddElementsFromString(string input, int version)
		{
			Debug.Assert(m_mainDoc != null);
			XmlNode root = m_mainDoc["Main"];
			var xdoc = new XmlDocument();
			xdoc.LoadXml(input);
			var nodeList = xdoc.SelectNodes(m_xpathElementsWanted);
			bool wasMerged;
			List<XmlNode> cleanedNodes = MergeAndUpdateNodes(nodeList, version, out wasMerged, false);
			LoadNodeList(cleanedNodes, version, root);
		}

		/// <summary>
		/// Take the collection of merged or retained nodes and insert them into our in memory document.
		/// </summary>
		/// <param name="nodeList"></param>
		/// <param name="version"></param>
		/// <param name="root"></param>
		private void LoadNodeList(List<XmlNode> nodeList, int version, XmlNode root)
		{
			foreach (XmlNode node in nodeList)
			{
				NoteIfNodeWsTagged(node);
				//"layoutType" nodes are handled differently in the Inventory.
				//Don't ask me why, the reason seems to have been lost to history.
				//It is important to follow through on that here though. naylor 6/7/2012
				if (node.Name == "layoutType")
				{
					AddLayoutTypeToInventory(node);
					continue;
				}
				//if the version is the same then we want to use the basic AddNode to get it into the inventory
				if(version == Int32.Parse(XmlUtils.GetOptionalAttributeValue(node, "version", version.ToString())))
				{
					AddNode(node, root);
					continue;
				}
				//set up the values for other options
				string[] keyAttrs;
				var key = GetKeyMain(node, out keyAttrs);
				XmlNode current;
				//if the element table already has a match then we want to insert replacing the current value
				if (m_getElementTable.TryGetValue(key, out current))
				{
					InsertNodeInDoc(node, current, m_mainDoc, key);
				}
				else //otherwise we are not going to attempt and replace any existing configurations
				{
					// We do NOT want this one to replace 'current', since it has a different name.
					// We already know there is no matching node to replace.
					InsertNodeInDoc(node, null, m_mainDoc, key);
				}
			}
		}

		/// <summary>
		/// This comparison class allows us to simplify getting only unique elements
		/// in the MergeAndUpdateNodes method below.
		/// </summary>
		class XmlNodeCompare : IEqualityComparer<XmlNode>
		{
			/// <summary></summary>
			public bool Equals(XmlNode first, XmlNode second)
			{
				return first.OuterXml == second.OuterXml;
			}

			/// <summary></summary>
			public int GetHashCode(XmlNode x)
			{
				return x.OuterXml.GetHashCode();
			}
		}

		/// <summary>
		/// This method will return a list of merged and unchanged nodes from the given list.
		/// </summary>
		/// <param name="nodeList"></param>
		/// <param name="version"></param>
		/// <param name="wasMerged">This parameter will be set to true if any nodes were merged to the latest version</param>
		/// <param name="loadUserOverRides">set to true if the version attribute needs to be added to elements in the configuration file.</param>
		/// <returns></returns>
		private List<XmlNode> MergeAndUpdateNodes(XmlNodeList nodeList, int version, out bool wasMerged, bool loadUserOverRides)
		{
			wasMerged = false;
			// Past bugs may have left duplication in the layout file.  (See FWNX-1057.)  Use a simple
			// check to weed out any duplicates, which will be exactly identical.
			var comparer = new XmlNodeCompare();
			HashSet<XmlNode> survivors = new HashSet<XmlNode>(comparer);
			foreach(XmlNode node in nodeList)
			{
				// Load only nodes that either have matching version number or none.
				// JohnT says that all user files will have a version number, and have had one from the beginning.
				// None of our installer provided files have a version number.
				// That is why we check for an optional version attribute.

				// LT-12778 revealed a defect related to version number. In previous versions of FieldWorks the version attribute
				// was not being added to the user configuration files. Therefore many layout elements were being processed
				// as if they were in the default configuration files.  The loadUserOverRides boolean was added to ensure
				// version number is added to the elements in the user configuration files.

				int fileVersion = Int32.Parse(XmlUtils.GetOptionalAttributeValue(node, "version",
																				 loadUserOverRides ? "0" : version.ToString()));

				//The layoutType element in user config files (i.e. a copy of the root dictionary view)
				//failed to include a version number, even though all the layout elements had one.
				if (fileVersion == 0)
				{
					//So to make sure that copies of the dictionary views get reloaded we will look for
					//the version number of a sibling node.
					if (node.ParentNode != null)
					{
						var versionedSibling = node.SelectSingleNode("../*[@version]");
						// ReSharper disable PossibleNullReferenceException
						// if we found a node, it has a version attribute.
						fileVersion = Int32.Parse(versionedSibling != null ? versionedSibling.Attributes["version"].Value : "0");
						// ReSharper restore PossibleNullReferenceException
					}
				}

				if (fileVersion != version && Merger != null && XmlUtils.GetOptionalAttributeValue(node, "base") == null)
				{
					if (node.Name == "layoutType")
					{
						AddLayoutTypeToInventory(node);
						if (loadUserOverRides)
							XmlUtils.SetAttribute(node, "version", version.ToString(CultureInfo.InvariantCulture));
						survivors.Add(node);
						wasMerged = true;
					}
					else
					{
						string[] keyAttrs;
						var key = GetKeyMain(node, out keyAttrs);
						XmlNode current;
						if (m_getElementTable.TryGetValue(key, out current))
						{
							XmlNode merged = Merger.Merge(current, node, m_mainDoc, string.Empty);
							if (loadUserOverRides)
								XmlUtils.SetAttribute(merged, "version", version.ToString());
							survivors.Add(merged);
							wasMerged = true;
						}
						else
						{
							// May be part of a named view or a duplicated node. Look for the unmodified one to merge with.
							string[] standardKeyVals;
							var oldLayoutSuffix = LayoutKeyUtils.GetSuffixedPartOfNamedViewOrDuplicateNode(keyAttrs, key.KeyVals, out standardKeyVals);
							if (!string.IsNullOrEmpty(oldLayoutSuffix))
							{
								var originalKey = new GetElementKey(key.ElementName, standardKeyVals, m_mainDoc);
								if (m_getElementTable.TryGetValue(originalKey, out current))
								{
									XmlNode merged = Merger.Merge(current, node, m_mainDoc, oldLayoutSuffix);
									// We'll do the below and a bunch of other mods inside of LayoutMerger from now on.
									//XmlUtils.SetAttribute(merged, "name", originalKey[2]); // give it the name from before
									if (loadUserOverRides)
										XmlUtils.SetAttribute(merged, "version", version.ToString(CultureInfo.InvariantCulture));
									survivors.Add(merged);
									wasMerged = true;
								}
							}
						}
					}
				}
				else if (fileVersion == version)
				{
					survivors.Add(node);
				}
				// Otherwise it's an old-version node and we can't merge it, so ignore it.
				// we should remove obsolete nodes to indicate the merge so don't add it to survivors
			}
			return survivors.ToList();
		}

		private void NoteIfNodeWsTagged(XmlNode node)
		{
			if (node.Name == "layout" &&
				XmlUtils.GetMandatoryAttributeValue(node, "type") == "jtview" &&
				XmlUtils.GetOptionalBooleanAttributeValue(node, "tagForWs", false))
			{
				m_wsTaggedNodes.Add(node);
			}
		}

		/// <summary>
		/// Displaying Reversal Indexes requires expanding a variable number of writing
		/// system specific layouts.  This method does that.
		/// </summary>
		/// <param name="sWsTag"></param>
		public void ExpandWsTaggedNodes(string sWsTag)
		{
			Debug.Assert(sWsTag != null && sWsTag.Length > 0);
			Debug.Assert(m_mainDoc != null);
			XmlNode root = m_mainDoc["Main"];

			foreach (XmlNode xn in m_wsTaggedNodes)
			{
				string sName = XmlUtils.GetMandatoryAttributeValue(xn, "name");
				string sWsName = String.Format("{0}-{1}", sName, sWsTag);
				string sClass = XmlUtils.GetMandatoryAttributeValue(xn, "class");
				string sType = XmlUtils.GetMandatoryAttributeValue(xn, "type");
				Debug.Assert(xn.Name == "layout" && sType == "jtview" &&
					XmlUtils.GetOptionalBooleanAttributeValue(xn, "tagForWs", false));
				XmlNode layout = GetElement("layout", new[] { sClass, sType, sWsName, null });
				if (layout != null)
					continue;		// node has already been added.
				XmlNode xnWs = xn.Clone();
				xnWs.Attributes["name"].Value = sWsName;
				foreach (XmlNode xnChild in xnWs.ChildNodes)
				{
					if (xnChild is XmlComment)
						continue;
					if (xnChild.Name == "sublayout")
					{
						string sSubName = XmlUtils.GetMandatoryAttributeValue(xnChild, "name");
						xnChild.Attributes["name"].Value = String.Format("{0}-{1}", sSubName, sWsTag);
					}
					else if (xnChild.Name == "part")
					{
						string sParam = XmlUtils.GetOptionalAttributeValue(xnChild, "param", null);
						if (!String.IsNullOrEmpty(sParam))
							xnChild.Attributes["param"].Value = String.Format("{0}-{1}", sParam, sWsTag);
					}
				}
				AddNode(xnWs, root);
			}
		}

		private void AddNode(XmlNode node, XmlNode root)
		{
			string[] keyAttrs;
			var keyMain = GetKeyMain(node, out keyAttrs);
			string[] keyVals = keyMain.KeyVals;
			string elementName = keyMain.ElementName;

			XmlNode extantNode = null;
			// Value may be null in the Dictionary, even if key is present.
			m_getElementTable.TryGetValue(keyMain, out extantNode);

			// Is the current node a derived node?
			string baseName = XmlUtils.GetOptionalAttributeValue(node, "base");
			if (baseName != null)
			{
				string id = XmlUtils.GetMandatoryAttributeValue(node, keyAttrs[keyAttrs.Length - 1]);
				if (id == baseName)
				{
					// it is an override.
					if (extantNode == null)
					{
						// Possibly trying to override a derived element?
						extantNode = GetElement(elementName, keyVals);
						if (extantNode == null)
							throw new Exception("no base found to override " + baseName);
					}
					var keyBase = new GetElementKey(elementName, keyVals, m_baseDoc);
					if (m_getElementTable.ContainsKey(keyBase))
						throw new Exception("only one level of override is allowed " + baseName);
					// Save the base node for future use.
					m_baseDoc["Main"].AppendChild(m_baseDoc.ImportNode(extantNode, true));
					m_getElementTable[keyBase] = extantNode;
					// Immediately compute the effect of the override and save it, replacing the base.
					XmlNode unified = Unify(node, extantNode);
					root.ReplaceChild(unified, extantNode);
					// and update the cache, which is loaded with the old element
					m_getElementTable[keyMain] = unified;
				}
				else
				{
					// it is a normal alteration node
					if (extantNode != null)
					{
						// derived node displaces non-derived one.
						root.RemoveChild(extantNode);
					}
				}
				// alteration node goes into alterations doc (displacing any previous alteration
				// with the same key).
				var keyAlterations = new GetElementKey(elementName, keyVals, m_alterationsDoc);
				extantNode = null;
				if (m_getElementTable.ContainsKey(keyAlterations))
					extantNode = m_getElementTable[keyAlterations]; // May still be null.
				CopyNodeToDoc(node, extantNode, m_alterationsDoc, keyAlterations);
			}
			else // not an override, just save it, replacing existing node if needed
			{
				CopyNodeToDoc(node, extantNode, m_mainDoc, keyMain);
			}
		}

		// Get the key we use to look up the specified element, and also the array of attribute names
		// appropriate to the element type.
		private GetElementKey GetKeyMain(XmlNode node, out string[] keyAttrs)
		{
			string elementName = node.Name;
			keyAttrs = null;
			if (m_keyAttrs.ContainsKey(elementName))
				keyAttrs = m_keyAttrs[elementName];
			if (keyAttrs == null)
				keyAttrs = new string[0];
			string[] keyVals = new string[keyAttrs.Length];
			int i = 0;
			foreach(string attr in keyAttrs)
			{
				string val = XmlUtils.GetOptionalAttributeValue(node, attr);
				keyVals[i++] = val;
			}
			return new GetElementKey(elementName, keyVals, m_mainDoc);
		}

		private void CopyNodeToDoc(XmlNode node, XmlNode extantNode, XmlDocument doc, GetElementKey key)
		{
			XmlNode newNode = doc.ImportNode(node, true);
			InsertNodeInDoc(newNode, extantNode, doc, key);
		}

		private void InsertNodeInDoc(XmlNode newNode, XmlNode extantNode, XmlDocument doc, GetElementKey key)
		{
			XmlNode root = doc["Main"];
			if (extantNode == null)
				root.AppendChild(newNode);
			else
				root.ReplaceChild(newNode, extantNode);
			if (newNode.Name != "layoutType")
				m_getElementTable[key] = newNode;
		}

		/// <summary>
		/// Collect all of the elements from wherever they come from.
		/// </summary>
		protected void LoadElements()
		{
			BasicInit();

			foreach(string inventoryPath in m_inventoryPaths)
			{
				//jdh added dec 2003
				string p = FwDirectoryFinder.GetCodeSubDirectory(inventoryPath);

				if (Directory.Exists(p))
					AddElementsFromFiles(DirectoryUtils.GetOrderedFiles(p, m_filePattern));
			}
		}

		/// <summary>
		/// Collect all of the elements from a specific input string (only). Used in tests.
		/// </summary>
		public void LoadElements(string input, int version)
		{
			BasicInit();

			AddElementsFromString(input, version);
		}

		private void BasicInit()
		{
			// If this is called more than once,
			// it will throw away the old xmlDocument here.
			m_fileInfo = new List<KeyValuePair<string, DateTime>>();
			m_mainDoc = new XmlDocument();
			m_mainDoc.AppendChild(m_mainDoc.CreateElement("Main"));
			m_alterationsDoc = new XmlDocument();
			m_alterationsDoc.AppendChild(m_alterationsDoc.CreateElement("Main"));
			m_baseDoc = new XmlDocument();
			m_baseDoc.AppendChild(m_baseDoc.CreateElement("Main"));
			m_getElementTable.Clear();
		}

		/// <summary>
		/// Answer true if no files we load changed since last load.
		/// </summary>
		/// <returns></returns>
		bool NoFilesChanged()
		{
			int ifile = 0;
			foreach(string inventoryPath in m_inventoryPaths)
			{
				//jdh added dec 2003
				string p = FwDirectoryFinder.GetCodeSubDirectory(inventoryPath);

				if (Directory.Exists(p))
				{
					foreach (string path in DirectoryUtils.GetOrderedFiles(p, m_filePattern))
					{
						if (ifile >= m_fileInfo.Count || m_fileInfo[ifile].Key != path
							|| m_fileInfo[ifile].Value != File.GetLastWriteTime(path))
						{
							return false;
						}
						ifile++;
					}
				}
			}
			return ifile == m_fileInfo.Count;
		}

		/// <summary>
		/// This routine combines two elements by copying the attributes of main,
		/// and unifying the children of the two nodes in the way we do for
		/// overriding. It saves the result and will reuse it if a unification
		/// of the same two elements is requested again.
		/// </summary>
		/// <param name="main"></param>
		/// <param name="alteration"></param>
		/// <returns></returns>
		public XmlNode GetUnified(XmlNode main, XmlNode alteration)
		{
			if (main.SelectNodes("part[@ref='AsLexemeForm' and @shouldNotMerge='true']").Count > 0)
				return main;
			XmlNode result;
			var key = new Tuple<XmlNode, XmlNode>(main, alteration);
			if (!m_unifiedNodes.TryGetValue(key, out result))
			{
				result = m_mainDoc.CreateNode(XmlNodeType.Element, main.Name, null);
				CopyAttributes(main, result, false);
				UnifyChildren(alteration, main, result);
				m_unifiedNodes[key] = result;
			}
			return result; // It will not be null.
		}

		/// <summary>
		/// Key used in Dictionary to optimize GetElementFromDoc.
		/// </summary>
		internal class GetElementKey
		{
			readonly string m_elementName;
			readonly string[] m_attrvals;
			readonly XmlDocument m_doc;
			internal GetElementKey(string elementName, string[] attrvals, XmlDocument doc)
			{
				m_elementName = elementName;
				m_attrvals = attrvals.Select(attrval => (attrval == null ? null : attrval.ToLowerInvariant())).ToArray();

				m_doc = doc;
			}

			public override bool Equals(object obj)
			{
				var other = obj as GetElementKey;

				if (other == null)
					return false;
				if (other.m_elementName != m_elementName)
					return false;
				if (other.m_doc != m_doc)
					return false;
				if (other.m_attrvals.Length != m_attrvals.Length)
					return false;
				for (int i = 0; i < m_attrvals.Length; i++)
					if (other.m_attrvals[i] != m_attrvals[i])
						return false;
				return true;
			}

			public string ElementName { get { return m_elementName; } }
			public string[] KeyVals { get { return m_attrvals; } }

			static int HashZeroForNull(object obj)
			{
				if (obj == null)
					return 0;
				return obj.GetHashCode();
			}

			public override int GetHashCode()
			{
				int result = HashZeroForNull(m_doc) + HashZeroForNull(m_elementName);
				foreach (string s in m_attrvals)
				{
					result += HashZeroForNull(s);
				}
				return result;
			}

			public override string ToString()
			{
				var bldr = new StringBuilder();
				if (!string.IsNullOrEmpty(m_elementName))
					bldr.AppendFormat("{0}: ", m_elementName);
				if (m_attrvals != null)
					bldr.Append(string.Join("-", m_attrvals));
				return bldr.Length > 0 ? bldr.ToString() : base.ToString();
			}
		}

		public ISet<string> ExistingDuplicateKeys()
		{
			var set = new HashSet<string>();
			foreach (var dupKey in
				from key in m_getElementTable.Keys select key.ElementName into elemName
				let idx = elemName.IndexOf('%') where idx >= 0
				select elemName.Substring(idx + 1) into dupKey
				where !String.IsNullOrEmpty(dupKey) && !set.Contains(dupKey) select dupKey)
			{
				set.Add(dupKey);
			}
			return set;
		}

		/// <summary>
		/// We want to override an attribute of the last part ref in the part, for the
		/// layout that is the first element.
		/// To do so, we build a modified version of that layout, which (for each part ref in path)
		/// has a child node of that name. The final part ref should have the modified attribute.
		/// </summary>
		/// <param name="path">The path.</param>
		/// <param name="attrName">Name of the attr.</param>
		/// <param name="value">The value.</param>
		/// <param name="version">The version.</param>
		/// <param name="newPartRef">The new part ref.</param>
		/// <returns></returns>
		static public XmlNode MakeOverride(object[] path, string attrName, string value, int version, out XmlNode newPartRef)
		{
			// if we are overriding an attribute in a part ref that is in a sublayout, we want to treat the sublayout as the
			// root layout, so we search starting from the end of the path for the last sublayout and if it exists we make it
			// the root layout
			int i;
			for (i = path.Length - 1; i > 0; i--)
			{
				var node = path[i] as XmlNode;
				if (node == null || node.Name != "sublayout")
					continue;

				i++;
				break;
			}
			var original = (XmlNode) path[i++];
			XmlNode result = original.Clone();
			XmlUtils.AppendAttribute(result, "version", version.ToString());
			XmlNode finalPartRef = null;
			XmlNode currentParent = result;
			for (; i < path.Length; i++)
			{
				var node = path[i] as XmlNode;
				if (node == null || node.Name != "part")
					continue;
				string partId = XmlUtils.GetOptionalAttributeValue(node, "ref");
				if (partId == null)
					continue; // a part node, but not a part ref.

				// Handle the possibility that the parent of the part ref we want to override
				// is not directly the containing part ref. For now we just handle the special case,
				// <indent>. If the current part ref we are trying to add has such a parent,
				// try to reuse a matching one from the currentParent; failing that, make
				// a suitable parent node and make it current.
				XmlNode parent = node.ParentNode;
				if (parent != null && parent.Name == "indent")
				{
					XmlNode adjustParent = null;
					foreach (XmlNode child in currentParent.ChildNodes)
					{
						if (child.Name == parent.Name)
						{
							adjustParent = child;
							break;
						}
					}
					if (adjustParent == null)
					{
						adjustParent = currentParent.OwnerDocument.ImportNode(parent, false);
						currentParent.AppendChild(adjustParent);
					}
					currentParent = adjustParent;
				}

				XmlNode currentChild = null;
				foreach (XmlNode child in currentParent.ChildNodes)
				{
					string partIdChild = XmlUtils.GetOptionalAttributeValue(child, "ref");
					// For most children, one with the right part ID is enough, but for custom ones
					// it must have the right param, as well.
					if (partIdChild == partId &&
						(partIdChild != "Custom" ||
							XmlUtils.GetOptionalAttributeValue(node, "param") == XmlUtils.GetOptionalAttributeValue(child, "param")))
					{
						currentChild = child;
						break;
					}
				}
				if (currentChild == null)
				{
					// It's not one of the children of the current parent, presumably, it's a parent
					// of a layout invoked by that parent. We create a direct child to override the
					// behavior of that layout.
					currentChild = currentParent.OwnerDocument.CreateNode(XmlNodeType.Element, "part", null);
					currentParent.AppendChild(currentChild);
					XmlUtils.AppendAttribute(currentChild, "ref", partId);
					if (XmlUtils.GetOptionalAttributeValue(node, "ref") == "Custom")
					{
						// In this case (and possibly this case only, at least, we weren't doing it
						// before), we need to copy the param attribute.
						string param = XmlUtils.GetOptionalAttributeValue(node, "param");
						if (!string.IsNullOrEmpty(param))
							XmlUtils.AppendAttribute(currentChild, "param", param);
					}
				}

				finalPartRef = currentChild;
				currentParent = currentChild; // if we continue we want a child of this next.
			}
			Debug.Assert(finalPartRef != null);
			XmlUtils.AppendAttribute(finalPartRef, attrName, value);

			newPartRef = finalPartRef;

			return result;
		}
	}

	/// <summary>
	/// This interface (the only current implementation is XmlViews.LayoutMerger) is used when we find an old version
	/// of an inventory element while loading user overrides. It is used only when there is a current element with
	/// the same key. It is passed the current element, the one 'wanted' (the old version), and the destination
	/// document in which a merged element should be created and returned.
	/// Enhance JohnT: We could pass null for current if there is no current node with that key. We could allow
	/// returning null if no merge is possible.
	/// </summary>
	public interface IOldVersionMerger
	{
		/// <summary>
		/// Do the merge.
		/// </summary>
		XmlNode Merge(XmlNode newMaster, XmlNode oldConfigured, XmlDocument dest, string oldLayoutLevelSuffix);
	}
}
