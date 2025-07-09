import { useEffect, useState } from 'react';
import { simulationService, SimulationStatus } from '../services/SimulationService';

// Hook to get current simulation status
export function useSimulationStatus() {
  const [status, setStatus] = useState<SimulationStatus | null>(
    simulationService.getCurrentStatus()
  );

  useEffect(() => {
    // Subscribe to status updates
    const unsubscribe = simulationService.onStatusUpdate((newStatus) => {
      setStatus(newStatus);
    });

    return unsubscribe;
  }, []);

  return status;
}

// Hook to listen for day changes and trigger a callback
export function useDayChangeEffect(callback: () => void) {
  useEffect(() => {
    // Subscribe to day changes
    const unsubscribe = simulationService.onDayChange((newDay) => {
      console.log(`Day changed to ${newDay}, waiting 5 seconds before triggering callback`);
      // Add 5-second delay before refetching data
      setTimeout(() => {
        console.log(`5 seconds elapsed, now triggering callback for day ${newDay}`);
        callback();
      }, 5000);
    });

    return unsubscribe;
  }, [callback]);
}
