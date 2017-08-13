
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


CH2HAdventureDlg::CH2HAdventureDlg(CWnd* pParent /*=NULL*/)
	: CDialogEx(IDD_H2HADVENTURE_DIALOG, pParent)
{
	m_hIcon = AfxGetApp()->LoadIcon(IDR_MAINFRAME);
	pBitmap = NULL;
	pInMemDC = NULL;
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
		HPEN penOld = (HPEN)::SelectObject(*pInMemDC, ::GetStockObject(NULL_PEN));

		int SIZE = 8;
		int H_PIXEL = SCREEN_WIDTH / SIZE;
		int V_PIXEL = SCREEN_HEIGHT / SIZE;
		for (int xctr = 0; xctr < SIZE; ++xctr) {
			for (int yctr = 0; yctr < SIZE; ++yctr) {
				HBRUSH newBrush = (HBRUSH)::CreateSolidBrush(RGB(gBrightness * xctr / SIZE, gBrightness * yctr / SIZE, gBrightness));
				HBRUSH oldBrush = (HBRUSH)::SelectObject(*pInMemDC, newBrush);

				::Rectangle(*pInMemDC, xctr * H_PIXEL, yctr * V_PIXEL, (xctr+1) * H_PIXEL+1, (yctr + 1) * V_PIXEL+1);

				::SelectObject(*pInMemDC, oldBrush);
				::DeleteObject(newBrush);
			}
		}

		::SelectObject(*pInMemDC, penOld);

		// Copy bitmap to window
		dc.BitBlt(10, 10, WinRect.right + 10, WinRect.bottom + 10, pInMemDC, 0, 0, SRCCOPY);

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
	gBrightness = (gBrightness < 255 ? gBrightness + 1 : 0);
	gThis->Invalidate(FALSE);
}



void CH2HAdventureDlg::OnBnClickedPlayButton()
{
	// TODO: Add your control notification handler code here
	gThis = this;
	DWORD timerId = ::timeSetEvent(16, 1000, (LPTIMECALLBACK)TimerWindowProc, NULL, TIME_PERIODIC);
}
