# CLAUDE.md

This file provides guidance to Claude Code (claude.ai/code) when working with code in this repository.

## Build and Test Commands

```bash
# Build the solution
dotnet build

# Run tests
dotnet test src/Tests/Tests.csproj

# Run a single test
dotnet test src/Tests/Tests.csproj --filter "FullyQualifiedName~TestClassName.TestMethodName"

# Run the web application
dotnet run --project src/HOA/HOA.csproj

# Publish for deployment
dotnet publish -c Release
```

## Architecture Overview

This is an ASP.NET Core 10.0 MVC application for managing HOA Architectural Review Board (ARB) submissions. The system tracks submissions through a multi-stage approval workflow.

### Project Structure

- `src/HOA/` - Main web application
- `src/Tests/` - xUnit test project (uses in-memory EF Core database and Moq)

### Submission Workflow States

Submissions flow through these states (defined in `Model/Submission.cs`):
1. `CommunityMgrReview` - Initial review by community manager
2. `ARBChairReview` - ARB chairman review
3. `CommitteeReview` - Board members vote
4. `ARBTallyVotes` - Votes are tallied
5. `HOALiasonReview` - HOA liaison review
6. `FinalResponse` - Response sent to submitter

Terminal states: `Approved`, `ConditionallyApproved`, `Rejected`, `MissingInformation`, `Retracted`

Special states: `CommunityMgrReturn` (returned for more info), `HOALiasonInput` (needs HOA input)

### User Roles

Defined in `Model/ApplicationUser.cs` as `RoleNames`:
- `Administrator` - Full system access
- `CommunityManager` - Initial submission processing
- `BoardChairman` - ARB chair responsibilities
- `ARBBoardMember` - Voting board member
- `HOALiaison` - HOA liaison review
- `HOABoardMember` - HOA board member

### Key Services (Dependency Injected)

- `IFileStore` - File storage abstraction (`AzureFileStore` for production, `MockFileStore` for local dev)
- `IEmailSender` - Email abstraction (`SendGridEmail` for production, `MockEmail` for local dev)

Services automatically use mock implementations when connection strings are empty in appsettings.json.

### Configuration

Key settings in `appsettings.json`:
- `SqlConnectionString` - SQL Server database
- `AzureStorageConnectionString` - Azure Blob Storage (use `UseDevelopmentStorage=true` for local Azure emulator)
- `EmailKey` - SendGrid API key
- `EmailSource` - From email address
- `EmailLinkHost` - Base URL for email links

### Key Controllers

- `SubmissionController` - Core submission workflow logic (largest controller)
- `AccountController` - User authentication and management
- `BackupController` - Data backup/restore
- `StatsController` - Reporting and statistics
