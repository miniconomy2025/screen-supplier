import { useSimulationStatus } from "./hooks/useSimulation";

function SimulationStatus() {
  const status = useSimulationStatus();

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
            <b style={{ fontVariantNumeric: 'tabular-nums', color: '#1976d2' }}>{status.timeUntilNextDay}</b>
          </div>
        </>
      ) : <span>Loading...</span>}
    </div>
  );
}

export default SimulationStatus;
