import { useState } from 'react';
import { Button } from 'react-bootstrap';

import '../App.css';
import '../css/Connect.css'

interface ConnectProps {
  /** The name of the currently logged in user */
  username: string;
}

/**
 * The page for offering multiple ways to find other players to connect with
 */
function ConnectPage({username}: ConnectProps) {

  let [selected, setSelected] = useState<string>('none');
  
  function showSchedule() {

  }
  
  return (
    <div className="connect-page">
      <div className="connect-room">
        <Button onClick={()=>{setSelected('schedule')}}>Join a scheduled event</Button>
        <Button onClick={()=>{setSelected('call')}}>Send out a call</Button>
        <Button onClick={()=>{setSelected('notify')}}>Get notified</Button>
        <Button onClick={()=>{setSelected('engage')}}>Reach out</Button>
      </div>
      <div className="connect-room">
      {(selected == 'schedule') &&
          <div>Below is the schedule ...</div>
        }
        {(selected == 'call') &&
          <div>Notify others that you want to play ...</div>
        }
        {(selected == 'notify') &&
          <div>Receive email when someone is looking to play ...</div>
        }
        {(selected == 'engage') &&
          <div>You must know someone who will like this. ...</div>
        }
      </div>
    </div>
  );
}

export default ConnectPage;
