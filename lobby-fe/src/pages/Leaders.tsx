import { useState } from 'react';
import { Form, Button } from 'react-bootstrap';

import '../App.css';
import '../css/Leaders.css'
import { PlayerStat } from '../domain/PlayerStat';

function LeaderBoard() {

  let [topScorers, setTopScorers] = useState<PlayerStat[]>([]);
  let [topScorersLoaded, setTopScorersLoaded] = useState<boolean>(false);


  return (

    <div className="leader-page">
      <h1>Leader Board</h1>
      <div className="leader-box">
        <h2>Top Scorers</h2>
        {!topScorersLoaded && <p>loading...</p>}
      </div>
    </div>
  );
}

export default LeaderBoard;
