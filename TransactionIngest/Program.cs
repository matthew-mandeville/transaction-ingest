using TransactionIngest.Data;
using TransactionIngest.Services;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

class Program
{
    static async Task Main(string[] args)
    {
        
        var builder = new ConfigurationBuilder()
        .SetBasePath(AppContext.BaseDirectory)
        .AddJsonFile("appsettings.json", optional: true, reloadOnChange: true);

        var configuration = builder.Build();

        var services = new ServiceCollection();
        services.AddSingleton<IConfiguration>(configuration);
        services.AddDbContext<AppDbContext>(opts =>
            opts.UseSqlite(configuration.GetConnectionString("Default")));

        services.AddScoped<TransactionService>();
        services.AddScoped<SnapShotService>();

        var provider = services.BuildServiceProvider();

        // Ensure database created
        using (var scope = provider.CreateScope())
        {
            var displayTransactions = false;

            var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
            var snapShotService = scope.ServiceProvider.GetRequiredService<SnapShotService>();
            var transactionService = scope.ServiceProvider.GetRequiredService<TransactionService>();

            // setup db
            //db.Database.EnsureDeleted(); // -- comment out to reset the db
            db.Database.EnsureCreated();

            var json = await snapShotService.GetSnapShotTransactions();
                      
            if (displayTransactions)
            {
                Console.WriteLine($"\nTransactions Before Snapshot Ingest *******");
                var transactions = await transactionService.GetAllAsync();
                transactions.ForEach(l => Console.WriteLine($"{l.Id}: {l.LocationCode} - {l.CardNumber} - {l.ProductName} - {l.Amount} - {l.TimeStamp}"));

                Console.WriteLine($"\n\nSnapshot Ingest json... \n{json}\n");
                Console.WriteLine("Creating transaction(s)...");
            }

            await transactionService.Upsert(json, displayTransactions, false, false);

            if (displayTransactions)
            {
                var transactions = await transactionService.GetAllAsync();
                Console.WriteLine($"\nTotal items: {transactions.Count}");
            }
        }

        Console.WriteLine("Snapshot Ingest complete.");
    }
}
