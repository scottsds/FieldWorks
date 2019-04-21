/*--------------------------------------------------------------------*//*:Ignore this sentence.
Copyright (c) 1999-2013 SIL International
This software is licensed under the LGPL, version 2.1 or later
(http://www.gnu.org/licenses/lgpl-2.1.html)

File: UtilRegistry.h
Responsibility:
Last reviewed:

Description:
	Provides Registry related utilities.
-------------------------------------------------------------------------------*//*:End Ignore*/
#pragma once
#ifndef UTILREGISTRY_H
#define UTILREGISTRY_H 1


#if defined(_WIN32) || defined(_M_X64)
int DeleteSubKey(HKEY hk, const achar *psz);


/*************************************************************************************
	A class wrapper for a registry key. The destructor closes the key.
*************************************************************************************/
class RegKey
{
protected:
	HKEY m_hkey;

public:
	RegKey(void) {
		m_hkey = NULL;
	}
	~RegKey(void) {
		Close();
	}
	RegKey(HKEY hkey)
	{
		m_hkey = hkey;
	}
	// JohnT: this might be handy but I haven't tested it yet.
 // 	RegKey(HKEY hKey, LPCTSTR lpSubKey, DWORD ulOptions, REGSAM samDesired, PHKEY phkResult)
	//{
	//	if (!ERROR_SUCCESS == ::RegOpenKeyEx(hKey, lpSubKey, ulOptions, samDesired, &m_hkey))
	//		m_hkey = NULL;
	//}

	// Initialize the RegKey (after closing any old one) with the specified path on the
	// local machine. Uses common defaults for other parameters and returns true if
	// successful.
	bool InitLm(achar * pszPath)
	{
		Close();
		return ::RegOpenKeyEx(HKEY_LOCAL_MACHINE, pszPath, 0, KEY_QUERY_VALUE, &m_hkey)
			== ERROR_SUCCESS;
	}

	// Initialize the RegKey (after closing any old one) with the specified path on the
	// current user. Uses common defaults for other parameters and returns true if
	// successful.
	bool InitCu(achar * pszPath)
	{
		Close();
		return ::RegOpenKeyEx(HKEY_CURRENT_USER, pszPath, 0, KEY_QUERY_VALUE, &m_hkey)
			== ERROR_SUCCESS;
	}

	// Cast operator.
	operator HKEY (void) {
		return m_hkey;
	}

	// Address of operator.
	HKEY * operator&(void) {
		Close();
		return &m_hkey;
	}

	void Close(void) {
		if (m_hkey) {
			RegCloseKey(m_hkey);
			m_hkey = NULL;
		}
	}
};
#endif//WIN32

/*************************************************************************************
	A class containing static methods for finding the important FieldWorks
	directories.
*************************************************************************************/
class DirectoryFinder
{
public:
	static StrUni FwRootDataDir();
	static StrUni FwRootCodeDir();
	static StrUni FwTemplateDir();
};

#endif //!UTILREGISTRY_H
