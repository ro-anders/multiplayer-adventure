import boto3
import decimal
import json
from boto3.dynamodb.conditions import Key, Attr

'''
Return two things all games need to check when they first run
 - whether the client needs to be updated
 - whether any important status message needs to be displayed (like anticipated downtime)

Copy this file into GameStatus lambda
'''

class DecimalEncoder(json.JSONEncoder):
    def default(self, o):
        if isinstance(o, decimal.Decimal):
            if o % 1 > 0:
                return float(o)
            else:
                return int(o)
        return super(DecimalEncoder, self).default(o)

def lambda_handler(event, context):

    SCOREBOARD_TOP_PK='ScoreboardStatus'
    MINIMAL_VIABLE_VERSION = 5
    RACE_TO_THE_EGG_MESSAGE_ID = 100;
    statusMessage = ''
    messageId = 0
    eggStatus = False

    # If we have no system status message, then send
    # race to the egg status
    if statusMessage == '':
        dynamodb = boto3.resource('dynamodb', region_name='us-east-2')
        table = dynamodb.Table('global')
        response = table.query(
            KeyConditionExpression=Key('PK').eq(SCOREBOARD_TOP_PK)
        )
        print("Response from race status query = " + json.dumps(response, cls=DecimalEncoder))
        if 'Items' in response and len(response['Items']) > 0:
            statusMessage = response['Items'][0]['Message'] + \
              '.\nCheck the leader board for the race to the egg.'
            messageId = RACE_TO_THE_EGG_MESSAGE_ID
            eggStatus = response['Items'][0]['Stage']==5
    
    response = {
        'MinimumVersion': MINIMAL_VIABLE_VERSION,
        'SystemmMessage': statusMessage,
        'EggStatus': eggStatus,
        'MessageId': messageId
    }
    
    return {
        'statusCode': 200,
        'body': json.dumps(response, cls=DecimalEncoder)
    }
