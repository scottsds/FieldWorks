// Copyright (c) 2015-2017 SIL International
// This software is licensed under the LGPL, version 2.1 or later
// (http://www.gnu.org/licenses/lgpl-2.1.html)

using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Windows.Forms;
using System.Xml;
using SIL.FieldWorks.Common.FwUtils;
using SIL.FieldWorks.Common.Widgets;
using SIL.LCModel;
using SIL.LCModel.DomainServices;
using SIL.FieldWorks.XWorks;
using SIL.Utils;
using XCore;

namespace SIL.FieldWorks.IText
{
	public partial class StatisticsView : UserControl, IxCoreContentControl
	{
		private bool _shouldNotCall;

		private string _areaName;
		private Mediator _mediator;
		private PropertyTable _propertyTable;
		private LcmCache _cache;
		private InterlinearTextsRecordClerk m_clerk;

		public StatisticsView()
		{
			InitializeComponent();

			ContextMenu cm = new ContextMenu();

			var mi = new MenuItem("Copy");
			mi.Click += new EventHandler(mi_Copy);
			cm.MenuItems.Add(mi);

			statisticsBox.ContextMenu = cm;
		}

		public string AccName
		{
			get { return ITextStrings.ksTextAreaStatisticsViewName; }
		}

		#region Implementation of IxCoreCtrlTabProvider

		/// <summary>
		/// Gather up suitable targets to Cntrl-(Shift-)Tab into.
		/// </summary>
		/// <param name="targetCandidates">List of places to move to.</param>
		/// <returns>A suitable target for moving to in Ctrl(+Shift)+Tab.
		/// This returned value should also have been added to the main list.</returns>
		public Control PopulateCtrlTabTargetCandidateList(List<Control> targetCandidates)
		{
			targetCandidates.Add(this);
			return this;
		}

		#endregion

		#region Implementation of IxCoreColleague

		public void Init(Mediator mediator, PropertyTable propertyTable, XmlNode configurationParameters)
		{
			CheckDisposed();
			_mediator = mediator; //allows the Cache property to function
			_propertyTable = propertyTable;
			_cache = _propertyTable.GetValue<LcmCache>("cache");

			string name = XmlUtils.GetAttributeValue(configurationParameters, "clerk");
			var clerk = RecordClerk.FindClerk(_propertyTable, name);
			m_clerk = (clerk == null || clerk is TemporaryRecordClerk) ?
				(InterlinearTextsRecordClerk)RecordClerkFactory.CreateClerk(mediator, _propertyTable, configurationParameters, true) :
				(InterlinearTextsRecordClerk)clerk;
			// There's no record bar for it to control, but it should control the staus bar (e.g., it should update if we change
			// the set of selected texts).
			m_clerk.ActivateUI(true);
			_areaName = XmlUtils.GetOptionalAttributeValue(configurationParameters, "area", "unknown");
			RebuildStatisticsTable();
			//add ourselves so that we can receive messages (related to the text selection currently misnamed AddTexts)
			mediator.AddColleague(this);
			//add our current state to the history system
			string toolName = _propertyTable.GetStringProperty("currentContentControl", "");
			mediator.SendMessage("AddContextToHistory", new FwLinkArgs(toolName, Guid.Empty), false);
		}

		private void RebuildStatisticsTable()
		{
			statisticsBox.Clear();
			// TODO-Linux: SelectionTabs isn't implemented on Mono
			statisticsBox.SelectionTabs = new int[] { 10, 300};
			//retrieve the default UI font.
			var font = FontHeightAdjuster.GetFontForStyle(StyleServices.NormalStyleName,
														  FontHeightAdjuster.StyleSheetFromPropertyTable(_propertyTable),
														  Cache.DefaultUserWs, Cache.WritingSystemFactory);
			//increase the size of the default UI font and make it bold for the header.
			Font headerFont = new Font(font.FontFamily, font.SizeInPoints + 1.0f, FontStyle.Bold, font.Unit, font.GdiCharSet);

			//refresh the statisticsDescription (in case of font changes)
			statisticsBox.Text = ITextStrings.ksStatisticsView_HeaderText;

			int row = 0;
			var textList = InterestingTextsDecorator.GetInterestingTextList(_mediator, _propertyTable, Cache.ServiceLocator);
			int numberOfSegments = 0;
			int wordCount = 0;
			int uniqueWords = 0;

			Dictionary<int, int> languageCount = new Dictionary<int, int>();
			Dictionary<int, HashSet<string>> languageTypeCount = new Dictionary<int, HashSet<string>>();
			//for each interesting text
			foreach (var text in textList.InterestingTexts)
			{
				//if a text is deleted in Interlinear there could be a text in this list which has invalid data.
				if (text.Hvo < 0)
					continue;
				//for every paragraph in the interesting text
				for (int index = 0; index < text.ParagraphsOS.Count; ++index)
				{
					//count the segments in this paragraph
					numberOfSegments += text[index].SegmentsOS.Count;
					//count all the things analyzed as words
					var words = new List<IAnalysis>(text[index].Analyses);
					var wordForms = new List<IWfiWordform>(words.Where(x => x.Wordform != null && x.Wordform.ShortName != "???").Select(y => y.Wordform));
					foreach (var wordForm in wordForms)
						{
							var valdWSs = wordForm.Form.AvailableWritingSystemIds;
							foreach (var ws in valdWSs)
							{
								// increase the count of words(tokens) for this language
								int count = 0;
								if (languageCount.TryGetValue(ws, out count))
								{
									languageCount[ws] = count + 1;
								}
								else
								{
									languageCount.Add(ws, 1);
								}
								//increase the count of unique words(types) for this language
								HashSet<string> pair;
								if (languageTypeCount.TryGetValue(ws, out pair))
								{
									//add the string for this writing system in all lower case to the set, unique count is case insensitive
								pair.Add(wordForm.Form.get_String(ws).Text.ToLower());
								}
								else
								{
									//add the string for this writing system in all lower case to the set, unique count is case insensitive
								languageTypeCount.Add(ws, new HashSet<string> {wordForm.Form.get_String(ws).Text.ToLower()});
							}
						}
					}
					words.RemoveAll(item => !item.HasWordform);
					wordCount += words.Count;
				}
			}
			// insert total word type count
			statisticsBox.Text += Environment.NewLine + Environment.NewLine + Environment.NewLine + "\t" + ITextStrings.ksStatisticsViewTotalWordTypesText + "\t"; // Todo: find the right System.?.NewLine constant

			++row;
			//add one row for the unique words in each language.
			foreach (KeyValuePair<int, HashSet<string>> keyValuePair in languageTypeCount)
			{
				var ws = Cache.WritingSystemFactory.get_EngineOrNull(keyValuePair.Key);

				string labText = (ws != null ? ws.ToString() : "#unknown#") + @":";
				statisticsBox.Text += Environment.NewLine + Environment.NewLine + "\t" + labText + "\t"; // Todo: find the right System.?.NewLine constant
				statisticsBox.Text += "" + keyValuePair.Value.Count;
				++row;
				uniqueWords += keyValuePair.Value.Count; //increase the total of unique words
			}

			// next insert the word count.
			statisticsBox.Text += Environment.NewLine + Environment.NewLine + Environment.NewLine + "\t" + ITextStrings.ksStatisticsViewTotalWordTokensText + "\t"; // Todo: find the right System.?.NewLine constant
			statisticsBox.Text += wordCount;
			++row;
			//add one row for the token count for each language.
			foreach (KeyValuePair<int, int> keyValuePair in languageCount)
			{
				var ws = Cache.WritingSystemFactory.get_EngineOrNull(keyValuePair.Key);

				string labText = (ws != null ? ws.ToString() : "#unknown#") + @":";
				statisticsBox.Text += Environment.NewLine + Environment.NewLine + "\t" + labText + "\t"; // Todo: find the right System.?.NewLine constant
				statisticsBox.Text += "" + keyValuePair.Value;
				++row;
			}
			statisticsBox.Text += Environment.NewLine + Environment.NewLine + Environment.NewLine + "\t" + ITextStrings.ksStatisticsViewTotalSentencesText + "\t"; // Todo: find the right System.?.NewLine constant

			// next insert the sentence count.
			statisticsBox.Text += numberOfSegments;

			// insert the total word type count into the richTextBox (it wasn't available earlier)
			statisticsBox.SelectionStart = statisticsBox.Find(ITextStrings.ksStatisticsViewTotalWordTypesText) +
										   ITextStrings.ksStatisticsViewTotalWordTypesText.Length;
			statisticsBox.SelectionLength = 1;
			statisticsBox.SelectedText = "\t" + uniqueWords;

			// Set the font for the header. Do this after we add the other stuff to make sure
			// it doesn't apply to extra text added adjacent to it.
			statisticsBox.Select(0, ITextStrings.ksStatisticsView_HeaderText.Length);
			statisticsBox.SelectionFont = headerFont;
			statisticsBox.Select(0, 0);
		}

		public IxCoreColleague[] GetMessageTargets()
		{
			CheckDisposed();

			return new IxCoreColleague[] { this };
		}

		public bool ShouldNotCall
		{
			get { return _shouldNotCall; }
		}

		public int Priority
		{
			get
			{
				return (int)ColleaguePriority.Low;
			}
		}

		public bool OnDisplayAddTexts(object commandObject, ref UIItemDisplayProperties display)
		{
			CheckDisposed();
			display.Enabled = m_clerk != null;
			display.Visible = display.Enabled;
			return true;
		}

		public bool OnAddTexts(object args)
		{
			bool result = m_clerk.OnAddTexts(args);
			if(result)
			{
				RebuildStatisticsTable();
			}
			return result;
		}

		#endregion

		#region Implementation of IxCoreContentControl

		public bool PrepareToGoAway()
		{
			CheckDisposed();

			return true;
		}

		public string AreaName
		{
			get { return _areaName; }
		}

		#endregion

		/// <summary>
		/// This method throws an ObjectDisposedException if IsDisposed returns
		/// true.  This is the case where a method or property in an object is being
		/// used but the object itself is no longer valid.
		///
		/// This method should be added to all public properties and methods of this
		/// object and all other objects derived from it (extensive).
		/// </summary>
		public void CheckDisposed()
		{
			if (IsDisposed)
				throw new ObjectDisposedException("StatisticsView has been disposed.");
		}

		/// <summary>
		/// FDO cache.
		/// </summary>
		protected LcmCache Cache
		{
			get
			{
				return _cache;
			}
		}
		//Code to add right click


		void mi_Copy(object sender, EventArgs e)
		{
			statisticsBox.Copy();
		}
	}
}
