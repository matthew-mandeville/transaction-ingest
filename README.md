# Transaction Ingest

TransactionIngest is a .NET 10 Console application using Entity Framework Core 
with a SQLite database to simulate ingesting transaction snapshots via a mocked API call.

## Build and Run Steps

1. Run the app (with selected snapshot transaction test data).

2. Run TransactionIngest.Tests.


## Run the App
1. Clone the repository and open the solution in Visual Studio.
1. Right click on the TransactionIngest project and select "Set as Startup Project".
    - Note: I noticed downloading the repo from Github was working differently than my local repo.  This will fix the issue.
1. To build and run the application
    - Hit F5 or 
    - Select Debug > Start Debugging or
    - Run the following command:
    ```
      dotnet run
    ```
    The SnapShotService.GetSnapShotTransactions() method returns transaction shapshot JSON.  Default data is generated at the end of the method.  Select a different mock data method to modify application runs.


## Run the Tests in Visual Studio Test Explorer
1. Open Test Explorer: In Visual Studio, go to the top menu bar and select Test > Test Explorer. 
1. Build Your Project: Ensure your test project is built by selecting Build > Build Solution. This step ensures Test Explorer discovers all tests.
1. Run Tests: In the Test Explorer window, click the Run All button. The tests will run, and the results will be displayed automatically in the window.
1. View Details: Select a specific test in the list to view its details, error messages, and console output in the details pane at the bottom of the window.


## Configuration
1. **appsettings.json:**
    - SQLite connection string for the local database.
    - Mock API URL
   
1. **Debug tracing** (via Console.WriteLines) for... 
    - Transactions
    - Audit Log
    - Status Types table

    The Program.cs Main method has 3 boolean flags to enable/disable debug tracing. By default these flags are set to false.
    - displayTransactions - tracing for transaction snapshots in Program.cs and in TransactionService.cs
    - The TransactionService Upsert method class has two additional boolean flags for tracing the audit log and status types tables.
    ```
      Upsert(string jsonSnapShot, bool displayTransactions = false, bool displayAuditLog = false, bool displayStatusTypes = false)
    ```
    - Set the display flag values in the Program.cs file to true to enable tracing for the respective tables.

 1. **Delete the SQLite database** to reset the database state for testing purposes.  Comment out the following line in the Program.cs Main method.
    ```
      db.Database.EnsureDeleted();
    ```


## Coding Approach
### Models
1. **Transaction** - represents a financial transaction
1. **StatusType** - represents a transaction status type
1. **AuditLog** - represents an audit log entry for transactions 


### Snapshot Transaction Ingest Process
The Program.cs makes calls to 2 service methods.  
1. SnapShotService.GetSnapShotTransactions() - simulates an API call to return transaction snapshot JSON data.  The method has multiple mock data methods to select from.
    - Code to return a JSON from a sample API call has been commented out.  If tracing is on the app will display the JSON, however, the TransactionService.Upsert() is not implemented to handle this JSON.

1. TransactionService.Upsert() - takes the transaction snapshot JSON and performs an upsert to the database.  The method also has optional boolean parameters to enable debug tracing for transactions, audit log, and status types tables.
    - The method is implemented to handle the JSON returned from the mock data methods in SnapShotService.GetSnapShotTransactions().
    - Only transactions with a time stamp in the last 24 hours will be processed.
    - Based on a Transaction.Id lookup, the transaction will either be inserted as a new record or updated if the transaction already exists in the database.  
    - Each transaction's card number is masked.
    - After all snapshot transactions are processed 2 additional methods are run to handle revoking and finalizing transactions.
         - RevokeTransactions() -  Updates the StatusType of all transactions less than 24 hours old and not in the snapshot to "Revoked".
         - FinalizeTransactions() - Updates the StatusType of all transactions with a time stamp greater than 24 hours old to "Finalized".


### Transaction Entity Audit Log
- Auditing takes place in the AppDbContext SaveChanges() method.  The method checks for any added or modified Transaction entities and creates a corresponding AuditLog entry for each change.
- Display of the audit log is controlled by the displayAuditLog boolean flag in the TransactionService.Upsert() method.


### Mock JSON Data
The SnapShotService.GetSnapShotTransactions() method has multiple mock data methods to select from.  By default, the GetMockSnapShotTransactions() method is selected which generates random transaction snapshot data.  To use a different set of mock data, simply change the return statement in the GetSnapShotTransactions() method to the desired mock data method.


### Assumptions
1. The API call will be mocked and will return a JSON response with transaction snapshot data.
1. The console appication will only support a single run and will not run multiple times independently.
1. The coding exercise data model does not match the sample JSON response.  I confirmed that TransactionId is an integer and will not accept alphanumeric characters.
 


