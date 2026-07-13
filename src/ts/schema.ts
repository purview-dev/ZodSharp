import { z } from "zod";

/**
 * Shared User schema — mirrors the C# TestUser / TestUserSchema.
 * Used by both the TS tests and the cross-platform fixture generation.
 */
export const UserSchema = z.object({
  name: z.string().min(1),
  age: z.number().int().min(0).max(120),
  email: z.string().email().optional(),
  tags: z.array(z.string()).default([]),
});

export type User = z.infer<typeof UserSchema>;

/**
 * A set of fixture objects that exercise valid, invalid, and edge cases.
 * These are serialized to JSON files that the C# tests consume, and vice-versa.
 */
export const fixtures = {
  valid: {
    name: "John Doe",
    age: 30,
    email: "john@example.com",
    tags: ["admin", "user"],
  },
  validMinimal: {
    name: "A",
    age: 0,
    tags: [],
  },
  validMaxAge: {
    name: "Elder",
    age: 120,
    tags: ["senior"],
  },
  invalidEmptyName: {
    name: "",
    age: 30,
    tags: [],
  },
  invalidNegativeAge: {
    name: "Bad",
    age: -1,
    tags: [],
  },
  invalidOverMaxAge: {
    name: "Too Old",
    age: 121,
    tags: [],
  },
  invalidBadEmail: {
    name: "Jane",
    age: 25,
    email: "not-an-email",
    tags: [],
  },
  invalidMissingName: {
    age: 25,
    tags: [],
  },
} as const;
