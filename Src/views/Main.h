/*--------------------------------------------------------------------*//*:Ignore this sentence.
Copyright (c) 1999-2013 SIL International
This software is licensed under the LGPL, version 2.1 or later
(http://www.gnu.org/licenses/lgpl-2.1.html)

File: Main.h
Responsibility: John Thomson
Last reviewed: Not yet.

Description:
	Main header file for the views component.
-------------------------------------------------------------------------------*//*:End Ignore*/
#ifdef _MSC_VER
#pragma once
#endif
#ifndef VIEWS_H
#define VIEWS_H 1

#define NO_EXCEPTIONS 1
#include "common.h"
#if defined(WIN32) || defined(WIN64)
#include <shlobj.h> // for one call to SHGetSpecialFolderPath
#endif

// We want to use std::min and std::max(for portability), so we have to undef min/max which
// are defined in WinDef.h
#undef min
#undef max
using namespace std;

/* ---------------------
If you want to show colored boxes around the boxes uncomment the following define:
----------------------*/
//#define _DEBUG_SHOW_BOX
/*----------------------*/
#define kdzptInch 72
#define kdzmpInch 72000

#define kchwHardLineBreak (wchar)0x2028

#include "VwResources.h"
#if defined(WIN32) || defined(WIN64)
#include "..\..\..\Src\AppCore\Res\AfAppRes.h"
#else
#include <Res/AfAppRes.h> // from AppCore
#endif

// This is needed now by FwGr.h which uses std::wstring
#include <string>

// This is needed for the min and max methods
#include <algorithm>

using namespace fwutil;

//:>**********************************************************************************
//:>	Forward declarations
//:>**********************************************************************************
class VwBox;
class VwGroupBox;
class VwRootBox;
class VwEnv;
class VwStringBox;
class VwStringBoxMain;
class VwBrokenStringBox;
class VwParagraphBox;
class VwRootBox;
class VwTableBox;
class VwAbstractNotifier;
class VwNotifier;
class VwSelection;
class VwTextSelection;
class VwTableRowBox;
class VwTableCellBox;
class SilDataAccess;
#if defined(WIN32) || defined(WIN64)
class VwAccessRoot;
#endif
class VwSynchronizer;
DEFINE_COM_PTR(VwSynchronizer); // deals with circularity of root box and synch.

//:>**********************************************************************************
//:>	Classes we have to include before we can do typedefs
//:>**********************************************************************************
#include "HashMap.h"
#include "Vector.h"

//:>**********************************************************************************
//:>	Other classes we have to include.
//:>**********************************************************************************

//:>**********************************************************************************
//:>	Interfaces.
//:>**********************************************************************************
#include <FwKernelTlb.h>
#include "ViewsTlb.h"
#include "LgUnicodeCollater.h"
#include "ActionHandler.h"

//:>**********************************************************************************
//:>	Types and constants used in View subsystem
//:>**********************************************************************************

// BuildRecs are used to construct a description of the part of a display that needs
// to be rebuilt when an underlying value changes.
// A record corresponds to two layers of the display hierarchy: one object, and one
// attribute of that object.
// By tracing up throught the display being rebuilt, we determine that we need to
// rebuild part of the display of a particular occurrence of a particular object
// in its parent attribute, and that we need to rebuild a particular attribute of
// that object (or part of that attribute).
struct BuildRec // Hungarian: bldrec
{
	HVO hvo;	// object we want to rebuild part of display for
	int ihvo;			// expected index of that object in its attribute to
						// distinguish multiple occurrences
	int tag;		// of attr we want from that object
	int cprop;		// last record: number of times to rebuild that prop
						// others: number of occurrences of that prop to ignore
						// before going ahead (for repeats in same object)
};
// A BuildVec is a list, ordered from the top level object in the display down to
// the one whose attr changed, completely describing what needs rebuilding.
typedef Vector<BuildRec> BuildVec; // Hungarian: bldvec


typedef HashMap<VwBox *, Rect> FixupMap; // Hungarian fixmap

typedef Vector<VwBox *> BoxVec; // Hungarian vbox

typedef Vector<VwGroupBox *> GroupBoxVec; // Hungarian vgbox

typedef Vector<int> IntVec; // Hungarian intvec, or replace int with what it signifies
typedef Vector<HVO> HvoVec; // Hungarian vhvo
typedef Vector<long> LongVec; // Hungarian vlo

typedef ComVector<VwAbstractNotifier> NotifierVec; // vpanote

typedef Set<VwBox *> BoxSet;  //Hungarian boxset

typedef ComMultiMap<VwBox *, VwAbstractNotifier> NotifierMap; // Hungarian mmboxqnote

typedef ComVector<ITsTextProps> TtpVec; // Hungarian vqttp
typedef ComVector<IVwPropertyStore> VwPropsVec; // Hungarian vqvps

typedef ComMultiMap<HVO, VwAbstractNotifier> ObjNoteMap; // Hungarian mmhvoqnote

#include "UtilView.h"
// Needed only so VwCacheDa can reference kflids
// that relate to structured text.
#include "CellarConstants.h"

//:>**********************************************************************************
//:>	Implementations.
//:>**********************************************************************************
// for interfacing with Graphite:
namespace gr {
typedef unsigned char utf8;
#if defined(WIN32) || defined(WIN64)
typedef wchar_t utf16;
#else
typedef wchar utf16;
#endif
typedef unsigned long int utf32;
#define UtfType LgUtfForm
class GrEngine;
} // gr

#include "LgLineBreaker.h"
class RomRenderEngine;
DEFINE_COM_PTR(RomRenderEngine);
class UniscribeEngine;
DEFINE_COM_PTR(UniscribeEngine);
class GraphiteEngine;
DEFINE_COM_PTR(GraphiteEngine);
#include "RomRenderSegment.h"
#include "RomRenderEngine.h"
#if !defined(_WIN32) && !defined(_M_X64)
#include "UniscribeLinux.h"
#endif
#include "UniscribeSegment.h"
#include "UniscribeEngine.h"
#include "GraphiteSegment.h"
#include "GraphiteEngine.h"

// obsolete #include "ActualTextProperties.h"
#include "ViewsGlobals.h"
#include "VwBaseDataAccess.h"
#include "VwBaseVirtualHandler.h"
#include "VwCacheDa.h"
#include "VwOverlay.h"
#include "VwGraphics.h"
#include "VwPropertyStore.h"
#include "VwTxtSrc.h"
#include "VwPrintContext.h"
#include "VwSimpleBoxes.h"
#include "VwNotifier.h"
#include "VwTextBoxes.h"
#include "VwSelection.h"
#include "VwRootBox.h"
#include "VwEnv.h"
#include "VwTableBox.h"
#include "VwLazyBox.h"
#include "StringToNumHelpers.h"
#if defined(WIN32) || defined(WIN64)
#include "AfDef.h"
#include "AfColorTable.h"
#include "AfGfx.h"
#include "VwPattern.h"
#include "VwAccessRoot.h"
#include "VwSynchronizer.h"
#include "VwTextStore.h"
#endif
#include "AfColorTable.h"
#include "VwPattern.h"
#include "VwSynchronizer.h"
#include "VwLayoutStream.h"
#include "VwUndo.h"
#include "VwInvertedViews.h"
#if !defined(_WIN32) && !defined(_M_X64)
#include "DisplayCapsInfo.h"
#endif
#include "TsString.h"
#include "TsTextProps.h"
#include "TsStrFactory.h"
#include "TsPropsFactory.h"
#include "TextServ.h"
#include "StringToNumHelpers.h"
#include "FwStyledText.h"
#if defined(WIN32) || defined(WIN64)
// for parsing XML files; in this DLL, we want the parser to work with wide characters,
// since we always parse BSTRs.
#define XML_UNICODE_WCHAR_T
#else
// XML_UNICODE_WCHAR_T causes XML_Char to be wchar_t
#ifdef XML_UNICODE
#error "Don't define XML_UNICODE as this causes XML_CHAR to be UTF-16 which expat on Linux can't handle"
#endif
#endif
#include "../Cellar/FwXml.h"

#endif //!VIEWS_H
