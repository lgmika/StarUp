"use client";

import { FormEvent, useEffect, useMemo, useRef, useState } from "react";
import { useQuery, useMutation, useQueryClient } from "@tanstack/react-query";
import { MessageSquare, Paperclip, RefreshCw, Send, Trash2, UserRound } from "lucide-react";
import { toast } from "sonner";
import { Badge } from "@/components/ui/badge";
import { Button } from "@/components/ui/button";
import { InlineError } from "@/components/ui/error-boundary";
import { Skeleton } from "@/components/ui/skeleton";
import { getApiErrorMessage } from "@/lib/api";
import { queryKeys } from "@/lib/query-keys";
import { chatService } from "@/services/chat-service";
import type { ConversationDto } from "@/types/chat";
import { cn } from "@/lib/utils";

export default function MessagesPage() {
  const queryClient = useQueryClient();
  const [selectedId, setSelectedId] = useState<string | null>(null);
  const [draft, setDraft] = useState("");
  const scrollRef = useRef<HTMLDivElement>(null);

  // Auto-scroll to bottom of messages
  useEffect(() => {
    if (scrollRef.current) {
      scrollRef.current.scrollTop = scrollRef.current.scrollHeight;
    }
  }, [selectedId, draft]); // Actually, should only trigger when messages change

  const conversationsQuery = useQuery({
    queryKey: [...queryKeys.chat, "conversations"],
    queryFn: async () => {
      const data = await chatService.listConversations();
      if (!selectedId && data.length > 0) {
        setSelectedId(data[0].id);
      }
      return data;
    },
  });

  const messagesQuery = useQuery({
    queryKey: [...queryKeys.chat, "messages", selectedId],
    queryFn: async () => {
      if (!selectedId) return [];
      const response = await chatService.listMessages(selectedId);
      await chatService.markRead(selectedId);
      void queryClient.invalidateQueries({ queryKey: [...queryKeys.chat, "conversations"] });
      return response.items;
    },
    enabled: Boolean(selectedId),
  });

  // Keep scroll at bottom when new messages arrive
  useEffect(() => {
    if (scrollRef.current && messagesQuery.data) {
      scrollRef.current.scrollTop = scrollRef.current.scrollHeight;
    }
  }, [messagesQuery.data]);

  const sendMutation = useMutation({
    mutationFn: async (content: string) => {
      if (!selectedId) throw new Error("No conversation selected");
      return chatService.sendMessage(selectedId, { content });
    },
    onSuccess: () => {
      setDraft("");
      void queryClient.invalidateQueries({ queryKey: [...queryKeys.chat, "messages", selectedId] });
      void queryClient.invalidateQueries({ queryKey: [...queryKeys.chat, "conversations"] });
    },
    onError: (error) => toast.error(getApiErrorMessage(error)),
  });

  const deleteMutation = useMutation({
    mutationFn: async (messageId: string) => chatService.deleteMessage(messageId),
    onSuccess: () => {
      void queryClient.invalidateQueries({ queryKey: [...queryKeys.chat, "messages", selectedId] });
    },
    onError: (error) => toast.error(getApiErrorMessage(error)),
  });

  function handleSend(event: FormEvent<HTMLFormElement>) {
    event.preventDefault();
    if (!selectedId || !draft.trim() || sendMutation.isPending) return;
    sendMutation.mutate(draft.trim());
  }

  const conversations = conversationsQuery.data ?? [];
  const messages = messagesQuery.data ?? [];

  const selectedConversation = useMemo(
    () => conversationsQuery.data?.find((conversation) => conversation.id === selectedId) ?? null,
    [conversationsQuery.data, selectedId]
  );

  return (
    <div className="flex h-[calc(100vh-6rem)] min-h-[600px] flex-col gap-4 lg:flex-row">
      {/* Sidebar: Conversations List */}
      <div className="flex w-full flex-col overflow-hidden rounded-2xl border border-border/60 bg-card shadow-sm lg:w-[380px] lg:shrink-0">
        <div className="flex items-center justify-between border-b border-border p-4">
          <div className="flex items-center gap-2">
            <div className="flex h-8 w-8 items-center justify-center rounded-lg bg-primary/10 text-primary">
              <MessageSquare className="h-4 w-4" />
            </div>
            <h2 className="font-bold">Messages</h2>
          </div>
          <Button
            variant="ghost"
            size="icon"
            className="h-8 w-8"
            onClick={() => void conversationsQuery.refetch()}
            disabled={conversationsQuery.isFetching}
          >
            <RefreshCw className={cn("h-4 w-4", conversationsQuery.isFetching && "animate-spin")} />
          </Button>
        </div>

        {conversationsQuery.error ? (
          <div className="p-4">
            <InlineError
              message={getApiErrorMessage(conversationsQuery.error)}
              onRetry={() => void conversationsQuery.refetch()}
            />
          </div>
        ) : null}

        <div className="flex-1 overflow-y-auto p-2 scrollbar-thin">
          {conversationsQuery.isLoading ? (
            <div className="space-y-2">
              {Array.from({ length: 5 }).map((_, i) => (
                <Skeleton key={i} className="h-20 w-full rounded-xl" />
              ))}
            </div>
          ) : conversations.length === 0 ? (
            <div className="flex h-full flex-col items-center justify-center space-y-3 text-center text-muted-foreground p-4">
              <MessageSquare className="h-8 w-8 opacity-20" />
              <p className="text-sm">No conversations yet.</p>
            </div>
          ) : (
            <div className="space-y-1">
              {conversations.map((conversation) => {
                const isActive = selectedId === conversation.id;
                const title = conversation.title ?? getConversationTitle(conversation);
                return (
                  <button
                    key={conversation.id}
                    type="button"
                    onClick={() => setSelectedId(conversation.id)}
                    className={cn(
                      "w-full rounded-xl p-3 text-left transition-all duration-200",
                      isActive
                        ? "bg-primary text-primary-foreground shadow-md"
                        : "hover:bg-muted"
                    )}
                  >
                    <div className="flex items-start justify-between gap-2">
                      <div className="min-w-0 flex-1">
                        <p className={cn("truncate font-semibold", !isActive && "text-foreground")}>
                          {title}
                        </p>
                        <p className={cn("mt-1 truncate text-xs", isActive ? "text-primary-foreground/80" : "text-muted-foreground")}>
                          {conversation.participants.length} participants
                        </p>
                      </div>
                      <Badge
                        tone="muted"
                        className={cn(
                          "shrink-0 text-[10px]",
                          isActive ? "bg-primary-foreground/20 text-primary-foreground hover:bg-primary-foreground/30" : "bg-muted-foreground/10 text-muted-foreground"
                        )}
                      >
                        {conversation.type}
                      </Badge>
                    </div>
                  </button>
                );
              })}
            </div>
          )}
        </div>
      </div>

      {/* Main Content: Chat Thread */}
      <div className="flex flex-1 flex-col overflow-hidden rounded-2xl border border-border/60 bg-card shadow-sm">
        {selectedId ? (
          <>
            {/* Chat Header */}
            <div className="flex items-center gap-3 border-b border-border p-4 shadow-sm z-10">
              <div className="flex h-10 w-10 shrink-0 items-center justify-center rounded-full bg-primary/10 text-primary">
                <UserRound className="h-5 w-5" />
              </div>
              <div>
                <h3 className="font-bold">
                  {selectedConversation?.title ?? getConversationTitle(selectedConversation)}
                </h3>
                <p className="text-xs text-muted-foreground">
                  {selectedConversation?.type} Chat
                </p>
              </div>
            </div>

            {/* Chat Messages */}
            <div
              ref={scrollRef}
              className="flex-1 overflow-y-auto p-4 space-y-4 scrollbar-thin bg-muted/10"
            >
              {messagesQuery.isLoading ? (
                <div className="space-y-4">
                  <Skeleton className="h-16 w-2/3 rounded-2xl rounded-tl-sm" />
                  <Skeleton className="h-16 w-2/3 self-end rounded-2xl rounded-tr-sm ml-auto" />
                  <Skeleton className="h-16 w-1/2 rounded-2xl rounded-tl-sm" />
                </div>
              ) : messagesQuery.error ? (
                <InlineError
                  message={getApiErrorMessage(messagesQuery.error)}
                  onRetry={() => void messagesQuery.refetch()}
                />
              ) : messages.length === 0 ? (
                <div className="flex h-full flex-col items-center justify-center text-center">
                  <div className="flex h-12 w-12 items-center justify-center rounded-full bg-primary/10 text-primary mb-3">
                    <MessageSquare className="h-6 w-6" />
                  </div>
                  <h4 className="font-semibold">Start the conversation</h4>
                  <p className="text-sm text-muted-foreground mt-1 max-w-sm">
                    Send a message to begin discussing with the participants of this thread.
                  </p>
                </div>
              ) : (
                messages.map((message) => {
                  // In a real app we'd compare with currentUser.email, but here we just
                  // alternate loosely based on senderEmail to simulate the UI, or 
                  // assume if it's the current user it will be on the right. 
                  // For now, we'll just put it on the left unless we know who the user is.
                  // Since we don't have current user in this context, we will style all left
                  // but we can make a visually distinct bubble.
                  return (
                    <div
                      key={message.id}
                      className={cn(
                        "group flex w-full flex-col",
                        // if we had a currentUser we would check: message.senderEmail === currentUser.email ? "items-end" : "items-start"
                        "items-start" 
                      )}
                    >
                      <div className="flex items-baseline gap-2 mb-1">
                        <span className="text-xs font-medium text-foreground">{message.senderEmail}</span>
                        <span className="text-[10px] text-muted-foreground">{new Date(message.createdAt).toLocaleTimeString([], { hour: '2-digit', minute: '2-digit' })}</span>
                      </div>
                      <div className="relative group/bubble flex items-start gap-2 max-w-[85%]">
                        <div
                          className={cn(
                            "rounded-2xl px-4 py-2.5 text-sm shadow-sm",
                            message.isDeleted
                              ? "bg-muted border border-border italic text-muted-foreground"
                              : "bg-card border border-border/60 text-foreground"
                          )}
                        >
                          <p className="whitespace-pre-wrap leading-relaxed">
                            {message.isDeleted ? "This message was deleted." : message.content}
                          </p>
                        </div>
                        {!message.isDeleted ? (
                          <Button
                            variant="ghost"
                            size="icon"
                            className="h-8 w-8 shrink-0 opacity-0 transition-opacity group-hover/bubble:opacity-100 self-center"
                            onClick={() => deleteMutation.mutate(message.id)}
                            disabled={deleteMutation.isPending}
                          >
                            <Trash2 className="h-4 w-4 text-destructive" />
                          </Button>
                        ) : null}
                      </div>
                    </div>
                  );
                })
              )}
            </div>

            {/* Chat Input */}
            <div className="border-t border-border p-4 bg-card z-10">
              <form
                onSubmit={handleSend}
                className="flex items-end gap-2 rounded-2xl border border-input bg-background p-2 focus-within:ring-2 focus-within:ring-ring"
              >
                <Button
                  type="button"
                  variant="ghost"
                  size="icon"
                  className="h-10 w-10 shrink-0 text-muted-foreground"
                  disabled
                >
                  <Paperclip className="h-5 w-5" />
                </Button>
                <textarea
                  value={draft}
                  onChange={(e) => setDraft(e.target.value)}
                  placeholder="Type a message..."
                  className="min-h-[40px] max-h-32 flex-1 resize-none bg-transparent px-2 py-2.5 text-sm outline-none placeholder:text-muted-foreground scrollbar-thin"
                  onKeyDown={(e) => {
                    if (e.key === "Enter" && !e.shiftKey) {
                      e.preventDefault();
                      handleSend(e as unknown as FormEvent<HTMLFormElement>);
                    }
                  }}
                  disabled={sendMutation.isPending}
                />
                <Button
                  type="submit"
                  size="icon"
                  className="h-10 w-10 shrink-0 rounded-xl"
                  disabled={!draft.trim() || sendMutation.isPending}
                >
                  <Send className="h-4 w-4" />
                </Button>
              </form>
              <p className="mt-2 text-center text-[10px] text-muted-foreground">
                Press <kbd className="rounded border bg-muted px-1 font-sans">Enter</kbd> to send, <kbd className="rounded border bg-muted px-1 font-sans">Shift + Enter</kbd> for new line
              </p>
            </div>
          </>
        ) : (
          <div className="flex h-full flex-col items-center justify-center space-y-4 text-center p-8">
            <div className="flex h-20 w-20 items-center justify-center rounded-full bg-primary/10 text-primary">
              <MessageSquare className="h-10 w-10" />
            </div>
            <div>
              <h3 className="text-xl font-bold">Your Messages</h3>
              <p className="text-muted-foreground mt-2 max-w-sm">
                Select a conversation from the sidebar to view the thread and reply.
              </p>
            </div>
          </div>
        )}
      </div>
    </div>
  );
}

function getConversationTitle(conversation: ConversationDto | null) {
  if (!conversation) return "";
  return conversation.participants.map((participant) => participant.fullName || participant.email).join(", ") || conversation.type;
}
