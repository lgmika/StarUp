import { chatService } from "@/services/chat-service";

export const chatApi = {
  createConversation: chatService.createConversation,
  getConversations: chatService.listConversations,
  getConversation: chatService.getConversation,
  getMessages: chatService.listMessages,
  sendMessage: chatService.sendMessage,
  markRead: chatService.markRead,
};
