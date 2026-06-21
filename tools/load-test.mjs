const url = process.argv[2] ?? "http://localhost:8080/api/v1/health/ready";
const totalRequests = parsePositiveInteger(process.argv[3], 500, "totalRequests");
const concurrency = parsePositiveInteger(process.argv[4], 25, "concurrency");
const maximumP95Ms = parsePositiveInteger(process.argv[5], 1000, "maximumP95Ms");
const maximumFailureRate = Number(process.argv[6] ?? "0.01");

if (maximumFailureRate < 0 || maximumFailureRate > 1) {
  throw new Error("maximumFailureRate must be between 0 and 1");
}

const durations = [];
const statusCounts = new Map();
let nextRequest = 0;
let failures = 0;
const startedAt = performance.now();

async function worker() {
  while (true) {
    const requestNumber = nextRequest++;
    if (requestNumber >= totalRequests) return;

    const requestStartedAt = performance.now();
    try {
      const response = await fetch(url, {
        headers: process.env.LOAD_TEST_TOKEN
          ? { Authorization: `Bearer ${process.env.LOAD_TEST_TOKEN}` }
          : undefined,
        signal: AbortSignal.timeout(10_000),
      });
      statusCounts.set(response.status, (statusCounts.get(response.status) ?? 0) + 1);
      if (!response.ok) failures++;
      await response.arrayBuffer();
    } catch {
      failures++;
      statusCounts.set("network-error", (statusCounts.get("network-error") ?? 0) + 1);
    } finally {
      durations.push(performance.now() - requestStartedAt);
    }
  }
}

await Promise.all(
  Array.from({ length: Math.min(concurrency, totalRequests) }, () => worker()),
);

durations.sort((left, right) => left - right);
const elapsedMs = performance.now() - startedAt;
const percentile = (value) => durations[Math.min(durations.length - 1, Math.ceil(durations.length * value) - 1)];
const p95 = percentile(0.95);
const failureRate = failures / totalRequests;

console.log(JSON.stringify({
  url,
  totalRequests,
  concurrency,
  elapsedMs: Math.round(elapsedMs),
  requestsPerSecond: Number((totalRequests / (elapsedMs / 1000)).toFixed(2)),
  p50Ms: Number(percentile(0.5).toFixed(2)),
  p95Ms: Number(p95.toFixed(2)),
  p99Ms: Number(percentile(0.99).toFixed(2)),
  failureRate,
  statusCounts: Object.fromEntries(statusCounts),
}, null, 2));

if (failureRate > maximumFailureRate || p95 > maximumP95Ms) {
  process.exitCode = 1;
}

function parsePositiveInteger(value, fallback, name) {
  const parsed = Number(value ?? fallback);
  if (!Number.isInteger(parsed) || parsed < 1) {
    throw new Error(`${name} must be a positive integer`);
  }
  return parsed;
}
