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


#import <Cocoa/Cocoa.h>

@interface AdventureView : NSView
{
    NSTimer *timer;
    IBOutlet NSTextField *mStatusMessage;
}

- (void)playGame;

- (IBAction)update:(id)sender;

- (void)displayStatus:(const char*)message :(int)durationSec;


@end
