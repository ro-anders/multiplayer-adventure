
// H2HAdventure.h : main header file for the PROJECT_NAME application
//

#pragma once

#ifndef __AFXWIN_H__
	#error "include 'stdafx.h' before including this file for PCH"
#endif

#include "resource.h"		// main symbols


// CH2HAdventureApp:
// See H2HAdventure.cpp for the implementation of this class
//

class CH2HAdventureApp : public CWinApp
{
public:
	CH2HAdventureApp();

// Overrides
public:
	virtual BOOL InitInstance();

// Implementation

	DECLARE_MESSAGE_MAP()
};

extern CH2HAdventureApp theApp;