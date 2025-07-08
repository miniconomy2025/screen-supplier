import React, { useEffect, useState, useRef } from "react";
import { apiClient } from "./apiClient";

interface SimulationStatusProps {
  onStatusRefresh?: (status: any) => void;
}

function SimulationStatus({ onStatusRefresh }: SimulationStatusProps) {
  const [status, setStatus] = useState<{
    isRunning: boolean;
    currentDay: number;
    simulationDateTime: string;
    timeUntilNextDay: string;
  } | null>(null);
  const [error, setError] = useState<string | null>(null);
  const [tickingTime, setTickingTime] = useState<string | null>(null);
  const intervalRef = useRef<NodeJS.Timeout | null>(null);
  const tickRef = useRef<NodeJS.Timeout | null>(null);

  // Helper to parse HH:MM:SS to seconds
  function parseTimeToSeconds(time: string) {
    const [h, m, s] = time.split(":").map(Number);
    return h * 3600 + m * 60 + s;
  }
  // Helper to format seconds to HH:MM:SS
  function formatSecondsToTime(secs: number) {
    const h = Math.floor(secs / 3600);
    const m = Math.floor((secs % 3600) / 60);
    const s = secs % 60;
    return [h, m, s].map(v => v.toString().padStart(2, "0")).join(":");
  }

  const fetchStatus = async () => {
    try {
      setError(null);
      const data = await apiClient.getSimulationStatus();
      setStatus(data);
      setTickingTime(data.timeUntilNextDay);
      if (onStatusRefresh) onStatusRefresh(data);
      // Clear previous tick
      if (tickRef.current) clearInterval(tickRef.current);
      // Start ticking down
      let seconds = parseTimeToSeconds(data.timeUntilNextDay);
      tickRef.current = setInterval(() => {
        seconds = Math.max(0, seconds - 1);
        setTickingTime(formatSecondsToTime(seconds));
        if (seconds === 0) {
          fetchStatus(); // Refresh status when timer hits zero
        }
      }, 1000);
    } catch (e: any) {
      setError(e.message || "Failed to fetch simulation status");
    }
  };

  useEffect(() => {
    fetchStatus();
    intervalRef.current = setInterval(fetchStatus, 30000);
    return () => {
      if (intervalRef.current) clearInterval(intervalRef.current);
      if (tickRef.current) clearInterval(tickRef.current);
    };
  }, []);

  return (
    <div style={{
      marginTop: 32,
      padding: 16,
      background: '#f0f4fa',
      borderRadius: 12,
      fontSize: 15,
      boxShadow: '0 2px 8px #0001',
      border: '1px solid #e0e7ef',
      color: '#223',
      minWidth: 0,
      maxWidth: 260,
      marginLeft: 'auto',
      marginRight: 'auto',
      marginBottom: 24,
      lineHeight: 1.7,
    }}>
      <div style={{ fontWeight: 700, fontSize: 16, marginBottom: 6, color: '#1a237e', letterSpacing: 0.2 }}>Simulation Status</div>
      {error && <div style={{ color: '#d32f2f', fontWeight: 500, marginBottom: 4 }}>{error}</div>}
      {status ? (
        <>
          <div style={{ display: 'flex', justifyContent: 'space-between' }}>
            <span>Status:</span>
            <b style={{ color: status.isRunning ? '#28a745' : '#d32f2f' }}>{status.isRunning ? 'Running' : 'Stopped'}</b>
          </div>
          <div style={{ display: 'flex', justifyContent: 'space-between' }}>
            <span>Day:</span>
            <b>{status.currentDay}</b>
          </div>
          <div style={{ display: 'flex', justifyContent: 'space-between' }}>
            <span>Date:</span>
            <b>{new Date(status.simulationDateTime).toLocaleDateString()}</b>
          </div>
          <div style={{ display: 'flex', justifyContent: 'space-between' }}>
            <span>Next day in:</span>
            <b style={{ fontVariantNumeric: 'tabular-nums', color: '#1976d2' }}>{tickingTime ?? status.timeUntilNextDay}</b>
          </div>
        </>
      ) : !error && <span>Loading...</span>}
    </div>
  );
};

export default SimulationStatus;
