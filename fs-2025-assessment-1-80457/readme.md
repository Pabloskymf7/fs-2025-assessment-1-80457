Dublin Bikes API (V1/V2)

This project implements a Web API for managing public bike station data (Dublin Bikes),
using ASP.NET Core and showcasing robust design patterns such as Service/Repository, API Versioning, and Caching Strategies.


1. Project Setup and Execution

1.1. Requirements

.NET 8 SDK

Visual Studio or VS Code

Azure Cosmos DB Emulator: Required for V2 functionality. (Must be installed separately).


1.2. Critical Setup for V2 (Cosmos DB)

To ensure the V2 API and the initial data seeding work correctly, you must:

Install & Start the Emulator: Download and start the Azure Cosmos DB Emulator. It should be running locally with its default settings before running dotnet run.

Verify Configuration: Ensure your appsettings.json contains the correct connection details for the emulator under the CosmosDb section.


1.3. Data Structure

The project utilizes two data layers:

V1 (Fast): In-Memory storage (InMemoryStationRepository).

V2 (Persistent): Azure Cosmos DB storage. Note: V2 data seeding and operation will fail if the Emulator is not running when the application starts.


1.4. Execution

To start the API and access the Swagger documentation:

# Navigate to the project root directory
dotnet run


The API will start on https://localhost:7001 (or similar). The Swagger documentation will be available at:
https://localhost:7001/swagger


2. Endpoints and Sample Calls (V1)

All V1 endpoints start with the /v1/ prefix.

GET

/v1/stations/summary

Retrieves aggregated metrics for the bike system.


GET

/v1/stations/{number}

Retrieves a specific station by its number.


GET

/v1/stations

Retrieves a paginated and filtered list of stations.


POST

/v1/stations

Adds a new station (only updates the in-memory repository).


PUT

/v1/stations/{number}

Updates an existing station (replaces the entire station resource).


DELETE

/v1/stations/{number}

Deletes a specific station by its number.


Example: Get Stations (Advanced)

This endpoint demonstrates the search, filtering, sorting, and pagination capabilities implemented in StationService.

Sample Call: Search for open stations with at least 5 bikes, sort by name descending, on page 2 with 10 results.

GET /v1/stations?q=docks&status=OPEN&minBikes=5&sort=name&dir=desc&page=2&pageSize=10


Example: Mutation (POST)

The request body must follow the Bike model (with properties in camelCase due to the JsonNamingPolicy configuration).

POST /v1/stations
Content-Type: application/json

{
  "number": 9999,
  "name": "NEW TEST STATION",
  "address": "Calle Falsa 123",
  "available_bikes": 10,
  "bike_stands": 10,
  "available_bike_stands": 0,
  "status": "OPEN"
}


3. Design and Architecture Notes

The project design incorporates several key patterns for scalability and testability:


1. API Versioning Strategy

The project uses the Asp.Versioning package to support multiple API versions (V1 and V2).
The version is specified via the URL segment (e.g., /v1/stations). 
This allows the data layer to evolve (from In-Memory to Cosmos DB) without breaking existing clients.


2. Service and Repository Pattern

A decoupled design is used:

IStationRepository: Defines the contract for data persistence (CRUD).

IStationService: Defines the contract for business logic (filtering, pagination, caching, summaries).

StationService implements the business logic, depending on IStationRepository for data access.


3. In-Memory Caching (V1)

The StationService uses IMemoryCache to cache the complete list of stations.

Read: The full list is read from the cache, and if it does not exist, it is fetched from the repository and cached for 5 minutes.

Write: Any mutation operation (Add, Update, Delete) automatically invalides the cache (_cache.Remove(CacheKey)), forcing a fresh data reload on the next query.


4. Asynchronous Update Service

An IHostedService (BikeUpdateService) is used to execute long-running tasks in the background. Its role is to:

Make periodic calls to an external source.

Update the in-memory repository (IStationRepository.ReplaceAll()) with the freshest data in a non-blocking manner.


5. Integration Test Configuration

The CustomWebApplicationFactory class is crucial for testability:

Mocking: It substitutes the persistence implementations (IStationRepository and ICosmosDbRepository) with a single mock implementation (InMemoryStationRepository), ensuring tests do not depend on external databases.

Determinism: It removes all hosted services (IHostedService), including the BikeUpdateService, to prevent background updates from interfering with test results.