import { useState } from 'react';
import Button from 'react-bootstrap/Button';
import { Form } from 'react-bootstrap';
import '../App.css';
import SubscriptionService from '../services/SubscriptionService';


/**
 * Displays ability to subscribe or unsubscibe email notifications.
 */
function Subscribe() {

  const [subscribeEmail, setSubscribeEmail] = useState<string>('');
  const [subscribeEmailValid, setSubscribeEmailValid] = useState<boolean>(true);
  const [subscribeCall, setSubscribeCall] = useState<boolean>(false);
  const [subscribeEvent, setSubscribeEvent] = useState<boolean>(false);
  const [subscriptionChanged, setSubscriptionChanged] = useState<boolean>(true)
  const [unsubscribeEmail, setUnsubscribeEmail] = useState<string>('');
  const [unsubscribeEmailValid, setUnsubscribeEmailValid] = useState<boolean>(true);
  const [unsubscriptionChanged, setUnsubscriptionChanged] = useState<boolean>(true)

  /**
   * Returns true if the passed in string is of the form of an email
   * @param s an email address
   * @returns true if the email address is of the valid form
   */
  function isValidEmail(s: string): boolean {
    const emailRegex = /^[a-zA-Z0-9._%+-]+@[a-zA-Z0-9.-]+\.[a-zA-Z]{2,}$/;
    return emailRegex.test(s)
  }

  /**
   * A new value has been entered in the subscribe email field
   * @param new_value the value in the field
   */
  function subscribeEmailChanged(new_value: string) {
    setSubscriptionChanged(true);
    if (isValidEmail(new_value)) {
      setSubscribeEmail(new_value);
      setSubscribeEmailValid(true);
    } else {
      setSubscribeEmailValid(false);
    }
  }

  /**
   * A new value has been entered in the unsubscribe email field
   * @param new_value the value in the field
   */
    function unsubscribeEmailChanged(new_value: string) {
      setUnsubscriptionChanged(true);
      if (isValidEmail(new_value)) {
        setUnsubscribeEmail(new_value);
        setUnsubscribeEmailValid(true);
      } else {
        setUnsubscribeEmailValid(false);
      }
    }
  
  /**
   * When "Subscribe" is clicked, register the email in the subscriptions table.
   */
  function subscribeClicked() {
    SubscriptionService.upsertSubscription(subscribeEmail, subscribeCall, subscribeEvent)
    setSubscriptionChanged(false)
  }
  
  /**
   * When "Subscribe" is clicked, register the email in the subscriptions table.
   */
  function unsubscribeClicked() {
    SubscriptionService.deleteSubscription(unsubscribeEmail)
    setUnsubscriptionChanged(false)
  }

  return (

    <div>
    <div>Receive email when someone is looking to play.</div>
    <div>
      <Form>
        <Form.Label>Notify me at</Form.Label>
        <Form.Control 
          placeholder="your email"
          isValid={subscribeEmailValid}
          onChange={(event)=>subscribeEmailChanged(event.target.value)} />
        <Form.Check
          type="checkbox"
          id="call"
          label="when someone sends out a call (is online ready to play)"
          checked={subscribeCall}
          onChange={()=>{setSubscribeCall(!subscribeCall);setSubscriptionChanged(true);}}
        />          
        <Form.Check
          type="checkbox"
          id="event"
          label="when a new event is scheduled"
          checked={subscribeEvent}
          onChange={()=>{setSubscribeEvent(!subscribeEvent);setSubscriptionChanged(true);}}
        />
        <Button 
          disabled={(!subscribeCall && !subscribeEvent) || !subscribeEmail || !subscribeEmailValid || !subscriptionChanged} 
          onClick={subscribeClicked}
        >
          Subscribe
        </Button>
      </Form>
    </div>
    <div><p>&nbsp;</p></div>
    <div>
      <Form>
        <Form.Label>Unsubscribe from all notifications</Form.Label>
        <Form.Control 
          placeholder="your email"
          isValid={unsubscribeEmailValid}
          onChange={(event)=>unsubscribeEmailChanged(event.target.value)} />
        <Button
          disabled={!unsubscribeEmail || !unsubscribeEmailValid || !unsubscriptionChanged}
          onClick={unsubscribeClicked}
        >
          Unsubscribe
        </Button>
      </Form>
    </div>
  </div>
);
}

export default Subscribe;
