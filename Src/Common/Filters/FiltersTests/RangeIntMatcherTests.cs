// Copyright (c) 2017 SIL International
// This software is licensed under the LGPL, version 2.1 or later
// (http://www.gnu.org/licenses/lgpl-2.1.html)
//

using NUnit.Framework;
using SIL.LCModel.Core.Text;

namespace SIL.FieldWorks.Filters
{
	/// ----------------------------------------------------------------------------------------
	/// <summary>
	/// Test the logic in the RangeIntMatcher.
	/// </summary>
	/// ----------------------------------------------------------------------------------------
	[TestFixture]
	public class RangeIntMatcherTests
	{
		public const int WsDummy = 987654321;

		/// <summary>
		/// Tests larger than int.MaxValue
		/// </summary>
		[Test]
		public void LongMaxValueTest()
		{
			RangeIntMatcher rangeIntMatch = new RangeIntMatcher(0, long.MaxValue);
			var tssLabel = TsStringUtils.MakeString("9223372036854775807", WsDummy);
			Assert.IsTrue(rangeIntMatch.Matches(tssLabel));
		}

		/// <summary>
		/// Tests RangeIntMatcher list behavior for ITsString
		/// </summary>
		[Test]
		public void MatchesIfOneInListMatches()
		{
			RangeIntMatcher rangeIntMatch = new RangeIntMatcher(2, 2);
			var tssLabel = TsStringUtils.MakeString("0 1 2", WsDummy);
			Assert.IsTrue(rangeIntMatch.Matches(tssLabel));
		}

		[Test]
		public void DoesNotMatchIfNoneInListMatch()
		{
			RangeIntMatcher rangeIntMatch = new RangeIntMatcher(3, 3);
			var tssLabel = TsStringUtils.MakeString("0 1 2", WsDummy);
			Assert.IsFalse(rangeIntMatch.Matches(tssLabel));
		}

		[Test]
		public void OutOfRangeDoesNotThrow()
		{
			RangeIntMatcher rangeIntMatch = new RangeIntMatcher(3, 3);
			var tssLabel = TsStringUtils.MakeString("999999999999999999999999", WsDummy);
			Assert.IsFalse(rangeIntMatch.Matches(tssLabel));
		}

		[Test]
		public void EmptyStringDoesNotThrow()
		{
			RangeIntMatcher rangeIntMatch = new RangeIntMatcher(3, 3);
			var tssLabel = TsStringUtils.EmptyString(WsDummy);
			Assert.IsFalse(rangeIntMatch.Matches(tssLabel));
		}
	}
}
