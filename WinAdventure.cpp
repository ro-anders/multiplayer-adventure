//
// Adventure: Revisited
// C++ Version Copyright © 2006 Peter Hirschberg
// peter@peterhirschberg.com
// http://peterhirschberg.com
//
// Big thanks to Joel D. Park and others for annotating the original Adventure decompiled assembly code.
// I relied heavily and deliberately on that commented code.
//
// Original Adventure™ game Copyright © 1980 ATARI, INC.
// Any trademarks referenced herein are the property of their respective holders.
// 
// Original game written by Warren Robinett. Warren, you rock.
//


#include "stdafx.h"
#include "WinAdventure.h"
#include <mmsystem.h>
#include <stdlib.h>
#include <stdio.h>
#include <time.h>


#define MAX_LOADSTRING 100

#include "adventure.h"


// Global Variables:
HINSTANCE hInst;                                // current instance
TCHAR szTitle[MAX_LOADSTRING];                  // The title bar text
TCHAR szWindowClass[MAX_LOADSTRING];            // the main window class name

HWND gWnd = NULL;
HDC gDC = NULL;
int gWindowSizeX = 0;
int gWindowSizeY = 0;
float gGfxScaler = 0;

int leftDifficulty=DIFFICULTY_B, rightDifficulty=DIFFICULTY_B;

UINT *bitsOffscreen = NULL;
HBITMAP bmpOffscreen = NULL;

HBITMAP CreateOffscreen();

#ifdef _DEBUG
DWORD frameTimer = 0;
DWORD frameCount = 0;
#endif


// Forward declarations of functions included in this code module:
ATOM                MyRegisterClass(HINSTANCE hInstance);
BOOL                InitInstance(HINSTANCE, int);
LRESULT CALLBACK    WndProc(HWND, UINT, WPARAM, LPARAM);


typedef void (CALLBACK TIMECALLBACK)(UINT uTimerID, UINT uMsg, DWORD_PTR dwUser, DWORD_PTR dw1, DWORD_PTR dw2);
typedef TIMECALLBACK FAR *LPTIMECALLBACK; 
void CALLBACK TimerWindowProc(UINT uTimerID, UINT uMsg, DWORD_PTR dwUser, DWORD_PTR dw1, DWORD_PTR dw2);

void SetFullscreen(BOOL aFullscreen);
DEVMODE previousDisplayMode = {0};
BOOL gFullscreen = FALSE;
WINDOWPLACEMENT previousWindowPlacement = {0};

int APIENTRY _tWinMain(HINSTANCE hInstance,
                     HINSTANCE hPrevInstance,
                     LPTSTR    lpCmdLine,
                     int       nCmdShow)
{
    // TODO: Place code here.
    MSG msg;
    HACCEL hAccelTable;

    // Initialize global strings
    LoadString(hInstance, IDS_APP_TITLE, szTitle, MAX_LOADSTRING);
    LoadString(hInstance, IDC_WINADVENTURE, szWindowClass, MAX_LOADSTRING);
    MyRegisterClass(hInstance);

    // Perform application initialization:
    if (!InitInstance (hInstance, nCmdShow)) 
    {
        return FALSE;
    }

    hAccelTable = LoadAccelerators(hInstance, (LPCTSTR)IDC_WINADVENTURE);

    // Read the INI file
    {
        char szModule[MAX_PATH];
        char szDrive[MAX_PATH];
        char szDir[MAX_PATH];
        char szINIPath[MAX_PATH];

        GetModuleFileName(NULL, szModule, MAX_PATH);
        _splitpath(szModule, szDrive, szDir, NULL, NULL);
        wsprintf(szINIPath, "%s%sadventure.ini", szDrive, szDir);

        char szValueLeft[256]={0}, szValueRight[256]={0};
        GetPrivateProfileString("DIFFICULTY_SWITCHES", "LEFT", "B", szValueLeft, 256, szINIPath);
        GetPrivateProfileString("DIFFICULTY_SWITCHES", "RIGHT", "B", szValueRight, 256, szINIPath);

        leftDifficulty = (szValueLeft[0] == 'A') ? DIFFICULTY_A : DIFFICULTY_B;
        rightDifficulty = (szValueRight[0] == 'A') ? DIFFICULTY_A : DIFFICULTY_B;
    }

    // Using the C runtime random functions
    // Seed the random-number generator with current time so that
    // the numbers will be different every time we run.
    //
    srand( (unsigned)time( NULL ) );

    // Start the timer
    DWORD timerId = ::timeSetEvent(1000/ADVENTURE_FPS, 1000/ADVENTURE_FPS, (LPTIMECALLBACK)TimerWindowProc, NULL, TIME_PERIODIC);

    // Main message loop:
    while (GetMessage(&msg, NULL, 0, 0)) 
    {
        if (!TranslateAccelerator(msg.hwnd, hAccelTable, &msg)) 
        {
            TranslateMessage(&msg);
            DispatchMessage(&msg);
        }
    }

   SetFullscreen(FALSE);

    ::timeKillEvent(timerId);

    ::DeleteObject(bmpOffscreen);
    bmpOffscreen=NULL;

    return (int) msg.wParam;
}



//
//  FUNCTION: MyRegisterClass()
//
//  PURPOSE: Registers the window class.
//
//  COMMENTS:
//
//    This function and its usage are only necessary if you want this code
//    to be compatible with Win32 systems prior to the 'RegisterClassEx'
//    function that was added to Windows 95. It is important to call this function
//    so that the application will get 'well formed' small icons associated
//    with it.
//
ATOM MyRegisterClass(HINSTANCE hInstance)
{
    WNDCLASSEX wcex;

    wcex.cbSize = sizeof(WNDCLASSEX); 

    wcex.style          = CS_HREDRAW | CS_VREDRAW;
    wcex.lpfnWndProc    = (WNDPROC)WndProc;
    wcex.cbClsExtra     = 0;
    wcex.cbWndExtra     = 0;
    wcex.hInstance      = hInstance;
    wcex.hIcon          = LoadIcon(hInstance, (LPCTSTR)IDI_WINADVENTURE);
    wcex.hCursor        = LoadCursor(NULL, IDC_ARROW);
    wcex.hbrBackground  = (HBRUSH)(COLOR_WINDOW+1);
    wcex.lpszMenuName   = NULL;//(LPCTSTR)IDC_WINADVENTURE;
    wcex.lpszClassName  = szWindowClass;
    wcex.hIconSm        = LoadIcon(wcex.hInstance, (LPCTSTR)IDI_SMALL);

    return RegisterClassEx(&wcex);
}

//
//   FUNCTION: InitInstance(HANDLE, int)
//
//   PURPOSE: Saves instance handle and creates main window
//
//   COMMENTS:
//
//        In this function, we save the instance handle in a global variable and
//        create and display the main program window.
//
BOOL InitInstance(HINSTANCE hInstance, int nCmdShow)
{
   hInst = hInstance; // Store instance handle in our global variable

   gWnd = CreateWindow(szWindowClass, szTitle, WS_OVERLAPPEDWINDOW,
      400, 250, 712, 484, NULL, NULL, hInstance, NULL);
   
   if (!gWnd)
   {
      return FALSE;
   }

   CreateOffscreen();

   //SetFullscreen(TRUE);

   ShowWindow(gWnd, nCmdShow);
   UpdateWindow(gWnd);

   return TRUE;
}

//
//  FUNCTION: WndProc(HWND, unsigned, WORD, LONG)
//
//  PURPOSE:  Processes messages for the main window.
//
//  WM_COMMAND  - process the application menu
//  WM_PAINT    - Paint the main window
//  WM_DESTROY  - post a quit message and return
//
//
LRESULT CALLBACK WndProc(HWND hWnd, UINT message, WPARAM wParam, LPARAM lParam)
{
    int wmId, wmEvent;

    switch (message) 
    {
        case WM_COMMAND:
            wmId    = LOWORD(wParam); 
            wmEvent = HIWORD(wParam); 
            // Parse the menu selections:
            switch (wmId)
            {
                case IDM_EXIT:
                    DestroyWindow(hWnd);
                    break;
                default:
                    return DefWindowProc(hWnd, message, wParam, lParam);
            }
            break;

        case WM_PAINT:
            {
                PAINTSTRUCT ps;
                HDC dc = BeginPaint(hWnd, &ps);
                EndPaint(hWnd, &ps);
            }
            break;

        case WM_SIZE:
            {
                gWindowSizeX = LOWORD(lParam);
                gWindowSizeY = HIWORD(lParam);

                // Find the best scale for this resolution
                int x = gWindowSizeX / ADVENTURE_SCREEN_WIDTH;
                int y = gWindowSizeY / ADVENTURE_SCREEN_HEIGHT;

                gGfxScaler = min(x, y);
                gGfxScaler = (gGfxScaler == 0) ? .5 : gGfxScaler;

                char s[2055];
                sprintf(s, "%d x %d -> %f\n", gWindowSizeX, gWindowSizeY, gGfxScaler);
                OutputDebugString(s);
    
                CreateOffscreen();

            }
            break;

        case WM_ERASEBKGND:

            {
                HDC dc = ::GetDC(hWnd);
                if (dc)
                {
                    RECT rectClient;
                    ::GetClientRect(hWnd, &rectClient);

                    HBRUSH oldBrush = (HBRUSH)::SelectObject(dc, ::GetStockObject(BLACK_BRUSH));

                    if (!gFullscreen)
                    {
                        int cx = (gWindowSizeX/2) - ((ADVENTURE_SCREEN_WIDTH * gGfxScaler)/2);
                        int cy = (gWindowSizeY/2) - ((ADVENTURE_SCREEN_HEIGHT * gGfxScaler)/2);
                        int cw = ADVENTURE_SCREEN_WIDTH * gGfxScaler;
                        int ch = ADVENTURE_SCREEN_HEIGHT * gGfxScaler;

                        ::ExcludeClipRect(dc, cx, cy, cx+cw, cy+ch);
                    }

                    ::Rectangle(dc, rectClient.left, rectClient.top, rectClient.right, rectClient.bottom);
                    ::SelectObject(dc, oldBrush);

                    ::ReleaseDC(hWnd, dc);
                }
            }
            break;

        case WM_DESTROY:
            PostQuitMessage(0);
            break;

        case WM_SYSKEYDOWN:
            if (wParam == VK_RETURN)
                SetFullscreen(!gFullscreen);
            // fall through

        default:
            return DefWindowProc(hWnd, message, wParam, lParam);
    }
    return 0;
}

void CALLBACK TimerWindowProc(UINT uTimerID, UINT uMsg, DWORD_PTR dwUser,
    DWORD_PTR dw1, DWORD_PTR dw2)
{
    HDC winDC = ::GetDC(gWnd);
    gDC = ::CreateCompatibleDC(winDC);

    HBITMAP bmpOld = (HBITMAP)::SelectObject(gDC, bmpOffscreen);
    HPEN penOld = (HPEN)::SelectObject(gDC, ::GetStockObject(NULL_PEN));

    Adventure_Run();

    int cx = (gWindowSizeX/2) - ((ADVENTURE_SCREEN_WIDTH * gGfxScaler)/2);
    int cy = (gWindowSizeY/2) - ((ADVENTURE_SCREEN_HEIGHT * gGfxScaler)/2);
    int cw = ADVENTURE_SCREEN_WIDTH * gGfxScaler;
    int ch = ADVENTURE_SCREEN_HEIGHT * gGfxScaler;

    ::BitBlt(winDC, cx, cy, cw, ch, gDC, 0, 0, SRCCOPY);

    ::SelectObject(gDC, penOld);
    ::SelectObject(gDC, bmpOld);

    ::DeleteDC(gDC);
    ::ReleaseDC(gWnd, winDC);
    gDC = NULL;

    // Calculate FPS
#if _DEBUG
    DWORD timeNew = GetTickCount();
    DWORD ms = timeNew - frameTimer;
    if (ms >= 1000)
    {
        char str[32];
        wsprintf(str, "FPS: %dms", frameCount);
        ::SetWindowText(gWnd, str);
        frameCount = 0;
        frameTimer = timeNew;
    }
    else ++frameCount;
#endif
}

HBITMAP CreateOffscreen()
{
    if (bmpOffscreen)
    {
        ::DeleteObject(bmpOffscreen);
        bmpOffscreen=NULL;
    }

    HDC hdc = ::GetDC(gWnd);
    if (hdc)
    {
        BITMAPINFO bmInfo; 
        ZeroMemory(&bmInfo, sizeof(BITMAPINFO));
        bmInfo.bmiHeader.biSize = sizeof(BITMAPINFOHEADER);
        bmInfo.bmiHeader.biWidth = ADVENTURE_SCREEN_WIDTH * gGfxScaler;
        bmInfo.bmiHeader.biHeight = -((LONG)ADVENTURE_TOTAL_SCREEN_HEIGHT * gGfxScaler);
        bmInfo.bmiHeader.biPlanes = 1;
        bmInfo.bmiHeader.biBitCount = 32;

        bitsOffscreen = NULL;
        bmpOffscreen = ::CreateDIBSection(hdc, &bmInfo, DIB_RGB_COLORS, (void**)&bitsOffscreen, NULL, 0);
    }
    return bmpOffscreen;
}


void SetFullscreen(BOOL aFullscreen)
{
    if (aFullscreen != gFullscreen)
    {
        if (aFullscreen)
        {
            previousDisplayMode.dmSize = sizeof(DEVMODE);

            if (EnumDisplaySettings(NULL, ENUM_CURRENT_SETTINGS, &previousDisplayMode))
            {
                // Save the current window placement
                previousWindowPlacement.length = sizeof(WINDOWPLACEMENT);
                ::GetWindowPlacement(gWnd, &previousWindowPlacement);

                BOOL foundMode = FALSE;
                DEVMODE newDisplayMode;
                int mode = 0;
                while (1)
                {
                    if (!::EnumDisplaySettings(NULL, mode, &newDisplayMode))
                        break;

                    if (newDisplayMode.dmBitsPerPel > 8)
                    {
                        if (newDisplayMode.dmPelsWidth>=320 && newDisplayMode.dmPelsHeight>=200)
                        {
                            foundMode = TRUE;
                            break;
                        }
                    }

                    ++mode;
                }

                if (foundMode)
                {
                    char s[1024];
                    sprintf(s, "mode = %d (%d x %d, %dbpp\n", mode, newDisplayMode.dmPelsWidth, newDisplayMode.dmPelsHeight, newDisplayMode.dmBitsPerPel);
                    OutputDebugString(s);

                    if (::ChangeDisplaySettings(&newDisplayMode, 0) == DISP_CHANGE_SUCCESSFUL)
                    {
                        OutputDebugString("display change successful\n");
                        gFullscreen = TRUE;

                        ::SetWindowLong(gWnd, GWL_STYLE, WS_VISIBLE);
                        ::SetWindowPos(gWnd, HWND_TOPMOST, 0, 0, newDisplayMode.dmPelsWidth, newDisplayMode.dmPelsHeight, SWP_FRAMECHANGED);

                        UpdateWindow(gWnd);
                    }
                    else
                    {
                        OutputDebugString("display change failed\n");
                        ::ChangeDisplaySettings(&previousDisplayMode, 0);
                        gFullscreen = FALSE;
                    }

                }

            }

        }
        else
        {
            ::ChangeDisplaySettings(&previousDisplayMode, 0);
            //::SetWindowPos(gWnd, HWND_NOTOPMOST, 400, 250, 712, 484, 0);

            // Restore the window placement from last time
            ::SetWindowPlacement(gWnd, &previousWindowPlacement);

            ::SetWindowLong(gWnd, GWL_STYLE, WS_OVERLAPPEDWINDOW | WS_VISIBLE);
            ::SetWindowPos(gWnd, NULL, 0,0,0,0, SWP_NOMOVE|SWP_NOSIZE|SWP_NOZORDER|SWP_FRAMECHANGED);
            UpdateWindow(gWnd);

            gFullscreen = FALSE;
        }
    }
}

void Platform_ReadJoystick(bool* left, bool* up, bool* right, bool* down, bool* fire)
{
    if (left) *left = GetAsyncKeyState(VK_LEFT) & 0x8000;
    if (up) *up = GetAsyncKeyState(VK_UP) & 0x8000;
    if (right) *right = GetAsyncKeyState(VK_RIGHT) & 0x8000;
    if (down) *down = GetAsyncKeyState(VK_DOWN) & 0x8000;
    if (fire) *fire = (GetAsyncKeyState(VK_SPACE) & 0x8000) || (GetAsyncKeyState(VK_LCONTROL) & 0x8000);
}

void Platform_ReadConsoleSwitches(bool* select, bool* reset)
{
    if (select) *select = GetAsyncKeyState('1') & 0x8000;
    if (reset) *reset = GetAsyncKeyState('2') & 0x8000;
}

void Platform_ReadDifficultySwitches(int* left, int* right)
{
    *left = leftDifficulty;
    *right = rightDifficulty;
}

void Platform_PaintPixel(int r, int g, int b, int x, int y, int width/*=1*/, int height/*=1*/)
{
    if (gDC)
    {
        HBRUSH newBrush = (HBRUSH)::CreateSolidBrush(RGB(r,g,b));
        HBRUSH oldBrush = (HBRUSH)::SelectObject(gDC, newBrush);

        // The game expects a bottom up buffer, so we flip the orientation here
        y = (ADVENTURE_SCREEN_HEIGHT - y) + ADVENTURE_OVERSCAN;

        x *= gGfxScaler;
        y *= gGfxScaler;
        width *= gGfxScaler;
        height *= gGfxScaler;
        ::Rectangle(gDC, x, y - height, x + width+1, y+1);

        ::SelectObject(gDC, oldBrush);
        ::DeleteObject(newBrush);
    }
}

void Platform_MakeSound(int sound)
{
    char szModule[MAX_PATH];
    char szDrive[MAX_PATH];
    char szDir[MAX_PATH];
    char szSoundPath[MAX_PATH];

    GetModuleFileName(NULL, szModule, MAX_PATH);
    _splitpath(szModule, szDrive, szDir, NULL, NULL);

    switch (sound)
    {
        case SOUND_PICKUP:
            wsprintf(szSoundPath, "%s%ssounds\\pickup.wav", szDrive, szDir);
            break;
        case SOUND_PUTDOWN:
            wsprintf(szSoundPath, "%s%ssounds\\putdown.wav", szDrive, szDir);
            break;
        case SOUND_WON:
            wsprintf(szSoundPath, "%s%ssounds\\won.wav", szDrive, szDir);
            break;
        case SOUND_ROAR:
            wsprintf(szSoundPath, "%s%ssounds\\roar.wav", szDrive, szDir);
            break;
        case SOUND_EATEN:
            wsprintf(szSoundPath, "%s%ssounds\\eaten.wav", szDrive, szDir);
            break;
        case SOUND_DRAGONDIE:
            wsprintf(szSoundPath, "%s%ssounds\\dragondie.wav", szDrive, szDir);
            break;
    }

    sndPlaySound(szSoundPath, SND_ASYNC | SND_NODEFAULT);
}

float Platform_Random()
{
    // Using the C runtime random functions
    return (float)rand() / RAND_MAX;
}





