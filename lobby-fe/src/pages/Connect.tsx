import { useState } from 'react';

import { Button, Form } from 'react-bootstrap';
import '../App.css';
import '../css/Connect.css'
import EventsList from '../components/EventsList';

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
          <div>Notify others that you want to play ...</div>
        }
        {(selected === 'notify') &&
          <div>
            <div>Receive email when someone is looking to play.</div>
            <div>
              <Form>
                <Form.Label>Notify me at</Form.Label>
                <Form.Control placeholder="acererak@gmail.com"/>
                <Form.Check
                  type="checkbox"
                  id="call"
                  label="when someone sends out a call (is online ready to play)"
                />
                <Form.Check
                  type="checkbox"
                  id="event"
                  label="when a new event is scheduled"
                />
                <Button>Subscribe</Button>
              </Form>
            </div>
            <div>
              <Form>
                <Form.Label>Unsubscribe from all notifications</Form.Label>
                <Form.Control placeholder="acererak@gmail.com"/>
                <Button>Unsubscribe</Button>
              </Form>
            </div>
          </div>
        }
        {(selected === 'engage') &&
          <div>You must know someone who will like this. ...</div>
        }
      </div>
    </div>
  );
}

export default ConnectPage;
