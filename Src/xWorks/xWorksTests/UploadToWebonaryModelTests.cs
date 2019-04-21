﻿// Copyright (c) 2014 SIL International
// This software is licensed under the LGPL, version 2.1 or later
// (http://www.gnu.org/licenses/lgpl-2.1.html)

using NUnit.Framework;

namespace SIL.FieldWorks.XWorks
{
	[TestFixture]
	class UploadToWebonaryModelTests
	{
		[Test]
		public void EncryptPassword_NullAndEmptyDoNotCrash()
		{
			Assert.DoesNotThrow(() => UploadToWebonaryModel.EncryptPassword(null));
			Assert.DoesNotThrow(() => UploadToWebonaryModel.EncryptPassword(string.Empty));
		}

		[Test]
		public void DecryptPassword_NullAndEmptyDoNotCrash()
		{
			Assert.DoesNotThrow(() => UploadToWebonaryModel.DecryptPassword(null));
			Assert.DoesNotThrow(() => UploadToWebonaryModel.DecryptPassword(string.Empty));
		}
	}
}
