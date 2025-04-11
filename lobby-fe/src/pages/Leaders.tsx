import { useEffect, useState } from 'react';

import '../App.css';
import '../css/Leaders.css'
import { PlayerStat } from '../domain/PlayerStat';
import PlayerService from '../services/PlayerService';
import AchieverList from '../components/AchieverList';

function LeaderBoard() {

  let [topScorers, setTopScorers] = useState<PlayerStat[]>([]);
  let [achievers, setAchievers] = useState<{[achivmt: string]: string[];}>({})
  let [topAchievment, setTopAchievment] = useState<string>("")
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

    // Now map player stats to player names
    const achievers_list: {[achvmt: string]: string[]} = {}
    for (let key in collated) {
      // Map through the list of objects and extract the 'name' attribute
      achievers_list[key] = collated[key].map(stat => stat.playername);
    }

    return achievers_list
  }


  // Load the scheduled events, but only do this once.
  // We don't poll the server for updates.  You'll have to 
  // refresh the page to do that.
  useEffect(() => {

      /**
       * Load the scheduled events from the backend.
       */
      async function loadPlayerStats() {
        const MIN_WINS = 1
        const stats = await PlayerService.getAllPlayerStats()
        // Sort the list by best players score, which is mostly win ratio but
        // modified so that more games played counts for something.
        // Also filter out anyone who hasn't won enough games.
        const score_list = [...stats].sort(scoreSort).filter(stat=>stat.wins >= MIN_WINS)
        const achiever_lists: {[achvmt:string]: string[]} = collect_achievers(stats)

        // Figure out what is the most important achiemvment achieved
        const ordered = ['egg', 'challenge', 'gate', 'key', 'castle']
        const achieved = ordered.filter(achvmt => achvmt in achiever_lists)
        setTopAchievment(achieved.length > 0 ? achieved[0] : "")
        setAchievers(achiever_lists)
        setTopScorers(score_list)
        setTopScorersLoaded(true)
      }

    if (!topScorersLoaded) {
      loadPlayerStats();
    }
  }, [topScorersLoaded]);

  return (

    <div className="leaders-page">
      <h1>Leader Board</h1>
      <div className="leaders-box">
        {Object.keys(achievers).length > 0 &&
          <div>
            <AchieverList achievment_description='beat the crystal challenge' achievers={achievers['challenge']} isTop={topAchievment==='challenge'}/>
            <AchieverList achievment_description='opened the crystal gate' achievers={achievers['gate']} isTop={topAchievment==='gate'}/>
            <AchieverList achievment_description='found the crystal key' achievers={achievers['key']} isTop={topAchievment==='key'}/>
            <AchieverList achievment_description='found the crystal castle' achievers={achievers['castle']} isTop={topAchievment==='castle'}/>
            </div>
        }
        <h2>Top Scorers</h2>
        {!topScorersLoaded && <p>loading...</p>}
        {topScorersLoaded && 
          <table>
            <thead>
              <tr><td><h4>Name</h4></td><td><h4>Wins</h4></td><td><h4>Games</h4></td></tr>
            </thead>
            <tbody>
              {topScorers.map((stat: PlayerStat) => (
                <tr key={stat.playername}><td>{stat.playername}</td><td>{stat.wins}</td><td>{stat.games}</td></tr>
              ))}
            </tbody>
          </table>
        }
      </div>
    </div>
  );
}

export default LeaderBoard;
