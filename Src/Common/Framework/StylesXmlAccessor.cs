// Copyright (c) 2007-2018 SIL International
// This software is licensed under the LGPL, version 2.1 or later
// (http://www.gnu.org/licenses/lgpl-2.1.html)
//
// File: StylesXmlAccessor.cs
// Responsibility: FieldWorks Team

using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.Drawing;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Xml;
using SIL.LCModel.Core.Text;
using SIL.LCModel.Core.KernelInterfaces;
using SIL.LCModel;
using SIL.LCModel.DomainServices;
using SIL.FieldWorks.FwCoreDlgControls;
using SIL.FieldWorks.Resources;
using SIL.LCModel.Infrastructure;
using SIL.LCModel.Utils;
using SIL.Utils;

namespace SIL.FieldWorks.Common.Framework
{
	/// <summary>
	/// A class that supports having a collection of factory styles defined in an XML file.
	/// A static method can be called to update the database styles if they are out of date
	/// with respect to the file.
	/// </summary>
	public abstract class StylesXmlAccessor : SettingsXmlAccessorBase
	{
		#region Data members
		/// <summary>The FDO cache (must not be null)</summary>
		protected readonly LcmCache m_cache;
		/// <summary>The progress dialog (may be null)</summary>
		protected IProgress m_progressDlg;
		/// <summary>The XmlNode from which to get the style info</summary>
		protected XmlNode m_sourceStyles;
		/// <summary>Styles to be renamed</summary>
		protected Dictionary<string, string> m_styleReplacements = new Dictionary<string, string>();
		/// <summary>Array of styles that the user has modified</summary>
		protected List<string> m_userModifiedStyles = new List<string>();
		/// <summary>Collection of styles in the DB</summary>
		protected ILcmOwningCollection<IStStyle> m_databaseStyles;
		Dictionary<IStStyle, IStStyle> m_replacedStyles = new Dictionary<IStStyle, IStStyle>();

		/// <summary>Dictionary of style names to StStyle objects representing the initial
		/// collection of styles in the DB</summary>
		protected Dictionary<string, IStStyle> m_htOrigStyles = new Dictionary<string, IStStyle>();
		/// <summary>
		/// Dictionary of style names to StStyle objects representing the collection of
		/// styles that should be factory styles in the DB (i.e., any factory styles in the
		/// original Dictionary that are not also in this Dictionary need to be removed or turned
		/// into user-defined styles).
		/// </summary>
		protected Dictionary<string, IStStyle> m_htUpdatedStyles = new Dictionary<string, IStStyle>();

		/// <summary>
		/// Maps from style name to ReservedStyleInfo.
		/// </summary>
		protected Dictionary<string, ReservedStyleInfo> m_htReservedStyles = new Dictionary<string, ReservedStyleInfo>();

		/// <summary>
		/// This indicates if the style file being imported contains ALL styles, or if it should be considered a partial set.
		/// If it is a partial set we don't want to delete the missing styles.
		/// </summary>
		private bool m_deleteMissingStyles;

		#endregion

		#region OverwriteOptions enum
		/// <summary>
		/// Options to indicate how to deal with user-modified styles.
		/// </summary>
		protected enum OverwriteOptions
		{
			/// <summary>
			/// Do not overwrite any properties of the user-modified style
			/// </summary>
			Skip,
			/// <summary>
			/// Overwrite only the functional properties of the user-modified style,
			/// not the properties that merely affect appearance.
			/// </summary>
			FunctionalPropertiesOnly,
			/// <summary>
			/// Overwrite all the properties of the user-modified style
			/// </summary>
			All,
		}
		#endregion

		#region Constructor
		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Initializes a new instance of the <see cref="StylesXmlAccessor"/> class. This
		/// version creates a pretty worthless accessor, but can be used to, for example,
		/// get a help topic out of the style file without needing a cache.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		protected StylesXmlAccessor()
		{
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Constructor is protected so only derived classes can create an instance
		/// </summary>
		/// ------------------------------------------------------------------------------------
		protected StylesXmlAccessor(LcmCache cache)
		{
			if (cache == null) throw new ArgumentNullException("cache");

			m_cache = cache;
		}

		#endregion

		#region Abstract properties
		/// <summary>
		/// The collection that owns the styles; for example, Scripture.StylesOC.
		/// </summary>
		protected abstract ILcmOwningCollection<IStStyle> StyleCollection { get; }

		#endregion

		#region Properties
		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Gets the required DTD version.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		protected override string DtdRequiredVersion
		{
			get { return "1610190E-D7A3-42D7-8B48-C0C49320435F"; }
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// The name of the root element in the XmlDocument that contains the styles.
		/// May actually be an arbitrary XPath that selectes the root element that has the
		/// DTDVer attribute and contains the "markup" element.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		protected override string RootNodeName
		{
			get { return "Styles"; }
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// List of user-modified styles.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		protected virtual List<string> UserModifiedStyles
		{
			get { return m_userModifiedStyles; }
		}
		#endregion

		#region Overridden methods
		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Gets a GUID based on the version attribute node.
		/// </summary>
		/// <param name="baseNode">The node containing the markup node</param>
		/// <returns>A GUID based on the version attribute node</returns>
		/// ------------------------------------------------------------------------------------
		protected override Guid GetVersion(XmlNode baseNode)
		{
			return base.GetVersion(GetMarkupNode(baseNode));
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Gets the markup node from the given root node.
		/// </summary>
		/// <param name="rootNode">The root node.</param>
		/// ------------------------------------------------------------------------------------
		public static XmlNode GetMarkupNode(XmlNode rootNode)
		{
			return rootNode.SelectSingleNode("markup");
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Process the resources (e.g., create styles or add publication info).
		/// </summary>
		/// <param name="dlg">The progress dialog manager.</param>
		/// <param name="doc">The loaded XML document that has the settings.</param>
		/// ------------------------------------------------------------------------------------
		protected override void ProcessResources(IThreadedProgress dlg, XmlNode doc)
		{
			dlg.RunTask(CreateStyles, StyleCollection, doc, true);
		}
		#endregion

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Complain if the context is not valid for the tool that is loading the styles.
		/// This default implementation allows anything.
		/// </summary>
		/// <param name="context"></param>
		/// <param name="styleName"></param>
		/// <returns></returns>
		/// ------------------------------------------------------------------------------------
		protected virtual void ValidateContext(ContextValues context, string styleName)
		{
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Load all styles from the XML file and create styles in the database for them.
		/// </summary>
		/// <param name="progressDlg">The progress dialog.</param>
		/// <param name="parameters">The parameters. First parameter is the style objects
		/// (a FdoOwningCollection&lt;IStStyle&gt;), second is the styles (an XmlNode).</param>
		/// ------------------------------------------------------------------------------------
		protected object CreateStyles(IProgress progressDlg, params object[] parameters)
		{
			Debug.Assert(parameters.Length == 3);
			m_databaseStyles = (ILcmOwningCollection<IStStyle>)parameters[0];
			m_sourceStyles = (XmlNode)parameters[1];
			m_deleteMissingStyles = (bool)parameters[2];
			m_progressDlg = progressDlg;

			NonUndoableUnitOfWorkHelper.DoUsingNewOrCurrentUOW(m_cache.ActionHandlerAccessor, CreateStyles);

			return null;
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Reset the properties of a StyleInfo to the factory default settings
		/// </summary>
		/// <param name="styleInfo">The style info.</param>
		/// ------------------------------------------------------------------------------------
		protected void ResetProps(StyleInfo styleInfo)
		{
			styleInfo.ResetAllPropertiesToFactoryValues(() =>
				{
					XmlNode styleNode = LoadDoc().SelectSingleNode("markup/tag[@id='" + styleInfo.Name.Replace(" ", "_") + "']");
					SetFontProperties(styleInfo.Name, styleNode,
						(tpt, iVar, iVal) => styleInfo.SetExplicitFontIntProp(tpt, iVal),
						(tpt, sVal) =>
						{
							if (tpt == (int)FwTextPropType.ktptWsStyle)
								styleInfo.ProcessWsSpecificOverrides(sVal);
							else
								throw new InvalidEnumArgumentException("tpt", tpt, typeof(FwTextPropType));
						},
						OverwriteOptions.All);
					if (styleInfo.IsParagraphStyle)
					{
						SetParagraphProperties(styleInfo.Name, styleNode, (tpt, iVar, iVal) =>
							{
								if (!styleInfo.SetExplicitParaIntProp(tpt, iVar, iVal))
									throw new InvalidEnumArgumentException("tpt", tpt, typeof(FwTextPropType));
							},
							(tpt, sVal) =>
							{
								if (tpt == (int)FwTextPropType.ktptWsStyle)
									styleInfo.ProcessWsSpecificOverrides(sVal);
								else
									throw new InvalidEnumArgumentException("tpt", tpt, typeof(FwTextPropType));
							},
							OverwriteOptions.All);
					}
				});
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Create a set of Scripture styles based on the given XML node.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		protected void CreateStyles()
		{
			string label = XmlUtils.GetOptionalAttributeValue(m_sourceStyles, "label", "");
			m_progressDlg.Title = String.Format(ResourceHelper.GetResourceString("kstidCreatingStylesCaption"), label);
			m_progressDlg.Message =
				string.Format(ResourceHelper.GetResourceString("kstidCreatingStylesStatusMsg"),
				string.Empty);
			m_progressDlg.Position = 0;

			// Move all styles from Scripture into LangProject if the Scripture object exists
			MoveStylesFromScriptureToLangProject();

			// Populate hashtable with initial set of styles
			// these are NOT from the *Styles.xml files or from TeStylesXmlAccessor.InitReservedStyles()
			// They are from loading scripture styles in TE tests only.
			foreach (var sty in m_databaseStyles)
				m_htOrigStyles[sty.Name] = sty;

			//Select all styles.
			XmlNode markup = GetMarkupNode(m_sourceStyles);
			XmlNodeList tagList = markup.SelectNodes("tag");

			m_progressDlg.Minimum = 0;
			m_progressDlg.Maximum = tagList.Count * 2;

			// First pass to create styles and set general properties.
			CreateAndUpdateStyles(tagList);

			// Second pass to set up "based-on" and "next" styles
			SetBasedOnAndNextProps(tagList);

			// Third pass to delete (and possibly prepare to rename) any styles that used to be
			// factory styles but aren't any longer
			if (m_deleteMissingStyles)
			{
				DeleteDeprecatedStylesAndDetermineReplacements();
			}

			// Final step is to walk through the DB and relace any retired styles
			ReplaceFormerStyles();

			// Finally, update styles version in database.
			SetNewResourceVersion(GetVersion(m_sourceStyles));
		}

		/// <summary>
		/// Moves styles that were specific to Scripture into the language project. Projects will have just one style sheet
		/// that will be used throughout the project, including imported scripture.
		/// </summary>
		protected void MoveStylesFromScriptureToLangProject()
		{
			IScripture scr = Cache.LangProject.TranslatedScriptureOA;
			if (scr == null)
				return;
			foreach (IStStyle style in scr.StylesOC)
			{
				if (m_databaseStyles.Any(st => st.Name == style.Name))
				{
					// We found a style with the same name as one already in out language project. Just use the one we already have.
					var flexStyle = m_databaseStyles.First(st => st.Name == style.Name);
					DomainObjectServices.ReplaceReferencesWhereValid(style, flexStyle);
					scr.StylesOC.Remove(style);
					continue;
				}
				// Adding the style to our database will automatically remove the style from Scripture.
				m_databaseStyles.Add(style);
			}
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Gets the application specific default font. This is usually the default font, but
		/// in the case of TE it is the body font.
		/// </summary>
		/// <value>The app default font.</value>
		/// ------------------------------------------------------------------------------------
		protected virtual string AppDefaultFont
		{
			get { return StyleServices.DefaultFont; }
		}

		/// <summary>
		/// Return true if this is the 'normal' style that all others inherit from. TE overrides.
		/// </summary>
		/// <param name="styleName"></param>
		/// <returns></returns>
		protected virtual bool IsNormalStyle(string styleName)
		{
			return styleName == "Normal";
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Define reserved styles. By default there is nothing to do.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		protected virtual void InitReservedStyles()
		{
		}

		/// -------------------------------------------------------------------------------------
		/// <summary>
		/// First pass of style creation phase: create any new styles and set/update general
		/// properties
		/// </summary>
		/// <param name="tagList">List of XML nodes representing factory styles to create</param>
		/// -------------------------------------------------------------------------------------
		private void CreateAndUpdateStyles(XmlNodeList tagList)
		{
			InitReservedStyles();

			foreach (XmlNode styleTag in tagList)
			{
				XmlAttributeCollection attributes = styleTag.Attributes;
				string styleName = GetStyleName(attributes);

				// Don't create a style for certain excluded contexts
				ContextValues context = GetContext(attributes, styleName);
				if (IsExcludedContext(context))
					continue;

				m_progressDlg.Step(0);
				m_progressDlg.Message =
					string.Format(ResourceHelper.GetResourceString("kstidCreatingStylesStatusMsg"),
					styleName);

				StyleType styleType = GetType(attributes, styleName, context);
				StructureValues structure = GetStructure(attributes, styleName);
				FunctionValues function = GetFunction(attributes, styleName);
				var atGuid = attributes["guid"];

				if (atGuid == null || String.IsNullOrEmpty(atGuid.Value))
				{
					ReportInvalidInstallation(String.Format(ResourceHelper.GetResourceString("ksNoGuidOnFactoryStyle"), styleName));
				}
				Guid factoryGuid = new Guid(atGuid.Value);

				var style = FindOrCreateStyle(styleName, styleType, context, structure, function, factoryGuid);

				if (m_htReservedStyles.ContainsKey(styleName))
				{
					ReservedStyleInfo info = m_htReservedStyles[styleName];
					info.created = true;
				}

				// set the user level
				style.UserLevel = int.Parse(attributes.GetNamedItem("userlevel").Value);

				// Set the usage info
				foreach (XmlNode usage in styleTag.SelectNodes("usage"))
				{
					int ws = GetWs(usage.Attributes);
					string usageInfo = usage.InnerText;
					if (ws > 0 && !String.IsNullOrEmpty(usageInfo))
						style.Usage.set_String(ws, TsStringUtils.MakeString(usageInfo, ws));
				}

				// If the user has modified the style manually, we don't want to overwrite it
				// with the standard definition.
				// Enhance JohnT: possibly there should be some marker in the XML to indicate that
				// a style has changed so drastically that it SHOULD overwrite the user modifications?
				OverwriteOptions option = OverwriteOptions.All;

				// Get props builder with default Text Properties
				ITsPropsBldr propsBldr = TsStringUtils.MakePropsBldr();
				if (style.IsModified)
				{
					m_userModifiedStyles.Add(style.Name);
					option = OverwriteUserModifications(style, styleTag);
					if (option == OverwriteOptions.Skip)
						continue;
					// Use existing props as starting point.
					if (style.Rules != null)
						propsBldr = style.Rules.GetBldr();
				}

				SetFontProperties(styleName, styleTag,
					((tpt, nVar, nVal) => m_progressDlg.SynchronizeInvoke.Invoke(() => propsBldr.SetIntPropValues(tpt, nVar, nVal))),
					((tpt, sVal) => m_progressDlg.SynchronizeInvoke.Invoke(() => propsBldr.SetStrPropValue(tpt, sVal))),
					option);

				// Get paragraph properties
				if (style.Type == StyleType.kstParagraph)
					SetParagraphProperties(styleName, styleTag,
					((tpt, nVar, nVal) => m_progressDlg.SynchronizeInvoke.Invoke(() => propsBldr.SetIntPropValues(tpt, nVar, nVal))),
					((tpt, sVal) => m_progressDlg.SynchronizeInvoke.Invoke(() => propsBldr.SetStrPropValue(tpt, sVal))),
					option);
				style.Rules = propsBldr.GetTextProps();
			}
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Virtual method to allow subclass to force overwriting of certain styles or types of
		/// style changes, even if the user had modified them.
		/// </summary>
		/// <param name="style">The style.</param>
		/// <param name="styleTag">The style tag.</param>
		/// <returns>OverwriteOptions.Skip to indicate that the caller should not alter the
		/// user-modified style;
		/// OverwriteOptions.FunctionalPropertiesOnly to indicate that the caller should update
		/// the functional properties of the style but leave the user-modified properties that
		/// affect only the appearance;
		/// OverwriteOptions.All to indicate that the caller should proceed with the style
		/// definition update, based on the information in the XML node.</returns>
		/// ------------------------------------------------------------------------------------
		protected virtual OverwriteOptions OverwriteUserModifications(IStStyle style, XmlNode styleTag)
		{
			return OverwriteOptions.Skip;
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Find the existing style if there is one; otherwise, create new style object
		/// These styles are defined in FlexStyles.xml which have fixed guids
		/// All guids of factory styles changed with release 7.3
		/// </summary>
		/// <param name="styleName">Name of style</param>
		/// <param name="styleType">Type of style (para or char)</param>
		/// <param name="context">Context</param>
		/// <param name="structure">Structure</param>
		/// <param name="function">Function</param>
		/// <param name="factoryGuid">The guid for this factory style</param>
		/// <returns>A new or existing style</returns>
		/// ------------------------------------------------------------------------------------
		internal IStStyle FindOrCreateStyle(string styleName, StyleType styleType,
			ContextValues context, StructureValues structure, FunctionValues function,
			Guid factoryGuid)
		{
			IStStyle style;
			bool fUsingExistingStyle = false;
			// EnsureCompatibleFactoryStyle will rename an incompatible user style to prevent collisions,
			// but it is our responsibility to update the GUID on a compatible user style.
			if (m_htOrigStyles.TryGetValue(styleName, out style) && EnsureCompatibleFactoryStyle(style, styleType, context, structure, function))
			{
				// A style with the same name already exists in the project.
				// It may be a user style or a factory style, but it has compatible context, structure, and function.
				if (style.Guid != factoryGuid) // This is a user style; give it the factory GUID and update all usages
				{
					// create a new style with the correct guid.
					IStStyle oldStyle = style; // REVIEW LastufkaM 2012.05: is there a copy constructor?
					style = m_cache.ServiceLocator.GetInstance<IStStyleFactory>().Create(m_cache, factoryGuid);

					// Before we set any data on the new style we should give it an owner.
					// Don't delete the old one yet, though, because we want to copy things from it (and references to it).
					var owningCollection = ((ILangProject) oldStyle.Owner).StylesOC;
					owningCollection.Add(style);

					style.IsBuiltIn = true; // whether or not it was before, it is now.
					style.IsModified = oldStyle.IsModified;
					style.Name = oldStyle.Name;
					style.Type = oldStyle.Type;
					style.Context = oldStyle.Context;
					style.Structure = oldStyle.Structure;
					style.Function = oldStyle.Function;
					style.Rules = oldStyle.Rules;
					style.BasedOnRA = oldStyle.BasedOnRA;
					style.IsPublishedTextStyle = oldStyle.IsPublishedTextStyle;
					style.NextRA = oldStyle.NextRA;
					style.UserLevel = oldStyle.UserLevel;

					// Anywhere the obsolete style object is used (e.g., in BasedOn or Next of other styles),
					// switch to refer to the new one. It's important to do this AFTER setting all the properties,
					// because validation of setting various references to this style depends on some of these properties.
					// (Also, oldNextRA might be oldStyle itself.)
					// It must be done AFTER the new style has an owner, but BEFORE the old one is deleted (and all refs
					// to it go away).
					// In pathological cases this might not be valid (e.g., the old stylesheet may somehow have invalid
					// arrangements of NextStyle). If so, just let those references stay for now (and be cleared when the old style
					// is deleted).
					DomainObjectServices.ReplaceReferencesWhereValid(oldStyle, style);
					owningCollection.Remove(oldStyle);
				}
				m_htUpdatedStyles[styleName] = style; // REVIEW (Hasso) 2017.04: any reason this is shoved in the middle here? Parallel or UOW reasons, perhaps?
			}
			else
			{
				// These factory styles aren't in the project yet.
				// WARNING: Using this branch may create ownerless StStyle objects! Shouldn't be possible!
				style = m_cache.ServiceLocator.GetInstance<IStStyleFactory>().Create(m_cache, factoryGuid);
				m_databaseStyles.Add(style);
				if (style.Owner == null)
					throw new ApplicationException("StStyle objects must be owned!");

				m_htUpdatedStyles[styleName] = style; // REVIEW (Hasso) 2017.04: any reason this is shoved in the middle here? Parallel or UOW reasons, perhaps?

				// Set properties not passed in as parameters
				style.IsBuiltIn = true;
				style.IsModified = false; // not found in our database, so use everything from the XML

				// Set the style name, type, context, structure, and function
				style.Name = styleName;
				style.Type = styleType;
				style.Context = context;
				style.Structure = structure;
				style.Function = function;
			}
			return style;
		}

		#region BasedOn Context, Structure, Function, and type interpreters
		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Interpret the based on attribute
		/// </summary>
		/// <param name="attributes">Collection of attributes that better have a "context"
		/// attribute</param>
		/// <param name="styleName">Stylename being processed (for error reporting purposes)
		/// </param>
		/// <returns>The name of the based-on style</returns>
		/// ------------------------------------------------------------------------------------
		private string GetBasedOn(XmlAttributeCollection attributes, string styleName)
		{
			if (m_htReservedStyles.ContainsKey(styleName))
			{
				return m_htReservedStyles[styleName].basedOn;
			}
			XmlNode basedOn = attributes.GetNamedItem("basedOn");
			return (basedOn == null) ? null : basedOn.Value.Replace("_", " ");
		}

		/// -------------------------------------------------------------------------------------
		/// <summary>
		/// Interpret the context attribute as a ContextValues value
		/// </summary>
		/// <param name="attributes">Collection of attributes that better have a "context"
		/// attribute</param>
		/// <param name="styleName">Stylename being processed (for error reporting purposes)
		/// </param>
		/// <returns>The context of the style</returns>
		/// -------------------------------------------------------------------------------------
		private ContextValues GetContext(XmlAttributeCollection attributes,
			string styleName)
		{
			if (m_htReservedStyles.ContainsKey(styleName))
			{
				return m_htReservedStyles[styleName].context;
			}

			string sContext = attributes.GetNamedItem("context").Value;
			// EndMarker was left out of the original conversion and would have raised an exception.
			if (sContext == "back") sContext = "BackMatter";
			try
			{   // convert the string to a valid enum case insensitive
				return (ContextValues)Enum.Parse(typeof(ContextValues), sContext, true);
			}
			catch (Exception ex)
			{
				Debug.Assert(false, "Unrecognized context attribute for style " + styleName +
					" in " + ResourceFileName + ": " + sContext);
				throw new Exception(ResourceHelper.GetResourceString("kstidInvalidInstallation"));
			}
		}

		/// -------------------------------------------------------------------------------------
		/// <summary>
		/// Interpret the use attribute as a StructureValues value
		/// </summary>
		/// <param name="attributes">Collection of attributes that better have a "structure"
		/// attribute</param>
		/// <param name="styleName">Stylename being processed (for error reporting purposes)
		/// </param>
		/// <returns>The structure of the style</returns>
		/// -------------------------------------------------------------------------------------
		private StructureValues GetStructure(XmlAttributeCollection attributes,
			string styleName)
		{
			if (m_htReservedStyles.ContainsKey(styleName))
			{
				return m_htReservedStyles[styleName].structure;
			}

			XmlNode node = attributes.GetNamedItem("structure");
			string sStructure = (node != null) ? node.Value : null;

			if (sStructure == null)
				return StructureValues.Undefined;

			switch(sStructure)
			{
				case "heading":
					return StructureValues.Heading;
				case "body":
					return StructureValues.Body;
				default:
					Debug.Assert(false, "Unrecognized structure attribute for style " + styleName +
						" in " + ResourceFileName + ": " + sStructure);
					throw new Exception(ResourceHelper.GetResourceString("kstidInvalidInstallation"));
			}
		}

		/// -------------------------------------------------------------------------------------
		/// <summary>
		/// Interpret the use attribute as a FunctionValues value
		/// </summary>
		/// <param name="attributes">Collection of attributes that better have a "use"
		/// attribute</param>
		/// <param name="styleName">Stylename being processed (for error reporting purposes)
		/// </param>
		/// <returns>The function of the style</returns>
		/// -------------------------------------------------------------------------------------
		private FunctionValues GetFunction(XmlAttributeCollection attributes,
			string styleName)
		{
			if (m_htReservedStyles.ContainsKey(styleName))
			{
				return m_htReservedStyles[styleName].function;
			}

			XmlNode node = attributes.GetNamedItem("use");
			string sFunction = (node != null) ? node.Value : null;
			if (sFunction == null)
				return FunctionValues.Prose;

			switch (sFunction)
			{
				case "prose":
				case "proseSentenceInitial":
				case "title":
				case "properNoun":
				case "special":
					return FunctionValues.Prose;
				case "line":
				case "lineSentenceInitial":
					return FunctionValues.Line;
				case "list":
					return FunctionValues.List;
				case "table":
					return FunctionValues.Table;
				case "chapter":
					return FunctionValues.Chapter;
				case "verse":
					return FunctionValues.Verse;
				case "footnote":
					return FunctionValues.Footnote;
				case "stanzabreak":
					return FunctionValues.StanzaBreak;
				default:
					Debug.Assert(false, "Unrecognized use attribute for style " + styleName +
						" in " + ResourceFileName + ": " + sFunction);
					throw new Exception(ResourceHelper.GetResourceString("kstidInvalidInstallation"));
			}
		}

		/// -------------------------------------------------------------------------------------
		/// <summary>
		/// Interpret the type attribute as a StyleType value
		/// </summary>
		/// <param name="attributes">Collection of attributes that better have a "type"
		/// attribute</param>
		/// <param name="styleName">Stylename being processed (for error reporting purposes)
		/// </param>
		/// <param name="context"></param>
		/// <returns>The type of the style</returns>
		/// -------------------------------------------------------------------------------------
		public StyleType GetType(XmlAttributeCollection attributes, string styleName,
			ContextValues context)
		{
			if (m_htReservedStyles.ContainsKey(styleName))
				return m_htReservedStyles[styleName].styleType;
			string sType = attributes.GetNamedItem("type").Value;
			ValidateContext(context, styleName);
			switch(sType)
			{
				case "paragraph":
					ValidateParagraphContext(context, styleName);
					return StyleType.kstParagraph;
				case "character":
					return StyleType.kstCharacter;
				default:
					Debug.Assert(false, "Unrecognized type attribute for style " + styleName +
						" in " + ResourceFileName + ": " + sType);
					throw new Exception(ResourceHelper.GetResourceString("kstidInvalidInstallation"));
			}
		}

		/// <summary>
		/// Throw an exception if the specified context is not valid for the specified paragraph style.
		/// TE overrides to forbid 'general' paragraph styles. This default does nothing.
		/// </summary>
		/// <param name="context"></param>
		/// <param name="styleName"></param>
		protected virtual void ValidateParagraphContext(ContextValues context, string styleName)
		{
		}
		#endregion

		#region Style upgrade stuff

		/// -------------------------------------------------------------------------------------
		/// <summary>
		/// Determine whether the given style is compatible with the given type, context, structure, and function.
		/// If the style is a factory style, and the context, structure, and function can't be adjusted to match, report an invalid installation.
		/// If the style is NOT a factory style, and the context, structure, and function don't all match, rename it to prevent collisions.
		/// If the style is not a factory style, but it is compatible, it is the CLIENT's responsibility to make adjustments.
		/// </summary>
		/// <param name="style">Style to check.</param>
		/// <param name="type">Style type we want</param>
		/// <param name="context">The context we want</param>
		/// <param name="structure">The structure we want</param>
		/// <param name="function">The function we want</param>
		/// <returns>True if the style can be used as-is or redefined as requested; False otherwise</returns>
		/// -------------------------------------------------------------------------------------
		public bool EnsureCompatibleFactoryStyle(IStStyle style, StyleType type,
			ContextValues context, StructureValues structure, FunctionValues function)
		{
			// If this is a bult-in style, but the context or function has changed, update them.
			if (style.IsBuiltIn &&
				(style.Context != context ||
				style.Function != function) &&
				IsValidInternalStyleContext(style, context))
			{   // For now, at least, this method only deals with context changes. Theoretically,
				// we could in the future have a function, type, or structure change that would
				// require some special action.
				ChangeFactoryStyleToInternal(style, context); // only overridden in TeStylesXmlAccessor so far
				if (style.Type != type)
					style.Type = type;
				// Structure and function are probably meaningless for internal styles, but just
				// to be sure...
				if (style.Structure != structure)
					style.Structure = structure;
				if (style.Function != function)
					style.Function = function;
				return true;
			}

			// Handle an incompatible Style by renaming a conflicting User style or reporting an invalid installation for an incompatible built-in style.
			if (style.Type != type ||
				!CompatibleContext(style.Context, context) ||
				style.Structure != structure ||
				!CompatibleFunction(style.Function, function))
			{
				if (style.IsBuiltIn)
					ReportInvalidInstallation(String.Format(
						FrameworkStrings.ksCannotRedefineFactoryStyle, style.Name, ResourceFileName));

				// If style is in use, add it to the list so we can search through all
				// paragraphs and replace it with a new renamed style (and rename the style
				// itself, too);
				if (StyleIsInUse(style))
				{
					// ENHANCE: Prompt user to pick a different name to rename to
					// TODO: Check for collision - make sure we get a unique name
					string sNewName = style.Name +
						ResourceHelper.GetResourceString("kstidUserStyleSuffix");
					m_styleReplacements[style.Name] = sNewName;
					style.Name = sNewName;
				}
				return false;
			}

			// Update context and function as needed
			if (style.Context != context)
				style.Context = context;
			if (style.Function != function)
				style.Function = function;
			return true;
		}

		/// -------------------------------------------------------------------------------------
		/// <summary>
		/// If the proposed context for a style is internal or internalMappable, make sure the
		/// program actually expects and supports this context for this style.
		/// </summary>
		/// <param name="style">The style being updated</param>
		/// <param name="proposedContext">The proposed context for the style</param>
		/// <returns><c>true</c>if the proposed context is internal or internal mappable and
		/// the program recognizes it as a valid</returns>
		/// -------------------------------------------------------------------------------------
		public virtual bool IsValidInternalStyleContext(IStStyle style,
			ContextValues proposedContext)
		{
			// By default we don't recognize any style as 'internal'. TE overrides.
			return false;
		}

		/// -------------------------------------------------------------------------------------
		/// <summary>
		/// Update the style context and do any special processing needed to deal with existing
		/// data that may be marked with the given style. (Since it was previously not an internal
		/// style, it is possible the user has used it in ways that would be incompatible with
		/// its intended use.) Any time a factory style is changed to an internal context,
		/// specific code must be written here to deal with it. Some possible options for dealing
		/// with this scenario are:
		/// * Delete any data previously marked with the style (and possibly set some other
		///   object properties)
		/// * Add to the m_styleReplacements dictionary so existing data will be marked with a
		///   different style (note that this will only work if no existing data should be
		///   preserved with the style).
		/// </summary>
		/// <param name="style">The style being updated</param>
		/// <param name="context">The context (either internal or internal mappable) that the
		/// style is to be given</param>
		/// -------------------------------------------------------------------------------------
		protected virtual void ChangeFactoryStyleToInternal(IStStyle style, ContextValues context)
		{
			// By default nothing to do.
		}

		/// -------------------------------------------------------------------------------------
		/// <summary>
		/// Detemine whether the newly proposed context for a style is compatible with its
		/// current context.
		/// </summary>
		/// <param name="currContext">The existing context of the style</param>
		/// <param name="proposedContext">The context we want</param>
		/// <returns><c>true </c>if the passed in context can be upgraded as requested;
		/// <c>false</c> otherwise.</returns>
		/// -------------------------------------------------------------------------------------
		public static bool CompatibleContext(ContextValues currContext, ContextValues proposedContext)
		{
			if (currContext == proposedContext)
				return true;
			// Internal and InternalMappable are mutually compatible
			if ((currContext == ContextValues.InternalMappable && proposedContext == ContextValues.Internal) ||
				(proposedContext == ContextValues.InternalMappable && currContext == ContextValues.Internal))
				return true;
			// A (character) style having a specific Context can be made General
			return (proposedContext == ContextValues.General);
		}

		/// -------------------------------------------------------------------------------------
		/// <summary>
		/// Detemine whether the newly proposed function for a style is compatible with its
		/// current function.
		/// </summary>
		/// <param name="currFunction">The existing function of the style</param>
		/// <param name="proposedFunction">The function we want</param>
		/// <returns><c>true </c>if the passed in function can be upgraded as requested;
		/// <c>false</c> otherwise.</returns>
		/// -------------------------------------------------------------------------------------
		public virtual bool CompatibleFunction(FunctionValues currFunction, FunctionValues proposedFunction)
		{
			return (currFunction == proposedFunction);
		}

		/// -------------------------------------------------------------------------------------
		/// <summary>
		/// Third pass of style creation phase: delete (or make as user-defined styles) any
		/// styles that used to be factory styles but aren't any longer. If a deprecated style
		/// is being renamed or replaced with another style (which should already be created by
		/// now), add the replacement to the list (final step of style update process will be to
		/// crawl through the DB and replace all uses).
		/// </summary>
		/// -------------------------------------------------------------------------------------
		protected void DeleteDeprecatedStylesAndDetermineReplacements()
		{
			m_progressDlg.Maximum += Math.Max(0, m_htOrigStyles.Count - m_htUpdatedStyles.Count);

			foreach (var style in m_htOrigStyles.Values)
			{
				string styleName = style.Name;
				if (style.IsBuiltIn && !m_htUpdatedStyles.ContainsKey(styleName))
				{
					m_progressDlg.Step(0);
					m_progressDlg.Message =
						string.Format(ResourceHelper.GetResourceString("kstidDeletingStylesStatusMsg"),
						styleName);

					ContextValues oldContext = style.Context;
					bool fStyleInUse = StyleIsInUse(style);

					// if the style is in use, set things up to replace/remove it in the data
					if (fStyleInUse)
					{
						// If the factory style has been renamed or replaced with another
						// factory style, then all instances of it have to be converted, so
						// add it to the replacement list.
						XmlNode change = m_sourceStyles.SelectSingleNode(
							"replacements/change[@old='" + styleName.Replace(" ", "_") + "']");
						if (change != null)
						{
							string replStyleName =
								change.Attributes.GetNamedItem("new").Value.Replace("_", " ");
							var repl = m_htUpdatedStyles[replStyleName];
							if (!CompatibleContext(oldContext, repl.Context) &&
								!StyleReplacementAllowed(styleName, replStyleName))
								ReportInvalidInstallation(String.Format(
									FrameworkStrings.ksCannotReplaceXwithY, styleName, replStyleName, ResourceFileName));

							m_styleReplacements[styleName] = replStyleName;
						}
						else
						{
							// TODO: If the style is in use then it needs to be changed to
							// a user-defined style instead of being deleted, unless it's an internal style.

							bool fIsCharStyle = style.Type == StyleType.kstCharacter;

							// Note: Instead of delete we replace the old style with the default style
							// for the correct context. Otherwise, deleting a style always sets the style
							// to "Normal", which is wrong in TE where the a) the default style is "Paragraph"
							// and b) the default style for a specific paragraph depends on the current
							// context (e.g. in an intro paragraph the default paragraph style is
							// "Intro Paragraph" instead of "Paragraph"). This fixes TE-5873.
							string defaultStyleName = DefaultStyleForContext(style.Context, fIsCharStyle);
							m_styleReplacements[styleName] = defaultStyleName;
						}
					}

					m_databaseStyles.Remove(style);
				}
			}
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Gets the name of the default style to use in the given context
		/// </summary>
		/// ------------------------------------------------------------------------------------
		protected virtual string DefaultStyleForContext(ContextValues context, bool fCharStyle)
		{
			return fCharStyle ? string.Empty : StyleServices.NormalStyleName;
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Special overridable method to allow application-specific overrides to allow a
		/// particular style to be renamed.
		/// </summary>
		/// <param name="styleName">Name of the original style.</param>
		/// <param name="replStyleName">Name of the replacement style.</param>
		/// <returns>The default always returns <c>false</c>; but an application may
		/// override this to return <c>true</c> for a specific pair of stylenames.</returns>
		/// ------------------------------------------------------------------------------------
		protected virtual bool StyleReplacementAllowed(string styleName, string replStyleName)
		{
			return false;
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Determines whether the given style is (possibly) in use.
		/// </summary>
		/// <remarks>This method is virtual to allow for applications (such as FLEx) that may
		/// not have made good use of the InUse property of styles.</remarks>
		/// <param name="style">The style.</param>
		/// <returns><c>true</c> if there is any reasonable chance the given style is in use
		/// somewhere in the project data; <c>false</c> if the style has never been used and
		/// there is no real possibility it could be in the data.</returns>
		/// ------------------------------------------------------------------------------------
		protected virtual bool StyleIsInUse(IStStyle style)
		{
			return style.InUse;
		}

		/// -------------------------------------------------------------------------------------
		/// <summary>
		/// Final step of style update process is to crawl through the DB and replace all uses of
		/// any deprecated or renamed styles
		/// </summary>
		/// -------------------------------------------------------------------------------------
		protected virtual void ReplaceFormerStyles()
		{
			if (m_styleReplacements.Count == 0)
				return;

			int nPrevMax = m_progressDlg.Maximum;
			m_progressDlg.Maximum = nPrevMax + 1;
			m_progressDlg.Position = nPrevMax;
			m_progressDlg.Message = ResourceHelper.GetResourceString("kstidReplacingStylesStatusMsg");
			StringServices.ReplaceStyles(m_cache, m_styleReplacements);
			m_progressDlg.Position = m_progressDlg.Maximum;
		}
		#endregion

		#region Style creation methods
		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Read the font properties from the XML node and set the properties in the given
		/// props builder.
		/// </summary>
		/// <param name="styleName">Name of style being created/updated (for error reporting)</param>
		/// <param name="styleTag">XML node that has the font properties</param>
		/// <param name="setIntProp">the delegate to set each int property</param>
		/// <param name="setStrProp">the delegate to set each string property</param>
		/// <param name="options">Indicates which properties to overwrite.</param>
		/// ------------------------------------------------------------------------------------
		private void SetFontProperties(string styleName, XmlNode styleTag,
			Action<int, int, int> setIntProp, Action<int, string> setStrProp, OverwriteOptions options)
		{
			// Get character properties
			var fontNode = styleTag.SelectSingleNode("font");
			XmlAttributeCollection fontAttributes = fontNode.Attributes;
			XmlNode attr = fontAttributes.GetNamedItem("spellcheck");
			bool fSpellcheck = (attr == null ? true : (attr.Value == "true"));
			// The default is to do normal spell-checking, so we only need to set this property
			// if we want to suppress spell-checking or if we're forcing an existing
			// user-modified style to have the correct value.
			if (!fSpellcheck || options == OverwriteOptions.FunctionalPropertiesOnly)
			{
				setIntProp((int)FwTextPropType.ktptSpellCheck, (int)FwTextPropVar.ktpvEnum,
					(int)(fSpellcheck ? SpellingModes.ksmNormalCheck : SpellingModes.ksmDoNotCheck));
			}

			if (options == OverwriteOptions.FunctionalPropertiesOnly)
				return;

			attr = fontAttributes.GetNamedItem("italic");
			if (attr != null)
			{
				setIntProp((int)FwTextPropType.ktptItalic, (int)FwTextPropVar.ktpvEnum,
					GetBoolAttribute(fontAttributes, "italic", styleName, ResourceFileName) ?
					(int)FwTextToggleVal.kttvInvert : (int)FwTextToggleVal.kttvOff);
			}

			attr = fontAttributes.GetNamedItem("bold");
			if (attr != null)
			{
				setIntProp((int)FwTextPropType.ktptBold, (int)FwTextPropVar.ktpvEnum,
					GetBoolAttribute(fontAttributes, "bold", styleName, ResourceFileName) ?
					(int)FwTextToggleVal.kttvInvert : (int)FwTextToggleVal.kttvOff);
			}

			// superscript and subscript should be considered mutually exclusive.
			// Results of setting one to true and the other to false may not be intuitive.
			attr = fontAttributes.GetNamedItem("superscript");
			if (attr != null)
			{
				setIntProp((int)FwTextPropType.ktptSuperscript, (int)FwTextPropVar.ktpvEnum,
					GetBoolAttribute(fontAttributes, "superscript", styleName, ResourceFileName) ?
					(int)FwSuperscriptVal.kssvSuper : (int)FwSuperscriptVal.kssvOff);
			}

			attr = fontAttributes.GetNamedItem("subscript");
			if (attr != null)
			{
				setIntProp((int)FwTextPropType.ktptSuperscript, (int)FwTextPropVar.ktpvEnum,
					GetBoolAttribute(fontAttributes, "subscript", styleName, ResourceFileName) ?
					(int)FwSuperscriptVal.kssvSub : (int)FwSuperscriptVal.kssvOff);
			}

			attr = fontAttributes.GetNamedItem("size");
			if (attr != null)
			{
				int nSize = InterpretMeasurementAttribute(attr.Value, "size", styleName, ResourceFileName);
				setIntProp((int)FwTextPropType.ktptFontSize, (int)FwTextPropVar.ktpvMilliPoint, nSize);
			}

			attr = fontAttributes.GetNamedItem("color");
			string sColor = (attr == null ? "default" : attr.Value);
			if (sColor != "default")
			{
				setIntProp((int)FwTextPropType.ktptForeColor, (int)FwTextPropVar.ktpvDefault,
					ColorVal(sColor, styleName));
			}

			attr = fontAttributes.GetNamedItem("underlineColor");
			sColor = (attr == null ? "default" : attr.Value);
			if (sColor != "default")
			{
				setIntProp((int)FwTextPropType.ktptUnderColor, (int)FwTextPropVar.ktpvDefault,
					ColorVal(sColor, styleName));
			}

			attr = fontAttributes.GetNamedItem("underline");
			string sUnderline = (attr == null) ? null : attr.Value;
			if (sUnderline != null)
			{
				int unt = InterpretUnderlineType(sUnderline);
				setIntProp((int)FwTextPropType.ktptUnderline, (int)FwTextPropVar.ktpvEnum, unt);
			}

			var overrides = new Dictionary<int, FontInfo>();
			foreach (XmlNode child in fontNode.ChildNodes)
			{
				if (child.Name == "override") // skip comments
				{
					int wsId = GetWs(child.Attributes);
					if (wsId == 0)
						continue; // WS not in use in this project?
					var fontInfo = new FontInfo();
					var family = XmlUtils.GetOptionalAttributeValue(child, "family");
					if (family != null)
					{
						fontInfo.m_fontName = new InheritableStyleProp<string>(family);
					}
					var sizeText = XmlUtils.GetOptionalAttributeValue(child, "size");
					if (sizeText != null)
					{
						int nSize = InterpretMeasurementAttribute(sizeText, "override.size", styleName, ResourceFileName);
						fontInfo.m_fontSize = new InheritableStyleProp<int>(nSize);
					}
					var color = XmlUtils.GetOptionalAttributeValue(child, "color");
					if (color != null)
					{
						Color parsedColor;
						if (color.StartsWith("("))
						{
							var colorVal = ColorVal(color, styleName);
							parsedColor = Color.FromArgb(colorVal);
						}
						else
						{
							parsedColor = Color.FromName(color);
						}
						fontInfo.m_fontColor = new InheritableStyleProp<Color>(parsedColor);
					}
					var bold = XmlUtils.GetOptionalAttributeValue(child, "bold");
					if (bold != null)
					{
						fontInfo.m_bold = new InheritableStyleProp<bool>(bool.Parse(bold));
					}
					var italic = XmlUtils.GetOptionalAttributeValue(child, "italic");
					if (italic != null)
					{
						fontInfo.m_italic = new InheritableStyleProp<bool>(bool.Parse(italic));
					}
					overrides[wsId] = fontInfo;
				}
			}
			if (overrides.Count > 0)
			{
				var overridesString = BaseStyleInfo.GetOverridesString(overrides);
				if (!string.IsNullOrEmpty(overridesString))
				{
					setStrProp((int)FwTextPropType.ktptWsStyle, overridesString);
				}
			}
			// TODO: Handle dropcap attribute
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Color value may be (red, green, blue) or one of the KnownColor values.
		/// Adapted from XmlVc routine.
		/// </summary>
		/// <param name="val">Value to interpret (a color name or (red, green, blue).</param>
		/// <param name="styleName">name of the style (for error reporting)</param>
		/// <returns>the color as a BGR 6-digit hex int</returns>
		/// ------------------------------------------------------------------------------------
		private int ColorVal(string val, string styleName)
		{
			if (val[0] == '(')
			{
				int firstComma = val.IndexOf(',');
				int red = Convert.ToInt32(val.Substring(1,firstComma - 1));
				int secondComma = val.IndexOf(',', firstComma + 1);
				int green = Convert.ToInt32(val.Substring(firstComma + 1, secondComma - firstComma - 1));
				int blue = Convert.ToInt32(val.Substring(secondComma + 1, val.Length - secondComma - 2));
				return(blue * 256 + green) * 256 + red;
			}
			Color col = Color.FromName(val);
			if (col.ToArgb() == 0)
			{
				ReportInvalidInstallation(String.Format(
					FrameworkStrings.ksUnknownUnderlineColor, styleName, ResourceFileName));
			}
			return (col.B * 256 + col.G) * 256 + col.R;
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Interpret an underline type string as an FwUnderlineType.
		/// Note that this is a duplicate of the routine on XmlVc (due to avoiding assembly references). Keep in sync.
		/// </summary>
		/// <param name="strVal"></param>
		/// <returns></returns>
		/// ------------------------------------------------------------------------------------
		public static int InterpretUnderlineType(string strVal)
		{
			int val = (int)FwUnderlineType.kuntSingle; // default
			switch(strVal)
			{
				case "single":
				case null:
					val = (int)FwUnderlineType.kuntSingle;
					break;
				case "none":
					val = (int)FwUnderlineType.kuntNone;
					break;
				case "double":
					val = (int)FwUnderlineType.kuntDouble;
					break;
				case "dotted":
					val = (int)FwUnderlineType.kuntDotted;
					break;
				case "dashed":
					val = (int)FwUnderlineType.kuntDashed;
					break;
				case "squiggle":
					val = (int)FwUnderlineType.kuntSquiggle;
					break;
				case "strikethrough":
					val = (int)FwUnderlineType.kuntStrikethrough;
					break;
				default:
					Debug.Assert(false, "Expected value single, none, double, dotted, dashed, strikethrough, or squiggle");
					break;
			}
			return val;
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Read the paragraph properties from the XML node and set the properties in the given
		/// props builder.
		/// </summary>
		/// <param name="styleName">Name of style being created/updated (for error reporting)
		/// </param>
		/// <param name="styleTag">XML node that has the paragraph properties</param>
		/// <param name="setIntProp">the delegate to set each int property</param>
		/// <param name="setStrProp">the delegate to set each string property</param>
		/// <param name="options">Indicates which properties to overwrite.</param>
		/// ------------------------------------------------------------------------------------
		private void SetParagraphProperties(string styleName, XmlNode styleTag,
			Action<int, int, int> setIntProp, Action<int, string> setStrProp, OverwriteOptions options)
		{
			XmlNode node = styleTag.SelectSingleNode("paragraph");
			if (node == null)
			{
				ReportInvalidInstallation(String.Format(
					FrameworkStrings.ksMissingParagraphNode, styleName, ResourceFileName));
			}
			XmlNode bulletFontInfoNode = node.SelectSingleNode("BulNumFontInfo");

			XmlAttributeCollection paraAttributes = node.Attributes;

			node = paraAttributes.GetNamedItem("keepWithNext");
			if (node != null)
			{
				setIntProp((int)FwTextPropType.ktptKeepWithNext,
					(int)FwTextPropVar.ktpvEnum,
					GetBoolAttribute(paraAttributes, "keepWithNext", styleName, ResourceFileName) ?
					(int)FwTextToggleVal.kttvForceOn :
					(int)FwTextToggleVal.kttvOff);
			}

			node = paraAttributes.GetNamedItem("keepTogether");
			if (node != null)
			{
				setIntProp((int)FwTextPropType.ktptKeepTogether,
					(int)FwTextPropVar.ktpvEnum,
					GetBoolAttribute(paraAttributes, "keepTogether", styleName, ResourceFileName) ?
					(int)FwTextToggleVal.kttvForceOn :
					(int)FwTextToggleVal.kttvOff);
			}

			node = paraAttributes.GetNamedItem("widowOrphan");
			if (node != null)
			{
				setIntProp((int)FwTextPropType.ktptWidowOrphanControl,
					(int)FwTextPropVar.ktpvEnum,
					GetBoolAttribute(paraAttributes, "widowOrphan", styleName, ResourceFileName) ?
					(int)FwTextToggleVal.kttvForceOn :
					(int)FwTextToggleVal.kttvOff);
			}

			if (options == OverwriteOptions.FunctionalPropertiesOnly)
				return;

			// Set alignment
			node = paraAttributes.GetNamedItem("alignment");
			if (node != null)
			{
				string sAlign = node.Value;
				int nAlign = (int)FwTextAlign.ktalLeading;
				switch (sAlign)
				{
					case "left":
						break;
					case "center":
						nAlign = (int)FwTextAlign.ktalCenter;
						break;
					case "right":
						nAlign = (int)FwTextAlign.ktalTrailing;
						break;
					case "full":
						nAlign = (int)FwTextAlign.ktalJustify;
						break;
					default:
						ReportInvalidInstallation(String.Format(
							FrameworkStrings.ksUnknownAlignmentValue, styleName, ResourceFileName));
						break;
				}
				setIntProp((int)FwTextPropType.ktptAlign,
					(int)FwTextPropVar.ktpvEnum, nAlign);
			}

			node = paraAttributes.GetNamedItem("background");
			var sColor = (node == null ? "default" : node.Value);
			if (sColor != "default")
			{
				setIntProp((int)FwTextPropType.ktptBackColor, (int)FwTextPropVar.ktpvDefault,
					ColorVal(sColor, styleName));
			}

			// set leading indentation
			node = paraAttributes.GetNamedItem("indentLeft");
			if (node != null)
			{
				int nLeftIndent = InterpretMeasurementAttribute(node.Value, "indentLeft",
					styleName, ResourceFileName);
				setIntProp(
					(int)FwTextPropType.ktptLeadingIndent,
					(int)FwTextPropVar.ktpvMilliPoint, nLeftIndent);
			}

			// Set trailing indentation
			node = paraAttributes.GetNamedItem("indentRight");
			if (node != null)
			{
				int nRightIndent = InterpretMeasurementAttribute(node.Value, "indentRight",
					styleName, ResourceFileName);
				setIntProp(
					(int)FwTextPropType.ktptTrailingIndent,
					(int)FwTextPropVar.ktpvMilliPoint, nRightIndent);
			}

			// Set first-line/hanging indentation
			int nFirstIndent = 0;
			bool fFirstLineOrHangingIndentSpecified = false;
			node = paraAttributes.GetNamedItem("firstLine");
			if (node != null)
			{
				nFirstIndent = InterpretMeasurementAttribute(node.Value, "firstLine",
					styleName, ResourceFileName);
				fFirstLineOrHangingIndentSpecified = true;
			}
			int nHangingIndent = 0;
			node = paraAttributes.GetNamedItem("hanging");
			if (node != null)
			{
				nHangingIndent = InterpretMeasurementAttribute(node.Value, "hanging",
					styleName, ResourceFileName);
				fFirstLineOrHangingIndentSpecified = true;
			}

			if (nFirstIndent != 0 && nHangingIndent != 0)
				ReportInvalidInstallation(String.Format(
					FrameworkStrings.ksInvalidFirstLineHanging, styleName, ResourceFileName));

			nFirstIndent -= nHangingIndent;
			if (fFirstLineOrHangingIndentSpecified)
			{
				setIntProp(
					(int)FwTextPropType.ktptFirstIndent,
					(int)FwTextPropVar.ktpvMilliPoint,
					nFirstIndent);
			}

			// Set space before
			node = paraAttributes.GetNamedItem("spaceBefore");
			if (node != null)
			{
				int nSpaceBefore = InterpretMeasurementAttribute(node.Value, "spaceBefore",
					styleName, ResourceFileName);
				setIntProp(
					(int)FwTextPropType.ktptSpaceBefore,
					(int)FwTextPropVar.ktpvMilliPoint, nSpaceBefore);
			}

			// Set space after
			node = paraAttributes.GetNamedItem("spaceAfter");
			if (node != null)
			{
				int nSpaceAfter = InterpretMeasurementAttribute(node.Value, "spaceAfter",
					styleName, ResourceFileName);
				setIntProp(
					(int)FwTextPropType.ktptSpaceAfter,
					(int)FwTextPropVar.ktpvMilliPoint, nSpaceAfter);
			}

			node = paraAttributes.GetNamedItem("lineSpacing");
			if (node != null)
			{
				int lineSpacing = InterpretMeasurementAttribute(node.Value, "lineSpacing", styleName, ResourceFileName);
				if (lineSpacing < 0)
				{
					ReportInvalidInstallation(String.Format(
						FrameworkStrings.ksNegativeLineSpacing, styleName, ResourceFileName));
				}

				// Set lineSpacing
				node = paraAttributes.GetNamedItem("lineSpacingType");
				string sLineSpacingType = "";
				if (node != null)
				{
					sLineSpacingType = node.Value;
					switch (sLineSpacingType)
					{
						// verify valid line spacing types
						case "atleast":
							setIntProp((int)FwTextPropType.ktptLineHeight,
								(int)FwTextPropVar.ktpvMilliPoint, lineSpacing);
							break;
						case "exact":
							lineSpacing *= -1; // negative lineSpacing indicates exact line spacing
							setIntProp((int)FwTextPropType.ktptLineHeight,
								(int)FwTextPropVar.ktpvMilliPoint, lineSpacing);
							break;
						case "rel":
							setIntProp((int) FwTextPropType.ktptLineHeight,
								(int) FwTextPropVar.ktpvRelative, lineSpacing);
							break;
						default:
							ReportInvalidInstallation(string.Format(
								FrameworkStrings.ksUnknownLineSpacingValue, styleName, ResourceFileName));
							break;
					}
				}
			}

			// Set borders
			node = paraAttributes.GetNamedItem("border");
			if (node != null)
			{
				int nBorder = 0;
				switch (node.Value)
				{
					case "top":
						nBorder = (int)FwTextPropType.ktptBorderTop;
						break;
					case "bottom":
						nBorder = (int)FwTextPropType.ktptBorderBottom;
						break;
					case "leading":
						nBorder = (int)FwTextPropType.ktptBorderLeading;
						break;
					case "trailing":
						nBorder = (int)FwTextPropType.ktptBorderTrailing;
						break;
					default:
						ReportInvalidInstallation(String.Format(
							FrameworkStrings.ksUnknownBorderValue, styleName, ResourceFileName));
						break;
				}
				setIntProp(nBorder, (int)FwTextPropVar.ktpvDefault,
					500);
			}

			node = paraAttributes.GetNamedItem("bulNumScheme");
			if (node != null)
			{
				setIntProp((int)FwTextPropType.ktptBulNumScheme,
					(int)FwTextPropVar.ktpvEnum,
					InterpretBulNumSchemeAttribute(node.Value, styleName, ResourceFileName));
			}
			node = paraAttributes.GetNamedItem("bulNumStartAt");
			if (node != null)
			{
				int nVal;
				if (!Int32.TryParse(node.Value, out nVal))
				{
					ReportInvalidInstallation(String.Format(FrameworkStrings.ksUnknownBulNumStartAtValue,
						styleName, ResourceFileName));
					nVal = 0;
				}
				setIntProp((int)FwTextPropType.ktptBulNumStartAt,
					(int)FwTextPropVar.ktpvDefault, nVal);
			}

			node = paraAttributes.GetNamedItem("bulNumTxtAft");
			if (node?.Value.Length > 0)
			{
				setStrProp((int) FwTextPropType.ktptBulNumTxtAft, node.Value);
			}

			node = paraAttributes.GetNamedItem("bulNumTxtBef");
			if (node?.Value.Length > 0)
			{
				setStrProp((int) FwTextPropType.ktptBulNumTxtBef, node.Value);
			}

			node = paraAttributes.GetNamedItem("bulCusTxt");
			if (node?.Value.Length > 0)
			{
				setStrProp((int) FwTextPropType.ktptCustomBullet, node.Value);
			}

			//Bullet Font Info
			if (bulletFontInfoNode?.ChildNodes == null)
				return;
			SetBulNumFontInfoProperties(styleName, setStrProp, bulletFontInfoNode);
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Read the BulNumFontInfo properties from the XML node and set the properties in the given
		/// props builder.
		/// </summary>
		/// <param name="styleName">Name of style being created/updated (for error reporting) </param>
		/// <param name="setStrProp">the delegate to set each string property</param>
		/// <param name="bulletFontInfoNode">BulNumFontInfo Node from Xml document</param>
		/// ------------------------------------------------------------------------------------
		private void SetBulNumFontInfoProperties(string styleName, Action<int, string> setStrProp, XmlNode bulletFontInfoNode)
		{
			ITsPropsBldr propsBldr = TsStringUtils.MakePropsBldr();
			int type, var;

			XmlAttributeCollection bulNumFontAttributes = bulletFontInfoNode.Attributes;
			if (bulNumFontAttributes == null) return;

			XmlNode attr = bulNumFontAttributes.GetNamedItem("italic");
			if (attr != null)
			{
				propsBldr.SetIntPropValues((int) FwTextPropType.ktptItalic, (int) FwTextPropVar.ktpvEnum,
					GetBoolAttribute(bulNumFontAttributes, "italic", styleName, ResourceFileName)
						? (int) FwTextToggleVal.kttvForceOn
						: (int) FwTextToggleVal.kttvOff);
			}

			attr = bulNumFontAttributes.GetNamedItem("bold");
			if (attr != null)
			{
				propsBldr.SetIntPropValues((int) FwTextPropType.ktptBold, (int) FwTextPropVar.ktpvEnum,
					GetBoolAttribute(bulNumFontAttributes, "bold", styleName, ResourceFileName)
						? (int) FwTextToggleVal.kttvForceOn
						: (int) FwTextToggleVal.kttvOff);
			}

			attr = bulNumFontAttributes.GetNamedItem("size");
			if (attr != null)
			{
				int nSize = InterpretMeasurementAttribute(attr.Value, "size", styleName, ResourceFileName);
				propsBldr.SetIntPropValues((int) FwTextPropType.ktptFontSize, (int) FwTextPropVar.ktpvMilliPoint, nSize);
			}

			attr = bulNumFontAttributes.GetNamedItem("color");
			string sbColor = (attr == null ? "default" : attr.Value);
			if (sbColor != "default")
			{
				propsBldr.SetIntPropValues((int) FwTextPropType.ktptForeColor, (int) FwTextPropVar.ktpvDefault,
					ColorVal(sbColor, styleName));
			}

			attr = bulNumFontAttributes.GetNamedItem("underlineColor");
			sbColor = (attr == null ? "default" : attr.Value);
			if (sbColor != "default")
			{
				propsBldr.SetIntPropValues((int) FwTextPropType.ktptUnderColor, (int) FwTextPropVar.ktpvDefault,
					ColorVal(sbColor, styleName));
			}

			attr = bulNumFontAttributes.GetNamedItem("underline");
			string sUnderline = (attr == null) ? null : attr.Value;
			if (sUnderline != null)
			{
				int unt = InterpretUnderlineType(sUnderline);
				propsBldr.SetIntPropValues((int) FwTextPropType.ktptUnderline, (int) FwTextPropVar.ktpvEnum, unt);
			}

			attr = bulNumFontAttributes.GetNamedItem("family");
			string sfamily = (attr == null) ? null : attr.Value;
			if (sfamily != null)
			{
				propsBldr.SetStrPropValue((int) FwTextPropType.ktptFontFamily, sfamily);
			}

			attr = bulNumFontAttributes.GetNamedItem("forecolor");
			sbColor = (attr == null ? "default" : attr.Value);
			if (sbColor != "default")
			{
				propsBldr.SetIntPropValues((int) FwTextPropType.ktptForeColor, (int) FwTextPropVar.ktpvDefault,
					ColorVal(sbColor, styleName));
			}

			attr = bulNumFontAttributes.GetNamedItem("backcolor");
			sbColor = (attr == null ? "default" : attr.Value);
			if (sbColor != "default")
			{
				propsBldr.SetIntPropValues((int) FwTextPropType.ktptBackColor, (int) FwTextPropVar.ktpvDefault,
					ColorVal(sbColor, styleName));
			}

			// Add the integer properties to the bullet props string
			StringBuilder bulletProps = new StringBuilder(propsBldr.IntPropCount * 3 + propsBldr.StrPropCount * 3);
			for (int i = 0; i < propsBldr.IntPropCount; i++)
			{
				var intValue = propsBldr.GetIntProp(i, out type, out var);
				bulletProps.Append((char) type);
				bulletProps.Append((char) (intValue & 0xFFFF));
				bulletProps.Append((char) ((intValue >> 16) & 0xFFFF));
			}

			// Add the string properties to the bullet props string
			for (int i = 0; i < propsBldr.StrPropCount; i++)
			{
				var strValue = propsBldr.GetStrProp(i, out type);
				bulletProps.Append((char) type);
				bulletProps.Append(strValue);
				bulletProps.Append('\u0000');
			}

			if (!string.IsNullOrEmpty(bulletProps.ToString()))
			{
				setStrProp((int) FwTextPropType.ktptBulNumFontInfo, bulletProps.ToString());
			}
		}

		/// -------------------------------------------------------------------------------------
		/// <summary>
		/// Get the ws value (hvo) from the wsId contained in the given attributes
		/// </summary>
		/// <param name="attribs">Collection of attributes that better have an "wsId"
		/// attribute</param>
		/// <returns></returns>
		/// -------------------------------------------------------------------------------------
		private int GetWs(XmlAttributeCollection attribs)
		{
			string wsId = attribs.GetNamedItem("wsId").Value;
			if (string.IsNullOrEmpty(wsId))
				return 0;
			return Cache.ServiceLocator.WritingSystemManager.GetWsFromStr(wsId);
		}

		/// -------------------------------------------------------------------------------------
		/// <summary>
		/// Second pass of style creation phase: set up "based-on" and "next" styles
		/// </summary>
		/// <param name="tagList">List of XML nodes representing factory styles to create</param>
		/// -------------------------------------------------------------------------------------
		private void SetBasedOnAndNextProps(XmlNodeList tagList)
		{
			foreach (XmlNode styleTag in tagList)
			{
				XmlAttributeCollection attributes = styleTag.Attributes;

				string styleName = GetStyleName(attributes);
				ContextValues context = GetContext(attributes, styleName);
				if (IsExcludedContext(context))
					continue;

				var style = m_htUpdatedStyles[styleName];
				// No need now to do the assert,
				// since the Dictionary will throw an exception,
				// if the key isn't present.
				//Debug.Assert(style != null);

				m_progressDlg.Step(0);
				m_progressDlg.Message =
					string.Format(ResourceHelper.GetResourceString("kstidUpdatingStylesStatusMsg"),
					styleName);

				XmlAttributeCollection attributesWithBasedOn = styleTag.Attributes;
				if (style.Type == StyleType.kstParagraph)
				{
					attributesWithBasedOn = styleTag.SelectSingleNode("paragraph").Attributes;
				}
				else if (style.Type == StyleType.kstCharacter && attributesWithBasedOn.GetNamedItem("basedOn") == null)
				{
					continue;
				}

				if (styleName != StyleServices.NormalStyleName)
				{
					string sBasedOnStyleName = GetBasedOn(attributesWithBasedOn, styleName);

					if (String.IsNullOrEmpty(sBasedOnStyleName))
						ReportInvalidInstallation(String.Format(
							FrameworkStrings.ksMissingBasedOnStyle, styleName, ResourceFileName));

					if (!m_htUpdatedStyles.ContainsKey(sBasedOnStyleName))
						ReportInvalidInstallation(String.Format(
							FrameworkStrings.ksUnknownBasedOnStyle, styleName, sBasedOnStyleName));

					var basedOnStyle = m_htUpdatedStyles[sBasedOnStyleName];
					if (basedOnStyle.Hvo == style.Hvo)
						ReportInvalidInstallation(String.Format(
							FrameworkStrings.ksNoBasedOnSelf, styleName, ResourceFileName));

					style.BasedOnRA = basedOnStyle;
				}

				string sNextStyleName = null;
				if (m_htReservedStyles.ContainsKey(styleName))
				{
					ReservedStyleInfo info = m_htReservedStyles[styleName];
					sNextStyleName = info.nextStyle;
				}
				else
				{
					XmlNode next = attributesWithBasedOn.GetNamedItem("next");
					if (next != null)
						sNextStyleName = next.Value.Replace("_", " ");
				}

				if (!string.IsNullOrEmpty(sNextStyleName))
				{
					if (!m_htUpdatedStyles.ContainsKey(sNextStyleName))
						ReportInvalidInstallation(String.Format(
							FrameworkStrings.ksUnknownNextStyle, styleName, sNextStyleName, ResourceFileName));

					if (m_htUpdatedStyles.ContainsKey(sNextStyleName))
						style.NextRA = m_htUpdatedStyles[sNextStyleName];
					else
						style.NextRA = null;
				}
			}
			SetBasedOnAndNextPropsReserved();
		}

		/// -------------------------------------------------------------------------------------
		/// <summary>
		/// Second pass of style creation phase for reserved styles that weren't in the external
		/// XML stylesheet: set up "based-on" and "next" styles
		/// </summary>
		/// -------------------------------------------------------------------------------------
		private void SetBasedOnAndNextPropsReserved()
		{
			foreach (string styleName in m_htReservedStyles.Keys)
			{
				ReservedStyleInfo info = m_htReservedStyles[styleName];
				if (!info.created)
				{
					var style = m_htUpdatedStyles[styleName];
					// No need now to do the assert,
					// since the Dictionary will throw an exception,
					// if the key isn't present.
					//Debug.Assert(style != null);

					if (style.Type == StyleType.kstParagraph)
					{
						m_progressDlg.Message =
							string.Format(ResourceHelper.GetResourceString("kstidUpdatingStylesStatusMsg"),
							styleName);

						IStStyle newStyle = null;
						if (styleName != StyleServices.NormalStyleName)
						{
							if (m_htUpdatedStyles.ContainsKey(info.basedOn))
								newStyle = m_htUpdatedStyles[info.basedOn];
							style.BasedOnRA = newStyle;
						}

						newStyle = null;
						if (m_htUpdatedStyles.ContainsKey(info.nextStyle))
							newStyle = m_htUpdatedStyles[info.nextStyle];
						style.NextRA = newStyle;
					}
				}
			}
		}
		#endregion

		#region Read XML Attributes
		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Interprets a given attribute as a boolean value
		/// </summary>
		/// <param name="attributes">Collection of attributes to look in</param>
		/// <param name="sAttrib">Named attribute</param>
		/// <param name="styleName">The name of the style to which this attribute pertains (used
		/// only for debug error reporting)</param>
		/// <param name="fileName">Name of XML file (for error reporting)</param>
		/// <returns>true if attribute value is "yes" or "true"</returns>
		/// ------------------------------------------------------------------------------------
		static public bool GetBoolAttribute(XmlAttributeCollection attributes, string sAttrib,
			string styleName, string fileName)
		{
			string sVal = attributes.GetNamedItem(sAttrib).Value;
			if (sVal == "yes" || sVal == "true")
				return true;
			else if (sVal == "no" || sVal == "false" || sVal == String.Empty)
				return false;

			ReportInvalidInstallation(String.Format(
				FrameworkStrings.ksUnknownStyleAttribute, sAttrib, styleName, fileName));
			return false; // Can't actually get here, but don't tell the compiler that!
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Interprets a given attribute value as a measurement (in millipoints)
		/// </summary>
		/// <param name="sSize">Attribute value</param>
		/// <param name="sAttrib">The name of the attribute being interpreted (used only for
		/// debug error reporting)</param>
		/// <param name="styleName">The name of the style to which this attribute pertains (used
		/// only for debug error reporting)</param>
		/// <param name="fileName">Name of XML file (for error reporting)</param>
		/// <returns>The value of the attribute interpreted as millipoints</returns>
		/// ------------------------------------------------------------------------------------
		static public int InterpretMeasurementAttribute(string sSize, string sAttrib,
			string styleName, string fileName)
		{
			sSize = sSize.Trim();
			if (sSize.Length >= 4)
			{
				string number = sSize.Substring(0, sSize.Length - 3);
				if (sSize.EndsWith(" pt"))
					return (int)(double.Parse(number, new CultureInfo("en-US")) * 1000.0);
				else if (sSize.EndsWith(" in"))
					return (int)(double.Parse(number, new CultureInfo("en-US")) * 72000.0);
				else
					ReportInvalidInstallation(String.Format(
						FrameworkStrings.ksUnknownAttrUnits, sAttrib, styleName, fileName));
			}
			return 0;
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Interprets a given attribute value as a Bullet/Number scheme.
		/// </summary>
		/// <param name="sScheme">attribute value</param>
		/// <param name="styleName">The name of the style to which this attribute pertains (used
		/// only for debug error reporting)</param>
		/// <param name="fileName">Name of XML file (for error reporting)</param>
		/// <returns>
		/// The value of the attribute interpreted as an enum value (int equivalent)
		/// </returns>
		/// ------------------------------------------------------------------------------------
		public static int InterpretBulNumSchemeAttribute(string sScheme, string styleName, string fileName)
		{
			sScheme = sScheme.Trim();
			switch (sScheme)
			{
				case "None":		return (int)VwBulNum.kvbnNone;
				case "Arabic":		return (int)VwBulNum.kvbnArabic;
				case "Arabic01":	return (int)VwBulNum.kvbnArabic01;
				case "LetterUpper":	return (int)VwBulNum.kvbnLetterUpper;
				case "LetterLower":	return (int)VwBulNum.kvbnLetterLower;
				case "RomanUpper":	return (int)VwBulNum.kvbnRomanUpper;
				case "RomanLower":	return (int)VwBulNum.kvbnRomanLower;
				case "Custom":      return (int)VwBulNum.kvbnBullet;
			}
			int nVal;
			if (sScheme.StartsWith("Bullet:"))
			{
				if (Int32.TryParse(sScheme.Substring(7), out nVal))
				{
					nVal += (int)VwBulNum.kvbnBulletBase;
					if (nVal >= (int)VwBulNum.kvbnBulletBase && nVal <= (int)VwBulNum.kvbnBulletMax)
						return nVal;
				}
			}
			else if (Int32.TryParse(sScheme, out nVal))
			{
				if (nVal >= (int)VwBulNum.kvbnBulletBase && nVal <= (int)VwBulNum.kvbnBulletMax)
					return nVal;
			}
			ReportInvalidInstallation(String.Format(FrameworkStrings.ksUnknownBulNumSchemeValue,
				styleName, fileName));
			return (int)VwBulNum.kvbnNone;
		}
		#endregion

		#region Helper methods
		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Returns whether or not the context of the specified style is excluded (i.e., should
		/// not result in the creation of a database Style). Default is to exclude nothing.
		/// </summary>
		/// <param name="context">The context to test</param>
		/// <returns>True if the context is excluded, false otherwise</returns>
		/// ------------------------------------------------------------------------------------
		protected virtual bool IsExcludedContext(ContextValues context)
		{
			return false;
		}
		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Retrieves a valid TE style name from the specified attributes.
		/// </summary>
		/// <param name="attributes">The attributes containing the style id to use</param>
		/// <returns>a valid TE style name</returns>
		/// ------------------------------------------------------------------------------------
		private static string GetStyleName(XmlAttributeCollection attributes)
		{
			return attributes.GetNamedItem("id").Value.Replace("_", " ");
		}
		#endregion
	}

	#region ReservedStyleInfo class
	/// ------------------------------------------------------------------------------------
	/// <summary>
	/// Holds info about certain reserved styles. This info overrides any conflicting info
	/// in the external XML stylesheet
	/// </summary>
	/// ------------------------------------------------------------------------------------
	public class ReservedStyleInfo
	{
		/// <summary>Factory guid</summary>
		public Guid guid;
		/// <summary> </summary>
		public bool created = false;
		/// <summary> </summary>
		public ContextValues context;
		/// <summary> </summary>
		public StructureValues structure;
		/// <summary> </summary>
		public FunctionValues function;
		/// <summary> </summary>
		public StyleType styleType;
		/// <summary> </summary>
		public string nextStyle;
		/// <summary> </summary>
		public string basedOn;

		/// --------------------------------------------------------------------------------
		/// <summary>
		/// General-purpose constructor
		/// </summary>
		/// <param name="context">Context</param>
		/// <param name="structure">Structure</param>
		/// <param name="function">Function</param>
		/// <param name="styleType">Paragraph or character</param>
		/// <param name="nextStyle">Name of "Next" style, or null if this is info about a
		/// character style</param>
		/// <param name="basedOn">Name of base style, or null if this is info about a
		/// character style </param>
		/// <param name="guid">The universal identifier for this style </param>
		/// --------------------------------------------------------------------------------
		public ReservedStyleInfo(ContextValues context, StructureValues structure,
			FunctionValues function, StyleType styleType, string nextStyle,
			string basedOn, string guid)
		{
			this.guid = new Guid(guid);
			this.context = context;
			this.structure = structure;
			this.function = function;
			this.styleType = styleType;
			this.nextStyle = nextStyle;
			this.basedOn = basedOn;
		}

		/// --------------------------------------------------------------------------------
		/// <summary>
		/// Constructor for character style info
		/// </summary>
		/// <param name="context">Context</param>
		/// <param name="structure">Structure</param>
		/// <param name="function">Function</param>
		/// <param name="guid">The universal identifier for this style </param>
		/// --------------------------------------------------------------------------------
		public ReservedStyleInfo(ContextValues context, StructureValues structure,
			FunctionValues function, string guid)
			: this(context, structure, function,
			StyleType.kstCharacter, null, null, guid)
		{
		}
	}
	#endregion
}
