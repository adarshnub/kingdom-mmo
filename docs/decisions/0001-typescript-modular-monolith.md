# ADR-0001: TypeScript modular monolith

Status: Accepted

Use Node.js 24, TypeScript, NestJS, and Fastify. Keep gameplay areas as explicit modules in one codebase, deployed as API, realtime, and worker processes. Extract a module into a service only after profiling shows an independent scaling or ownership need.

This keeps transactions and delivery simple for a small team while preserving boundaries needed for future decomposition.
