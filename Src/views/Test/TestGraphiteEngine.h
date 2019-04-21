/*--------------------------------------------------------------------*//*:Ignore this sentence.
Copyright (c) 2003-2013 SIL International
This software is licensed under the LGPL, version 2.1 or later
(http://www.gnu.org/licenses/lgpl-2.1.html)

File: TestGraphiteEngine.h
Responsibility:
Last reviewed:

	Unit tests for the GraphiteEngine class.
-------------------------------------------------------------------------------*//*:End Ignore*/
#ifndef TESTGRAPHITEENGINE_H_INCLUDED
#define TESTGRAPHITEENGINE_H_INCLUDED

#pragma once

#include "testViews.h"
#include "RenderEngineTestBase.h"

namespace TestViews
{
	/*******************************************************************************************
		Tests for TestGraphiteEngine
	 ******************************************************************************************/
	class TestGraphiteEngine : public RenderEngineTestBase, public unitpp::suite
	{
	public:
		void testNullArgs()
		{
			RenderEngineTestBase::VerifyNullArgs();
		}

		void testBreakPointing()
		{
			RenderEngineTestBase::VerifyBreakPointing();
		}

		virtual IRenderEnginePtr GetRenderer(LgCharRenderProps*)
		{
			return m_qre;
		}

		TestGraphiteEngine();
		virtual void Setup()
		{
			RenderEngineTestBase::Setup();
			ILgWritingSystemPtr qws;
			SmartBstr sbstr;

			SmartBstr fontStr(L"Charis SIL");
			g_qwsf->get_EngineOrNull(g_wsEng, &qws);
			MockLgWritingSystem* mws = dynamic_cast<MockLgWritingSystem*>(qws.Ptr());
			mws->put_DefaultFontName(fontStr);

			g_qwsf->get_EngineOrNull(g_wsFrn, &qws);
			mws = dynamic_cast<MockLgWritingSystem*>(qws.Ptr());
			mws->put_DefaultFontName(fontStr);

			g_qwsf->get_EngineOrNull(g_wsGer, &qws);
			mws = dynamic_cast<MockLgWritingSystem*>(qws.Ptr());
			mws->put_DefaultFontName(fontStr);

			HDC hdc;
#if defined(WIN32) || defined(_M_X64)
			int dxMax = 600;
			hdc = ::CreateCompatibleDC(::GetDC(::GetDesktopWindow()));
			HBITMAP hbm = ::CreateCompatibleBitmap(hdc, dxMax, dxMax);
			::SelectObject(hdc, hbm);
			::SetMapMode(hdc, MM_TEXT);
#else
			hdc = 0;
#endif
			IVwGraphicsWin32Ptr qvg;
			qvg.CreateInstance(CLSID_VwGraphicsWin32);
			qvg->Initialize(hdc);
			LgCharRenderProps chrp;
			chrp.dympHeight = 0;
			wcscpy_s(chrp.szFaceName, 32, StrUni(L"Charis SIL").Chars());
			qvg->SetupGraphics(&chrp);

			m_qre.CreateInstance(CLSID_GraphiteEngine);
			m_qre->InitRenderer(qvg, NULL);
			m_qre->putref_WritingSystemFactory(g_qwsf);
			m_qre->putref_RenderEngineFactory(m_qref);

			qvg.Clear();
#if defined(WIN32) || defined(_M_X64)
			::DeleteObject(hbm);
			::DeleteDC(hdc);
#endif
		}
		virtual void Teardown()
		{
			m_qre.Clear();
			RenderEngineTestBase::Teardown();
		}

	};
}

#endif /*TESTGRAPHITEENGINE_H_INCLUDED*/

// Local Variables:
// mode:C++
// compile-command:"cmd.exe /e:4096 /c c:\\FW\\Bin\\mklg-tst.bat DONTRUN"
// End: (These 4 lines are useful to Steve McConnel.)
