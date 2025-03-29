/**
 * Represents the statistics about a player
 */
export interface PlayerStat {
    /** username of the player */
    playername : string

    /** the number of games this player has played */
    games: number

    /** the number of games this player has won */
    wins: number

    /** how many steps has player taken to achieving easter egg */
    achvmts: number
}

