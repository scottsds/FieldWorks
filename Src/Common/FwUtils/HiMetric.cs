// Copyright (c) 2009-2017 SIL International
// This software is licensed under the LGPL, version 2.1 or later
// (http://www.gnu.org/licenses/lgpl-2.1.html)

using System;

namespace SIL.FieldWorks.Common.FwUtils
{
	/// <summary>
	/// Facilites the converstion of pixels to hiMetrics and vice versa.
	/// HiMetric is 1/1000 of a cm.
	/// </summary>
	public class HiMetric
	{
		// definition taken from C++ views code
		private const int HIMETRIC_INCH = 2540;

		/// <summary>Stores the current value in HiMetric</summary>
		protected int m_hiMetric;

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Initializes a new instance of the <see cref="HiMetric"/> class.
		/// </summary>
		/// <param name="pixels">The pixels.</param>
		/// <param name="dpi">The dpi.</param>
		/// ------------------------------------------------------------------------------------
		public HiMetric(int pixels, int dpi)
		{
			m_hiMetric = (int)Math.Round((double)pixels*(double)HIMETRIC_INCH / (double)dpi);
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Initializes a new instance of the <see cref="HiMetric"/> class.
		/// </summary>
		/// <param name="hiMetrix">The hi metrix.</param>
		/// ------------------------------------------------------------------------------------
		public HiMetric(int hiMetrix)
		{
			m_hiMetric = hiMetrix;
		}

		//
		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Gets the value in pixels for a given dpi
		/// </summary>
		/// <param name="dpi">The dpi.</param>
		/// <returns>returns the value in pixels for a given dpi</returns>
		/// ------------------------------------------------------------------------------------
		public int GetPixels(int dpi)
		{
			return (int)Math.Round((double)dpi * (double)m_hiMetric / (double)HIMETRIC_INCH);
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Gets the value in HiMetric.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		public int Value
		{
			get
			{
				return m_hiMetric;
			}
		}
	}
}
