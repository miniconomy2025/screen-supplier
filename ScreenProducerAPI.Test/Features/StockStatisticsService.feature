Feature: Stock Statistics Service

@StockStatistics
Scenario: Get material statistics when no equipment exists
  Given there is no equipment
  And the target quantities are:
    | Material | Target | ReorderPoint | OrderQuantity |
    | Sand     | 50     | 0            | 100           |
    | Copper   | 20     | 0            | 50            |
  And the stock management options have logistics lead time of 5 days
  When I retrieve the material statistics
  Then the daily consumption for sand should be 0
  And the reorder point for sand should be 50
  And the daily consumption for copper should be 0
  And the reorder point for copper should be 20

@StockStatistics
Scenario: Get material statistics when equipment exists
  Given there are 2 pieces of equipment
  And the equipment parameters are:
    | InputSandKg | InputCopperKg | OutputScreens |
    | 10          | 5             | 50            |
  And the target quantities are:
    | Material | Target | ReorderPoint | OrderQuantity |
    | Sand     | 50     | 0            | 100           |
    | Copper   | 20     | 0            | 50            |
  And the stock management options have logistics lead time of 5 days
  When I retrieve the material statistics
  Then the daily consumption for sand should be 20
  And the reorder point for sand should be 150
  And the daily consumption for copper should be 10
  And the reorder point for copper should be 70
