# Library gRPC API

This project is a Library Management System built using .NET 9 and gRPC. It provides a robust API for managing books, users, and borrowing activities, leveraging MongoDB as the backend database. The solution is designed for scalability, testability, and modern cloud-native development.

## Features

- **Book Management:**
  - Retrieve all books in the library
  - Check the status of book copies (total, borrowed, available)
  - Get the most borrowed books within a time range
  - Find other books borrowed by users who borrowed a specific book

- **User Management:**
  - Track books borrowed by a user within a specific timeframe
  - Calculate reading rates for users on specific books

- **Borrowing System:**
  - Record and query borrowing and returning of books
  - Aggregate borrowing statistics for analytics

## Project Structure

- **Library-gRPC-API/**
  - Main gRPC API project
  - Contains service implementations, models, proto definitions, and utility classes

- **Extensions/**
  - Extension methods and helpers for the API

- **Models/**
  - Data models for books, users, and borrowing records

- **Protos/**
  - gRPC service and message definitions (`library.proto`)

- **Services/**
  - Core gRPC service logic (e.g., `UserLibraryService.cs`)

- **Utils/**
  - Utility classes (e.g., database seeding)

## Test Projects

The solution includes comprehensive test coverage with different types of test projects:

- **LibraryAPI.UnitTests/**
  - Unit tests for core business logic and service methods
  - Fast, isolated tests using mocks and stubs

- **LibraryAPI.FunctionalTests/**
  - Functional tests to verify end-to-end API behavior
  - Test real gRPC calls and integration with MongoDB (may use in-memory or test containers)

- **LibraryAPI.IntegrationTests/**
  - Integration tests for verifying interactions between components (e.g., database, services)
  - Ensures that the system works as expected when all parts are combined

- **LibraryAPI.SystemTests/**
  - System-level tests simulating real-world scenarios
  - Validate workflows and user stories across the entire API

## Getting Started

1. **Clone the repository:**
   ```sh
   git clone <repo-url>
   cd LibraryAPI
   ```
2. **Restore dependencies:**
   ```sh
   dotnet restore
   ```
3. **Build the solution:**
   ```sh
   dotnet build
   ```
4. **Run the API:**
   ```sh
   dotnet run --project Library-gRPC-API/Library-gRPC-API.csproj
   ```
5. **Run tests:**
   ```sh
   dotnet test
   ```

## Technologies Used

- .NET 9
- gRPC
- MongoDB
- xUnit (for testing)

## Notes

- Ensure MongoDB is running locally or update the connection string in `appsettings.json` as needed.
- The API is designed for extensibility and can be deployed in cloud or containerized environments.

## Testing the API with Postman (gRPC)

You can use Postman to test the gRPC endpoints exposed by this API. Follow these steps:

### 1. Start the API Server

First, run the API locally:

```sh
# From the project root
cd Library-gRPC-API
# Run the API (it will start on http://localhost:5266)
dotnet run
```

### 2. Create a Postman Collection

- Open Postman.
- Click **New** > **Collection** and give it a name (e.g., `Library gRPC API`).

### 3. Add a gRPC Request

- Inside your collection, click **Add Request** > **gRPC Request**.
- In the request tab:
  - For **Server**, enter: `localhost:5266`
  - Click **Select Proto File** and choose the `library.proto` file from the `Library-gRPC-API/Protos/` directory.
  - Select the desired service and method (e.g., `LibraryService/GetMostBorrowedBooks`).

### 4. Example Requests

Below are example request bodies for various endpoints. Paste these into the request body in Postman after selecting the appropriate method.

#### GetMostBorrowedBooks
```
{
  "start_time": { "seconds": 1742993208, "nanos": 0 },
  "end_time":   { "seconds": 1748913208, "nanos": 0 }
}
```

#### GetBookCopiesStatus
```
{
    "book_id": "book3"
}
```

#### GetBooksBorrowedByUserWithinTimeFrame
```
{
  "start_time": { "seconds": 1746445301 },
  "end_time":   { "seconds": 1747309301 },
  "user_id": "user1"
}
```

#### GetOtherBooksBorrowedByUserWhoBorrowedThisBook
```
{
    "book_id": "book1"
}
```

#### GetBookReadingRate
```
{
    "book_id": "book2"
}
```

---

For more details on request/response formats, see the `library.proto` file in the `Protos` directory.
