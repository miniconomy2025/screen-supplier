import { useEffect, useState } from "react";
import DashboardCards from "./components/dashboard/DashboardCards";
import { usePeriodReport } from "./hooks/queries";
import RefreshButton from "./components/RefreshButton";
import { useDayChangeEffect, useSimulationStatus } from "./hooks/useSimulation";

export default function Dashboard() {
  const [days] = useState(30);
  const [error, setError] = useState<string | null>(null);
  const simulationStatus = useSimulationStatus();

  const {
    data,
    isFetching: isLoading,
    refetch,
    error: queryError,
  } = usePeriodReport(days);

  // Refetch data when simulation day changes
  useDayChangeEffect(() => {
    console.log("Dashboard: Day changed, refetching data");
    refetch();
  });

  useEffect(() => {
    if (queryError) setError('Error fetching dashboard data. Please try again.');
  }, [queryError]);

  // Manual refresh handler
  const handleRefresh = () => {
    refetch();
  };

  const latest = Array.isArray(data) && data.length > 0 ? data[data.length - 1] : null;

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
          data={latest}
          isLoading={isLoading}
          currentDay={simulationStatus?.currentDay}
          onRefresh={handleRefresh}
          RefreshButtonComponent={RefreshButton}
        />
      )}
    </div>
  );
}
