import type { NextConfig } from "next";

const nextConfig: NextConfig = {
  // Allow images from external sources if needed
  images: {
    remotePatterns: [
      {
        protocol: "http",
        hostname: "localhost",
        port: "8080",
      },
    ],
  },
};

export default nextConfig;
