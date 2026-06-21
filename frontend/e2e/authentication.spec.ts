import { expect, test } from "@playwright/test";

const verifiedUser = "verified@startupconnect.local";

test("unauthenticated users are redirected to login", async ({ page }) => {
  await page.goto("/dashboard");
  await expect(page).toHaveURL(/\/auth\/login\?next=%2Fdashboard/);
  await expect(page.getByRole("heading", { name: "Sign in" })).toBeVisible();
});

test("user can sign in through the UI", async ({ page }) => {
  await page.goto("/auth/login?next=/dashboard");
  await page.getByLabel("Email").fill(verifiedUser);
  await page.getByLabel("Password").fill("Startup123!");
  await page.getByRole("button", { name: "Sign in" }).click();

  await expect(page).toHaveURL((url) => url.pathname === "/dashboard");
  await expect(page.getByRole("heading", { name: "Verified User" })).toBeVisible();

  await page.goto("/projects");
  await expect(page.getByRole("link", { name: "Verified User" })).toBeVisible();
  await expect(page.getByRole("link", { name: "Sign in" })).toHaveCount(0);
});

test("invalid credentials show an error", async ({ page }) => {
  await page.goto("/auth/login");
  await page.getByLabel("Email").fill("invalid@example.com");
  await page.getByLabel("Password").fill("WrongPassword123!");
  await page.getByRole("button", { name: "Sign in" }).click();

  await expect(page).toHaveURL(/\/auth\/login/);
  await expect(page.getByText(/invalid email or password/i)).toBeVisible();
});

test("temporary session errors do not sign the user out", async ({ page }) => {
  await page.addInitScript(() => {
    localStorage.setItem("startupconnect_access_token", "valid-session-token");
    localStorage.setItem("startupconnect_refresh_token", "valid-refresh-token");
  });
  await page.route("**/api/v1/auth/me", (route) =>
    route.fulfill({
      status: 429,
      contentType: "application/json",
      body: JSON.stringify({ message: "Too many requests" }),
    })
  );

  await page.goto("/dashboard");

  await expect(page).toHaveURL((url) => url.pathname === "/dashboard");
  await expect(page.getByRole("heading", { name: "Session check unavailable" })).toBeVisible();
  await expect(page.getByRole("button", { name: "Try again" })).toBeVisible();
  expect(await page.evaluate(() => localStorage.getItem("startupconnect_access_token"))).toBe("valid-session-token");
});

test("user can switch accounts without stale session data", async ({ page }) => {
  test.setTimeout(90_000);
  const accounts = [
    { email: verifiedUser, route: "/dashboard", role: "VerifiedUser" },
    { email: "admin@startupconnect.local", route: "/admin", role: "Admin" },
  ];

  for (const account of accounts) {
    await page.goto("/auth/login");
    await page.getByLabel("Email").fill(account.email);
    await page.getByLabel("Password").fill("Startup123!");
    await page.getByRole("button", { name: "Sign in" }).click();
    await expect(page).toHaveURL(new RegExp(`${account.route.replaceAll("/", "\\/")}$`));
    await expect(page.getByRole("button", { name: account.role, exact: true })).toBeVisible();
  }
});
