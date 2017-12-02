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

#include "AdventureView.h"
#include "Sys.hpp"

// Some types
typedef unsigned char byte;

// Screen characteristics
#define ADVENTURE_SCREEN_WIDTH              250//320
#define ADVENTURE_SCREEN_HEIGHT             150//192
#define ADVENTURE_FPS                       58

void PaintScreen(int shade);
void FreeOffscreen();

short GetKeyState(unsigned short k);

float gGfxScaler = 3.0f;
byte* gPixelBucket = NULL;
CGContextRef gDC = NULL;

unsigned char mKeyMap = 0;

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

    timer = [NSTimer scheduledTimerWithTimeInterval: 0.016
                                             target: self
                                           selector: @selector(update:)
                                           userInfo: nil
                                            repeats: YES];
    /**/
    return self;
}

- (void) dealloc
{
    [timer invalidate];
    
    FreeOffscreen();
    
    [super dealloc];
}

- (bool)CreateOffscreen
{
    NSRect rectWindow = [self bounds];
    
    // Find the best scale for this resolution
    int sx = rectWindow.size.width / ADVENTURE_SCREEN_WIDTH;
    int sy = rectWindow.size.height / ADVENTURE_SCREEN_HEIGHT;
    gGfxScaler = (sx < sy) ? sx : sy; // min(sx, sy);
    gGfxScaler = (gGfxScaler == 0) ? .5 : gGfxScaler;
    int aWidth = ADVENTURE_SCREEN_WIDTH * gGfxScaler;
    int aHeight = ADVENTURE_SCREEN_HEIGHT * gGfxScaler;

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
    static bool isSetup = false;
    static int shade = 0;
    static long timestamp = Sys::runTime();
    
    shade = shade + 1;
    if (shade == 240) {
        shade = 0;
        long now = Sys::runTime();
        long ellapsed = now - timestamp;
        // 240 times at 60 times per second should be about 4 seconds.
        if (ellapsed > 4400) {
            printf("Painting took too long.  Only painting %ld times per second.\n", 240000 / ellapsed);
        }
        timestamp = now;
        
    }
    
    if (!isSetup) {
        [self CreateOffscreen];
        isSetup = true;
    } else {
        PaintScreen(shade);
    }
    
    // Display it
    [self setNeedsDisplay:YES];
}

- (void)drawRect:(NSRect)rect
{
    if (gDC)
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
                
    }
}


@end // AdventureView


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

void PaintScreen(int shade) {
    if (gPixelBucket) {
        int bufferWidth = ADVENTURE_SCREEN_WIDTH*gGfxScaler;
        int bufferHeight = ADVENTURE_SCREEN_HEIGHT*gGfxScaler;

        for(int py=0; py<bufferHeight; ++py) {
            for(int px=0; px<bufferWidth; ++px) {
                byte* p = &gPixelBucket[(unsigned int)((py*bufferWidth) + px) * 4];
                if ((px >= 0) && (px < bufferWidth))
                {
                    p++;    // skip alpha
                    *(p++) = shade & 0xff;
                    *(p++) = shade & 0xff;
                    *(p++) = shade & 0xff;
                }
            }
        }
    }
}






