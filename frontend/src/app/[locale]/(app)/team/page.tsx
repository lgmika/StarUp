"use client";

import { FormEvent, useEffect, useMemo, useState } from "react";
import { Check, MailPlus, RefreshCw, UserMinus, X } from "lucide-react";
import { LoadingState } from "@/components/common/loading-state";
import { Button } from "@/components/ui/button";
import { Input } from "@/components/ui/input";
import { Panel, PanelBody, PanelHeader, PanelTitle } from "@/components/ui/panel";
import { getApiErrorMessage } from "@/lib/api";
import { PageHeader } from "@/components/workspace/page-header";
import { StatusBadge } from "@/components/workspace/status-badge";
import { projectService } from "@/services/project-service";
import { projectTeamService } from "@/services/project-team-service";
import { ProjectMemberRole } from "@/types/enums";
import type { ProjectSummaryDto } from "@/types/project";
import type { ProjectInvitationDto, ProjectMemberDto } from "@/types/project-team";

export default function TeamPage() {
  const [projects, setProjects] = useState<ProjectSummaryDto[]>([]);
  const [selectedProjectId, setSelectedProjectId] = useState("");
  const [members, setMembers] = useState<ProjectMemberDto[]>([]);
  const [invitations, setInvitations] = useState<ProjectInvitationDto[]>([]);
  const [email, setEmail] = useState("");
  const [role, setRole] = useState<ProjectMemberRole>(ProjectMemberRole.Member);
  const [message, setMessage] = useState("");
  const [isLoading, setIsLoading] = useState(true);
  const [isMutating, setIsMutating] = useState(false);
  const [error, setError] = useState<string | null>(null);

  const selectedProject = useMemo(
    () => projects.find((project) => project.id === selectedProjectId) ?? null,
    [projects, selectedProjectId]
  );

  async function loadProjects() {
    setError(null);
    try {
      const [owned, joined] = await Promise.all([
        projectService.listOwnedProjects().catch(() => []),
        projectService.listJoinedProjects().catch(() => []),
      ]);
      const allProjects = dedupeProjects([...owned, ...joined]);
      setProjects(allProjects);
      setSelectedProjectId((current) => current || allProjects[0]?.id || "");
    } catch (loadError) {
      setError(getApiErrorMessage(loadError));
    } finally {
      setIsLoading(false);
    }
  }

  async function loadTeam(projectId: string) {
    setError(null);
    try {
      const [nextMembers, nextInvitations] = await Promise.all([
        projectTeamService.listMembers(projectId),
        projectTeamService.listInvitations(projectId),
      ]);
      setMembers(nextMembers);
      setInvitations(nextInvitations);
    } catch (loadError) {
      setError(getApiErrorMessage(loadError));
      setMembers([]);
      setInvitations([]);
    }
  }

  useEffect(() => {
    void loadProjects();
  }, []);

  useEffect(() => {
    if (selectedProjectId) void loadTeam(selectedProjectId);
  }, [selectedProjectId]);

  async function handleInvite(event: FormEvent<HTMLFormElement>) {
    event.preventDefault();
    if (!selectedProjectId || !email.trim()) return;
    setIsMutating(true);
    setError(null);
    try {
      await projectTeamService.invite(selectedProjectId, {
        email: email.trim(),
        role,
        message: message.trim() || undefined,
      });
      setEmail("");
      setMessage("");
      await loadTeam(selectedProjectId);
    } catch (inviteError) {
      setError(getApiErrorMessage(inviteError));
    } finally {
      setIsMutating(false);
    }
  }

  async function runMutation(action: () => Promise<unknown>) {
    if (!selectedProjectId) return;
    setIsMutating(true);
    setError(null);
    try {
      await action();
      await loadTeam(selectedProjectId);
    } catch (actionError) {
      setError(getApiErrorMessage(actionError));
    } finally {
      setIsMutating(false);
    }
  }

  if (isLoading) return <LoadingState label="Đang tải team" />;

  return (
    <div className="space-y-5">
      <PageHeader
        title="Team"
        description="Quản lý thành viên, lời mời, vai trò và quyền sở hữu theo từng dự án."
        actions={
          <Button variant="outline" size="sm" onClick={() => selectedProjectId && void loadTeam(selectedProjectId)}>
            <RefreshCw className="h-4 w-4" />
            Refresh
          </Button>
        }
      />
      {error ? <div className="rounded-md border border-red-200 bg-red-50 p-3 text-sm text-red-700">{error}</div> : null}
      <Panel>
        <PanelBody>
          <label className="space-y-1 text-sm font-medium">
            Dự án
            <select
              className="mt-1 h-10 w-full rounded-md border border-input bg-background px-3 text-sm"
              value={selectedProjectId}
              onChange={(event) => setSelectedProjectId(event.target.value)}
            >
              {projects.map((project) => (
                <option key={project.id} value={project.id}>
                  {project.title}
                </option>
              ))}
            </select>
          </label>
          {!selectedProject ? <p className="mt-3 text-sm text-muted-foreground">Bạn chưa có dự án để quản lý team.</p> : null}
        </PanelBody>
      </Panel>
      <Panel>
        <PanelHeader>
          <PanelTitle>Mời thành viên</PanelTitle>
        </PanelHeader>
        <PanelBody>
          <form className="grid gap-3 lg:grid-cols-[1fr_180px_1fr_auto]" onSubmit={handleInvite}>
            <Input placeholder="Email thành viên" type="email" value={email} onChange={(event) => setEmail(event.target.value)} disabled={!selectedProjectId || isMutating} />
            <select className="h-10 rounded-md border border-input bg-background px-3 text-sm" value={role} onChange={(event) => setRole(event.target.value as ProjectMemberRole)} disabled={!selectedProjectId || isMutating}>
              {Object.values(ProjectMemberRole).map((item) => <option key={item} value={item}>{item}</option>)}
            </select>
            <Input placeholder="Lời nhắn tùy chọn" value={message} onChange={(event) => setMessage(event.target.value)} disabled={!selectedProjectId || isMutating} />
            <Button type="submit" disabled={!selectedProjectId || !email.trim() || isMutating}>
              <MailPlus className="h-4 w-4" />
              Gửi lời mời
            </Button>
          </form>
        </PanelBody>
      </Panel>
      <Panel>
        <PanelHeader>
          <PanelTitle>Thành viên</PanelTitle>
        </PanelHeader>
        <PanelBody className="overflow-x-auto">
          <table className="w-full min-w-[760px] text-left text-sm">
            <thead className="border-b border-border text-xs text-muted-foreground">
              <tr>
                <th className="py-2 font-medium">Tên</th>
                <th className="py-2 font-medium">Email</th>
                <th className="py-2 font-medium">Vai trò</th>
                <th className="py-2 font-medium">Trạng thái</th>
                <th className="py-2 text-right font-medium">Action</th>
              </tr>
            </thead>
            <tbody>
              {members.map((member) => (
                <tr key={member.id} className="border-b border-border">
                  <td className="py-3 font-medium">{member.fullName}</td>
                  <td className="py-3 text-muted-foreground">{member.email}</td>
                  <td className="py-3">{member.role}</td>
                  <td className="py-3"><StatusBadge value={member.isActive ? "Active" : "Inactive"} /></td>
                  <td className="py-3">
                    <div className="flex justify-end gap-2">
                      <Button variant="outline" size="sm" disabled={isMutating} onClick={() => void runMutation(() => projectTeamService.updateMember(selectedProjectId, member.id, { role: ProjectMemberRole.CoFounder, reason: "Promoted from team UI" }))}>
                        CoFounder
                      </Button>
                      <Button variant="ghost" size="icon" aria-label="Xóa thành viên" disabled={isMutating} onClick={() => void runMutation(() => projectTeamService.removeMember(selectedProjectId, member.id))}>
                        <UserMinus className="h-4 w-4" />
                      </Button>
                    </div>
                  </td>
                </tr>
              ))}
            </tbody>
          </table>
          {members.length === 0 ? <p className="py-8 text-center text-sm text-muted-foreground">Chưa có member hoặc bạn không có quyền xem danh sách.</p> : null}
        </PanelBody>
      </Panel>
      <Panel>
        <PanelHeader>
          <PanelTitle>Lời mời đang chờ</PanelTitle>
        </PanelHeader>
        <PanelBody className="space-y-3">
          {invitations.map((invitation) => (
            <div key={invitation.id} className="flex flex-col justify-between gap-3 rounded-md border border-border p-4 md:flex-row md:items-center">
              <div>
                <div className="flex flex-wrap items-center gap-2">
                  <p className="font-medium">{invitation.email}</p>
                  <StatusBadge value={invitation.status} />
                </div>
                <p className="mt-1 text-sm text-muted-foreground">{invitation.role} · Expires {invitation.expiresAt}</p>
              </div>
              <div className="flex gap-2">
                <Button variant="outline" size="icon" aria-label="Accept invitation" disabled={isMutating} onClick={() => void runMutation(() => projectTeamService.acceptInvitation(invitation.id))}>
                  <Check className="h-4 w-4" />
                </Button>
                <Button variant="ghost" size="icon" aria-label="Reject invitation" disabled={isMutating} onClick={() => void runMutation(() => projectTeamService.rejectInvitation(invitation.id))}>
                  <X className="h-4 w-4" />
                </Button>
              </div>
            </div>
          ))}
          {invitations.length === 0 ? <p className="py-8 text-center text-sm text-muted-foreground">Không có lời mời đang chờ.</p> : null}
        </PanelBody>
      </Panel>
    </div>
  );
}

function dedupeProjects(projects: ProjectSummaryDto[]) {
  return Array.from(new Map(projects.map((project) => [project.id, project])).values());
}
