Feature: Reporting Service

@ReportingService
Scenario: Get daily report creates production history when none exists
	Given the simulation time is "2025-01-16 14:00:00"
	And there is no production history for date "2025-01-16 14:00:00"
	And there are no purchase orders for date "2025-01-16 14:00:00"
	And there are no screen orders for date "2025-01-16 14:00:00"
	And there is equipment with sand input 10, copper input 5, and screen output 10
	When I get daily report for date "2025-01-16 14:00:00"
	Then the daily report should be generated successfully
	And the report should have date "2025-01-16 14:00:00"
	And the report should have sand stock 0
	And the report should have copper stock 0
	And the report should have sand purchased 0
	And the report should have copper purchased 0
	And the report should have screens produced 0

@ReportingService
Scenario: Get daily report handles exception and returns null
	Given the simulation time is "2025-01-17 09:00:00"
	And the production history service will throw an exception
	When I get daily report for date "2025-01-17 09:00:00"
	Then no daily report should be returned

@ReportingService
Scenario: Get daily report handles zero screens produced
	Given the simulation time is "2025-01-19 08:00:00"
	And there is production history for date "2025-01-19 08:00:00" with sand stock 100, copper stock 50, screens produced 0, screen stock 100, screen price 500, working equipment 2
	And there is equipment with sand input 10, copper input 5, and screen output 10
	When I get daily report for date "2025-01-19 08:00:00"
	Then the daily report should be generated successfully
	And the report should have sand consumed 0
	And the report should have copper consumed 0

@ReportingService
Scenario Outline: Get last period reports for multiple days
	Given the simulation time is "2025-01-20 15:00:00"
	And there are daily reports for the last <pastDays> days from "2025-01-20 15:00:00"
	When I get last period reports for <pastDays> days from "2025-01-20 15:00:00"
	Then <pastDays> daily reports should be returned
	And the reports should be ordered by date ascending

Examples:
	| pastDays |
	| 1        |
	| 3        |
	| 7        |

@ReportingService
Scenario: Get last period reports excludes null reports
	Given the simulation time is "2025-01-21 10:00:00"
	And there are some null daily reports in the last 5 days from "2025-01-21 10:00:00"
	When I get last period reports for 5 days from "2025-01-21 10:00:00"
	Then only non-null daily reports should be returned
	And the reports should be ordered by date ascending

@ReportingService
Scenario: Get daily report with no equipment
	Given the simulation time is "2025-01-23 11:15:00"
	And there is production history for date "2025-01-23 11:15:00" with sand stock 50, copper stock 25, screens produced 10, screen stock 90, screen price 550, working equipment 0
	And there is no equipment available
	When I get daily report for date "2025-01-23 11:15:00"
	Then the daily report should be generated successfully
	And the report should have working machines 0
	And the report should have sand consumed 0
	And the report should have copper consumed 0