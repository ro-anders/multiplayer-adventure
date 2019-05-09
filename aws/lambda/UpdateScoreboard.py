import boto3
import json
import time

'''
Updates the race-to-the-egg scoreboard with new achievment

Copy this file into UpdateScoreboard lambda
'''
def lambda_handler(event, context):
    '''
    event will be of the form
    {
        'Player': name of winner
        'Stage': int ( 0 = found Robinett room
                       1 = glimpsed crystal castle 
                       2 = found crystal castle
                 )
    }

    '''
    SCOREBOARD_PK='EggAchievment'
    SCOREBOARD_TOP_PK='ScoreboardStatus'
    SCOREBOARD_TOP_SK=SCOREBOARD_TOP_PK
    MINIMUM_STAGE_TO_ANNOUNCE = 2;
    MESSAGES = [
        # No message for first two stages
        '{} has found the crystal castle, but only one can win the egg',
        '{} has found the crystal key, but only one can win the egg',
        '{} has opened the crystal gate, but only one can win the egg',
        '{} has won the egg'
    ]

    try:
        print("event = " + json.dumps(event))
    
        client = boto3.client('dynamodb')
        table = 'global'

        # First see if this player has already accomplished this
        # achievment.  If they have, we do nothing
        sk = "{}-{}".format(event['Player'], event['Stage'])
        key={
            'PK': {'S': SCOREBOARD_PK},
            'SK': {'S': sk}
        }
        response = client.get_item(TableName=table, Key=key)
        if 'Item' not in response:

            new_acheivment = {
                'PK': {'S': SCOREBOARD_PK},
                'SK': {'S': sk},
                'Username': {'S': event['Player']},
                'Stage': {'N': str(event['Stage'])},
                'Time': {'N': str(int(time.time()))}
            }
            print("Upserting {} with new scoreboard acheivment {}".format(event['Player'], event['Stage']))
            client.put_item(TableName=table, Item=new_acheivment)

            # Figure out if this is the first time anyone has acheived
            # That acheivment
            stage = int(event['Stage'])
            if stage >= MINIMUM_STAGE_TO_ANNOUNCE:
                key={
                    'PK': {'S': SCOREBOARD_TOP_PK},
                    'SK': {'S': SCOREBOARD_TOP_SK}
                }
                response = client.get_item(TableName=table, Key=key)
                if 'Item' not in response or \
                    int(response['Item']['Stage']) < stage:

                 # Create a new entry for the top of the scoreboard
                 message = MESSAGES[stage-MINIMUM_STAGE_TO_ANNOUNCE]
                 new_top = {
                    'PK': {'S': SCOREBOARD_TOP_PK},
                    'SK': {'S': SCOREBOARD_TOP_SK},
                    'Username': {'S': event['Player']},
                    'Stage': {'N': str(stage)},
                    'Message': {'S': message.format(event['Player'])}
                 }
                print("Upserting {} as new scoreboard leader".format(event['Player']))
                client.put_item(TableName=table, Item=new_top)

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
