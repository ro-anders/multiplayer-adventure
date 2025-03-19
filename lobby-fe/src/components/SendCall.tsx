import { useState } from 'react';
import Button from 'react-bootstrap/Button';
import Modal from 'react-bootstrap/Modal'
import { Form } from 'react-bootstrap';
import '../App.css';
import SubscriptionService from '../services/SubscriptionService';

interface SendCallProps {
  /** The name of the currently logged in user */
  current_user: string;
}



/**
 * Displays a button to send an email to all subscribed users that
 * someone is online and wants to play.
 */
function Subscribe({current_user}: SendCallProps) {

  const [showModal, setShowModal] = useState<boolean>(false)
  const [callSent, setCallSent] = useState<boolean>(false)

  /**
   * When "Subscribe" is clicked, register the email in the subscriptions table.
   */
  function sendCallClicked() {
    setShowModal(true)
  }

  function onHideModal() {
    setShowModal(false)
  }

  function sendCall() {
    setShowModal(false)
    SubscriptionService.notify('sendcall', {initiator: current_user} )
    setCallSent(true)

  }
  
  return (

    <div>
      <p>Some people want to be notified when others are online and want to play.</p>
      <p>&nbsp;</p>
      {!callSent && 
        <div>
          <p>Notify them.</p>
          <Form>
            <Button onClick={sendCallClicked}>Send Call</Button>
          </Form>
        </div>
      }
      {callSent && 
        <div>
          <p>Users have been notified.</p>
          <p>Consider subscribing yourself to such notifications with "Get notified"</p>
          <p>or go back to the lobby to wait</p>
        </div>
      }
      <Modal 
          dialogClassName="schedule-modal App" 
          size="lg"
          show={showModal} 
          onHide={onHideModal}>
        <Modal.Header closeButton>
          <Modal.Title>Sending out a call...</Modal.Title>
        </Modal.Header>
        <Modal.Body className="propose-modal-body">
          <p>It may take a few minutes for people to disengage from whatever they are doing,
            get to their computers and login.  It would be very frustrating to find, after doing
            all that, that you waited two minutes and left.  If you send out a call you are committing
            to waiting for at least 15 minutes for someone to show up.
          </p>
        </Modal.Body>
        <Modal.Footer>
          <p>Will you stay on this site for the next fifteen minutes?</p>
          <Button variant="secondary" onClick={onHideModal}>
            Nevermind
          </Button>
          <Button 
            variant="primary" 
            onClick={sendCall}>
            Yes
          </Button>
        </Modal.Footer>
      </Modal>
    </div>
);
}

export default Subscribe;
