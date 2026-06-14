import api from "@/lib/api";
import type { ApiResponse } from "@/types/api";
import type {
  CreateProjectInvitationRequest,
  ProjectInvitationDto,
  ProjectMemberDto,
  TransferOwnershipRequest,
  UpdateProjectMemberRequest,
} from "@/types/project-team";

export const projectTeamService = {
  async listMembers(projectId: string) {
    const { data } = await api.get<ApiResponse<ProjectMemberDto[]>>(`/projects/${projectId}/members`);
    return data.data;
  },

  async updateMember(projectId: string, memberId: string, request: UpdateProjectMemberRequest) {
    const { data } = await api.patch<ApiResponse<ProjectMemberDto>>(`/projects/${projectId}/members/${memberId}`, request);
    return data.data;
  },

  async removeMember(projectId: string, memberId: string) {
    await api.delete<ApiResponse<null>>(`/projects/${projectId}/members/${memberId}`);
  },

  async invite(projectId: string, request: CreateProjectInvitationRequest) {
    const { data } = await api.post<ApiResponse<ProjectInvitationDto>>(`/projects/${projectId}/invitations`, request);
    return data.data;
  },

  async listInvitations(projectId: string) {
    const { data } = await api.get<ApiResponse<ProjectInvitationDto[]>>(`/projects/${projectId}/invitations`);
    return data.data;
  },

  async acceptInvitation(invitationId: string) {
    const { data } = await api.post<ApiResponse<ProjectInvitationDto>>(`/project-invitations/${invitationId}/accept`);
    return data.data;
  },

  async rejectInvitation(invitationId: string) {
    const { data } = await api.post<ApiResponse<ProjectInvitationDto>>(`/project-invitations/${invitationId}/reject`);
    return data.data;
  },

  async cancelInvitation(invitationId: string) {
    const { data } = await api.post<ApiResponse<ProjectInvitationDto>>(`/project-invitations/${invitationId}/cancel`);
    return data.data;
  },

  async transferOwnership(projectId: string, request: TransferOwnershipRequest) {
    const { data } = await api.post<ApiResponse<unknown>>(`/projects/${projectId}/transfer-ownership`, request);
    return data.data;
  },

  async leave(projectId: string) {
    await api.post<ApiResponse<null>>(`/projects/${projectId}/leave`);
  },
};
