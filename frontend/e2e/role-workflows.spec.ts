import {
  expect,
  test,
  type APIRequestContext,
  type Page,
} from "@playwright/test";

const API_BASE_URL =
  process.env.E2E_API_BASE_URL ?? "http://localhost:8080/api/v1";
const demoUsers = {
  admin: "admin@startupconnect.local",
  moderator: "moderator@startupconnect.local",
  investor: "investor@startupconnect.local",
  business: "business@startupconnect.local",
  verified: "verified@startupconnect.local",
} as const;
type DemoRole = keyof typeof demoUsers;
const tokenCache = new Map<DemoRole, { accessToken: string; refreshToken: string }>();

async function loginByApi(
  page: Page,
  request: APIRequestContext,
  role: DemoRole,
) {
  let tokens = tokenCache.get(role);
  if (!tokens) {
    const response = await request.post(`${API_BASE_URL}/auth/login`, {
      data: { email: demoUsers[role], password: "Startup123!" },
    });
    expect(response.ok(), await response.text()).toBeTruthy();
    const body = (await response.json()) as { data: { accessToken: string; refreshToken: string } };
    tokens = body.data;
    tokenCache.set(role, tokens);
  }

  await page.addInitScript(
    ({ accessToken, refreshToken }) => {
      localStorage.setItem("startupconnect_access_token", accessToken);
      localStorage.setItem("startupconnect_refresh_token", refreshToken);
    },
    tokens,
  );
}

const roleRoutes: Array<{
  role: DemoRole;
  route: string;
  heading: string | RegExp;
  visibleNav?: string;
  hiddenNav?: string;
}> = [
  { role: "verified", route: "/dashboard", heading: /Verified User|StartupConnect|Dashboard unavailable/, visibleNav: "Applications", hiddenNav: "Create project" },
  { role: "business", route: "/projects/me/owned", heading: "My Projects", visibleNav: "Received applications", hiddenNav: "Investor profile" },
  { role: "investor", route: "/investor", heading: "Investor Dashboard", visibleNav: "My interests", hiddenNav: "Pending projects" },
  { role: "moderator", route: "/moderator", heading: "Moderator Dashboard", visibleNav: "Pending projects", hiddenNav: "Create project" },
  { role: "admin", route: "/admin", heading: "Admin Dashboard", visibleNav: "Users", hiddenNav: "Discover projects" },
  { role: "admin", route: "/admin/users", heading: "Admin Users" },
  { role: "admin", route: "/admin/email-outbox", heading: "Email Outbox" },
  { role: "admin", route: "/admin/subscriptions", heading: "Plans & Quotas" },
];

for (const scenario of roleRoutes) {
  test(`${scenario.role} can open ${scenario.route}`, async ({ page, request }) => {
    await loginByApi(page, request, scenario.role);
    await page.goto(scenario.route);

    await expect(page).toHaveURL(new RegExp(`${scenario.route.replaceAll("/", "\\/")}$`));
    await expect(page.getByRole("heading", { name: scenario.heading }).first()).toBeVisible();
    await expect(page.getByText(/Checking your session/i)).toBeHidden();
    if (!test.info().project.name.includes("mobile") && scenario.visibleNav && scenario.hiddenNav) {
      await expect(page.getByRole("link", { name: scenario.visibleNav, exact: true }).first()).toBeVisible();
      await expect(page.getByRole("link", { name: scenario.hiddenNav, exact: true })).toHaveCount(0);
    }
  });
}

test("verified user cannot open the admin area", async ({ page, request }) => {
  await loginByApi(page, request, "verified");
  await page.goto("/admin");

  await expect(page.getByText(/does not have permission/i)).toBeVisible();
});

test("mobile app shell opens navigation and switches theme", async ({ page, request }, testInfo) => {
  test.skip(!testInfo.project.name.includes("mobile"), "Mobile-only shell check");
  await loginByApi(page, request, "verified");
  await page.goto("/dashboard");

  await page.getByRole("button", { name: "Open navigation" }).click();
  await expect(page.getByRole("link", { name: "Dashboard" }).last()).toBeVisible();
  await page.getByRole("button", { name: "Close navigation" }).last().click();

  const themeButton = page.getByRole("button", { name: /Use (dark|light) theme/ });
  const wasDark = await page.locator("html").evaluate((element) => element.classList.contains("dark"));
  await themeButton.click();
  await expect.poll(() => page.locator("html").evaluate((element) => element.classList.contains("dark"))).toBe(!wasDark);
});
