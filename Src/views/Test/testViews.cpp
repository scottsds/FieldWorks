/*--------------------------------------------------------------------*//*:Ignore this sentence.
Copyright (c) 2003-2013 SIL International
This software is licensed under the LGPL, version 2.1 or later
(http://www.gnu.org/licenses/lgpl-2.1.html)

File: testViews.cpp
Responsibility:
Last reviewed:

	Global initialization/cleanup for unit testing the Views DLL classes.
-------------------------------------------------------------------------------*//*:End Ignore*/
#if !defined(WIN32) && !defined(_M_X64)
#define INITGUID
#endif
#include "testViews.h"
#include "RedirectHKCU.h"
#if !defined(WIN32) && !defined(_M_X64)
// These define GUIDs that we need to define globally somewhere
#include "TestVwTxtSrc.h"
#include "TestLayoutPage.h"
#endif

namespace unitpp
{
	void GlobalSetup(bool verbose)
	{
#if defined(WIN32) || defined(_M_X64)
		ModuleEntry::DllMain(0, DLL_PROCESS_ATTACH);
#endif
		::OleInitialize(NULL);
		RedirectRegistry();
		StrUtil::InitIcuDataDir();

	}
	void GlobalTeardown()
	{
		::OleUninitialize();
#if defined(WIN32) || defined(_M_X64)
		ModuleEntry::DllMain(0, DLL_PROCESS_DETACH);
#endif
	}
}

namespace TestViews
{
	ILgWritingSystemFactoryPtr g_qwsf;
	int g_wsEng = 0;
	int g_wsFrn = 0;
	int g_wsGer = 0;
	int g_wsTest = 0;
	int g_wsTest2 = 0;

	// Create a dummy writing system factory with English and French.
	void CreateTestWritingSystemFactory()
	{
		StrUni stuWs;
		ILgWritingSystemPtr qws;
		g_qwsf.Attach(NewObj MockLgWritingSystemFactory);

		// Add a writing system for English.
		stuWs.Assign(L"en");
		CheckHr(g_qwsf->get_Engine(stuWs.Bstr(), &qws));
		MockLgWritingSystem* mws = dynamic_cast<MockLgWritingSystem*>(qws.Ptr());
		StrUni stuTimesNewRoman(L"Times New Roman");
		mws->put_DefaultFontName(stuTimesNewRoman.Bstr());
		CheckHr(qws->get_Handle(&g_wsEng));
		CheckHr(g_qwsf->put_UserWs(g_wsEng));

		// Add a writing system for French.
		stuWs.Assign(L"fr");
		CheckHr(g_qwsf->get_Engine(stuWs.Bstr(), &qws));
		mws = dynamic_cast<MockLgWritingSystem*>(qws.Ptr());
		mws->put_DefaultFontName(stuTimesNewRoman.Bstr());
		CheckHr(qws->get_Handle(&g_wsFrn));

		// Add a writing system for German.
		stuWs.Assign(L"de");
		CheckHr(g_qwsf->get_Engine(stuWs.Bstr(), &qws));
		mws = dynamic_cast<MockLgWritingSystem*>(qws.Ptr());
		mws->put_DefaultFontName(stuTimesNewRoman.Bstr());
		CheckHr(qws->get_Handle(&g_wsGer));
	}

	// Free the dummy writing system factory.
	void CloseTestWritingSystemFactory()
	{
		if (g_qwsf)
		{
			g_qwsf.Clear();
		}
	}
}

// Local Variables:
// mode:C++
// compile-command:"cmd.exe /e:4096 /c c:\\FW\\Bin\\mkvw-tst.bat DONTRUN"
// End: (These 4 lines are useful to Steve McConnel.)
