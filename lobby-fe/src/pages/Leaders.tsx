import { useEffect, useState } from 'react';
import { Form, Button } from 'react-bootstrap';

import '../App.css';
import '../css/Leaders.css'
import { PlayerStat } from '../domain/PlayerStat';
import PlayerService from '../services/PlayerService';

function LeaderBoard() {

  let [topScorers, setTopScorers] = useState<PlayerStat[]>([]);
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
   * Load the scheduled events from the backend.
   */
  async function loadPlayerStats() {
    const stats = await PlayerService.getAllPlayerStats()
    // Sort the list by best players score, which is a little funky in that
    // we do wins / (games+2) so that 3 out of 3 doesn't look more impressive than 26 out of 27.
    const score_list = [...stats].sort(scoreSort)
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
