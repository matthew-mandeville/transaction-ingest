using TransactionIngest.Data;
using TransactionIngest.Models;
using TransactionIngest.Services;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json;
using System.Threading.Tasks;
using Xunit;

namespace TransactionIngest.Tests
{
    public class TransactionServiceTests : IClassFixture<SqliteFixture>
    {
        private readonly SqliteFixture _fixture;
        private readonly AppDbContext _context;
        private readonly TransactionService _service;

        public TransactionServiceTests(SqliteFixture fixture)
        {
            _fixture = fixture;
            _context = _fixture.CreateContext();
            _service = new TransactionService(_context);

            _context.Database.EnsureCreated();

        }

        [Fact]
        public async Task CreateTransaction()
        {
            var created = await _service.CreateAsync(GetTransactionTestData());

            Assert.True(created.Id > 0);
            Assert.Single(_context.Transactions.Where(x => x.Id == created.Id)); 
        }

        [Fact]
        public async Task GetTransaction()
        {
            var created = await _service.CreateAsync(GetTransactionTestData(productName: "Test"));

            Assert.True(created.Id > 0);

            var fetched = await _service.GetAsync(created.Id);

            Assert.NotNull(fetched);
            Assert.Equal("Test", fetched!.ProductName);
        }

        [Fact]
        public async Task Upsert_Single_Transaction()
        {
            Transaction transaction = GetTransactionTestData();
            _context.Transactions.Add(transaction);
            await _context.SaveChangesAsync();

            var savedTransactionId = transaction.Id;

            // modify transaction
            transaction.ProductName = transaction.ProductName + "-edit";

            // create new snapshot with modified transaction
            var transactionList = new List<Transaction>();
            transactionList.Add(transaction);
            var json = JsonSerializer.Serialize(transactionList);

            await _service.Upsert(json);

            var processedTransaction = await _context.Transactions.Where(t => t.Id == savedTransactionId).FirstOrDefaultAsync();

            Assert.NotNull(processedTransaction);
            Assert.Equal(savedTransactionId, processedTransaction.Id);
            Assert.Equal((int)StatusTypeValues.Active, processedTransaction.StatusTypeId);
            Assert.Contains("*", processedTransaction.CardNumber);
        }

        [Fact]
        public async Task Upsert_Multiple_Transactions_No_Duplicates()
        {
        }

        [Fact]
        public async Task Upsert_Multiple_Transactions_With_Duplicates()
        {
        }

        [Fact]
        public async Task Upsert_No_Transactions()
        {
            var transactionsBefore = await _context.Transactions.ToListAsync();

            var transactionList = new List<Transaction>();
            var json = JsonSerializer.Serialize(transactionList);

            await _service.Upsert(json);

            var transactionsAfter = await _context.Transactions.ToListAsync();

            Assert.Equal(transactionsBefore.Count, transactionsAfter.Count);
        }



        [Fact]
        public async Task Revoke_Transaction_Not_In_Snapshot()
        {
            // add transaction with timestamp in the past 24 hours
            Transaction transaction = GetTransactionTestData(DateTime.UtcNow.AddHours(-12));
            _context.Transactions.Add(transaction);
            await _context.SaveChangesAsync();

            var revokeTransactionId = transaction.Id;

            // create snapshot with new transaction
            var transactionList = new List<Transaction>();
            transactionList.Add(GetTransactionTestData(DateTime.UtcNow, revokeTransactionId + 1));
            var json = JsonSerializer.Serialize(transactionList);

            await _service.RevokeTransactions(transactionList);

            // retreive the transaction and that should have been revoked
            var processedTransaction = await _context.Transactions.Where(t => t.Id == revokeTransactionId).FirstOrDefaultAsync();

            Assert.NotNull(processedTransaction);
            Assert.Equal((int)StatusTypeValues.Revoked, processedTransaction.StatusTypeId);
        }

        [Fact]
        public async Task Do_Not_Revoke_Transaction_In_Snapshot()
        {
            // add transaction
            Transaction transaction = GetTransactionTestData();
            _context.Transactions.Add(transaction);
            await _context.SaveChangesAsync();

            var savedTransactionId = transaction.Id;

            // modify transaction
            transaction.ProductName = transaction.ProductName + "-edit";

            // create new snapshot with modified transaction
            var transactionList = new List<Transaction>();
            transactionList.Add(transaction);
            var json = JsonSerializer.Serialize(transactionList);

            await _service.RevokeTransactions(transactionList);

            // retreive the transaction and verify it is not revoked
            var processedTransaction = await _context.Transactions.Where(t => t.Id == savedTransactionId).FirstOrDefaultAsync();

            Assert.NotNull(processedTransaction);
            Assert.Equal((int)StatusTypeValues.Active, processedTransaction.StatusTypeId);

        }

        [Fact]
        public async Task Finalize_Old_Transaction()
        {
            // add transaction with timestamp more than 24 hours in the past
            Transaction transaction = GetTransactionTestData(DateTime.UtcNow.AddDays(-10));
            _context.Transactions.Add(transaction);
            await _context.SaveChangesAsync();

            var finalizedTransactionId = transaction.Id;

            await _service.FinalizeTransactions();

            // retreive the transaction and that should have been finalized
            var processedTransaction = await _context.Transactions.Where(t => t.Id == finalizedTransactionId).FirstOrDefaultAsync();

            Assert.NotNull(processedTransaction);
            Assert.Equal((int)StatusTypeValues.Finalized, processedTransaction.StatusTypeId);
        }

        [Fact]
        public async Task Do_Not_Finalize_Active_Transaction()
        {
            // add transaction with timestamp less than 24 hours in the past
            Transaction transaction = GetTransactionTestData(DateTime.UtcNow.AddHours(-3));
            _context.Transactions.Add(transaction);
            await _context.SaveChangesAsync();

            var finalizedTransactionId = transaction.Id;

            await _service.FinalizeTransactions();

            // retreive the transaction and that should have been finalized
            var processedTransaction = await _context.Transactions.Where(t => t.Id == finalizedTransactionId).FirstOrDefaultAsync();

            Assert.NotNull(processedTransaction);
            Assert.NotEqual((int)StatusTypeValues.Finalized, processedTransaction.StatusTypeId);
        }


        #region Helper Methods

        private Transaction GetTransactionTestData(DateTime? timeStamp = null, int id = 0, string cardNumber = "123456789", string locationCode = "IA", decimal amount = 1.99m, string productName = "test product")
        {
            var tx = new Transaction
            {
                CardNumber = cardNumber,
                LocationCode = locationCode,
                Amount = amount,
                ProductName = productName,
                TimeStamp = timeStamp ?? DateTime.UtcNow,
                StatusTypeId = (int)StatusTypeValues.Active
            };

            if (id > 0)
            {
                tx.Id = id;
            }

            return tx;
        }

        private List<Transaction> GetTransactionListTestData()
        {
            return new List<Transaction>
            {
                GetTransactionTestData()
            };

        }

        #endregion
    }
}
