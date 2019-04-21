// Copyright (c) 2010-2017 SIL International
// This software is licensed under the LGPL, version 2.1 or later
// (http://www.gnu.org/licenses/lgpl-2.1.html)

using System;
using System.Collections;
using System.Collections.Generic;

namespace SIL.FieldWorks.Common.FwUtils
{
	/// ----------------------------------------------------------------------------------------
	/// <summary>
	/// This class allows an array of bytes to be enumerated as a collection of characters
	/// </summary>
	/// ----------------------------------------------------------------------------------------
	public class CharEnumeratorForByteArray : IEnumerable<char>
	{
		private readonly byte[] m_data;

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Initializes a new instance of the <see cref="CharEnumeratorForByteArray"/> class.
		/// </summary>
		/// <param name="data">The data.</param>
		/// ------------------------------------------------------------------------------------
		public CharEnumeratorForByteArray(byte[] data)
		{
			if (data == null)
				throw new ArgumentNullException("data");
			m_data = data;
		}

		#region IEnumerable<char> Members
		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Returns an enumerator that iterates through the characters.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		public IEnumerator<char> GetEnumerator()
		{
			for (int i = 0; i < m_data.Length - 1; i += 2)
			{
				// ENHANCE: Need to change the byte order for Mac (or other big-endian) if we
				// support them
				yield return (char)(m_data[i] | m_data[i + 1] << 8);
			}

			if ((m_data.Length & 1) != 0)
			{
				// We had an odd number of bytes, so return the last byte
				yield return (char)m_data[m_data.Length - 1];
			}
		}
		#endregion

		#region IEnumerable Members
		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Returns an enumerator that iterates through the characters.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		IEnumerator IEnumerable.GetEnumerator()
		{
			return GetEnumerator();
		}
		#endregion
	}
}
