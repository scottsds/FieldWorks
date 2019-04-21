/*-----------------------------------------------------------------------*//*:Ignore in Surveyor
Copyright (c) 1999-2013 SIL International
This software is licensed under the LGPL, version 2.1 or later
(http://www.gnu.org/licenses/lgpl-2.1.html)

File: TextServ.h
Responsibility: Jeff Gayle
Last reviewed: 8/25/99

	Declarations for all of TextServ.
-------------------------------------------------------------------------------*//*:End Ignore*/
#pragma once
#ifndef TextServ_H
#define TextServ_H 1

#if !defined(_WIN32) && !defined(_M_X64)
#include <pthread.h>
#endif


class TextServGlobals;

typedef Vector<TextServGlobals *> TsgVec; // Hungarian vtsg

/*----------------------------------------------------------------------------------------------
	Globals for the text services DLL. Note that these are thread local globals and are stored
	in thread local storage.
	Hungarian: tsg
----------------------------------------------------------------------------------------------*/
class TextServGlobals
{
public:
	// This holds common strings.
	TsStrHolder * m_ptsh;

	// This holds text properties.
	TsPropsHolder * m_ptph;

	// This manages the critical section for accessing the vector of allocated TSGs
#if defined(WIN32) || defined(WIN64)
	static CRITICAL_SECTION g_crs;
#else
	static pthread_mutex_t g_crs;
#endif //WIN32



	static TextServGlobals * GetTsGlobals(void);

	static void ActivateEmptyString(bool fActive);
	static void GetEmptyString(ITsString ** pptss);
	static void AddToTsgVec(TextServGlobals * ptsg);
	static void RemoveFromTsgVec(TextServGlobals * ptsg);

protected:
	friend class TextServEntry;

	static class TextServEntry s_tse;
#if defined(WIN32) || defined(WIN64)
	static ulong s_luTls;  // Thread local storage value.
#else
	static pthread_key_t s_luTls;  // Key for the thread-specific buffer
#endif //WIN32

	int m_cactActive;
	ITsStringPtr m_qtssEmpty;

	TextServGlobals(void)
	{
		Assert(!m_ptsh);
		Assert(!m_ptph);
	}

	~TextServGlobals(void)
	{
		if (m_ptsh)
		{
			delete m_ptsh;
			m_ptsh = NULL;
		}
		if (m_ptph)
		{
			delete m_ptph;
			m_ptph = NULL;
		}
	}

	static void ProcessAttach(void);
	static void ProcessDetach(void);
	static void ThreadDetach(void);
	static void ThreadAttach(void);
};

#endif // !TextServ_H
