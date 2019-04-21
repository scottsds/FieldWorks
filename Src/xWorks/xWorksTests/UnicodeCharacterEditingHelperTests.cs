﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Windows.Forms;
using NUnit.Framework;
using SIL.FieldWorks.XWorks.DictionaryDetailsView;
using SIL.LCModel.Utils;

namespace SIL.FieldWorks.XWorks
{
	public class UnicodeCharacterEditingHelperTests
	{
		[Test]
		public void TextSuffixReturnsSame()
		{
			var errorMessage = "should not have changed an input that did not end in numbers";
			string input = "xyz";
			Assert.That(UnicodeCharacterEditingHelper.ConvertFinalDigitsToCharacter(input), Is.EqualTo(input), errorMessage);
			input = "111jabcj2222xyz";
			Assert.That(UnicodeCharacterEditingHelper.ConvertFinalDigitsToCharacter(input), Is.EqualTo(input), errorMessage);
			input = null;
			Assert.That(UnicodeCharacterEditingHelper.ConvertFinalDigitsToCharacter(input), Is.EqualTo(input), errorMessage);
			// Ends in space
			input = "1234 ";
			Assert.That(UnicodeCharacterEditingHelper.ConvertFinalDigitsToCharacter(input), Is.EqualTo(input), errorMessage);
			input = "";
			Assert.That(UnicodeCharacterEditingHelper.ConvertFinalDigitsToCharacter(input), Is.EqualTo(input), errorMessage);
		}

		[Test]
		public void EndingInNumberGivesConversion()
		{
			var errorMessage = "should have converted ending numbers to unicode character";
			Assert.That(UnicodeCharacterEditingHelper.ConvertFinalDigitsToCharacter("1234"), Is.EqualTo("\u1234"), "should have converted to unicode character");
			Assert.That(UnicodeCharacterEditingHelper.ConvertFinalDigitsToCharacter("222j333"), Is.EqualTo("222j\u0333"), errorMessage);
			Assert.That(UnicodeCharacterEditingHelper.ConvertFinalDigitsToCharacter("j333"), Is.EqualTo("j\u0333"), errorMessage);
			Assert.That(UnicodeCharacterEditingHelper.ConvertFinalDigitsToCharacter("j1234"), Is.EqualTo("j\u1234"), errorMessage);
			Assert.That(UnicodeCharacterEditingHelper.ConvertFinalDigitsToCharacter("222j1234"), Is.EqualTo("222j\u1234"), errorMessage);
			Assert.That(UnicodeCharacterEditingHelper.ConvertFinalDigitsToCharacter("111jabc"), Is.EqualTo("111j\u0abc"), errorMessage);
			Assert.That(UnicodeCharacterEditingHelper.ConvertFinalDigitsToCharacter("BbzcCx111jAbC"), Is.EqualTo("BbzcCx111j\u0abc"), "Should have handled mixed-case hex digits");
			Assert.That(UnicodeCharacterEditingHelper.ConvertFinalDigitsToCharacter("11\n1jabc"), Is.EqualTo("11\n1j\u0abc"),"Should have worked even with a newline character in input");
			Assert.That(UnicodeCharacterEditingHelper.ConvertFinalDigitsToCharacter("FFFF"), Is.EqualTo("\uFFFF"), "should have supported high numbers");
			Assert.That(UnicodeCharacterEditingHelper.ConvertFinalDigitsToCharacter("111jedcb"), Is.EqualTo("111j\uedcb"), "should have supported high numbers");
		}

		[Test]
		public void EndingInHexDigitsForSurrogateDoesNotOperate()
		{
			Assert.That(UnicodeCharacterEditingHelper.ConvertFinalDigitsToCharacter("111jD834"), Is.EqualTo("111jD834"), "Don't operate on surrogates");
		}

		[Test]
		public void ManyDigitsAtEndOnlyAccountsForLastFour()
		{
			var errorMessage = "Only handle last 4 digits";
			// Only supporting 4 digits presently
			Assert.That(UnicodeCharacterEditingHelper.ConvertFinalDigitsToCharacter("111j12345"), Is.EqualTo("111j1\u2345"), errorMessage);
			Assert.That(UnicodeCharacterEditingHelper.ConvertFinalDigitsToCharacter("111jaaaaa"), Is.EqualTo("111ja\uaaaa"), errorMessage);
			Assert.That(UnicodeCharacterEditingHelper.ConvertFinalDigitsToCharacter("111jaaaaaa"), Is.EqualTo("111jaa\uaaaa"), errorMessage);
		}

		[Test]
		public void AllowForUPlusNotation()
		{
			var errorMessage = "Handle U+ or u+ notation";
			Assert.That(UnicodeCharacterEditingHelper.ConvertFinalDigitsToCharacter("111jU+1234"), Is.EqualTo("111j\u1234"), errorMessage);
			Assert.That(UnicodeCharacterEditingHelper.ConvertFinalDigitsToCharacter("u+1234"), Is.EqualTo("\u1234"), errorMessage);
		}

		[Test]
		public void CharacterToNumbersConvertsFinal()
		{
			Assert.That(UnicodeCharacterEditingHelper.ConvertFinalCharacterToCodepoint("A"), Is.EqualTo("0041"), "should have converted final character to four hex-digit unicode representation");
			Assert.That(UnicodeCharacterEditingHelper.ConvertFinalCharacterToCodepoint("AA"), Is.EqualTo("A0041"));
			Assert.That(UnicodeCharacterEditingHelper.ConvertFinalCharacterToCodepoint("1234A"), Is.EqualTo("12340041"));
			Assert.That(UnicodeCharacterEditingHelper.ConvertFinalCharacterToCodepoint("\u1234"), Is.EqualTo("1234"));
			Assert.That(UnicodeCharacterEditingHelper.ConvertFinalCharacterToCodepoint("AA\u1234"), Is.EqualTo("AA1234"));
			Assert.That(UnicodeCharacterEditingHelper.ConvertFinalCharacterToCodepoint("AA5555"), Is.EqualTo("AA5550035"), "Should have converted final character to four hex-digit representation");
			Assert.That(UnicodeCharacterEditingHelper.ConvertFinalCharacterToCodepoint("AB\nBA"), Is.EqualTo("AB\nB0041"), "Should have worked even with a newline character in input");
		}

		[Test]
		public void CharacterToNumbersDoesNotOperateOnSurrogates()
		{
			string input;
			input = "\uD834\uDD1E";
			Assert.That(UnicodeCharacterEditingHelper.ConvertFinalCharacterToCodepoint(input), Is.EqualTo(input), "Don't operate on surrogates");
			input = "\U0001D11E";
			Assert.That(UnicodeCharacterEditingHelper.ConvertFinalCharacterToCodepoint(input), Is.EqualTo(input),"Don't operate on the resulting surrogates");
		}

		[Test]
		public void CharacterToNumbersIsNoopForEmptyOrNullInput()
		{
			string input = "";
			Assert.That(UnicodeCharacterEditingHelper.ConvertFinalCharacterToCodepoint(input), Is.EqualTo(input), "Don't change if empty string");
			input = null;
			Assert.That(UnicodeCharacterEditingHelper.ConvertFinalCharacterToCodepoint(input), Is.EqualTo(input), "Don't change if null");
		}

		[Test]
		public void ConvertEitherWay()
		{
			Assert.That(UnicodeCharacterEditingHelper.ConvertFinal("Some text here1234"), Is.EqualTo("Some text here\u1234"), "Should have detected final numbers and given a unicode character");
			Assert.That(UnicodeCharacterEditingHelper.ConvertFinal("Some text here!"), Is.EqualTo("Some text here0021"), "Should have detected final non-number character and given four hex digits representing it");
		}

		[Test]
		public void HandlerSetsIPCorrectlyWhenConvertToChar()
		{
			var keyEventArgs = new KeyEventArgs(Keys.Alt | Keys.X);
			using (var textBox = new TextBox())
			{
				textBox.Text = "Some 1234text here";
				textBox.SelectionStart = 9;

				UnicodeCharacterEditingHelper.HandleKeyDown(textBox, keyEventArgs);
				Assert.That(textBox.Text, Is.EqualTo("Some \u1234text here"),
					"The string should have been converted to include the unicode character");
				Assert.That(textBox.SelectionStart, Is.EqualTo(6),
					"SelectionStart should have been moved back");
			}
		}

		[Test]
		public void HandlerSetsIPCorrectlyWhenConvertToCharAtEnd()
		{
			var keyEventArgs = new KeyEventArgs(Keys.Alt | Keys.X);
			using (var textBox = new TextBox())
			{
				textBox.Text = "Some 1234";
				textBox.SelectionStart = 9;

				UnicodeCharacterEditingHelper.HandleKeyDown(textBox, keyEventArgs);
				Assert.That(textBox.Text, Is.EqualTo("Some \u1234"),
					"The string should have been converted to include the unicode character");
				Assert.That(textBox.SelectionStart, Is.EqualTo(6),
					"SelectionStart should have been moved back");
			}
		}

		[Test]
		public void HandlerSetsIPCorrectlyWhenConvertFromChar()
		{
			var keyEventArgs = new KeyEventArgs(Keys.Alt | Keys.X);
			using (var textBox = new TextBox())
			{
				textBox.Text = "Some \u1234text here";
				textBox.SelectionStart = 6;

				UnicodeCharacterEditingHelper.HandleKeyDown(textBox, keyEventArgs);
				Assert.That(textBox.Text, Is.EqualTo("Some 1234text here"),
					"The string should have been converted the character back to numbers");
				Assert.That(textBox.SelectionStart, Is.EqualTo(9),
					"SelectionStart should have been moved forward");
			}
		}

		[Test]
		public void HandlerSetsIPCorrectlyWhenConvertFromCharAtEnd()
		{
			var keyEventArgs = new KeyEventArgs(Keys.Alt | Keys.X);
			using (var textBox = new TextBox())
			{
				textBox.Text = "Some \u1234";
				textBox.SelectionStart = 6;

				UnicodeCharacterEditingHelper.HandleKeyDown(textBox, keyEventArgs);
				Assert.That(textBox.Text, Is.EqualTo("Some 1234"),
					"The string should have been converted the character back to numbers");
				Assert.That(textBox.SelectionStart, Is.EqualTo(9),
					"SelectionStart should have been moved forward");
			}
		}

		/// <summary>
		/// This test is ensuring 200F which is the Right to Left unicode character
		/// gets converted to [RLM] and the cursor is positioned in the correct place.
		/// </summary>
		[Test]
		public void ConvertRLM()
		{
			var keyEventArgs = new KeyEventArgs(Keys.Alt | Keys.X);
			using (var textBox = new TextBox())
			{
				textBox.Text = "Some 200Ftext here";
				textBox.TextChanged += SpecialCharacterHandling.RevealInvisibleCharacters;
				textBox.SelectionStart = 9;

				UnicodeCharacterEditingHelper.HandleKeyDown(textBox, keyEventArgs);
				Assert.That(textBox.Text, Is.EqualTo("Some∙[RLM]text∙here"),
					"The string should have been converted to 'Some∙[RLM]text∙here'");
				Assert.That(textBox.SelectionStart, Is.EqualTo(10),
					"SelectionStart should be positioned based on 200F converting to [RLM]");
			}
		}

		[Test]
		public void ConvertRLMAtEnd()
		{
			var keyEventArgs = new KeyEventArgs(Keys.Alt | Keys.X);
			using (var textBox = new TextBox())
			{
				textBox.Text = "Blah 200F";
				textBox.TextChanged += SpecialCharacterHandling.RevealInvisibleCharacters;
				textBox.SelectionStart = 9;

				UnicodeCharacterEditingHelper.HandleKeyDown(textBox, keyEventArgs);
				Assert.That(textBox.Text, Is.EqualTo("Blah∙[RLM]"),
					"The string should have been converted to include the character and then to visible characters");
				Assert.That(textBox.SelectionStart, Is.EqualTo(10),
					"SelectionStart should be positioned based on 200F ultimately converting to [RLM]");
			}
		}
	}
}
