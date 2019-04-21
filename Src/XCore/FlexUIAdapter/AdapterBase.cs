// Copyright (c) 2004-2013 SIL International
// This software is licensed under the LGPL, version 2.1 or later
// (http://www.gnu.org/licenses/lgpl-2.1.html)

using System;
using System.IO;
using System.Windows.Forms;

namespace XCore
{
	/// <summary>
	/// Base class for all adapters
	/// </summary>
	public abstract class AdapterBase : IUIAdapter
	{
		#region Data members

		/// <summary>
		/// The XCore mediator.
		/// </summary>
		protected Mediator m_mediator;
		/// <summary>
		///
		/// </summary>
		protected PropertyTable m_propertyTable;
		/// <summary>
		/// The main XWindow form.
		/// </summary>
		protected Form m_window;
		/// <summary>
		/// Collection of small images.
		/// </summary>
		protected IImageCollection m_smallImages;
		/// <summary>
		/// Collection of large images.
		/// </summary>
		protected IImageCollection m_largeImages;
		/// <summary>
		/// The subclass specific main control that is given back to the adapter library client.
		/// </summary>
		protected System.Windows.Forms.Control m_control;

		#endregion Data members

		/// <summary>
		/// store the location/settings of various widgets so that we can restore them next time
		/// </summary>
		public virtual void PersistLayout()
		{
		}

		protected string SettingsPath()
		{
			string path = Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData);
			path = Path.Combine(path, Path.Combine(Application.CompanyName, Application.ProductName));
			Directory.CreateDirectory(path);
			return path;
		}

		#region Properties

		/// <summary>
		/// Gets the main control that is given to the adapter library client.
		/// </summary>
		protected virtual Control MyControl
		{
			get { return m_control; }
		}

		/// <summary>
		/// The manager for whatever bars can be docked on any of the four main window edges.
		/// </summary>
		protected ToolStripManager Manager
		{
			get
			{
				ToolStripManager manager = m_propertyTable.GetValue<ToolStripManager>("ToolStripManager");
				if (manager == null)
				{
					manager = new ToolStripManager();
					m_window.Controls.Add(manager);
					m_propertyTable.SetProperty("ToolStripManager", manager, true);
					m_propertyTable.SetPropertyPersistence("ToolStripManager", false);
				}

				return manager;
			}

		}
		#endregion Properties

		#region Construction

		/// <summary>
		/// Constructor.
		/// </summary>
		public AdapterBase()
		{
		}

		#endregion Construction

		#region IUIAdapter implementation

		/// <summary>
		/// Initializes the adapter.
		/// </summary>
		/// <param name="window">The main form.</param>
		/// <param name="smallImages">Collection of small images.</param>
		/// <param name="largeImages">Collection of large images.</param>
		/// <param name="mediator">XCore Mediator.</param>
		/// <param name="propertyTable"></param>
		/// <returns>A Control for use by client.</returns>
		public virtual Control Init(Form window,
			IImageCollection smallImages, IImageCollection largeImages, Mediator mediator, PropertyTable propertyTable)
		{
			m_window = window;
			m_smallImages = smallImages;
			m_largeImages = largeImages;
			if (this is IxCoreColleague)
			{
				((IxCoreColleague)this).Init(mediator, propertyTable, null/*I suppose we could get the config node to the adapter if someone needs that someday*/);
			}
			else
			{
				m_mediator = mediator;
				m_propertyTable = propertyTable;
			}
			return MyControl;
		}

		/// <summary>
		/// Implement do-nothing method to keep the compiler happy.
		/// Subclasses override this method to do useful things.
		/// </summary>
		/// <param name="groupCollection">Collection of choices.</param>
		public virtual void CreateUIForChoiceGroupCollection(ChoiceGroupCollection groupCollection)
		{
		}


		/// <summary>
		/// Implement a do-nothing method to keep the compiler happy.
		/// </summary>
		/// <param name="group">The group that is the basis for this menu</param>
		public virtual void CreateUIForChoiceGroup(ChoiceGroup group)
		{
		}

		/// <summary>
		/// Implement a do-nothing method to keep the compiler happy.
		/// </summary>
		public virtual void OnIdle()
		{
		}

		/// <summary>
		/// Implement a do-nothing method to keep the compiler happy.
		/// </summary>
		public virtual void FinishInit()
		{
		}

		#endregion IUIAdapter implementation
	}
}
