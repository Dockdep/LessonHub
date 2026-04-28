# Stage 1: install + build (browser + SSR bundles)
FROM node:20-alpine AS build
WORKDIR /app

# Install deps from lockfile to maximise cache reuse
COPY package.json package-lock.json ./
# `ng add @angular/ssr` ships a peer-range that npm 11 considers strict; legacy
# resolver matches what `npm install` does on the host.
RUN npm ci --legacy-peer-deps

# Build artefacts: dist/lessonshub-ui/{browser,server}
COPY . .
RUN npm run build

# Stage 2: runtime — Node 20 alpine running the SSR server
FROM node:20-alpine AS runtime
WORKDIR /app
ENV NODE_ENV=production
ENV PORT=4000

# Only the build output + production deps. Skipping the source tree keeps the
# image small and avoids leaking dev-time files.
COPY --from=build /app/package.json /app/package-lock.json ./
RUN npm ci --omit=dev --legacy-peer-deps && npm cache clean --force

COPY --from=build /app/dist ./dist

EXPOSE 4000

# server.mjs reads PORT env var; default 4000.
CMD ["node", "dist/lessonshub-ui/server/server.mjs"]
