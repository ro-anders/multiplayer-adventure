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
    IBOutlet NSTextField *mAnnouncementMessage;
    IBOutlet NSButton *mAnnouncementLink;
    
}
- (IBAction)clickAnnouncementLink:(id)sender;

- (bool)CreateOffscreen;

- (bool)checkCanPlay;

- (void)playGame:(NSString*)playerName :(int)gameNum :(int)desiredPlayers :(bool)diff1Switch :(bool)diff2Switch
                :(NSString*)waitFor1 :(NSString*)waitFor2;

- (IBAction)update:(id)sender;

- (void)displayStatus:(const char*)message :(int)durationSec;


@end
