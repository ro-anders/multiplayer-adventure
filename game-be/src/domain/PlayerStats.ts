
/**
 * A struture containing the statistics on a player.
 */
export interface PlayerStats {
	playername: string;
	games: number; // number games played
	wins: number;  // number games won
	achvmts: number;  // number steps along path to easter egg have been achieved
	achvmt_time: number; // timestamp when the last achievment was achieved
}
