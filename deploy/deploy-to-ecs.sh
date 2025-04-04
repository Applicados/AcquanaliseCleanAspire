#!/bin/bash
set -e

# Load environment variables
if [ -f "./.env.ecs" ]; then
    source "./.env.ecs"
    export $(grep -v '^#' "./.env.ecs" | xargs)
elif [ -f "./.env" ]; then
    source "./.env"
    export $(grep -v '^#' "./.env" | xargs)
else
    echo "Error: No environment file found"
    exit 1
fi

# Check required dependencies and env vars
for cmd in aws docker jq; do
    command -v $cmd &> /dev/null || { echo "$cmd not installed"; exit 1; }
done

[ -z "$ECR_REPOSITORY_URI" ] || [ -z "$AWS_REGION" ] || [ -z "$AWS_LOGS_GROUP" ] && { 
    echo "Missing required environment variables"; exit 1; 
}

# Parse command-line arguments
BUILD_API=false
BUILD_WEB=false
SKIP_BUILD=false
SKIP_DEPLOY=false

for arg in "$@"; do
  case $arg in
    --build-api) BUILD_API=true ;;
    --build-web) BUILD_WEB=true ;;
    --skip-build) SKIP_BUILD=true ;;
    --skip-deploy) SKIP_DEPLOY=true ;;
    --help)
      echo "Usage: $0 [--build-api|--build-web|--skip-build|--skip-deploy]"
      exit 0 ;;
  esac
done

# Set defaults if no specific build flag
[ "$SKIP_BUILD" = false ] && [ "$BUILD_API" = false ] && [ "$BUILD_WEB" = false ] && {
  BUILD_API=true
  BUILD_WEB=true
}

if [ "$SKIP_BUILD" = false ]; then
  # Login to ECR
  aws ecr get-login-password --region $AWS_REGION | docker login --username AWS --password-stdin $ECR_REPOSITORY_URI

  # Build and push images
  if [ "$BUILD_API" = true ]; then
    docker build -t $ECR_REPOSITORY_URI/cleanaspire-api:$IMAGE_TAG -f "./Dockerfile.api" ..
    docker push $ECR_REPOSITORY_URI/cleanaspire-api:$IMAGE_TAG
  fi

  if [ "$BUILD_WEB" = true ]; then
    docker build -t $ECR_REPOSITORY_URI/cleanaspire-webapp:$IMAGE_TAG -f "./Dockerfile.web" ..
    docker push $ECR_REPOSITORY_URI/cleanaspire-webapp:$IMAGE_TAG
  fi
fi

[ "$SKIP_DEPLOY" = true ] && exit 0

# Ensure log group exists
aws logs create-log-group --log-group-name "$AWS_LOGS_GROUP" --region "$AWS_REGION" || true

# Get AWS account ID
AWS_ACCOUNT_ID=$(aws sts get-caller-identity --query "Account" --output text)

# Create task definition from template
cp ./ecs-task-definition.json ./ecs-task-definition-processed.json
sed -i "s|\${AWS_ACCOUNT_ID}|${AWS_ACCOUNT_ID}|g; s|\${ECR_REPOSITORY_URI}|${ECR_REPOSITORY_URI}|g; s|\${IMAGE_TAG}|${IMAGE_TAG}|g; s|\${AWS_LOGS_GROUP}|${AWS_LOGS_GROUP}|g; s|\${AWS_REGION}|${AWS_REGION}|g" ./ecs-task-definition-processed.json

# Register the task definition
TASK_DEFINITION=$(aws ecs register-task-definition --cli-input-json file://./ecs-task-definition-processed.json --region $AWS_REGION)
TASK_DEFINITION_ARN=$(echo $TASK_DEFINITION | jq -r '.taskDefinition.taskDefinitionArn')

# Set cluster and network config
CLUSTER_NAME="cleanaspire"
SUBNET_IDS="subnet-06a7f09abc607b36b,subnet-05a8aee3d7969ad85,subnet-0644b02ceeda2654f,subnet-0f7856536cce2702e,subnet-0aa6b9cebea9f7231"
SECURITY_GROUPS="sg-017ebd2fe7f73fc40"

# Create ECS cluster if needed
aws ecs describe-clusters --clusters $CLUSTER_NAME --region $AWS_REGION | jq -r '.clusters | length' | grep -q "0" && {
    aws ecs create-cluster --cluster-name $CLUSTER_NAME --region $AWS_REGION
}

# Check if service exists
SERVICE_EXISTS=$(aws ecs list-services --cluster $CLUSTER_NAME --region $AWS_REGION 2>/dev/null | jq -r '.serviceArns | length')

if [ "$SERVICE_EXISTS" = "0" ]; then
    # Create service
    aws ecs create-service \
        --cluster $CLUSTER_NAME \
        --service-name cleanaspire-service \
        --task-definition $TASK_DEFINITION_ARN \
        --desired-count 1 \
        --launch-type FARGATE \
        --network-configuration "awsvpcConfiguration={subnets=[$SUBNET_IDS],securityGroups=[$SECURITY_GROUPS],assignPublicIp=ENABLED}" \
        --region $AWS_REGION
else
    # Update service
    aws ecs update-service \
        --cluster $CLUSTER_NAME \
        --service cleanaspire-service \
        --task-definition $TASK_DEFINITION_ARN \
        --network-configuration "awsvpcConfiguration={subnets=[$SUBNET_IDS],securityGroups=[$SECURITY_GROUPS],assignPublicIp=ENABLED}" \
        --region $AWS_REGION
fi

echo "Deployment completed successfully!"