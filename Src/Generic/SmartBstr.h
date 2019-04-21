/*--------------------------------------------------------------------*//*:Ignore this sentence.
Copyright (c) 1999-2013 SIL International
This software is licensed under the LGPL, version 2.1 or later
(http://www.gnu.org/licenses/lgpl-2.1.html)

File: SmartBstr.h
Responsibility: Shon Katzenberger
Last reviewed:

	This defines a smart BSTR class that handles freeing the string when destructed, etc.
----------------------------------------------------------------------------------------------*/
#pragma once
#ifndef SmartBstr_H
#define SmartBstr_H 1
//:End Ignore

/*----------------------------------------------------------------------------------------------
	This encapsulates a BSTR. It frees the BSTR in its destructor. It represents an error
	condition by setting m_bstr to (BSTR)1.
----------------------------------------------------------------------------------------------*/
class SmartBstr
{
public:
#ifdef DEBUG
	bool AssertValid(void) const
	{
#if defined(_M_X64) // Windows 64bit
		if ((ULONGLONG)m_bstr == (ULONGLONG)1)
			return true;
		Assert(!((ULONGLONG)m_bstr & 1));
#else
		if ((ulong)m_bstr == (ulong)1)
			return true;
		Assert(!((ulong)m_bstr & 1));
#endif // defined(_M_X64)
		AssertBstrN(m_bstr);
		return true;
	}
#endif // DEBUG

	~SmartBstr(void)
	{
		AssertObj(this);
		Clear();
	}

	void Clear(void)
	{
		AssertObj(this);
		if (!_Null())
			SysFreeString(m_bstr);
		m_bstr = NULL;
	}

	#ifdef DEBUG
#if defined(_WIN32) || defined(_M_X64)
	#define INIT_DBW() { m_dbw1.m_pbstr = this; }
#else
	#define INIT_DBW()
#endif
	#else
	#define INIT_DBW()
	#endif //DEBUG

	SmartBstr(void)
	{
		m_bstr = NULL;
		AssertObj(this);
		INIT_DBW();
	}

	SmartBstr(const SmartBstr & sbstr)
	{
		Assert(sbstr.AssertValid());
		m_bstr = NULL;
		Assign(sbstr);
		INIT_DBW();
	}

	SmartBstr(LPCOLESTR psz)
	{
		AssertPszN(psz);
		m_bstr = NULL;
		Assign(psz, StrLen(psz));
		INIT_DBW();
	}

	SmartBstr(const OLECHAR * prgch, int cch)
	{
		AssertArray(prgch, cch);
		m_bstr = NULL;
		Assign(prgch, cch);
		INIT_DBW();
	}

	SmartBstr & operator=(const SmartBstr & sbstr)
	{
		AssertObj(this);
		Assert(sbstr.AssertValid());
		Assign(sbstr);
		return *this;
	}

	SmartBstr & operator=(LPCOLESTR psz)
	{
		AssertObj(this);
		AssertPszN(psz);
		Assign(psz, StrLen(psz));
		return *this;
	}

	void Assign(const SmartBstr & sbstr)
	{
		AssertObj(this);
		Assert(sbstr.AssertValid());
		Assign(sbstr.m_bstr, sbstr.Length());
	}

	void Assign(LPCOLESTR psz)
	{
		AssertObj(this);
		AssertPszN(psz);
		Assign(psz, StrLen(psz));
	}

	/*------------------------------------------------------------------------------------------
		Allocates new memory for the indicated string. If the sbstr was previously holding
		another string, that memory is deallocated.
		WARNING!! This is not reference counted, so no other variables should be using the
		value that was previously stored in this sbstr.
		@param prgch Pointer to the 16-bit character array.
		@param cch The number of characters to store.
	------------------------------------------------------------------------------------------*/
	void Assign(const OLECHAR * prgch, int cch)
	{
		AssertObj(this);
		AssertArray(prgch, cch);
		if (!cch)
		{
			Clear();
			return;
		}
		BSTR bstr = ::SysAllocStringLen(prgch, cch);
		if (!bstr)
		{
			ThrowHr(WarnHr(E_OUTOFMEMORY));
		}
		if (!_Null())
			::SysFreeString(m_bstr);
		m_bstr = bstr;
	}

#if !defined(_WIN32) && !defined(_M_X64)
	SmartBstr(const wchar_t* psz)
	{
		AssertPszN(psz);
		m_bstr = NULL;
		Assign(psz, StrLen(psz));
		INIT_DBW();
	}

	SmartBstr(const wchar_t* prgch, int cch)
	{
		AssertArray(prgch, cch);
		m_bstr = NULL;
		Assign(prgch, cch);
		INIT_DBW();
	}

	SmartBstr& operator=(const wchar_t* psz)
	{
		AssertObj(this);
		AssertPszN(psz);
		Assign(psz, StrLen(psz));
		return *this;
	}

	void Assign(const wchar_t* psz)
	{
		AssertObj(this);
		AssertPszN(psz);
		Assign(psz, StrLen(psz));
	}

	void Assign(const wchar_t* prgch, int cch)
	{
		AssertObj(this);
		AssertArray(prgch, cch);
		if (!cch)
		{
			Clear();
			return;
		}
		BSTR bstr = ::SysAllocStringLen(0, cch);
		if (!bstr)
		{
			ThrowHr(WarnHr(E_OUTOFMEMORY));
		}
		if (!_Null())
			::SysFreeString(m_bstr);
		m_bstr = bstr;
		// Copy UTF-32 to UTF-16 - TODO: use ICU function that does it properly
		while (cch-- > 0)
			*bstr++ = *prgch++;
	}

	operator std::wstring()
	{
		// Copy UTF-16 to UTF-32 - TODO: use ICU function that does it properly
		return std::wstring(Chars(), Chars() + Length());
	}
#endif

	BSTR * operator&(void)
	{
		Clear();
		return &m_bstr;
	}

	operator BSTR(void)
	{
		AssertObj(this);
		return m_bstr;
	}

	BSTR Bstr(void)
	{
		AssertObj(this);
		return m_bstr;
	}

	// Pointer to the characters. This is read-only!
	const OLECHAR * Chars(void)
	{
		AssertObj(this);
		if (!m_bstr)
		{
			static OLECHAR empty[1] = {0};
			return empty;
		}
		return (OLECHAR *)m_bstr;
	}

	int Length(void) const
	{
		AssertObj(this);
		return _Null() ? 0 : ::SysStringLen(m_bstr);
	}

	int ByteLen(void) const
	{
		AssertObj(this);
		return _Null() ? 0 : ::SysStringByteLen(m_bstr);
	}

	void Attach(BSTR bstr)
	{
		AssertObj(this);
		AssertBstrN(bstr);
		if (bstr != m_bstr)
		{
			Clear();
			m_bstr = bstr;
		}
	}

	BSTR Detach(void)
	{
		AssertObj(this);
		BSTR bstr = m_bstr;
#if defined(_M_X64) // Windows 64bit
		if ((ULONGLONG)bstr == 1)
			bstr = NULL;
#else
		if ((ulong)bstr == 1)
			bstr = NULL;
#endif

		m_bstr = NULL;
		return bstr;
	}

	void Copy(BSTR * pbstr)
	{
		AssertObj(this);
		AssertPtr(pbstr);
		if (!m_bstr)
		{
			*pbstr = NULL;
			return;
		}
		*pbstr = ::SysAllocStringByteLen((char *)m_bstr, ByteLen());
		if (!*pbstr)
			ThrowHr(WarnHr(E_OUTOFMEMORY));
	}

	bool operator!() const
	{
		AssertObj(this);
		return _Null();
	}

	SmartBstr & operator+=(const SmartBstr & sbstr)
	{
		AssertObj(this);
		Assert(sbstr.AssertValid());
		Append(sbstr);
		return *this;
	}

	SmartBstr & operator+=(LPCOLESTR psz)
	{
		AssertObj(this);
		AssertPszN(psz);
		Append(psz);
		return *this;
	}

	void Append(const SmartBstr & sbstr)
	{
		AssertObj(this);
		Assert(sbstr.AssertValid());
		if (!sbstr.m_bstr)
			return;
		if (!m_bstr)
		{
			Assign(sbstr.m_bstr, sbstr.Length());
			return;
		}
		AppendCore(sbstr.m_bstr, sbstr.Length());
	}

	void Append(LPCOLESTR psz)
	{
		AssertObj(this);
		AssertPszN(psz);
		int cch = StrLen(psz);
		if (!cch)
			return;
		AppendCore(psz, cch);
	}

	void Append(const OLECHAR * prgch, int cch)
	{
		AssertObj(this);
		AssertArray(prgch, cch);
		if (!cch)
			return;
		AppendCore(prgch, cch);
	}

	void AppendBstr(BSTR bstr)
	{
		AssertObj(this);
		AssertBstrN(bstr);
		int cch = BstrLen(bstr);
		if (!cch)
			return;
		AppendCore(bstr, cch);
	}

	bool Equals(const SmartBstr & sbstr) const
	{
		AssertObj(this);
		Assert(sbstr.AssertValid());
		int cch = Length();
		if (cch != sbstr.Length())
			return false;
		if (!cch)
			return true;
		return memcmp(m_bstr, sbstr.m_bstr, cch * isizeof(OLECHAR)) == 0;
	}

	bool Equals(LPCOLESTR psz) const
	{
		AssertObj(this);
		AssertPszN(psz);
		return Equals(psz, StrLen(psz));
	}

	bool Equals(const OLECHAR * prgch, int cch) const
	{
		AssertObj(this);
		AssertArray(prgch, cch);
		if (cch != Length())
			return false;
		if (!cch)
			return true;
		return memcmp(m_bstr, prgch, cch * isizeof(OLECHAR)) == 0;
	}

	bool operator==(const SmartBstr & sbstr) const
	{
		return Equals(sbstr);
	}

	bool operator==(LPCOLESTR psz) const
	{
		AssertPszN(psz);
		return Equals(psz, StrLen(psz));
	}

	bool operator!=(const SmartBstr & sbstr) const
	{
		return !Equals(sbstr);
	}

	bool operator!=(LPCOLESTR psz) const
	{
		AssertPszN(psz);
		return !Equals(psz, StrLen(psz));
	}

	// Case insensitive compare.
	bool EqualsCI(const SmartBstr & sbstr) const
	{
		AssertObj(this);
		Assert(sbstr.AssertValid());
		int cch = Length();
		if (cch != sbstr.Length())
			return false;
		if (!cch)
			return true;
		return EqualsRgchCI(m_bstr, sbstr.m_bstr, cch);
	}
	bool EqualsCI(LPCOLESTR psz)
	{
		AssertPszN(psz);
		return EqualsCI(psz, StrLen(psz));
	}
	bool EqualsCI(const OLECHAR * prgch, int cch)
	{
		AssertObj(this);
		AssertArray(prgch, cch);
		if (cch != Length())
			return false;
		if (!cch)
			return true;
		return EqualsRgchCI(m_bstr, prgch, cch);
	}

protected:
	BSTR m_bstr;

#if defined(_WIN32) || defined(_M_X64)
#ifdef DEBUG
	class Dbw1 : public DebugWatch
	{
		virtual OLECHAR * Watch()
		{
			Output ("#%d sbstr %d: \"", ++m_nSerial, m_pbstr->Length());
			for (int i=0; i<m_pbstr->Length(); i++)
				Output ("%lc", (wchar)m_pbstr->Chars()[i]);
			Output ("\"\n");
			static wchar * msg	= L"See Debugger Output window";
			return msg;
		}
	public:
		SmartBstr * m_pbstr;
	};
	Dbw1 m_dbw1;
#endif //DEBUG
#endif

	bool _Null(void) const
	{
#if defined(_M_X64) // Windows 64bit
		return (ULONGLONG)m_bstr <= (ULONGLONG)1;
#else
		return (ulong)m_bstr <= (ulong)1;
#endif
	}

	void AppendCore(const OLECHAR * prgch, int cch)
	{
		AssertArray(prgch, cch);
		Assert(cch > 0);

		int cchCur = Length();
		BSTR bstr = ::SysAllocStringLen(NULL, cchCur + cch);
		if (!bstr)
		{
			ThrowHr(WarnHr(E_OUTOFMEMORY));
		}
		CopyItems(m_bstr, bstr, cchCur);
		CopyItems(prgch, bstr + cchCur, cch);
		bstr[cchCur + cch] = 0;
		::SysFreeString(m_bstr);
		m_bstr = bstr;
		return;
	}
};


/*----------------------------------------------------------------------------------------------
	Allows comparing a string to a smart bstr.
----------------------------------------------------------------------------------------------*/
inline bool operator==(LPCOLESTR psz, const SmartBstr & sbstr)
{
	return sbstr == psz;
}


/*----------------------------------------------------------------------------------------------
	Allows comparing a string to a smart bstr.
----------------------------------------------------------------------------------------------*/
inline bool operator!=(LPCOLESTR psz, const SmartBstr & sbstr)
{
	return !(sbstr == psz);
}


#endif // !SmartBstr_H
