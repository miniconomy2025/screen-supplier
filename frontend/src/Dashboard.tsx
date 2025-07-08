import { useEffect, useState } from "react";
import DashboardCards from "./components/dashboard/DashboardCards";
import { apiClient, PeriodReport } from "./apiClient";

interface DashboardProps {
  refreshKey?: number;
  simulationStatus?: any;
}

export default function Dashboard({ refreshKey, simulationStatus }: DashboardProps) {
  const [isLoading, setIsLoading] = useState(false);
  const [data, setData] = useState<PeriodReport | null>(null);
  const [days] = useState(30);
  const [simulationDay, setSimulationDay] = useState<number | undefined>(undefined);

  const loadData = async () => {
    setIsLoading(true);
    try {
      const reports = await apiClient.getPeriodReport(days);
      const latest = reports.length > 0 ? reports[reports.length - 1] : null;
      setData(latest);
    } catch (error) {
      console.error('Error fetching dashboard data:', error);
    } finally {
      setIsLoading(false);
    }
  };

  useEffect(() => {
    loadData();
  }, [refreshKey, days]);

  useEffect(() => {
    if (simulationStatus && typeof simulationStatus.currentDay === 'number') {
      setSimulationDay(simulationStatus.currentDay);
    }
  }, [simulationStatus]);

  return (
    <div>
      <DashboardCards
        data={data}
        isLoading={isLoading}
        currentDay={simulationDay}
      />
    </div>
  );
}
