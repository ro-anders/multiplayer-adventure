#Build stage
FROM --platform=linux/amd64 node:20-alpine AS build

WORKDIR /app

COPY package.json package-lock.json .

RUN npm install

COPY . .

RUN npm run build

#Production stage
FROM node:20-alpine AS production

WORKDIR /app

COPY package.json package-lock.json .

RUN npm ci --only=production

COPY --from=build /app/dist ./dist

EXPOSE 80

CMD ["node", "dist/index.js"]