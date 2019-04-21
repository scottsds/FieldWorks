// Copyright (c) 2003-2017 SIL International
// This software is licensed under the LGPL, version 2.1 or later
// (http://www.gnu.org/licenses/lgpl-2.1.html)

using System;
using System.Drawing;
using System.IO;
using System.Reflection;
using System.Windows.Forms;
using System.Xml;
using SIL.LCModel.Utils;
using SIL.Utils;

namespace XCore
{
	/// <summary/>
	public class ImageCollection: IImageCollection
	{
		protected ImageList m_images;
		protected System.Collections.Specialized.StringCollection m_labels;

		/// <summary/>
		public ImageCollection(bool isLarge)
		{
			m_images = new ImageList();
			m_images.ColorDepth = ColorDepth.Depth24Bit;
			if(isLarge)
				m_images.ImageSize = new System.Drawing.Size(32,32);
			m_labels = new System.Collections.Specialized.StringCollection();
		}

		#region Disposable stuff
		#if DEBUG
		/// <summary/>
		~ImageCollection()
		{
			Dispose(false);
		}
		#endif

		/// <summary/>
		public bool IsDisposed { get; private set; }

		/// <summary/>
		public void Dispose()
		{
			Dispose(true);
			GC.SuppressFinalize(this);
		}

		/// <summary/>
		protected virtual void Dispose(bool fDisposing)
		{
			System.Diagnostics.Debug.WriteLineIf(!fDisposing, "****** Missing Dispose() call for " + GetType().Name + ". ****** ");
			if (fDisposing && !IsDisposed)
			{
				// dispose managed and unmanaged objects
				if (m_images != null)
					m_images.Dispose();
			}
			m_images = null;
			IsDisposed = true;
		}
		#endregion

		/// <summary>
		/// append the images in this list to our ImageList
		/// </summary>
		/// <param name="list">the ImageList to append</param>
		/// <param name="labels">the labels, in order, for each imaging the list</param>
		public void AddList (ImageList list, string[] labels)
		{
			//Debug.Assert(list == labels.Length);
			foreach(Image image in list.Images)
			{
				m_images.Images.Add(image);
			}
			foreach(string label in labels)
			{
				m_labels.Add(label.Trim()); //it's easy to lead a extra space sneak in there and ruin your day
			}


			//note that we can only handle one transparent color,
			//which is set here. Therefore, ImageList's added via this function should have the same transparency color.
			m_images.TransparentColor = list.TransparentColor;
		}

		/// <summary/>
		public Image GetImage(string label)
		{
			int i = m_labels.IndexOf(label);
			if(i>=0)
				return m_images.Images[i];
			else if(label !=null && label.Length>0 &&  m_images.Images.Count>0)
				return m_images.Images[0];		//let the first one be the default
			else
				return null;
		}

		/// <summary/>
		public int GetImageIndex(string label)
		{
			int i = m_labels.IndexOf(label);
			if(i>=0)
				return i;
			else
				return 0;//let 0 be the default in case something goes wrong
		}

		/// <summary/>
		public ImageList ImageList
		{
			get
			{
				return m_images;
			}
		}

		/// <summary/>
		public void AddList(XmlNodeList nodes)
		{
			foreach(XmlNode node in nodes)
			{
				string assemblyName = XmlUtils.GetAttributeValue(node, "assemblyPath").Trim();
				// Prepend the directory where the current DLL lives.  This should fix
				// LT-1541 (and similar bugs) once and for all!
				string codeBasePath = FileUtils.StripFilePrefix(Assembly.GetExecutingAssembly().CodeBase);
				string baseDir = Path.GetDirectoryName(codeBasePath);
				string assemblyPath = Path.Combine(baseDir, assemblyName);
				string className = XmlUtils.GetAttributeValue(node, "class").Trim();
				string field = XmlUtils.GetAttributeValue(node, "field").Trim();

				Assembly assembly;
				try
				{
					assembly = Assembly.LoadFrom(assemblyPath);
					if (assembly == null)
						throw new ApplicationException(); //will be caught and described in the catch
				}
				catch (Exception error)
				{
					throw new RuntimeConfigurationException("XCore Could not load the  DLL at :"+assemblyPath, error);
				}

				//make the  holder
				object holder = assembly.CreateInstance(className);
				try
				{
				if(holder == null)
				  throw new RuntimeConfigurationException("XCore could not create the class: "+className+". Make sure capitalization is correct and that you include the name space (e.g. XCore.ImageHolder).");

				//get the named ImageList
				FieldInfo info = holder.GetType().GetField(field);

				if(info== null)
				  throw new RuntimeConfigurationException("XCore could not find the field '"+field+"' in the class: "+className+". Make sure that the field is marked 'public' and that capitalization is correct.");

				ImageList images = (ImageList) info.GetValue(holder);

				string[] labels = XmlUtils.GetAttributeValue(node, "labels").Split(new char[]{','});
				if(labels.Length != images.Images.Count)
					throw new ConfigurationException("The number of image labels does not match the number of images in this <imageList>: "+node.OuterXml);
				this.AddList(images, labels);
			}
				finally
				{
					var disposable = holder as IDisposable;
					if (disposable != null)
						disposable.Dispose();
				}
			}
		}
	}
}
