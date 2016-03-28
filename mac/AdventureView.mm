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
#include "PosixTcpTransport.hpp"
#include "PosixUdpTransport.hpp"
#include "Transport.hpp"
#include "YTransport.hpp"

bool CreateOffscreen(int aWidth, int aHeight);
void FreeOffscreen();

short GetKeyState(unsigned short k);

float gGfxScaler = 2.0f;
byte* gPixelBucket = NULL;
CGContextRef gDC = NULL;

unsigned char mKeyMap = 0;

#define KEY_LEFT	0x01
#define KEY_UP		0x02
#define KEY_RIGHT	0x04
#define KEY_DOWN	0x08
#define KEY_FIRE	0x10
#define KEY_RESET	0x20
#define KEY_SELECT	0x40

bool gLeftDifficulty = TRUE;	// true = dragons pause before eating you
bool gRightDifficulty = TRUE;	// true = dragons run from the sword
bool gMenuItemReset = FALSE;
bool gMenuItemSelect = FALSE;

bool gMute = FALSE;


// *******************************************************************************************
// Our NSView interface
// *******************************************************************************************

@implementation AdventureView

- (id)initWithFrame:(NSRect)frameRect
{
	[super initWithFrame:frameRect];
    // Randomize the random number generator
    timeval time;
    gettimeofday(&time, NULL);
    long millis = (time.tv_sec * 1000) + (time.tv_usec / 1000);
    srandom(millis);
    
    // Expecting args: gameLevel playerNum sockAddress1 sockAddress2
    int argc;
    char** argv;
    Args_GetArgs(&argc, &argv);
    
    
    // Test UDP Sockets
    if ((argc > 2) && (strcmp(argv[1], "test")==0)) {
        Transport* toTest = NULL;
        if (argc == 2) {
            toTest = new PosixUdpTransport();
        } else {
            Transport::Address addr1 = Transport::parseUrl(argv[2]);
            Transport::Address addr2 = Transport::parseUrl(argv[3]);
            toTest = new PosixUdpTransport(addr1, addr2);
        }
        Transport::testTransport(*toTest);
    }

    int numPlayers;
    int thisPlayer;
    Transport* transport;
    
    int gameLevel = 1;
    if (argc > 2) {
        gameLevel = atoi(argv[1]);
    }
    
    // Read the command line arguments and setup the communication with the other players
    const int DEFAULT_PORT = 5678;
    if (argc <= 2) {
        numPlayers = 2;
        transport = new PosixUdpTransport();
        transport->connect();
        thisPlayer = transport->getTestSetupNumber();
        Platform_MuteSound(thisPlayer == 1);
    } else {
        numPlayers = argc-3;
        thisPlayer = atoi(argv[2])-1;
        Transport::Address addr0 = Transport::parseUrl(argv[3]);
        Transport::Address addr1 = Transport::parseUrl(argv[4]);
        transport = new PosixUdpTransport(addr0, addr1);
        
        // Process player 3
        Transport* transport2 = NULL;
        if (argc > 5) {
            Transport::Address addr2 = Transport::parseUrl(argv[5]);
            transport2 = new PosixUdpTransport(addr0, addr2);
            transport = new YTransport(transport, transport2);
        }
        transport->connect();
    }
    
	if (CreateOffscreen(ADVENTURE_SCREEN_WIDTH, ADVENTURE_SCREEN_HEIGHT))
	{
        Adventure_Setup(numPlayers, thisPlayer, transport, gameLevel, 0, 0);
		timer = [NSTimer scheduledTimerWithTimeInterval: 0.016
												 target: self
											   selector: @selector(update:)
											   userInfo: nil
												repeats: YES];
	}
	
    return self;
}

- (void) dealloc
{
	[timer invalidate];

	FreeOffscreen();
		
	[super dealloc];
}

- (void)viewDidEndLiveResize
{
	NSRect rectWindow = [self bounds];
	
	// Find the best scale for this resolution
	int sx = rectWindow.size.width / ADVENTURE_SCREEN_WIDTH;
	int sy = rectWindow.size.height / ADVENTURE_SCREEN_HEIGHT;
	gGfxScaler = (sx < sy) ? sx : sy; // min(sx, sy);
	gGfxScaler = (gGfxScaler == 0) ? .5 : gGfxScaler;
	
	CreateOffscreen(ADVENTURE_SCREEN_WIDTH * gGfxScaler, ADVENTURE_SCREEN_HEIGHT * gGfxScaler);
}

- (IBAction)update:(id)sender
{
	// Run a frame of the game
	Adventure_Run();

	// Display it
    [self setNeedsDisplay:YES];
}

- (BOOL)acceptsFirstResponder
{
	// Make it so we accept keyboard messages directed at the window
    return YES;
}

- (void)drawRect:(NSRect)rect
{
	if (gDC)
	{
		NSRect rectWindow = [self bounds];
		
		// Set up a graphics context to draw to the window
		CGContextRef dc = (CGContextRef) [[NSGraphicsContext currentContext] graphicsPort];

		// Find the best scale for this resolution
		int x = rectWindow.size.width / ADVENTURE_SCREEN_WIDTH;
		int y = rectWindow.size.height / ADVENTURE_SCREEN_HEIGHT;
		gGfxScaler = (x < y) ? x : y; // min(x, y);
		gGfxScaler = (gGfxScaler == 0) ? .5 : gGfxScaler;

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
        case 0x7e: //up
            mKeyMap |= KEY_UP;
            break;
        case 0x7d: //down
            mKeyMap |= KEY_DOWN;
            break;
        case 0x7b: //left
            mKeyMap |= KEY_LEFT;
            break;
        case 0x7c: //right
            mKeyMap |= KEY_RIGHT;
            break;
        case 0x31: //fire
            mKeyMap |= KEY_FIRE;
            break;
        case 0x12: //reset
            mKeyMap |= KEY_RESET;
            break;
        case 0x13: //select
            mKeyMap |= KEY_SELECT;
            break;
    }
}

- (void) keyUp:(NSEvent *) theEvent
{
    unsigned short key = [theEvent keyCode];
    switch(key)
    {
        case 0x7e: //up
            mKeyMap &= ~KEY_UP;
            break;
        case 0x7d: //down
            mKeyMap &= ~KEY_DOWN;
            break;
        case 0x7b: //left
            mKeyMap &= ~KEY_LEFT;
            break;
        case 0x7c: //right
            mKeyMap &= ~KEY_RIGHT;
            break;
        case 0x31: //fire
            mKeyMap &= ~KEY_FIRE;
            break;
        case 0x12: //reset
            mKeyMap &= ~KEY_RESET;
            break;
        case 0x13: //select
            mKeyMap &= ~KEY_SELECT;
            break;
    }
}

@end // AdventureView


// *******************************************************************************************
// Buffer stuff
// *******************************************************************************************

bool CreateOffscreen(int aWidth, int aHeight)
{
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
		x *= gGfxScaler;
		y *= gGfxScaler;
		width *= gGfxScaler;
		height *= gGfxScaler;
				
		int bufferWidth = ADVENTURE_SCREEN_WIDTH*gGfxScaler;
		int bufferHeight = ADVENTURE_SCREEN_HEIGHT*gGfxScaler;
		
		for (int cy=0; cy<height; cy++)
		{
			// The game expects a bottom up buffer, so we flip the orientation here.
			// Also, the game actually draws more than would show on a TV screen, hence the adjustment for overscan
			
			int py = ((ADVENTURE_SCREEN_HEIGHT*gGfxScaler) - (y + cy)) + (ADVENTURE_OVERSCAN*gGfxScaler);
			
			if ((py >= 0) && (py < (ADVENTURE_SCREEN_HEIGHT*gGfxScaler)))
			{
				for (int cx=0; cx<width; cx++)
				{
					int px = cx + x;
					byte* p = &gPixelBucket[(unsigned int)((py*(ADVENTURE_SCREEN_WIDTH*gGfxScaler)) + px) * 4];
					
					if ((px >= 0) && (px < (ADVENTURE_SCREEN_WIDTH*gGfxScaler)))
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

void Platform_ReadDifficultySwitches(int* left, int* right)
{
	if (left) *left = gLeftDifficulty;	// true = dragons pause before eating you
	if (right) *right = gRightDifficulty;  // true = dragons do not run from the sword
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






