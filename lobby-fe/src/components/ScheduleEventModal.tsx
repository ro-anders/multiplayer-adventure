import { useState } from 'react';
import { Modal, Button, Form } from 'react-bootstrap';
import DatePicker from "react-datepicker";
import "react-datepicker/dist/react-datepicker.css";
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
function ScheduleEventModal({current_user, show, schedule_event_callback, onHide}: ScheduleEventModalProps) {
  const [eventDate, setEventDate] = useState<Date>(new Date())
  const [eventHour, setEventHour] = useState<number>(0);
  const [eventMinute, setEventMinutes] = useState<number>(0);
  const [hasTimeHasBeenPicked, setHasTimeBeenPicked] = useState<boolean>(false)
  const [isTimeValid, setIsTimeValid] = useState<boolean>(true);

  /**
   * Event handler for when the user clicks the "Schedule" button.
   * Construct the events full data and time, pass that back to the page,
   * and close the modal.
   */
  function scheduleClicked() {
    // Add the time to the picked date
    eventDate.setHours(eventHour, eventMinute, 0, 0); 
    const new_event: ScheduledEvent = {
      starttime: eventDate.getTime(),
      note: "",
      players: [current_user]
    }
    schedule_event_callback(new_event)
    onHide();
  }

  /**
   * Event handler for when a user types a time in the form.
   * Validates that it is a valid time and then saves the time
   * chosen.
   * @param e React event of changing the form value
   */
  function timePicked(e: React.ChangeEvent<HTMLInputElement >) {
    // See if the value is of the correct format.  If not, do not process it, but
    // flag an error.
    const timeStr = e.currentTarget.value;
    const timeRegex = /^(1[0-2]|0?[1-9]):([0-5][0-9])(AM|PM)$/i;
    const match = timeStr.match(timeRegex);
    if (!match) {
        setIsTimeValid(false)
    } else {
      setIsTimeValid(true)
      // Time is valid.  Parse it and put it in the time part of the startDate.
      let [, hour, minute, period] = match;
      let hours = parseInt(hour, 10);
      let minutes = parseInt(minute, 10);
  
      if (period.toUpperCase() === "PM" && hours !== 12) {
          hours += 12;
      } else if (period.toUpperCase() === "AM" && hours === 12) {
          hours = 0;
      }
      setEventHour(hours)
      setEventMinutes(minutes)
      setHasTimeBeenPicked(true);
    }

  }

  /**
   * Event handler when user clicks a date int the date picker.
   * Just stores the date chosen.
   * @param date date passed back from the DatePicker
   * @param event ReactEvent like mouse click that triggered data being picked
   */
  function datePicked(date: Date | null, event: any) {
    if (date) {
      setEventDate(date);
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
            <Form.Group>
              <DatePicker
                selected={eventDate}
                onChange={datePicked}
              />
            </Form.Group>
          </div>
          <div className="schedule-modal-field">
            <Form.Label>Choose a time to play</Form.Label>
            <Form.Control 
              className="schedule-form-field" 
              type="text" 
              placeholder="10:00PM" 
              isInvalid={!isTimeValid}
              onChange={timePicked}/>
          </div>
        </Modal.Body>
        <Modal.Footer>
          <Button variant="secondary" onClick={onHide}>
            Cancel
          </Button>
          <Button 
            variant="primary" 
            onClick={scheduleClicked}
            disabled={!isTimeValid || !hasTimeHasBeenPicked}>
            Schedule
          </Button>
        </Modal.Footer>
      </Modal>
      );
}
    
export default ScheduleEventModal;
    