import { BASE_URL } from '../config';

export interface ThoHSimulationStatus {
  status: string;
  isRunning: boolean;
  epochStartTime?: number;
  startTime?: string;
}

class ThoHSimulationService {
  private currentStatus: ThoHSimulationStatus | null = null;
  private listeners: Array<(status: ThoHSimulationStatus) => void> = [];
  private pollInterval: NodeJS.Timeout | null = null;

  // Fetch ThoH simulation status from API
  private async fetchStatus(): Promise<void> {
    try {
      const response = await fetch(`${BASE_URL}/report/hand-simulation-status`);
      
      if (!response.ok) {
        throw new Error(`HTTP ${response.status}: ${response.statusText}`);
      }
      
      const data = await response.json();
      
      let status: string;
      let startTime: string | undefined;
      
      if (data.isRunning) {
        status = 'Running';
        // Convert epoch time to readable format
        if (data.epochStartTime) {
          // Handle both seconds and milliseconds epoch times
          const timestamp = data.epochStartTime > 1000000000000 
            ? data.epochStartTime 
            : data.epochStartTime * 1000;
          startTime = new Date(timestamp).toLocaleString();
        }
      } else {
        status = 'Stopped';
      }
      
      const newStatus: ThoHSimulationStatus = {
        status,
        isRunning: data.isRunning || false,
        epochStartTime: data.epochStartTime,
        startTime
      };
      
      this.currentStatus = newStatus;
      
      // Notify status listeners
      this.listeners.forEach(listener => listener(newStatus));
      
      // Schedule next poll in 30 seconds
      this.scheduleNextPoll();
    } catch (error) {
      console.error('ThoHSimulationService: Error fetching status:', error);
      
      const offlineStatus: ThoHSimulationStatus = {
        status: 'Offline',
        isRunning: false
      };
      
      this.currentStatus = offlineStatus;
      
      // Notify status listeners
      this.listeners.forEach(listener => listener(offlineStatus));
      
      // Retry in 30 seconds on error
      setTimeout(() => this.fetchStatus(), 30000);
    }
  }

  // Schedule the next poll
  private scheduleNextPoll(): void {
    if (this.pollInterval) {
      clearTimeout(this.pollInterval);
    }

    this.pollInterval = setTimeout(() => {
      this.fetchStatus();
    }, 30000); // Poll every 30 seconds
  }

  // Start the polling service
  start(): void {
    console.log('ThoHSimulationService: Starting polling service');
    this.fetchStatus(); // Initial fetch
  }

  // Stop the polling service
  stop(): void {
    console.log('ThoHSimulationService: Stopping polling service');
    if (this.pollInterval) {
      clearTimeout(this.pollInterval);
      this.pollInterval = null;
    }
  }

  // Subscribe to status updates
  onStatusUpdate(listener: (status: ThoHSimulationStatus) => void): () => void {
    this.listeners.push(listener);
    
    // If we already have status, call the listener immediately
    if (this.currentStatus) {
      listener(this.currentStatus);
    }
    
    // Return unsubscribe function
    return () => {
      const index = this.listeners.indexOf(listener);
      if (index > -1) {
        this.listeners.splice(index, 1);
      }
    };
  }

  // Get current status (synchronous)
  getCurrentStatus(): ThoHSimulationStatus | null {
    return this.currentStatus;
  }
}

// Export singleton instance
export const thoHSimulationService = new ThoHSimulationService();
