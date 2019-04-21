// Copyright (c) 2003-2013 SIL International
// This software is licensed under the LGPL, version 2.1 or later
// (http://www.gnu.org/licenses/lgpl-2.1.html)
//
// File: ResolveDirectory.cs
// History: John Hatton
// Last reviewed:
//
// <remarks>
//	This file includes a non FieldWorks implementation.
//	A more complicated, FieldWorks only implementation, is/will be
//	part of a FieldWorks only utilities assembly.
// </remarks>
// -------
using System;
using System.IO;
using SIL.PlatformUtilities;

namespace SIL.Utils
{

	public interface IResolvePath
	{
		/// <summary>
		/// Given some description of a directory or file, return the path to the actual directory.
		/// </summary>
		/// <param name="path"></param>
		/// <param name="fIgnoreMissingFile">If true, return null if not found. Otherwise throw.</param>
		/// <exception cref="FileNotFoundException">thrown into the resulting directory does not exist</exception>
		/// <returns></returns>
		string Resolve(string path, bool fIgnoreMissingFile);
		string Resolve(string path);
		string Resolve (string parentPath, string path);
		string Resolve(string parentPath, string path, bool fIgnoreMissingFile);

		string BaseDirectory {get; set;}
	}

	/// <summary>
	/// A simple, non-FieldWorks-reliant implementation which provides a base directory and
	/// replaces any environment variables
	/// </summary>
	public class SimpleResolver : IResolvePath
	{
		protected string m_baseDirectory=null;

		public string Resolve(string path)
		{
			return Resolve(path, false);
		}

		/// <summary>
		/// Try to resolve the path, but if fIgnoreMissingFile is true, don't complain if it doesn't exist; return null.
		/// </summary>
		/// <param name="path"></param>
		/// <param name="fIgnoreMissingFile"></param>
		/// <returns></returns>
		public string Resolve(string path, bool fIgnoreMissingFile)
		{
			string result = Environment.ExpandEnvironmentVariables(path);
			if (m_baseDirectory!= null)
				result = Path.Combine(m_baseDirectory, result);
			else
				result = Path.GetFullPath(result);

			if (!Platform.IsWindows)
			{
				// TODO-Linux: xml files contain include paths of the form fruit\veggies
				// which System.IO.File.Exists (on Linux) can't handle because of the '\' char
				// so we convert if System.IO.File.Exists fails and reattempt the Exists method

				string modifiedResult;
				if (!File.Exists(result))
				{
					modifiedResult = result.Replace('\\', '/');
					if (File.Exists(modifiedResult))
						result = modifiedResult;
				}
			}

			if (!File.Exists(result))
			{
				if (fIgnoreMissingFile)
					return null;

				throw new FileNotFoundException(result);
			}
			return result;
		}

		/// <summary>
		/// try to find the file, first looking in the given parentPath. Throw if not found.
		/// </summary>
		/// <param name="parentPath"></param>
		/// <param name="path"></param>
		/// <returns></returns>
		public string Resolve(string parentPath, string path)
		{
			return Resolve(parentPath, path, false);
		}
		/// <summary>
		/// try to find the file, first looking in the given parentPath.
		/// </summary>
		/// <param name="parentPath"></param>
		/// <param name="path"></param>
		/// <param name="fIgnoreMissingFile">If true, return null if not found. Otherwise throw.</param>
		/// <returns></returns>
		public string Resolve(string parentPath, string path, bool fIgnoreMissingFile)
		{
			string result = Environment.ExpandEnvironmentVariables(path);
			if (parentPath!= null)
				result = Path.Combine(parentPath, result);
			else
				result = Path.GetFullPath(result);

			if(!File.Exists(result))
				return Resolve(path, fIgnoreMissingFile);//using the parentPath did not help, try looking in the base directory

			return result;
		}

		public void test()
		{
		}

		/// <summary>
		/// If you specify this, then any future paths will be resolved relative to this directory.
		/// Otherwise, they will be resolved relative to the current working directory.
		///
		/// environment variables will be expanded.
		/// </summary>
		public string  BaseDirectory
		{
			get { return m_baseDirectory; }
			set
			{
				m_baseDirectory= Environment.ExpandEnvironmentVariables(value);
			}
		}
	}
}
