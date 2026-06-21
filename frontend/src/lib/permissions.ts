import { SystemRoles, type SystemRole } from './constants';

/**
 * Check if user has a specific role
 */
export function hasRole(userRoles: string[], role: SystemRole): boolean {
  return userRoles.includes(role);
}

/**
 * Check if user has any of the specified roles
 */
export function hasAnyRole(userRoles: string[], roles: SystemRole[]): boolean {
  return roles.some((role) => userRoles.includes(role));
}

/**
 * Check if user is an admin
 */
export function isAdmin(userRoles: string[]): boolean {
  return hasRole(userRoles, SystemRoles.Admin);
}

/**
 * Check if user is a moderator (or admin)
 */
export function isModerator(userRoles: string[]): boolean {
  return hasAnyRole(userRoles, [SystemRoles.Moderator, SystemRoles.Admin]);
}

/**
 * Check if user is an investor (or admin)
 */
export function isInvestor(userRoles: string[]): boolean {
  return hasAnyRole(userRoles, [SystemRoles.Investor, SystemRoles.Admin]);
}

/**
 * Check if user is a verified user (or admin)
 */
export function isVerifiedUser(userRoles: string[]): boolean {
  return hasAnyRole(userRoles, [SystemRoles.VerifiedUser, SystemRoles.Admin]);
}

/**
 * Check if user is a business user (or admin)
 */
export function isBusiness(userRoles: string[]): boolean {
  return hasAnyRole(userRoles, [SystemRoles.Business, SystemRoles.Admin]);
}

/**
 * Check if a user is the owner of a project
 */
export function isProjectOwner(userId: string, projectOwnerUserId: string): boolean {
  return userId === projectOwnerUserId;
}

/**
 * Get the primary display role for a user (highest priority role)
 */
export function getPrimaryRole(roles: string[]): string {
  const priority: SystemRole[] = [
    SystemRoles.Admin,
    SystemRoles.Moderator,
    SystemRoles.Investor,
    SystemRoles.Business,
    SystemRoles.VerifiedUser,
    SystemRoles.User,
    SystemRoles.Guest,
  ];

  for (const role of priority) {
    if (roles.includes(role)) return role;
  }

  return SystemRoles.Guest;
}

export function getRoleHome(roles: string[]): string {
  switch (getPrimaryRole(roles)) {
    case SystemRoles.Admin:
      return '/admin';
    case SystemRoles.Moderator:
      return '/moderator';
    case SystemRoles.Investor:
      return '/investor';
    case SystemRoles.Business:
      return '/projects/me/owned';
    default:
      return '/dashboard';
  }
}
