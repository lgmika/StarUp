import { expect, test, type APIRequestContext, type Page } from "@playwright/test";

const API_BASE_URL = process.env.E2E_API_BASE_URL ?? "http://localhost:8080/api/v1";

async function login(request: APIRequestContext, email: string) {
  const response = await request.post(`${API_BASE_URL}/auth/login`, { data: { email, password: "Startup123!" } });
  expect(response.ok(), await response.text()).toBeTruthy();
  const body = (await response.json()) as { data: { accessToken: string; refreshToken: string } };
  return { tokens: body.data, headers: { Authorization: `Bearer ${body.data.accessToken}` } };
}

async function authenticatePage(page: Page, tokens: { accessToken: string; refreshToken: string }) {
  await page.addInitScript((auth) => {
    localStorage.setItem("startupconnect_access_token", auth.accessToken);
    localStorage.setItem("startupconnect_refresh_token", auth.refreshToken);
  }, tokens);
}

async function createPublishedProject(request: APIRequestContext, visibility: "Public" | "NdaRequired") {
  const business = await login(request, "business@startupconnect.local");
  const moderator = await login(request, "moderator@startupconnect.local");
  const suffix = `${Date.now()}-${Math.random().toString(16).slice(2)}`;
  const title = `E2E ${visibility} ${suffix}`;
  const draftResponse = await request.post(`${API_BASE_URL}/projects/drafts`, {
    headers: business.headers,
    data: { title, summary: "Context-driven frontend compliance flow.", problem: "Protected workflows need UI coverage.", solution: "Exercise the real API through Playwright.", stage: "MVP", visibility },
  });
  expect(draftResponse.ok(), await draftResponse.text()).toBeTruthy();
  const draft = (await draftResponse.json()) as { data: { id: string } };
  const submit = await request.post(`${API_BASE_URL}/projects/${draft.data.id}/submit-review`, { headers: business.headers });
  expect(submit.ok(), await submit.text()).toBeTruthy();
  const approve = await request.post(`${API_BASE_URL}/moderator/projects/${draft.data.id}/approve`, { headers: moderator.headers, data: { reason: "Approved for frontend E2E coverage." } });
  expect(approve.ok(), await approve.text()).toBeTruthy();
  return { id: draft.data.id, title };
}

test("verified user accepts a project NDA through protected detail", async ({ page, request }) => {
  const project = await createPublishedProject(request, "NdaRequired");
  const verified = await login(request, "verified@startupconnect.local");
  await authenticatePage(page, verified.tokens);

  await page.goto(`/projects/${project.id}`);
  await expect(page.getByRole("heading", { name: "Protected project" })).toBeVisible();
  await expect(page.getByText(/^NDA version \d+$/)).toBeVisible();
  await page.getByRole("checkbox").check();
  await page.getByRole("button", { name: "Accept NDA" }).click();

  await expect(page.getByRole("heading", { name: "Protected project" })).toBeVisible();
  await expect(page.getByText(/NDA version .* accepted/i)).toBeVisible();
});

test("verified user reports a project from its current context", async ({ page, request }) => {
  const project = await createPublishedProject(request, "Public");
  const verified = await login(request, "verified@startupconnect.local");
  await authenticatePage(page, verified.tokens);

  await page.goto(`/projects/${project.id}`);
  await page.getByRole("button", { name: "Report" }).click();
  const dialog = page.getByRole("dialog", { name: "Report Project" });
  await expect(dialog.getByText(project.title)).toBeVisible();
  await dialog.getByLabel("Description").fill("Automated report validating context-bound target selection.");
  await dialog.getByRole("button", { name: "Submit report" }).click();
  await expect(page.getByText("Report submitted.")).toBeVisible();
});

test("CV upload validates type and sends a PDF through the UI", async ({ page, request }) => {
  const verified = await login(request, "verified@startupconnect.local");
  await authenticatePage(page, verified.tokens);
  await page.goto("/cvs");

  const input = page.getByLabel("Upload CV PDF");
  await input.setInputFiles({ name: "not-a-pdf.txt", mimeType: "text/plain", buffer: Buffer.from("not a pdf") });
  await expect(page.getByText("Only PDF uploads are supported.")).toBeVisible();

  const fileName = `e2e-cv-${Date.now()}.pdf`;
  await input.setInputFiles({ name: fileName, mimeType: "application/pdf", buffer: Buffer.from("%PDF-1.4\n%%EOF") });
  await expect(page.getByText("PDF uploaded.")).toBeVisible();
  await expect(page.getByText(`File: ${fileName}`)).toBeVisible();

  const cvsResponse = await request.get(`${API_BASE_URL}/cvs/me`, { headers: verified.headers });
  const cvs = (await cvsResponse.json()) as { data: Array<{ id: string; fileName?: string }> };
  const uploaded = cvs.data.find((cv) => cv.fileName === fileName);
  if (uploaded) await request.delete(`${API_BASE_URL}/cvs/${uploaded.id}`, { headers: verified.headers });
});
