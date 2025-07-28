# DbTracker
DbTracker is a .NET console application designed to monitor real-time changes in multiple SQL Server databases and trigger actions based on those changes. Using the SqlTableDependency library, it tracks INSERT, UPDATE, and DELETE operations on specified tables across different databases and logs these changes with precise timing. The application can also be extended to perform actions like updating other databases or calling external services when changes are detected. This makes it ideal for scenarios where you need to synchronize data or respond to database events across multiple systems, such as databases from different companies.
Features
         
#### Real-Time Monitoring: Tracks row-level changes (INSERT, UPDATE, DELETE) in SQL Server tables using SqlTableDependency.
#### Multi-Database Support: Monitors multiple databases simultaneously, with independent event handling for each.
#### Event-Driven Actions: Triggers custom actions (e.g., logging, syncing to another database, or calling external services) when changes occur.
#### Excel Import Compatibility: Detects changes from external sources, such as Excel data imports via SQL Server Management Studio (SSMS) or SSIS.
#### Precise Timing: Logs change events with millisecond precision for performance analysis.
#### Error Handling: Captures and logs errors during monitoring for robust operation.

## Prerequisites

.NET 8.0 SDK or later
SQL Server (e.g., LocalDB, Express, or full SQL Server instance)
NuGet Packages:
TableDependency.SqlClient (for change tracking)
Microsoft.EntityFrameworkCore.SqlServer (for database initialization)


SQL Server Management Studio (SSMS) (optional, for testing manual changes or Excel imports)

## Installation
```
Clone the Repository:
git clone https://github.com/mohisa302/DbTracker.git
cd DbTracker
```

Restore NuGet Packages:
dotnet restore


## Set Up Databases:

Ensure SQL Server is running (e.g., (localdb)\MSSQLLocalDB).
The application automatically creates two databases (FirstDB and SecondDB) with a Students table on first run.
Verify the connection strings in Program.cs match your SQL Server instance:const string DB1_Conn = @"Server=(localdb)\MSSQLLocalDB;Database=FirstDB;Integrated Security=True;";
const string DB2_Conn = @"Server=(localdb)\MSSQLLocalDB;Database=SecondDB;Integrated Security=True;";



```
Build and Run:
dotnet build
dotnet run
```


## Usage

Running the Application:

The app initializes two databases (FirstDB and SecondDB) and sets up monitoring for the Students table in each.
It performs a test scenario with INSERT and UPDATE operations to demonstrate change detection.
The console logs changes with timestamps, including the change type (Insert, Update, Delete) and affected row data.


## Testing with SSMS:

Open SSMS and connect to your SQL Server instance.
Run SQL commands on the Students table in FirstDB or SecondDB, e.g.:
```
INSERT INTO FirstDB.dbo.Students (Name) VALUES ('SSMS Test');
UPDATE FirstDB.dbo.Students SET Name = 'SSMS Updated' WHERE Id = 1;

```
The app will log these changes in real-time:
```
[13:36:12.345] DB1 Change: Insert | ID: 2 | Name: SSMS Test
[13:36:13.456] DB1 Change: Update | ID: 1 | Name: SSMS Updated
```



Testing with Excel Imports:

Create an Excel file (students.xlsx) with a Name column (e.g., Excel Student 1, Excel Student 2).
Use SSMS’s Import Data Wizard (Tasks → Import Data) to import the file into the Students table of FirstDB or SecondDB.
The app will detect and log each inserted row:[13:36:14.567] DB1 Change: Insert | ID: 3 | Name: Excel Student 1




Extending Functionality:

Modify the OnChanged event handler in Program.cs to add custom actions, such as:
Syncing changes to another database.
Calling an external API using HttpClient.


Example:
```
tracker.OnChanged += async (sender, e) =>
{
    Console.WriteLine($"[{DateTime.Now:HH:mm:ss.fff}] {dbName} Change: {e.ChangeType} | ID: {e.Entity.Id} | Name: {e.Entity.Name}");
    // Example: Sync to another database
    await ExecuteSqlAsync(DB2_Conn, $"INSERT INTO Students (Name) VALUES ('{e.Entity.Name}')");
};
```




## Project Structure

Program.cs: Main application logic, including database initialization, SqlTableDependency setup, and test scenarios.
Student.cs: Entity class representing the Students table with Id (primary key) and Name properties.
StorageBroker.cs: EF Core DbContext for database initialization and management.

## Dependencies

TableDependency.SqlClient: Monitors table changes using SQL Server Service Broker.
Microsoft.EntityFrameworkCore.SqlServer: Handles database creation and initialization.

## Troubleshooting

Permission Issues: Ensure Service Broker is enabled (ALTER DATABASE [DatabaseName] SET ENABLE_BROKER;) and the user has permissions to create triggers and queues.
Excel Import Issues: Verify that imports use standard INSERT operations (avoid BULK INSERT with FIRE_TRIGGERS = FALSE).
Errors in Console: Check the OnError event logs for details:tracker.OnError += (sender, e) => Console.WriteLine($"[{DateTime.Now:HH:mm:ss.fff}] {dbName} Error: {e.Message}");



## Contributing
Contributions are welcome! Please submit issues or pull requests to the repository. Ensure code follows the existing style and includes tests where applicable.
License
This project is licensed under the MIT License. See the LICENSE file for details.
Contact
