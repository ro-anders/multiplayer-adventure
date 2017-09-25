
// H2HAdventureDlg.h : header file
//

#pragma once

class UdpTransport;
class RestClient;
class GameSetup;

// CH2HAdventureDlg dialog
class CH2HAdventureDlg : public CDialogEx
{
// Construction
public:
	CH2HAdventureDlg(CWnd* pParent = NULL);	// standard constructor

// Dialog Data
#ifdef AFX_DESIGN_TIME
	enum { IDD = IDD_H2HADVENTURE_DIALOG };
#endif

	protected:
	virtual void DoDataExchange(CDataExchange* pDX);	// DDX/DDV support


// Implementation
protected:
	HICON m_hIcon;
	CBitmap* pBitmap;
	CDC* pInMemDC;
	BOOL gameStarted;
	UdpTransport* xport;
	GameSetup* setup;
	RestClient* client;

	// Generated message map functions
	virtual BOOL OnInitDialog();
	afx_msg void OnPaint();
	void OnDraw(CDC* pDC);
	void DrawPixel(CDC* pDC, int r, int g, int b, int x, int y, int width, int height);
	afx_msg HCURSOR OnQueryDragIcon();
	DECLARE_MESSAGE_MAP()
public:
	void OnOK();
	void update();
	afx_msg void OnBnClickedPlayButton();
};
