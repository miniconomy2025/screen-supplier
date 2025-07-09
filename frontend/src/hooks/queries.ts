import { useQuery } from '@tanstack/react-query';
import { apiClient } from '../apiClient';

export function usePeriodReport(days: number) {
  return useQuery({
    queryKey: ['periodReport', days],
    queryFn: () => apiClient.getPeriodReport(days),
    refetchOnWindowFocus: false,
    retry: false,
  });
}

export function useOrdersPeriod() {
  return useQuery({
    queryKey: ['ordersPeriod'],
    queryFn: () => apiClient.getOrdersPeriod(90),
    refetchOnWindowFocus: false,
    retry: false,
  });
}

export function usePurchases() {
  return useQuery({
    queryKey: ['purchases'],
    queryFn: () => apiClient.getPurchases(),
    refetchOnWindowFocus: false,
    retry: false,
  });
}

export function useSimulationStatus(enabled: boolean = false, refreshTrigger?: number) {
  return useQuery({
    queryKey: ['simulationStatus', refreshTrigger],
    queryFn: () => apiClient.getSimulationStatus(),
    refetchInterval: enabled ? 30000 : false,
    refetchOnWindowFocus: false,
    retry: false,
  });
}
