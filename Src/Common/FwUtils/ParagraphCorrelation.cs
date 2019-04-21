// Copyright (c) 2006-2013 SIL International
// This software is licensed under the LGPL, version 2.1 or later
// (http://www.gnu.org/licenses/lgpl-2.1.html)
//
// File: ParagraphCorrelation.cs
// Responsibility: TE Team
// Last reviewed:
//
// <remarks>
// </remarks>

using System;
using System.Collections.Generic;
using System.Text;
using SIL.LCModel.Core.Text;
using SIL.LCModel.Utils;

namespace SIL.FieldWorks.Common.FwUtils
{
	/// -----------------------------------------------------------------------------------
	/// <summary>
	/// Determine how well paragraph strings match
	/// </summary>
	/// -----------------------------------------------------------------------------------
	public class ParagraphCorrelation
	{
		private string m_para1;
		private string m_para2;
		private double m_correlationFactor;

		/// <summary>
		/// The word list is a hashtable that maps words to word counts in the two paragraphs.
		/// Each entry in this table is an integer array with two elements - the word count
		/// in the first paragraph and the word count in the second paragraph.
		/// </summary>
		private Dictionary<string, int[]> m_wordList;

		/// -----------------------------------------------------------------------------------
		/// <summary>
		/// Initializes a new instance of the <see cref="ParagraphCorrelation"/> class.
		/// </summary>
		/// -----------------------------------------------------------------------------------
		public ParagraphCorrelation(string para1, string para2)
		{
			m_para1 = (para1 == null) ? string.Empty : para1;
			m_para2 = (para2 == null) ? string.Empty : para2;
			if (m_para1 == m_para2)
				m_correlationFactor = 1.0;
			else
			{
				m_wordList = new Dictionary<string, int[]>();
				BuildCorrelation();
			}
		}

		/// -----------------------------------------------------------------------------------
		/// <summary>
		/// Read the paragraphs, build word list and calculate the correlation factor
		/// </summary>
		/// -----------------------------------------------------------------------------------
		private void BuildCorrelation()
		{
			AddToWordList(m_para1, 0);
			AddToWordList(m_para2, 1);

			CalculateCorrelationFactor();
		}

		/// -----------------------------------------------------------------------------------
		/// <summary>
		/// Calculate a correlation factor between the two paragraphs
		/// </summary>
		/// <remarks>
		/// The correlation factor is calculated as an average of the differences in the word
		/// counts. If paragraphs contain identical words, the resulting factor will be 1.0.
		/// </remarks>
		/// -----------------------------------------------------------------------------------
		private void CalculateCorrelationFactor()
		{
			double sum = 0.0;

			foreach (int[] wordCounts in m_wordList.Values)
			{
				int count1 = wordCounts[0];
				int count2 = wordCounts[1];
				double delta = Math.Abs(count1 - count2);
				sum += 1.0 - (delta / Math.Max(count1, count2));
			}

			m_correlationFactor = sum / m_wordList.Count;
		}

		/// -----------------------------------------------------------------------------------
		/// <summary>
		/// Add to the word list using the given paragraph.
		/// </summary>
		/// <param name="para">paragraph text to scan</param>
		/// <param name="paraIndex">index of the given paragraph (0 or 1)</param>
		/// -----------------------------------------------------------------------------------
		private void AddToWordList(string para, int paraIndex)
		{
			char[] punctuation = new char[] {' '};
			string cleanPara = RemoveDigitsAndPunc(para);

			foreach (string word in cleanPara.Split(punctuation))
			{
				// If the word is found, then increment the count
				int[] wordCounts = null;
				if (m_wordList.ContainsKey(word))
				{
					wordCounts = m_wordList[word];
					++wordCounts[paraIndex];
				}
				else
				{
					// If the word does not yet exist, then make an entry for it.
					wordCounts = new int[2];
					wordCounts[paraIndex] = 1;
					m_wordList[word] = wordCounts;
				}
			}
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Removes the digits and punctuation from a paragraph so that they will not be
		/// considered in the paragraph correlation.
		/// </summary>
		/// <param name="para">The para.</param>
		/// <returns>The para with the digits and punctuation removed.</returns>
		/// ------------------------------------------------------------------------------------
		private string RemoveDigitsAndPunc(string para)
		{
			StringBuilder strBldr = new StringBuilder(para.Length);

			for (int iChar = 0; iChar < para.Length; iChar++)
			{
				if (!Icu.Character.IsNumeric(para[iChar]) &&
					!Icu.Character.IsPunct(para[iChar]))
				{
					strBldr.Append(para[iChar]);
				}
			}

			return strBldr.ToString().Trim();
		}

		#region Static methods
		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Determine string correlation between the two given strings.
		/// </summary>
		/// <param name="stringCurr">current string</param>
		/// <param name="stringRev">revision string</param>
		/// <returns>
		/// correlation factor between stringCurr and stringRev
		/// </returns>
		/// ------------------------------------------------------------------------------------
		public static double DetermineStringCorrelation(string stringCurr, string stringRev)
		{
			ParagraphCorrelation pc = new ParagraphCorrelation(stringCurr, stringRev);
			return pc.CorrelationFactor;
		}
		#endregion

		#region properties
		/// -----------------------------------------------------------------------------------
		/// <summary>
		/// Return the correlation factor between the paragraphs
		/// </summary>
		/// -----------------------------------------------------------------------------------
		public double CorrelationFactor
		{
			get { return m_correlationFactor; }
		}

		/// -----------------------------------------------------------------------------------
		/// <summary>
		/// Find the longest common substring between the two paragraphs that is useful.
		/// </summary>
		/// -----------------------------------------------------------------------------------
		public string LongestUsefulSubstring
		{
			get
			{
				if (m_para1 == m_para2)
					return m_para1;
				if (m_correlationFactor < .2)
					return string.Empty;

				bool fWholeWord;
				return StringUtils.LongestUsefulCommonSubstring(m_para1, m_para2, false, out fWholeWord);
			}
		}
		#endregion
	}
}
