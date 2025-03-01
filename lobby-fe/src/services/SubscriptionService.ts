/**
 * A class for performing CRUD operations on the Game database table.
 */
export default class SubscriptionService {

	static back_end = process.env.REACT_APP_LOBBY_BE_HOST

	/**
	 * Create a new subscription or update an existing subscription
	 * @param email the email to subscribe
	 * @param onSendCall whether to email this address when someone sends out a call
	 * @param onNewEvent whether to email this address when a new event is scheduled
	 */
	static async upsertSubscription(email: string, onSendCall: boolean, onNewEvent: boolean): Promise<void> {
		const headers: Headers = new Headers()
		headers.set('Content-Type', 'application/json')
		headers.set('Accept', 'application/json')

		// Create the request object, which will be a RequestInfo type. 
		// Here, we will pass in the URL as well as the options object as parameters.
		const request: RequestInfo = new Request(`${SubscriptionService.back_end}/subscription/${encodeURIComponent(email)}`, {
			method: 'PUT',
			headers: headers,
			body: JSON.stringify({address: email, on_send_call: onSendCall, on_new_event: onNewEvent})
		})

		await fetch(request);
	}

	/**
	 * Delete an existing subscription.
	 * @param email the email to remove
	 */
	static async deleteSubscription(email: string) {
		const headers: Headers = new Headers()
		headers.set('Content-Type', 'application/json')
		headers.set('Accept', 'application/json')

		// Create the request object, which will be a RequestInfo type. 
		// Here, we will pass in the URL as well as the options object as parameters.
		const request: RequestInfo = new Request(`${SubscriptionService.back_end}/subscription/${encodeURIComponent(email)}`, {
			method: 'DELETE',
			headers: headers
		})

		await fetch(request);
	}

}

