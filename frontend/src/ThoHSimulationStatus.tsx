import { useThoHSimulationStatus } from "./hooks/useThoHSimulation";

function ThoHSimulationStatus() {
  const status = useThoHSimulationStatus();

  return (
    <div style={{
      marginTop: 16,
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
      <div style={{ fontWeight: 700, fontSize: 16, marginBottom: 6, color: '#1a237e', letterSpacing: 0.2 }}>ThoH Simulation</div>
      {status ? (
        <>
          <div style={{ display: 'flex', justifyContent: 'space-between' }}>
            <span>Status:</span>
            <b style={{ color: status.isRunning ? '#28a745' : status.status === 'Offline' ? '#d32f2f' : '#d32f2f' }}>{status.status}</b>
          </div>
          {status.isRunning && status.startTime && (
            <div style={{ display: 'flex', justifyContent: 'space-between' }}>
              <span>Started:</span>
              <b style={{ fontSize: '13px' }}>{status.startTime}</b>
            </div>
          )}
        </>
      ) : <span>Loading...</span>}
    </div>
  );
}

export default ThoHSimulationStatus;
