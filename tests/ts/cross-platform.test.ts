/**
 * Cross-platform tests: TS/Zod side.
 *
 * These tests verify that:
 * 1. Zod validates the shared fixtures correctly (produces a manifest).
 * 2. JSON produced by Purview.ZodSharp (C#) can be parsed by Zod and vice-versa.
 * 3. The validation outcomes match between Zod and Purview.ZodSharp for the same data.
 */
import { describe, it, expect } from "vitest";
import { readFileSync, readdirSync } from "node:fs";
import { resolve, basename } from "node:path";
import { UserSchema, fixtures } from "../../src/ts/schema";

const fixturesDir = resolve("src", "ts", "fixtures");
const csharpOutputDir = resolve("src", "tests", "cross-platform", "output");

describe("Zod validates shared fixtures", () => {
  for (const [key, value] of Object.entries(fixtures)) {
    it(`${key} — validation outcome matches expectations`, () => {
      const result = UserSchema.safeParse(value);
      const invalidKeys = [
        "invalidEmptyName",
        "invalidNegativeAge",
        "invalidOverMaxAge",
        "invalidBadEmail",
        "invalidMissingName",
      ];
      const shouldBeValid = !invalidKeys.includes(key);

      expect(result.success).toBe(shouldBeValid);
    });
  }
});

describe("Zod JSON serialization is canonical", () => {
  it("serializes valid user to expected JSON shape", () => {
    const json = JSON.stringify(fixtures.valid);
    const parsed = JSON.parse(json);

    expect(parsed.name).toBe("John Doe");
    expect(parsed.age).toBe(30);
    expect(parsed.email).toBe("john@example.com");
    expect(parsed.tags).toEqual(["admin", "user"]);
  });

  it("round-trips: serialize -> parse -> validate", () => {
    const json = JSON.stringify(fixtures.valid);
    const parsed = JSON.parse(json);
    const result = UserSchema.safeParse(parsed);

    expect(result.success).toBe(true);
    if (result.success) {
      expect(result.data.name).toBe("John Doe");
      expect(result.data.age).toBe(30);
    }
  });
});

describe("Cross-platform: C# output parseable by Zod", () => {
  // The C# cross-platform tests write each run into its own GUID-named subdirectory under
  // `output/`, so this must recurse. A flat readdir only ever sees those directory names,
  // none of which end in ".json" — which would silently leave this suite with nothing to check.
  let csharpFiles: string[] = [];
  try {
    csharpFiles = readdirSync(csharpOutputDir, { recursive: true })
      .map((entry) => entry.toString())
      .filter((f) => f.endsWith(".json"));
  } catch {
    // Output dir does not exist — the C# cross-platform tests have not run. Handled below.
  }

  if (csharpFiles.length === 0) {
    // Do NOT pass here. An empty output directory means this suite verified nothing, and the
    // cross-platform parity guarantee would be silently unproven. Fail with the fix instead.
    it("has C# output to validate", () => {
      expect.fail(
        `No C# output files found in ${csharpOutputDir}. ` +
          "Run the C# cross-platform tests first (`just test`) so they write their output, " +
          "then re-run `bun run test`.",
      );
    });
  }

  // Output accumulates one GUID directory per run, so group by fixture name: the suite then has a
  // stable set of test names and a stable count however many runs are present on disk.
  const byFixtureName = new Map<string, string[]>();
  for (const file of csharpFiles) {
    const name = basename(file);
    byFixtureName.set(name, [...(byFixtureName.get(name) ?? []), file]);
  }

  for (const [name, files] of [...byFixtureName].sort(([a], [b]) => a.localeCompare(b))) {
    it(`C# fixture ${name} is valid JSON parseable by Zod`, () => {
      for (const file of files) {
        const content = readFileSync(resolve(csharpOutputDir, file), "utf-8");
        const parsed = JSON.parse(content);
        const result = UserSchema.safeParse(parsed);

        // All C# output should be valid (C# only writes validated data)
        expect(result.success, `${file} failed Zod validation`).toBe(true);
      }
    });
  }
});

describe("Cross-platform: fixture manifest consistency", () => {
  it("manifest.json exists and matches Zod validation outcomes", () => {
    const manifestPath = resolve(fixturesDir, "manifest.json");
    let content: string;

    try {
      content = readFileSync(manifestPath, "utf-8");
    } catch {
      // Manifest not generated yet — generate inline
      const manifest = Object.fromEntries(
        Object.entries(fixtures).map(([key, value]) => {
          const result = UserSchema.safeParse(value);
          return [key, { valid: result.success }];
        }),
      );
      content = JSON.stringify(manifest);
    }

    const manifest = JSON.parse(content);
    const invalidKeys = [
      "invalidEmptyName",
      "invalidNegativeAge",
      "invalidOverMaxAge",
      "invalidBadEmail",
      "invalidMissingName",
    ];

    for (const [key, value] of Object.entries(fixtures)) {
      const zodResult = UserSchema.safeParse(value);
      const manifestResult = manifest[key]?.valid;

      expect(manifestResult).toBe(zodResult.success);
      expect(manifestResult).toBe(!invalidKeys.includes(key));
    }
  });
});
