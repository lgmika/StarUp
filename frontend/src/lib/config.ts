export const API_BASE_URL = process.env.NEXT_PUBLIC_API_BASE_URL ?? "http://localhost:8080/api/v1";

export const REALTIME_HUB_URL =
  process.env.NEXT_PUBLIC_SIGNALR_HUB_URL ?? `${new URL(API_BASE_URL).origin}/hubs/realtime`;

export const MAX_CV_UPLOAD_BYTES = Number(
  process.env.NEXT_PUBLIC_MAX_CV_UPLOAD_BYTES ?? 5 * 1024 * 1024,
);
