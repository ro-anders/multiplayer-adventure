/**
 * The service manager controls the lifecycle of this service.
 * It periodically reports its up status to the lobby backend
 * and if it recognizes it is no longer being used, shuts itself down.
 */

import { 
	ECSClient,
	DescribeTasksCommand, DescribeTasksCommandInput, DescribeTasksCommandOutput,
	ListTasksCommand, ListTasksCommandInput, ListTasksCommandOutput,
	Task
 } from '@aws-sdk/client-ecs'
 import {
	EC2Client,
    DescribeNetworkInterfacesCommand, DescribeNetworkInterfacesCommandInput, DescribeNetworkInterfacesCommandOutput,
 } from '@aws-sdk/client-ec2'
import LobbyBackend from './LobbyBackend'

export default class ServiceMgr {

	/** The time period to wait before deciding no one is using the service
	 * and shutting down.
	 */
	static SHUTDOWN_SERVICE_TIMEOUT = 10 * 60 * 1000 // 10 minutes in milliseconds
	//static SHUTDOWN_SERVICE_TIMEOUT = 60000 // 1 minutes 

	constructor(private lobby_backend: LobbyBackend,
				private last_comm_time: number = Date.now(),
				private interval_id: NodeJS.Timeout = null,
				private ip: string = null
	) 
	{		
		this.report_to_lobby()
		console.log("Creating periodic update")
		this.interval_id = setInterval(this.periodic_update.bind(this), ServiceMgr.SHUTDOWN_SERVICE_TIMEOUT)
	}	 

	/**
	 * Mark that we just received another websocket message from a running game.
	 * Needs to know this becuase when messages stop coming, this service shuts down.
	 */
	got_game_message() {
		this.last_comm_time = Date.now()
	}

	/**
	 * Posts to the lobby back end that it is still up.
	 */	
	async report_to_lobby() {
		if (this.ip === null) {
			this.ip = await this.get_ip()
		}
		this.lobby_backend.set_gamesever_ip(this.ip)
	}

	/**
	 * This service does two things periodically.
	 * First, it reports that it is up to the lobby.
	 * Second, if it has not received any websocket messages within a given time limit it
	 * decides it is not being used and shuts down.
	 */
	periodic_update() {
		console.log("Running periodic update")
		if (Date.now() - this.last_comm_time > ServiceMgr.SHUTDOWN_SERVICE_TIMEOUT) {
			this.shutdown()
		}
		else {
			this.report_to_lobby()
		}
	}

	/**
	 * No games are currently being played.  Shut down the game backend service.
	 */
	async shutdown() {
		console.log("Game Backend shutting down due to inactivity");
		clearInterval(this.interval_id);

		this.lobby_backend.set_gamesever_ip(null);

		// Shutdown
		process.exit(0);
	}

	/**
	 * Query AWS for the IP address that this service's Fargate Service is using.
	 */
	async get_ip(): Promise<string> {

		if (process.env.NODE_ENV === 'development') {
			return '127.0.0.1'
		}
		
		try {
			console.log("Determining IP")
			// aws ecs list-tasks --cluster h2hadv-serverCluster | jq -re ".taskArns[0]
			const ecs_client: ECSClient = new ECSClient();
			const list_tasks_req: ListTasksCommandInput = {
				cluster: 'h2hadv-serverCluster'
			}
			const list_tasks_resp: ListTasksCommandOutput = 
				await ecs_client.send(new ListTasksCommand(list_tasks_req))
			const task_arn: string = list_tasks_resp.taskArns[0]
			console.log(`Task ARN = ${task_arn}`)

			// aws ecs describe-tasks --cluster h2hadv-serverCluster --task $TASKARN | \
			//   jq -r -e '.tasks[0].attachments[0].details[] | select(.name=="networkInterfaceId").value'
			const desc_tasks_req: DescribeTasksCommandInput = {
				cluster: 'h2hadv-serverCluster',
				tasks: [task_arn]
			}
			var eni = null
			const desc_tasks_resp: DescribeTasksCommandOutput = 
				await ecs_client.send(new DescribeTasksCommand(desc_tasks_req))
			const task: Task = desc_tasks_resp.tasks[0]
			const attachment_details = task.attachments[0].details
			for (var detail of attachment_details) {
				if (detail.name === "networkInterfaceId") {
					eni = detail.value
					break
				}
			}
			if (!eni) {
				throw new Error(`Could not find ENI for task ${task_arn}`)
			}
			console.log(`ENI = ${eni}`)

			// aws ec2 describe-network-interfaces --network-interface-ids $ENI | \
			//   jq -r -e ".NetworkInterfaces[0].Association.PublicIp"
			const ec2_client: EC2Client = new EC2Client();
			const desc_net_req: DescribeNetworkInterfacesCommandInput = {
				NetworkInterfaceIds: [eni]
			}
			const desc_net_resp: DescribeNetworkInterfacesCommandOutput =
				await ec2_client.send(new DescribeNetworkInterfacesCommand(desc_net_req))
			const ip = desc_net_resp.NetworkInterfaces[0].Association.PublicIp
			console.log(`IP = ${ip}`)
			return ip
		}
		catch (err: any) {
			console.log(`Encountered error determining IP: ${err.message}`)
			const stack = err.stack.split("\n").slice(1, 4).join("\n");
			console.log(stack); 
			
			return null;
		}

		return ''
	}


  
  
}

