import { useState } from 'react';

import { Button, Form } from 'react-bootstrap';
import '../App.css';
import '../css/Connect.css'
import EventsList from '../components/EventsList';
import Subscribe from '../components/Subscribe';
import SendCall from '../components/SendCall';

interface ConnectProps {
  /** The name of the currently logged in user */
  username: string;
}

/**
 * The page for offering multiple ways to find other players to connect with
 */
function ConnectPage({username}: ConnectProps) {

  let [selected, setSelected] = useState<string>('none');
  
  return (
    <div className="connect-page">
      <div className="connect-room">
        <div onClick={()=>{setSelected('schedule')}}>Join a scheduled event</div>
        <div onClick={()=>{setSelected('call')}}>Send out a call</div>
        <div onClick={()=>{setSelected('notify')}}>Get notified</div>
        <div onClick={()=>{setSelected('engage')}}>Reach out</div>
      </div>
      <div className="connect-room">
        {(selected === 'schedule') &&
          <div>
            <h3>Scheduled Events</h3>
            <EventsList current_user={username}/>
          </div>
        }
        {(selected === 'call') &&
          <SendCall/>
        }
        {(selected === 'notify') &&
          <Subscribe/>
        }
        {(selected === 'engage') &&
          <div>You must know someone who will like this. ...</div>
        }
      </div>
    </div>
  );
}

export default ConnectPage;
