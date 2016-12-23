#!/bin/bash
cat ~/Documents/stuff/multiplayer-adventure/scripts/intro.script > all1_raw.script
~/Documents/stuff/ss/bin/ss 2521 < ~/Documents/stuff/multiplayer-adventure/scripts/fighting-over-chalice.script >> all1_raw.script 
~/Documents/stuff/ss/bin/ss 4850 < ~/Documents/stuff/multiplayer-adventure/scripts/laying-rhindle-trap.script >> all1_raw.script
~/Documents/stuff/ss/bin/ss 5653 < ~/Documents/stuff/multiplayer-adventure/scripts/hand_off_dragon.script >> all1_raw.script
~/Documents/stuff/ss/bin/ss 5983 < ~/Documents/stuff/multiplayer-adventure/scripts/stealing-bridge.script >> all1_raw.script
~/Documents/stuff/ss/bin/ss 6591 < ~/Documents/stuff/multiplayer-adventure/scripts/dragons-resurrecting.script >> all1_raw.script
~/Documents/stuff/ss/bin/ss 7006 < ~/Documents/stuff/multiplayer-adventure/scripts/locking-in-castle.script >> all1_raw.script
~/Documents/stuff/ss/bin/ss 7706 < ~/Documents/stuff/multiplayer-adventure/scripts/standoff.script >> all1_raw.script

cat ~/Documents/stuff/multiplayer-adventure/scripts/fighting-over-chalice.script > all2_raw.script
~/Documents/stuff/ss/bin/ss 2278 < ~/Documents/stuff/multiplayer-adventure/scripts/trapping-in-dragon.script >> all2_raw.script 
~/Documents/stuff/ss/bin/ss 2793 < ~/Documents/stuff/multiplayer-adventure/scripts/laying-rhindle-trap.script >> all2_raw.script 
~/Documents/stuff/ss/bin/ss 3637 < ~/Documents/stuff/multiplayer-adventure/scripts/locking-in-castle.script >> all2_raw.script

(cat all1_raw.script | grep -v "^Enter input" | grep -v "^[\.]") > all1.script
echo "." >> all1.script

(cat all2_raw.script | grep -v "^Enter input" | grep -v "^[\.]") > all2.script
echo "." >> all2.script
