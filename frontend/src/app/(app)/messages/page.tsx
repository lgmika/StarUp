"use client";

import { FormEvent, useEffect, useMemo, useState } from "react";
import { Paperclip, RefreshCw, Send, Trash2 } from "lucide-react";
import { LoadingState } from "@/components/common/loading-state";
import { Button } from "@/components/ui/button";
import { Input } from "@/components/ui/input";
import { Panel, PanelBody, PanelHeader, PanelTitle } from "@/components/ui/panel";
import { getApiErrorMessage } from "@/lib/api";
import { PageHeader } from "@/components/workspace/page-header";
import { StatusBadge } from "@/components/workspace/status-badge";
import { chatService } from "@/services/chat-service";
import type { ConversationDto, MessageDto } from "@/types/chat";

export default function MessagesPage() {
  const [conversations, setConversations] = useState<ConversationDto[]>([]);
  const [selectedId, setSelectedId] = useState<string | null>(null);
  const [messages, setMessages] = useState<MessageDto[]>([]);
  const [draft, setDraft] = useState("");
  const [isLoading, setIsLoading] = useState(true);
  const [isSending, setIsSending] = useState(false);
  const [error, setError] = useState<string | null>(null);

  const selectedConversation = useMemo(
    () => conversations.find((conversation) => conversation.id === selectedId) ?? null,
    [conversations, selectedId]
  );

  async function loadConversations() {
    setError(null);
    try {
      const nextConversations = await chatService.listConversations();
      setConversations(nextConversations);
      setSelectedId((current) => current ?? nextConversations[0]?.id ?? null);
    } catch (loadError) {
      setError(getApiErrorMessage(loadError));
    } finally {
      setIsLoading(false);
    }
  }

  async function loadMessages(conversationId: string) {
    setError(null);
    try {
      const response = await chatService.listMessages(conversationId);
      setMessages(response.items);
      await chatService.markRead(conversationId);
    } catch (loadError) {
      setError(getApiErrorMessage(loadError));
      setMessages([]);
    }
  }

  useEffect(() => {
    void loadConversations();
  }, []);

  useEffect(() => {
    if (selectedId) void loadMessages(selectedId);
  }, [selectedId]);

  async function handleSend(event: FormEvent<HTMLFormElement>) {
    event.preventDefault();
    if (!selectedId || !draft.trim()) return;
    setIsSending(true);
    setError(null);
    try {
      const sent = await chatService.sendMessage(selectedId, { content: draft.trim() });
      setMessages((current) => [...current, sent]);
      setDraft("");
      await loadConversations();
    } catch (sendError) {
      setError(getApiErrorMessage(sendError));
    } finally {
      setIsSending(false);
    }
  }

  async function handleDelete(messageId: string) {
    setError(null);
    try {
      await chatService.deleteMessage(messageId);
      setMessages((current) => current.filter((message) => message.id !== messageId));
    } catch (deleteError) {
      setError(getApiErrorMessage(deleteError));
    }
  }

  if (isLoading) return <LoadingState label="Đang tải tin nhắn" />;

  return (
    <div className="space-y-5">
      <PageHeader
        title="Messages"
        description="Conversations, thread messages, send message, mark read và delete message qua backend Chat API."
        actions={
          <Button variant="outline" size="sm" onClick={() => void loadConversations()}>
            <RefreshCw className="h-4 w-4" />
            Refresh
          </Button>
        }
      />
      {error ? <div className="rounded-md border border-red-200 bg-red-50 p-3 text-sm text-red-700">{error}</div> : null}
      <div className="grid gap-4 lg:grid-cols-[340px_1fr]">
        <Panel>
          <PanelHeader><PanelTitle>Conversations</PanelTitle></PanelHeader>
          <PanelBody className="space-y-2">
            {conversations.map((conversation) => (
              <button
                key={conversation.id}
                type="button"
                className={`w-full rounded-md border border-border p-3 text-left hover:bg-accent ${selectedId === conversation.id ? "bg-accent" : ""}`}
                onClick={() => setSelectedId(conversation.id)}
              >
                <div className="flex items-center justify-between gap-2">
                  <p className="truncate text-sm font-medium">{conversation.title ?? getConversationTitle(conversation)}</p>
                  <StatusBadge value={conversation.type} />
                </div>
                <p className="mt-1 text-xs text-muted-foreground">
                  {conversation.participants.length} participants · {conversation.lastMessageAt ?? conversation.createdAt}
                </p>
              </button>
            ))}
            {conversations.length === 0 ? <p className="py-8 text-center text-sm text-muted-foreground">Chưa có conversation nào.</p> : null}
          </PanelBody>
        </Panel>
        <Panel>
          <PanelHeader>
            <PanelTitle>{selectedConversation ? (selectedConversation.title ?? getConversationTitle(selectedConversation)) : "Thread"}</PanelTitle>
          </PanelHeader>
          <PanelBody className="flex min-h-[520px] flex-col">
            <div className="flex-1 space-y-3 overflow-y-auto">
              {messages.map((message) => (
                <div key={message.id} className="group max-w-2xl rounded-md border border-border bg-muted p-3 text-sm">
                  <div className="flex items-start justify-between gap-3">
                    <div>
                      <p className="text-xs font-medium text-muted-foreground">{message.senderEmail}</p>
                      <p className="mt-1 whitespace-pre-wrap">{message.isDeleted ? "Tin nhắn đã bị xóa" : message.content}</p>
                      <p className="mt-2 text-xs text-muted-foreground">{message.createdAt}</p>
                    </div>
                    {!message.isDeleted ? (
                      <Button variant="ghost" size="icon" aria-label="Xóa tin nhắn" onClick={() => void handleDelete(message.id)}>
                        <Trash2 className="h-4 w-4" />
                      </Button>
                    ) : null}
                  </div>
                </div>
              ))}
              {selectedConversation && messages.length === 0 ? <p className="py-10 text-center text-sm text-muted-foreground">Chưa có tin nhắn.</p> : null}
              {!selectedConversation ? <p className="py-10 text-center text-sm text-muted-foreground">Chọn một conversation để xem thread.</p> : null}
            </div>
            <form className="mt-4 flex gap-2" onSubmit={handleSend}>
              <Button variant="outline" size="icon" aria-label="Đính kèm file" disabled>
                <Paperclip className="h-4 w-4" />
              </Button>
              <Input value={draft} onChange={(event) => setDraft(event.target.value)} placeholder="Nhập tin nhắn..." disabled={!selectedId || isSending} />
              <Button type="submit" disabled={!selectedId || !draft.trim() || isSending}>
                <Send className="h-4 w-4" />
                Gửi
              </Button>
            </form>
          </PanelBody>
        </Panel>
      </div>
    </div>
  );
}

function getConversationTitle(conversation: ConversationDto) {
  return conversation.participants.map((participant) => participant.fullName || participant.email).join(", ") || conversation.type;
}
