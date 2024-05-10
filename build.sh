#!/bin/bash
# Arguments
# REGISTRY_USER: cloudfun01
# $2: 3 # Array Length, 用來方便處理陣列值
# DEPLOY_TYPES=("services" "payment" "storage")
# DOCKER_PATHS=("services/Admin/Dockerfile" "services/Payment/Dockerfile" "services/Storage/Dockerfile")
# PROJECT_NAME="empty-project"
# VERSION="c31def2"
REGISTRY_USER=$1
DEPLOY_TYPES=("${@:3:$2}")
DOCKER_PATHS=("${@:(3+$2):$2}")
PROJECT_NAME="${@:(3+$2+$2):1}"
VERSION="${@:(3+$2+$2+1):1}"

BUILD_IMAGE_NAMES=()
for ((i=0; i<${#DEPLOY_TYPES[@]}; i++))
do
  fullName="${REGISTRY_USER}/${PROJECT_NAME}-${DEPLOY_TYPES[i]}:${VERSION}"

  BUILD_IMAGE_NAMES+=($fullName)
  docker build -t $fullName -f ${DOCKER_PATHS[i]} .
  if [ $? -ne 0 ]; then
    echo "Failed to build image, $fullName"
    exit 1
  fi
done

for image in ${BUILD_IMAGE_NAMES[@]}
do
  docker push $image
done

echo "BUILD_IMAGE_NAMES=$(IFS=' '; echo "${BUILD_IMAGE_NAMES[*]}")" > build.env