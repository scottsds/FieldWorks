// Copyright (c) 2015-2017 SIL International
// This software is licensed under the LGPL, version 2.1 or later
// (http://www.gnu.org/licenses/lgpl-2.1.html)

using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Windows.Forms;
using SIL.LCModel.Core.WritingSystems;
using SIL.FieldWorks.Common.Controls;
using SIL.FieldWorks.Common.FwUtils;
using SIL.WritingSystems;
using System.Text.RegularExpressions;

namespace SIL.FieldWorks.FwCoreDlgControls
{
	/// <summary>
	/// Summary description for RegionVariantControl.
	/// </summary>
	public class RegionVariantControl : UserControl
	{
		private Label m_variantNameLabel;
		// Note: this currently has a max length set to 30. This is to ensure that any
		// combination of language (max 11), country (max 3) and variant with two
		// underscores will be under the 49-char limit for the overall length of a
		// locale ID. We even gave ourselves a margin of a couple of characters,
		// since 30 is a lot and it may be useful to have a couple left in case ICU
		// reduces the limit or we want to add a version number or something like that.
		private Label m_scriptNameLabel;
		private FwOverrideComboBox m_scriptName;
		private Label m_scriptAbbrevLabel;
		private TextBox m_scriptAbbrev;
		//Control member to hold the value of the scriptName string
		private string m_scriptNameString;

		private Label m_regionNameLabel;
		private FwOverrideComboBox m_regionName;
		private Label m_regionAbbrevLabel;
		private TextBox m_regionAbbrev;
		private string m_regionNameString;

		private Label m_variantAbbrevLabel;
		private FwOverrideComboBox m_variantName;
		private TextBox m_variantAbbrev;
		private string m_variantNameString;

		private HelpProvider m_helpProvider;

		private CoreWritingSystemDefinition m_ws;
		private ScriptSubtag m_origScriptSubtag;
		private RegionSubtag m_origRegionSubtag;
		private readonly List<VariantSubtag> m_origVariantSubtags = new List<VariantSubtag>();

		/// <summary>
		/// This is set when changing multiple subtags at once to prevent the side effects of
		/// changing one from interfering with changing the other. It should always be set
		/// by calling EnableScriptTagSideEffects and should only be disabled temporarily.
		/// </summary>
		private bool m_enableLangTagSideEffects = true;



		/// <summary>
		/// We need an event to let the parents control know when comboBox values have changed
		/// </summary>
		public event EventHandler ScriptRegionVariantChanged;

		/// <summary>
		/// Constructor.
		/// </summary>
		public RegionVariantControl()
		{
			// This call is required by the Windows.Forms Form Designer.
			InitializeComponent();
		}

		/// <summary/>
		protected override void Dispose(bool disposing)
		{
			System.Diagnostics.Debug.WriteLineIf(!disposing, "****** Missing Dispose() call for " + GetType() + ". ******");
			base.Dispose(disposing);
		}

		#region Component Designer generated code
		/// <summary>
		/// Required method for Designer support - do not modify
		/// the contents of this method with the code editor.
		/// </summary>
		private void InitializeComponent()
		{
			System.ComponentModel.ComponentResourceManager resources = new System.ComponentModel.ComponentResourceManager(typeof(RegionVariantControl));
			this.m_regionAbbrev = new System.Windows.Forms.TextBox();
			this.m_variantNameLabel = new System.Windows.Forms.Label();
			this.m_variantAbbrev = new System.Windows.Forms.TextBox();
			this.m_regionAbbrevLabel = new System.Windows.Forms.Label();
			this.m_variantAbbrevLabel = new System.Windows.Forms.Label();
			this.m_regionNameLabel = new System.Windows.Forms.Label();
			this.m_helpProvider = new System.Windows.Forms.HelpProvider();
			this.m_scriptNameLabel = new System.Windows.Forms.Label();
			this.m_scriptAbbrevLabel = new System.Windows.Forms.Label();
			this.m_scriptAbbrev = new System.Windows.Forms.TextBox();
			this.m_scriptName = new SIL.FieldWorks.Common.Controls.FwOverrideComboBox();
			this.m_variantName = new SIL.FieldWorks.Common.Controls.FwOverrideComboBox();
			this.m_regionName = new SIL.FieldWorks.Common.Controls.FwOverrideComboBox();
			this.SuspendLayout();
			//
			// m_regionAbbrev
			//
			resources.ApplyResources(this.m_regionAbbrev, "m_regionAbbrev");
			this.m_regionAbbrev.Name = "m_regionAbbrev";
			this.m_helpProvider.SetShowHelp(this.m_regionAbbrev, ((bool)(resources.GetObject("m_regionAbbrev.ShowHelp"))));
			this.m_regionAbbrev.TextChanged += new System.EventHandler(this.m_regionCode_TextChanged);
			this.m_regionAbbrev.KeyPress += new System.Windows.Forms.KeyPressEventHandler(this.m_regionCode_KeyPress);
			//
			// m_variantNameLabel
			//
			resources.ApplyResources(this.m_variantNameLabel, "m_variantNameLabel");
			this.m_variantNameLabel.BackColor = System.Drawing.Color.Transparent;
			this.m_variantNameLabel.Name = "m_variantNameLabel";
			this.m_helpProvider.SetShowHelp(this.m_variantNameLabel, ((bool)(resources.GetObject("m_variantNameLabel.ShowHelp"))));
			//
			// m_variantAbbrev
			//
			resources.ApplyResources(this.m_variantAbbrev, "m_variantAbbrev");
			this.m_variantAbbrev.Name = "m_variantAbbrev";
			this.m_helpProvider.SetShowHelp(this.m_variantAbbrev, ((bool)(resources.GetObject("m_variantAbbrev.ShowHelp"))));
			this.m_variantAbbrev.TextChanged += new System.EventHandler(this.m_variantCode_TextChanged);
			this.m_variantAbbrev.KeyPress += new System.Windows.Forms.KeyPressEventHandler(this.m_variantCode_KeyPress);
			//
			// m_regionAbbrevLabel
			//
			resources.ApplyResources(this.m_regionAbbrevLabel, "m_regionAbbrevLabel");
			this.m_regionAbbrevLabel.BackColor = System.Drawing.Color.Transparent;
			this.m_regionAbbrevLabel.Name = "m_regionAbbrevLabel";
			this.m_helpProvider.SetShowHelp(this.m_regionAbbrevLabel, ((bool)(resources.GetObject("m_regionAbbrevLabel.ShowHelp"))));
			//
			// m_variantAbbrevLabel
			//
			resources.ApplyResources(this.m_variantAbbrevLabel, "m_variantAbbrevLabel");
			this.m_variantAbbrevLabel.BackColor = System.Drawing.Color.Transparent;
			this.m_variantAbbrevLabel.Name = "m_variantAbbrevLabel";
			this.m_helpProvider.SetShowHelp(this.m_variantAbbrevLabel, ((bool)(resources.GetObject("m_variantAbbrevLabel.ShowHelp"))));
			//
			// m_regionNameLabel
			//
			resources.ApplyResources(this.m_regionNameLabel, "m_regionNameLabel");
			this.m_regionNameLabel.BackColor = System.Drawing.Color.Transparent;
			this.m_regionNameLabel.Name = "m_regionNameLabel";
			this.m_helpProvider.SetShowHelp(this.m_regionNameLabel, ((bool)(resources.GetObject("m_regionNameLabel.ShowHelp"))));
			//
			// m_scriptNameLabel
			//
			resources.ApplyResources(this.m_scriptNameLabel, "m_scriptNameLabel");
			this.m_scriptNameLabel.Name = "m_scriptNameLabel";
			this.m_helpProvider.SetShowHelp(this.m_scriptNameLabel, ((bool)(resources.GetObject("m_scriptNameLabel.ShowHelp"))));
			//
			// m_scriptAbbrevLabel
			//
			resources.ApplyResources(this.m_scriptAbbrevLabel, "m_scriptAbbrevLabel");
			this.m_scriptAbbrevLabel.Name = "m_scriptAbbrevLabel";
			this.m_helpProvider.SetShowHelp(this.m_scriptAbbrevLabel, ((bool)(resources.GetObject("m_scriptAbbrevLabel.ShowHelp"))));
			//
			// m_scriptAbbrev
			//
			resources.ApplyResources(this.m_scriptAbbrev, "m_scriptAbbrev");
			this.m_scriptAbbrev.Name = "m_scriptAbbrev";
			this.m_helpProvider.SetShowHelp(this.m_scriptAbbrev, ((bool)(resources.GetObject("m_scriptAbbrev.ShowHelp"))));
			this.m_scriptAbbrev.TextChanged += new System.EventHandler(this.m_scriptCode_TextChanged);
			this.m_scriptAbbrev.KeyPress += new System.Windows.Forms.KeyPressEventHandler(this.m_scriptCode_KeyPress);
			//
			// m_scriptName
			//
			this.m_scriptName.AllowSpaceInEditBox = true;
			//			this.m_scriptName.DataSource = this.regionVariantControlBindingSource1;
			resources.ApplyResources(this.m_scriptName, "m_scriptName");
			this.m_scriptName.Name = "m_scriptName";
			this.m_helpProvider.SetShowHelp(this.m_scriptName, ((bool)(resources.GetObject("m_scriptName.ShowHelp"))));
			this.m_scriptName.Sorted = true;
			this.m_scriptName.TextChanged += new System.EventHandler(this.m_scriptName_TextChanged);

			//
			// m_variantName
			//
			this.m_variantName.AllowSpaceInEditBox = true;
			this.m_helpProvider.SetHelpString(this.m_variantName, resources.GetString("m_variantName.HelpString"));
			resources.ApplyResources(this.m_variantName, "m_variantName");
			this.m_variantName.Name = "m_variantName";
			this.m_helpProvider.SetShowHelp(this.m_variantName, ((bool)(resources.GetObject("m_variantName.ShowHelp"))));
			this.m_variantName.Sorted = true;
			this.m_variantName.SelectedIndexChanged += m_variantName_TextChanged;
			this.m_variantName.TextChanged += new System.EventHandler(this.m_variantName_TextChanged);
			//
			// m_regionName
			//
			this.m_regionName.AllowSpaceInEditBox = true;
			this.m_helpProvider.SetHelpString(this.m_regionName, resources.GetString("m_regionName.HelpString"));
			resources.ApplyResources(this.m_regionName, "m_regionName");
			this.m_regionName.Name = "m_regionName";
			this.m_helpProvider.SetShowHelp(this.m_regionName, ((bool)(resources.GetObject("m_regionName.ShowHelp"))));
			this.m_regionName.Sorted = true;
			this.m_regionName.TextChanged += new System.EventHandler(this.m_regionName_TextChanged);
			//
			//
			// RegionVariantControl
			//
			this.BackColor = System.Drawing.SystemColors.Control;
			this.Controls.Add(this.m_scriptName);
			this.Controls.Add(this.m_scriptAbbrev);
			this.Controls.Add(this.m_scriptAbbrevLabel);
			this.Controls.Add(this.m_scriptNameLabel);
			this.Controls.Add(this.m_variantName);
			this.Controls.Add(this.m_regionName);
			this.Controls.Add(this.m_regionAbbrev);
			this.Controls.Add(this.m_variantNameLabel);
			this.Controls.Add(this.m_variantAbbrev);
			this.Controls.Add(this.m_regionAbbrevLabel);
			this.Controls.Add(this.m_variantAbbrevLabel);
			this.Controls.Add(this.m_regionNameLabel);
			this.Name = "RegionVariantControl";
			this.m_helpProvider.SetShowHelp(this, ((bool)(resources.GetObject("$this.ShowHelp"))));
			resources.ApplyResources(this, "$this");
			this.ResumeLayout(false);
			this.PerformLayout();

		}
		#endregion


		/// <summary>
		/// Check to see if the object has been disposed.
		/// All public Properties and Methods should call this
		/// before doing anything else.
		/// </summary>
		public void CheckDisposed()
		{
			if (IsDisposed)
				throw new ObjectDisposedException(String.Format("'{0}' in use after being disposed.", GetType().Name));
		}

		/// <summary>
		/// The larger component using this control must supply a writing system
		/// which this control will help to edit.
		/// </summary>
		public CoreWritingSystemDefinition WritingSystem
		{
			get
			{
				CheckDisposed();
				return m_ws;
			}
			set
			{
				CheckDisposed();

				m_ws = value;
				LoadControlsFromWritingSystem();
			}
		}

		/// <summary>
		/// Indicates whether the Region or Variant or Script name has changed in the control since initialization.
		/// </summary>
		public bool RegionOrVariantOrScriptChanged
		{
			get
			{
				if (m_ws.Region != m_origRegionSubtag
					|| !m_ws.Variants.SequenceEqual(m_origVariantSubtags)
					|| m_ws.Script != m_origScriptSubtag)
				{
					return true;
				}
				return false;
			}
		}

		/// <summary>
		/// Gets or sets the script subtag.
		/// </summary>
		/// <value>The script subtag.</value>
		public ScriptSubtag ScriptSubtag
		{
			get
			{
				if (DesignMode)
					return null;

				CheckDisposed();

				ScriptSubtag subtag = null;
				if (m_scriptAbbrev.Enabled)
				{
					string code = m_scriptAbbrev.Text.Trim();
					if (!string.IsNullOrEmpty(code))
						subtag = new ScriptSubtag(code, m_scriptName.Text.Trim());
				}
				else if (IsVoiceWritingSystem)
				{
					subtag = WellKnownSubtags.AudioScript;
				}
				else
				{
					subtag = (ScriptSubtag) m_scriptName.SelectedItem;
				}
				return subtag;
			}

			set
			{
				CheckDisposed();

				if (value == null)
				{
					m_scriptAbbrev.Text = "";
					m_scriptAbbrev.Enabled = false;
					ScriptName = "";
				}
				else
				{

					bool wasEnabled = m_scriptName.Enabled; //be a good neighbor
					m_scriptName.Enabled = true; // somehow necessary to allow it to be changed??
					ScriptName = value.Name;
					if (m_scriptName.SelectedItem == null)
					{
						m_scriptName.Items.Add(value);
						m_scriptName.SelectedIndex = m_scriptName.Items.IndexOf(value);
					}
					m_scriptName.Enabled = wasEnabled; //be a good neighbor
					m_scriptAbbrev.Enabled = value.IsPrivateUse;
					m_scriptAbbrev.Text = value.Code;
				}
			}
		}

		/// <summary>
		/// Gets or sets the region subtag.
		/// </summary>
		/// <value>The region subtag.</value>
		public RegionSubtag RegionSubtag
		{
			get
			{
				CheckDisposed();

				RegionSubtag subtag = null;
				if (m_regionAbbrev.Enabled)
				{
					string code = m_regionAbbrev.Text.Trim();
					if (!string.IsNullOrEmpty(code))
						subtag = new RegionSubtag(code, m_regionName.Text.Trim());
				}
				else
				{
					subtag = (RegionSubtag) m_regionName.SelectedItem;
				}
				return subtag;
			}

			set
			{
				CheckDisposed();

				if (value == null)
				{
					m_regionAbbrev.Text = "";
					m_regionAbbrev.Enabled = false;
					RegionName = "";
				}
				else
				{
					if (value.Name != null) //set the RegionName to the value in the given subtag
					{
						RegionName = value.Name;
					}
					else //if the subtag has no name try to be nice and look one up, this avoids getting our controls in a bad state
					{
						RegionSubtag region;
						RegionName = StandardSubtags.RegisteredRegions.TryGet(value.Code, out region) ? region.Name : value.Code;
					}
					//m_regionName.SelectedItem = value;
					if (m_regionName.SelectedItem == null)
					{
						m_regionName.Items.Add(value);
						m_regionName.SelectedIndex = m_regionName.Items.IndexOf(value);

					}
					m_regionAbbrev.Enabled = value.IsPrivateUse;
					m_regionAbbrev.Text = value.Code;
				}
			}
		}

		/// <summary>
		/// Gets or sets the variant subtag.
		/// </summary>
		/// <value>The variant subtag.</value>
		public IEnumerable<VariantSubtag> VariantSubtags
		{
			get
			{
				CheckDisposed();

				if (m_variantAbbrev.Enabled)
				{
					string code = m_variantAbbrev.Text.Trim();
					IEnumerable<VariantSubtag> variantSubtags;
					code = Regex.Replace(code, "^x-", "");
					if (IetfLanguageTag.TryGetVariantSubtags("x-" + code, out variantSubtags, m_variantNameString))
					{
						foreach (VariantSubtag variantSubtag in variantSubtags)
							yield return variantSubtag;
					}
				}
				else
				{
					if (m_variantName.SelectedItem != null)
					{
						var variantSubtag = (VariantSubtag) m_variantName.SelectedItem;
						if (variantSubtag == WellKnownSubtags.IpaPhonemicPrivateUse || variantSubtag == WellKnownSubtags.IpaPhoneticPrivateUse)
							yield return WellKnownSubtags.IpaVariant;
						yield return variantSubtag;
					}
				}
			}

			set
			{
				CheckDisposed();

				VariantSubtag[] variantSubtags = value.ToArray();
				if (variantSubtags.Length == 0)
				{
					VariantName = "";
					m_variantAbbrev.Enabled = false;
					m_variantAbbrev.Text = "";
				}
				else
				{
					VariantSubtag variantSubtag = null;
					if (variantSubtags[0] == WellKnownSubtags.IpaVariant)
					{
						if (variantSubtags.Length == 1)
							variantSubtag = variantSubtags[0];
						if (variantSubtags.Length == 2)
						{
							if (variantSubtags[1] == WellKnownSubtags.IpaPhonemicPrivateUse || variantSubtags[1] == WellKnownSubtags.IpaPhoneticPrivateUse)
								variantSubtag = variantSubtags[1];
						}
					}
					else if (variantSubtags.Length == 1)
					{
						variantSubtag = variantSubtags[0];
					}

					if (variantSubtag != null)
					{
						m_variantName.SelectedIndex = m_variantName.Items.IndexOf(variantSubtag);

						if (m_variantName.SelectedItem == null)
						{
							m_variantName.Items.Add(variantSubtag);
							VariantName = variantSubtag.Name;
						}

						m_variantAbbrev.Enabled = variantSubtag.IsPrivateUse && !m_ws.IsVoice;
						m_variantAbbrev.Text = (variantSubtag.IsPrivateUse ? "x-" : "") + variantSubtag.Code;
					}
					else
					{
						VariantName = "";
						m_variantAbbrev.Enabled = true;
						m_variantAbbrev.Text = IetfLanguageTag.GetVariantCodes(variantSubtags);
					}
				}
			}
		}

		/// <summary>
		/// Gets or sets the name of the script.
		/// </summary>
		/// <value>The name of the script.</value>
		public string ScriptName
		{
			get
			{
				CheckDisposed();
				return m_scriptNameString;
			}

			set
			{
				CheckDisposed();
				m_scriptNameString = value;
				m_scriptName.SelectedIndex = m_scriptName.FindStringExact(value);
			}
		}

		/// <summary>
		/// Gets or sets the name of the region.
		/// </summary>
		/// <value>The name of the region.</value>
		public string RegionName
		{
			get
			{
				CheckDisposed();
				return m_regionNameString;
			}

			set
			{
				CheckDisposed();
				m_regionNameString = value;
				m_regionName.SelectedIndex = m_regionName.FindStringExact(value);
			}
		}

		/// <summary>
		/// Gets or sets the name of the variant.
		/// </summary>
		/// <value>The name of the variant.</value>
		public string VariantName
		{
			get
			{
				CheckDisposed();
				return m_variantNameString;
			}

			set
			{
				CheckDisposed();

				m_variantNameString = value;
				m_variantName.SelectedIndex = m_variantName.FindStringExact(value);
			}
		}

		/// <summary>
		/// Activates a child control.
		/// </summary>
		/// <param name="directed">true to specify the direction of the control to select; otherwise, false.</param>
		/// <param name="forward">true to move forward in the tab order; false to move backward in the tab order.</param>
		protected override void Select(bool directed, bool forward)
		{
			base.Select(directed, forward);
			if (!directed)
				SelectNextControl(null, forward, true, true, false);
		}

		/// <summary>
		/// Load the controls from the writing system, if it is not null. If it is null, clear all controls.
		/// If the combo boxes are not populated, do nothing...the method will get called again
		/// when the form loads.
		/// </summary>
		private void LoadControlsFromWritingSystem()
		{
			m_enableLangTagSideEffects = false;
			if (m_ws == null)
				return; // Probably in design mode; can't populate.
			m_origVariantSubtags.Clear();
			m_origVariantSubtags.AddRange(m_ws.Variants);
			m_origRegionSubtag = m_ws.Region;
			m_origScriptSubtag = m_ws.Script;
			m_scriptName.ClearItems();
			m_scriptName.Items.AddRange(StandardSubtags.RegisteredScripts.Cast<object>().ToArray());
			ScriptSubtag = m_origScriptSubtag;

			m_regionName.ClearItems();
			m_regionName.Items.AddRange(StandardSubtags.RegisteredRegions.Cast<object>().ToArray());
			RegionSubtag = m_origRegionSubtag;

			PopulateVariantCombo(false);
			VariantSubtags = m_origVariantSubtags;
			m_enableLangTagSideEffects = true;
		}

		private void PopulateVariantCombo(bool fPreserve)
		{
			m_variantName.TextChanged -= m_variantName_TextChanged; // don't modify the WS while fixing up the combo.
			m_variantName.BeginUpdate();
			IEnumerable<VariantSubtag> orig = VariantSubtags;
			m_variantName.ClearItems();
			m_variantName.Items.AddRange(StandardSubtags.RegisteredVariants.Concat(StandardSubtags.CommonPrivateUseVariants)
				.Where(v => v.IsVariantOf(m_ws.LanguageTag)).Cast<object>().ToArray());
			if (orig != null && fPreserve)
				VariantSubtags = orig;
			m_variantName.EndUpdate();
			m_variantName.TextChanged += m_variantName_TextChanged;
		}

		/// <summary>
		/// Check that the contents of the control are valid. If not, report the error
		/// to the user and return false. This should prevent the user from closing the
		/// containing form using OK, but not from cancelling.
		/// </summary>
		public bool CheckValid()
		{
			CheckDisposed();

			string caption = FwCoreDlgControls.kstidError;

			ScriptSubtag scriptSubtag = ScriptSubtag;
			// Can't allow a script name without an abbreviation.
			if (scriptSubtag == null && !string.IsNullOrEmpty(m_scriptName.Text.Trim()))
			{
				MessageBox.Show(FindForm(), FwCoreDlgControls.kstidMissingScrAbbr, caption);
				return false;
			}
			if (scriptSubtag != null && scriptSubtag.IsPrivateUse)
			{
				if (StandardSubtags.RegisteredScripts.Contains(scriptSubtag.Code))
				{
					MessageBox.Show(FindForm(), FwCoreDlgControls.kstidDupScrAbbr, caption);
					return false;
				}
				if (!IetfLanguageTag.IsValidScriptCode(scriptSubtag.Code))
				{
					MessageBox.Show(FindForm(), FwCoreDlgControls.kstidInvalidScrAbbr, caption);
					return false;
				}
			}

			RegionSubtag regionSubtag = RegionSubtag;
			// Can't allow a country name without an abbreviation.
			if (regionSubtag == null && !string.IsNullOrEmpty(m_regionName.Text.Trim()))
			{
				MessageBox.Show(FindForm(), FwCoreDlgControls.kstidMissingRgnAbbr, caption);
				return false;
			}
			if (regionSubtag != null && regionSubtag.IsPrivateUse)
			{
				if (StandardSubtags.RegisteredRegions.Contains(regionSubtag.Code))
				{
					MessageBox.Show(FindForm(), FwCoreDlgControls.kstidDupRgnAbbr, caption);
					return false;
				}
				if (!IetfLanguageTag.IsValidRegionCode(regionSubtag.Code))
				{
					MessageBox.Show(FindForm(), FwCoreDlgControls.kstidInvalidRgnAbbr, caption);
					return false;
				}
			}

			VariantSubtag[] variantSubtags = VariantSubtags.ToArray();
			// Can't allow a variant name without an abbreviation.
			if (string.IsNullOrEmpty(m_variantAbbrev.Text.Trim()) && !string.IsNullOrEmpty(m_variantName.Text.Trim()))
			{
				MessageBox.Show(FindForm(), FwCoreDlgControls.kstidMissingVarAbbr, caption);
				return false;
			}

			if (variantSubtags.Length > 0)
			{
				foreach (VariantSubtag variantSubtag in variantSubtags)
				{
					if (variantSubtag.IsPrivateUse)
					{
						if (StandardSubtags.RegisteredVariants.Contains(variantSubtag.Code))
						{
							MessageBox.Show(FindForm(), FwCoreDlgControls.kstidDupVarAbbr, caption);
							return false;
						}
						if (!IetfLanguageTag.IsValidPrivateUseCode(variantSubtag.Code))
						{
							MessageBox.Show(FindForm(), FwCoreDlgControls.kstidInvalidVarAbbr, caption);
							return false;
						}
					}
				}

				List<string> parts = variantSubtags.Select(v => v.Code).ToList();
				// If these subtags are private use, the first element of each must also be distinct.
				if (m_ws.Language.IsPrivateUse)
					parts.Add(m_ws.Language.Code.Split('-').First());
				if (scriptSubtag != null && scriptSubtag.IsPrivateUse)
					parts.Add(scriptSubtag.Code.Split('-').First());
				if (regionSubtag != null && regionSubtag.IsPrivateUse)
					parts.Add(regionSubtag.Code.Split('-').First());
				var uniqueParts = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
				foreach (string part in parts)
				{
					if (uniqueParts.Contains(part))
					{
						MessageBox.Show(FindForm(), String.Format(FwCoreDlgControls.kstidDuplicateParts, part), caption);
						return false;
					}
					uniqueParts.Add(part);
				}
			}

			return true;
		}

		private bool IsVoiceWritingSystem
		{
			get { return m_ws.IsVoice; }
		}

		/// <summary>
		/// This is also called when the selection changes. If the user types the exact name
		/// of an existing item, we may get both notifications, and the order we get them
		/// is not certain (In fact, it appears that it is somewhat unpredictable(!) whether
		/// the index changed happens at all!).
		/// However, whatever the order, we make the behavior depend only on whether what's
		/// in the text matches one of the items.
		/// We do this continuously, not just when the user leaves the control, because
		/// the natural place to go when leaving is the abbreviation, but that might be
		/// disabled when the user starts editing this box.
		/// </summary>
		/// <param name="sender"></param>
		/// <param name="e"></param>
		private void m_scriptName_TextChanged(object sender, EventArgs e)
		{
			string scriptName = m_scriptName.Text.Trim();
			// We don't want to store a trimmed version here because it causes very strange
			// behavior when backspace over a space.
			int selIndex = m_scriptName.FindStringExact(scriptName);
			if (selIndex >= 0)
			{
				ScriptName = scriptName;
				m_scriptAbbrev.Text = ((ScriptSubtag)m_scriptName.Items[selIndex]).Code;
				m_scriptAbbrev.Enabled = ((ScriptSubtag)m_scriptName.Items[selIndex]).IsPrivateUse;
				return;
			}

			if (string.IsNullOrEmpty(scriptName))
			{
				ScriptName = "";
				m_scriptAbbrev.Text = "";
				m_scriptAbbrev.Enabled = false;
			}
			else
			{
				m_scriptAbbrev.Text = CultureInfo.InvariantCulture.TextInfo.ToTitleCase(GetValidAbbr(scriptName, 4));
				m_scriptAbbrev.Enabled = m_scriptName.Enabled; // true except for Zxxx for audio
			}

			// We need to prevent setting this if we already set it as a side effect of choosing audio.
			// Otherwise the WS changes it to x-Zxxx.
			if (!IsVoiceWritingSystem)
				m_ws.Script = ScriptSubtag;

			if (m_enableLangTagSideEffects)
				DoSideEffectsOfChangingScriptTag();
		}

		/// <summary>
		/// Turn on or off certain side effects of changing the various components of the language tag.
		/// Turning on runs the code that normally does the side effects at once.
		/// </summary>
		void EnableLangTagSideEffects(bool enable)
		{
			m_enableLangTagSideEffects = enable;
			if (enable)
				DoSideEffectsOfChangingScriptTag();
		}

		private void DoSideEffectsOfChangingScriptTag()
		{
			PopulateVariantCombo(true);
			OnScriptRegionVariantChanged(EventArgs.Empty);
		}

		/// <summary>
		/// This is also called when the selection changes. If the user types the exact name
		/// of an existing item, we may get both notifications, and the order we get them
		/// is not certain (In fact, it appears that it is somewhat unpredictable(!) whether
		/// the index changed happens at all!).
		/// However, whatever the order, we make the behavior depend only on whether what's
		/// in the text matches one of the items.
		/// We do this continuously, not just when the user leaves the control, because
		/// the natural place to go when leaving is the abbreviation, but that might be
		/// disabled when the user starts editing this box.
		/// </summary>
		/// <param name="sender"></param>
		/// <param name="e"></param>
		private void m_regionName_TextChanged(object sender, EventArgs e)
		{
			string regionName = m_regionName.Text.Trim();

			// We don't want to store a trimmed version here because it causes very strange
			// behavior when backspace over a space.
			int selIndex = m_regionName.FindStringExact(regionName);
			if (selIndex >= 0)
			{
				RegionName = regionName;
				m_regionAbbrev.Text = ((RegionSubtag)m_regionName.Items[selIndex]).Code;
				m_regionAbbrev.Enabled = ((RegionSubtag)m_regionName.Items[selIndex]).IsPrivateUse;
				return;
			}

			if (string.IsNullOrEmpty(regionName))
			{
				m_regionAbbrev.Text = "";
				m_regionAbbrev.Enabled = false;
			}
			else
			{
				m_regionAbbrev.Enabled = true;
				m_regionAbbrev.Text = GetValidAbbr(regionName, 2).ToUpperInvariant();
			}

			RegionName = regionName;
			m_ws.Region = RegionSubtag;

			if (m_enableLangTagSideEffects)
				DoSideEffectsOfChangingScriptTag();
		}

		/// <summary>
		/// This is also called when the selection changes. If the user types the exact name
		/// of an existing item, we may get both notifications, and the order we get them
		/// is not certain. However, we expect that whatever the order, the last notification
		/// will have a correct SelectedIndex and so should produce the right effects.
		/// We do this continuously, not just when the user leaves the control, because
		/// the natural place to go when leaving is the abbreviation, but that might be
		/// disabled when the user starts editing this box.
		/// </summary>
		/// <param name="sender"></param>
		/// <param name="e"></param>
		private void m_variantName_TextChanged(object sender, EventArgs e)
		{
			m_enableLangTagSideEffects = false;
			string variantName = m_variantName.Text.Trim();
			// We don't want to store a trimmed version here because it causes very strange
			// behavior when backspace over a space.
			int selIndex = m_variantName.FindStringExact(variantName);
			if (selIndex >= 0)
			{
				m_variantName.SelectedIndex = selIndex;
				m_variantAbbrev.Enabled = true;
				var variantSubtag = (VariantSubtag) m_variantName.Items[selIndex];
				if (variantSubtag == WellKnownSubtags.IpaPhonemicPrivateUse || variantSubtag == WellKnownSubtags.IpaPhoneticPrivateUse)
					m_variantAbbrev.Text = WellKnownSubtags.IpaVariant + "-x-" + variantSubtag.Code;
				else
					m_variantAbbrev.Text = variantSubtag.IsPrivateUse ? "x-" + variantSubtag.Code : variantSubtag.Code;
				m_variantAbbrev.Enabled = variantSubtag.IsPrivateUse && !StandardSubtags.CommonPrivateUseVariants.Contains(variantSubtag);
			}
			else if (string.IsNullOrEmpty(variantName))
			{
				m_variantAbbrev.Enabled = false;
				m_variantAbbrev.Text = "";
			}
			else
			{
				m_variantAbbrev.Enabled = true;
				m_variantAbbrev.Text = GetValidAbbr(variantName, 8).ToLowerInvariant();
			}
			VariantName = variantName;

			HandleAudioVariant();
			m_enableLangTagSideEffects = true;
			OnScriptRegionVariantChanged(EventArgs.Empty);
		}

		/// <summary>Deal with special Audio handling</summary>
		private void HandleAudioVariant()
		{
			VariantSubtag[] variantSubtags = VariantSubtags.ToArray();
			if (variantSubtags.Length == 1 && variantSubtags[0] == WellKnownSubtags.AudioPrivateUse)
			{
				m_ws.IsVoice = true;
				m_scriptName.Enabled = false;
				string newScriptName = StandardSubtags.RegisteredScripts[WellKnownSubtags.AudioScript].Name;
				if (m_scriptNameString != newScriptName)
				{
					if (m_scriptName.FindStringExact(newScriptName) < 0)
						m_scriptName.Items.Add(WellKnownSubtags.AudioScript);
					ScriptName = newScriptName;
				}
				m_variantAbbrev.Text = "x-" + WellKnownSubtags.AudioPrivateUse;
				m_variantAbbrev.Enabled = false;
				m_scriptAbbrev.Text = WellKnownSubtags.AudioScript;
			}
			else
			{
				//we are changing from Audio to something else
				if (m_scriptAbbrev.Text == WellKnownSubtags.AudioScript)
				{
					m_scriptAbbrev.Text = "";
					ScriptName = "";
					m_enableLangTagSideEffects = true;
				}

				m_ws.IsVoice = false;

				// Safest to set AFTER we turn off IsVoice, but BEFORE we change the script, to avoid
				// a tempoarty invalid state. But we can't, too bad, it's invalid until this line.
				m_ws.Variants.Clear();
				foreach (VariantSubtag variantSubtag in variantSubtags)
					m_ws.Variants.Add(variantSubtag);

				m_scriptName.Enabled = true;
			}
		}

		/// <summary>
		/// Suppress entering invalid characters. Note that, for incomprehensible reasons,
		/// Backspace and returns come through this validator, while Delete and arrow keys don't,
		/// so we have to allow Backspace/return explicitly but can ignore the others.
		/// </summary>
		/// <param name="sender"></param>
		/// <param name="e"></param>
		private void m_regionCode_KeyPress(object sender, KeyPressEventArgs e)
		{
			HandleKeyPress(e);
		}

		/// <summary>
		/// Suppress entering invalid characters. Note that, for incomprehensible reasons,
		/// Backspace and returns come through this validator, while Delete and arrow keys don't,
		/// so we have to allow Backspace/return explicitly but can ignore the others.
		/// </summary>
		private void m_scriptCode_KeyPress(object sender, KeyPressEventArgs e)
		{
			HandleKeyPress(e);
		}

		/// <summary>
		/// Suppress entering invalid characters.
		/// </summary>
		/// <param name="sender"></param>
		/// <param name="e"></param>
		private void m_variantCode_KeyPress(object sender, KeyPressEventArgs e)
		{
			m_enableLangTagSideEffects = false;
			HandleKeyPress(e);
		}

		private static void HandleKeyPress(KeyPressEventArgs e)
		{
			if (e.KeyChar != (int)Keys.Back && e.KeyChar != (int)Keys.Return && e.KeyChar != (int)Keys.Delete
				&& !IsValidAbbrChar(e.KeyChar))
			{
				// Stop the character from being entered into the control since it is not valid.
				e.Handled = true;
				FwUtils.ErrorBeep();
			}
		}

		private static string GetValidAbbr(string name, int maxLen)
		{
			var sb = new StringBuilder();
			foreach (char c in name)
			{
				if (IsValidAbbrChar(c))
				{
					sb.Append(c);
					if (sb.Length == maxLen)
						break;
				}
			}
			return sb.ToString();
		}

		/// <summary>
		/// Identify characters allowed in the abbreviation fields: upper case or numeric
		/// plus hyphen. Strictly ASCII characters are used to name locales.
		/// Review: do we need to allow backspace, del, arrow keys?
		/// </summary>
		/// <param name="ch"></param>
		/// <returns></returns>
		private static bool IsValidAbbrChar(char ch)
		{
			return (ch >= 'A' && ch <= 'Z') || (ch >= '0' && ch <= '9') || ch == '-' || (ch >= 'a' && ch <= 'z');
		}

		private void m_scriptCode_TextChanged(object sender, EventArgs e)
		{
			m_ws.Script = ScriptSubtag;

			if (m_enableLangTagSideEffects)
				DoSideEffectsOfChangingScriptTag();
		}

		private void m_regionCode_TextChanged(object sender, EventArgs e)
		{
			m_ws.Region = RegionSubtag;

			if (m_enableLangTagSideEffects)
				DoSideEffectsOfChangingScriptTag();
		}

		private void m_variantCode_TextChanged(object sender, EventArgs e)
		{
			// If it's too early, don't fire the handler.  (See FWNX-999 for why this
			// is needed.)
			if (m_enableLangTagSideEffects)
				OnScriptRegionVariantChanged(EventArgs.Empty);
			else
				HandleAudioVariant();
		}

		/// <summary>
		/// Raises the <see cref="T:ScriptRegionVariantChanged"/> event.
		/// </summary>
		/// <param name="e">The <see cref="T:System.EventArgs"/> instance containing the event data.</param>
		protected virtual void OnScriptRegionVariantChanged(EventArgs e)
		{
			if (ScriptRegionVariantChanged != null)
				ScriptRegionVariantChanged(this, e);
		}
	}
}
