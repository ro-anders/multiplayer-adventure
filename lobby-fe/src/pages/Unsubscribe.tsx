import { useEffect, useState } from 'react';
import { Form, Button } from 'react-bootstrap';
import { useNavigate, useSearchParams } from "react-router-dom";

import '../App.css';
import '../css/Login.css'
import SubscriptionService from '../services/SubscriptionService';

function UnsubscribePage() {

  const [unsubscribed, setUnsubscribed] = useState<boolean>(false)
  const [searchParams, setSearchParams] = useSearchParams();

  useEffect(() => {
    async function unsubscribe() {
      const email = searchParams.get("email")
      if (email) {
        await SubscriptionService.deleteSubscription(email);
        setUnsubscribed(true)
      }
    }
    unsubscribe();
  }, [unsubscribed]);

  const email = searchParams.get("email");
  return (
    <div>
      {!email &&
        <p>No operation.</p>
      }
      {email && !unsubscribed &&
        <p>Unsubscribing...</p>
      }
      {email && unsubscribed &&
        <p>Unsubscribed {email}.</p>
      }
    </div>
  );
}

export default UnsubscribePage;
