// Create clients and set shared const values outside of the handler.

// Create a DocumentClient that represents the query to add an item
import { DynamoDBClient, ListTablesCommand, CreateTableCommand } from '@aws-sdk/client-dynamodb';
import { DynamoDBDocumentClient, ScanCommand } from '@aws-sdk/lib-dynamodb';

const local_dynamo_connect_config = {
  region: 'local',
  endpoint: 'http://docker.for.mac.localhost:8000',
  credentials: {
    accessKeyId: 'xxx',
    secretAccessKey: 'yyy',
  },
}

const production_dynamo_connect_config = {
  region: 'us-east-2'
}

/** The time a player will stay in the active players table before being cleaned out by the system. */
export const ACTIVE_PLAYERS_TTL = 60 * 60 * 1000; // One hour

export const DDBClient = new DynamoDBClient(
  (process.env.ENVIRONMENT_TYPE === 'Dev' ? local_dynamo_connect_config : production_dynamo_connect_config)
);
const ddbDocClient = DynamoDBDocumentClient.from(DDBClient);

/**
 * Call this when the dynamodb is brand new and needs a schema
 */
const initializeSchema = async () => {

  // Create the Settings table
  const settingsDef = { 
    AttributeDefinitions: [ 
      { 
        AttributeName: "setting_name", 
        AttributeType: "S", 
      },
    ],
    TableName: "Settings"+process.env.ENVIRONMENT_TYPE, 
    KeySchema: [ 
      { 
        AttributeName: "setting_name", 
        KeyType: "HASH", 
      },
    ],
    BillingMode: "PAY_PER_REQUEST"
  };
  await ddbDocClient.send(new CreateTableCommand(settingsDef))


  // Create the players table.  This only keeps the players last active time.
  // All other player info is kept in another table, and this drops non-recent players
  // so all players can be returned in a 1MB call. 
  const playersDef = { 
    AttributeDefinitions: [ 
      { 
        AttributeName: "playername", 
        AttributeType: "S", 
      }
    ],
    TableName: "Players"+process.env.ENVIRONMENT_TYPE, 
    KeySchema: [ 
      { 
        AttributeName: "playername", 
        KeyType: "HASH", 
      }
    ],
    BillingMode: "PAY_PER_REQUEST"
  };
  await ddbDocClient.send(new CreateTableCommand(playersDef))


  // Create the player stats table.  This only keeps a couple running counts on each player
  // in hopes that getting all stats doesn't exceed 1MB.  If it does, we'll have to add paging
  // or an index or something.
  const playerStatsDef = { 
    AttributeDefinitions: [ 
      { 
        AttributeName: "playername", 
        AttributeType: "S", 
      }
    ],
    TableName: "PlayerStats"+process.env.ENVIRONMENT_TYPE, 
    KeySchema: [ 
      { 
        AttributeName: "playername", 
        KeyType: "HASH", 
      }
    ],
    BillingMode: "PAY_PER_REQUEST"
  };
  await ddbDocClient.send(new CreateTableCommand(playerStatsDef))


  // Create the games table
  const gamesDef = { 
    AttributeDefinitions: [ 
      { 
        AttributeName: "session", 
        AttributeType: "N", 
      },
    ],
    TableName: "Games"+process.env.ENVIRONMENT_TYPE, 
    KeySchema: [ 
      { 
        AttributeName: "session", 
        KeyType: "HASH", 
      },
    ],
    BillingMode: "PAY_PER_REQUEST"
  };
  await ddbDocClient.send(new CreateTableCommand(gamesDef))


  // Create the scheduled events table
  const eventsDef = { 
    TableName: "ScheduledEvents"+process.env.ENVIRONMENT_TYPE, 
    AttributeDefinitions: [ 
      { 
        AttributeName: "partitionkey", 
        AttributeType: "S", 
      },
      {
        AttributeName: "starttime", 
        AttributeType: "N", 
      }
    ],
    KeySchema: [ 
      { 
        AttributeName: "partitionkey", 
        KeyType: "HASH", 
      },
      { 
        AttributeName: "starttime", 
        KeyType: "RANGE", 
      },
    ],
    BillingMode: "PAY_PER_REQUEST"
  };
  await ddbDocClient.send(new CreateTableCommand(eventsDef))


  // Create the chat table
  const chatDef = { 
    AttributeDefinitions: [ 
      { 
        AttributeName: "partitionkey", 
        AttributeType: "S", 
      },
      {
        AttributeName: "sortkey", 
        AttributeType: "S", 
      }
    ],
    TableName: "Chat"+process.env.ENVIRONMENT_TYPE, 
    KeySchema: [ 
      { 
        AttributeName: "partitionkey", 
        KeyType: "HASH", 
      },
      { 
        AttributeName: "sortkey", 
        KeyType: "RANGE", 
      },
    ],
    BillingMode: "PAY_PER_REQUEST"
  };
  await ddbDocClient.send(new CreateTableCommand(chatDef))

  // Create the subscriptions  table
  const subsDef = { 
    TableName: "Subscriptions"+process.env.ENVIRONMENT_TYPE, 
    AttributeDefinitions: [ 
      { 
        AttributeName: "address", 
        AttributeType: "S", 
      }
    ],
    KeySchema: [ 
      { 
        AttributeName: "address", 
        KeyType: "HASH", 
      }
    ],
    BillingMode: "PAY_PER_REQUEST"
  };
  await ddbDocClient.send(new CreateTableCommand(subsDef))

} 

/**
 * Checks to see if the dynamodb has its schema setup.  If not, sets up the schema.
 */
export const CheckDDB = async () => {
  // Only needs to check schema in development
  if (process.env.ENVIRONMENT_TYPE === 'Dev') {
    const data = await ddbDocClient.send(new ListTablesCommand());
    if (!'TableNames' in data) {
      throw new Error(`Unexpected response from database.: ${data}`);
    }
    if (!data.TableNames.includes('Players'+process.env.ENVIRONMENT_TYPE)) {
      console.log("No schema detected.  Initializing database schema.")
      await initializeSchema();
    }
  }
}
