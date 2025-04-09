import { useEffect, useState } from 'react';
import { Form, Button } from 'react-bootstrap';

import '../App.css';
import '../css/Leaders.css'
import { PlayerStat } from '../domain/PlayerStat';
import PlayerService from '../services/PlayerService';

function LeaderBoard() {

  let [topScorers, setTopScorers] = useState<PlayerStat[]>([]);
  let [achievers, setAchievers] = useState<{[achivmt: string]: string[];}>({})
  let [topScorersLoaded, setTopScorersLoaded] = useState<boolean>(false);

  /**
   * This is the function used to order the scorers leader board.
   * Given two players' stats, this orders them.
   * @param stat1 the stats for one player
   * @param stat2 the stats for another player
   * @returns a negative nnumber if the first players stats are more impressive, a positive
   * number if the second players stats are more impressive, and 0 if they have the same
   * stats.
   */
  function scoreSort(stat1: PlayerStat, stat2: PlayerStat) : number {
    // This mostly goes on win percentage (win/games) but to put some weight behind
    // consistent winning (e.g. 26 out of 27 is more impressive than 3 out of 3) we slightly
    // degrade the win rate and the degradation affects stats with fewer games more
    // than stats with more games.
    const degrade_rate1 = stat1.wins / (stat1.games + 2.01)
    const degrade_rate2 = stat2.wins / (stat2.games + 2.01)
    return degrade_rate2 - degrade_rate1
  }

  /**
   * This looks at all stats and collects the list of those who have
   * scored achievments into lists, one for each achievment.
   * @param stats all players stats
   * @returns a dictionary mapping an achievment to the players that
   * achieved it in order of who achieved it first, though each player is
   * only listed for their last achievment.
   */
  function collect_achievers(stats: PlayerStat[]): {[achvmt: string]: string[]} {

    let achievments: {[id: number]: string} = {3: "castle", 4: "key", 5: "gate", 6: "challenge"}

    // Collate all players into the lists, ingnoring any stats that have
    // achievments we aren't tracking.
    const collated: {[achvmt_name: string]: PlayerStat[]} = {}
    for(let stat of stats) {
      if (stat.achvmts in achievments) {
        const achvmt_name = achievments[stat.achvmts]
        if (achvmt_name in collated) {
          collated[achvmt_name].push(stat)
        }
        else {
        // First time we've seen this achievment, make a new list.
        collated[achvmt_name] = [stat]
        }
      }
    }

    // Sort the lists.
    for (const achvmt_name in collated) {
      collated[achvmt_name].sort((a, b) => a.achvmt_time - b.achvmt_time)
    }

    // The very first "challenge" achiever gets the "egg" achievment
    if ("challenge" in collated) {
      collated["egg"] = [collated["challenge"].shift() as PlayerStat]
      if (collated["challenge"].length == 0) {
        delete collated["challenge"]
      }
    }

    // Now map player stats to player names
    const achievers_list: {[achvmt: string]: string[]} = {}
    for (let key in collated) {
      // Map through the list of objects and extract the 'name' attribute
      achievers_list[key] = collated[key].map(stat => stat.playername);
    }
    return achievers_list
  }

  /**
   * Load the scheduled events from the backend.
   */
  async function loadPlayerStats() {
    const stats = await PlayerService.getAllPlayerStats()
    // Sort the list by best players score, which is a little funky in that
    // we do wins / (games+2) so that 3 out of 3 doesn't look more impressive than 26 out of 27.
    const score_list = [...stats].sort(scoreSort)
    const achiever_lists: {[achvmt:string]: string[]} = collect_achievers(stats)
    setAchievers(achiever_lists)
    setTopScorers(score_list)
    setTopScorersLoaded(true)
  }

  // Load the scheduled events, but only do this once.
  // We don't poll the server for updates.  You'll have to 
  // refresh the page to do that.
  useEffect(() => {
    if (!topScorersLoaded) {
      loadPlayerStats();
    }
  }, [topScorersLoaded]);

  return (

    <div className="leaders-page">
      <h1>Leader Board</h1>
      <div className="leaders-box">
        <h2>Top Scorers</h2>
        {!topScorersLoaded && <p>loading...</p>}
        {topScorersLoaded && 
          <table>
            <tr><td><h4>Name</h4></td><td><h4>Wins</h4></td><td><h4>Games</h4></td></tr>
            {topScorers.map((stat: PlayerStat) => (
              <tr><td>{stat.playername}</td><td>{stat.wins}</td><td>{stat.games}</td></tr>
            ))}
          </table>
        }
      </div>
    </div>
  );
}

export default LeaderBoard;
