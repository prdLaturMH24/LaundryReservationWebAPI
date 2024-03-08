using Microsoft.EntityFrameworkCore;
using ReservationWebAPI.Data;
using ReservationWebAPI.Interfaces;
using ReservationWebAPI.Models;
using System;

namespace ReservationWebAPI.Repositories
{
    public class ReservationRepository : IReservationRepository
    {
        private readonly LaundryDbContext _laundryDbContext;

        public ReservationRepository(LaundryDbContext laundryDbContext)
        {
            _laundryDbContext = laundryDbContext;
        }

        public async Task<bool> AddReservationAsync(Reservation reservation)
        {
            await _laundryDbContext.Reservation.AddAsync(reservation);
            return await SaveChangesAsync();
        }

        public string GenerateRandomPin()
        {
            Random _random = new Random();
            int randomPin = _random.Next(00000, 100000);
            string formattedPin = randomPin.ToString("D5");
            return formattedPin;
        }

        public async Task<IEnumerable<Reservation>> GetAllReservationsAsync()
        {
            return await _laundryDbContext.Reservation.Include(r => r.Machine).ToListAsync();
        }

        public async Task<Reservation> GetReservationAsync(string email, string phone)
        {
            return await _laundryDbContext.Reservation.Include(r => r.Machine)
                .Where(r => r.Email == email && r.CellPhoneNumber == phone)
                .FirstOrDefaultAsync();
        }

        public async Task<bool> DeleteReservationAsync(Reservation reservation)
        {
            _laundryDbContext.Reservation.Remove(reservation);
            return await SaveChangesAsync();
        }

        public async Task<bool> UpdateReservationAsync(Reservation reservation)
        {
            _laundryDbContext.Reservation.Update(reservation);
            return await SaveChangesAsync();
        }

        public async Task<bool> SaveChangesAsync()
        {
            int savedEntries =await _laundryDbContext.SaveChangesAsync();
            return savedEntries > 0 ? true : false;
        }
    }
}
