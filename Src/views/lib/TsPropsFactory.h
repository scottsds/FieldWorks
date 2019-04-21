/*-----------------------------------------------------------------------*//*:Ignore in Surveyor
Copyright (c) 1999-2013 SIL International
This software is licensed under the LGPL, version 2.1 or later
(http://www.gnu.org/licenses/lgpl-2.1.html)

File: TsPropsFactory.h
Responsibility: Jeff Gayle
Last reviewed:

	Implementation of ITsPropsFactory.
-------------------------------------------------------------------------------*//*:End Ignore*/
#pragma once
#ifndef TsPropsFactory_H
#define TsPropsFactory_H 1


/*----------------------------------------------------------------------------------------------
	Implements ITsPropsFactory.
	Hungarian: tpf / ztpf.
----------------------------------------------------------------------------------------------*/
class TsPropsFact : public ITsPropsFactory
{
public:
	static void CreateCom(IUnknown * punkOuter, REFIID iid, void ** ppv);

	// IUnknown methods.
	STDMETHOD(QueryInterface)(REFIID iid, void ** ppv);
	STDMETHOD_(UCOMINT32, AddRef)(void);
	STDMETHOD_(UCOMINT32, Release)(void);

	// ITsPropsFactory methods.
	STDMETHOD(MakeProps)(BSTR bstrStyle, int ws, int ows, ITsTextProps ** ppttp);
	STDMETHOD(MakePropsRgch)(const OLECHAR * prgchStyle, int cch, int ws, int ows,
		ITsTextProps ** ppttp);

	STDMETHOD(GetPropsBldr)(ITsPropsBldr ** pptpb);

private:
	static TsPropsFact g_tpf;

#if defined(WIN32) || defined(WIN64)
	IUnknownPtr m_qunkMarshaler;
#endif

	TsPropsFact(void)
	{
		// Don't call ModuleAddRef since there is a global singleton TsPropsFact. Its
		// AddRef and Release call ModuleAddRef and ModuleRelease.
#if defined(WIN32) || defined(WIN64)
		CoCreateFreeThreadedMarshaler(this, &m_qunkMarshaler);
#endif
	}
};

#endif // !TsPropsFactory_H
