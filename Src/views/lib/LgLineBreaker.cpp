/*--------------------------------------------------------------------*//*:Ignore this sentence.
Copyright (c) 2002-2013 SIL International
This software is licensed under the LGPL, version 2.1 or later
(http://www.gnu.org/licenses/lgpl-2.1.html)
-------------------------------------------------------------------------------*//*:End Ignore*/

//:>********************************************************************************************
//:>	Include files
//:>********************************************************************************************
#include "Main.h"
#pragma hdrstop
// any other headers (not precompiled)
#include <limits.h>
#if defined(WIN32) || defined(WIN64)
#include <io.h>
#endif
#undef THIS_FILE
DEFINE_THIS_FILE

#if !defined(_WIN32) && !defined(_M_X64)
#include <sys/types.h>
#include <sys/stat.h>
#include <unistd.h>
#include <cstdio>
#include <iostream>
#include <fstream>
using namespace std;
#endif

//:>********************************************************************************************
//:>	Forward declarations
//:>********************************************************************************************

//:>********************************************************************************************
//:>	Local Constants and static variables, etc.
//:>********************************************************************************************

//This table maps from the return values of ICU's u_getIntPropertyValue((int),
//UCHAR_LINE_BREAK) to numbers consistent with John's original LgLBP enumeration.
//Used in GetLineBreakInfo.
static LgLBP g_rglbp[] = {
	klbpXX,
	klbpAI,
	klbpAL,
	klbpB2,
	klbpBA,
	klbpBB,
	klbpBK,
	klbpCB,
	klbpCL,
	klbpCM,
	klbpCR,
	klbpEX,
	klbpGL,
	klbpHY,
	klbpID,
	klbpIN,
	klbpIS,
	klbpLF,
	klbpNS,
	klbpNU,
	klbpOP,
	klbpPO,
	klbpPR,
	klbpQU,
	klbpSA,
	klbpSG,
	klbpSP,
	klbpSY,
	klbpZW
};  //hungarian lbp

//:>********************************************************************************************
//:>	Constructor/Destructor
//:>********************************************************************************************

LgLineBreaker::LgLineBreaker()
{
	m_cref = 1;
	ModuleEntry::ModuleAddRef();
	m_pLocale = NULL;
	m_pBrkit = NULL;
	StrUni stuUserWs(L"en");
	// We at least need to initialize the ICU data directory...
	CheckHr(Initialize(stuUserWs.Bstr()));
}

LgLineBreaker::LgLineBreaker(BSTR bstrLocale)
{
	m_cref = 1;
	ModuleEntry::ModuleAddRef();
	m_pLocale = NULL;
	m_pBrkit = NULL;
	CheckHr(Initialize(bstrLocale));
}

LgLineBreaker::~LgLineBreaker()
{
	ModuleEntry::ModuleRelease();
	if (m_pLocale)
	{
		delete m_pLocale;
		m_pLocale = NULL;
	}
	CleanupBreakIterator();
	// Be extra paranoid...
	if (m_pBrkit != NULL)
	{
		delete m_pBrkit;
		m_pBrkit = NULL;
	}
}

// Set up a break iterator if we don't currently have one.
void LgLineBreaker::SetupBreakIterator()
{
	if (m_pBrkit)
		return;
	// This is essential when calling from Python where we haven't yet initialized the ICU directory.
	StrUtil::InitIcuDataDir();
	//setting up our breakiterator as well so it will always be ready
	UErrorCode uerr = U_ZERO_ERROR;
	// Getting a BreakIterator locks icudt28l_uprops.icu until the BreakIterator memory is
	// freed and u_cleanup() is called. To allow it to be freed as needed we make a callback.
	m_pBrkit = BreakIterator::createLineInstance(*m_pLocale, uerr);
	if (!U_SUCCESS(uerr))
	{
		if (m_pBrkit)
			delete m_pBrkit;
		ThrowHr(E_FAIL, L"The ICU function to initialize the BreakIterator returned an error.");
	}
}

//:>********************************************************************************************
//:>	Generic factory stuff to allow creating an instance with CoCreateInstance.
//:>********************************************************************************************
static GenericFactory g_fact(
	_T("SIL.Language1.LgLineBreaker"),
	&CLSID_LgLineBreaker,
	_T("SIL line breaker"),
	_T("Apartment"),
	&LgLineBreaker::CreateCom);


void LgLineBreaker::CreateCom(IUnknown *punkCtl, REFIID riid, void ** ppv)
{
	AssertPtr(ppv);
	Assert(!*ppv);
	if (punkCtl)
		ThrowHr(WarnHr(CLASS_E_NOAGGREGATION));

	ComSmartPtr<LgLineBreaker> qzpropeng;

	qzpropeng.Attach(NewObj LgLineBreaker());		// ref count initially 1
	CheckHr(qzpropeng->QueryInterface(riid, ppv));
}

//:>********************************************************************************************
//:>	IUnknown Methods
//:>********************************************************************************************
STDMETHODIMP LgLineBreaker::QueryInterface(REFIID riid, void ** ppv)
{
	AssertPtr(ppv);
	if (!ppv)
		return WarnHr(E_POINTER);
	*ppv = NULL;

	if (riid == IID_IUnknown)
		*ppv = static_cast<IUnknown *>(static_cast<ILgLineBreaker *>(this));
	else if (riid == IID_ILgLineBreaker)
		*ppv = static_cast<ILgLineBreaker *>(this);
	else if (riid == IID_ISimpleInit)
		*ppv = static_cast<ISimpleInit *>(this);
	else if (riid == IID_ISupportErrorInfo)
	{
		*ppv = NewObj CSupportErrorInfo2(static_cast<ILgLineBreaker *>(this),
			IID_ISimpleInit, IID_ILgLineBreaker);
		return S_OK;
	}
	else
		return E_NOINTERFACE;

	reinterpret_cast<IUnknown *>(*ppv)->AddRef();
	return NOERROR;
}

//:>********************************************************************************************
//:>	ISimpleInit Methods
//:>********************************************************************************************

/*----------------------------------------------------------------------------------------------
	Initialize an instance. This method is supported to allow creating instances using
	ClassInitMoniker, which may become the standard way we initialize Language engines.
	However, this class does not actually need initialization.

	If you do want to create a moniker, you can do it like this:

	@code{
	IClassInitMonikerPtr qcim;
	hr = qcim.CreateInstance(CLSID_ClassInitMoniker);
	hr = qcim->InitNew(CLSID_LgSystemCollater, NULL, 0);
	}
----------------------------------------------------------------------------------------------*/
STDMETHODIMP LgLineBreaker::InitNew(const BYTE * prgb, int cb)
{
	BEGIN_COM_METHOD
	ChkComArrayArg(prgb, cb);
	if (cb != 0)
		ThrowInternalError(E_INVALIDARG, "Expected empty init string");

	END_COM_METHOD(g_fact, IID_ISimpleInit);
}

/*----------------------------------------------------------------------------------------------
	Return the initialization value previously set by InitNew.

	@param pbstr Pointer to a BSTR for returning the initialization data.
----------------------------------------------------------------------------------------------*/
STDMETHODIMP LgLineBreaker::get_InitializationData(BSTR * pbstr)
{
	BEGIN_COM_METHOD
	ChkComArgPtr (pbstr)
	*pbstr = NULL;
	return S_OK;
	END_COM_METHOD(g_fact, IID_ISimpleInit);
}

//:>********************************************************************************************
//:>	ILgLineBreaker Methods
//:>********************************************************************************************

/*----------------------------------------------------------------------------------------------
Initialize an instance with the proper Locale values.
----------------------------------------------------------------------------------------------*/
STDMETHODIMP LgLineBreaker::Initialize(BSTR bstrLocale)
{
	BEGIN_COM_METHOD
	StrAnsi staLocale(bstrLocale);
	Assert(staLocale.Length());

	if (m_pLocale != NULL)
	{
		delete m_pLocale;
		m_pLocale = NULL;
	}

	m_pLocale = new Locale(staLocale.Chars());

	SetupBreakIterator();

	END_COM_METHOD(g_fact, IID_ILgLineBreaker);
}

//:Ignore (no reason to have this table or the comments about it in the web page)
const byte LgLineBreaker::s_rglbs[32][32] = {
//    AI AL B2 BA BB BK CB CL CM CR EX GL HY ID IN IS LF NS NU OP PO PR QU SA SG SP SY XX ZW
/*AI*/{3, 3, 2, 3, 2, 4, 2, 1, 0, 4, 1, 3, 3, 2, 3, 3, 4, 3, 3, 2, 3, 2, 3, 3, 3, 0, 1, 3, 1},
/*AL*/{3, 3, 2, 3, 2, 4, 2, 1, 0, 4, 1, 3, 3, 2, 3, 3, 4, 3, 3, 2, 3, 2, 3, 3, 3, 0, 1, 3, 1},
/*B2*/{2, 2, 1, 3, 2, 4, 2, 1, 0, 4, 1, 3, 3, 2, 2, 3, 4, 3, 2, 2, 2, 2, 3, 2, 2, 0, 1, 2, 1},
/*BA*/{2, 2, 2, 3, 2, 4, 2, 1, 0, 4, 1, 3, 3, 2, 2, 3, 4, 3, 2, 2, 2, 2, 3, 2, 2, 0, 1, 2, 1},
/*BB*/{1, 1, 1, 3, 2, 4, 1, 1, 0, 4, 1, 1, 3, 1, 1, 1, 4, 3, 1, 1, 1, 1, 1, 1, 1, 0, 1, 1, 1},
/*BK*/{0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0},
/*CB*/{2, 2, 2, 3, 2, 4, 2, 1, 0, 4, 1, 3, 3, 2, 2, 3, 4, 3, 2, 2, 2, 2, 3, 2, 2, 0, 1, 2, 1},
/*CL*/{2, 2, 2, 3, 2, 4, 2, 1, 0, 4, 1, 3, 3, 2, 2, 3, 4, 3, 2, 2, 1, 2, 3, 2, 2, 0, 1, 2, 1},
/*CM*/{0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0},
/*CR*/{0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0},
/*EX*/{2, 2, 2, 3, 2, 4, 2, 1, 0, 4, 1, 3, 3, 2, 2, 3, 4, 3, 2, 2, 3, 2, 3, 2, 2, 0, 1, 2, 1},
/*GL*/{3, 3, 3, 3, 3, 4, 3, 1, 0, 4, 1, 3, 3, 3, 3, 3, 4, 3, 3, 3, 3, 3, 3, 3, 3, 0, 1, 3, 1},
/*HY*/{2, 2, 2, 2, 2, 4, 2, 1, 0, 4, 1, 3, 3, 2, 2, 3, 4, 2, 3, 2, 2, 2, 2, 2, 2, 0, 1, 2, 1},
/*ID*/{2, 2, 2, 3, 2, 4, 2, 1, 0, 4, 1, 3, 3, 2, 3, 3, 4, 3, 2, 2, 3, 2, 3, 2, 2, 0, 1, 2, 1},
/*IN*/{2, 2, 2, 3, 2, 4, 2, 1, 0, 4, 1, 3, 3, 2, 1, 3, 4, 3, 2, 2, 3, 2, 3, 2, 2, 0, 1, 2, 1},
/*IS*/{2, 2, 2, 3, 2, 4, 2, 1, 0, 4, 1, 3, 3, 2, 2, 3, 4, 3, 3, 2, 2, 2, 3, 2, 2, 0, 1, 2, 1},
/*LF*/{0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0},
/*NS*/{3, 3, 2, 3, 2, 4, 2, 1, 0, 4, 1, 3, 3, 2, 2, 3, 4, 3, 3, 2, 3, 2, 3, 3, 3, 0, 1, 3, 1},
/*NU*/{3, 3, 2, 3, 2, 4, 2, 1, 0, 4, 1, 3, 3, 2, 3, 3, 4, 3, 3, 2, 1, 2, 3, 3, 3, 0, 1, 3, 1},
/*OP*/{1, 1, 1, 1, 1, 4, 1, 1, 0, 4, 1, 1, 1, 1, 1, 1, 4, 1, 1, 1, 1, 1, 1, 1, 1, 0, 1, 1, 1},
/*PO*/{3, 3, 2, 3, 2, 4, 2, 1, 0, 4, 1, 3, 3, 2, 2, 3, 4, 3, 3, 2, 3, 2, 3, 3, 3, 0, 1, 3, 1},
/*PR*/{2, 2, 2, 3, 2, 4, 2, 1, 0, 4, 1, 3, 3, 3, 2, 3, 4, 3, 1, 1, 2, 2, 3, 2, 2, 0, 1, 2, 1},
/*QU*/{3, 3, 3, 3, 2, 4, 3, 1, 0, 4, 1, 3, 3, 3, 3, 3, 4, 3, 3, 1, 3, 3, 3, 3, 3, 0, 1, 3, 1},
/*SA*/{3, 3, 2, 3, 2, 4, 2, 1, 0, 4, 1, 3, 3, 2, 3, 3, 4, 3, 3, 2, 3, 2, 3, 3, 3, 0, 1, 3, 1},
/*SG*/{4, 4, 4, 4, 4, 4, 4, 4, 0, 4, 4, 4, 4, 4, 4, 4, 4, 4, 4, 4, 4, 4, 4, 4, 4, 0, 4, 4, 4},
/*SP*/{0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0},
/*SY*/{2, 2, 2, 3, 2, 4, 2, 1, 0, 4, 1, 3, 3, 2, 2, 3, 4, 3, 3, 2, 3, 2, 3, 2, 2, 0, 1, 2, 1},
/*XX*/{3, 3, 2, 3, 2, 4, 2, 1, 0, 4, 1, 3, 3, 2, 3, 3, 4, 3, 3, 2, 3, 2, 3, 3, 3, 0, 1, 3, 1},
/*ZW*/{2, 2, 2, 2, 2, 4, 2, 1, 0, 4, 1, 2, 2, 2, 2, 2, 4, 2, 2, 2, 2, 2, 2, 2, 2, 0, 1, 2, 1}
};
// Notes on the table:
//	1. Rows BK, CM, CR, LF and SP are not accessed by the line breaking algorithm.
//  2. Columns CM and SP are not accessed by the algorithm.
//  3. XX (character unassigned in Unicode) is represented in the table with the same
//     properties as AL. Also AI and SA. The default option of CB same as B2 has been chosen.
//  4. Table values: 0: invalid                     1: word break prohibited between pair
//                   2: break allowed between pair  3: break allowed when spaces separate pair
//                   4: word break and letter break both prohibited between pair
//  5. Rows are labelled according to the "Before" value, columns according to the "After"
//     value.
//  6. The ordering of the rows and columns of this table must be kept in line with the
//     definitions of klbpAI, etc in the LgLBP enum.
//  7. The size of the table (32x32) is chosen in the hope of greater access efficiency.
//:End Ignore

/*----------------------------------------------------------------------------------------------
	Get the line breaking properties for a range of characters.
	The output bytes have the following format:
	Lower 5 bits - actual line breaking property from table.
	High bit     - set if the general character property is "Zs" , i.e. space of some kind.
	This added bit is used later on to tell the renderer that characters so marked can be
	ignored if they are at the end of a line.
----------------------------------------------------------------------------------------------*/
STDMETHODIMP LgLineBreaker::GetLineBreakProps(const OLECHAR * prgchIn, int cchIn,
	byte * prglbpOut)
{
	BEGIN_COM_METHOD
	ChkComArrayArg(prgchIn, cchIn);
	ChkComArrayArg(prglbpOut, cchIn);

	int ich;
	int ch;
	for (ich = 0; ich < cchIn; ++ich)
	{
		ch = *prgchIn++;
		*prglbpOut = (byte)g_rglbp[u_getIntPropertyValue(ch, UCHAR_LINE_BREAK)];
		if (u_charType(ch) == U_SPACE_SEPARATOR)
			*prglbpOut |= 0x80;
		++prglbpOut;
	}

	END_COM_METHOD(g_fact, IID_ILgLineBreaker);
}

/*----------------------------------------------------------------------------------------------
	Get the line break status array for a subrange of characters. This is a shortcut for
	calling ${#GetLineBreakProps} followed by ${#GetLineBreakStatus} on the whole string.
	The logic of the method is roughly a combination of that of the other two.
----------------------------------------------------------------------------------------------*/
STDMETHODIMP LgLineBreaker::GetLineBreakInfo(const OLECHAR * prgchIn, int cchIn,
	int ichMin, int ichLim, byte * prglbsOut, int * pichBreak)
{
	BEGIN_COM_METHOD
	ChkComArrayArg(prgchIn,ichLim);
	ChkComArrayArg(prglbsOut, ichLim - ichMin);
	ChkComOutPtr(pichBreak);
	if (cchIn < ichLim)
		ThrowInternalError(E_INVALIDARG, "Range beyond end of string");
	if (ichLim < ichMin)
		ThrowInternalError(E_INVALIDARG, "Range limits out of order");
	*pichBreak = -1;	// This will be set to something else if we come to a "break/stop".
	if (ichLim == ichMin)
		return S_FALSE;

	// Intermediate array for line break properties of interest. Make it of size ichLim + 1
	// so that we can examine properties before the output range and (as long as cchIn > ichLim)
	// one position past the end of the range to obtain the status of the last element.
	byte rglbp[1000];
	Vector<byte> vlbp;
	byte * prglbpBuf = rglbp;	// Pointer to start of intermediate buffer.
	if (ichLim > 999)
	{
		vlbp.Resize(ichLim + 1);
		prglbpBuf = vlbp.Begin();
	}
	byte * prglbp = prglbpBuf;	// Initial value of pointer to intermediate buffer.

	// Count of characters in whose properties we are interested.
	int cb = ichLim;
	if (cchIn > ichLim)
		++cb;

	// Fill intermediate array of line break properties.
	int ich;
	int ch;
	// Trap this character and trigger a stop of the process when reached.
	const int kchTAB = 0x0009;
	for (ich = 0; ich < cb; ++ich)
	{
		ch = *prgchIn++;
		int lpbVal = u_getIntPropertyValue(ch, UCHAR_LINE_BREAK);
		if (lpbVal > klbpZW) // klbpZW is max enum < icu 2.6
		{
			// TODO-Linux FWNX-207: can't handle icu ULineBreak >= 2.6.
			lpbVal = klbpXX; // unknown line break
		}
		*prglbp = (byte)g_rglbp[lpbVal];
		if (u_charType(ch) == U_SPACE_SEPARATOR)
			*prglbp |= 0x80;
		if (ch == kchTAB)
			*prglbp |= 0x40;	// This bit causes process to be stopped if found.
		++prglbp;
	}
	byte * plbsOut;                   // pointer to current position in output array
	const byte * plbpLast = prglbpBuf + cb - 1; // pointer to last relevant element of lbp array
	int lbpCurrent;                // value of lpb currently considered as first of pair
	int lbpNext;                   // value of lbp currently considered as second of pair
	const byte klbsBS = kflbsBrk | kflbsSpace; // status is space with break allowed after
	const byte klbsN = 0;          // status is non-space with no break allowed after

	int i;
	int cbOut = ichLim - ichMin; // count of bytes to output
	for (i = 0; i < cbOut; ++i)
		*(prglbsOut + i) = kflbsBrkL;	// Initialise output array to kflbsBrkL; has the
										// effect of allowing only letter breaks as default.
	prglbp = prglbpBuf + ichMin;
	plbsOut = prglbsOut;

	// If there are no characters beyond ichLim, assume break is allowed after last character.
	// -- No, that is not a good default.
	if (cchIn == ichLim)
	{
//		*(plbsOut + cbOut - 1) = (*plbpLast & 0x80) ? klbsBS : (byte)kflbsBrk;
		if (*plbpLast & 0x80)
			*(plbsOut + cbOut - 1) = klbsBS;
		if (cbOut == 1)
			return S_OK; // If only one character, we have finished.
	}

	// Now search backwards to see if there is a line break property which is not SP or CM.
	// If there is, make this the value of lbpCurrent.
	i = ichMin;
	while (i >= 0)
	{
		lbpCurrent = *prglbp & 0x3f;	// Clear high bits if set.
		if (lbpCurrent != klbpSP && lbpCurrent != klbpCM)
			i = -2;
		else
		{
			--i;
			--prglbp;
		}
	}
	if (i != -2)
		// We didn't find a property at or before ichMin which was not SP or CM.
		lbpCurrent = klbpAL;  // Make as if a leading SP or CM follows an alphabetic character.

	byte lbs;
	// Last character has already been handled if necessary.
	for (prglbp = prglbpBuf + ichMin; prglbp < plbpLast; ++prglbp)
	{
		if (*pichBreak >= 0)
			break; // Exit from the loop if we found a reason to stop during the last iteration.
		if (*prglbp & 0x40)
		{
			// If current character is a TAB, set *pichBreak. This will stop the loop next time.
			// Note that we want the TAB to be processed "normally" for its line breaking
			// properties, so we continue through this iteration.
			*pichBreak = (int)(prglbp - prglbpBuf);
		}
		lbpNext = *(prglbp + 1) & 0x3f;	// Clear high bits if set.
		if (lbpCurrent == klbpBK || lbpCurrent == klbpLF)
		{
			// Mark hard breaks as break allowed and stop looking for more properties.
			*plbsOut++ = kflbsBrk;
			*pichBreak = (int)(prglbp - prglbpBuf);
			break;
		}
		if (lbpCurrent == klbpCR)
		{
			// Break allowed (actually mandatory) unless NL follows
			*plbsOut++ = (lbpNext == klbpLF) ? klbsN : (byte)kflbsBrk;
			if (lbpNext == klbpLF)
			{
				*pichBreak = (int)(prglbp - prglbpBuf);
				break;
			}
			// If next line begins with CM or SP we make as if they follow an AL.
			lbpCurrent = (lbpNext == klbpSP || lbpNext == klbpCM) ? klbpAL : lbpNext;
			continue;
		}
		if (lbpNext == klbpSP)
		{
			// prevent break before space; do not reset lbpCurrent
			*plbsOut++ = ((*prglbp & 0x7f) == klbpSP) ? (byte)kflbsSpace : klbsN;
			continue;
		}
		if (lbpNext == klbpCM)
		{
			// prevent letter break before CM; do not reset lbpCurrent
			*plbsOut++ = klbsN;	// (side effect of space before CM not marked as space)
			continue;
		}

		Assert(lbpCurrent >= 0 && lbpCurrent <= klbpZW);
		Assert(lbpNext >= 0 && lbpNext <= klbpZW);
		lbs = s_rglbs[lbpCurrent][lbpNext]; // look up break status in table
		Assert(lbs > 0 && lbs < 5);  // values other than 1 to 4 indicate an internal error

		if (lbs == 2)
			*plbsOut = kflbsBrk;
		if (lbs == 3 && (*prglbp & 0x3f) == klbpSP)
			*plbsOut = kflbsBrk;
		if (lbs == 4)
			*plbsOut = klbsN;
		if (*prglbp & 0x80)
			*plbsOut |= kflbsSpace;
		// Note that for some reason low surrogates (as well as high surrogates) are listed
		// with SG line breaking property in Unicode 3.0. This means that letter break is
		// prevented after a surrogate pair, not just after the high part of the pair. The
		// character following a surrogate pair could presumably be a space.
		// If none of these conditions is met, *pOut is left as intialised (kflbsBrkL)
		// which implies "not a space" and "letter break allowed after".

		lbpCurrent = lbpNext;
		++plbsOut;
	}
	END_COM_METHOD(g_fact, IID_ILgLineBreaker);
}

/*----------------------------------------------------------------------------------------------
	Sets the text for the LineBreakBefore and the LineBreakAfter functions to use.
----------------------------------------------------------------------------------------------*/
STDMETHODIMP LgLineBreaker::put_LineBreakText(OLECHAR * prgchIn, int cch)
{
	BEGIN_COM_METHOD;
	ChkComArgPtr(prgchIn);
	ChkComArrayArg(prgchIn, cch);

	Assert(m_pLocale);
	if (!m_pLocale)
		return E_UNEXPECTED;
	SetupBreakIterator(); //make sure we have one.

	m_cchBrkMax = cch;
	m_pBrkit->setText(m_usBrkIt.setTo(prgchIn, cch));

	END_COM_METHOD(g_fact, IID_ILgLineBreaker);
}

/*----------------------------------------------------------------------------------------------
	Gets the text that the LineBreakBefore and LineBreakAfter functions are using.  This
	function is included for completion.
----------------------------------------------------------------------------------------------*/
STDMETHODIMP LgLineBreaker::GetLineBreakText(int cchMax, OLECHAR * prgchOut,
	int * pcchOut)
{
	BEGIN_COM_METHOD;
	ChkComArrayArg(prgchOut, cchMax);
	ChkComOutPtr(pcchOut);

	Assert(m_pLocale);
	if (!m_pLocale)
		return E_UNEXPECTED;
	SetupBreakIterator(); //make sure we have one.

	UnicodeString ustrAns;

	const CharacterIterator & chIter = m_pBrkit->getText();
	const_cast<CharacterIterator &>(chIter).getText(ustrAns);

	if ((cchMax < ustrAns.length()) && (cchMax > 0))
		ThrowHr(E_FAIL, L"The buffer passed to this method was too small to hold the result.");

	*pcchOut = ustrAns.length();

	if (cchMax > 0)
	{
		for (int ch = 0; ch < ustrAns.length(); ch++)
			prgchOut[ch] = ustrAns[ch];
	}

	END_COM_METHOD(g_fact, IID_ILgLineBreaker);
}

/*----------------------------------------------------------------------------------------------
	Finds the nearest line break immediately before the given index (ichIn).  The function
	returns not only a location but the weight of the line break (currently not implemented).
	See http://www.unicode.org/unicode/reports/tr14/ for more information on line breaking
	properties.  The third parameter is not currently used.
----------------------------------------------------------------------------------------------*/
STDMETHODIMP LgLineBreaker::LineBreakBefore(int ichIn, int * pichOut,
	LgLineBreak * plbWeight)
{
	BEGIN_COM_METHOD;
	ChkComOutPtr(pichOut);
	ChkComArgPtr(plbWeight);

	Assert(m_pLocale);
	if (!m_pLocale)
		return E_UNEXPECTED;
	SetupBreakIterator(); //make sure we have one.

	if ((ichIn < 0) || (ichIn >= m_cchBrkMax))
		ThrowHr(E_FAIL, L"The line break asked for was out of range of the given text.");

	*pichOut = m_pBrkit->preceding(ichIn);

	END_COM_METHOD(g_fact, IID_ILgLineBreaker);
}

/*----------------------------------------------------------------------------------------------
	Finds the nearest line break immediately after the given index (ichIn).  The function
	returns not only a location but the weight of the line break (currently not implemented).
	See http://www.unicode.org/unicode/reports/tr14/ for more information on line breaking
	properties.  The third parameter is not currently used.
----------------------------------------------------------------------------------------------*/
STDMETHODIMP LgLineBreaker::LineBreakAfter(int ichIn, int * pichOut,
	LgLineBreak * plbWeight)
{
	BEGIN_COM_METHOD;
	ChkComArgPtr(pichOut);
	ChkComArgPtr(plbWeight);

	Assert(m_pLocale);
	if (!m_pLocale)
		return E_UNEXPECTED;
	SetupBreakIterator(); //make sure we have one.

	if ((ichIn < 0) || (ichIn >= m_cchBrkMax))
		ThrowHr(E_FAIL, L"The line break asked for was out of range of the given text.");

		*pichOut = m_pBrkit->following(ichIn);

	END_COM_METHOD(g_fact, IID_ILgLineBreaker);
}

//:>********************************************************************************************
//:>	Utility methods
//:>********************************************************************************************

/*----------------------------------------------------------------------------------------------
	Cleanup the callback object and the associated break iterator, if any.
----------------------------------------------------------------------------------------------*/
void LgLineBreaker::CleanupBreakIterator()
{
	Assert(m_pBrkit != NULL);
	delete m_pBrkit;
	m_pBrkit = NULL;
}
