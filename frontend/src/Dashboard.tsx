import { useEffect, useState } from "react";
import DashboardCards from "./components/dashboard/DashboardCards";
import { apiClient, PeriodReport } from "./apiClient";
import RefreshButton from "./components/RefreshButton";

interface DashboardProps {
  refreshKey?: number;
  simulationStatus?: any;
}

export default function Dashboard({ refreshKey, simulationStatus }: DashboardProps) {
  const [isLoading, setIsLoading] = useState(false);
  const [data, setData] = useState<PeriodReport | null>(null);
  const [days] = useState(30);
  const [simulationDay, setSimulationDay] = useState<number | undefined>(undefined);
  const [error, setError] = useState<string | null>(null);

  const loadData = async () => {
    setIsLoading(true);
    setError(null);
    try {
      const reports = await apiClient.getPeriodReport(days);
      const latest = reports.length > 0 ? reports[reports.length - 1] : null;
      setData(latest);
    } catch (error) {
      setError('Error fetching dashboard data. Please try again.');
      setData(null);
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
      {error && (
        <div style={{ background: '#ffe0e0', color: '#a00', padding: '10px', marginBottom: '16px', borderRadius: '4px', textAlign: 'center' }}>
          {error}
        </div>
      )}
      {isLoading ? (
        <div style={{ textAlign: 'center', padding: '40px 0' }}>
          <div className="loader" style={{ margin: '0 auto', width: '40px', height: '40px', border: '4px solid #ccc', borderTop: '4px solid #333', borderRadius: '50%', animation: 'spin 1s linear infinite' }} />
          <style>{`@keyframes spin { 0% { transform: rotate(0deg); } 100% { transform: rotate(360deg); } }`}</style>
        </div>
      ) : (
        <DashboardCards
          data={data}
          isLoading={isLoading}
          currentDay={simulationDay}
          onRefresh={loadData}
          RefreshButtonComponent={RefreshButton}
        />
      )}
    </div>
  );
}
