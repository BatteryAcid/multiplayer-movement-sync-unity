"""An AWS Python Pulumi program"""

import json
import re
import pulumi
import pulumi_aws as aws

lambdaFunctionKeys = [
    "game-messaging",
    "join-game",
    "disconnect-game",
]

websocketApiRouteKeys = [
    "OnMessage",
    "$connect",
    "$disconnect",
    "$default",
]

websocketApiIntegrationKeys = [
    "OnMessageIntegration",
    "JoinGameIntegration",
    "DisconnectGameIntegration",
    "DefaultIntegration",
]

websocketApiRoutes = []
lambdaFunctionCode = []
lambdaFunctions = []
lambdaPermissions = []
apiRoutes = []
websocketApiIntegrations = []

namingSuffix = "-2022-pulumi"

tags = {
    "Environment": pulumi.get_stack(),
    "ProjectName": pulumi.get_project(),
    "StackName": pulumi.get_stack(),
    "Team": "WeiWu",
}

# DynamoDB item table
dbTable = aws.dynamodb.Table(
    "game-session" + namingSuffix,    
    name="game-session" + namingSuffix,
    attributes=[aws.dynamodb.TableAttributeArgs(name="uuid", type="S")],
    hash_key="uuid",
    read_capacity=1,
    write_capacity=1,
    tags=tags,
)

dbTableIAmPolicy = aws.iam.Policy(
    "iam-policy" + namingSuffix,
    name="iam-policy" + namingSuffix,
    description="This policy grants permission to interact with DynamoDB",
    policy=dbTable.arn.apply(
        lambda arn: json.dumps(
            {
                "Version": "2012-10-17",
                "Statement": [
                    {
                        "Effect": "Allow",
                        "Action": [
                            "dynamodb:DeleteItem",
                            "dynamodb:GetItem",
                            "dynamodb:PutItem",
                            "dynamodb:Scan",
                            "dynamodb:UpdateItem",
                        ],
                        "Resource": arn,
                    }
                ],
            }
        )
    ),
    tags=tags,
)

# Create Lambda Function role
dbTableIAmRole = aws.iam.Role(
    "game-server-role" + namingSuffix,
    name="game-server-role" + namingSuffix,
    assume_role_policy=json.dumps(
        {
            "Version": "2012-10-17",
            "Statement": [
                {
                    "Action": "sts:AssumeRole",
                    "Effect": "Allow",
                    "Sid": "",
                    "Principal": {
                        "Service": "lambda.amazonaws.com",
                    },
                },
            ],
        }
    ),
    managed_policy_arns=[
        "arn:aws:iam::aws:policy/service-role/AWSLambdaBasicExecutionRole",
        "arn:aws:iam::aws:policy/AmazonAPIGatewayInvokeFullAccess",
        "arn:aws:iam::aws:policy/AmazonDynamoDBFullAccess",
        "arn:aws:iam::aws:policy/CloudWatchLogsFullAccess",
        "arn:aws:iam::aws:policy/AmazonAPIGatewayAdministrator",
        dbTableIAmPolicy.arn,
    ],
    tags=tags,
)

# Get Lambda Code
for i in range(len(lambdaFunctionKeys)):
    lambdaFunctionCode.append(
        pulumi.asset.AssetArchive(
        {
            ".": pulumi.asset.FileArchive("./src/lambda/" + lambdaFunctionKeys[i]),
        }
        )
    ) 

# Creating API Gateway WebSocket API
gameWebsocketApi = aws.apigatewayv2.Api(
    "game-websocket-api" + namingSuffix,
    name="game-websocket-api" + namingSuffix,
    route_selection_expression="$request.body.action",
    protocol_type="WEBSOCKET",
    description="API Gateway Websocket API",
    tags=tags,
)

# Create Lambda Function
for i in range(len(lambdaFunctionKeys)):
    lambdaFunctions.append(
        aws.lambda_.Function(
        lambdaFunctionKeys[i] + namingSuffix,
        name=lambdaFunctionKeys[i] + namingSuffix,
        role=dbTableIAmRole.arn,
        handler="index.handler",
        runtime=aws.lambda_.Runtime.NODE_JS12D_X,
        code=lambdaFunctionCode[i],
        tags=tags,
        environment=aws.lambda_.FunctionEnvironmentArgs(
            variables={"DYNAMODB_TABLE": dbTable.name}
        ),
        )
    ) 
    lambdaPermissions.append(
        aws.lambda_.Permission(
        lambdaFunctionKeys[i] + "Permissions" + namingSuffix,
        action="lambda:InvokeFunction",
        principal="apigateway.amazonaws.com",
        function=lambdaFunctions[i].arn,
        source_arn=pulumi.Output.concat(gameWebsocketApi.execution_arn, "/*/*"),
        opts=pulumi.ResourceOptions(depends_on=[gameWebsocketApi, lambdaFunctions[i]]),
        )
    )

for i in range(len(websocketApiIntegrationKeys)):
    websocketApiIntegrations.append(
        aws.apigatewayv2.Integration(
        websocketApiIntegrationKeys[i] + namingSuffix,
        api_id=gameWebsocketApi.id,
        integration_type="AWS_PROXY",
        connection_type="INTERNET",
        content_handling_strategy="CONVERT_TO_TEXT",
        description="Integration for game-messaging lambda function",
        integration_method="POST",
        integration_uri=lambdaFunctions[i%len(lambdaFunctions)].invoke_arn,
        passthrough_behavior="WHEN_NO_MATCH"
        )
    ) 

# Creating routes
for i in range(len(websocketApiRouteKeys)):
    websocketApiRoutes.append(
        aws.apigatewayv2.Route(
            websocketApiRouteKeys[i] + namingSuffix,
            api_id=gameWebsocketApi.id,
            route_key=websocketApiRouteKeys[i],
            target=pulumi.Output.concat(
                "integrations/", websocketApiIntegrations[i].id
            ),
        )
    )


# Creating default stage
gameWebsocketApiStage = aws.apigatewayv2.Stage(
    "gameWebsocketApiStage" + namingSuffix,
    name="gameWebsocketApiStage" + namingSuffix,
    api_id=gameWebsocketApi.id,
    auto_deploy=True,
    tags=tags,
    opts=pulumi.ResourceOptions(depends_on=websocketApiRoutes),
)

# Outputs
for i in range(len(lambdaFunctionKeys)):
    pulumi.export(name=lambdaFunctionKeys[i], value=lambdaFunctions[i].name)
    
pulumi.export("gameWebsocketApiStage" + namingSuffix, value=gameWebsocketApiStage.invoke_url)

f = open("../Unity/Assets/Resources/aws-api.txt", "w")
f.write(gameWebsocketApiStage.invoke_url)
f.close()