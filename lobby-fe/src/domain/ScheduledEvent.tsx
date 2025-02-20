/**
 * Represents a time that someone has scheduled to say they are going to play
 * at this time.  Others can also say they are going to join at this time.
 */
export interface ScheduledEvent {
    starttime: number;
    note: string;
    players: string[];
}

