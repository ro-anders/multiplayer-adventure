import {ScheduledEvent} from '../domain/ScheduledEvent'
import SubscriptionService from './SubscriptionService'

/**
 * A class for performing CRUD operations on the Game database table.
 */
export default class GameService {

	static back_end = process.env.REACT_APP_LOBBY_BE_HOST

	/**
	 * Fetch all scheduled events that occur today or after today.
	 * @returns list of scheduled events
	 */
	static async getScheduledEvents(): Promise<ScheduledEvent[]> {
		const headers: Headers = new Headers()
		headers.set('Content-Type', 'application/json')
		headers.set('Accept', 'application/json')

		const request: RequestInfo = new Request(`${GameService.back_end}/event`, {
			method: 'GET',
			headers: headers
		})

		return fetch(request)
			// the JSON body is taken from the response
			.then(res => res.json())
			.then(res => {
			return res
		})
	}

	/**
	 * Create a new scheduled event or update an existing scheduled event
	 * @param scheduled_event the details of the event
	 * @param isNew true if this is a brand new event
	 */
	static async upsertScheduleEvent(
		event: ScheduledEvent,
		isNew: boolean
	): Promise<void> {
		const headers: Headers = new Headers()
		headers.set('Content-Type', 'application/json')
		headers.set('Accept', 'application/json')

		// Create the request object, which will be a RequestInfo type. 
		// Here, we will pass in the URL as well as the options object as parameters.
		const request: RequestInfo = new Request(`${GameService.back_end}/event`, {
			method: 'PUT',
			headers: headers,
			body: JSON.stringify(event)
		})
		await fetch(request);

		// If this is a brand new event, notify everyone who subscribed
		// to new events.
		if (isNew) {
			await SubscriptionService.notify('newscheduledevent', 
				{ initiator: event.players[0], starttime: event.starttime })
		}
	}

	/**
	 * Delete an existing scheduled event.
	 * @param event the details of the event
	 */
	static async deleteScheduledEvent(event: ScheduledEvent) {
		const headers: Headers = new Headers()
		headers.set('Content-Type', 'application/json')
		headers.set('Accept', 'application/json')

		// Create the request object, which will be a RequestInfo type. 
		// Here, we will pass in the URL as well as the options object as parameters.
		const request: RequestInfo = new Request(`${GameService.back_end}/event/${event.starttime}`, {
			method: 'DELETE',
			headers: headers
		})

		await fetch(request);
	}

}

