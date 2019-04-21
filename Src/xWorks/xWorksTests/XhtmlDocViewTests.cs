﻿// Copyright (c) 2014 SIL International
// This software is licensed under the LGPL, version 2.1 or later
// (http://www.gnu.org/licenses/lgpl-2.1.html)

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Xml;
using NUnit.Framework;
using SIL.LCModel.Core.Text;
using SIL.IO;
using SIL.FieldWorks.Common.FwUtils;
using SIL.LCModel;
using SIL.LCModel.Infrastructure;
using XCore;

namespace SIL.FieldWorks.XWorks
{
	[TestFixture]
	class XhtmlDocViewTests : XWorksAppTestBase, IDisposable
	{
		private PropertyTable m_propertyTable;
		private Mediator m_mediator;
		[TestFixtureSetUp]
		public override void FixtureInit()
		{
			// Init() is called from XWorksAppTestBase's TestFixtureSetup, so we won't call it here.
			base.FixtureInit();
			var testProjPath = Path.Combine(Path.GetTempPath(), "XhtmlDocViewtestProj");
			if(Directory.Exists(testProjPath))
				Directory.Delete(testProjPath, true);
			Directory.CreateDirectory(testProjPath);
			Cache.ProjectId.Path = testProjPath;
		}

		private const string ConfigurationTemplate = "<?xml version='1.0' encoding='utf-8'?><DictionaryConfiguration name='AConfigPubtest'>" +
		"<Publications></Publications></DictionaryConfiguration>";
		private const string ConfigurationTemplateWithAllPublications = "<?xml version='1.0' encoding='utf-8'?><DictionaryConfiguration name='AConfigPubtest' allPublications='true'>" +
		"<Publications></Publications></DictionaryConfiguration>";

		[Test]
		public void SplitPublicationsByConfiguration_AllPublicationIsIn()
		{
			using (var helper = new UndoableUnitOfWorkHelper(Cache.ActionHandlerAccessor, "doit", "undoit"))
			{
				var testPubItem = Cache.ServiceLocator.GetInstance<ICmPossibilityFactory>().Create();
				int enId = Cache.WritingSystemFactory.GetWsFromStr("en");
				var testPubName = TsStringUtils.MakeString("TestPub", enId);
				Cache.LangProject.LexDbOA.PublicationTypesOA.PossibilitiesOS.Add(testPubItem);
				testPubItem.Name.set_String(enId, testPubName);
				var allPubsConfig = ConfigurationTemplateWithAllPublications;
				using(var docView = new TestXhtmlDocView())
				using(var tempConfigFile = TempFile.WithFilename(Path.Combine(Path.GetTempPath(),
																								  "AllPubsConf"+DictionaryConfigurationModel.FileExtension)))
				{
					docView.SetConfigObjectName("Dictionary");
					docView.SetMediator(m_mediator);
					docView.SetPropertyTable(m_propertyTable);
					File.WriteAllText(tempConfigFile.Path, allPubsConfig);
					List<string> pubsInConfig;
					List<string> pubsNotInConfig;
					// SUT
					docView.SplitPublicationsByConfiguration(
						Cache.LangProject.LexDbOA.PublicationTypesOA.PossibilitiesOS,
						tempConfigFile.Path, out pubsInConfig, out pubsNotInConfig);
					CollectionAssert.Contains(pubsInConfig, testPubName.Text);
					CollectionAssert.DoesNotContain(pubsNotInConfig, testPubName.Text);
				}
			}
		}

		[Test]
		public void SplitPublicationsByConfiguration_UnmatchedPublicationIsOut()
		{
			using (var helper = new UndoableUnitOfWorkHelper(Cache.ActionHandlerAccessor, "doit", "undoit"))
			{
				var testPubItem = Cache.ServiceLocator.GetInstance<ICmPossibilityFactory>().Create();
				int enId = Cache.WritingSystemFactory.GetWsFromStr("en");
				var testPubName = TsStringUtils.MakeString("TestPub", enId);
				Cache.LangProject.LexDbOA.PublicationTypesOA.PossibilitiesOS.Add(testPubItem);
				testPubItem.Name.set_String(enId, testPubName);
				var notTestPubItem = Cache.ServiceLocator.GetInstance<ICmPossibilityFactory>().Create();
				var notTestPubName = TsStringUtils.MakeString("NotTestPub", enId);
				Cache.LangProject.LexDbOA.PublicationTypesOA.PossibilitiesOS.Add(notTestPubItem);
				notTestPubItem.Name.set_String(enId, notTestPubName);
				var configWithoutTestPub = ConfigurationTemplate.Replace("</Publications>", "<Publication>NotTestPub</Publication></Publications>");
				using(var docView = new TestXhtmlDocView())
				using(var tempConfigFile = TempFile.WithFilename(Path.Combine(Path.GetTempPath(),
																								  "AllPubsConf"+DictionaryConfigurationModel.FileExtension)))
				{
					docView.SetConfigObjectName("Dictionary");
					docView.SetMediator(m_mediator);
					docView.SetPropertyTable(m_propertyTable);
					File.WriteAllText(tempConfigFile.Path, configWithoutTestPub);
					List<string> pubsInConfig;
					List<string> pubsNotInConfig;
					// SUT
					docView.SplitPublicationsByConfiguration(
						Cache.LangProject.LexDbOA.PublicationTypesOA.PossibilitiesOS,
						tempConfigFile.Path, out pubsInConfig, out pubsNotInConfig);
					CollectionAssert.DoesNotContain(pubsInConfig, testPubName.Text);
					CollectionAssert.Contains(pubsNotInConfig, testPubName.Text);
				}
			}
		}

		[Test]
		public void SplitPublicationsByConfiguration_MatchedPublicationIsIn()
		{
			using (var helper = new UndoableUnitOfWorkHelper(Cache.ActionHandlerAccessor, "doit", "undoit"))
			{
				var testPubItem = Cache.ServiceLocator.GetInstance<ICmPossibilityFactory>().Create();
				int enId = Cache.WritingSystemFactory.GetWsFromStr("en");
				var testPubName = TsStringUtils.MakeString("TestPub", enId);
				Cache.LangProject.LexDbOA.PublicationTypesOA.PossibilitiesOS.Add(testPubItem);
				testPubItem.Name.set_String(enId, testPubName);
				var configWithTestPub = ConfigurationTemplate.Replace("</Publications>", "<Publication>TestPub</Publication></Publications>");
				using(var docView = new TestXhtmlDocView())
				using(var tempConfigFile = TempFile.WithFilename(Path.Combine(Path.GetTempPath(),
																								  "Foo"+DictionaryConfigurationModel.FileExtension)))
				{
					docView.SetConfigObjectName("Dictionary");
					docView.SetMediator(m_mediator);
					docView.SetPropertyTable(m_propertyTable);
					File.WriteAllText(tempConfigFile.Path, configWithTestPub);
					List<string> inConfig;
					List<string> outConfig;
					// SUT
					docView.SplitPublicationsByConfiguration(
						Cache.LangProject.LexDbOA.PublicationTypesOA.PossibilitiesOS,
						tempConfigFile.Path, out inConfig, out outConfig);
					CollectionAssert.Contains(inConfig, testPubName.Text);
					CollectionAssert.DoesNotContain(outConfig, testPubName.Text);
				}
			}
		}

		[Test]
		public void SplitConfigurationsByPublication_ConfigWithAllPublicationIsIn()
		{
			using(var helper = new UndoableUnitOfWorkHelper(Cache.ActionHandlerAccessor, "doit", "undoit"))
			{
				var allPubsConfig = ConfigurationTemplateWithAllPublications;
				using(var docView = new TestXhtmlDocView())
				using(var tempConfigFile = TempFile.WithFilename(Path.Combine(Path.GetTempPath(),
																								  "AllPubsConf"+DictionaryConfigurationModel.FileExtension)))
				{
					docView.SetConfigObjectName("Dictionary");
					docView.SetMediator(m_mediator);
					docView.SetPropertyTable(m_propertyTable);
					File.WriteAllText(tempConfigFile.Path, allPubsConfig);
					IDictionary<string, string> configsWithPub;
					IDictionary<string, string> configsWithoutPub;
					var configurations = new Dictionary<string, string>();
					configurations["AConfigPubtest"] = tempConfigFile.Path;
					// SUT
					docView.SplitConfigurationsByPublication(configurations,
																		  "TestPub", out configsWithPub, out configsWithoutPub);
					CollectionAssert.Contains(configsWithPub.Values, tempConfigFile.Path);
					CollectionAssert.DoesNotContain(configsWithoutPub.Values, tempConfigFile.Path);
				}
			}
		}

		[Test]
		public void SplitConfigurationsByPublication_AllPublicationIsMatchedByEveryConfiguration()
		{
			using(var helper = new UndoableUnitOfWorkHelper(Cache.ActionHandlerAccessor, "doit", "undoit"))
			{
				var testPubItem = Cache.ServiceLocator.GetInstance<ICmPossibilityFactory>().Create();
				int enId = Cache.WritingSystemFactory.GetWsFromStr("en");
				var testPubName = TsStringUtils.MakeString("TestPub", enId);
				Cache.LangProject.LexDbOA.PublicationTypesOA.PossibilitiesOS.Add(testPubItem);
				testPubItem.Name.set_String(enId, testPubName);
				using(var docView = new TestXhtmlDocView())
				using(var tempConfigFile = TempFile.WithFilename(Path.Combine(Path.GetTempPath(),
																								  "NoPubsConf"+DictionaryConfigurationModel.FileExtension)))
				{
					docView.SetConfigObjectName("Dictionary");
					docView.SetMediator(m_mediator);
					docView.SetPropertyTable(m_propertyTable);
					File.WriteAllText(tempConfigFile.Path, ConfigurationTemplate);
					IDictionary<string, string> configsWithPub;
					IDictionary<string, string> configsWithoutPub;
					var configurations = new Dictionary<string, string>();
					configurations["AConfigPubtest"] = tempConfigFile.Path;
					// SUT
					docView.SplitConfigurationsByPublication(configurations,
																		  xWorksStrings.AllEntriesPublication, out configsWithPub, out configsWithoutPub);
					CollectionAssert.Contains(configsWithPub.Values, tempConfigFile.Path);
					CollectionAssert.IsEmpty(configsWithoutPub.Values, tempConfigFile.Path);
				}
			}
		}

		[Test]
		public void SplitConfigurationsByPublication_UnmatchedPublicationIsOut()
		{
			using(var helper = new UndoableUnitOfWorkHelper(Cache.ActionHandlerAccessor, "doit", "undoit"))
			{
				var testPubItem = Cache.ServiceLocator.GetInstance<ICmPossibilityFactory>().Create();
				int enId = Cache.WritingSystemFactory.GetWsFromStr("en");
				var testPubName = TsStringUtils.MakeString("TestPub", enId);
				Cache.LangProject.LexDbOA.PublicationTypesOA.PossibilitiesOS.Add(testPubItem);
				testPubItem.Name.set_String(enId, testPubName);
				var notTestPubItem = Cache.ServiceLocator.GetInstance<ICmPossibilityFactory>().Create();
				var notTestPubName = TsStringUtils.MakeString("NotTestPub", enId);
				Cache.LangProject.LexDbOA.PublicationTypesOA.PossibilitiesOS.Add(notTestPubItem);
				notTestPubItem.Name.set_String(enId, notTestPubName);
				var configWithoutTestPub = ConfigurationTemplate.Replace("</Publications>", "<Publication>NotTestPub</Publication></Publications>");
				using(var docView = new TestXhtmlDocView())
				using(var tempConfigFile = TempFile.WithFilename(Path.Combine(Path.GetTempPath(),
																								  "Unremarkable"+DictionaryConfigurationModel.FileExtension)))
				{
					docView.SetConfigObjectName("Dictionary");
					docView.SetMediator(m_mediator);
					docView.SetPropertyTable(m_propertyTable);
					File.WriteAllText(tempConfigFile.Path, configWithoutTestPub);
					IDictionary<string, string> configsWithPub;
					IDictionary<string, string> configsWithoutPub;
					var configurations = new Dictionary<string, string>();
					configurations["AConfigPubtest"] = tempConfigFile.Path;
					// SUT
					docView.SplitConfigurationsByPublication(configurations,
																		  "TestPub", out configsWithPub, out configsWithoutPub);
					CollectionAssert.DoesNotContain(configsWithPub.Values, tempConfigFile.Path);
					CollectionAssert.Contains(configsWithoutPub.Values, tempConfigFile.Path);
				}
			}
		}

		[Test]
		public void SplitConfigurationsByPublication_MatchedPublicationIsIn()
		{
			using(var helper = new UndoableUnitOfWorkHelper(Cache.ActionHandlerAccessor, "doit", "undoit"))
			{
				var testPubItem = Cache.ServiceLocator.GetInstance<ICmPossibilityFactory>().Create();
				int enId = Cache.WritingSystemFactory.GetWsFromStr("en");
				var testPubName = TsStringUtils.MakeString("TestPub", enId);
				Cache.LangProject.LexDbOA.PublicationTypesOA.PossibilitiesOS.Add(testPubItem);
				testPubItem.Name.set_String(enId, testPubName);
				var configWithTestPub = ConfigurationTemplate.Replace("</Publications>", "<Publication>TestPub</Publication></Publications>");
				using(var docView = new TestXhtmlDocView())
				using(var tempConfigFile = TempFile.WithFilename(Path.Combine(Path.GetTempPath(),
																								  "baz"+DictionaryConfigurationModel.FileExtension)))
				{
					docView.SetConfigObjectName("Dictionary");
					docView.SetMediator(m_mediator);
					docView.SetPropertyTable(m_propertyTable);
					File.WriteAllText(tempConfigFile.Path, configWithTestPub);
					IDictionary<string, string> configsWithPub;
					IDictionary<string, string> configsWithoutPub;
					var configurations = new Dictionary<string, string>();
					configurations["AConfigPubtest"] = tempConfigFile.Path;
					// SUT
					docView.SplitConfigurationsByPublication(configurations,
																		  "TestPub", out configsWithPub, out configsWithoutPub);
					CollectionAssert.Contains(configsWithPub.Values, tempConfigFile.Path);
					CollectionAssert.DoesNotContain(configsWithoutPub.Values, tempConfigFile.Path);
				}
			}
		}

		[Test]
		public void GetValidConfigurationForPublication_ReturnsAlreadySelectedConfigIfValid()
		{
			using(var helper = new UndoableUnitOfWorkHelper(Cache.ActionHandlerAccessor, "doit", "undoit"))
			{
				var testPubItem = Cache.ServiceLocator.GetInstance<ICmPossibilityFactory>().Create();
				int enId = Cache.WritingSystemFactory.GetWsFromStr("en");
				var testPubName = TsStringUtils.MakeString("TestPub", enId);
				Cache.LangProject.LexDbOA.PublicationTypesOA.PossibilitiesOS.Add(testPubItem);
				testPubItem.Name.set_String(enId, testPubName);
				// Change the project path to temp for this test
				Cache.ProjectId.Path = Path.GetTempPath();
				var configWithTestPub = ConfigurationTemplate.Replace("</Publications>", "<Publication>TestPub</Publication></Publications>");
				using(var docView = new TestXhtmlDocView())
				{
					var configPathForTest = Path.Combine(Path.GetTempPath(), "ConfigurationSettings", "Dictionary");
					Directory.CreateDirectory(configPathForTest);
					using(var tempConfigFile = TempFile.WithFilename(Path.Combine(configPathForTest,
																									  "Squirrel"+DictionaryConfigurationModel.FileExtension)))
					{
						docView.SetConfigObjectName("Dictionary");
						docView.SetMediator(m_mediator);
						docView.SetPropertyTable(m_propertyTable);
						m_propertyTable.SetProperty("currentContentControl", "lexiconDictionary", false);
						m_propertyTable.SetProperty("DictionaryPublicationLayout", tempConfigFile.Path, true);
						File.WriteAllText(tempConfigFile.Path, configWithTestPub);
						// SUT
						Assert.That(docView.GetValidConfigurationForPublication("TestPub"), Is.StringContaining(tempConfigFile.Path));
					}
				}
			}
		}

		[Test]
		public void GetValidConfigurationForPublication_AllEntriesReturnsAlreadySelectedConfig()
		{
			using(var helper = new UndoableUnitOfWorkHelper(Cache.ActionHandlerAccessor, "doit", "undoit"))
			{
				var testPubItem = Cache.ServiceLocator.GetInstance<ICmPossibilityFactory>().Create();
				int enId = Cache.WritingSystemFactory.GetWsFromStr("en");
				var testPubName = TsStringUtils.MakeString("TestPub", enId);
				Cache.LangProject.LexDbOA.PublicationTypesOA.PossibilitiesOS.Add(testPubItem);
				testPubItem.Name.set_String(enId, testPubName);

				var configWithTestPub = ConfigurationTemplate.Replace("</Publications>", "<Publication>TestPub</Publication></Publications>");
				var subDir = Path.Combine(Path.GetTempPath(), "Dictionary");
				Directory.CreateDirectory(subDir); // required by DictionaryConfigurationListener.GetCurrentConfiguration()
				using(var docView = new TestXhtmlDocView())
				using(var tempConfigFile = TempFile.WithFilename(
					Path.Combine(subDir, "baz"+DictionaryConfigurationModel.FileExtension)))
				{
					docView.SetConfigObjectName("Dictionary");
					docView.SetMediator(m_mediator);
					docView.SetPropertyTable(m_propertyTable);
					m_propertyTable.SetProperty("DictionaryPublicationLayout", tempConfigFile.Path, true);
					// DictionaryConfigurationListener.GetCurrentConfiguration() needs to know the currentContentControl.
					m_propertyTable.SetProperty("currentContentControl", "lexiconDictionary", true);
					File.WriteAllText(tempConfigFile.Path, configWithTestPub);
					// SUT
					Assert.That(docView.GetValidConfigurationForPublication(xWorksStrings.AllEntriesPublication),
									Is.StringContaining(tempConfigFile.Path));
				}
			}
		}

		[Test]
		public void GetValidConfigurationForPublication_PublicationThatMatchesNoConfigReturnsNull()
		{
			using(var helper = new UndoableUnitOfWorkHelper(Cache.ActionHandlerAccessor, "doit", "undoit"))
			{
				var testPubItem = Cache.ServiceLocator.GetInstance<ICmPossibilityFactory>().Create();
				int enId = Cache.WritingSystemFactory.GetWsFromStr("en");
				var testPubName = TsStringUtils.MakeString("NotTheTestPub", enId);
				Cache.LangProject.LexDbOA.PublicationTypesOA.PossibilitiesOS.Add(testPubItem);
				testPubItem.Name.set_String(enId, testPubName);
				var configSansTestPub = ConfigurationTemplate.Replace("</Publications>",
																						 "<Publication>NotTheTestPub</Publication></Publications>");
				var overrideFiles = new List<TempFile>();
				using(var docView = new TestXhtmlDocView())
				{
					docView.SetConfigObjectName("Dictionary");
					docView.SetMediator(m_mediator);
					docView.SetPropertyTable(m_propertyTable);
					var projConfigs = Path.Combine(LcmFileHelper.GetConfigSettingsDir(Cache.ProjectId.ProjectFolder),
															 "Dictionary");
					Directory.CreateDirectory(projConfigs);
					// override every shipped config with a config that does not have the TestPub publication
					var shippedFileList = Directory.EnumerateFiles(Path.Combine(FwDirectoryFinder.DefaultConfigurations, "Dictionary"),
						"*" + DictionaryConfigurationModel.FileExtension);
					var overrideCount = 0;
					foreach(var shippedFile in shippedFileList)
					{
						++overrideCount;
						var tempFileName = Path.Combine(projConfigs, overrideCount + DictionaryConfigurationModel.FileExtension);
						var tempConfigFile = TempFile.WithFilename(tempFileName);
						overrideFiles.Add(tempConfigFile);
						using(var stream = new FileStream(shippedFile, FileMode.Open))
						{
							var doc = new XmlDocument();
							doc.Load(stream);
							var node = doc.SelectSingleNode("DictionaryConfiguration");
							var shippedName = node.Attributes["name"].Value;
							File.WriteAllText(tempConfigFile.Path,
													configSansTestPub.Replace("name='AConfigPubtest'", "name='"+shippedName+"'"));
						}
					}
					// SUT
					var result = docView.GetValidConfigurationForPublication("TestPub");
					// Delete all our temp files before asserting so they are sure to go away
					foreach(var tempFile in overrideFiles)
					{
						tempFile.Dispose();
					}
					Assert.IsNull(result, "When no configurations have the publication null should be returned.");
				}
			}
		}

		[Test]
		public void GetValidConfigurationForPublication_ConfigurationContainingPubIsPicked()
		{
			using(var helper = new UndoableUnitOfWorkHelper(Cache.ActionHandlerAccessor, "doit", "undoit"))
			{
				var testPubItem = Cache.ServiceLocator.GetInstance<ICmPossibilityFactory>().Create();
				int enId = Cache.WritingSystemFactory.GetWsFromStr("en");
				var testPubName = TsStringUtils.MakeString("TestPub", enId);
				Cache.LangProject.LexDbOA.PublicationTypesOA.PossibilitiesOS.Add(testPubItem);
				testPubItem.Name.set_String(enId, testPubName);
				var notTestPubItem = Cache.ServiceLocator.GetInstance<ICmPossibilityFactory>().Create();
				var notTestPubName = TsStringUtils.MakeString("NotTestPub", enId);
				Cache.LangProject.LexDbOA.PublicationTypesOA.PossibilitiesOS.Add(notTestPubItem);
				notTestPubItem.Name.set_String(enId, notTestPubName);
				var nonMatchingConfig = ConfigurationTemplate.Replace("</Publications>", "<Publication>NotTestPub</Publication></Publications>");
				//Change the name for the matching config so that our two user configs don't conflict with each other
				var matchingConfig = ConfigurationTemplate.Replace("</Publications>",
																					"<Publication>TestPub</Publication></Publications>").Replace("AConfigPub", "AAConfigPub");
				var dictionaryConfigPath = Path.Combine(LcmFileHelper.GetConfigSettingsDir(Cache.ProjectId.ProjectFolder), "Dictionary");
				using(var docView = new TestXhtmlDocView())
				using(var nonMatchedConfigFile = TempFile.WithFilename(Path.Combine(dictionaryConfigPath,
																								  "NoMatch"+DictionaryConfigurationModel.FileExtension)))
				using(var matchedConfigFile = TempFile.WithFilename(Path.Combine(dictionaryConfigPath,
																								  "Match"+DictionaryConfigurationModel.FileExtension)))
				{
					File.WriteAllText(nonMatchedConfigFile.Path, nonMatchingConfig);
					File.WriteAllText(matchedConfigFile.Path, matchingConfig);
					docView.SetConfigObjectName("Dictionary");
					m_propertyTable.SetProperty("currentContentControl", "lexiconDictionary", false);
					m_propertyTable.SetProperty("DictionaryPublicationLayout", nonMatchedConfigFile.Path, true);
					docView.SetMediator(m_mediator);
					docView.SetPropertyTable(m_propertyTable);
					// SUT
					var validConfig = docView.GetValidConfigurationForPublication("TestPub");
					Assert.That(validConfig, Is.Not.StringContaining(nonMatchedConfigFile.Path));
					Assert.That(validConfig, Is.StringContaining(matchedConfigFile.Path));
				}
			}
		}

		private class TestXhtmlDocView : XhtmlDocView
		{
			internal void SetConfigObjectName(string name)
			{
				m_configObjectName = name;
			}

			internal void SetMediator(Mediator mediator)
			{
				m_mediator = mediator;
			}

			public void SetPropertyTable(PropertyTable propertyTable)
			{
				m_propertyTable = propertyTable;
			}
		}

		protected override void Init()
		{
			m_application = new MockFwXApp(new MockFwManager { Cache = Cache }, null, null);
			m_configFilePath = Path.Combine(FwDirectoryFinder.CodeDirectory, m_application.DefaultConfigurationPathname);
			m_window = new MockFwXWindow(m_application, m_configFilePath);
			((MockFwXWindow)m_window).Init(Cache); // initializes Mediator values
			m_propertyTable = m_window.PropTable;
		}

		#region IDisposable Section (aka keep Gendarme happy)
		~XhtmlDocViewTests()
		{
			Dispose(false);
		}
		public void Dispose()
		{
			Dispose(true);
			GC.SuppressFinalize(this);
		}

		protected virtual void Dispose(bool disposing)
		{
			System.Diagnostics.Debug.WriteLineIf(!disposing, "****** Missing Dispose() call for " + GetType().Name + ". ****** ");
			if(m_propertyTable != null)
				m_propertyTable.Dispose();
			if (m_window != null)
				m_window.Dispose();

			if (m_application != null)
				m_application.Dispose();

			if (m_mediator != null)
				m_mediator.Dispose();

			m_application = null;
			m_window = null;
			m_mediator = null;
		}
		#endregion
	}
}
