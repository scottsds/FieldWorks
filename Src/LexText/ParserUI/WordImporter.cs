// Copyright (c) 2002-2016 SIL International
// This software is licensed under the LGPL, version 2.1 or later
// (http://www.gnu.org/licenses/lgpl-2.1.html)

using System.IO;
using System.Collections.Generic;
using SIL.LCModel.Core.Text;
using SIL.LCModel.Core.WritingSystems;
using SIL.LCModel.Core.KernelInterfaces;
using SIL.LCModel;
using SIL.LCModel.DomainServices;

namespace SIL.FieldWorks.LexText.Controls
{
	/// <summary>
	/// a class for parsing text files and populating WfiWordSets with them.
	/// </summary>
	public class WordImporter
	{
		private readonly LcmCache m_cache;
		private readonly CoreWritingSystemDefinition m_ws;

		public WordImporter(LcmCache cache)
		{
			m_cache = cache;
			m_ws = cache.ServiceLocator.WritingSystems.DefaultVernacularWritingSystem;
		}

		public void PopulateWordset(string path, IWfiWordSet wordSet)
		{
			// Note: The ref collection class ensures duplicates are not added to CasesRC.
			foreach (var wordform in GetWordformsInFile(path))
				wordSet.CasesRC.Add(wordform);
		}

		// NB: This will currently return hvos
		private IEnumerable<IWfiWordform> GetWordformsInFile(string path)
		{
			// Note: The GetUniqueWords uses the key for faster processing,
			// even though we don;t use it here.
			var wordforms = new Dictionary<string, IWfiWordform>();
			using (TextReader reader = File.OpenText(path))
			{
// do timing tests; try doing readtoend() to one string and then parsing that; see if more efficient
				// While there is a line to be read in the file
				// REVIEW: can this be broken by a very long line?
				// RR answer: Yes it can.
				// According to the ReadLine docs an ArgumentOutOfRangeException
				// exception will be thrown if the line length exceeds the MaxValue constant property of an Int32,
				// which is 2,147,483,647. I doubt you will run into this exception any time soon. :-)
				string line;
				while((line = reader.ReadLine()) != null)
					GetUniqueWords(wordforms, line);
			}
			return wordforms.Values;
		}

		/// <summary>
		/// Collect up a set of unique WfiWordforms.
		/// </summary>
		private void GetUniqueWords(Dictionary<string, IWfiWordform> wordforms, string buffer)
		{
			int start = -1; // -1 means we're still looking for a word to start.
			int length = 0;
			int totalLengh = buffer.Length;
			for(int i = 0; i < totalLengh; i++)
			{
				bool isWordforming = m_ws.get_IsWordForming(buffer[i]);
				if (isWordforming)
				{
					length++;
					if (start < 0) //first character in this word?
						start = i;
				}

				if ((start > -1) // had a word and found yet?
					 && (!isWordforming || i == totalLengh - 1 /*last char of the input*/))
				{
					string word = buffer.Substring(start, length);
					if (!wordforms.ContainsKey(word))
					{
						ITsString tss = TsStringUtils.MakeString(word, m_ws.Handle);
						wordforms.Add(word, WfiWordformServices.FindOrCreateWordform(m_cache, tss));
					}
					length = 0;
					start = -1;
				}
			}
		}
	}
}
