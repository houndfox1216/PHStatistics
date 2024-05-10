!/bin/bash
# Arguments
# IMAGE_FULL_NAME="cloudfun01/empty-project-services:c12bdo"
# CONTAINER_PORT=5000
IMAGE_FULL_NAME=$1
CONTAINER_PORT=$2

docker login

dockerResp=$(docker pull $IMAGE_FULL_NAME)
if [[ $dockerResp = *"up to date"* ]]
then
  echo "Image is up to date."
  exit 0
fi

nameArray=()
IFS=':' read -ra nameArray <<< "${IMAGE_FULL_NAME#*/}"
imageAlias=${nameArray[0]}
projectName=${imageAlias%-*}
deployType=${imageAlias##*-}
containerData=($(docker inspect --format='{{.Id}} {{.Image}}' $(docker ps -aq -f name=$imageAlias)))
port=$(docker ps -a -f name=$imageAlias --format '{{.Ports}}' | awk -F "->" '{print $1}' | cut -d ":" -f2)
CONTAINER_PORT=${CONTAINER_PORT:-$port}

if [ -z $CONTAINER_PORT ]
then
  # Find port from 5000 ~ 7000
  startPort=5000
  endPort=7000
  increment=1
  dockerPorts=$(docker ps --format "{{.Ports}}" | awk -F "->" '{print $1}' | cut -d ":" -f2)
  while [ -z $CONTAINER_PORT ] && [[ $startPort -le $endPort ]]; do
    if echo "$dockerPorts" | grep -q "\<$CONTAINER_PORT\>"; then continue; fi
    CONTAINER_PORT=$(nc -zv localhost $startPort 2>&1 | grep -m 1 "refused" | grep -oP "\d+")
    ((startPort+=increment))
  done
fi
if [ -z "$CONTAINER_PORT" ]
then
  echo "No port for deploy."
  exit 1
fi

fileVolumeName="${projectName}-files-volume"
resourceVolumeName="${projectName}-resources-volume"
dockerScript=""
if [ $deployType = 'admin' ]
then
  dockerScript="docker run -d --name ${imageAlias} -p ${CONTAINER_PORT}:8080 --restart=always"
else
  dockerScript="docker run -d --name ${imageAlias} -p ${CONTAINER_PORT}:80 --restart=always -v ${fileVolumeName}:/app/wwwroot/files -v ${resourceVolumeName}:/app/wwwroot/resources -v /var/log/nlog/${imageAlias}/${deployType}:/app/Logs ${IMAGE_FULL_NAME}"
fi

docker volume create ${fileVolumeName}
docker volume create ${resourceVolumeName}
if [ -z "${containerData[0]}" ]
then
  $dockerScript
else
  docker rm -f ${containerData[0]}
  $dockerScript
  docker rmi -f ${containerData[1]##*:}
fi