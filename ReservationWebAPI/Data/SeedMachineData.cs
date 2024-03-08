using Microsoft.EntityFrameworkCore;
using ReservationWebAPI.Models;
using System;

namespace ReservationWebAPI.Data
{
    public static class SeedMachineData
    {
        public static void Initialize(IServiceProvider serviceProvider)
        {
            using (var context = new LaundryDbContext(
                serviceProvider.GetRequiredService<DbContextOptions<LaundryDbContext>>()))
            {
                // Check if there are any machines in the database
                if (context.Machines.Any())
                {
                    return; // Database has been seeded
                }

                // Add initial machines
                context.Machines.AddRange(
                    new Machine { MachineNumber=  "M000", IsLocked=  false },
                    new Machine { MachineNumber = "M001", IsLocked = false },
                    new Machine { MachineNumber = "M003", IsLocked = false },
                    new Machine { MachineNumber = "M003", IsLocked = false }
                // Add more machines as needed
                );

                context.SaveChanges();
            }
        }
    }
}
