import { useEffect, useState } from 'react';
import Button from 'react-bootstrap/Button';
import ListGroup from 'react-bootstrap/ListGroup';
import '../App.css';
import { ScheduledEvent } from '../domain/ScheduledEvent';
import ScheduledEventService from '../services/ScheduledEventService';
import ScheduleEventModal from './ScheduleEventModal';

interface EventsListProps {
  /** The name of the currently logged in user */
  current_user: string;
}

/**
 * Displays a list of scheduled events.
 */
function EventsList({current_user}: EventsListProps) {

  let [events, setEvents] = useState<ScheduledEvent[]>([]);
  let [eventsLoaded, setEventsLoaded] = useState<boolean>(false);
  let [showModal, setShowModal] = useState<boolean>(false);

  /** Formatter we use to show the labels of dates and times */
  const dateFormatter = new Intl.DateTimeFormat(undefined, {
    weekday: 'short',
    month: 'short',
    day: 'numeric',
  });
  const timeFormatter = new Intl.DateTimeFormat(undefined, {
    hour: 'numeric',
    minute: 'numeric',
    hour12: true,
  });



  /**
   * User has just pressed "Join" on an event.
   * @param event which event they have joined
   */
  function joinEvent(event: ScheduledEvent) {
    if (!event.players.includes(current_user)) {
      event.players.push(current_user)
      ScheduledEventService.upsertScheduleEvent(event, false)
      setEvents(events.slice())
    }
  }

  /**
   * User has just pressed "Leave"
   * @param event which event they are leaving
   */
  function quitEvent(event: ScheduledEvent) {
    if (event.players.includes(current_user)) {
      if (event.players.length === 1) {
        // Last player.  Delete the whole event
        ScheduledEventService.deleteScheduledEvent(event)
        const newEventsList = events.filter(e => e.starttime !== event.starttime)
        setEvents(newEventsList)
      } else {
        // Remove the player from the list of players
        const newPlayerList = event.players.filter(player => player !== current_user)
        event.players = newPlayerList
        ScheduledEventService.upsertScheduleEvent(event, false)
        setEvents(events.slice())
      }
    }
  }

  function createNewEvent(new_event: ScheduledEvent) {
    events.push(new_event)
    events.sort((a: ScheduledEvent, b: ScheduledEvent) => a.starttime - b.starttime)
    ScheduledEventService.upsertScheduleEvent(new_event, true)
    setEvents(events.slice())
  }

  /**
   * Get a human readable label for the time of the event
   * @param timestamp timestamp of the event
   * @returns something human readable like Sun, Feb 4 at 10:30PM or Tomorrow at 2:00PM
   */
  function formatTimestamp(timestamp: number) {
    const date = new Date(timestamp);

    // Check if the date is today, tomorrow, or a different day
    const isToday = new Date().toDateString() === date.toDateString();
    const isTomorrow = new Date(Date.now() + 86400000).toDateString() === date.toDateString();

    // Format the date part as "Today" or "Tomorrow" if needed
    let dateString = '';
    if (isToday) {
        dateString = "Today";
    } else if (isTomorrow) {
        dateString = "Tomorrow";
    } else {
        dateString = dateFormatter.format(date);
    }

    // Format the time part
    const timeString = timeFormatter.format(date);

    return `${dateString} at ${timeString}`;
  }


  /**
   * Load the scheduled events from the backend.
   */
  async function loadScheduledEvents() {
    const events = await ScheduledEventService.getScheduledEvents()
    setEvents(events)
    setEventsLoaded(true)
  }

  // Load the scheduled events, but only do this once.
  // We don't poll the server for updates.  You'll have to 
  // refresh the page to do that.
  useEffect(() => {
    if (!eventsLoaded) {
      loadScheduledEvents();
    }
  }, [eventsLoaded]);

  return (

    <div className="schedule-list">
      <header>
        {events.length > 0 && 
          <ListGroup>
              {/* List all the events */}
              {events.map((event: ScheduledEvent) => (
              <ListGroup.Item className="connect-event" key={event.starttime}>

                {/* An event has two lines, first being the date and
                a button to Join or Quit and second being list of people 
                registered for the event */}
                <div>
                {formatTimestamp(event.starttime)}:
                  {event.players.includes(current_user) && 
                    <Button size="sm" className='connect-event-action' 
                      onClick={() => quitEvent(event)}>Leave</Button>}
                  {!event.players.includes(current_user) && 
                    <Button size="sm" className='connect-event-action' 
                      onClick={() => joinEvent(event)}>Register</Button>}
                </div>
                <div>Attending: {event.players.join(", ")}</div>
              </ListGroup.Item>
              ))}
          </ListGroup>
      }
      {events.length == 0 &&
        <div>
          No one has indicated a time they plan to play.
        </div>
      }
      </header>
      <Button onClick={()=>setShowModal(true)}>Schedule a time</Button>
      <ScheduleEventModal 
        current_user={current_user} 
        show={showModal} 
        schedule_event_callback={createNewEvent}
        onHide={()=>setShowModal(false)}/>
    </div>
  );
}

export default EventsList;
