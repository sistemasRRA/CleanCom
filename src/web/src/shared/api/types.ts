import type { paths } from './schema';

type Operation<P extends keyof paths, M extends string> = M extends keyof paths[P] ? paths[P][M] : never;

/** Query string of an endpoint, from the OpenAPI types. */
export type Query<P extends keyof paths, M extends string> = Operation<P, M> extends { parameters: { query?: infer Q } } ? NonNullable<Q> : never;

/** Body of an endpoint (JSON or multipart), from the OpenAPI types. */
export type Body<P extends keyof paths, M extends string> = Operation<P, M> extends { requestBody?: { content: infer C } } ? C[keyof C] : never;
