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

---

For more details, see the code and comments in each project folder.
