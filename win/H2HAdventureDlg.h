
// H2HAdventureDlg.h : header file
//

#pragma once


// CH2HAdventureDlg dialog
class CH2HAdventureDlg : public CDialogEx
{
// Construction
public:
	CH2HAdventureDlg(CWnd* pParent = NULL);	// standard constructor
	virtual  ~CH2HAdventureDlg();

	virtual void OnDraw(CDC* pDC);  // overridden to draw this view

// Dialog Data
#ifdef AFX_DESIGN_TIME
	enum { IDD = IDD_H2HADVENTURE_DIALOG };
#endif

	protected:
	virtual void DoDataExchange(CDataExchange* pDX);	// DDX/DDV support
	void DrawPixel(CDC* pDC, int r, int g, int b, int x, int y, int width, int height);

// Implementation
protected:
	HICON m_hIcon;
	CDC*     m_pdcMemory;
	CBitmap* m_pBitmap;

	bool gameStarted;

	// Generated message map functions
	virtual BOOL OnInitDialog();
	afx_msg void OnPaint();
	afx_msg HCURSOR OnQueryDragIcon();
	DECLARE_MESSAGE_MAP()
public:
	afx_msg void OnBnClickedPlayButton();
};
