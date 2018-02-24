
// H2HAdventureDlg.cpp : implementation file
//

#include "stdafx.h"
#include "..\engine\adventure_sys.h"
#include "..\engine\Adventure.h"
#include "..\engine\GameSetup.hpp"
#include "..\engine\Logger.hpp"
#include "..\engine\Sys.hpp"
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

int numPixels = 0;
const int MAX_PIXELS = 20000;
int pixelArray[MAX_PIXELS];
bool reportedOverflow = false;

CH2HAdventureDlg::CH2HAdventureDlg(CWnd* pParent /*=NULL*/)
	: CDialogEx(IDD_H2HADVENTURE_DIALOG, pParent)
{
	m_hIcon = AfxGetApp()->LoadIcon(IDR_MAINFRAME);
	pBitmap = NULL;
	pInMemDC = NULL;
	gameStarted = FALSE;
	xport = NULL;
	setup = NULL;
	client = new WinRestClient();
	socket = NULL;
}

void CH2HAdventureDlg::DoDataExchange(CDataExchange* pDX)
{
	CDialogEx::DoDataExchange(pDX);
}

BEGIN_MESSAGE_MAP(CH2HAdventureDlg, CDialogEx)
	ON_WM_PAINT()
	ON_WM_QUERYDRAGICON()
	ON_BN_CLICKED(IDC_PLAY_BUTTON, &CH2HAdventureDlg::OnBnClickedPlayButton)
	ON_CBN_SELCHANGE(IDC_PLAYERS_COMBO, &CH2HAdventureDlg::OnCbnSelchangePlayersCombo)
END_MESSAGE_MAP()


// CH2HAdventureDlg message handlers

BOOL CH2HAdventureDlg::OnInitDialog()
{
	CDialogEx::OnInitDialog();

	gThis = this;

	// Set the icon for this dialog.  The framework does this automatically
	//  when the application's main window is not a dialog
	SetIcon(m_hIcon, TRUE);			// Set big icon
	SetIcon(m_hIcon, FALSE);		// Set small icon

	// TODO: Add extra initialization here
	Logger::setup(Logger::FILE, Logger::INFO);

	CComboBox* playersCombo = (CComboBox*)GetDlgItem(IDC_PLAYERS_COMBO);
	playersCombo->SetCurSel(0);
	CComboBox* gameCombo = (CComboBox*)GetDlgItem(IDC_GAME_COMBO);
	gameCombo->SetCurSel(0);

	BOOL canPlay = checkCanPlay();
	if (!canPlay) {
		GetDlgItem(IDC_NAME_EDIT)->EnableWindow(FALSE);
		GetDlgItem(IDC_GAME_COMBO)->EnableWindow(FALSE);
		GetDlgItem(IDC_PLAYERS_COMBO)->EnableWindow(FALSE);
		GetDlgItem(IDC_DRAGON_SPEED_CHECK)->EnableWindow(FALSE);
		GetDlgItem(IDC_DRAGON_FEAR_CHECK)->EnableWindow(FALSE);
		GetDlgItem(IDC_WAIT1_EDIT)->EnableWindow(FALSE);
		GetDlgItem(IDC_WAIT2_EDIT)->EnableWindow(FALSE);
		GetDlgItem(IDC_PLAY_BUTTON)->EnableWindow(FALSE);
	}


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
		if ((setup != NULL) && (setup->isGameSetup())) {
			int SCREEN_MARGIN = 10;
			const int SCREEN_TOP = 76;
			float scale = 1;
			int leftMargin = SCREEN_MARGIN;
			computeScale(SCREEN_TOP, SCREEN_MARGIN, &scale, &leftMargin);
			dc.StretchBlt(leftMargin, SCREEN_TOP, (int)(WinRect.right * scale), (int)(WinRect.bottom * scale), pInMemDC, 0, 0, WinRect.right, WinRect.bottom, SRCCOPY);
		}
	}
}

BOOL CH2HAdventureDlg::checkCanPlay() {
	xport = new UdpTransport();
	socket = new WinUdpSocket();
	xport->useSocket(socket);
	setup = new GameSetup(*client, *xport);

	// Read the command line and pass it in to setup.
	USES_CONVERSION;
	LPCWSTR wholeCommandLine = GetCommandLine();
	int argc = 0;
	LPWSTR* parsedCommandLine = CommandLineToArgvW(wholeCommandLine, &argc);
	char** argv = new char*[argc];
	for (int ctr = 0; ctr < argc; ++ctr) {
		const char* argStr = W2A(parsedCommandLine[ctr]);
		argv[ctr] = new char[strlen(argStr) + 1];
		strcpy(argv[ctr], argStr);
	}
	setup->setCommandLineArgs(argc - 1, argv + 1);
	for (int ctr = 0; ctr < argc; ++ctr) {
		delete[] argv[ctr];
	}
	delete[] argv;
	// TODO: Handle disabling play button
	bool canPlay = setup->checkAnnouncements();
	return canPlay;

}

void CH2HAdventureDlg::computeScale(int SCREEN_TOP, int SCREEN_MARGIN, float* scale, int* margin) {
	RECT rcClient;
	this->GetClientRect(&rcClient);

	int windowHeight = rcClient.bottom - rcClient.top - SCREEN_TOP - SCREEN_MARGIN;
	float heightRatio = windowHeight / (float)ADVENTURE_SCREEN_HEIGHT;

	int windowWidth = rcClient.right - rcClient.left - 2* SCREEN_MARGIN;
	float widthRatio = windowWidth / (float)ADVENTURE_SCREEN_WIDTH;

	float minRatio = (widthRatio < heightRatio ? widthRatio : heightRatio);
	*scale = (float)(minRatio < 1 ? 0.5 : floor(minRatio));
	*margin = (int)(windowWidth - (*scale * ADVENTURE_SCREEN_WIDTH))/2 + SCREEN_MARGIN;
}


void CH2HAdventureDlg::OnDraw(CDC* pDC) {
	HPEN penOld = (HPEN)::SelectObject(*pInMemDC, ::GetStockObject(NULL_PEN));

	for (int pixel = 0; pixel < numPixels; ++pixel) {
		int row = pixel * 7;
		DrawPixel(pDC, pixelArray[row], pixelArray[row + 1], pixelArray[row + 2], pixelArray[row + 3], pixelArray[row + 4],
			pixelArray[row + 5], pixelArray[row + 6]);
	}
	numPixels = 0;
	reportedOverflow = false;

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
	reportedOverflow = false;

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

			Adventure_Setup(params.numberPlayers, params.thisPlayer, xport, params.gameLevel, 
				(params.diff1Switch ? DIFFICULTY_A : DIFFICULTY_B), (params.diff2Switch ? DIFFICULTY_A : DIFFICULTY_B));
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

		CEdit* nameLabel = (CEdit*)gThis->GetDlgItem(IDC_NAME_LABEL);
		nameLabel->ShowWindow(SW_HIDE);
		CEdit* waitLabel = (CEdit*)gThis->GetDlgItem(IDC_WAIT_LABEL);
		waitLabel->ShowWindow(SW_HIDE);
		CEdit* dontWaitLabel = (CEdit*)gThis->GetDlgItem(IDC_DONT_WAIT_LABEL);
		dontWaitLabel->ShowWindow(SW_HIDE);
		CEdit* nameEdit = (CEdit*)gThis->GetDlgItem(IDC_NAME_EDIT);
		nameEdit->ShowWindow(SW_HIDE);
		CComboBox* gameCombo = (CComboBox*)gThis->GetDlgItem(IDC_GAME_COMBO);
		gameCombo->ShowWindow(SW_HIDE);
		CComboBox* playersCombo = (CComboBox*)gThis->GetDlgItem(IDC_PLAYERS_COMBO);
		playersCombo->ShowWindow(SW_HIDE);
		CButton* dragonSpeedChk = (CButton*)gThis->GetDlgItem(IDC_DRAGON_SPEED_CHECK);
		dragonSpeedChk->ShowWindow(SW_HIDE);
		CButton* dragonFearChk = (CButton*)gThis->GetDlgItem(IDC_DRAGON_FEAR_CHECK);
		dragonFearChk->ShowWindow(SW_HIDE);
		CEdit* wait1Edit = (CEdit*)gThis->GetDlgItem(IDC_WAIT1_EDIT);
		wait1Edit->ShowWindow(SW_HIDE);
		CEdit* wait2Edit = (CEdit*)gThis->GetDlgItem(IDC_WAIT2_EDIT);
		wait2Edit->ShowWindow(SW_HIDE);
		CButton* playButton = (CButton*)gThis->GetDlgItem(IDC_PLAY_BUTTON);
		playButton->ShowWindow(SW_HIDE);
		gThis->GetDlgItem(IDC_MFCLINK1)->ShowWindow(SW_HIDE);
        gThis->GetDlgItem(IDC_MFCLINK2)->ShowWindow(SW_HIDE);
        gThis->GetDlgItem(IDC_MFCLINK3)->ShowWindow(SW_HIDE);

		USES_CONVERSION;
		WCHAR buffer[100];
		nameEdit->GetWindowTextW(buffer, 100);
		const char* name = W2A(buffer);
		setup->setPlayerName(name);
		int gameSelected = gameCombo->GetCurSel();
		gameSelected = (gameSelected < 0 ? 1 : gameSelected);
		setup->setGameLevel(gameSelected);
		int playersSelected = playersCombo->GetCurSel();
		playersSelected = (playersSelected < 0 ? 2 : playersSelected +2);
		setup->setNumberPlayers(playersSelected);
		setup->setDifficultySwitches(dragonSpeedChk->GetCheck() == BST_CHECKED,
			dragonFearChk->GetCheck() == BST_CHECKED);

		GameSetup::GameParams params = setup->getSetup();
		if (params.noTransport) {
			xport->useSocket(NULL);
			delete socket;
			socket = NULL;
		}
		//Adventure_Setup(2, 0, NULL, 0, 1, 1);
		DWORD timerId = ::timeSetEvent((UINT)(ADVENTURE_FRAME_PERIOD*1000), 1000, (LPTIMECALLBACK)TimerWindowProc, NULL, TIME_PERIODIC);
		gameStarted = TRUE;
	}
}

void Platform_ReadJoystick(bool* left, bool* up, bool* right, bool* down, bool* fire)
{
	if (left) *left = (GetAsyncKeyState(leftKey) & 0x8000) > 0;
	if (up) *up = (GetAsyncKeyState(upKey) & 0x8000) > 0;
	if (right) *right = (GetAsyncKeyState(rightKey) & 0x8000) > 0;
	if (down) *down = (GetAsyncKeyState(downKey) & 0x8000) > 0;
	if (fire) *fire = (GetAsyncKeyState(dropKey) & 0x8000) > 0;
}

void Platform_ReadConsoleSwitches(bool* reset)
{
	if (reset) *reset = (GetAsyncKeyState(resetKey) & 0x8000) > 0;
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
	if (row + 7 >= MAX_PIXELS) {
		if (!reportedOverflow) {
			Logger::logError("Too many pixels to paint.");
			reportedOverflow = true;
		}
	}
	else {
		pixelArray[row] = r;
		pixelArray[row + 1] = g;
		pixelArray[row + 2] = b;
		pixelArray[row + 3] = x;
		pixelArray[row + 4] = y;
		pixelArray[row + 5] = width;
		pixelArray[row + 6] = height;
		++numPixels;
	}
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

void Platform_ReportToServer(const char* message) {
    setup->reportToServer(message);
}

void CH2HAdventureDlg::OnCbnSelchangePlayersCombo()
{
	CComboBox* playersCombo = (CComboBox*)this->GetDlgItem(IDC_PLAYERS_COMBO);
	int playersSelected = playersCombo->GetCurSel();
	playersSelected = (playersSelected < 0 ? 2 : playersSelected + 2);
	CEdit* wait2Edit = (CEdit*)this->GetDlgItem(IDC_WAIT2_EDIT);
	wait2Edit->ShowWindow(playersSelected == 2 ? SW_HIDE: SW_SHOW);
}
