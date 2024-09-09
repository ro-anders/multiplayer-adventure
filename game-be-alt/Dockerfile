#Build stage
FROM node:16-alpine AS build

WORKDIR /app

COPY package.json package-lock.json .

RUN npm install

COPY . .

RUN npm run build

#Production stage
FROM node:16-alpine AS production

WORKDIR /app

COPY package.json package-lock.json .

RUN npm ci --only=production

COPY --from=build /app/dist ./dist

EXPOSE 3000

CMD ["node", "dist/index.js"]