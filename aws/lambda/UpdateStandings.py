import boto3
import json
import time

'''
Updates the standings in response to game win event

Copy this file into UpdateStandings lambda
'''
def lambda_handler(event, context):
    '''
    event will be of the form
    {
        'Won': name of winner
        'Lost': [array of names of losers]
    }

    '''
    STANDING_PK='Standing'
    try:
        print("event = " + json.dumps(event))
    
        client = boto3.client('dynamodb')
        table = 'global'

        # Handle the winner first
        # Load their standing if it already exists
        key={
            'PK': {'S': STANDING_PK},
            'SK': {'S': event['Won']}
        }
        response = client.get_item(TableName=table, Key=key)
        print("check for winner response = {}".format(json.dumps(response)))
        standing = response['Item'] if 'Item' in response else {
            'PK': {'S': STANDING_PK},
            'SK': {'S': event['Won']},
            'Username': {'S': event['Won']},
            'Wins': {'N': '0'},
            'Losses': {'N': '0'}
        }
        newWins = int(standing['Wins']['N'])+1
        standing['Wins'] = {'N': str(newWins)}
        print("Upserting {} standing with win".format(event['Won']))
        client.put_item(TableName=table, Item=standing)

        # Handle the losers
        for loser in event['Lost']:
            # Load their standing if it already exists
            key={
                'PK': {'S': STANDING_PK},
                'SK': {'S': loser}
            }
            response = client.get_item(TableName=table, Key=key)
            print("check for loser response = {}".format(json.dumps(response)))
            standing = response['Item'] if 'Item' in response else {
                'PK': {'S': STANDING_PK},
                'SK': {'S': loser},
                'Username': {'S': loser},
                'Wins': {'N': '0'},
                'Losses': {'N': '0'}
            }
            newLosses = int(standing['Losses']['N'])+1
            standing['Losses'] = {'N': str(newLosses)}
            print("Upserting {} standing with loss".format(loser))
            client.put_item(TableName=table, Item=standing)

        return {
            'statusCode': 200,
            'body': 'Upsert succeeded'
        }

    except Exception as e:
        message = 'Upsert failed: {}'.format(e)
        print("Hit exception.  Returning: " + message)
        return {
            'statusCode': 500,
            'body': message
        }
