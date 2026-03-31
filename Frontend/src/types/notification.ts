export interface UserNotification {
  id: string;
  type: string;
  title: string;
  message: string;
  itineraryId: string | null;
  actorUserId: string | null;
  createdAt: string;
  readAt: string | null;
}
