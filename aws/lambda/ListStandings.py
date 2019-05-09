import boto3
import decimal
import json
from boto3.dynamodb.conditions import Key, Attr

'''
List all standings
Copy this file into ListHiScores lambda
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
    STANDINGS_PK='Standing'
    SCOREBOARD_PK='EggAchievment'
    SCOREBOARD_TOP_PK='ScoreboardStatus'

    dynamodb = boto3.resource('dynamodb', region_name='us-east-2')
    
    table = dynamodb.Table('global')
    
    response1 = table.query(
        KeyConditionExpression=Key('PK').eq(STANDINGS_PK)
    )

    response2 = table.query(
        KeyConditionExpression=Key('PK').eq(SCOREBOARD_PK)
    )

    raceMessage = ''
    response3 = table.query(
        KeyConditionExpression=Key('PK').eq(SCOREBOARD_TOP_PK)
    )
    if 'Items' in response3 and len(response3['Items']) > 0:
        raceMessage = response3['Items'][0]['Message'] + '.'

    returnObj = {
        'Status': True,
        'Standings': response1['Items'],
        'RaceToEgg': response2['Items'],
        'RaceStatus': raceMessage
    }

    return {
        'statusCode': 200,
        'body': json.dumps(returnObj, cls=DecimalEncoder)
    }
