"use client";

import type { ReactNode } from "react";
import { ForbiddenState } from "@/components/common/forbidden-state";
import { hasAnyRole } from "@/lib/permissions";
import type { SystemRole } from "@/lib/constants";
import { useAuthStore } from "@/stores/auth-store";

interface RoleGuardProps {
  allowedRoles: SystemRole[];
  children: ReactNode;
}

export function RoleGuard({ allowedRoles, children }: RoleGuardProps) {
  const user = useAuthStore((state) => state.user);

  if (!user || !hasAnyRole(user.roles, allowedRoles)) {
    return <ForbiddenState />;
  }

  return children;
}
