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

/* Controller */

#import <Cocoa/Cocoa.h>

@interface Controller : NSObject
{
    IBOutlet NSMenuItem *mMenuDragonsHesitate;
    IBOutlet NSMenuItem *mMenuDragonsRun;
    IBOutlet NSTextField *mNameText;
}

@property (assign) IBOutlet NSButton *playButton;
@property (assign) IBOutlet NSPopUpButton *gameSelectPopup;
@property (assign) IBOutlet NSPopUpButton *playersSelectPopup;
@property (assign) IBOutlet NSButton *dragonSpeedCheck;
@property (assign) IBOutlet NSButton *dragonFearCheck;
@property (assign) IBOutlet NSTextField *wait1Text;
@property (assign) IBOutlet NSTextField *wait2Text;
@property (assign) IBOutlet NSTextField *waitLabel;

- (IBAction)clickDragonsHesitate:(id)sender;
- (IBAction)clickDragonsRun:(id)sender;
- (IBAction)clickReset:(id)sender;
- (IBAction)clickSelect:(id)sender;
- (IBAction)selectGameAction:(id)sender;

@end
