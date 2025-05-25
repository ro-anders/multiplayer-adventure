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

  /**
   * This stores the flex values needed to place the center path
   * next to the chosen action.
   */
  const pathPositions: { [key: string]: number } = {}
  pathPositions['none'] = 1;
  pathPositions['schedule'] = 10;
  pathPositions['call'] = 1.8;
  pathPositions['notify'] = 0.56;
  pathPositions['engage'] = 0.1;
  const default_action = 'schedule';

  let [selected, setSelected] = useState<string>(default_action);
  let [pathPosition, setPathPosition] = useState<number>(pathPositions[default_action]);

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
            <h3>Scheduled Play Events</h3>
            <p>
              Planning to play soon?  Let everyone know when, so they
              can plan to join you.
            </p>
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
          <div>
            <h2>No one knows this games exists.</h2>
            <h4>That's why the lobby is usually empty.</h4>
            <p>
              Did you play Adventure as a kid?  Reach out to an old
              friend and see if they want to play.
            </p>
            <p>
              Post about playing this game on social media.
              Others may be insterested in it.
            </p>
            <p>
              Tell your friends about how cool Atari Adventure was.
              They may want to check it out.
            </p>
            <p>
              Watch Ready Player One.  Or, better yet, read the book.
            </p>
          </div>
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
