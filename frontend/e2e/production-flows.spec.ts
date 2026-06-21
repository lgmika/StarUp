import { expect, test, type APIRequestContext } from "@playwright/test";

const API_BASE_URL =
  process.env.E2E_API_BASE_URL ?? "http://localhost:8080/api/v1";

async function login(request: APIRequestContext, email: string) {
  const response = await request.post(`${API_BASE_URL}/auth/login`, {
    data: { email, password: "Startup123!" },
  });
  expect(response.ok(), await response.text()).toBeTruthy();
  const body = (await response.json()) as { data: { accessToken: string } };
  return { Authorization: `Bearer ${body.data.accessToken}` };
}

async function createPublishedProject(
  request: APIRequestContext,
  businessHeaders: Record<string, string>,
  moderatorHeaders: Record<string, string>,
  visibility = "Public",
) {
  const suffix = `${Date.now()}-${Math.random().toString(16).slice(2)}`;
  const draftResponse = await request.post(`${API_BASE_URL}/projects/drafts`, {
    headers: businessHeaders,
    data: {
      title: `E2E Production Flow ${suffix}`,
      summary: "Automated production workflow validation.",
      problem: "Critical workflows require repeatable end-to-end coverage.",
      solution: "Exercise the real API and PostgreSQL state transitions.",
      stage: "MVP",
      visibility,
    },
  });
  expect(draftResponse.ok(), await draftResponse.text()).toBeTruthy();
  const draft = (await draftResponse.json()) as { data: { id: string } };

  const submitResponse = await request.post(
    `${API_BASE_URL}/projects/${draft.data.id}/submit-review`,
    { headers: businessHeaders },
  );
  expect(submitResponse.ok(), await submitResponse.text()).toBeTruthy();

  const approveResponse = await request.post(
    `${API_BASE_URL}/moderator/projects/${draft.data.id}/approve`,
    {
      headers: moderatorHeaders,
      data: { reason: "Approved by automated production workflow." },
    },
  );
  expect(approveResponse.ok(), await approveResponse.text()).toBeTruthy();
  return draft.data.id;
}

test("application acceptance creates exactly one active project member", async ({
  request,
}) => {
  const businessHeaders = await login(request, "business@startupconnect.local");
  const moderatorHeaders = await login(request, "moderator@startupconnect.local");
  const applicantHeaders = await login(request, "verified@startupconnect.local");
  const projectId = await createPublishedProject(
    request,
    businessHeaders,
    moderatorHeaders,
  );

  const applyResponse = await request.post(
    `${API_BASE_URL}/projects/${projectId}/applications`,
    {
      headers: applicantHeaders,
      data: { cvId: null, coverLetter: "Automated E2E application." },
    },
  );
  expect(applyResponse.ok(), await applyResponse.text()).toBeTruthy();
  const application = (await applyResponse.json()) as { data: { id: string } };

  const acceptResponse = await request.post(
    `${API_BASE_URL}/projects/${projectId}/applications/${application.data.id}/accept`,
    {
      headers: businessHeaders,
      data: { reason: "Accepted by E2E.", founderNote: "Welcome." },
    },
  );
  expect(acceptResponse.ok(), await acceptResponse.text()).toBeTruthy();

  const membersResponse = await request.get(
    `${API_BASE_URL}/projects/${projectId}/members`,
    { headers: businessHeaders },
  );
  expect(membersResponse.ok(), await membersResponse.text()).toBeTruthy();
  const members = (await membersResponse.json()) as {
    data: Array<{ email: string; isActive: boolean }>;
  };
  expect(
    members.data.filter(
      (member) =>
        member.email === "verified@startupconnect.local" && member.isActive,
    ),
  ).toHaveLength(1);
});

test("accepted investor interest grants project access", async ({ request }) => {
  const businessHeaders = await login(request, "business@startupconnect.local");
  const moderatorHeaders = await login(request, "moderator@startupconnect.local");
  const investorHeaders = await login(request, "investor@startupconnect.local");
  const projectId = await createPublishedProject(
    request,
    businessHeaders,
    moderatorHeaders,
    "InvestorOnly",
  );

  const beforeAcceptance = await request.get(
    `${API_BASE_URL}/projects/${projectId}`,
    { headers: investorHeaders },
  );
  expect(beforeAcceptance.status()).toBe(403);

  const interestResponse = await request.post(
    `${API_BASE_URL}/projects/${projectId}/investor-interests`,
    {
      headers: investorHeaders,
      data: { message: "Automated E2E investor interest." },
    },
  );
  expect(interestResponse.ok(), await interestResponse.text()).toBeTruthy();
  const interest = (await interestResponse.json()) as { data: { id: string } };

  const acceptResponse = await request.post(
    `${API_BASE_URL}/projects/${projectId}/investor-interests/${interest.data.id}/accept`,
    {
      headers: businessHeaders,
      data: { response: "Accepted by E2E." },
    },
  );
  expect(acceptResponse.ok(), await acceptResponse.text()).toBeTruthy();

  const projectResponse = await request.get(
    `${API_BASE_URL}/projects/${projectId}`,
    { headers: investorHeaders },
  );
  expect(projectResponse.ok(), await projectResponse.text()).toBeTruthy();
});
