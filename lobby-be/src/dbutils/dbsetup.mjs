// Create clients and set shared const values outside of the handler.

// Create a DocumentClient that represents the query to add an item
import { DynamoDBClient, ListTablesCommand, CreateTableCommand } from '@aws-sdk/client-dynamodb';
import { DynamoDBDocumentClient, ScanCommand } from '@aws-sdk/lib-dynamodb';
import { NodeHttpHandler }  from "@aws-sdk/node-http-handler";

export const DDBClient = new DynamoDBClient({
    region: 'local',
    endpoint: 'http://docker.for.mac.localhost:8000',
    credentials: {
      accessKeyId: 'xxx',
      secretAccessKey: 'yyy',
    },
  });
const ddbDocClient = DynamoDBDocumentClient.from(DDBClient);

/**
 * Call this when the dynamodb is brand new and needs a schema
 */
const initializeSchema = async () => {

  // Create the SampleTable table
  const input = { 
    AttributeDefinitions: [ 
      { 
        AttributeName: "id", 
        AttributeType: "S", 
      },
    ],
    TableName: "SampleTable", 
    KeySchema: [ 
      { 
        AttributeName: "id", 
        KeyType: "HASH", 
      },
    ],
    BillingMode: "PAY_PER_REQUEST"
  };
  await ddbDocClient.send(new CreateTableCommand(input))
  // Create the players table
  const playersDef = { 
    AttributeDefinitions: [ 
      { 
        AttributeName: "playername", 
        AttributeType: "S", 
      },
    ],
    TableName: "Players", 
    KeySchema: [ 
      { 
        AttributeName: "playername", 
        KeyType: "HASH", 
      },
    ],
    BillingMode: "PAY_PER_REQUEST"
  };
  await ddbDocClient.send(new CreateTableCommand(playersDef))
  // Create the games table
  const gamesDef = { 
    AttributeDefinitions: [ 
      { 
        AttributeName: "session", 
        AttributeType: "N", 
      },
    ],
    TableName: "Games", 
    KeySchema: [ 
      { 
        AttributeName: "session", 
        KeyType: "HASH", 
      },
    ],
    BillingMode: "PAY_PER_REQUEST"
  };
  await ddbDocClient.send(new CreateTableCommand(gamesDef))
} 

/**
 * Checks to see if the dynamodb has its schema setup.  If not, sets up the schema.
 */
export const CheckDDB = async () => {
  // Only needs to check schema in development
  if (process.env.ENVIRONMENT_TYPE === 'development') {
    const data = await ddbDocClient.send(new ListTablesCommand());
    if (!'TableNames' in data) {
      throw new Error(`Unexpected response from database.: ${data}`);
    }
    if (!data.TableNames.includes('Players')) {
      console.log("No schema detected.  Initializing database schema.")
      await initializeSchema();
    }
  }
}
