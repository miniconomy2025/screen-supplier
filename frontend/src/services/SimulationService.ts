import { apiClient } from '../apiClient';

export interface SimulationStatus {
  currentDay: number;
  simulationDateTime: string;
  timeUntilNextDay: string;
  isRunning: boolean;
}

class SimulationService {
  private currentStatus: SimulationStatus | null = null;
  private listeners: Array<(status: SimulationStatus) => void> = [];
  private dayChangeListeners: Array<(newDay: number) => void> = [];
  private pollInterval: NodeJS.Timeout | null = null;
  private tickingInterval: NodeJS.Timeout | null = null;
  private localTickingTime: string | null = null;

  // Convert time string (HH:MM:SS) to total seconds
  private timeToSeconds(timeStr: string): number {
    const [hours, minutes, seconds] = timeStr.split(':').map(Number);
    return hours * 3600 + minutes * 60 + seconds;
  }

  // Convert seconds to time string (HH:MM:SS)
  private secondsToTimeString(totalSeconds: number): string {
    const hours = Math.floor(totalSeconds / 3600);
    const minutes = Math.floor((totalSeconds % 3600) / 60);
    const seconds = totalSeconds % 60;
    return `${hours.toString().padStart(2, '0')}:${minutes.toString().padStart(2, '0')}:${seconds.toString().padStart(2, '0')}`;
  }

  // Start local ticking timer
  private startLocalTicking(): void {
    if (this.tickingInterval) {
      clearInterval(this.tickingInterval);
    }

    if (!this.currentStatus) return;

    let secondsRemaining = this.timeToSeconds(this.currentStatus.timeUntilNextDay);
    console.log(`SimulationService: Starting local countdown from ${this.currentStatus.timeUntilNextDay}`);
    
    this.tickingInterval = setInterval(() => {
      if (secondsRemaining > 0) {
        secondsRemaining--;
        this.localTickingTime = this.secondsToTimeString(secondsRemaining);
        
        // Notify listeners with updated ticking time
        this.listeners.forEach(listener => listener({
          ...this.currentStatus!,
          timeUntilNextDay: this.localTickingTime!
        }));
      } else {
        // Timer reached zero, stop ticking
        this.localTickingTime = "00:00:00";
        if (this.tickingInterval) {
          clearInterval(this.tickingInterval);
          this.tickingInterval = null;
        }
      }
    }, 1000);
  }

  // Stop local ticking timer
  private stopLocalTicking(): void {
    if (this.tickingInterval) {
      clearInterval(this.tickingInterval);
      this.tickingInterval = null;
    }
  }

  // Calculate next aligned poll time
  private getNextPollDelay(): number {
    if (!this.currentStatus) return 30000; // Default 30 seconds

    const secondsUntilNextDay = this.timeToSeconds(this.currentStatus.timeUntilNextDay);
    
    // If less than 30 seconds until next day, wait until after the day change
    if (secondsUntilNextDay < 30) {
      return (secondsUntilNextDay + 5) * 1000; // Wait 5 seconds after day change
    }

    // Calculate alignment: find the next 30-second boundary
    const remainder = secondsUntilNextDay % 30;
    const alignmentDelay = remainder === 0 ? 30 : remainder;
    
    console.log(`SimulationService: Next poll in ${alignmentDelay}s (${secondsUntilNextDay}s until next day)`);
    return alignmentDelay * 1000;
  }

  // Fetch simulation status from API
  private async fetchStatus(): Promise<void> {
    try {
      const newStatus = await apiClient.getSimulationStatus();
      const previousDay = this.currentStatus?.currentDay;
      
      this.currentStatus = newStatus;
      
      // Sync local ticking time with server response
      this.localTickingTime = newStatus.timeUntilNextDay;
      console.log(`SimulationService: Synced local timer to server time: ${newStatus.timeUntilNextDay}`);
      
      // Notify status listeners with fresh server data
      this.listeners.forEach(listener => listener(newStatus));
      
      // Check for day change
      if (previousDay !== undefined && newStatus.currentDay !== previousDay) {
        console.log(`SimulationService: Day changed from ${previousDay} to ${newStatus.currentDay}`);
        this.dayChangeListeners.forEach(listener => listener(newStatus.currentDay));
      }
      
      // Start/restart local ticking timer if simulation is running
      if (newStatus.isRunning) {
        this.startLocalTicking();
      } else {
        this.stopLocalTicking();
      }
      
      // Schedule next poll with alignment
      this.scheduleNextPoll();
    } catch (error) {
      console.error('SimulationService: Error fetching status:', error);
      // Retry in 5 seconds on error
      setTimeout(() => this.fetchStatus(), 5000);
    }
  }

  // Schedule the next poll with proper alignment
  private scheduleNextPoll(): void {
    if (this.pollInterval) {
      clearTimeout(this.pollInterval);
    }

    const delay = this.getNextPollDelay();
    this.pollInterval = setTimeout(() => {
      this.fetchStatus();
    }, delay);
  }

  // Start the polling service
  start(): void {
    console.log('SimulationService: Starting polling service');
    this.fetchStatus(); // Initial fetch
  }

  // Stop the polling service
  stop(): void {
    console.log('SimulationService: Stopping polling service');
    if (this.pollInterval) {
      clearTimeout(this.pollInterval);
      this.pollInterval = null;
    }
    this.stopLocalTicking();
  }

  // Subscribe to status updates
  onStatusUpdate(listener: (status: SimulationStatus) => void): () => void {
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

  // Subscribe to day change events
  onDayChange(listener: (newDay: number) => void): () => void {
    this.dayChangeListeners.push(listener);
    
    // Return unsubscribe function
    return () => {
      const index = this.dayChangeListeners.indexOf(listener);
      if (index > -1) {
        this.dayChangeListeners.splice(index, 1);
      }
    };
  }

  // Get current status (synchronous)
  getCurrentStatus(): SimulationStatus | null {
    return this.currentStatus;
  }
}

// Export singleton instance
export const simulationService = new SimulationService();
