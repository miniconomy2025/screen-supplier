Feature: Queue Processing Background Service
  The Queue Processing Background Service runs continuously to process purchase order queues
  according to the configured interval and handles exceptions gracefully.

  @Queue
  Scenario: Background service starts and populates the queue
    Given a queue processing interval of 1 seconds
    When the background service starts
    Then PopulateQueueFromDatabaseAsync should be called once

  @Queue
  Scenario: Background service processes queue multiple times
    Given a queue processing interval of 1 seconds
    When the background service starts
    And the background service runs for 2500 milliseconds
    And I stop the background service
    Then ProcessQueueAsync should be called at least 2 times

  @Queue
  Scenario: Background service handles exceptions and continues running
    Given a queue processing interval of 1 seconds
    When the background service starts
    And the background service runs for 2500 milliseconds
    And I stop the background service
    Then ProcessQueueAsync should handle exceptions and continue running