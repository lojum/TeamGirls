using System;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using Domain.Entities;
using Domain.Enums;
using DataAccess;

namespace BusinessLogic.Services
{
    public class LoanEventArgs : EventArgs
    {
        public string BookTitle { get; set; } = string.Empty;
        public string ReaderName { get; set; } = string.Empty;
    }

    public delegate void BookLoanedEventHandler(object sender, LoanEventArgs e);

    public interface ILoanService
    {
        // Подія, на яку підпишеться ViewModel Оксани
        event BookLoanedEventHandler BookLoaned;
        Task<bool> IssueBookAsync(string inventoryNumber, string readerEmail, long librarianId);
    }

    public class LoanService : ILoanService
    {
        private readonly SmartLibDbContext _context;
        
        public event BookLoanedEventHandler? BookLoaned;

        public LoanService(SmartLibDbContext context)
        {
            _context = context;
        }

        public async Task<bool> IssueBookAsync(string inventoryNumber, string readerEmail, long librarianId)
        {
            var reader = await _context.Users
                .FirstOrDefaultAsync(u => u.Email == readerEmail && u.Role == UserRole.Reader);
            
            if (reader == null) 
                throw new Exception("Читача з таким Email не знайдено.");

            var copy = await _context.BookCopies
                .Include(c => c.Book)
                .FirstOrDefaultAsync(c => c.InventoryNumber == inventoryNumber);

            if (copy == null) 
                throw new Exception("Примірник з таким інвентарним номером не знайдено.");
                
            if (copy.Status != CopyStatus.Available) 
                throw new Exception("Цей примірник зараз недоступний для видачі (вже виданий або списаний).");

            var loan = new Loan
            {
                CopyId = copy.CopyId,
                ReaderId = reader.UserId,
                LibrarianId = librarianId, 
                LoanedAt = DateTime.UtcNow,
                DueDate = DateTime.UtcNow.AddDays(14) 
            };

            _context.Loans.Add(loan);
            
            await _context.SaveChangesAsync();

            BookLoaned?.Invoke(this, new LoanEventArgs 
            { 
                BookTitle = copy.Book.Title, 
                ReaderName = reader.FullName 
            });

            return true;
        }
    }
}