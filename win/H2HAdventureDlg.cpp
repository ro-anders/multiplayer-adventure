
// H2HAdventureDlg.cpp : implementation file
//

#include "stdafx.h"
#include "H2HAdventure.h"
#include "H2HAdventureDlg.h"
#include "afxdialogex.h"

#ifdef _DEBUG
#define new DEBUG_NEW
#endif


// CH2HAdventureDlg dialog



CH2HAdventureDlg::CH2HAdventureDlg(CWnd* pParent /*=NULL*/)
	: CDialogEx(IDD_H2HADVENTURE_DIALOG, pParent),
	  gameStarted(false)
{
	m_hIcon = AfxGetApp()->LoadIcon(IDR_MAINFRAME);
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

	return TRUE;  // return TRUE  unless you set the focus to a control
}

// If you add a minimize button to your dialog, you will need the code below
//  to draw the icon.  For MFC applications using the document/view model,
//  this is automatically done for you by the framework.

void CH2HAdventureDlg::OnPaint()
{
	if (IsIconic())
	{
		CPaintDC dc(this); // device context for painting

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
	}
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
static CClientDC* gDc = NULL;


void CH2HAdventureDlg::OnBnClickedPlayButton()
{
	if (!gameStarted) {
		// Start the timer
		gThis = this;
		DWORD timerId = ::timeSetEvent(60, 60, (LPTIMECALLBACK)TimerWindowProc, NULL, TIME_PERIODIC);
	}
}

void CALLBACK TimerWindowProc(UINT uTimerID, UINT uMsg, DWORD_PTR dwUser,
	DWORD_PTR dw1, DWORD_PTR dw2)
{
	static int color = 0;

	CClientDC dc(gThis); // device context for painting
	gDc = &dc;

	//HBITMAP bmpOld = (HBITMAP)::SelectObject(gDC, bmpOffscreen);
	HPEN penOld = (HPEN)::SelectObject(dc, ::GetStockObject(NULL_PEN));

	//Adventure_Run();

	HBRUSH newBrush = (HBRUSH)::CreateSolidBrush(RGB(color, color, color));
	color ++;
	HBRUSH oldBrush = (HBRUSH)::SelectObject(dc, newBrush);
	dc.Rectangle(12, 194, 640, 448);
	RECT rcClient;
	gThis->GetClientRect(&rcClient);

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
	gDc = NULL;
}

void Platform_ReadJoystick(bool* left, bool* up, bool* right, bool* down, bool* fire)
{
	/*
	if (left) *left = GetAsyncKeyState(leftKey) & 0x8000;
	if (up) *up = GetAsyncKeyState(upKey) & 0x8000;
	if (right) *right = GetAsyncKeyState(rightKey) & 0x8000;
	if (down) *down = GetAsyncKeyState(downKey) & 0x8000;
	if (fire) *fire = GetAsyncKeyState(dropKey) & 0x8000;
	*/
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
/*
	if (gDC)
	{
		HBRUSH newBrush = (HBRUSH)::CreateSolidBrush(RGB(r, g, b));
		HBRUSH oldBrush = (HBRUSH)::SelectObject(gDC, newBrush);

		// The game expects a bottom up buffer, so we flip the orientation here
		y = (ADVENTURE_SCREEN_HEIGHT - y) + ADVENTURE_OVERSCAN;

		x *= gGfxScaler;
		y *= gGfxScaler;
		width *= gGfxScaler;
		height *= gGfxScaler;
		::Rectangle(gDC, x, y - height, x + width + 1, y + 1);

		::SelectObject(gDC, oldBrush);
		::DeleteObject(newBrush);
	}
*/
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
