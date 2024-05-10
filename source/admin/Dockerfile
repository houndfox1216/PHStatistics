FROM node:16.18.1
ENV TZ=Asia/Taipei
RUN ln -snf /usr/share/zoneinfo/$TZ /etc/localtime && echo $TZ > /etc/timezone
WORKDIR /app
COPY ./admin/package*.json ./
RUN npm config set registry http://192.168.11.7:4873
RUN npm install -g http-server
RUN npm ci --cache .npm --prefer-offline --slient
COPY ./admin .
RUN npm run build
EXPOSE 8080
CMD [ "http-server", "dist" ]
