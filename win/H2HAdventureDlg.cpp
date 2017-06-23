
// H2HAdventureDlg.cpp : implementation file
//

#include "stdafx.h"
#include "..\engine\adventure_sys.h"
#include "..\engine\Adventure.h"
#include "..\engine\Sys.hpp"
#include "H2HAdventure.h"
#include "H2HAdventureDlg.h"
#include "afxdialogex.h"

#ifdef _DEBUG
#define new DEBUG_NEW
#endif


// CH2HAdventureDlg dialog

// Hold my beer.  Trying sumtin.
int pixelArray[7000];
int numPixels = 0;


CH2HAdventureDlg::CH2HAdventureDlg(CWnd* pParent /*=NULL*/)
	: CDialogEx(IDD_H2HADVENTURE_DIALOG, pParent),
	  gameStarted(false)
{
	m_hIcon = AfxGetApp()->LoadIcon(IDR_MAINFRAME);
	m_pdcMemory = new CDC;
	m_pBitmap = new CBitmap;
	pixelArray[0] = -1;
}

CH2HAdventureDlg::~CH2HAdventureDlg()
{
	delete m_pBitmap; // already deselected
	delete m_pdcMemory;
}


void CH2HAdventureDlg::DoDataExchange(CDataExchange* pDX)
{
	CDialogEx::DoDataExchange(pDX);
}

BEGIN_MESSAGE_MAP(CH2HAdventureDlg, CDialogEx)
	ON_WM_PAINT()
	ON_WM_QUERYDRAGICON()
	ON_BN_CLICKED(IDC_PLAY_BUTTON, &CH2HAdventureDlg::OnBnClickedPlayButton)
END_MESSAGE_MAP()


// CH2HAdventureDlg message handlers

BOOL CH2HAdventureDlg::OnInitDialog()
{
	CDialogEx::OnInitDialog();

	// Set the icon for this dialog.  The framework does this automatically
	//  when the application's main window is not a dialog
	SetIcon(m_hIcon, TRUE);			// Set big icon
	SetIcon(m_hIcon, FALSE);		// Set small icon

	// TODO: Add extra initialization here

	// creates the memory device context and the bitmap
	if (m_pdcMemory->GetSafeHdc() == NULL) {
		CClientDC dc(this);
		// OnPrepareDC(&dc);  Don't think we need to call this because this is a dialog not a scrollview
		CSize sizeTotal(ADVENTURE_SCREEN_WIDTH, ADVENTURE_SCREEN_HEIGHT + ADVENTURE_OVERSCAN);
		CRect rectMax(0, 0, sizeTotal.cx, -sizeTotal.cy);
		dc.LPtoDP(rectMax);
		m_pdcMemory->CreateCompatibleDC(&dc);
		// makes bitmap same size as display window
		m_pBitmap->CreateCompatibleBitmap(&dc, rectMax.right, rectMax.bottom);
		m_pdcMemory->SetMapMode(MM_LOENGLISH);
	}

	return TRUE;  // return TRUE  unless you set the focus to a control
}

// If you add a minimize button to your dialog, you will need the code below
//  to draw the icon.  For MFC applications using the document/view model,
//  this is automatically done for you by the framework.

void CH2HAdventureDlg::OnPaint()
{
	static int lastTime = 0;
	int startTime = Sys::runTime();
	CPaintDC dc(this); // device context for painting

	if (IsIconic())
	{

		SendMessage(WM_ICONERASEBKGND, reinterpret_cast<WPARAM>(dc.GetSafeHdc()), 0);

		// Center icon in client rectangle
		int cxIcon = GetSystemMetrics(SM_CXICON);
		int cyIcon = GetSystemMetrics(SM_CYICON);
		CRect rect;
		GetClientRect(&rect);
		int x = (rect.Width() - cxIcon + 1) / 2;
		int y = (rect.Height() - cyIcon + 1) / 2;

		// Draw the icon
		dc.DrawIcon(x, y, m_hIcon);
	}
	else
	{
		CDialogEx::OnPaint();

		// TODO: Add your message handler code here
		CPaintDC dc(this);
		// OnPrepareDC(&dc);  Don't think we need to call this because this is a dialog not a scrollview
		CRect rectUpdate;
		dc.GetClipBox(&rectUpdate);

		CBitmap* pOldBitmap = m_pdcMemory->SelectObject(m_pBitmap);
		m_pdcMemory->SelectClipRgn(NULL);
		m_pdcMemory->IntersectClipRect(&rectUpdate);
		CBrush backgroundBrush((COLORREF) ::GetSysColor(COLOR_WINDOW));
		CBrush* pOldBrush = m_pdcMemory->SelectObject(&backgroundBrush);
		m_pdcMemory->PatBlt(rectUpdate.left, rectUpdate.top,
			rectUpdate.Width(), rectUpdate.Height(), PATCOPY);
		OnDraw(m_pdcMemory);
		dc.BitBlt(rectUpdate.left, rectUpdate.top,
			rectUpdate.Width(), rectUpdate.Height(),
			m_pdcMemory, rectUpdate.left, rectUpdate.top, SRCCOPY);
		m_pdcMemory->SelectObject(pOldBitmap);
		m_pdcMemory->SelectObject(pOldBrush);
		// Do not call CScrollView::OnPaint() for painting messages


		// OnDraw(&dc);  No longer call this but call it earlier passing in bitmap context
	}
}

void CH2HAdventureDlg::OnDraw(CDC* pDC) {
	for (int pixel = 0; pixel < numPixels; ++pixel) {
		int row = pixel * 7;
		DrawPixel(pDC, pixelArray[row], pixelArray[row + 1], pixelArray[row + 2], pixelArray[row + 3], pixelArray[row + 4],
			pixelArray[row + 5], pixelArray[row + 6]);
	}
	numPixels = 0;
}


// The system calls this function to obtain the cursor to display while the user drags
//  the minimized window.
HCURSOR CH2HAdventureDlg::OnQueryDragIcon()
{
	return static_cast<HCURSOR>(m_hIcon);
}

#include <mmsystem.h>
typedef void (CALLBACK TIMECALLBACK)(UINT uTimerID, UINT uMsg, DWORD_PTR dwUser, DWORD_PTR dw1, DWORD_PTR dw2);
typedef TIMECALLBACK FAR *LPTIMECALLBACK;
void CALLBACK TimerWindowProc(UINT uTimerID, UINT uMsg, DWORD_PTR dwUser, DWORD_PTR dw1, DWORD_PTR dw2);

// We need to make a couple variables available via static-global declaration.
// The 'this' object is needed by the timed callback and the window's device context (DC) is needed
// by the PaintPixel call out.
static CDialogEx* gThis = NULL;
static CClientDC* gDC = NULL;
static float gGfxScaler = 1;


void CH2HAdventureDlg::OnBnClickedPlayButton()
{
	if (!gameStarted) {
		gThis = this;
		
		Adventure_Setup(2, 0, NULL, 2, 1, 1);

		// Start the timer
		DWORD timerId = ::timeSetEvent(16, 16, (LPTIMECALLBACK)TimerWindowProc, NULL, TIME_PERIODIC);
		gameStarted = true;
	}
}

void CALLBACK TimerWindowProc(UINT uTimerID, UINT uMsg, DWORD_PTR dwUser,
	DWORD_PTR dw1, DWORD_PTR dw2)
{
	static int lastTime = 0;
	long startTime = Sys::runTime();
	static int color = 0;

	CClientDC dc(gThis); // device context for painting
	gDC = &dc;

	// If we were using a bitmap
	//HBITMAP bmpOld = (HBITMAP)::SelectObject(gDC, bmpOffscreen);
	HPEN penOld = (HPEN)::SelectObject(dc, ::GetStockObject(NULL_PEN));

	numPixels = 0;
	pixelArray[0] = -1;
	
	Adventure_Run();

	CPoint screenTopLeft(12, 194);
	CSize screenSize(ADVENTURE_SCREEN_WIDTH, ADVENTURE_SCREEN_HEIGHT+ADVENTURE_OVERSCAN);
	CRect screenRect(screenTopLeft, screenSize);
	dc.LPtoDP(screenRect);
	gThis->InvalidateRect(screenRect, TRUE);

	/*
	Original test code to draw a big gray block
	HBRUSH newBrush = (HBRUSH)::CreateSolidBrush(RGB(color, color, color));
	color ++;
	HBRUSH oldBrush = (HBRUSH)::SelectObject(dc, newBrush);
	dc.Rectangle(12, 194, 640, 448);
	RECT rcClient;
	gThis->GetClientRect(&rcClient);
	*/

	// If we were using a bitmap
	//int cx = (gWindowSizeX / 2) - ((ADVENTURE_SCREEN_WIDTH * gGfxScaler) / 2);
	//int cy = (gWindowSizeY / 2) - ((ADVENTURE_SCREEN_HEIGHT * gGfxScaler) / 2);
	//int cw = ADVENTURE_SCREEN_WIDTH * gGfxScaler;
	//int ch = ADVENTURE_SCREEN_HEIGHT * gGfxScaler;
	//::BitBlt(winDC, cx, cy, cw, ch, gDC, 0, 0, SRCCOPY);
	//::SelectObject(gDC, bmpOld);
	

	::SelectObject(dc, penOld);

	// Do we need to clean up dc?
	//::DeleteDC(gDC);
	//::ReleaseDC(gWnd, winDC);
	gDC = NULL;

	int totalTime = Sys::runTime() - startTime;
	int sinceLast = startTime - lastTime;
	lastTime = startTime;
	char message[1000];
	sprintf(message, "Timed proc took %d ms.  Last called %d ms ago at %d.\n", totalTime, sinceLast, lastTime/1000);
	Sys::consoleLog(message);
}

void Platform_ReadJoystick(bool* left, bool* up, bool* right, bool* down, bool* fire)
{
	if (left) *left = GetAsyncKeyState(VK_LEFT) & 0x8000;
	if (up) *up = GetAsyncKeyState(VK_UP) & 0x8000;
	if (right) *right = GetAsyncKeyState(VK_RIGHT) & 0x8000;
	if (down) *down = GetAsyncKeyState(VK_DOWN) & 0x8000;
	if (fire) *fire = GetAsyncKeyState(VK_SPACE) & 0x8000;
}

void Platform_ReadConsoleSwitches(bool* reset)
{
/*
	if (reset) *reset = GetAsyncKeyState(resetKey) & 0x8000;
*/
}

void Platform_ReadDifficultySwitches(int* left, int* right)
{
/*
	*left = leftDifficulty;
	*right = rightDifficulty;
*/
}

void Platform_PaintPixel(int r, int g, int b, int x, int y, int width/*=1*/, int height/*=1*/)
{
	int row = numPixels * 7;
	pixelArray[row] = r;
	pixelArray[row + 1] = g;
	pixelArray[row + 2] = b;
	pixelArray[row + 3] = x;
	pixelArray[row + 4] = y;
	pixelArray[row + 5] = width;
	pixelArray[row + 6] = height;
	++numPixels;
	pixelArray[numPixels * 7] = -1;
}

void CH2HAdventureDlg::DrawPixel(CDC* pDC, int r, int g, int b, int x, int y, int width, int height)
{
	/*
	//HBITMAP bmpOld = (HBITMAP)::SelectObject(gDC, bmpOffscreen);
	HPEN penOld = (HPEN)::SelectObject(dc, ::GetStockObject(NULL_PEN));

	//Adventure_Run();

	HBRUSH newBrush = (HBRUSH)::CreateSolidBrush(RGB(color, color, color));
	color++;
	HBRUSH oldBrush = (HBRUSH)::SelectObject(dc, newBrush);
	dc.Rectangle(12, 194, 640, 448);
	RECT rcClient;
	gThis->GetClientRect(&rcClient);
*/
	if (pDC)
	{
		HBRUSH newBrush = (HBRUSH)::CreateSolidBrush(RGB(r, g, b));
		HBRUSH oldBrush = (HBRUSH)::SelectObject(*pDC, newBrush);

		// The game expects a bottom up buffer, so we flip the orientation here
		y = (ADVENTURE_SCREEN_HEIGHT - y) + ADVENTURE_OVERSCAN;

		x *= gGfxScaler;
		x += 12;
		y *= gGfxScaler;
		y += 194;
		width *= gGfxScaler;
		height *= gGfxScaler;
		::Rectangle(*pDC, x, y - height, x + width + 1, y + 1);

		::SelectObject(*pDC, oldBrush);
		::DeleteObject(newBrush);
	}
}

void Platform_MuteSound(bool nMute)
{
	// TODO: Implement
}

void Platform_MakeSound(int sound, float volume)
{
/*
	// TODO: Handle volume
	char szModule[MAX_PATH];
	char szDrive[MAX_PATH];
	char szDir[MAX_PATH];
	char szSoundPath[MAX_PATH];

	GetModuleFileName(NULL, szModule, MAX_PATH);
	_splitpath(szModule, szDrive, szDir, NULL, NULL);

	switch (sound)
	{
	case SOUND_PICKUP:
		PlaySound((char*)IDR_PICKUP_WAV, NULL, SND_RESOURCE | SND_ASYNC);
		wsprintf(szSoundPath, "%s%ssounds\\pickup.wav", szDrive, szDir);
		break;
	case SOUND_PUTDOWN:
		PlaySound((char*)IDR_PUTDOWN_WAV, NULL, SND_RESOURCE | SND_ASYNC);
		break;
	case SOUND_WON:
		PlaySound((char*)IDR_WON_WAV, NULL, SND_RESOURCE | SND_ASYNC);
		break;
	case SOUND_ROAR:
		PlaySound((char*)IDR_ROAR_WAV, NULL, SND_RESOURCE | SND_ASYNC);
		break;
	case SOUND_EATEN:
		PlaySound((char*)IDR_EATEN_WAV, NULL, SND_RESOURCE | SND_ASYNC);
		break;
	case SOUND_DRAGONDIE:
		PlaySound((char*)IDR_DRAGONDIE_WAV, NULL, SND_RESOURCE | SND_ASYNC);
		break;
	}

	sndPlaySound(szSoundPath, SND_ASYNC | SND_NODEFAULT);
*/
}

float Platform_Random()
{
	// Using the C runtime random functions
	return (float)rand() / RAND_MAX;
}

void Platform_DisplayStatus(const char* message, int duration) {
/*
	static const char* title = "";
	int msgboxID = MessageBox(
		NULL,
		(LPCSTR)message,
		(LPCSTR)title,
		MB_ICONWARNING | MB_OK | MB_DEFBUTTON2
	);
*/
}
