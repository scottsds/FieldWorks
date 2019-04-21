# Input
# =====
# BUILD_ROOT: d:\FieldWorks
# BUILD_TYPE: d, r, p
# BUILD_CONFIG: Debug, Release, Profile
#

BUILD_PRODUCT=FwKernel
BUILD_EXTENSION=dll
BUILD_REGSVR=1

DEFS=$(DEFS) /D_MERGE_PROXYSTUB /I"$(COM_OUT_DIR)" /I"$(COM_OUT_DIR_RAW)"

FWKERNEL_SRC=$(BUILD_ROOT)\src\Kernel
GENERIC_SRC=$(BUILD_ROOT)\src\Generic
APPCORE_SRC=$(BUILD_ROOT)\src\AppCore
DEBUGPROCS_SRC=$(BUILD_ROOT)\src\DebugProcs
CELLAR_SRC=$(BUILD_ROOT)\Src\Cellar

# Set the USER_INCLUDE environment variable.
UI=$(FWKERNEL_SRC);$(GENERIC_SRC);$(APPCORE_SRC);$(DEBUGPROCS_SRC);$(CELLAR_SRC)

!IF "$(USER_INCLUDE)"!=""
USER_INCLUDE=$(UI);$(USER_INCLUDE)
!ELSE
USER_INCLUDE=$(UI)
!ENDIF

!INCLUDE "$(BUILD_ROOT)\bld\_init.mak"

!INCLUDE "$(BUILD_ROOT)\bld\_rule.mak"

PATH=$(COM_OUT_DIR);$(PATH)

DEFFILE=FwKernel.def
LINK_LIBS=Generic.lib $(LINK_LIBS)

# === Object Lists ===

OBJ_FWKERNEL=\
	$(INT_DIR)\autopch\ModuleEntry.obj\
	$(INT_DIR)\autopch\dlldatax.obj\


OBJ_ALL=$(OBJ_FWKERNEL)


IDL_MAIN=$(COM_OUT_DIR)\FwKernelTlb.idl

PS_MAIN=FwKernelPs


# === Targets ===
!INCLUDE "$(BUILD_ROOT)\bld\_targ.mak"


# === Rules ===
PCHNAME=main

ARG_SRCDIR=$(FWKERNEL_SRC)
!INCLUDE "$(BUILD_ROOT)\bld\_rule.mak"

ARG_SRCDIR=$(GENERIC_SRC)
!INCLUDE "$(BUILD_ROOT)\bld\_rule.mak"

ARG_SRCDIR=$(APPCORE_SRC)
!INCLUDE "$(BUILD_ROOT)\bld\_rule.mak"

ARG_SRCDIR=$(CELLAR_SRC)
!INCLUDE "$(BUILD_ROOT)\bld\_rule.mak"


# === Custom Rules ===


# === Custom Targets ===
