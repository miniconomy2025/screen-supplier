import { BASE_URL } from "./config";

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
  async getPurchases(): Promise<any[]> {
    const response = await fetch(`${BASE_URL}/report/purchases`);
    if (!response.ok) {
      throw new Error(`HTTP ${response.status}: ${response.statusText}`);
    }
    return response.json();
  },
};
