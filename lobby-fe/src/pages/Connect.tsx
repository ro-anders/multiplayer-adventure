import { useState } from 'react';

import '../App.css';
import '../css/Connect.css'
import EventsList from '../components/EventsList';
import Subscribe from '../components/Subscribe';
import SendCall from '../components/SendCall';
import TitleBar from '../components/TitleBar';

interface ConnectProps {
  /** The name of the currently logged in user */
  username: string;
}

/**
 * The page for offering multiple ways to find other players to connect with
 */
function ConnectPage({username}: ConnectProps) {

  let [selected, setSelected] = useState<string>('none');
  let [pathPosition, setPathPosition] = useState<number>(1);

  const pathPositions: { [key: string]: number } = {}
  pathPositions['schedule'] = 10;
  pathPositions['call'] = 1.8;
  pathPositions['notify'] = 0.56;
  pathPositions['engage'] = 0.1;

  /**
   * React when an action is chosen.
   * @param chosen the string code for the chosen action
   */
  function onSelected(chosen: string) {
    setSelected(chosen)
    setPathPosition(pathPositions[chosen])

  }
  
  return (
    <div className="connect-page">
      <TitleBar/>
      <div className="connect-separator-column">
        <div className="connect-separator-wall"/>
        <div className="connect-separator-path"/>
        <div className="connect-separator-wall"/>
      </div>
      <div className="connect-room">
        <div onClick={()=>{onSelected('schedule')}}>Join a scheduled event</div>
        <div onClick={()=>{onSelected('call')}}>Send out a call</div>
        <div onClick={()=>{onSelected('notify')}}>Get notified</div>
        <div onClick={()=>{onSelected('engage')}}>Reach out</div>
      </div>
      <div className="connect-separator-column">
        <div className="connect-separator-wall"/>
        <div className="connect-separator-path"/>
        <div style={{flex: pathPosition}} className="connect-separator-wall"/>
      </div>
      <div className="connect-room">
        {(selected === 'schedule') &&
          <div>
            <h3>Scheduled Events</h3>
            <EventsList current_user={username}/>
          </div>
        }
        {(selected === 'call') &&
          <SendCall current_user={username}/>
        }
        {(selected === 'notify') &&
          <Subscribe/>
        }
        {(selected === 'engage') &&
          <div>You must know someone who will like this. ...</div>
        }
      </div>
      <div className="connect-separator-column">
        <div className="connect-separator-wall"/>
        <div className="connect-separator-path"/>
        <div className="connect-separator-wall"/>
      </div>
    </div>
  );
}

export default ConnectPage;
