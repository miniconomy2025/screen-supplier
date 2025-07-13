import React from "react";
import { LineChart, Line, XAxis, YAxis, CartesianGrid, Tooltip, Legend, ResponsiveContainer } from "recharts";

interface DashboardChartsProps {
  chartData: any[];
  metrics: { key: string; label: string; color: string }[];
}

const DashboardCharts: React.FC<DashboardChartsProps> = ({ chartData, metrics }) => {
  console.debug("DashboardCharts: Rendering with chartData:", chartData);
  if (chartData[chartData.length - 1]?.revenue === 0 && chartData[chartData.length - 2]?.revenue !== 0) {
    chartData.pop();
  }
    return (
      <div
        style={{
          marginBottom: 32,
        display: 'grid',
        gridTemplateColumns: 'repeat(auto-fit, minmax(320px, 1fr))',
        gap: 24,
        width: '100%',
      }}
    >
      {
        metrics.map((metric) => (
          <div
            key={metric.key}
            style={{
              background: '#fff',
              borderRadius: 8,
              padding: 16,
              boxShadow: '0 1px 4px #0001',
              marginBottom: 0,
              display: 'flex',
              flexDirection: 'column',
              alignItems: 'stretch',
            }}
          >
            <h3 style={{ marginBottom: 8 }}>{metric.label}</h3>
            <ResponsiveContainer width="100%" height={220}>
              <LineChart data={chartData} margin={{ top: 10, right: 30, left: 0, bottom: 0 }}>
                <XAxis dataKey="date" tickFormatter={() => ''} />
                <YAxis tickFormatter={(v: any) => v.toLocaleString()} />
                <Tooltip 
                  formatter={(v: any) => v.toLocaleString()} 
                  labelFormatter={(label: string) => {
                    const date = new Date(label);
                    return date.toLocaleDateString();
                  }}
                />
                <Legend />
                <Line type="monotone" dataKey={metric.key} stroke={metric.color} dot={false} />
              </LineChart>
            </ResponsiveContainer>
          </div>
        ))
      }
    </div>
  );
};

export default DashboardCharts;
