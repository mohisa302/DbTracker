using Microsoft.EntityFrameworkCore;
using System;
using System.Data.SqlClient;
using System.Diagnostics;
using System.Threading.Tasks;
using TableDependency.SqlClient;

namespace DatabaseTracker
{
    public class Student
    {
        public int Id { get; set; }
        public string Name { get; set; }
    }

    public class StorageBroker : DbContext
    {
        private readonly string connectionString;

        public StorageBroker(string connectionString)
        {
            this.connectionString = connectionString;
        }

        protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder) =>
            optionsBuilder.UseSqlServer(connectionString);

        public DbSet<Student> Students { get; set; }
    }

    class Program
    {
        const string DB1_Conn = @"Server=(localdb)\MSSQLLocalDB;Database=FirstDB;Integrated Security=True;";
        const string DB2_Conn = @"Server=(localdb)\MSSQLLocalDB;Database=SecondDB;Integrated Security=True;";

        static SqlTableDependency<Student> _db1Tracker;
        static SqlTableDependency<Student> _db2Tracker;

        static async Task Main(string[] args)
        {
            Console.WriteLine("=== DATABASE CHANGE TRACKER STARTED ===");
            Console.WriteLine("Initializing databases...\n");

            // Setup databases
            await InitializeDatabases();

            // Start monitoring both databases
            StartMonitoring(DB1_Conn, "DB1");
            StartMonitoring(DB2_Conn, "DB2");

            // Test sequence
            await TestScenario();

            // Keep monitoring for SSMS/Excel changes
            Console.WriteLine("\nMonitoring for SSMS/Excel changes. Press any key to exit...");
            Console.ReadKey();

            // Cleanup
            _db1Tracker?.Stop();
            _db2Tracker?.Stop();
        }

        static void StartMonitoring(string connectionString, string dbName)
        {
            var tracker = new SqlTableDependency<Student>(connectionString, "Students");
            tracker.OnChanged += (sender, e) =>
            {
                var timing = $"{DateTime.Now:HH:mm:ss.fff}";
                Console.WriteLine($"[{timing}] {dbName} Change: {e.ChangeType} | ID: {e.Entity.Id} | Name: {e.Entity.Name}");
            };
            tracker.OnError += (sender, e) =>
            {
                Console.WriteLine($"[{DateTime.Now:HH:mm:ss.fff}] {dbName} Error: {e.Message}");
            };
            tracker.Start();
            Console.WriteLine($"🔔 Monitoring STARTED for {dbName}");

            if (dbName == "DB1") _db1Tracker = tracker;
            else _db2Tracker = tracker;
        }

        static async Task InitializeDatabases()
        {
            using var db1 = new StorageBroker(DB1_Conn);
            using var db2 = new StorageBroker(DB2_Conn);
            await db1.Database.EnsureCreatedAsync();
            await db2.Database.EnsureCreatedAsync();

            // Clear existing data using raw SQL to avoid trigger conflicts
            await ExecuteSqlAsync(DB1_Conn, "DELETE FROM Students");
            await ExecuteSqlAsync(DB2_Conn, "DELETE FROM Students");
        }

        static async Task TestScenario()
        {
            // Simulate Excel data insertion using raw SQL
            Console.WriteLine("\n=== TESTING DB1 CHANGES ===");
            var stopwatch = Stopwatch.StartNew();
            await ExecuteSqlAsync(DB1_Conn, "INSERT INTO Students (Name) VALUES ('Alice (Excel Row 1)')");
            Console.WriteLine($"[{DateTime.Now:HH:mm:ss.fff}] Executed DB1 Insert ({stopwatch.ElapsedMilliseconds}ms)");

            await Task.Delay(1000);

            Console.WriteLine("\n=== TESTING DB2 CHANGES ===");
            stopwatch.Restart();
            await ExecuteSqlAsync(DB2_Conn, "INSERT INTO Students (Name) VALUES ('Bob (Excel Row 1)')");
            Console.WriteLine($"[{DateTime.Now:HH:mm:ss.fff}] Executed DB2 Insert ({stopwatch.ElapsedMilliseconds}ms)");

            await Task.Delay(1000);

            Console.WriteLine("\n=== TESTING CONCURRENT CHANGES ===");
            stopwatch.Restart();
            await Task.WhenAll(
                ExecuteSqlAsync(DB1_Conn, "UPDATE Students SET Name = 'Alice Updated' WHERE Id = 1"),
                ExecuteSqlAsync(DB2_Conn, "UPDATE Students SET Name = 'Bob Updated' WHERE Id = 1")
            );
            Console.WriteLine($"[{DateTime.Now:HH:mm:ss.fff}] Executed Concurrent Updates ({stopwatch.ElapsedMilliseconds}ms)");
        }

        static async Task ExecuteSqlAsync(string connectionString, string sql)
        {
            using var conn = new SqlConnection(connectionString);
            await conn.OpenAsync();
            using var cmd = new SqlCommand(sql, conn);
            await cmd.ExecuteNonQueryAsync();
        }
    }
}