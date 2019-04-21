// Copyright (c) 2015 SIL International
// This software is licensed under the LGPL, version 2.1 or later
// (http://www.gnu.org/licenses/lgpl-2.1.html)

using System.Collections;
using System.Xml;
using SIL.Utils;

namespace XCore
{
	/// <summary>
	/// concrete implementations of this provide a list of RecordFilters to offer to the user.
	/// </summary>
	public abstract class RecordFilterListProvider : IxCoreColleague
	{
		protected XmlNode m_configuration;
		protected Mediator m_mediator;
		protected PropertyTable m_propertyTable;

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// a factory method for RecordFilterListProvider
		/// </summary>
		/// <param name="mediator">The mediator.</param>
		/// <param name="propertyTable">The property table</param>
		/// <param name="configuration">The configuration.</param>
		/// <returns></returns>
		/// ------------------------------------------------------------------------------------
		static public RecordFilterListProvider Create(Mediator mediator, PropertyTable propertyTable, XmlNode configuration)
		{
			RecordFilterListProvider p = (RecordFilterListProvider)DynamicLoader.CreateObject(configuration);
			if (p != null)
				p.Init(mediator, propertyTable, configuration);
			return p;
		}


		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Initialize the filter list. this is called because we are an IxCoreColleague
		/// </summary>
		/// <param name="mediator">The mediator.</param>
		/// <param name="propertyTable">The PropertyTable</param>
		/// <param name="configuration">The configuration.</param>
		/// ------------------------------------------------------------------------------------
		public virtual void Init(Mediator mediator, PropertyTable propertyTable, XmlNode configuration)
		{
			m_mediator = mediator;
			m_propertyTable = propertyTable;
			m_configuration = configuration;
		}

		/// <summary>
		/// reload the data items
		/// </summary>
		public virtual void ReLoad()
		{
		}


		/// <summary>
		/// return an array of all of the objects which should
		/// 1) be queried when looking for someone to deliver a message to
		/// 2) be potential recipients of a broadcast
		/// this is called because we are an IxCoreColleague
		/// </summary>
		/// <returns></returns>
		public IxCoreColleague[] GetMessageTargets()
		{
			return new IxCoreColleague[]{this};
		}

		/// <summary>
		/// Never any reason not to call this instance.
		/// </summary>
		public bool ShouldNotCall
		{
			get { return false; }
		}

		public virtual int Priority { get { return (int)ColleaguePriority.Medium; } }

		/// <summary>
		/// the list of filters.
		/// </summary>
		public abstract ArrayList Filters
		{
			get;
		}

		//this has a signature of object just because is confined to XCore, so does not know about FDO RecordFilters
		public abstract object GetFilter(string id);

		/// <summary>
		/// May want to update / reload the list based on user selection.
		/// </summary>
		/// <param name="argument"></param>
		/// <returns>true if handled.</returns>
		public virtual bool OnAdjustFilterSelection(object argument)
		{
			return false;
		}
	}
}
