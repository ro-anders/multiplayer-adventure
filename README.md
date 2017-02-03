# multiplayer-adventure
Head-on-head internet version of Atari's classic Adventure

Two or three players in the same maze, each with their own home castle.  First one to bring the chalice back to their home castle wins.

Aside from multiplayer aspect, as close as possible reproduction to the original Atari Adventure.

## To Build
On Mac, open the project file mac/MacAdventure.xcodeproj using XCode.
On Windows, build the project file pc/WinAdventure.vcxproj using Visual Studio.

## To Run
If you downloaded the executable, MacAdventure.app.zip, you must first unpack the zip file by clicking on it, which will create the MacAdventure.app file.

You CANNOT run the game simply by double clicking the exe or app file.  This beta version requires command line flags and so must be run from a command line.
First argument is "broker" which means to ask a broker out on the internet to setup the game
Second argument is the game you want to play, 1-3 (1 is the truncated maze with no white castle, 2 is the full maze, and 3 is the full maze with randomized placement of objects)
Third argument is the number of players that will be connecting, 2 or 3.

For example, playing game 2 with 3 players:
On Windows: WinAdventure.exe broker 2 3
On Mac: first "cd MacAdventure.app/Contents/MacOS" then "./MacAdventure broker 2 3"

NOTE: This doesn't work on a local area network.  This assumes these are two machines out on the internet.  If two machines are on the same network, (i.e. both on the same wifi) they will not be able to see each other, and the more complicated peer-to-peer setup must be used.

## To Run Peer-to-Peer
The H2HAdventure broker is a server out on the internet that connects players.  It determines what IP address each player appears as out on the internet and it determines which players start at which castles.  However, H2HAdventure does not need to use the broker if all that information is provided on the command line.

If you are playing two players and they are both on the same local network, you need to determine your local ip address (often it's something like 192.168.1.4).
You also need to agree who will be player 1 (starts at gold castle), player 2 (starts at copper castle), and player 3 (starts at jade castle).
Finally you need to agree on which game you will play, 1, 2, or 3.
Let's say the gold player has IP address 192.168.1.11 and copper has 192.168.1.12 and jade has 192.168.1.13 and they are going to play game 2.  They need to type:
player1 -> WinAdventure.exe 2 1 5001 192.168.1.12:5002 192.168.1.13:5003
player2 -> WinAdventure.exe 2 2 5002 192.168.1.11:5001 192.168.1.13:5003
player3 -> WinAdventure.exe 2 3 5003 192.168.1.11:5001 192.168.1.12:5002

For Mac it would be ./MacAdventure instead of WinAdventure.exe.

The 5001,5002,5003 is specifying a "port" for the computer to listen on.  It can actually be any number as long as its uniquer.

If only two players are playing omit the final argument.

If players are not on the same local network peer-to-peer setup is more difficult.  You need to know what your public IP is - the one you appear to use to other computers on the internet.  Visit whatismyip.com to determine this.  Usually, but not always, the port you listen on will remain unchanged.  When specifying the IP numbers of the other players on the command line you need to specify the local IP for machines on your network and the public IP for machines on the internet.

Let's say player3 in the example above is out on the internet, and it appears to other machines as 300.300.300.300.  Let's say player1 and player2 both appear as 100.100.100.100.  The commands to type are:
player1 -> WinAdventure.exe 2 1 5001 192.168.1.12:5002 300.300.300.300:5003
plater2 -> WinAdventure.exe 2 2 5002 192.168.1.11:5001 300.300.300.300:5002
player3 -> WinAdventure.exe 2 3 5003 100.100.100.100:5001 100.100.100.100:5003

 
