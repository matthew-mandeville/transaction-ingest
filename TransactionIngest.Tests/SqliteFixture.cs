using TransactionIngest.Data;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using System;

namespace TransactionIngest.Tests
{
    public class SqliteFixture : IDisposable
    {
        private readonly SqliteConnection _connection;
        public DbContextOptions<AppDbContext> ContextOptions { get; }

        public SqliteFixture()
        {
            _connection = new SqliteConnection("Filename=:memory:");
            _connection.Open(); // Keep connection open for persistence

            ContextOptions = new DbContextOptionsBuilder<AppDbContext>()
                .UseSqlite(_connection)
                .Options;

            // Seed the database schema and initial data
            using var context = new AppDbContext(ContextOptions);
            context.Database.EnsureCreated();
        }

        public AppDbContext CreateContext() => new AppDbContext(ContextOptions);

        public void Dispose()
        {
            _connection.Close();
            _connection.Dispose();
        }
    }
}
