
// H2HAdventureDlg.cpp : implementation file
//

#include "stdafx.h"
#include "..\engine\adventure_sys.h"
#include "..\engine\Adventure.h"
#include "..\engine\GameSetup.hpp"
#include "..\engine\UdpTransport.hpp"
#include "H2HAdventure.h"
#include "H2HAdventureDlg.h"
#include "WinRestClient.h"
#include "WinUdpSocket.h"
#include "afxdialogex.h"
#include "Resource.h"

#ifdef _DEBUG
#define new DEBUG_NEW
#endif

#include <mmsystem.h>
typedef void (CALLBACK TIMECALLBACK)(UINT uTimerID, UINT uMsg, DWORD_PTR dwUser, DWORD_PTR dw1, DWORD_PTR dw2);
typedef TIMECALLBACK FAR *LPTIMECALLBACK;
void CALLBACK TimerWindowProc(UINT uTimerID, UINT uMsg, DWORD_PTR dwUser, DWORD_PTR dw1, DWORD_PTR dw2);

// CH2HAdventureDlg dialog
static CH2HAdventureDlg* gThis = NULL;
static int gBrightness = 0;

int leftKey = VK_LEFT;
int rightKey = VK_RIGHT;
int upKey = VK_UP;
int downKey = VK_DOWN;
int dropKey = VK_SPACE;
int resetKey = VK_RETURN;


int pixelArray[7000];
int numPixels = 0;


CH2HAdventureDlg::CH2HAdventureDlg(CWnd* pParent /*=NULL*/)
	: CDialogEx(IDD_H2HADVENTURE_DIALOG, pParent)
{
	m_hIcon = AfxGetApp()->LoadIcon(IDR_MAINFRAME);
	pBitmap = NULL;
	pInMemDC = NULL;
	pixelArray[0] = -1;
	gameStarted = FALSE;
	xport = NULL;
	setup = NULL;
	client = new WinRestClient();
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

		// Setup Bitmap
		CRect WinRect(0, 0, ADVENTURE_SCREEN_WIDTH, ADVENTURE_SCREEN_HEIGHT);
		if (pBitmap == NULL)
		{
			pInMemDC = new CDC();
			pInMemDC->CreateCompatibleDC(&dc);
			pBitmap = new CBitmap();
			pBitmap->CreateCompatibleBitmap(&dc, WinRect.Width(), WinRect.Height());
			pInMemDC->SelectObject(pBitmap);
		}

		// Painting on dialog
		OnDraw(pInMemDC);

		// Copy bitmap to window
		dc.BitBlt(10, 10, WinRect.right + 10, WinRect.bottom + 10, pInMemDC, 0, 0, SRCCOPY);

	}
}

void CH2HAdventureDlg::OnDraw(CDC* pDC) {
	HPEN penOld = (HPEN)::SelectObject(*pInMemDC, ::GetStockObject(NULL_PEN));

	for (int pixel = 0; pixel < numPixels; ++pixel) {
		int row = pixel * 7;
		DrawPixel(pDC, pixelArray[row], pixelArray[row + 1], pixelArray[row + 2], pixelArray[row + 3], pixelArray[row + 4],
			pixelArray[row + 5], pixelArray[row + 6]);
	}
	numPixels = 0;

	::SelectObject(*pInMemDC, penOld);
}


void CH2HAdventureDlg::DrawPixel(CDC* pDC, int r, int g, int b, int x, int y, int width, int height)
{
	if (pDC)
	{

		// The game expects a bottom up buffer, so we flip the orientation here
		y = (ADVENTURE_SCREEN_HEIGHT - y) + ADVENTURE_OVERSCAN;

		/*
		x *= gGfxScaler;
		y *= gGfxScaler;
		width *= gGfxScaler;
		height *= gGfxScaler;
		*/

		HBRUSH newBrush = (HBRUSH)::CreateSolidBrush(RGB(r, g, b));
		HBRUSH oldBrush = (HBRUSH)::SelectObject(*pInMemDC, newBrush);

		::Rectangle(*pInMemDC, x, y - height, x + width + 1, y + 1);

		::SelectObject(*pInMemDC, oldBrush);
		::DeleteObject(newBrush);

	}
}


// The system calls this function to obtain the cursor to display while the user drags
//  the minimized window.
HCURSOR CH2HAdventureDlg::OnQueryDragIcon()
{
	return static_cast<HCURSOR>(m_hIcon);
}

void CALLBACK TimerWindowProc(UINT uTimerID, UINT uMsg, DWORD_PTR dwUser,
	DWORD_PTR dw1, DWORD_PTR dw2)
{
	numPixels = 0;
	pixelArray[0] = -1;

	gThis->update();

	gThis->Invalidate(FALSE);
}

void CH2HAdventureDlg::update() {
	if (!setup->isGameSetup()) {
		setup->checkSetup();
		if (setup->isGameSetup()) {
			GameSetup::GameParams params = setup->getSetup();
			if (params.noTransport) {
				delete xport;
				xport = NULL;
			}
			Platform_MuteSound(params.shouldMute);

			Adventure_Setup(params.numberPlayers, params.thisPlayer, xport, params.gameLevel, 1, 1);
		}
	}
	else {
		// Run a frame of the game
		Adventure_Run();
	}

}

void CH2HAdventureDlg::OnOK() {
	// Override to do nothing instead of closing the window.
}


void CH2HAdventureDlg::OnBnClickedPlayButton()
{
	if (!gameStarted) {

		gThis = this;

		xport = new UdpTransport();
		setup = new GameSetup(*client, *xport);
		/*
		setup->setGameLevel(1);
		setup->setNumberPlayers(2);
		*/
		char** argv = new char*[3];
		argv[0] = "broker";
		argv[1] = "1";
		argv[2] = "2";
		setup->setCommandLineArgs(2, argv);

		setup->setPlayerName("Waldo");
		CComboBox* gameCombo = (CComboBox*)gThis->GetDlgItem(IDC_GAME_COMBO);
		int gameSelected = gameCombo->GetCurSel();
		setup->setGameLevel(gameSelected);
		CComboBox* playersCombo = (CComboBox*)gThis->GetDlgItem(IDC_PLAYERS_COMBO);
		int playersSelected = playersCombo->GetCurSel() + 2;
		setup->setNumberPlayers(playersSelected);

		GameSetup::GameParams params = setup->getSetup();
		if (!params.noTransport) {
			WinUdpSocket* socket = new WinUdpSocket();
			xport->useSocket(socket);
		}
		//Adventure_Setup(2, 0, NULL, 0, 1, 1);
		DWORD timerId = ::timeSetEvent(16, 1000, (LPTIMECALLBACK)TimerWindowProc, NULL, TIME_PERIODIC);
		gameStarted = TRUE;
	}
}

void Platform_ReadJoystick(bool* left, bool* up, bool* right, bool* down, bool* fire)
{
	if (left) *left = GetAsyncKeyState(leftKey) & 0x8000;
	if (up) *up = GetAsyncKeyState(upKey) & 0x8000;
	if (right) *right = GetAsyncKeyState(rightKey) & 0x8000;
	if (down) *down = GetAsyncKeyState(downKey) & 0x8000;
	if (fire) *fire = GetAsyncKeyState(dropKey) & 0x8000;
}

void Platform_ReadConsoleSwitches(bool* reset)
{
	if (reset) *reset = GetAsyncKeyState(resetKey) & 0x8000;
}

void Platform_ReadDifficultySwitches(int* left, int* right)
{
	// Haven't figured out how to support difficulty.  Pegged to B for right now.
	*left = DIFFICULTY_B;
	*right = DIFFICULTY_B;
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

void Platform_MuteSound(bool nMute)
{
	// TODO: Implement
}

void Platform_MakeSound(int sound, float volume)
{

	if (volume > 0.5 * MAX_VOLUME) {
		switch (sound)
		{
		case SOUND_PICKUP:
			PlaySoundA((char*)IDR_PICKUP_WAV, NULL, SND_RESOURCE | SND_ASYNC); break;
		case SOUND_PUTDOWN:
			PlaySoundA((char*)IDR_PUTDOWN_WAV, NULL, SND_RESOURCE | SND_ASYNC); break;
		case SOUND_ROAR:
			PlaySoundA((char*)IDR_ROAR_WAV, NULL, SND_RESOURCE | SND_ASYNC); break;
		case SOUND_EATEN:
			PlaySoundA((char*)IDR_EATEN_WAV, NULL, SND_RESOURCE | SND_ASYNC); break;
		case SOUND_DRAGONDIE:
			PlaySoundA((char*)IDR_DRAGONDIE_WAV, NULL, SND_RESOURCE | SND_ASYNC); break;
		case SOUND_WON:
			PlaySoundA((char*)IDR_WON_WAV, NULL, SND_RESOURCE | SND_ASYNC); break;
		case SOUND_GLOW:
			PlaySoundA((char*)IDR_GLOW_WAV, NULL, SND_RESOURCE | SND_ASYNC); break;
		}
	}
	else if (volume > 0.25 * MAX_VOLUME) {
		switch (sound)
		{
		case SOUND_PICKUP:
			PlaySoundA((char*)IDR_PICKUPNEAR_WAV, NULL, SND_RESOURCE | SND_ASYNC); break;
		case SOUND_PUTDOWN:
			PlaySoundA((char*)IDR_PUTDOWNNEAR_WAV, NULL, SND_RESOURCE | SND_ASYNC); break;
		case SOUND_ROAR:
			PlaySoundA((char*)IDR_ROARNEAR_WAV, NULL, SND_RESOURCE | SND_ASYNC); break;
		case SOUND_EATEN:
			PlaySoundA((char*)IDR_EATENNEAR_WAV, NULL, SND_RESOURCE | SND_ASYNC); break;
		case SOUND_DRAGONDIE:
			PlaySoundA((char*)IDR_DRAGONDIENEAR_WAV, NULL, SND_RESOURCE | SND_ASYNC); break;
		case SOUND_WON:
			PlaySoundA((char*)IDR_WON_WAV, NULL, SND_RESOURCE | SND_ASYNC); break;
		case SOUND_GLOW:
			PlaySoundA((char*)IDR_GLOWNEAR_WAV, NULL, SND_RESOURCE | SND_ASYNC); break;
		}
	}
	else if (volume > 0) {
		switch (sound)
		{
		case SOUND_PICKUP:
			PlaySoundA((char*)IDR_PICKUPFAR_WAV, NULL, SND_RESOURCE | SND_ASYNC); break;
		case SOUND_PUTDOWN:
			PlaySoundA((char*)IDR_PUTDOWNFAR_WAV, NULL, SND_RESOURCE | SND_ASYNC); break;
		case SOUND_ROAR:
			PlaySoundA((char*)IDR_ROARFAR_WAV, NULL, SND_RESOURCE | SND_ASYNC); break;
		case SOUND_EATEN:
			PlaySoundA((char*)IDR_EATENFAR_WAV, NULL, SND_RESOURCE | SND_ASYNC); break;
		case SOUND_DRAGONDIE:
			PlaySoundA((char*)IDR_DRAGONDIEFAR_WAV, NULL, SND_RESOURCE | SND_ASYNC); break;
		case SOUND_WON:
			PlaySoundA((char*)IDR_WON_WAV, NULL, SND_RESOURCE | SND_ASYNC); break;
		case SOUND_GLOW:
			PlaySoundA((char*)IDR_GLOWFAR_WAV, NULL, SND_RESOURCE | SND_ASYNC); break;
		}
	}
}

float Platform_Random()
{
	// Using the C runtime random functions
	return (float)rand() / RAND_MAX;
}

void Platform_DisplayStatus(const char* message, int duration) {
	int a = lstrlenA(message);
	BSTR unicodestr = SysAllocStringLen(NULL, a);
	::MultiByteToWideChar(CP_ACP, 0, message, a, unicodestr, a);

	CWnd* label = gThis->GetDlgItem(IDC_STATUS_LABEL);
	label->SetWindowText(unicodestr);

	::SysFreeString(unicodestr);
}
