FROM node:24.18.0-alpine AS build
WORKDIR /workspace
RUN corepack enable && corepack prepare pnpm@10.14.0 --activate
COPY package.json pnpm-lock.yaml pnpm-workspace.yaml tsconfig.base.json eslint.config.mjs .prettierrc.json ./
COPY apps/server/package.json apps/server/package.json
COPY packages/contracts/package.json packages/contracts/package.json
COPY packages/domain/package.json packages/domain/package.json
RUN pnpm install --frozen-lockfile
COPY apps/server apps/server
COPY packages packages
COPY scripts/copy-server-assets.mjs scripts/copy-server-assets.mjs
RUN pnpm --filter @kingdom/contracts build && pnpm --filter @kingdom/domain build && pnpm --filter @kingdom/server build

FROM node:24.18.0-alpine AS runtime
ENV NODE_ENV=production
WORKDIR /workspace
RUN addgroup -S kingdom && adduser -S kingdom -G kingdom
COPY --from=build --chown=kingdom:kingdom /workspace /workspace
USER kingdom
EXPOSE 3000 3001
CMD ["node", "apps/server/dist/src/main-api.js"]

