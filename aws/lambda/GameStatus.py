import boto3
import decimal
import json

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

    MINIMAL_VIABLE_VERSION = 2
    
    STATUS_MESSAGE = ""
    
    response = {
        'MinimumVersion': MINIMAL_VIABLE_VERSION,
        'SystemMessage': STATUS_MESSAGE
    }
    
    return {
        'statusCode': 200,
        'body': json.dumps(response, cls=DecimalEncoder)
    }
