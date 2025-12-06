# ?? Dublin Bikes Client (Blazor Master/Detail)

This project is the client application built with **Blazor Server** to consume
the **Dublin Bikes Station API** (developed in Assignment 1).
It provides a complete, interactive user interface
for viewing, searching, filtering, sorting, and managing the station 
data using full **CRUD** (Create, Read, Update, Delete) capabilities.

## ? Core Features & Functionality

This Blazor application implements a rich set of features to interact with the API:

| Feature | Implementation Details |
| :--- | :--- |
| **Master/Detail View** | The main list (`/stations`) provides a master list of stations. Selecting a row displays a rich detail panel (`StationDetail.razor`) below. |
| **CRUD Operations** | Implemented using the reusable `StationForm.razor` component for both **Create** (`POST`) and **Edit** (`PUT`). Stations can be **Deleted** (`DELETE`) directly from the list using a confirmation dialog. |
| **Paging & Sorting** | **Paging** is implemented at the bottom of the list for navigation. **Sorting** is available by **Name** and **Available Bikes** (Bikes). The current sort field and direction (ascending/descending) are clearly indicated in the table headers. |
| **Search** | A text input field allows for live **searching** across the station **Name** and **Address**. |
| **Filters** | Multiple filters can be applied simultaneously: **Status Filter** (`OPEN` / `CLOSED`) and **Minimum Bikes Filter** (a numeric input to show stations with at least that many bikes available). |
| **API Client** | The `StationsApiClient.cs` service handles all communication, mapping UI parameters (page, search, sort, filters) to the correct V2 API endpoints (`/api/v2/stations/search`). |

***

## ??? Configuration and Running the Project

To successfully run this Blazor Client, you must have the backend API from Assignment 1 running first.

### Prerequisites

* **.NET SDK:** .NET 8 or later.
* **API Service:** The Dublin Bikes API backend must be running (e.g., typically on `https://localhost:5041`).

### 1. How to Configure the API Base URL

The application uses an `HttpClient` injected into the `StationsApiClient` to communicate with the backend. You must ensure the `BaseAddress` is correct.

1.  Open the **`Program.cs`** file in the root of the Blazor Client project.
2.  Locate the service registration for `IStationsApiClient` and verify the `BaseAddress`:

    ```csharp
    // Program.cs
    builder.Services.AddHttpClient<IStationsApiClient, StationsApiClient>(client =>
    {
        // ?? IMPORTANT: SET YOUR API'S BASE ADDRESS HERE ??
        client.BaseAddress = new Uri("https://localhost:7001/"); 
    });
    ```

3.  If your API is running on a different port or domain, change the `https://localhost:5041/` value accordingly.

### 2. How to Run the Blazor Project

1.  **Start the API:** Ensure the backend API project is running (e.g., by launching it from Visual Studio or the command line).
2.  **Start the Client:**
    * **Visual Studio:** Right-click the Blazor Client project and select **Debug > Start new instance**.
    * **Command Line:** Navigate to the client project directory and run:

    ```bash
    dotnet run
    ```

3.  The application will launch in your browser (usually at `https://localhost:port`). Navigate to the `/stations` route to see the main interface.
