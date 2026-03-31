import { HubConnection, HubConnectionBuilder, HubConnectionState, LogLevel } from '@microsoft/signalr';
import {
  getApiBaseUrl,
  mapUserRealtimeNotification,
  type ItineraryRealtimeNotification,
  type UserRealtimeNotification,
  type UserRealtimeNotificationDto,
} from './api';

interface RealtimeHandlers {
  onItineraryNotification: (notification: ItineraryRealtimeNotification) => void;
  onUserNotification: (notification: UserRealtimeNotification) => void;
}

class ItineraryRealtimeClient {
  private connection: HubConnection | null = null;
  private token: string | null = null;
  private handlers: RealtimeHandlers | null = null;
  private joinedItineraryIds = new Set<string>();
  private desiredItineraryIds = new Set<string>();

  async connect(token: string, handlers: RealtimeHandlers) {
    this.handlers = handlers;

    if (this.connection && this.token === token && this.connection.state === HubConnectionState.Connected) {
      return;
    }

    await this.disconnect();

    this.token = token;
    this.connection = new HubConnectionBuilder()
      .withUrl(`${getApiBaseUrl()}/hubs/itinerary`, {
        accessTokenFactory: () => token,
      })
      .withAutomaticReconnect()
      .configureLogging(LogLevel.Warning)
      .build();

    this.connection.on('itineraryUpdated', (notification: ItineraryRealtimeNotification) => {
      this.handlers?.onItineraryNotification(notification);
    });

    this.connection.on('userNotification', (notification: UserRealtimeNotificationDto) => {
      this.handlers?.onUserNotification(mapUserRealtimeNotification(notification));
    });

    this.connection.onreconnected(() => this.rejoinDesiredItineraries());
    this.connection.onclose(() => {
      this.joinedItineraryIds.clear();
    });

    await this.connection.start();
    await this.rejoinDesiredItineraries();
  }

  async syncItineraries(itineraryIds: string[]) {
    this.desiredItineraryIds = new Set(itineraryIds);

    if (!this.connection || this.connection.state !== HubConnectionState.Connected) {
      return;
    }

    const nextIds = new Set(itineraryIds);

    for (const itineraryId of this.joinedItineraryIds) {
      if (nextIds.has(itineraryId)) {
        continue;
      }

      await this.connection.invoke('LeaveItinerary', itineraryId);
      this.joinedItineraryIds.delete(itineraryId);
    }

    for (const itineraryId of nextIds) {
      if (this.joinedItineraryIds.has(itineraryId)) {
        continue;
      }

      await this.connection.invoke('JoinItinerary', itineraryId);
      this.joinedItineraryIds.add(itineraryId);
    }
  }

  async disconnect() {
    if (this.connection) {
      try {
        for (const itineraryId of this.joinedItineraryIds) {
          await this.connection.invoke('LeaveItinerary', itineraryId);
        }
      } catch {
        // Ignore best-effort group cleanup during disconnect.
      }

      this.joinedItineraryIds.clear();
      this.desiredItineraryIds.clear();
      await this.connection.stop();
      this.connection = null;
    }
  }

  private async rejoinDesiredItineraries() {
    await this.syncItineraries(Array.from(this.desiredItineraryIds));
  }
}

export const itineraryRealtimeClient = new ItineraryRealtimeClient();
