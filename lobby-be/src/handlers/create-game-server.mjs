import { ECSClient, RunTaskCommand } from "@aws-sdk/client-ecs";
import { DynamoDBDocumentClient, GetCommand, PutCommand, DeleteCommand } from '@aws-sdk/lib-dynamodb';
import {DDBClient, CheckDDB} from '../dbutils/dbsetup.mjs'

const ddbDocClient = DynamoDBDocumentClient.from(DDBClient);

/** How often the game backend pings the lobby backend to let it know it's still up */
const GAMEBACKEND_PING_PERIOD = 1 * 60 * 1000 // milliseconds
// DON'T CHANGE THIS WITHOUT CHANGING LOBBY FRONTEND AND GAME BACKEND CONSTANTS!!!

export const createGameServerHandler = async (event) => {
    console.info('received:', event);
    console.log(`Calling ${event.httpMethod} ${event.path} while ENVIRONMENT_TYPE=${process.env.ENVIRONMENT_TYPE} ` +
      `fullpath=https://${event.requestContext.domainName}/${event.requestContext.path}`
    )
    if (event.httpMethod !== 'POST') {
        throw new Error(`postMethod only accepts POST method, you tried: ${event.httpMethod} method.`);
    }

    await CheckDDB();

    let status_code = 200
    if (process.env.ENVIRONMENT_TYPE !== 'Dev') {  
      
      const game_server_running = await isGameServerRunning()
      if (game_server_running) {
        console.log("Game server is already running")
        status_code = 302;
      } else {
        console.log('Setting game server ip to "starting"')
        // Set the game server setting to "starting".
        // I know we just checked if the server is already running, but
        // we need to check and update in a single transaction, so
        // change to "starting" but abort if someone else has added an
        // entry between when we checked and now.
        const setting_name='game_server_ip'
        const dynamo_params = {
            TableName: "Settings"+process.env.ENVIRONMENT_TYPE,
            Item: {
                setting_name: setting_name,
                setting_value: 'starting',
                time_set: Date.now()
            },
            ConditionExpression: 'attribute_not_exists(setting_name)'
        }
        try {
            // Execute transaction
            await ddbDocClient.send(new PutCommand(dynamo_params))


            // The full path is probably something like /Prod/setting/game_server_ip and we want the "/Prod" part.
            // So pull the REST path off the end of the full path to get that.
            const path_prefix = ( event.requestContext.path.endsWith(event.path) ?
              event.requestContext.path.slice(0, -event.path.length) : event.requestContext.path);
            const lobby_url = `https://${event.requestContext.domainName}${path_prefix}`
            const accountId = event.requestContext.accountId
            // Then issue the AWS commands to spawn a server in a fargate task
            // aws ecs run-task \
            // --cluster h2hadv-server-prod \ 
            // --task-definition arn:aws:ecs:us-east-2:637423607158:task-definition/h2hadv-server-prod \
            // --launch-type FARGATE \
            // --network-configuration "awsvpcConfiguration={subnets=[subnet-0d46ce42b6ae7a1ee,subnet-011083badbc3f216e],securityGroups=[sg-07539077994dfb96c],assignPublicIp=ENABLED}" \
            // --overrides '{ "containerOverrides": [ { "name": "h2hadv-server", "environment": [ { "name": "LOBBY_URL", "value": "https://xx11yyyy11.execute-api.us-east-2.amazonaws.com/Prod" } ] } ] }'
            // Couple of things are hard-coded that we eventually want to make dynamic
            const env = process.env.NODE_ENV.toLowerCase();
            const subnets = ["subnet-0d46ce42b6ae7a1ee","subnet-011083badbc3f216e"]
            const security_group = "sg-07539077994dfb96c"
            const region = `us-east-2`
            const ecsClient = new ECSClient();
            const ecs_params = {
                cluster: `h2hadv-server-${env}`,              
                taskDefinition: `arn:aws:ecs:${region}:${accountId}:task-definition/h2hadv-server-${env}`,    
                launchType: "FARGATE",
                networkConfiguration: {
                  awsvpcConfiguration: {
                    subnets: subnets,
                    securityGroups: [security_group],      
                    assignPublicIp: "ENABLED",
                  },
                },
                overrides: {
                  containerOverrides: [
                    {
                      name: `h2hadv-server-${env}`,               
                      environment: [
                        { name: "LOBBY_URL", value: lobby_url },
                        { name: "NODE_ENV", value: (env === 'prod' ? 'production' : 'test') }
                      ],
                    },
                  ],
                },
                count: 1,
              };
              console.log(`Spawning game server with LOBBY_URL=${lobby_url}`)
              const command = new RunTaskCommand(ecs_params);
              const response = await ecsClient.send(command);
              console.log("Task started:", response.tasks[0].taskArn);
                    
        } catch (error) {
            if (error.name === "ConditionalCheckFailedException") {
                console.error("Aborting.  Game server already in starting or running state.");
                status_code = 302;
            } else {
                console.error("Unexpected error recording game server as starting.", error);
                status_code = 500;
            }
        }
          
      }
    }
    
    const response = {
        statusCode: status_code,
        headers: {
            "Access-Control-Allow-Headers" : "Content-Type",
            "Access-Control-Allow-Origin": "*", // Allow from anywhere 
            "Access-Control-Allow-Methods": "PUT" // Allow only GET request 
        }
    };

    return response;
};

/**
 * Check the database to see if a game server is already running.
 * Make sure the database entry is recent.  If it isn't, delete the entry.  It must
 * be left behind from a crashed instance.
 * @returns true if a recent setting exists in the database.
 */
const isGameServerRunning = async () => {
  var found_server = false;
  try {
    // Query the database for the game server setting.
    var params = {
      TableName : "Settings"+process.env.ENVIRONMENT_TYPE,
      Key: { setting_name: "game_server_ip" },
    };
    const data = await ddbDocClient.send(new GetCommand(params));
    if (data.Item) {
      // There is a game setting in the database.  See if it is recent.
      const value = data.Item.setting_value;
      const time_set = data.Item.time_set;
      const time_since = Date.now() - time_set;
      // If the game server is starting it may take a few minutes to start up.
      // Otherwise it should have been updated within the last minute.
      const max_time = (value === "starting" ? 240000 /* four minutes */ : 2 * GAMEBACKEND_PING_PERIOD)
      const too_old = time_since > max_time;
      if (too_old) {
        console.log("Found server that is old.  Deleting")
        // There is an entry in the database, but it's out of date and
        // probably from a crashed process.  Delete it and report no server.
        var delete_params = {
          TableName : "Settings"+process.env.ENVIRONMENT_TYPE,
          Key: { setting_name: "game_server_ip" }
        };
        try {
          await ddbDocClient.send(new DeleteCommand(params));
        } catch (err) {
          console.log("Error deleting game_server_ip setting", err.stack);
        }
      }
      else {
        // Found a server that looks recent
        found_server = true
      }
    }
  } catch (err) {
    console.log("Error retrieving game_server_ip setting", err);
  }

  return found_server;
}

