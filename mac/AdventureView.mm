//
// Adventure: Revisited
// C++ Version Copyright © 2007 Peter Hirschberg
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

#include <sys/time.h>

#include "adventure_sys.h"
#include "AdventureView.h"
#include "Adventure.h"
#include "args.h"
#include "GameSetup.hpp"
#include "Logger.hpp"
#include "MacRestClient.hpp"
#include "PosixUdpSocket.hpp"
#include "Sys.hpp"
#include "Transport.hpp"
#include "UdpTransport.hpp"



void FreeOffscreen();

short GetKeyState(unsigned short k);

float gGfxScaler = 1.0f;
byte* gPixelBucket = NULL;
CGContextRef gDC = NULL;

unsigned char mKeyMap = 0;

time_t mDisplayStatusExpiration = -1;
UdpTransport* xport = NULL;
PosixUdpSocket* xportSocket = NULL;
MacRestClient client;

GameSetup* setup;

// Flag to ignore key up events so that we can lock keys
bool lockKeys = false;

#define KEY_LEFT	0x01
#define KEY_UP		0x02
#define KEY_RIGHT	0x04
#define KEY_DOWN	0x08
#define KEY_FIRE	0x10
#define KEY_RESET	0x20

bool gLeftDifficulty = TRUE;	// true = dragons pause before eating you
bool gRightDifficulty = TRUE;	// true = dragons run from the sword
bool gMenuItemReset = FALSE;
bool gMenuItemSelect = FALSE;
AdventureView* gView = NULL;

bool gMute = FALSE;


// *******************************************************************************************
// Our NSView interface
// *******************************************************************************************

@implementation AdventureView

- (id)initWithFrame:(NSRect)frameRect
{
    [super initWithFrame:frameRect];
    
    gView = self;

    // Randomize the random number generator
    timeval time;
    gettimeofday(&time, NULL);
    long millis = (time.tv_sec * 1000) + (time.tv_usec / 1000);
    srandom(millis);
    
    // Setup logging
    Logger::setup(Logger::FILE, Logger::INFO);
    Logger::log() << "Logging setup at " << Sys::datetime() << "." << Logger::EOM;
    
    return self;
}

- (bool) checkCanPlay
{
    // Check with the broker for announcements
    xport = new UdpTransport();
    xportSocket = new PosixUdpSocket();
    xport->useSocket(xportSocket);
    setup = new GameSetup(client, *xport);
    int argc;
    char** argv;
    Args_GetArgs(&argc, &argv);
    setup->setCommandLineArgs(argc-1, argv+1);
    // TODO: Handle disabling play button
    bool canPlay = setup->checkAnnouncements();
    return canPlay;
}

- (void) dealloc
{
    [timer invalidate];
    
    FreeOffscreen();
    
    [super dealloc];
}

- (IBAction)clickAnnouncementLink:(id)sender {
    [[NSWorkspace sharedWorkspace] openURL:[NSURL URLWithString:[mAnnouncementLink title]]];
}

- (bool)CreateOffscreen
{
    NSRect rectWindow = [self bounds];
    
    // Find the best scale for this resolution
    int sx = rectWindow.size.width / ADVENTURE_SCREEN_WIDTH;
    int sy = rectWindow.size.height / ADVENTURE_SCREEN_HEIGHT;
    gGfxScaler = (sx < sy) ? sx : sy; // min(sx, sy);
    gGfxScaler = (gGfxScaler == 0) ? .5 : gGfxScaler;
    int aWidth = ADVENTURE_SCREEN_WIDTH;
    int aHeight = ADVENTURE_SCREEN_HEIGHT;
    
    
    if (gPixelBucket)
    {
        delete gPixelBucket;
        gPixelBucket = NULL;
    }
    if (gDC)
    {
        CGContextRelease(gDC);
        gDC = NULL;
    }

    size_t rowBytes = aWidth * 4;
    
    gPixelBucket = new byte [rowBytes * aHeight];
    CGBitmapInfo info = kCGImageAlphaNoneSkipFirst | kCGBitmapByteOrder32Big;
    
    CGColorSpaceRef colorspace = CGColorSpaceCreateDeviceRGB();
    
    gDC = CGBitmapContextCreate(gPixelBucket, aWidth, aHeight, 8, rowBytes, colorspace, info);
    return gDC ? TRUE : FALSE;
}


- (void)viewDidEndLiveResize
{
    [self CreateOffscreen];
}

- (IBAction)update:(id)sender
{
    static bool isGraphicsSetup = false;
    
    // Dismiss current display message when it is time
    if (mDisplayStatusExpiration >= 0) {
        time_t currentTime = time(NULL);
        if (currentTime > mDisplayStatusExpiration) {
           [mStatusMessage setHidden:YES];
            mDisplayStatusExpiration = -1;
        }
    }
    
    if (!setup->isGameSetup()) {
        setup->checkSetup();
        if (setup->isGameSetup()) {
            GameSetup::GameParams params = setup->getSetup();
            if (params.noTransport) {
                delete xport;
                xport = NULL;
            }   
            Platform_MuteSound(params.shouldMute);
            
            if ([self CreateOffscreen])
            {
                Adventure_Setup(params.numberPlayers, params.thisPlayer, xport, params.gameLevel,
                                (params.diff1Switch ? DIFFICULTY_A : DIFFICULTY_B),
                                (params.diff2Switch ? DIFFICULTY_A : DIFFICULTY_B));
                isGraphicsSetup = true;
            }
            // TODO: What if CreateOffscreen fails.  Silently stops working.
        }
    } else if (isGraphicsSetup) {
        // Run a frame of the game
        Adventure_CheckTime(gGfxScaler);
        Adventure_Run();
    }
    
    
    
    // Display it
    [self setNeedsDisplay:YES];
}

-(void)displayStatus:(const char*)message :(int)durationSec
{
    NSString *nsstring = [NSString stringWithUTF8String:message];
    [mStatusMessage setStringValue:nsstring];
    [mStatusMessage setHidden:NO];
    mDisplayStatusExpiration = (durationSec >= 0 ? time(NULL) + durationSec : -1);
}

-(void)displayAnnouncement:(const char*)announcement :(const char*)link
{
    NSString *nsstring1 = [NSString stringWithUTF8String:announcement];
    [mAnnouncementMessage setStringValue:nsstring1];
    [mAnnouncementMessage setHidden:NO];

    NSString *nsstring2 = [NSString stringWithUTF8String:link];
    [mAnnouncementLink setTitle:nsstring2];
    NSColor *linkColor = [NSColor blueColor];
    
    NSMutableAttributedString *colorTitle = [[NSMutableAttributedString alloc] initWithAttributedString:[mAnnouncementLink attributedTitle]];
    NSRange titleRange = NSMakeRange(0, [colorTitle length]);
    [colorTitle addAttribute:NSForegroundColorAttributeName value:linkColor range:titleRange];
    [colorTitle addAttribute:NSUnderlineStyleAttributeName value:@(NSUnderlineStyleSingle) range:titleRange];
    [mAnnouncementLink setAttributedTitle:colorTitle];
    [mAnnouncementLink setHidden:NO];
}


- (void)playGame:(NSString*)playerName :(int)gameNum :(int)desiredPlayers :(bool)diff1Switch :(bool)diff2Switch
                :(NSString*)waitFor1 :(NSString*)waitFor2
{
    [mAnnouncementMessage setHidden:YES];
    [mAnnouncementLink setHidden:YES];
    GameSetup::GameParams params;
    try {
        setup->setPlayerName([playerName UTF8String]);
        setup->setGameLevel(gameNum);
        setup->setDifficultySwitches(diff1Switch, diff2Switch);
        setup->setNumberPlayers(desiredPlayers);
        setup->addPlayerToWaitFor([waitFor1 UTF8String]);
        if (desiredPlayers > 2) {
            setup->addPlayerToWaitFor([waitFor2 UTF8String]);
        }
        GameSetup::GameParams params = setup->getSetup();
        if (params.noTransport) {
            xport->useSocket(NULL);
            delete xportSocket;
            xportSocket = NULL;
        }
        timer = [NSTimer scheduledTimerWithTimeInterval: ADVENTURE_FRAME_PERIOD
                                                 target: self
                                               selector: @selector(update:)
                                               userInfo: nil
                                                repeats: YES];

    } catch (std::exception& e) {
        Logger::logError() << e.what() << "\nAborting." << Logger::EOM;
        exit(-1);
    } catch (...) {
        Logger::logError("Unexpected error.  Aborting.");
    }

}

- (BOOL)acceptsFirstResponder
{
    // Make it so we accept keyboard messages directed at the window
    return YES;
}

- (void)drawRect:(NSRect)rect
{
    if (gDC && (setup != NULL) && setup->isGameSetup())
    {
        NSRect rectWindow = [self bounds];
        
        // Set up a graphics context to draw to the window
        CGContextRef dc = (CGContextRef) [[NSGraphicsContext currentContext] graphicsPort];
        
        // Only use subsampling when scaling below 100%
        CGContextSetInterpolationQuality(dc, (gGfxScaler < 1) ? kCGInterpolationHigh : kCGInterpolationNone);
        
        // Center it within the window
        int cx = (rectWindow.size.width/2) - ((ADVENTURE_SCREEN_WIDTH * gGfxScaler)/2);
        int cy = (rectWindow.size.height/2) - ((ADVENTURE_SCREEN_HEIGHT * gGfxScaler)/2);
        int cw = ADVENTURE_SCREEN_WIDTH * gGfxScaler;
        int ch = ADVENTURE_SCREEN_HEIGHT * gGfxScaler;
        CGRect dstRect = CGRectMake(cx,cy,cw,ch);
        
        // Blit the backbuffer
        CGImageRef imgRef = CGBitmapContextCreateImage(gDC);
        CGContextDrawImage(dc, dstRect, imgRef);
        CGImageRelease(imgRef);
        
        // Draw the border stroke
        [[NSColor colorWithDeviceRed:0 green:0 blue:0 alpha:.3] set];
        NSBezierPath *borderPath= [[NSBezierPath alloc] init];
        [borderPath setLineWidth:0.4];
        [borderPath moveToPoint:NSMakePoint(cx, cy)];
        [borderPath lineToPoint:NSMakePoint(cx, cy+ch)];
        [borderPath lineToPoint:NSMakePoint(cx+cw, cy+ch)];
        [borderPath lineToPoint:NSMakePoint(cx+cw, cy)];
        [borderPath closePath];
        [borderPath stroke];
        
    }
}

- (void) keyDown:(NSEvent *) theEvent
{
    unsigned short key = [theEvent keyCode];
    switch(key)
    {
        case 0x33: // delete
            lockKeys = true;
            break;
        case 0x7e: //up arrow
            mKeyMap |= KEY_UP;
            break;
        case 0x7d: //down arrow
            mKeyMap |= KEY_DOWN;
            break;
        case 0x7b: //left arrow
            mKeyMap |= KEY_LEFT;
            break;
        case 0x7c: //right arrow
            mKeyMap |= KEY_RIGHT;
            break;
        case 0x31: //space bar
            mKeyMap |= KEY_FIRE;
            break;
        case 0x24: //return
            mKeyMap |= KEY_RESET;
            break;
    }
}

- (void) keyUp:(NSEvent *) theEvent
{
    unsigned short key = [theEvent keyCode];
    if (key == 0x33) { // delete
        lockKeys = false;
    } else if (!lockKeys) {
        switch(key)
        {
            case 0x7e: //up arrow
                mKeyMap &= ~KEY_UP;
                break;
            case 0x7d: //down arrow
                mKeyMap &= ~KEY_DOWN;
                break;
            case 0x7b: //left arrow
                mKeyMap &= ~KEY_LEFT;
                break;
            case 0x7c: //right arrow
                mKeyMap &= ~KEY_RIGHT;
                break;
            case 0x31: //space bar
                mKeyMap &= ~KEY_FIRE;
                break;
            case 0x24: //return
                mKeyMap &= ~KEY_RESET;
                break;
        }
    }
}

@end // AdventureView


// *******************************************************************************************
// Buffer stuff
// *******************************************************************************************


void FreeOffscreen()
{
    if (gDC)
    {
        CGContextRelease(gDC);
        gDC = NULL;
    }
    if (gPixelBucket)
    {
        delete gPixelBucket;
        gPixelBucket = NULL;
    }
}

// *******************************************************************************************
// Platform callbacks from game code
// *******************************************************************************************

void Platform_PaintPixel(int r, int g, int b, int x, int y, int width/*=1*/, int height/*=1*/)
{
    if (gPixelBucket)
    {
        int bufferWidth = ADVENTURE_SCREEN_WIDTH;
        int bufferHeight = ADVENTURE_SCREEN_HEIGHT;
        int bufferOverscan = ADVENTURE_OVERSCAN;
        
        for (int cy=0; cy<height; cy++)
        {
            // The game expects a bottom up buffer, so we flip the orientation here.
            // Also, the game actually draws more than would show on a TV screen, hence the adjustment for overscan
            
            int py = (bufferHeight - (y + cy)) + bufferOverscan;
            
            if ((py >= 0) && (py < bufferHeight))
            {
                for (int cx=0; cx<width; cx++)
                {
                    int px = cx + x;
                    byte* p = &gPixelBucket[(unsigned int)((py*bufferWidth) + px) * 4];
                    
                    if ((px >= 0) && (px < bufferWidth))
                    {
                        p++;	// skip alpha
                        *(p++) = r & 0xff;
                        *(p++) = g & 0xff;
                        *(p++) = b & 0xff;
                    }
                }
            }
            
        }
    }
}

void Platform_ReadJoystick(bool* left, bool* up, bool* right, bool* down, bool* fire)
{
    if (left) *left = mKeyMap & KEY_LEFT;
    if (up) *up =  mKeyMap & KEY_UP;
    if (right) *right =  mKeyMap & KEY_RIGHT;
    if (down) *down =  mKeyMap & KEY_DOWN;
    
    if (fire) *fire =  mKeyMap & KEY_FIRE;
}

void Platform_ReadConsoleSwitches(bool* reset)
{
    if (reset) *reset =  (mKeyMap & KEY_RESET) | gMenuItemReset;
    
    gMenuItemReset = FALSE;
}

void Platform_MuteSound(bool nMute)
{
    gMute = nMute;
}

void Platform_MakeSound(int nSound, float volume)
{
    NSSound* sound = NULL;
    
    if (!gMute && volume > 0) {
        switch (nSound)
        {
            case SOUND_PICKUP:
                sound = [NSSound soundNamed:@"pickup"];
                break;
            case SOUND_PUTDOWN:
                sound = [NSSound soundNamed:@"putdown"];
                break;
            case SOUND_WON:
                sound = [NSSound soundNamed:@"won"];
                break;
            case SOUND_ROAR:
                sound = [NSSound soundNamed:@"roar"];
                break;
            case SOUND_EATEN:
                sound = [NSSound soundNamed:@"eaten"];
                break;
            case SOUND_DRAGONDIE:
                sound = [NSSound soundNamed:@"dragondie"];
                break;
            case SOUND_GLOW:
                sound = [NSSound soundNamed:@"glow"];
                break;
        }
        
        if (sound)
        {
            [sound setVolume:volume/MAX_VOLUME];
            [sound play];
        }
    }
}

float Platform_Random()
{
    long r = random();
    float val = ((float)r/32767.0f);
    val = (val + 1) / 2;
    return val;
}

void Platform_DisplayStatus(const char* message, int durationSec) {
    [gView displayStatus:message :durationSec];
}

void Platform_DisplayAnnouncement(const char* announcement, const char* link) {
    [gView displayAnnouncement:announcement :link];
}


void Platform_ReportToServer(const char* message) {
    setup->reportToServer(message);
}





