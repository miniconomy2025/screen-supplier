import React from "react";
import { LineChart, Line, XAxis, YAxis, CartesianGrid, Tooltip, Legend, ResponsiveContainer } from "recharts";

interface ProfitLossData {
  date: string;
  revenue: number;
  costs: number;
  profit: number;
}

interface ProfitLossChartProps {
  data: ProfitLossData[];
}

const ProfitLossChart: React.FC<ProfitLossChartProps> = React.memo(({ data }) => {
  return (
    <div
      style={{
        background: '#fff',
        borderRadius: 8,
        padding: 16,
        boxShadow: '0 1px 4px #0001',
        marginBottom: 24,
      }}
    >
      <h3 style={{ marginBottom: 16, fontSize: 18, fontWeight: 600 }}>Daily Profit/Loss</h3>
      <ResponsiveContainer width="100%" height={300}>
        <LineChart
          width={500}
          height={300}
          data={data}
          margin={{
            top: 5,
            right: 30,
            left: 20,
            bottom: 5,
          }}
        >
          <CartesianGrid strokeDasharray="3 3" />
          <XAxis 
            dataKey="date" 
            tickFormatter={(value) => {
              const date = new Date(value);
              return `${date.getMonth() + 1}/${date.getDate()}`;
            }}
          />
          <YAxis tickFormatter={(v: any) => v.toLocaleString()} />
          <Tooltip 
            formatter={(value: any, name: string) => {
              let label = name;
              if (name === 'revenue') label = 'Revenue';
              else if (name === 'costs') label = 'Costs';
              else if (name === 'profit') label = 'Profit';
              
              return [`Ð${value.toLocaleString()}`, label];
            }}
            labelFormatter={(label: string) => {
              const date = new Date(label);
              return date.toLocaleDateString();
            }}
          />
          <Legend />
          <Line type="monotone" dataKey="profit" stroke="#007bff" />
        </LineChart>
      </ResponsiveContainer>
    </div>
  );
});

export default ProfitLossChart;
