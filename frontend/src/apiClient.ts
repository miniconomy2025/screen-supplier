export interface PeriodReport {
  date: string;
  sandStock: number;
  copperStock: number;
  sandPurchased: number;
  copperPurchased: number;
  sandConsumed: number;
  copperConsumed: number;
  screensProduced: number;
  workingMachines: number;
  screensSold: number;
  revenue: number;
}

export interface ScreenOrder {
  id: number;
  quantity: number;
  orderDate: string;
  unitPrice: number;
  orderStatusId: number;
  orderStatus: {
    id: number;
    name: string;
  };
  productId: number;
  product: {
    id: number;
    name: string;
  };
  amountPaid?: number;
}

const BASE_URL = 'https://localhost:7074';

export const apiClient = {
  async getPeriodReport(pastDaysToInclude: number): Promise<PeriodReport[]> {
    const response = await fetch(`${BASE_URL}/report/period?pastDaysToInclude=${pastDaysToInclude}`);
    
    if (!response.ok) {
      throw new Error(`HTTP ${response.status}: ${response.statusText}`);
    }
    
    const data = await response.json();
    console.debug('Period report data:', data);
    
    return data;
  },
  async getOrdersPeriod(pastDaysToInclude: number): Promise<ScreenOrder[]> {
    const response = await fetch(`${BASE_URL}/order/period?pastDaysToInclude=${pastDaysToInclude}`);
    if (!response.ok) {
      throw new Error(`HTTP ${response.status}: ${response.statusText}`);
    }
    const data = await response.json();
    return data;
  },
  async getSimulationStatus(): Promise<{
    isRunning: boolean;
    currentDay: number;
    simulationDateTime: string;
    timeUntilNextDay: string;
  }> {
    const response = await fetch(`${BASE_URL}/simulation`);
    if (!response.ok) {
      throw new Error(`HTTP ${response.status}: ${response.statusText}`);
    }
    return await response.json();
  },
  async getScreensHistory(): Promise<{ screens: { quantity: number; price: number }, date: string }[]> {
    // Fetch simulation status to get the simulation date
    const simStatusResp = await fetch(`${BASE_URL}/simulation`);
    if (!simStatusResp.ok) {
      throw new Error(`HTTP ${simStatusResp.status}: ${simStatusResp.statusText}`);
    }
    const simStatus = await simStatusResp.json();
    const simDate = simStatus.simulationDateTime ? simStatus.simulationDateTime.slice(0, 10) : new Date().toISOString().slice(0, 10);

    const response = await fetch(`${BASE_URL}/screens`);
    if (!response.ok) {
      throw new Error(`HTTP ${response.status}: ${response.statusText}`);
    }
    const result = await response.json();
    // Assume result is [{ screens: { quantity, price } }]
    const entry = { ...result[0], date: simDate };
    // Load previous
    let history: { screens: { quantity: number; price: number }, date: string }[] = [];
    try {
      const raw = localStorage.getItem('screensHistory');
      if (raw) history = JSON.parse(raw);
    } catch {}
    // Remove any entry for this simulation date
    history = history.filter(e => e.date !== simDate);
    // Add new entry
    history.push(entry);
    // Keep only last 90
    if (history.length > 90) history = history.slice(history.length - 90);
    // Save
    localStorage.setItem('screensHistory', JSON.stringify(history));
    return history;
  },
};
