import { expect, test } from "@playwright/test";

const API_BASE_URL =
  process.env.E2E_API_BASE_URL ?? "http://localhost:8080/api/v1";

test.describe("backend readiness", () => {
  for (const endpoint of ["health", "health/live", "health/ready"]) {
    test(`${endpoint} is healthy`, async ({ request }) => {
      const response = await request.get(`${API_BASE_URL}/${endpoint}`);
      expect(response.ok(), await response.text()).toBeTruthy();
    });
  }
});
