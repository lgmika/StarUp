import api from "@/lib/api";
import type { ApiResponse } from "@/types/api";
import type {
  ConversationDto,
  CreateConversationRequest,
  MessageDto,
  MessageListResponse,
  SendMessageRequest,
} from "@/types/chat";

export const chatService = {
  async createConversation(request: CreateConversationRequest) {
    const { data } = await api.post<ApiResponse<ConversationDto>>("/conversations", request);
    return data.data;
  },

  async listConversations() {
    const { data } = await api.get<ApiResponse<ConversationDto[]>>("/conversations");
    return data.data;
  },

  async getConversation(conversationId: string) {
    const { data } = await api.get<ApiResponse<ConversationDto>>(`/conversations/${conversationId}`);
    return data.data;
  },

  async listMessages(conversationId: string, before?: string, pageSize = 20) {
    const { data } = await api.get<ApiResponse<MessageListResponse>>(`/conversations/${conversationId}/messages`, {
      params: { before, pageSize },
    });
    return data.data;
  },

  async sendMessage(conversationId: string, request: SendMessageRequest) {
    const { data } = await api.post<ApiResponse<MessageDto>>(`/conversations/${conversationId}/messages`, request);
    return data.data;
  },

  async markRead(conversationId: string) {
    await api.post<ApiResponse<null>>(`/conversations/${conversationId}/read`);
  },

  async deleteMessage(messageId: string) {
    await api.delete<ApiResponse<null>>(`/messages/${messageId}`);
  },
};
