import { HubConnection, HubConnectionBuilder, HubConnectionState, LogLevel } from '@microsoft/signalr';
import { getApiBaseUrl, type ItineraryRealtimeNotification } from './api';

type NotificationHandler = (notification: ItineraryRealtimeNotification) => void;

class ItineraryRealtimeClient {
  private connection: HubConnection | null = null;
  private token: string | null = null;
  private handler: NotificationHandler | null = null;
  private joinedItineraryIds = new Set<string>();

  async connect(token: string, onNotification: NotificationHandler) {
    this.handler = onNotification;

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
      this.handler?.(notification);
    });

    await this.connection.start();
  }

  async syncItineraries(itineraryIds: string[]) {
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
      await this.connection.stop();
      this.connection = null;
    }
  }
}

export const itineraryRealtimeClient = new ItineraryRealtimeClient();
