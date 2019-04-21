/*-----------------------------------------------------------------------*//*:Ignore in Surveyor
Copyright (c) 1999-2013 SIL International
This software is licensed under the LGPL, version 2.1 or later
(http://www.gnu.org/licenses/lgpl-2.1.html)

File: TextServ.cpp
Responsibility: Jeff Gayle
Last reviewed: 8/25/99

	Implementation of the TextServ TLS globals.
-------------------------------------------------------------------------------*//*:End Ignore*/
#include "../Main.h"
#pragma hdrstop

#include "Vector_i.cpp"
#if !defined(_WIN32) && !defined(_M_X64)
#include <pthread.h>
#endif

#undef THIS_FILE
DEFINE_THIS_FILE

/* Key for the thread-specific buffer */
#if defined(WIN32) || defined(WIN64)
ulong TextServGlobals::s_luTls;
#else
pthread_key_t TextServGlobals::s_luTls;
#endif //WIN32

/*----------------------------------------------------------------------------------------------
	Provides hooks into module level events for managing our thread local storage data.
	Hungarian: tse.
----------------------------------------------------------------------------------------------*/
class TextServEntry : public ModuleEntry
{
public:
	virtual void ProcessAttach(void)
	{
		TextServGlobals::ProcessAttach();
	}

	virtual void ProcessDetach(void)
	{
		TextServGlobals::ProcessDetach();
	}

	virtual void ThreadDetach(void)
	{
		TextServGlobals::ThreadDetach();
	}

	virtual void ThreadAttach(void)
	{
		TextServGlobals::ThreadAttach();
	}
};

TextServEntry g_tse;
#if defined(WIN32) || defined(WIN64)
CRITICAL_SECTION TextServGlobals::g_crs;
#else
pthread_mutex_t TextServGlobals::g_crs = PTHREAD_MUTEX_INITIALIZER;
#endif //WIN32


/*----------------------------------------------------------------------------------------------
	Allocate our TLS slot.
----------------------------------------------------------------------------------------------*/
void TextServGlobals::ProcessAttach(void)
{
#if defined(WIN32) || defined(WIN64)
	s_luTls = TlsAlloc();
	if (0xFFFFFFFF == s_luTls)
		ThrowHr(WarnHr(E_FAIL));
#else
	if (pthread_key_create(&s_luTls, 0) != 0)
		ThrowHr(WarnHr(E_FAIL));
#endif //WIN32
#if defined(WIN32) || defined(WIN64)
	InitializeCriticalSection(&g_crs);
#else
	pthread_mutex_init(&g_crs, 0);
#endif //WIN32
}


/*----------------------------------------------------------------------------------------------
	Free our TLS slot, and all the allocated memory. Note that this may not be called in
	the same thread as ProcessAttach.
----------------------------------------------------------------------------------------------*/
void TextServGlobals::ProcessDetach(void)
{
#if defined(WIN32) || defined(WIN64)
	EnterCriticalSection(&g_crs);
#else
	pthread_mutex_lock(&g_crs);
#endif //WIN32

	for (int itsg = 0; itsg < ViewsGlobals::g_vptsg->Size(); itsg++)
		delete (*ViewsGlobals::g_vptsg)[itsg];

#if defined(WIN32) || defined(WIN64)
	LeaveCriticalSection(&g_crs);
	// Assume other threads are gone...
	DeleteCriticalSection(&g_crs);
#else
	pthread_mutex_unlock(&g_crs);
	// Assume other threads are gone...
	pthread_mutex_destroy(&g_crs);
#endif

#if defined(WIN32) || defined(WIN64)
	TlsFree(s_luTls);
	s_luTls = 0xFFFFFFFF;
#else
	pthread_key_delete(s_luTls);
#endif
}

/*----------------------------------------------------------------------------------------------
	Note the thread is attached. Currently there is nothing to do.
	Note that we don't get this call for the thread that calls ProcessAttach.
----------------------------------------------------------------------------------------------*/
void TextServGlobals::ThreadAttach(void)
{
}


/*----------------------------------------------------------------------------------------------
	Free our thread local data. Note that we are not guaranteed this is called for every
	thread; it is called if a thread is explicitly killed, but not if the library is unloaded
	or the process ends.
----------------------------------------------------------------------------------------------*/
void TextServGlobals::ThreadDetach(void)
{
#if defined(WIN32) || defined(WIN64)
	TextServGlobals * ptsg = static_cast<TextServGlobals *>(TlsGetValue(s_luTls));
#else
	TextServGlobals * ptsg = static_cast<TextServGlobals *>(pthread_getspecific(s_luTls));
#endif //WIN32
	AssertPtrN(ptsg);
	if (ptsg)
	{
		delete ptsg;
#if defined(WIN32) || defined(WIN64)
		TlsSetValue(s_luTls, NULL);
#else
		pthread_setspecific(s_luTls, 0);
#endif //WIN32
		RemoveFromTsgVec(ptsg);
	}
}

/*----------------------------------------------------------------------------------------------
	Add to the vector of blocks to free if we get a ProcessDetach.
----------------------------------------------------------------------------------------------*/
void TextServGlobals::AddToTsgVec(TextServGlobals * ptsg)
{
#if defined(WIN32) || defined(WIN64)
	EnterCriticalSection(&g_crs);
#else
	pthread_mutex_lock(&g_crs);
#endif //WIN32
	ViewsGlobals::g_vptsg->Push(ptsg);
#if defined(WIN32) || defined(WIN64)
	LeaveCriticalSection(&g_crs);
#else
	pthread_mutex_unlock(&g_crs);
#endif //WIN32
}
/*----------------------------------------------------------------------------------------------
	Free our thread local data.
----------------------------------------------------------------------------------------------*/
void TextServGlobals::RemoveFromTsgVec(TextServGlobals * ptsg)
{
#if defined(WIN32) || defined(WIN64)
	EnterCriticalSection(&g_crs);
#else
	pthread_mutex_lock(&g_crs);
#endif //WIN32

	for (int itsg = 0; itsg < ViewsGlobals::g_vptsg->Size(); itsg++)
	{
		if ((*ViewsGlobals::g_vptsg)[itsg] == ptsg)
		{
			ViewsGlobals::g_vptsg->Delete(itsg);
			break;
		}
	}
#if defined(WIN32) || defined(WIN64)
	LeaveCriticalSection(&g_crs);
#else
	pthread_mutex_unlock(&g_crs);
#endif //WIN32
}


/*----------------------------------------------------------------------------------------------
	Get our thread local data.
----------------------------------------------------------------------------------------------*/
TextServGlobals * TextServGlobals::GetTsGlobals(void)
{
#if !defined(_WIN32) && !defined(_M_X64)
	// Ensure ProcessAttach is called exactly once
	static pthread_once_t once_control = PTHREAD_ONCE_INIT;
	pthread_once(&once_control, ProcessAttach);
#endif

#if defined(WIN32) || defined(WIN64)
	TextServGlobals * ptsg = static_cast<TextServGlobals *>(TlsGetValue(s_luTls));
#else
	TextServGlobals * ptsg = static_cast<TextServGlobals *>(pthread_getspecific(s_luTls));
#endif //WIN32
	AssertPtrN(ptsg);

	if (!ptsg)
	{
		ptsg = NewObj TextServGlobals;
#if defined(WIN32) || defined(WIN64)
		TlsSetValue(s_luTls, ptsg);
#else
		pthread_setspecific(s_luTls, ptsg);
#endif //WIN32
		AddToTsgVec(ptsg);
	}

	return ptsg;
}


/*----------------------------------------------------------------------------------------------
	Increment or decrement the count of active clients of the empty string. When the count
	becomes zero, the string is freed.
----------------------------------------------------------------------------------------------*/
void TextServGlobals::ActivateEmptyString(bool fActive)
{
	TextServGlobals * ptsg = GetTsGlobals();

	if (fActive)
	{
		Assert(ptsg->m_cactActive >= 0);
		ptsg->m_cactActive++;
	}
	else
	{
		Assert(ptsg->m_cactActive > 0);
		if (ptsg->m_cactActive > 0 && !--ptsg->m_cactActive)
			ptsg->m_qtssEmpty.Clear();
	}
}


/*----------------------------------------------------------------------------------------------
	Get the empty string for this thread.
----------------------------------------------------------------------------------------------*/
void TextServGlobals::GetEmptyString(ITsString ** pptss)
{
	AssertPtr(pptss);
	Assert(!*pptss);

	TextServGlobals * ptsg = GetTsGlobals();

	if (!ptsg->m_qtssEmpty)
	{
		if (ptsg->m_cactActive <= 0)
		{
			Warn("Empty string requested with no active clients");
			TsStrSingle::Create((OLECHAR *)NULL, 0, (TsTextProps *)NULL, pptss);
		}
		TsStrSingle::Create((OLECHAR *)NULL, 0, (TsTextProps *)NULL, &ptsg->m_qtssEmpty);
	}

	*pptss = ptsg->m_qtssEmpty;
	AddRefObj(*pptss);
}

#include <Vector_i.cpp>
template class Vector<TextServGlobals *>; // TsgVec; // Hungarian vtsg
