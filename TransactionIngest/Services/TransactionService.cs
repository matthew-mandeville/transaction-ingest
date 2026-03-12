using TransactionIngest.Data;
using TransactionIngest.Models;
using Microsoft.EntityFrameworkCore;

namespace TransactionIngest.Services
{
    public class TransactionService
    {
        private readonly AppDbContext _context;

        public TransactionService(AppDbContext db)
        {
            _context = db;
        }

        public async Task<Transaction> CreateAsync(Transaction transaction)
        {
            _context.Transactions.Add(transaction);
            await _context.SaveChangesAsync();
            return transaction;
        }

        public async Task<Transaction?> GetAsync(int id)
        {
            return await _context.Transactions.FindAsync(id);
        }

        public async Task<List<Transaction>> GetAllAsync()
        {
            return await _context.Transactions.AsNoTracking().ToListAsync();
        }

        public async Task<bool> UpdateAsync(Transaction transaction)
        {
            var exists = await _context.Transactions.AnyAsync(x => x.Id == transaction.Id);
            if (!exists) return false;
            _context.Transactions.Update(transaction);
            await _context.SaveChangesAsync();
            return true;
        }

        public async Task Upsert(string jsonSnapShot, bool displayTransactions = false, bool displayAuditLog = false, bool displayStatusTypes = false)
        {
            var transactionList = System.Text.Json.JsonSerializer.Deserialize<List<Transaction>>(jsonSnapShot);

            await using var dbTransaction = await _context.Database.BeginTransactionAsync();

            try
            {
                if (transactionList != null)
                {
                    foreach (var transaction in transactionList)
                    {
                        if (IsLessThan24Hours(transaction.TimeStamp))
                        {
                            var existingTransaction = await _context.Transactions.FirstOrDefaultAsync(x => x.Id == transaction.Id);
                            if (existingTransaction != null)
                            {
                                if (existingTransaction.StatusTypeId == (int)StatusTypeValues.Finalized)
                                {
                                    Console.WriteLine($"Skipping update for transaction {existingTransaction.Id} as it is not active.");
                                    continue;
                                }

                                // Update existing transaction
                                existingTransaction.CardNumber = MaskCardNumber(transaction.CardNumber);
                                existingTransaction.LocationCode = transaction.LocationCode;
                                existingTransaction.ProductName = transaction.ProductName;
                                existingTransaction.Amount = transaction.Amount;
                                existingTransaction.TimeStamp = DateTime.UtcNow;
                                _context.Transactions.Update(existingTransaction);
                            }
                            else
                            {
                                // Insert new transaction
                                transaction.CardNumber = MaskCardNumber(transaction.CardNumber);
                                _context.Transactions.Add(transaction);
                            }
                        }
                        await _context.SaveChangesAsync();
                    }

                    RevokeTransactions(transactionList, displayTransactions);

                    FinalizeTransactions(displayTransactions);

                    DisplayDataToConsole(displayTransactions, displayAuditLog, displayStatusTypes);
                }
                await dbTransaction.CommitAsync();

                if (displayTransactions)
                    Console.WriteLine("\n\nTransaction(s) committed successfully.");
            }
            catch (Exception ex)
            {
                // If any operation fails, roll back all changes
                await dbTransaction.RollbackAsync();
                Console.WriteLine($"An error occurred: {ex.Message}");
                Console.WriteLine("Transaction rolled back.");
            }
        }

        public async Task RevokeTransactions(List<Transaction> transactionList, bool displayTransactions = false)
        {
            DateTime cutoffTime = DateTime.UtcNow.AddHours(-24);

            // Build a list of ids from the incoming transactions and use that list for the EF Core query.
            var ids = transactionList.Select(t => t.Id).ToList();

            // Query transactions whose Id is present in the incoming list.
            var transactionsToRevoke = _context.Transactions.Where(t => !ids.Contains(t.Id) && t.StatusTypeId == (int)StatusTypeValues.Active && t.TimeStamp > cutoffTime).ToList();

            foreach (var transaction in transactionsToRevoke)
            {
                transaction.StatusTypeId = (int)StatusTypeValues.Revoked;
                _context.Transactions.Update(transaction);

                    if (displayTransactions)
                        Console.WriteLine($"\n\nRevoked: {transaction.Id} - Card Number: {transaction.CardNumber} - Product Name: {transaction.ProductName} - Status Type: {transaction.StatusTypeId}");
            }
            _context.SaveChanges();
        }

        public async Task FinalizeTransactions(bool displayTransactions = false)
        {
            var cutoffTime = DateTime.UtcNow.AddHours(-24);

            var transactionsToFinalize = _context.Transactions.Where(t => t.TimeStamp < cutoffTime).ToList();
            foreach (var transaction in transactionsToFinalize)
            {
                transaction.StatusTypeId = (int)StatusTypeValues.Finalized;
                _context.Transactions.Update(transaction);
                
                if (displayTransactions)
                    Console.WriteLine($"\n\nFinalized: {transaction.Id} - Card Number: {transaction.CardNumber} - Product Name: {transaction.ProductName} - Status Type: {transaction.StatusTypeId}");
            }
            _context.SaveChanges();
        }

        #region Helper Methods

        private void DisplayDataToConsole(bool displayTransactions, bool displayAuditLog, bool displayStatusTypes)
        {
            if (displayTransactions)
            {
                Console.WriteLine($"\nTransactions *******");
                var transactions = _context.Transactions.ToList();
                transactions.ForEach(l => Console.WriteLine($"{l.Id}: {l.LocationCode} - {l.CardNumber} - {l.ProductName} - {l.Amount} - {l.TimeStamp}"));
            }

            if (displayAuditLog)
            {
                Console.WriteLine($"\n\nAUDIT LOG *******");
                var logs = _context.AuditLogs.ToList();
                logs.ForEach(l => Console.WriteLine($"{l.Timestamp}: {l.EntityName} - {l.Action} - {l.Changes}\n"));
            }

            if (displayStatusTypes)
            {
                Console.WriteLine($"\nStatus Types *******");
                var statusTypes = _context.StatusTypes.ToList();
                statusTypes.ForEach(s => Console.WriteLine($"{s.Id}: {s.Name}"));
            }
        }

        private bool IsLessThan24Hours(DateTime timeStamp)
        {
            if (timeStamp.Kind != DateTimeKind.Utc)
            {
                throw new ArgumentException("TimeStamp must have DateTimeKind.Utc", nameof(timeStamp));
            }

            DateTime currentUtcTime = DateTime.UtcNow;
            TimeSpan difference = currentUtcTime.Subtract(timeStamp);

             return currentUtcTime > timeStamp && difference.TotalHours < 24;
        }

        private static string MaskCardNumber(string cardNumber)
        {
            if (string.IsNullOrWhiteSpace(cardNumber) || cardNumber.Length <= 4)
            {
                // handle null, empty and short card numbers
                return cardNumber;
            }

            string lastFour = cardNumber.Substring(cardNumber.Length - 4);

            string mask = new string('*', cardNumber.Length - 4);

            return mask + lastFour;
        }

        #endregion
    }
}
