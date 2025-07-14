import { useEffect, useState } from 'react';
import { thoHSimulationService, ThoHSimulationStatus } from '../services/ThoHSimulationService';

// Hook to get current ThoH simulation status
export function useThoHSimulationStatus() {
  const [status, setStatus] = useState<ThoHSimulationStatus | null>(
    thoHSimulationService.getCurrentStatus()
  );

  useEffect(() => {
    // Subscribe to status updates
    const unsubscribe = thoHSimulationService.onStatusUpdate((newStatus) => {
      setStatus(newStatus);
    });

    return unsubscribe;
  }, []);

  return status;
}
