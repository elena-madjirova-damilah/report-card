# Wonde API Aggregator

This repository contains a minimal .NET 7 web API which integrates with the [Wonde API](https://docs.wonde.com/docs/api/sync/).

The service exposes an endpoint `/aggregate` which performs the following actions:

1. Requests staff information including roles and classes.
2. For every staff member, requests the classes they teach including students.
3. Requests all classes that have students.
4. For each student discovered, requests details including attendance summaries, results and behaviours.
5. Aggregates all the responses and writes them to `aggregated_results.json` in the application directory.

The project files reside in the `WondeApiAggregator` directory.

## Building and Running

This project targets **.NET 7**. Use the .NET 7 SDK to build and run:

```bash
cd WondeApiAggregator
# Build
 dotnet build
# Run
 dotnet run
```

Once running, navigate to `http://localhost:5000/aggregate` to trigger the aggregation process. The endpoint returns the aggregated JSON and also stores it on disk.

> **Note**: The token used for the API requests is hardcoded in `WondeService.cs`. Replace it with a valid token if necessary.
