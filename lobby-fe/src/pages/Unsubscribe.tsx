import { useEffect, useState } from 'react';
import { useSearchParams } from "react-router-dom";

import '../App.css';
import '../css/Unsubscribe.css'
import SubscriptionService from '../services/SubscriptionService';

function UnsubscribePage() {

  const [unsubscribed, setUnsubscribed] = useState<boolean>(false)
  const [searchParams, ] = useSearchParams();

  useEffect(() => {
    async function unsubscribe() {
      const email = searchParams.get("email")
      if (email) {
        const uudecoded_email = decodeURIComponent(email)
        await SubscriptionService.deleteSubscription(uudecoded_email);
        setUnsubscribed(true)
      }
    }
    unsubscribe();
  }, [unsubscribed, searchParams]);

  const email = searchParams.get("email");
  return (
    <div className="unsubscribe-main">
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
