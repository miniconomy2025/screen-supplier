import React from "react";

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

export default DashboardStatCard;
