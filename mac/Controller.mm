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

#import "Controller.h"
#import "AdventureView.h"

@implementation Controller

extern bool gLeftDifficulty;
extern bool gRightDifficulty;
extern bool gMenuItemReset;
extern bool gMenuItemSelect;

// This is NOT how to do MVC with Cocoa, but I just want to get this working.
extern AdventureView* gView;


- (void)awakeFromNib
{
	if (gLeftDifficulty)
		[mMenuDragonsHesitate setState:NSOnState];
	else
		[mMenuDragonsHesitate setState:NSOffState];
	
	if (!gRightDifficulty)
		[mMenuDragonsRun setState:NSOnState];
	else
		[mMenuDragonsRun setState:NSOffState];
    bool canPlay = [gView checkCanPlay];
    if (!canPlay) {
        [_playButton setEnabled:FALSE];
        [mNameText setEnabled:FALSE];
        [_gameSelectPopup setEnabled:FALSE];
        [_playersSelectPopup setEnabled:FALSE];
        [_dragonSpeedCheck setEnabled:FALSE];
        [_dragonFearCheck setEnabled:FALSE];
        [_wait1Text setEnabled:FALSE];
        [_wait2Text setEnabled:FALSE];
        [_waitLabel setEnabled:FALSE];
    }
    
    NSColor *linkColor = [NSColor blueColor];

    NSMutableAttributedString *colorTitle1 = [[NSMutableAttributedString alloc] initWithAttributedString:[_coordinateLink attributedTitle]];
    NSRange titleRange1 = NSMakeRange(0, [colorTitle1 length]);
    [colorTitle1 addAttribute:NSForegroundColorAttributeName value:linkColor range:titleRange1];
    [colorTitle1 addAttribute:NSUnderlineStyleAttributeName value:@(NSUnderlineStyleSingle) range:titleRange1];
    [_coordinateLink setAttributedTitle:colorTitle1];

    NSMutableAttributedString *colorTitle2 = [[NSMutableAttributedString alloc] initWithAttributedString:[_chatLink attributedTitle]];
    NSRange titleRange2 = NSMakeRange(0, [colorTitle2 length]);
    [colorTitle2 addAttribute:NSForegroundColorAttributeName value:linkColor range:titleRange2];
    [colorTitle2 addAttribute:NSUnderlineStyleAttributeName value:@(NSUnderlineStyleSingle) range:titleRange2];
    [_chatLink setAttributedTitle:colorTitle2];

    NSMutableAttributedString *colorTitle3 = [[NSMutableAttributedString alloc] initWithAttributedString:[_helpLink attributedTitle]];
    NSRange titleRange3 = NSMakeRange(0, [colorTitle3 length]);
    [colorTitle3 addAttribute:NSForegroundColorAttributeName value:linkColor range:titleRange3];
    [colorTitle3 addAttribute:NSUnderlineStyleAttributeName value:@(NSUnderlineStyleSingle) range:titleRange3];
    [_helpLink setAttributedTitle:colorTitle3];
}

- (IBAction)clickDragonsHesitate:(id)sender
{
	// toggle the check mark and value
	int state = [mMenuDragonsHesitate state];
	if (state == NSOnState)
	{
		gLeftDifficulty = FALSE;
		[mMenuDragonsHesitate setState:NSOffState];
	}
	else
	{
		gLeftDifficulty = TRUE;
		[mMenuDragonsHesitate setState:NSOnState];
	}
}

- (IBAction)clickDragonsRun:(id)sender
{
	// toggle the check mark and value
	int state = [mMenuDragonsRun state];
	if (state == NSOnState)
	{
		gRightDifficulty = TRUE;
		[mMenuDragonsRun setState:NSOffState];
	}
	else
	{
		gRightDifficulty = FALSE;
		[mMenuDragonsRun setState:NSOnState];
	}
}

- (IBAction)clickSelect:(id)sender
{
	gMenuItemSelect = TRUE;
}

- (IBAction)selectGameAction:(id)sender {
    int desiredPlayers = [_playersSelectPopup indexOfSelectedItem] + 2;
    bool hideSecond = (desiredPlayers == 2);
    [_wait2Text setHidden:hideSecond];
}

- (IBAction)clickCoordinateLink:(id)sender {
    [[NSWorkspace sharedWorkspace] openURL:[NSURL URLWithString:@"http://h2hadventure.ddns.net/home/coordinate"]];
}

- (IBAction)clickChatLink:(id)sender {
    [[NSWorkspace sharedWorkspace] openURL:[NSURL URLWithString:@"http://h2hadventure.ddns.net/home/audiochat"]];
}

- (IBAction)clickHelpLink:(id)sender {
    [[NSWorkspace sharedWorkspace] openURL:[NSURL URLWithString:@"http://h2hadventure.ddns.net/home/help"]];
}

- (IBAction)clickReset:(id)sender
{
	gMenuItemReset = TRUE;
}

- (IBAction)clickPlay:(id)sender {
    [_playButton setHidden:TRUE];
    [mNameText setHidden:TRUE];
    [_gameSelectPopup setHidden:TRUE];
    [_playersSelectPopup setHidden:TRUE];
    [_dragonSpeedCheck setHidden:TRUE];
    [_dragonFearCheck setHidden:TRUE];
    [_wait1Text setHidden:TRUE];
    [_wait2Text setHidden:TRUE];
    [_waitLabel setHidden:TRUE];
    
    int gameNum = [_gameSelectPopup indexOfSelectedItem];
    int desiredPlayers = [_playersSelectPopup indexOfSelectedItem] + 2;
    bool diff1Switch = [_dragonSpeedCheck state];
    bool diff2Switch = [_dragonFearCheck state];
    
    [gView playGame: [mNameText stringValue] :gameNum :desiredPlayers :diff1Switch :diff2Switch :[_wait1Text stringValue] :[_wait2Text stringValue] ];
}

@end
