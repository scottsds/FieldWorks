/*--------------------------------------------------------------------*//*:Ignore this sentence.
Copyright (c) 1999-2013 SIL International
This software is licensed under the LGPL, version 2.1 or later
(http://www.gnu.org/licenses/lgpl-2.1.html)

File: Debug.cpp
Responsibility: Darrell Zook
Last reviewed: Not yet.

Description:
	This file provides the standard debug error functions.
-------------------------------------------------------------------------------*//*:End Ignore*/

#include "main.h"
#pragma hdrstop
#undef THIS_FILE
DEFINE_THIS_FILE

#ifdef DEBUG

void WINAPI AssertProcLocal(const char * pszExp, const char * pszFile, int nLine,
	bool fCritical)
{
	AssertProc(pszExp, pszFile, nLine, fCritical, ModuleEntry::GetModuleHandle());
}

void WINAPI WarnProcLocal(const char * pszExp, const char * pszFile, int nLine,
	bool fCritical)
{
	WarnProc(pszExp, pszFile, nLine, fCritical, ModuleEntry::GetModuleHandle());
}

void WINAPI WarnHrProc(HRESULT hr, const char * pszFile, int nLine, bool fCritical)
{
#if defined(_WIN32) || defined(_M_X64)
	char szBuffer[MAX_PATH + 25];
	sprintf_s(szBuffer, sizeof(szBuffer), "HRESULT[0x%08x]--", hr);
	int cch = (int)strlen(szBuffer);
	FormatMessageA(FORMAT_MESSAGE_FROM_SYSTEM, NULL, hr, 0, szBuffer + cch,
		sizeof(szBuffer) - cch, NULL);
	szBuffer[max(strlen(szBuffer) - 2, 0)] = 0;
	WarnProcLocal(szBuffer, pszFile, nLine, fCritical);
#endif
}

#if defined(_WIN32) || defined(_M_X64)
OLECHAR * DebugWatch::WatchNV()
{
	// JT: we need this, otherwise if dealing with a deleted object or null pointer
	// we may try to call a virtual function where there is no valid VTABLE pointer.
	// In the debugger we can't be sure of not dereferencing a bad pointer.
	if (!::_CrtIsValidPointer(this, isizeof(this), TRUE ))
		return L"A very bad object pointer";
	// We could also be referencing memory that is trashed (e.g., the object has
	// been deleted, or deleted and replaced by something else).
	if (!dynamic_cast<DebugWatch *>(this))
		return L"A bad object pointer";
	return Watch();
}

void DebugWatch::Output (char *fmt, ...) // LIMITED to 10 pointer-sized arguments
{
	struct args { void * pv[10]; };
	_CrtDbgReport (_CRT_WARN, NULL, NULL, __FILE__, fmt, *(args*)(&fmt+1));
}
#endif

#endif // DEBUG
