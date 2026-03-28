import { useTravelStore } from '../app/store/useTravelStore';

export const useCurrentUser = () =>
  useTravelStore((state) => state.users.find((user) => user.id === state.currentUserId) ?? null);
