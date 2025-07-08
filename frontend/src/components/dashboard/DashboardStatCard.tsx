import React, { useEffect, useState } from "react";

interface DashboardStatCardProps {
  title: string;
  value: React.ReactNode;
  icon: React.ReactNode;
  description: string;
  sublabel?: React.ReactNode;
  valueColor?: string;
}

const DashboardStatCard: React.FC<DashboardStatCardProps> = ({
  title,
  value,
  icon,
  description,
  sublabel,
  valueColor,
}) => (
  <div
    style={{
      background: '#fff',
      borderRadius: 8,
      padding: 16,
      boxShadow: '0 1px 4px #0001',
      marginBottom: 0,
      display: 'flex',
      gap: 16,
      flexDirection: 'column',
      alignItems: 'stretch',
    }}
  >
    <div style={{ display: 'flex', alignItems: 'center', gap: 8 }}>
      <h3 style={{ margin: 0 }}>{title}</h3>
      {icon}
    </div>
    <div style={{ fontSize: 24, fontWeight: 600, color: valueColor || '#222' }}>{value}</div>
    <p style={{ color: '#666', margin: 0 }}>{description}</p>
    {sublabel && <p style={{ margin: 0 }}>{sublabel}</p>}
  </div>
);

// --- SimulationStatus component ---
function SimulationStatus() {
  const [status, setStatus] = useState<{
    isRunning: boolean;
    currentDay: number;
    simulationDateTime: string;
    timeUntilNextDay: string;
  } | null>(null);
  const [error, setError] = useState<string | null>(null);

  const fetchStatus = async () => {
    try {
      setError(null);
      const res = await fetch("/simulation");
      if (!res.ok) throw new Error(`HTTP ${res.status}`);
      const data = await res.json();
      setStatus(data);
    } catch (e: any) {
      setError(e.message || "Failed to fetch simulation status");
    }
  };

  useEffect(() => {
    fetchStatus();
    const interval = setInterval(fetchStatus, 30000);
    return () => clearInterval(interval);
  }, []);

  return (
    <div style={{ marginTop: 32, padding: 12, background: '#f5f5f5', borderRadius: 8, fontSize: 15 }}>
      <strong>Simulation Status:</strong><br />
      {error && <span style={{ color: '#d32f2f' }}>{error}</span>}
      {status ? (
        <>
          <div>Status: <b style={{ color: status.isRunning ? '#28a745' : '#d32f2f' }}>{status.isRunning ? 'Running' : 'Stopped'}</b></div>
          <div>Current Day: <b>{status.currentDay}</b></div>
          <div>Date: <b>{new Date(status.simulationDateTime).toLocaleDateString()}</b></div>
          <div>Time until next day: <b>{status.timeUntilNextDay}</b></div>
        </>
      ) : !error && <span>Loading...</span>}
    </div>
  );
}

export default DashboardStatCard;
