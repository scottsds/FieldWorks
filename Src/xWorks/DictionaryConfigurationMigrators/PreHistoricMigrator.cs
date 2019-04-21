// Copyright (c) 2017 SIL International
// This software is licensed under the LGPL, version 2.1 or later
// (http://www.gnu.org/licenses/lgpl-2.1.html)

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Xml;
using SIL.LCModel.Core.Cellar;
using SIL.LCModel.Core.Text;
using SIL.FieldWorks.Common.Controls;
using SIL.LCModel.Core.KernelInterfaces;
using SIL.FieldWorks.Common.FwUtils;
using SIL.FieldWorks.Common.Widgets;
using SIL.LCModel;
using SIL.LCModel.DomainServices;
using SIL.LCModel.Infrastructure;
using SIL.Utils;
using XCore;
using DCM = SIL.FieldWorks.XWorks.DictionaryConfigurationMigrator;
using DCL = SIL.FieldWorks.XWorks.DictionaryConfigurationListener;

namespace SIL.FieldWorks.XWorks.DictionaryConfigurationMigrators
{
	/// <summary>
	/// The PreHistoricMigrator is responsible for migrating a pre-8.3Alpha db with the old layout
	/// system into the new configuration system. Other migrators will take it from there. Part of
	/// the process involves comparing converted nodes to the static 8.3Alpha configuration files
	/// stored in {DistFiles}/Language Explorer/AlphaConfigs.
	/// </summary>
	public class PreHistoricMigrator : IDictionaryConfigurationMigrator, ILayoutConverter
	{
		private PropertyTable m_propertyTable;
		private Mediator m_mediator;
		private Inventory m_layoutInventory;
		private Inventory m_partInventory;
		private SimpleLogger m_logger;

		/// <summary>
		/// Dictionary of custom fields for each parent field type: Key is parent field type (Type; e.g. ILexEntry)
		/// Value is Dictionary of custom fields: Key is custom field Label, Value is custom field Name
		/// Dictionary&lt;parent field type, Dictionary&lt;custom field label, custom field name&gt;&gt;
		/// Backwards because a freshly-migrated Dictionary Configuration comes with Labels but not Field Names.
		/// Needed because we can't look Custom Fields' Names up in our shipping Configurations as we do for standard Fields. Cached for performance.
		/// </summary>
		private Dictionary<string, Dictionary<string, string>> m_classToCustomFieldsLabelToName;

		/// <summary>
		/// The innermost directory of the configurations presently being migrated.
		/// To migrate, this class calls out to <see cref="LegacyConfigurationUtils"/>, which calls this class back through the
		/// <see cref="ILayoutConverter"/> interface. There is no way to pass this directory name out and back through the current
		/// interfaces, so we store it as a member variable.
		/// </summary>
		internal string m_configDirSuffixBeingMigrated;

		/// <summary>
		/// The mediator is needed to retrieve information about the prehistoric configurations
		/// </summary>
		public PreHistoricMigrator(Mediator mediator) : this(null, mediator, null) {}

		/// <summary>
		/// Constructor for tests
		/// </summary>
		internal PreHistoricMigrator(LcmCache cache, Mediator mediator, PropertyTable propertyTable)
		{
			Cache = cache;
			m_propertyTable = propertyTable;
			m_mediator = mediator;
		}

		public const int VersionPre83 = -1;
		public const int VersionAlpha1 = 1;

		/// <summary>
		/// Name of the folder where the static 8.3Alpha1 configuration files are stored
		/// for migration of older dbs.
		/// </summary>
		private const string AlphaConfigFolder = "AlphaConfigs";

		public void MigrateIfNeeded(SimpleLogger logger, PropertyTable propertyTable, string appVersion)
		{
			m_logger = logger;
			m_propertyTable = propertyTable;
			Cache = propertyTable.GetValue<LcmCache>("cache");
			LayoutLevels = new LayoutLevels();
			m_layoutInventory = Inventory.GetInventory("layouts", Cache.ProjectId.Name);
			m_partInventory = Inventory.GetInventory("parts", Cache.ProjectId.Name);

			if (ConfigsNeedMigratingFromPre83())
			{
				m_logger.WriteLine(string.Format("{0}: Old configurations were found in need of migration. - {1}",
					appVersion, DateTime.Now.ToString("yyyy MMM d h:mm:ss")));
				var projectPath = LcmFileHelper.GetConfigSettingsDir(Cache.ProjectId.ProjectFolder);

				m_logger.WriteLine("Migrating dictionary configurations");
				m_configDirSuffixBeingMigrated = DictionaryConfigurationListener.DictionaryConfigurationDirectoryName;
				Directory.CreateDirectory(Path.Combine(projectPath, m_configDirSuffixBeingMigrated));
				UndoableUnitOfWorkHelper.DoUsingNewOrCurrentUOW(
					"Undo Migrate old Dictionary Configurations", "Redo Migrate old Dictionary Configurations",
					Cache.ActionHandlerAccessor, PerformMigrationUOW);
				m_logger.WriteLine(string.Format("Migrating Reversal Index configurations, if any - {0}",
					DateTime.Now.ToString("h:mm:ss")));
				m_configDirSuffixBeingMigrated = DictionaryConfigurationListener.ReversalIndexConfigurationDirectoryName;
				Directory.CreateDirectory(Path.Combine(projectPath, m_configDirSuffixBeingMigrated));
				UndoableUnitOfWorkHelper.DoUsingNewOrCurrentUOW(
					"Undo Migrate old Reversal Configurations", "Redo Migrate old Reversal Configurations",
					Cache.ActionHandlerAccessor, PerformMigrationUOW);
			}
		}

		/// <summary>Perform the migration for Dictionary or Reversal (depending on m_configDirSuffixBeingMigrated.</summary>
		/// <remarks>Must be called in an UndoableUnitOfWork.</remarks>
		private void PerformMigrationUOW()
		{
			var tool = m_configDirSuffixBeingMigrated == DictionaryConfigurationListener.DictionaryConfigurationDirectoryName
				? "lexiconDictionary"
				: "reversalToolEditComplete";
			var configureLayouts = GetConfigureLayoutsNodeForTool(tool);
			LegacyConfigurationUtils.BuildTreeFromLayoutAndParts(configureLayouts, this);
		}

		/// <summary>
		/// Loads the xml configuration for the given tool and returns its configureLayouts child.
		/// </summary>
		private XmlNode GetConfigureLayoutsNodeForTool(string tool)
		{
			var collector = new XmlNode[1];
			var parameter = new Tuple<string, string, XmlNode[]>("lexicon", tool, collector);
			m_mediator.SendMessage("GetContentControlParameters", parameter);
			var controlNode = collector[0];
			var parameters = controlNode.SelectSingleNode("parameters");
			var configureLayouts = XmlUtils.FindNode(parameters, "configureLayouts");
			return configureLayouts;
		}

		/// <summary>
		/// In the old system, Dictionary and Reversal Index configurations were stored across a hairball of *.fwlayout files. Rather than trying to
		/// determine what the user has configured, if the user has configured anything, migrate everything.
		/// </summary>
		/// <remarks>Internal only for tests, production entry point is MigrateOldConfigurationsIfNeeded()</remarks>
		internal bool ConfigsNeedMigratingFromPre83()
		{
			// If the project already has up-to-date configurations then we don't need to migrate
			var configSettingsDir = LcmFileHelper.GetConfigSettingsDir(Cache.ProjectId.ProjectFolder);
			var newDictionaryConfigLoc = Path.Combine(configSettingsDir, DCL.DictionaryConfigurationDirectoryName);
			if (DCM.ConfigFilesInDir(newDictionaryConfigLoc).Any())
			{
				return false;
			}
			var newReversalIndexConfigLoc = Path.Combine(configSettingsDir, DCL.ReversalIndexConfigurationDirectoryName);
			if (DCM.ConfigFilesInDir(newReversalIndexConfigLoc).Any())
			{
				return false;
			}
			// If the project has old configurations, we need to migrate them; if it doesn't, there is nothing worth migrating.
			return Directory.Exists(configSettingsDir) && Directory.EnumerateFiles(configSettingsDir, "*.fwlayout").Any();
		}

		/// <summary>ILayoutConverter implementation</summary>
		public void AddDictionaryTypeItem(XmlNode layoutNode, List<XmlDocConfigureDlg.LayoutTreeNode> oldNodes)
		{
			// layoutNode is expected to look similar to:
			//<layoutType label="Stem-based (complex forms as main entries)" layout="publishStem">
			//<configure class="LexEntry" label="Main Entry" layout="publishStemEntry" />
			//<configure class="LexEntry" label="Minor Entry" layout="publishStemMinorEntry" hideConfig="true" />
			//</layoutType>
			var label = XmlUtils.GetMandatoryAttributeValue(layoutNode, "label");
			var layout = XmlUtils.GetMandatoryAttributeValue(layoutNode, "layout");
			m_logger.WriteLine(string.Format("Migrating old fwlayout and parts config: '{0}' - {1}.", label, layout));
			m_logger.IncreaseIndent();
			var configNodeList = oldNodes.Select(ConvertLayoutTreeNodeToConfigNode).ToList();
			var convertedModel = new DictionaryConfigurationModel { Parts = configNodeList, Label = label, Version = VersionPre83, AllPublications = true };
			DictionaryConfigurationModel.SpecifyParentsAndReferences(convertedModel.Parts);
			CopyNewDefaultsIntoConvertedModel(layout, convertedModel);
			convertedModel.Save();
			MigratePublicationLayoutSelection(layout, convertedModel.FilePath);
			m_logger.DecreaseIndent();
		}

		/// <summary>
		/// This method will take a skeleton model which has been already converted from LayoutTreeNodes
		/// and fill in data that we did not convert for some reason. It will use the static 8.3Alpha1
		/// shipping model for the layout which the old model used. (eg. publishStem)
		/// </summary>
		internal void CopyNewDefaultsIntoConvertedModel(string layout, DictionaryConfigurationModel convertedModel)
		{
			if (convertedModel.Version != VersionPre83)
				return;
			DictionaryConfigurationModel alpha83DefaultModel;
			const string extension = DictionaryConfigurationModel.FileExtension;
			var projectPath = Path.Combine(LcmFileHelper.GetConfigSettingsDir(Cache.ProjectId.ProjectFolder), m_configDirSuffixBeingMigrated);
			var alphaConfigsPath = Path.Combine(FwDirectoryFinder.FlexFolder, AlphaConfigFolder);

			var newDictionaryConfigLoc = Path.Combine(FwDirectoryFinder.DefaultConfigurations, DCL.DictionaryConfigurationDirectoryName);
			var newReversalConfigLoc = Path.Combine(FwDirectoryFinder.DefaultConfigurations, DCL.ReversalIndexConfigurationDirectoryName);
			const string defaultLexemeName = DCM.LexemeFileName + extension;
			const string defaultRootName = DCM.RootFileName + extension;
			const string defaultReversalName = DCM.ReversalFileName + extension;
			switch (layout)
			{
				case "publishStem":
				{
					convertedModel.FilePath = Path.Combine(projectPath, defaultLexemeName);
					// Though the name change from Stem to Lexeme happened after we shipped the Alpha we will change the name here for pre-Alpha projects
					alpha83DefaultModel = DCM.LoadConfigWithCurrentDefaults(Path.Combine(alphaConfigsPath, "Stem.fwdictconfig"), Cache,
						Path.Combine(newDictionaryConfigLoc, defaultLexemeName));
					break;
				}
				case "publishRoot":
				{
					convertedModel.FilePath = Path.Combine(projectPath, defaultRootName);
					alpha83DefaultModel = DCM.LoadConfigWithCurrentDefaults(Path.Combine(alphaConfigsPath, defaultRootName), Cache,
						Path.Combine(newDictionaryConfigLoc, "Root.fwdictconfig"));
					break;
				}
				case "publishReversal":
				{
					convertedModel.FilePath = Path.Combine(projectPath, defaultReversalName);
					alpha83DefaultModel = DCM.LoadConfigWithCurrentDefaults(Path.Combine(alphaConfigsPath, defaultReversalName), Cache,
						Path.Combine(newReversalConfigLoc, "AllReversalIndexes.fwdictconfig"));
					break;
				}
				default:
				{
					// If a user copied an old configuration FLEx appended '#' followed by a unique integer to the layout name.
					// We will write out the new configuration to a file which uses what the user named it but preserving the integer
					// as a potential customer support aid.
					var customSuffixIndex = layout.IndexOf('#');
					if (customSuffixIndex > 0 && layout.StartsWith("publishStem"))
					{
						var customFileName = string.Format("{0}-Stem-{1}{2}", convertedModel.Label, layout.Substring(customSuffixIndex), extension);
						convertedModel.FilePath = Path.Combine(projectPath, customFileName);
						alpha83DefaultModel = DCM.LoadConfigWithCurrentDefaults(Path.Combine(alphaConfigsPath, "Stem.fwdictconfig"), Cache,
							Path.Combine(newDictionaryConfigLoc, defaultLexemeName));
					}
					else if (customSuffixIndex > 0 && layout.StartsWith("publishRoot"))
					{
						var customFileName = string.Format("{0}-Root-{1}{2}", convertedModel.Label, layout.Substring(customSuffixIndex), extension);
						convertedModel.FilePath = Path.Combine(projectPath, customFileName);
						alpha83DefaultModel = DCM.LoadConfigWithCurrentDefaults(Path.Combine(alphaConfigsPath, defaultRootName), Cache,
							Path.Combine(newDictionaryConfigLoc, "Root.fwdictconfig"));
					}
					else if (layout.StartsWith("publishReversal")) // a reversal index for a specific language or a copied Reversal Index Config
					{
						// Label similar to publishReversal-en#Engli704, including one or both suffixes
						var languageSuffixIndex = layout.IndexOf('-') + 1;
						string reversalIndex;
						if (languageSuffixIndex > 0)
						{
							var languageCode = customSuffixIndex > 0
								? layout.Substring(languageSuffixIndex, customSuffixIndex - languageSuffixIndex)
								: layout.Substring(languageSuffixIndex);
							reversalIndex = Cache.ServiceLocator.WritingSystemManager.Get(languageCode).Id;
						}
						else
						{
							reversalIndex = "AllReversalIndexes";
						}
						var customFileName = customSuffixIndex > 0
							? string.Format("{0}-{1}-{2}{3}", convertedModel.Label, reversalIndex, layout.Substring(customSuffixIndex), extension)
							: string.Format("{0}{1}", reversalIndex, extension);
						convertedModel.FilePath = Path.Combine(projectPath, customFileName);
						alpha83DefaultModel = DCM.LoadConfigWithCurrentDefaults(Path.Combine(alphaConfigsPath, defaultReversalName), Cache,
							Path.Combine(newReversalConfigLoc, "AllReversalIndexes.fwdictconfig"));
					}
					else
						throw new NotImplementedException("Classified Dictionary migration or something has not yet been implemented.");
					break;
				}
			}
			CopyNewDefaultsIntoConvertedModel(convertedModel, alpha83DefaultModel);
		}

		/// <remarks>Internal only for tests; production entry point is MigrateOldConfigurationsIfNeeded()</remarks>
		internal void CopyNewDefaultsIntoConvertedModel(DictionaryConfigurationModel convertedModel, DictionaryConfigurationModel alphaDefaultModel)
		{
			convertedModel.SharedItems = convertedModel.SharedItems ?? new List<ConfigurableDictionaryNode>();
			convertedModel.IsRootBased = alphaDefaultModel.IsRootBased;

			// Stem-based treats Complex Forms as Main Entries. Previously, they had all been configured by the same Main Entries node,
			// but in FLEx versions 8.3.0 through 8.3.4, they were configured in a separate "Main Entries (Complex Forms)" node.
			// REVIEW (Hasso) 2017.02: would it be safe to rip out this intermediate step?
			if (Path.GetFileNameWithoutExtension(alphaDefaultModel.FilePath) == DCM.LexemeFileName)
			{
				convertedModel.Parts.Insert(0, convertedModel.Parts[0].DeepCloneUnderSameParent()); // Split Main into Main and Main (Complex)
				CopyDefaultsIntoConfigNode(convertedModel, convertedModel.Parts[0], alphaDefaultModel.Parts[0]); // Main Entry
				CopyDefaultsIntoMinorEntryNode(convertedModel, convertedModel.Parts[1], alphaDefaultModel.Parts[1], 0); // Main Entry (Complex Forms)
				convertedModel.Parts[1].Style = alphaDefaultModel.Parts[1].Style; // Main Entry had no style in the old model
				for (var i = 2; i < convertedModel.Parts.Count; ++i)
				{
					CopyDefaultsIntoMinorEntryNode(convertedModel, convertedModel.Parts[i], alphaDefaultModel.Parts[2], // Minor Entry (Variants)
						DictionaryNodeListOptions.ListIds.Variant);
				}
			}
			else
			{
				if (alphaDefaultModel.Label == DictionaryConfigurationModel.AllReversalIndexes
					&& convertedModel.Label != DictionaryConfigurationModel.AllReversalIndexes)
				{
					// If this is a WS-specific Reversal Index, set its WS
					DCM.SetWritingSystemForReversalModel(convertedModel, Cache);
				}
				else if (convertedModel.Label == DictionaryConfigurationModel.AllReversalIndexes)
				{
					convertedModel.WritingSystem = "";
				}

				CopyDefaultsIntoConfigNode(convertedModel, convertedModel.Parts[0], alphaDefaultModel.Parts[0]); // copy defaults into Main Entry
				for (var i = 1; i < convertedModel.Parts.Count; ++i)
				{
					// Any copies of the minor entry node in the model we are converting should use the defaults from the minor entry nodes,
					// split into Complex Forms and Variants
					var currentDefaultComplexNode = alphaDefaultModel.Parts[1];
					var currentDefaultVariantNode = alphaDefaultModel.Parts[2];

					var convertedNode = convertedModel.Parts[i];
					var selectedMinorEntryTypes = ((DictionaryNodeListOptions)convertedNode.DictionaryNodeOptions).Options;
					var hasComplexTypesSelected = HasComplexFormTypesSelected(selectedMinorEntryTypes);
					var hasVariantTypesSelected = HasVariantTypesSelected(selectedMinorEntryTypes);
					// We should create a Complex Forms node if this is the original (non-duplicate) node, if the user has selected at least one
					// Complex Form Type, or if the user has not selected any Types for display--otherwise this node would disappear entirely!
					var shouldCreateComplexNode = !convertedNode.IsDuplicate || hasComplexTypesSelected || !hasVariantTypesSelected;
					var shouldCreateVariantNode = !convertedNode.IsDuplicate || hasVariantTypesSelected || !hasComplexTypesSelected;
					if (shouldCreateComplexNode && shouldCreateVariantNode)
					{
						var convertedComplexNode = convertedNode;
						var convertedVariantNode = convertedNode.DeepCloneUnderSameParent();
						convertedModel.Parts.Insert(++i, convertedVariantNode);
						CopyDefaultsIntoMinorEntryNode(convertedModel, convertedComplexNode, currentDefaultComplexNode,
							DictionaryNodeListOptions.ListIds.Complex);
						CopyDefaultsIntoMinorEntryNode(convertedModel, convertedVariantNode, currentDefaultVariantNode,
							DictionaryNodeListOptions.ListIds.Variant);
					}
					else if (shouldCreateComplexNode)
					{
						CopyDefaultsIntoMinorEntryNode(convertedModel, convertedNode, currentDefaultComplexNode,
							DictionaryNodeListOptions.ListIds.Complex);
					}
					else // if (shouldCreateVariantNode)
					{
						CopyDefaultsIntoMinorEntryNode(convertedModel, convertedNode, currentDefaultVariantNode,
							DictionaryNodeListOptions.ListIds.Variant);
					}
				}
			}

			convertedModel.Version = VersionAlpha1; // Pre83 Migration is complete; update the version
		}

		private void CopyDefaultsIntoChildren(DictionaryConfigurationModel convertedModel, ConfigurableDictionaryNode convertedNode, ConfigurableDictionaryNode currentDefaultNode)
		{
			var currentDefaultChildren = new List<ConfigurableDictionaryNode>(currentDefaultNode.Children);
			var matchedChildren = new List<ConfigurableDictionaryNode>();
			foreach (var child in convertedNode.Children)
			{
				var pathStringToNode = DCM.BuildPathStringFromNode(child);
				child.Label = HandleChildNodeRenaming(convertedModel.Version, child);
				// Attempt to find a matching node from the current default model from which to copy defaults
				ConfigurableDictionaryNode matchFromBase;
				if (TryGetMatchingNode(child.Label, currentDefaultChildren, matchedChildren, out matchFromBase))
					CopyDefaultsIntoConfigNode(convertedModel, child, matchFromBase);
				else
				{
					// This node does not match anything in the shipping defaults; it may be a custom field, or it may
					// have been overlooked before, or we may have garbage.  See https://jira.sil.org/browse/LT-16735.
					var parentType = DictionaryConfigurationController.GetLookupClassForCustomFieldParent(currentDefaultNode, Cache);
					bool isCustom;
					if (IsFieldValid(child.Label, parentType, out isCustom))
					{
						if (isCustom)
						{
							m_logger.WriteLine(string.Format("Could not match '{0}' in defaults, but it is a valid custom field.", pathStringToNode));
							SetNodeAsCustom(child, parentType);
						}
						else
						{
							m_logger.WriteLine(string.Format("Could not match '{0}' in defaults, but it actually exists in the model.", pathStringToNode));
							if (child.FieldDescription == null)
								child.FieldDescription = child.Label;
						}
					}
					else
					{
						m_logger.WriteLine(string.Format(
							"Could not match '{0}' in defaults. It may have been valid in a previous version, but is no longer. It will be removed next time the model is loaded.",
							pathStringToNode));
						// Treat this as a custom field so that unit tests will pass.
						SetNodeAsCustom(child, parentType);
					}
				}
			}
			//remove all the matches from default list
			currentDefaultChildren.RemoveAll(matchedChildren.Contains);
			foreach (var newChild in currentDefaultChildren)
			{
				m_logger.WriteLine(string.Format("'{0}{1}{2}' was not in the old version; adding from default config.",
					DCM.BuildPathStringFromNode(convertedNode), // BuildPath from convertedNode to display LabelSuffixes
					DCM.NodePathSeparator, newChild));
				convertedNode.Children.Add(newChild);
			}
		}

		/// <remarks>Internal only for tests, production entry point is MigrateOldConfigurationsIfNeeded()</remarks>
		internal ConfigurableDictionaryNode ConvertLayoutTreeNodeToConfigNode(XmlDocConfigureDlg.LayoutTreeNode node)
		{
			var convertedNode = new ConfigurableDictionaryNode
			{
				IsDuplicate = node.IsDuplicate,
				LabelSuffix = node.DupString,
				Before = node.Before,
				After = node.After,
				Between = node.Between,
				Style = node.StyleName,
				Label = node.Label,
				IsEnabled = node.Checked,
				DictionaryNodeOptions = CreateOptionsFromLayoutTreeNode(node)
			};

			// Custom fields were implicitly marked in the old configuration files.  Decode the implicit marking.  See LT-17032.
			if (node.Configuration != null && node.Configuration.Attributes != null)
			{
				var attr = node.Configuration.Attributes["ref"];
				convertedNode.IsCustomField = (attr != null && attr.Value == "$child");
			}

			// LT-17356 Converting the Label in HandleChildNodeRenaming() requires more info than we have there.
			// ReSharper disable LocalizableElement - Justification: node.Parent.Text should not be localized during migration.
			if (node.Label == "Bibliography" && node.Parent.Text == "Referenced Senses")
				// ReSharper restore LocalizableElement
			{
				convertedNode.Label = node.ClassName == "LexEntry" ? "Bibliography (Entry)" : "Bibliography (Sense)";
			}

			// ConfigurableDictionaryNode.Label properties don't include the suffix like XmlDocConfigureDlg.LayoutTreeNode.Label properties do.
			if (convertedNode.IsDuplicate)
			{
				// XmlDocConfigureDlg.LayoutTreeNode's that are duplicates of duplicates will have a set of suffixes
				// in the DupString property, separated by hyphens. The last one is what we want.
				var i = convertedNode.LabelSuffix.LastIndexOf("-", StringComparison.Ordinal);
				if (i >= 0)
					convertedNode.LabelSuffix = convertedNode.LabelSuffix.Substring(i + 1);
				var suffixNotation = string.Format(" ({0})", convertedNode.LabelSuffix);
				if (convertedNode.Label.EndsWith(suffixNotation))
				{
					convertedNode.Label = convertedNode.Label.Remove(convertedNode.Label.Length - suffixNotation.Length);
				}
				else
				{
					m_logger.WriteLine(string.Format("The node '{0}' does not seem to be a duplicate; treating as original",
						BuildPathStringFromNode(node)));
					convertedNode.LabelSuffix = null;
					convertedNode.IsDuplicate = false;
				}
			}

			if (node.Nodes.Count > 0)
			{
				convertedNode.Children = new List<ConfigurableDictionaryNode>();
				foreach (XmlDocConfigureDlg.LayoutTreeNode childNode in node.Nodes)
				{
					convertedNode.Children.Add(ConvertLayoutTreeNodeToConfigNode(childNode));
				}
			}
			return convertedNode;
		}

		private static string BuildPathStringFromNode(XmlDocConfigureDlg.LayoutTreeNode child)
		{
			var path = string.Format("{0} ({1})", child.Label, child.DupString);
			var node = child;
			while (node.Parent != null // If 'Minor Entry' is duplicated, both copies get a new, common parent 'Minor Entry', which does not affect migration
				// apart from making log entries about 'Minor Entry > Minor Entry (1) > and so on'
				&& !(node.Parent.Parent == null || ((XmlDocConfigureDlg.LayoutTreeNode)node.Parent).Label.Equals(node.Label)))
			{
				node = (XmlDocConfigureDlg.LayoutTreeNode)node.Parent;
				path = node.Label + DCM.NodePathSeparator + path;
			}
			return path;
		}

		private DictionaryNodeOptions CreateOptionsFromLayoutTreeNode(XmlDocConfigureDlg.LayoutTreeNode node)
		{
			DictionaryNodeOptions options = null;
			if (!string.IsNullOrEmpty(node.WsType))
			{
				options = new DictionaryNodeWritingSystemOptions
				{
					DisplayWritingSystemAbbreviations = node.ShowWsLabels,
					WsType = DictionaryConfigurationController.GetWsTypeFromMagicWsName(node.WsType),
					Options = MigrateWsOptions(node.WsLabel)
				};
			}
			if (node.ShowSenseConfig)
			{
				string before = null, style = null, after = null;
				if (!string.IsNullOrEmpty(node.Number))
				{
					node.SplitNumberFormat(out before, out style, out after);
				}
				options = new DictionaryNodeSenseOptions
				{
					DisplayEachSenseInAParagraph = node.ShowSenseAsPara,
					ShowSharedGrammarInfoFirst = node.ShowSingleGramInfoFirst,
					NumberEvenASingleSense = node.NumberSingleSense,
					BeforeNumber = before,
					AfterNumber = after,
					NumberingStyle = style,
					NumberStyle = GenerateNumberStyleFromLayoutTreeNode(node)
				};
			}
			if (!string.IsNullOrEmpty(node.LexRelType))
			{
				options = new DictionaryNodeListOptions();
				SetListOptionsProperties(node.LexRelType, node.LexRelTypeSequence, (DictionaryNodeListOptions)options);
			}
			if (!string.IsNullOrEmpty(node.EntryType))
			{
				// Root-based Minor Entry - Components should not have a display-each-in-paragraph checkbox. See LT-15834.
				if (node.EntryType == "complex" && node.PartName != "LexEntry-Jt-StemMinorComponentsConfig")
				{
					options = new DictionaryNodeListAndParaOptions { DisplayEachInAParagraph = node.ShowComplexFormPara };

					if (node.PartName == "LexEntry-Jt-RootSubentriesConfig")
					{
						// LT-15834
						((DictionaryNodeListAndParaOptions)options).DisplayEachInAParagraph = true;
					}
				}
				else
				{
					options = new DictionaryNodeListOptions();
				}
				SetListOptionsProperties(node.EntryType, node.EntryTypeSequence, (DictionaryNodeListOptions)options);
			}

			return options;
		}

		/// <remarks>Internal only for tests; production entry point is MigrateOldConfigurationsIfNeeded()</remarks>
		internal void CopyDefaultsIntoMinorEntryNode(DictionaryConfigurationModel convertedModel, ConfigurableDictionaryNode convertedNode, ConfigurableDictionaryNode currentDefaultNode, DictionaryNodeListOptions.ListIds complexOrVariant)
		{
			convertedNode.Label = currentDefaultNode.Label;
			var nodeOptions = convertedNode.DictionaryNodeOptions as DictionaryNodeListOptions;
			if (nodeOptions != null)
			{
				nodeOptions.ListId = complexOrVariant;
				var availableOptions = complexOrVariant == DictionaryNodeListOptions.ListIds.Complex
					? AvailableComplexFormTypes
					: AvailableVariantTypes;
				nodeOptions.Options = nodeOptions.Options.Where(option => availableOptions.Contains(option.Id)).ToList();
			}
			CopyDefaultsIntoConfigNode(convertedModel, convertedNode, currentDefaultNode);
		}

		/// <remarks>Internal only for tests; production entry point is MigrateOldConfigurationsIfNeeded()</remarks>
		internal bool HasComplexFormTypesSelected(List<DictionaryNodeListOptions.DictionaryNodeOption> options)
		{
			return AvailableComplexFormTypes.Intersect(options.Where(option => option.IsEnabled).Select(option => option.Id)).Any();
		}

		/// <remarks>Internal only for tests, production entry point is MigrateOldConfigurationsIfNeeded()</remarks>
		internal IEnumerable<string> AvailableComplexFormTypes
		{
			get
			{
				return new[] { XmlViewsUtils.GetGuidForUnspecifiedComplexFormType().ToString() }
					.Union(Cache.LangProject.LexDbOA.ComplexEntryTypesOA.ReallyReallyAllPossibilities.Select(item => item.Guid.ToString()));
			}
		}

		/// <remarks>Internal only for tests, production entry point is MigrateOldConfigurationsIfNeeded()</remarks>
		internal bool HasVariantTypesSelected(List<DictionaryNodeListOptions.DictionaryNodeOption> options)
		{
			return AvailableVariantTypes.Intersect(options.Where(option => option.IsEnabled).Select(option => option.Id)).Any();
		}

		private IEnumerable<string> AvailableVariantTypes
		{
			get
			{
				return new[] { XmlViewsUtils.GetGuidForUnspecifiedVariantType().ToString() }
					.Union(Cache.LangProject.LexDbOA.VariantEntryTypesOA.ReallyReallyAllPossibilities.Select(item => item.Guid.ToString()));
			}
		}

		/// <summary>
		/// This method will copy values that were not converted (eg. FieldDescription and SubField) from the current default node
		/// into the converted node and add any children that are new in the current defaults to the converted node. The order of children
		/// in the converted node is maintained.
		/// </summary>
		internal void CopyDefaultsIntoConfigNode(DictionaryConfigurationModel convertedModel, ConfigurableDictionaryNode convertedNode, ConfigurableDictionaryNode currentDefaultNode)
		{
			if (convertedNode.Label != currentDefaultNode.Label)
			{
				// This check is necessary to handle splitting "Minor Entries" to
				// "Minor Entries (Complex Forms)" and "Minor Entries (Variants)".
				if (!currentDefaultNode.Label.StartsWith(convertedNode.Label + " "))
				{
					throw new ArgumentException(string.Format("Cannot merge two nodes that do not match. [{0}, {1}]",
						convertedNode.Label, currentDefaultNode.Label));
				}
			}
			convertedNode.FieldDescription = currentDefaultNode.FieldDescription;
			convertedNode.SubField = currentDefaultNode.SubField;
			convertedNode.ReferenceItem = currentDefaultNode.ReferenceItem;
			if (convertedNode.DictionaryNodeOptions == null)
				convertedNode.DictionaryNodeOptions = currentDefaultNode.DictionaryNodeOptions;
			if (string.IsNullOrEmpty(convertedNode.Style))
			{
				convertedNode.StyleType = currentDefaultNode.StyleType;
				convertedNode.Style = currentDefaultNode.Style;
			}
			convertedNode.CSSClassNameOverride = currentDefaultNode.CSSClassNameOverride;

			if (convertedModel.Version == VersionPre83 && IsReferencedEntriesNode(convertedNode))
			{
				ConvertReferencedEntries(convertedNode, currentDefaultNode);
				return;
			}

			// top-level nodes (Main, Minor, and Reversal Index Entry) don't need Surrounding Characters (Before, Between, After)
			if (convertedNode.Parent == null)
				convertedNode.After = convertedNode.Between = convertedNode.Before = null;

			if (convertedNode.Children == null || !convertedNode.Children.Any())
			{
				// if the new default node has children and the converted node doesn't, they need to be added
				if (currentDefaultNode.Children != null && currentDefaultNode.Children.Any())
				{
					convertedNode.Children = new List<ConfigurableDictionaryNode>(currentDefaultNode.Children);
					if (convertedNode.Label == "Subsenses")
					{
						// considering these clone recursive because we are presently copying the entire model
						convertedNode.Children = new List<ConfigurableDictionaryNode>(convertedNode.Parent.Children.Select(
							node => node.DeepCloneUnderParent(convertedNode, true)));
						CopyDefaultsIntoChildren(convertedModel, convertedNode, currentDefaultNode);
					}
				}
				return;
			}
			// if there are child lists to merge then merge them
			if (currentDefaultNode.Children != null && currentDefaultNode.Children.Any())
			{
				CopyDefaultsIntoChildren(convertedModel, convertedNode, currentDefaultNode);
			}
			else // if the converted node has children and default doesn't
			{
				throw new Exception("These nodes are not likely to match the convertedModel.");
			}
		}

		/// <summary>
		/// The new defaults made the following changes to this section of the old configuration (LT-15801):
		/// Removed Referenced Sense Headword from the configuration tree.
		///	Whether the target is a whole entry or a specific sense, show the Headword if "Referenced Headword" is checked.
		/// Remove Summary Definition.
		/// Change "Gloss" to "Gloss (or Summary Definition)".
		///	If the target is a specific sense, show that sense's gloss.
		///	If the target is a whole entry show the Summary Definition for the entry (if there is one)
		/// The IsEnabled property on the new nodes will be the logical or of the two old node values
		/// </summary>
		private void ConvertReferencedEntries(ConfigurableDictionaryNode convertedNode, ConfigurableDictionaryNode defaultNode)
		{
			var newChildren = new List<ConfigurableDictionaryNode>(defaultNode.Children.Select(n => n.DeepCloneUnderParent(convertedNode)));
			var newHeadword = newChildren.First(child => child.Label == "Referenced Headword");
			var oldHeadwordNode = convertedNode.Children.First(child => child.Label == "Referenced Headword");
			// Usually "Referenced Sense Headword" but in one case it is "Referenced Sense"
			var oldSenseHeadwordNode = convertedNode.Children.FirstOrDefault(child => child.Label.StartsWith("Referenced Sense"))
				?? new ConfigurableDictionaryNode();
			newHeadword.IsEnabled = oldHeadwordNode.IsEnabled || oldSenseHeadwordNode.IsEnabled;
			newHeadword.Before = !string.IsNullOrEmpty(oldHeadwordNode.Before) ? oldHeadwordNode.Before : oldSenseHeadwordNode.Before;
			newHeadword.Between = !string.IsNullOrEmpty(oldHeadwordNode.Between) ? oldHeadwordNode.Between : oldSenseHeadwordNode.Between;
			newHeadword.After = !string.IsNullOrEmpty(oldHeadwordNode.After) ? oldHeadwordNode.After : oldSenseHeadwordNode.After;
			// Set the new Headword options based off the old headword (or old sense headword) settings
			var oldOptions = oldHeadwordNode.DictionaryNodeOptions ?? oldSenseHeadwordNode.DictionaryNodeOptions;
			if (oldHeadwordNode.DictionaryNodeOptions != null)
			{
				newHeadword.DictionaryNodeOptions = oldOptions.DeepClone();
			}

			var newGloss = newChildren.First(child => child.Label == "Gloss (or Summary Definition)");
			var oldSummaryNode = convertedNode.Children.First(child => child.Label == "Summary Definition");
			var oldGlossNode = convertedNode.Children.FirstOrDefault(child => child.Label == "Gloss") ?? new ConfigurableDictionaryNode();
			newGloss.IsEnabled = oldSummaryNode.IsEnabled || oldGlossNode.IsEnabled;
			newGloss.Before = !string.IsNullOrEmpty(oldGlossNode.Before) ? oldGlossNode.Before : oldSummaryNode.Before;
			newGloss.Between = !string.IsNullOrEmpty(oldGlossNode.Between) ? oldGlossNode.Between : oldSummaryNode.Between;
			newGloss.After = !string.IsNullOrEmpty(oldGlossNode.After) ? oldGlossNode.After : oldSummaryNode.After;
			newGloss.Style = !string.IsNullOrEmpty(oldGlossNode.Style) ? oldGlossNode.Style : oldSummaryNode.Style;
			// Set the new gloss options based off the old summary definition (or old gloss) settings
			oldOptions = oldSummaryNode.DictionaryNodeOptions ?? oldGlossNode.DictionaryNodeOptions;
			if (oldHeadwordNode.DictionaryNodeOptions != null)
			{
				newGloss.DictionaryNodeOptions = oldOptions.DeepClone();
			}

			if (convertedNode.Children.Count != 4)
			{
				m_logger.WriteLine(string.Format("{0} had children (probably duplicates) that were not migrated.",
					DCM.BuildPathStringFromNode(convertedNode)));
			}
			convertedNode.Children = newChildren;
		}

		private static bool IsReferencedEntriesNode(ConfigurableDictionaryNode convertedNode)
		{
			return convertedNode.Label == "Referenced Entries";
		}


		private void SetNodeAsCustom(ConfigurableDictionaryNode child, string parentType)
		{
			m_logger.IncreaseIndent();
			SetupCustomFieldNameDictionaries();
			Dictionary<string, string> cfLabelToName;
			SetupCustomField(child,
				// If we know the Custom Field's parent's type, pass a dictionary of the Custom Fields available on that type
				(parentType != null && m_classToCustomFieldsLabelToName.TryGetValue(parentType, out cfLabelToName))
					? cfLabelToName
					: null);
			m_logger.DecreaseIndent();
		}

		/// <summary>
		/// Check whether the given field is valid for the given type (class/interface).  Also set an output flag for whether
		/// it is a custom field.
		/// </summary>
		private bool IsFieldValid(string field, string type, out bool isCustom)
		{
			isCustom = false;
			if (type == null)
				return false;
			try
			{
				// Convert an interface type name to a class type name if necessary.
				if (type.StartsWith("I") && char.IsUpper(type[1]))
					type = type.Substring(1);
				var metaDataCache = Cache.MetaDataCacheAccessor;
				var clsid = metaDataCache.GetClassId(type);
				if (clsid == 0)
					return false;
				var flid = metaDataCache.GetFieldId2(clsid, field, true);
				if (flid == 0)
					return false;
				isCustom = Cache.GetIsCustomField(flid);
			}
			catch
			{
				return false;
			}
			return true;
		}

		/// <summary>
		/// Some configuration nodes had name changes in the new verison
		/// </summary>
		/// <remarks>
		/// If we have later version label changes, we should no longer have to change this
		/// method. Later changes will go in FirstAlphaMigrator.HandleNodeWiseChanges().
		/// The only reason to put changes here is if we discover that the PreHistoricMigrator
		/// crashes loading an old database.
		/// </remarks>
		private static string HandleChildNodeRenaming(int version, ConfigurableDictionaryNode child)
		{
			if (version != VersionPre83) // safety valve; shouldn't ever happen
			{
				Debug.Assert(false, "PreHistoricMigrator should not be running on this file!");
				return child.Label;
			}
			switch (child.Label)
			{
				case "Components":
					if (child.Parent.Label == "Component References")
						return "Referenced Entries";
					break;
				case "Abbreviation":
					// Don't rename Components -> Complex Form Type -> Abbreviation,
					// but do rename Subentries -> CFT -> Abbreviations to Reverse Abbreviation
					if (child.Parent.Label == "Complex Form Type" &&
						child.Parent.Parent.Label == "Subentries")
						return "Reverse Abbreviation";
					break;
				case "Features":
					if (child.Parent.Label == "Grammatical Info.")
						return "Inflection Features";
					break;
				case "Form":
					if (child.Parent.Label == "Reversal Entry")
						return "Reversal Form";
					if (child.Parent.Label == "Subentry Under Reference")
						return "Referenced Headword";
					break;
				case "Category":
					if (child.Parent.Label == "Reversal Entry")
						return "Reversal Category";
					break;
				case "Type":
					if (child.Parent.Label == "Variants (of Entry)")
						return "Variant Type";
					break;
				case "Homograph Number":
					return "Secondary Homograph Number";
				//case "Headword": now handled in FirstAlphaMigrator
			}
			return child.Label;
		}

		private void SetupCustomFieldNameDictionaries()
		{
			if (m_classToCustomFieldsLabelToName != null)
				return;
			m_classToCustomFieldsLabelToName = new Dictionary<string, Dictionary<string, string>>();
			var metaDataCache = Cache.MetaDataCacheAccessor;

			foreach (var classToCustomFields in DictionaryConfigurationController.BuildCustomFieldMap(Cache))
			{
				var labelToName = m_classToCustomFieldsLabelToName[classToCustomFields.Key] = new Dictionary<string, string>();
				foreach (var flid in classToCustomFields.Value)
				{
					labelToName[metaDataCache.GetFieldLabel(flid)] = metaDataCache.GetFieldName(flid);
				}
			}
		}

		/// <param name="node">the node that has been identified as a Custom Field</param>
		/// <param name="labelToName">a Dictionary of possible Custom Field Names, keyed by Label</param>
		private void SetupCustomField(ConfigurableDictionaryNode node, IDictionary<string, string> labelToName)
		{
			node.IsCustomField = true;
			string nodeName;
			if (labelToName != null && labelToName.TryGetValue(node.Label, out nodeName))
			{
				m_logger.WriteLine(string.Format("Found name '{0}' for Custom Field labeled '{1}'; using.", nodeName, node.Label));
				node.FieldDescription = nodeName;
			}
			else
			{
				node.FieldDescription = node.Label;
			}

			var metaDataCache = (IFwMetaDataCacheManaged)Cache.MetaDataCacheAccessor;
			if (node.Children != null)
			{
				foreach (var child in node.Children)
				{
					m_logger.WriteLine(string.Format("Treating '{0}' as custom.",
						DCM.BuildPathStringFromNode(child)));
					SetupCustomField(child, null);
					// Children should be not marked as custom unless we know they are.
					var field = GetFieldIdForNode(child, metaDataCache);
					child.IsCustomField = field != 0 && metaDataCache.IsCustom(field);
				}
			}
			else
			{
				var field = GetFieldIdForNode(node, metaDataCache);
				if (field != 0)
				{
					var listId = metaDataCache.GetFieldListRoot(field);
					if (listId != Guid.Empty)
						DictionaryConfigurationController.AddFieldsForPossibilityList(node);
					var type = metaDataCache.GetFieldType(field);
					// Create the proper node options object.
					switch (type)
					{
						case (int)CellarPropertyType.ReferenceCollection:
						case (int)CellarPropertyType.ReferenceSequence:
							if (listId != Guid.Empty)
								node.DictionaryNodeOptions = new DictionaryNodeListOptions();
							break;
						case (int)CellarPropertyType.OwningCollection:
						case (int)CellarPropertyType.OwningSequence:
							break;
						case (int)CellarPropertyType.MultiUnicode:
						case (int)CellarPropertyType.MultiString:
							node.DictionaryNodeOptions = new DictionaryNodeWritingSystemOptions();
							break;
						case (int)CellarPropertyType.ReferenceAtomic:
							if (listId != Guid.Empty)
								node.DictionaryNodeOptions = new DictionaryNodeListOptions();
							break;
						case (int)CellarPropertyType.OwningAtomic:
							break;
						case (int)CellarPropertyType.GenDate:
						case (int)CellarPropertyType.Time:
							break;
						case (int)CellarPropertyType.String:
							break;
					}
				}
			}
		}

		private int GetFieldIdForNode(ConfigurableDictionaryNode node, IFwMetaDataCacheManaged metaDataCache)
		{
			try
			{
				var parentType = DictionaryConfigurationController.GetLookupClassForCustomFieldParent(node.Parent, Cache);
				if (parentType == null)
					return 0;
				var flid = metaDataCache.GetFieldId(parentType, node.FieldDescription, false);
				return flid;
			}
			catch (Exception)
			{
				return 0;
			}
		}
		/// <summary>Attempts to find and return a node from the currentDefaultChildren whose Label matches childLabel.</summary>
		/// <returns>true if successful</returns>
		private static bool TryGetMatchingNode(string childLabel,
			IEnumerable<ConfigurableDictionaryNode> currentDefaultChildren, List<ConfigurableDictionaryNode> matchedChildren,
			out ConfigurableDictionaryNode matchFromCurrentDefault)
		{
			matchFromCurrentDefault = currentDefaultChildren.FirstOrDefault(baseChild => childLabel == baseChild.Label);
			if (matchFromCurrentDefault != null)
			{
				matchedChildren.Add(matchFromCurrentDefault);
				return true;
			}
			return false;
		}
		private void SetListOptionsProperties(string type, string sequence, DictionaryNodeListOptions options)
		{
			options.Options = new List<DictionaryNodeListOptions.DictionaryNodeOption>();
			options.ListId = (DictionaryNodeListOptions.ListIds)Enum.Parse(typeof(DictionaryNodeListOptions.ListIds), type, true);
			// Create a list of dictionary node options from a string of the format "+guid,-guid,+guid"
			options.Options.AddRange(sequence.Split(',').Select(id => new DictionaryNodeListOptions.DictionaryNodeOption
			{
				IsEnabled = id.StartsWith("+"),
				Id = id.Trim('+', '-', ' ')
			}));
		}

		private string GenerateNumberStyleFromLayoutTreeNode(XmlDocConfigureDlg.LayoutTreeNode node)
		{
			var styleSheet = FontHeightAdjuster.StyleSheetFromPropertyTable(m_propertyTable);
			const string senseNumberStyleBase = "Dictionary-SenseNumber";
			var senseNumberStyleName = senseNumberStyleBase;
			var matchedOrCreated = false;
			var styleNumberSuffix = 1;
			do
			{
				if (styleSheet.FindStyle(senseNumberStyleName) == null)
				{
					var senseNumberStyle = Cache.ServiceLocator.GetInstance<IStStyleFactory>().Create();
					Cache.LangProject.StylesOC.Add(senseNumberStyle);
					senseNumberStyle.Name = senseNumberStyleName;
					senseNumberStyle.Type = StyleType.kstCharacter;
					senseNumberStyle.UserLevel = 1;
					senseNumberStyle.IsBuiltIn = false;
					var propsBldr = TsStringUtils.MakePropsBldr();
					propsBldr.SetStrPropValue((int)FwTextPropType.ktptFontFamily, node.NumFont);
					if (!string.IsNullOrEmpty(node.NumStyle))
					{
						if (node.NumStyle.Contains("-bold"))
						{
							propsBldr.SetIntPropValues((int)FwTextPropType.ktptBold, (int)FwTextPropVar.ktpvEnum, (int)FwTextToggleVal.kttvOff);
						}
						else if (node.NumStyle.Contains("bold"))
						{
							propsBldr.SetIntPropValues((int)FwTextPropType.ktptBold, (int)FwTextPropVar.ktpvEnum, (int)FwTextToggleVal.kttvForceOn);
						}
						if (node.NumStyle.Contains("-italic"))
						{
							propsBldr.SetIntPropValues((int)FwTextPropType.ktptItalic, (int)FwTextPropVar.ktpvEnum, (int)FwTextToggleVal.kttvOff);
						}
						else if (node.NumStyle.Contains("italic"))
						{
							propsBldr.SetIntPropValues((int)FwTextPropType.ktptItalic, (int)FwTextPropVar.ktpvEnum, (int)FwTextToggleVal.kttvForceOn);
						}
						senseNumberStyle.Rules = propsBldr.GetTextProps();
					}
					styleSheet.PutStyle(senseNumberStyleName, "Used for configuring some sense numbers in the dictionary",
						senseNumberStyle.Hvo, 0, 0, (int)StyleType.kstCharacter, false, false, propsBldr.GetTextProps());
					matchedOrCreated = true;
				}
				else if (LayoutOptionsMatchStyle(styleSheet.Styles[senseNumberStyleName], node))
				{
					matchedOrCreated = true;
				}
				else
				{
					senseNumberStyleName = string.Format("{0}-{1}", senseNumberStyleBase, ++styleNumberSuffix);
				}
			} while (!matchedOrCreated);
			return senseNumberStyleName;
		}

		private bool LayoutOptionsMatchStyle(BaseStyleInfo style, XmlDocConfigureDlg.LayoutTreeNode node)
		{
			// if the style isn't even a character style
			if (!style.IsCharacterStyle)
			{
				return false;
			}
			var fontInfo = style.DefaultCharacterStyleInfo;
			// if nothing about bold or italic are in the node but there is information in the style
			if (string.IsNullOrEmpty(node.NumStyle) && (fontInfo.Bold.ValueIsSet || fontInfo.Italic.ValueIsSet))
			{
				return false;
			}
			// if we have bold or italic info in the node but it doesn't match the style
			if (!string.IsNullOrEmpty(node.NumStyle) && ((node.NumStyle.Contains("-bold") && fontInfo.Bold.ValueIsSet && fontInfo.Bold.Value) ||
				(!node.NumStyle.Contains("-bold") && node.NumStyle.Contains("bold") && fontInfo.Bold.ValueIsSet && !fontInfo.Bold.Value) ||
				(node.NumStyle.Contains("bold") && !fontInfo.Bold.ValueIsSet) || (!node.NumStyle.Contains("bold") && fontInfo.Bold.ValueIsSet) ||
				(node.NumStyle.Contains("-italic") && fontInfo.Italic.ValueIsSet && fontInfo.Italic.Value) ||
				(!node.NumStyle.Contains("-italic") && node.NumStyle.Contains("italic") && fontInfo.Italic.ValueIsSet && !fontInfo.Italic.Value) ||
				(node.NumStyle.Contains("italic") && !fontInfo.Italic.ValueIsSet) || (!node.NumStyle.Contains("italic") && fontInfo.Italic.ValueIsSet)))
			{
				return false;
			}
			// if the font doesn't match
			if (string.IsNullOrEmpty(node.NumFont) && fontInfo.FontName.ValueIsSet || // node value is empty but fontInfo isn't
				!string.IsNullOrEmpty(node.NumFont) && !fontInfo.FontName.ValueIsSet || // fontinfo is empty but node value isn't
				(fontInfo.FontName.ValueIsSet && string.Compare(node.NumFont, fontInfo.FontName.Value, StringComparison.Ordinal) != 0))
			{
				// node value was empty but fontInfo isn't or
				// fontInfo was empty but node value wasn't or
				// both strings had content but it didn't match
				return false;
			}
			return true;
		}
		private List<DictionaryNodeListOptions.DictionaryNodeOption> MigrateWsOptions(string wsLabel)
		{
			return wsLabel.Split(',').Select(item => new DictionaryNodeListOptions.DictionaryNodeOption { Id = item.Trim(), IsEnabled = true }).ToList();
		}

		private void MigratePublicationLayoutSelection(string oldLayout, string newPath)
		{
			if (oldLayout.Equals(m_propertyTable.GetStringProperty("DictionaryPublicationLayout", string.Empty)))
			{
				m_propertyTable.SetProperty("DictionaryPublicationLayout", newPath, true);
			}
			else if (oldLayout.Equals(m_propertyTable.GetStringProperty("ReversalIndexPublicationLayout", string.Empty)))
			{
				m_propertyTable.SetProperty("ReversalIndexPublicationLayout", newPath, true);
			}
		}

		#region trivial portions of the ILayoutConverter implementation
		public IEnumerable<XmlNode> GetLayoutTypes()
		{
			return m_layoutInventory.GetLayoutTypes();
		}

		public LcmCache Cache { get; private set; }
		public StringTable StringTable { get { return null; } } // used solely for l10n of nodes, which is a hindrance to migration.
		public LayoutLevels LayoutLevels { get; private set; }

		public void ExpandWsTaggedNodes(string sWsTag)
		{
			m_layoutInventory.ExpandWsTaggedNodes(sWsTag);
		}

		public void SetOriginalIndexForNode(XmlDocConfigureDlg.LayoutTreeNode mainLayoutNode)
		{
			//Not important for migration
		}

		public XmlNode GetLayoutElement(string className, string layoutName)
		{
			return LegacyConfigurationUtils.GetLayoutElement(m_layoutInventory, className, layoutName);
		}

		public XmlNode GetPartElement(string className, string sRef)
		{
			return LegacyConfigurationUtils.GetPartElement(m_partInventory, className, sRef);
		}

		public void BuildRelationTypeList(XmlDocConfigureDlg.LayoutTreeNode ltn)
		{
			//Not important for migration - Handled separately by the new configuration dialog
		}

		public void BuildEntryTypeList(XmlDocConfigureDlg.LayoutTreeNode ltn, string layoutName)
		{
			//Not important for migration - Handled separately by the new configuration dialog
		}

		public void LogConversionError(string errorLog)
		{
			m_logger.WriteLine(errorLog);
		}
		#endregion

		/// <summary>
		/// Only use for tests which don't enter the migrator class at the standard entry point
		/// </summary>
		internal SimpleLogger SetTestLogger
		{
			set { m_logger = value; }
		}
	}
}