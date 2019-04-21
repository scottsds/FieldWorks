/*--------------------------------------------------------------------*//*:Ignore this sentence.
Copyright (c) 1999-2013 SIL International
This software is licensed under the LGPL, version 2.1 or later
(http://www.gnu.org/licenses/lgpl-2.1.html)

File: VwPattern.h
Responsibility: John Thomson
Last reviewed: Not yet.

Description:
	1) Header for search patterns.
	2) Header for Search Operation abort mechanism
-------------------------------------------------------------------------------*//*:End Ignore*/
#pragma once
#ifndef VwPattern_INCLUDED
#define VwPattern_INCLUDED

#include "SmartBstr.h"

/*----------------------------------------------------------------------------------------------
This class implements a search pattern and the top level mechanisms to do the actual searching.
@h3{Hungarian: zpat}
----------------------------------------------------------------------------------------------*/
class VwPattern : public IVwPattern
{
	friend class VwLazyBox; // Can set relevant instance vars on successful find.
	friend class FindInAlgorithm; // used in method implementation.
	friend class FindInAlgorithmBase; // used in method implementation.
	friend class RegExFindInAlgorithm; // used in method implementation.
	friend class VwPatternIcuCleanupCallback; // allowed to clean up our pattern
public:
	// Static methods
	static void CreateCom(IUnknown *punkCtl, REFIID riid, void ** ppv);

	// Constructors/destructors/etc.
	VwPattern();
	virtual ~VwPattern();

	// IUnknown methods
	STDMETHOD(QueryInterface)(REFIID iid, void ** ppv);
	STDMETHOD_(UCOMINT32, AddRef)(void)
	{
		return InterlockedIncrement(&m_cref);
	}
	STDMETHOD_(UCOMINT32, Release)(void)
	{
		long cref = InterlockedDecrement(&m_cref);
		if (cref == 0)
		{
			m_cref = 1;
			delete this;
		}
		return cref;
	}

	// IVwPattern methods
	STDMETHOD(putref_Pattern)(ITsString * ptssPattern);
	STDMETHOD(get_Pattern)(ITsString ** pptssPattern);
	STDMETHOD(putref_Overlay)(IVwOverlay * pvo);
	STDMETHOD(get_Overlay)(IVwOverlay ** ppvo);
	STDMETHOD(put_MatchCase)(ComBool fMatch);
	STDMETHOD(get_MatchCase)(ComBool * pfMatch);
	STDMETHOD(put_MatchDiacritics)(ComBool fMatch);
	STDMETHOD(get_MatchDiacritics)(ComBool * pfMatch);
	STDMETHOD(put_MatchWholeWord)(ComBool fMatch);
	STDMETHOD(get_MatchWholeWord)(ComBool * pfMatch);
	STDMETHOD(put_MatchExactly)(ComBool fMatch);
	STDMETHOD(get_MatchExactly)(ComBool * pfMatch);
	STDMETHOD(put_UseRegularExpressions)(ComBool fMatch);
	STDMETHOD(get_UseRegularExpressions)(ComBool * pfMatch);
	STDMETHOD(put_MatchCompatibility)(ComBool fMatch);
	STDMETHOD(get_MatchCompatibility)(ComBool * pfMatch);
	STDMETHOD(put_MatchOldWritingSystem)(ComBool fMatch);
	STDMETHOD(get_MatchOldWritingSystem)(ComBool * pfMatch);
	STDMETHOD(Find)(IVwRootBox * prootb, ComBool fForward, IVwSearchKiller * pxserkl);
	STDMETHOD(FindFrom)(IVwSelection * psel, ComBool fForward, IVwSearchKiller * pxserkl);
	STDMETHOD(FindNext)(ComBool fForward, IVwSearchKiller * pxserkl);
	STDMETHOD(FindIn)(IVwTextSource * pts, int ichStart, int ichEnd, ComBool fForward,
		int * pichMinFound, int * pichLimFound, IVwSearchKiller * pxserkl);
	STDMETHOD(Install)();
	STDMETHOD(get_Found)(ComBool * pfFound);
	STDMETHOD(GetSelection)(ComBool fInstall, IVwSelection ** ppsel);
	STDMETHOD(CLevels)(int * pclev);
	STDMETHOD(AllTextSelInfo)(int * pihvoRoot, int cvlsi, VwSelLevInfo * prgvsli,
		PropTag * ptagTextProp, int * pcpropPrevious, int * pichAnchor, int * pichEnd,
		int * pws);
	STDMETHOD(MatchWhole)(IVwSelection * psel, ComBool * pfMatch);
	STDMETHOD(putref_Limit)(IVwSelection * psel);
	STDMETHOD(get_Limit)(IVwSelection ** ppsel);
	STDMETHOD(putref_StartingPoint)(IVwSelection * psel);
	STDMETHOD(get_StartingPoint)(IVwSelection ** ppsel);
	STDMETHOD(put_SearchWindow)(DWORD hwnd);
	STDMETHOD(get_SearchWindow)(DWORD * phwnd);
	STDMETHOD(get_StoppedAtLimit)(ComBool * pfAtLimit);
	STDMETHOD(put_StoppedAtLimit)(ComBool fAtLimit);
	STDMETHOD(get_LastDirection)(ComBool * pfForward);
	STDMETHOD(putref_ReplaceWith)(ITsString * ptssPattern);
	STDMETHOD(get_ReplaceWith)(ITsString ** pptssPattern);
	STDMETHOD(put_ShowMore)(ComBool fMore);
	STDMETHOD(get_ShowMore)(ComBool * pfMore);
	STDMETHOD(get_IcuLocale)(BSTR * pbstrLocale);
	STDMETHOD(put_IcuLocale)(BSTR bstrLocale);
	STDMETHOD(get_IcuCollatingRules)(BSTR * pbstrRules);
	STDMETHOD(put_IcuCollatingRules)(BSTR bstrRules);
	STDMETHOD(get_ErrorMessage)(BSTR * pbstrMsg);
	STDMETHOD(get_ReplacementText)(ITsString ** pptssText);
	STDMETHOD(get_Group)(int iGroup, ITsString ** pptssGroup);

	// Other public methods

	VwTextSelection * Selection() {return m_qselFound;}
	IVwSelection * Limit() {return m_qselLimit;}
	void SetSelection(VwTextSelection * psel) {m_qselFound = psel;}
	bool Forward() {return m_fForward;}
	void SetFound(bool fFound = true) {m_fFound = fFound;}
	void RemoveIgnorableRuns(ITsString * ptssIn, ITsString ** pptssOut);

protected:
	// Member variables
	ITsStringPtr m_qtssPattern; // Characters to search for, writing system may be significant.
	StrUni m_stuCompiled; // text of m_qtssPattern set by Compile();
	RegexMatcher * m_pmatcher; // ICU regular expression matcher.
	// Compile produces this simplified version of the string, in which we have only the
	// properties that m_fMatchStyles, m_fMatchTags and m_fMatchWritingSystem indicate are
	// relevant, and also have eliminated any runs which are 'ignorable' (in the sense that
	// our collater considers them equal to an empty string).
	ITsStringPtr m_qtssReducedPattern;
	ITsStringPtr m_qtssReplaceWith; // Characters to replace with.
	StrUni m_stuErrorMessage;
	IVwOverlayPtr m_qvo;
	bool m_fMatchCase;
	bool m_fMatchDiacritics;
	bool m_fMatchWholeWord;
	bool m_fMatchExactly;
	bool m_fUseRegularExpressions;
	bool m_fMatchCompatibility;
	bool m_fMatchWritingSystem;
	bool m_fCompiled;
	bool m_fShowMore;

	bool m_fMatchStyles;
	bool m_fMatchTags;

	long m_cref;
	bool m_fFound; // True if we have a current match.
	bool m_fStoppedAtLimit; // True if search terminated by reaching m_qselLimit.
	bool m_fForward; // true if searching forward.
	// The box to start searching from.
	// If we made a selection, this is its paragraph box.
	// If we found inside a lazy box, this is it.
	// If we started from some arbitrary position, this is it.
	VwBox * m_pboxStart;
	// If we found it in a real paragraph, this is a corresponding selection.
	VwTextSelectionPtr m_qselFound;
	// If we found it in a lazy box, this is it. Only if qvwselFound is NULL.
	// The following group of variables allows us to answer AllTextSelInfo directly,
	// and also determines the position from which to continue the search in the lazy box.
	VwLazyBox * m_plzbFound;
	int m_ihvoRoot;
	typedef Vector<VwSelLevInfo> VecSelLevInfo; // Hungarian vvsli
	VecSelLevInfo m_vvsli;
	PropTag m_tagTextProp;
	int m_cpropPrevious; // if this property occurred multiple times
	int m_ichMinFoundLog, m_ichLimFoundLog; // of range found, search offsets (logical char index)
	int m_wsMla; // if it is one alternative of an MLA.
	IVwTextSourcePtr m_qtsWhereFound; // text source in which we found our match.

	IVwSelectionPtr m_qselLimit;
	IVwSelectionPtr m_qselStartingPoint;
	DWORD m_hwndSearch;

	StrUni m_stuLocale; // The name of the locale to use in searching.
	Locale m_locale; // The locale itself.
	StrUni m_stuRules; // Any collating rules to use in searching. If the first character is #, it is followed by a locale name, otherwise whole is ICU rules.
	Collator * m_pcoll; // Collator with all relevant properties.
	RuleBasedCollator * m_prcoll; // Same collater, if rule-based; otherwise null.
	Collator::ECollationStrength m_strength; // (PRIMARY, SECONDARY or TERTIARY)
	SmartBstr m_sbstrDefaultCharStyle;

	// Other protected methods
	void Compile();
	void CleanupRegexPattern();
};


/*----------------------------------------------------------------------------------------------
This class implements a mechanism for allowing search operations to be aborted.
Note that there can be a delay between the user requesting an abort, and the abort happening.
@h3{Hungarian: zserkl}
----------------------------------------------------------------------------------------------*/
class VwSearchKiller : public IVwSearchKiller
{
public:
	// Static methods
	static void CreateCom(IUnknown *punkCtl, REFIID riid, void ** ppv);

	// Constructors/destructors/etc.
	VwSearchKiller();
	virtual ~VwSearchKiller();

	// IUnknown methods
	STDMETHOD(QueryInterface)(REFIID iid, void ** ppv);
	STDMETHOD_(UCOMINT32, AddRef)(void)
	{
		return InterlockedIncrement(&m_cref);
	}
	STDMETHOD_(UCOMINT32, Release)(void)
	{
		long cref = InterlockedDecrement(&m_cref);
		if (cref == 0)
		{
			m_cref = 1;
			delete this;
		}
		return cref;
	}

	// IVwPattern methods
	STDMETHOD(put_Window)(DWORD * hwnd);
	STDMETHOD(FlushMessages)();
	STDMETHOD(get_AbortRequest)(ComBool * pfAbort);
	STDMETHOD(put_AbortRequest)(ComBool fAbort);

protected:
	long m_cref;
	bool m_fAbort;
	HWND m_hwnd;
};

typedef ComSmartPtr<VwSearchKiller> VwSearchKillerPtr;

#endif  //VwPattern_INCLUDED
