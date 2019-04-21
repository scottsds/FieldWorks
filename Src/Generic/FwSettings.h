/*-----------------------------------------------------------------------*//*:Ignore in Surveyor
Copyright (c) 2006-2013 SIL International
This software is licensed under the LGPL, version 2.1 or later
(http://www.gnu.org/licenses/lgpl-2.1.html)

File: FwSettings.h
Responsibility: Steve McConnel
Last reviewed:

Description:
	This file contains Utility methods. FwSettings used to be in AppCore.
	It contains class declarations for the following classes and structs:
		class FwSettings
-------------------------------------------------------------------------------*//*:End Ignore*/
#pragma once
#ifndef FWSETTINGS_H_INCLUDED
#define FWSETTINGS_H_INCLUDED 1

/*----------------------------------------------------------------------------------------------
	This class is used to persist application settings.  Currently it stores stuff in the
	registry.  SetRoot must be called before any other methods are called.  Usually it should
	be called in the constructor of the application.

	@h3{Hungarian: fws}
----------------------------------------------------------------------------------------------*/
class FwSettings
{
public:
	void SetRoot(const achar * pszRoot);
	StrApp GetRoot();

	bool GetString(const achar * pszSubKey, const achar * pszValue, StrApp & str);
	bool SetString(const achar * pszSubKey, const achar * pszValue, StrApp & str);

	bool GetDword(const achar * pszSubKey, const achar * pszValue, DWORD * pdwT);

	bool SetDword(const achar * pszSubKey, const achar * pszValue, DWORD dwT);

	bool GetBinary(const achar * pszSubKey, const achar * pszValue, BYTE * pv, DWORD cb);
	bool SetBinary(const achar * pszSubKey, const achar * pszValue, BYTE * pv, DWORD cb);

	static bool SetString(const achar * pszRootKey, const achar * pszSubKey, const achar * pszValue,
		StrApp & str);
	static bool GetString(const achar * pszRootKey, const achar * pszSubKey, const achar * pszValue,
		StrApp & str);
	static bool SetDword(const achar * pszRootKey, const achar * pszSubKey, const achar * pszValue,
		DWORD dwT);
	static bool GetDword(const achar * pszRootKey, const achar * pszSubKey, const achar * pszValue,
		DWORD * pdwT);
	static bool SetBool(const achar * pszRootKey, const achar * pszSubKey, const achar * pszValue,
		bool fValue);
	static bool GetBool(const achar * pszRootKey, const achar * pszSubKey, const achar * pszValue,
		bool * pfValue);

#if defined(_WIN32) || defined(_M_X64)
	typedef enum
	{
		katRead = KEY_READ,
		katWrite = KEY_WRITE,
		katBoth = katRead | katWrite,
	} AccessType;
#endif //WIN32

	void RemoveAll();

protected:
	StrApp m_strRoot;	// Root of the key name in the registry for the application.

	static bool OpenKey(const achar * pszRootKey, const achar * pszSubKey, int at, HKEY * phkey);
	bool OpenKey(const achar * pszSubKey, int at, HKEY * phkey);
};

#endif
