import { useState } from 'react';
import { Modal, Button, Form } from 'react-bootstrap';
import "../css/ProposeModal.css"
import { ScheduledEvent } from '../domain/ScheduledEvent';

interface ScheduleEventModalProps {
    /** The current username */
    current_user: string;
    /** Whether the modal is visible */
    show: boolean;
    /** The callback to register a newly scheduled event */
    schedule_event_callback: (new_event: ScheduledEvent) => void;
    /** The callback to hide the modal */
    onHide: () => void;
  }
  
/**
 * Modal that lets user schedule a new event.
 */
function ScheduleEventModalModal({current_user, show, schedule_event_callback, onHide}: ScheduleEventModalProps) {
  const [start, setStart] = useState<Date>(new Date());
  const [dateValid, setDateValid] = useState<boolean>(true);
  const [timeValid, setTimeValid] = useState<boolean>(true);

  function scheduleClicked() {
    const new_event: ScheduledEvent = {
      starttime: start.getTime(),
      note: "",
      players: [current_user]
    }
    schedule_event_callback(new_event)
    onHide();
  }

  function validateTime(e: React.ChangeEvent<HTMLInputElement >) {
    // See if the value is of the correct format.  If not, do not process it, but
    // flag an error.
    const timeStr = e.currentTarget.value;
    const timeRegex = /^(1[0-2]|0?[1-9]):([0-5][0-9])(AM|PM)$/i;
    const match = timeStr.match(timeRegex);
    if (!match) {
        setTimeValid(false)
    } else {
      setTimeValid(true)
      // Time is valid.  Parse it and put it in the time part of the startDate.
      let [_, hour, minute, period] = match;
      let hours = parseInt(hour, 10);
      let minutes = parseInt(minute, 10);
  
      if (period.toUpperCase() === "PM" && hours !== 12) {
          hours += 12;
      } else if (period.toUpperCase() === "AM" && hours === 12) {
          hours = 0;
      }
      start.setHours(hours, minutes, 0, 0); // Set time while keeping today's date
      setStart(start)

    }

  }

  return (
        <Modal 
          dialogClassName="schedule-modal App" 
          size="lg"
          show={show} 
          onHide={onHide}>
        <Modal.Header closeButton>
          <Modal.Title>Schedule an Event</Modal.Title>
        </Modal.Header>
        <Modal.Body className="propose-modal-body">

          <div className="schedule-modal-field">
            <Form.Label>Choose a date to play</Form.Label>
            <Form.Control className="schedule-form-field" type="text" placeholder="April 1"/>
          </div>
          <div className="schedule-modal-field">
            <Form.Label>Choose a time to play</Form.Label>
            <Form.Control 
              className="schedule-form-field" 
              type="text" 
              placeholder="10:00PM" 
              isInvalid={!timeValid}
              onChange={validateTime}/>
          </div>
          Computed={start.toISOString()}={new Date(start.getTime()).toISOString()}
        </Modal.Body>
        <Modal.Footer>
          <Button variant="secondary" onClick={onHide}>
            Cancel
          </Button>
          <Button 
            variant="primary" 
            onClick={scheduleClicked}
            disabled={!timeValid}>
            Schedule
          </Button>
        </Modal.Footer>
      </Modal>
      );
}
    
export default ScheduleEventModalModal;
    