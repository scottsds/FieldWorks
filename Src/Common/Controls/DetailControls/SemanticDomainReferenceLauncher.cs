﻿// Copyright (c) 2012-2013 SIL International
// This software is licensed under the LGPL, version 2.1 or later
// (http://www.gnu.org/licenses/lgpl-2.1.html)
//
// File: SemanticDomainReferenceLauncher.cs
//
// <remarks>
// </remarks>

using System.Diagnostics;
using System.Windows.Forms;
using SIL.FieldWorks.Common.Controls;
using SIL.FieldWorks.Common.FwUtils;
using SIL.LCModel;
using SIL.LCModel.Infrastructure;

namespace SIL.FieldWorks.Common.Framework.DetailControls
{
	internal class SemanticDomainReferenceLauncher : PossibilityVectorReferenceLauncher
	{
		#region Construction, Initialization, and Disposing

		/// -----------------------------------------------------------------------------------
		/// <summary>
		/// Initializes a new instance of the <see cref="ReferenceLauncher"/> class.
		/// </summary>
		/// -----------------------------------------------------------------------------------
		public SemanticDomainReferenceLauncher()
		{
		}

		#endregion // Construction, Initialization, and Disposing

		/// <summary>
		/// Handle launching of the standard chooser.
		/// </summary>
		/// <remarks>
		/// Subclasses should override this method, if the SimpleListChooser is not suitable.
		/// </remarks>
		protected override void HandleChooser()
		{
			const string displayWs = "best analysis";
			var sense = m_obj as ILexSense;
			if (sense == null)
			{
				Debug.Assert(sense != null, "This chooser can only be applied to senses");
				// ReSharper disable HeuristicUnreachableCode
				//reachable in release mode you usually intelligent program.
				return;
				// ReSharper restore HeuristicUnreachableCode
			}
			var linkCommandNode = m_configurationNode.SelectSingleNode("descendant::chooserLink");
			var chooser = new SemanticDomainsChooser
				{
					Mediator = m_mediator,
					Cache = m_cache,
					DisplayWs = displayWs,
					Sense = sense,
					LinkNode = linkCommandNode,
					HelpTopicProvider = m_propertyTable.GetValue<IHelpTopicProvider>("HelpTopicProvider")
			};

			var labels = ObjectLabel.CreateObjectLabels(m_cache, m_obj.ReferenceTargetCandidates(m_flid),
				m_displayNameProperty, displayWs);
			chooser.Initialize(labels, sense.SemanticDomainsRC, m_propertyTable);
			var result = chooser.ShowDialog();
			if(result == DialogResult.OK)
			{
				UndoableUnitOfWorkHelper.DoUsingNewOrCurrentUOW(Resources.DetailControlsStrings.ksUndoSet,
																Resources.DetailControlsStrings.ksRedoSet,
																m_cache.ActionHandlerAccessor,
																() => sense.SemanticDomainsRC.Replace(sense.SemanticDomainsRC, chooser.SemanticDomains));
			}
		}
	}
}
