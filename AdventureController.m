#import "AdventureController.h"

@implementation AdventureController

- (void)awakeFromNib
{
	[[mDragonsHesitate menu] setAutoenablesItems:NO];
	
	[mDragonsHesitate setState:NSOnState];
	[mDragonsHesitate setEnabled:YES];
	
	[mDragonsRun setState:NSOffState];
	[mDragonsRun setEnabled:YES];
}

@end
