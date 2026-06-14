import { adminService } from "@/services/admin-service";
import { backgroundJobService } from "@/services/background-job-service";
import { ndaService } from "@/services/nda-service";
import { reportService } from "@/services/report-service";

export const adminApi = {
  getDashboard: adminService.getDashboard,
  getUsers: adminService.listUsers,
  getReports: reportService.listModeratorReports,
  getAuditLogs: adminService.listAuditLogs,
  getRoles: adminService.listRoles,
  getBackgroundJobs: backgroundJobService.list,
  nda: ndaService,
};
