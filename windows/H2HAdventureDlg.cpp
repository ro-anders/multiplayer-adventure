
// H2HAdventureDlg.cpp : implementation file
//

#include "stdafx.h"
#include "H2HAdventure.h"
#include "H2HAdventureDlg.h"
#include "afxdialogex.h"

#ifdef _DEBUG
#define new DEBUG_NEW
#endif

#include <mmsystem.h>
typedef void (CALLBACK TIMECALLBACK)(UINT uTimerID, UINT uMsg, DWORD_PTR dwUser, DWORD_PTR dw1, DWORD_PTR dw2);
typedef TIMECALLBACK FAR *LPTIMECALLBACK;
void CALLBACK TimerWindowProc(UINT uTimerID, UINT uMsg, DWORD_PTR dwUser, DWORD_PTR dw1, DWORD_PTR dw2);

// CH2HAdventureDlg dialog
static CDialogEx* gThis = NULL;
static int gBrightness = 0;
static int SCREEN_HEIGHT = 224;
static int SCREEN_WIDTH = 320;

int pixelArray[7000];
int numPixels = 0;


CH2HAdventureDlg::CH2HAdventureDlg(CWnd* pParent /*=NULL*/)
	: CDialogEx(IDD_H2HADVENTURE_DIALOG, pParent)
{
	m_hIcon = AfxGetApp()->LoadIcon(IDR_MAINFRAME);
	pBitmap = NULL;
	pInMemDC = NULL;
	pixelArray[0] = -1;

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
		CRect WinRect(0, 0, SCREEN_WIDTH, SCREEN_HEIGHT);
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

		/*
		// The game expects a bottom up buffer, so we flip the orientation here
		y = (ADVENTURE_SCREEN_HEIGHT - y) + ADVENTURE_OVERSCAN;

		x *= gGfxScaler;
		y *= gGfxScaler;
		width *= gGfxScaler;
		height *= gGfxScaler;
		*/

		HBRUSH newBrush = (HBRUSH)::CreateSolidBrush(RGB(r, g, b));
		HBRUSH oldBrush = (HBRUSH)::SelectObject(*pInMemDC, newBrush);

		::Rectangle(*pInMemDC, x, y, x + width + 1, y + width + 1);

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

void CALLBACK TimerWindowProc(UINT uTimerID, UINT uMsg, DWORD_PTR dwUser,
	DWORD_PTR dw1, DWORD_PTR dw2)
{
	numPixels = 0;
	pixelArray[0] = -1;

	gBrightness = (gBrightness < 255 ? gBrightness + 1 : 0);
	int SIZE = 8;
	int H_PIXEL = SCREEN_WIDTH / SIZE;
	int V_PIXEL = SCREEN_HEIGHT / SIZE;
	for (int xctr = 0; xctr < SIZE; ++xctr) {
		for (int yctr = 0; yctr < SIZE; ++yctr) {
			Platform_PaintPixel(gBrightness * xctr / SIZE, gBrightness * yctr / SIZE, gBrightness, xctr * H_PIXEL, yctr * V_PIXEL, H_PIXEL, V_PIXEL);
		}
	}

	gThis->Invalidate(FALSE);
}



void CH2HAdventureDlg::OnBnClickedPlayButton()
{
	// TODO: Add your control notification handler code here
	gThis = this;
	DWORD timerId = ::timeSetEvent(16, 1000, (LPTIMECALLBACK)TimerWindowProc, NULL, TIME_PERIODIC);
}
