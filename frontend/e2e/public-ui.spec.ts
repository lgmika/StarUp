import { expect, test } from "@playwright/test";

test("project discovery supports backend search and clear", async ({ page }) => {
  await page.goto("/projects");
  await expect(page.getByRole("heading", { name: "Discover projects" })).toBeVisible();
  await page.getByPlaceholder("Search by keyword").fill("query-that-will-not-match-any-project-92841");
  await page.getByRole("button", { name: "Search" }).click();
  await expect(page.getByText(/0 projects|No projects/i).first()).toBeVisible();
  await page.getByRole("button", { name: "Clear" }).click();
  await expect(page.getByPlaceholder("Search by keyword")).toHaveValue("");
});

test("authentication forms expose validation states", async ({ page }) => {
  await page.goto("/auth/register");
  await page.getByRole("button", { name: /create account/i }).click();
  await expect(page.locator("form").getByText(/required|at least|invalid/i).first()).toBeVisible();

  await page.goto("/auth/forgot-password");
  await expect(page.getByRole("heading", { name: "Reset password" })).toBeVisible();
});

test("project discovery falls back to guest when stored session is invalid", async ({ page }) => {
  await page.addInitScript(() => {
    localStorage.setItem("startupconnect_access_token", "expired-access-token");
    localStorage.setItem("startupconnect_refresh_token", "expired-refresh-token");
  });
  await page.goto("/projects");
  await expect(page).toHaveURL(/\/projects$/);
  await expect(page.getByRole("heading", { name: "Discover projects" })).toBeVisible();
  await expect(page.getByRole("link", { name: "Sign in" })).toBeVisible();
});
