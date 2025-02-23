using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System;
using System.Globalization;
using Wafi.SampleTest.Dtos;
using Wafi.SampleTest.Entities;

namespace Wafi.SampleTest.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class BookingsController : ControllerBase
    {
        private readonly WafiDbContext _context;

        public BookingsController(WafiDbContext context)
        {
            _context = context;
        }


        [HttpGet("Booking")]
        public async Task<IEnumerable<BookingCalendarDto>> GetCalendarBookings(BookingFilterDto input)
        {
            var bookings = await _context.Bookings
                .Include(b => b.Car)
                .Where(b => b.CarId == input.CarId &&
                            b.BookingDate >= input.StartBookingDate &&
                            b.BookingDate <= input.EndBookingDate)
                .ToListAsync();

            var calendar = new Dictionary<DateOnly, List<Booking>>();

            foreach (var booking in bookings)
            {
                var currentDate = booking.BookingDate;
                while (currentDate <= (booking.EndRepeatDate ?? booking.BookingDate))
                {
                    if (currentDate < input.StartBookingDate || currentDate > input.EndBookingDate)
                    {
                        currentDate = currentDate.AddDays(1);
                        continue;
                    }

                    if (!calendar.ContainsKey(currentDate))
                        calendar[currentDate] = new List<Booking>();

                    if (booking.RepeatOption == RepeatOption.Weekly && booking.DaysToRepeatOn.HasValue)
                    {
                        if (((DaysOfWeek)(1 << (int)currentDate.DayOfWeek) & booking.DaysToRepeatOn.Value) == 0)
                        {
                            currentDate = currentDate.AddDays(1);
                            continue;
                        }
                    }

                    calendar[currentDate].Add(booking);

                    currentDate = booking.RepeatOption switch
                    {
                        RepeatOption.Daily => currentDate.AddDays(1),
                        RepeatOption.Weekly => currentDate.AddDays(7),
                        _ => booking.EndRepeatDate.HasValue ? booking.EndRepeatDate.Value.AddDays(1) : currentDate.AddDays(1)
                    };
                }
            }

            var result = new List<BookingCalendarDto>();
            foreach (var item in calendar)
            {
                foreach (var booking in item.Value)
                {
                    result.Add(new BookingCalendarDto
                    {
                        BookingDate = item.Key,
                        CarModel = booking.Car.Model,
                        StartTime = booking.StartTime,
                        EndTime = booking.EndTime
                    });
                }
            }

            return result;
        }


        //// GET: api/Bookings
        //[HttpGet("Booking")]
        //public async Task<IEnumerable<BookingCalendarDto>> GetCalendarBookings(BookingFilterDto input)
        //{
        //    // Get booking from the database and filter the data
        //    //var bookings = await _context.Bookings.ToListAsync();

        //    // TO DO: convert the database bookings to calendar view (date, start time, end time). Consiser NoRepeat, Daily and Weekly options
        //    //return bookings;

        //    throw new NotImplementedException();
        //}

        [HttpPost("Booking")]
        public async Task<CreateUpdateBookingDto> PostBooking(CreateUpdateBookingDto bookingDto)
        {
            // Step 1: Get all bookings from the database (include Car data if needed)
            var existingBookings = await _context.Bookings.Include(b => b.Car).ToListAsync();

            // Step 2: Expand existing bookings to include all recurring instances
            var expandedBookings = ExpandRecurringBookings(existingBookings, bookingDto.BookingDate, bookingDto.EndRepeatDate ?? bookingDto.BookingDate);

            // Step 3: Check for conflicts
            if (expandedBookings.Any(b => CheckBookingConflict(b, bookingDto)))
            {
                throw new InvalidOperationException("Booking time conflicts with an existing booking.");
            }

            // Step 4: Map DTO to entity
            var booking = new Booking
            {
                Id = Guid.NewGuid(),
                BookingDate = bookingDto.BookingDate,
                StartTime = bookingDto.StartTime,
                EndTime = bookingDto.EndTime,
                RepeatOption = bookingDto.RepeatOption,
                EndRepeatDate = bookingDto.EndRepeatDate,
                DaysToRepeatOn = bookingDto.DaysToRepeatOn,
                RequestedOn = DateTime.UtcNow,
                CarId = bookingDto.CarId
            };

            // Step 5: Save booking to database
            await _context.Bookings.AddAsync(booking);
            await _context.SaveChangesAsync();

            return bookingDto;
        }
        private List<BookingCalendarDto> ExpandRecurringBookings(List<Booking> bookings, DateOnly startDate, DateOnly endDate)
        {
            var calendar = new List<BookingCalendarDto>();

            foreach (var booking in bookings)
            {
                var currentDate = booking.BookingDate;
                while (currentDate <= (booking.EndRepeatDate ?? booking.BookingDate) && currentDate <= endDate)
                {
                    if (currentDate >= startDate)
                    {
                        if (booking.RepeatOption == RepeatOption.Weekly && booking.DaysToRepeatOn.HasValue)
                        {
                            if (((DaysOfWeek)(1 << (int)currentDate.DayOfWeek) & booking.DaysToRepeatOn.Value) == 0)
                            {
                                currentDate = currentDate.AddDays(1);
                                continue;
                            }
                        }

                        calendar.Add(new BookingCalendarDto
                        {
                            BookingDate = currentDate,
                            StartTime = booking.StartTime,
                            EndTime = booking.EndTime,
                            CarModel = booking.Car?.Model ?? "Unknown"
                        });
                    }

                    currentDate = booking.RepeatOption switch
                    {
                        RepeatOption.Daily => currentDate.AddDays(1),
                        RepeatOption.Weekly => currentDate.AddDays(7),
                        _ => booking.EndRepeatDate.HasValue ? booking.EndRepeatDate.Value.AddDays(1) : currentDate.AddDays(1)
                    };
                }
            }

            return calendar;
        }
        private bool CheckBookingConflict(BookingCalendarDto existing, CreateUpdateBookingDto newBooking)
        {
            return existing.BookingDate == newBooking.BookingDate &&
                   !(newBooking.EndTime <= existing.StartTime || newBooking.StartTime >= existing.EndTime);
        }





        // POST: api/Bookings
        //[HttpPost("Booking")]
        //public async Task<CreateUpdateBookingDto> PostBooking(CreateUpdateBookingDto booking)
        //{
        //    // TO DO: Validate if any booking time conflicts with existing data. Return error if any conflicts

        //    //await _context.Bookings.AddAsync(booking);
        //    //await _context.SaveChangesAsync();

        //    //return booking;

        //    throw new NotImplementedException();
        //}

        // GET: api/SeedData
        // For test purpose
        [HttpGet("SeedData")]
        public async Task<IEnumerable<BookingCalendarDto>> GetSeedData()
        {
            var cars = await _context.Cars.ToListAsync();

            if (!cars.Any())
            {
                cars = GetCars().ToList();
                await _context.Cars.AddRangeAsync(cars);
                await _context.SaveChangesAsync();
            }

            var bookings = await _context.Bookings.ToListAsync();

            if(!bookings.Any())
            {
                bookings = GetBookings().ToList();

                await _context.Bookings.AddRangeAsync(bookings);
                await _context.SaveChangesAsync();
            }

            var calendar = new Dictionary<DateOnly, List<Booking>>();

            foreach (var booking in bookings)
            {
                var currentDate = booking.BookingDate;
                while (currentDate <= (booking.EndRepeatDate ?? booking.BookingDate))
                {
                    if (!calendar.ContainsKey(currentDate))
                        calendar[currentDate] = new List<Booking>();

                    calendar[currentDate].Add(booking);

                    currentDate = booking.RepeatOption switch
                    {
                        RepeatOption.Daily => currentDate.AddDays(1),
                        RepeatOption.Weekly => currentDate.AddDays(7),
                        _ => booking.EndRepeatDate.HasValue ? booking.EndRepeatDate.Value.AddDays(1) : currentDate.AddDays(1)
                    };
                }
            }

            List<BookingCalendarDto> result = new List<BookingCalendarDto>();

            foreach (var item in calendar)
            {
                foreach(var booking in item.Value)
                {
                    result.Add(new BookingCalendarDto 
                    { 
                        BookingDate = booking.BookingDate, 
                        CarModel = booking.Car.Model, 
                        StartTime = booking.StartTime, 
                        EndTime = booking.EndTime 
                    });
                }
            }

            return result;
        }

        #region Sample Data

        private IList<Car> GetCars()
        {
            var cars = new List<Car>
            {
                new Car { Id = Guid.NewGuid(), Make = "Toyota", Model = "Corolla" },
                new Car { Id = Guid.NewGuid(), Make = "Honda", Model = "Civic" },
                new Car { Id = Guid.NewGuid(), Make = "Ford", Model = "Focus" }
            };

            return cars;
        }

        private IList<Booking> GetBookings()
        {
            var cars = GetCars();

            var bookings = new List<Booking>
            {
                new Booking { Id = Guid.NewGuid(), BookingDate = new DateOnly(2025, 2, 5), StartTime = new TimeSpan(10, 0, 0), EndTime = new TimeSpan(12, 0, 0), RepeatOption = RepeatOption.DoesNotRepeat, RequestedOn = DateTime.Now, CarId = cars[0].Id, Car = cars[0] },
                new Booking { Id = Guid.NewGuid(), BookingDate = new DateOnly(2025, 2, 10), StartTime = new TimeSpan(14, 0, 0), EndTime = new TimeSpan(16, 0, 0), RepeatOption = RepeatOption.Daily, EndRepeatDate = new DateOnly(2025, 2, 20), RequestedOn = DateTime.Now, CarId = cars[1].Id, Car = cars[1] },
                new Booking { Id = Guid.NewGuid(), BookingDate = new DateOnly(2025, 2, 15), StartTime = new TimeSpan(9, 0, 0), EndTime = new TimeSpan(10, 30, 0), RepeatOption = RepeatOption.Weekly, EndRepeatDate = new DateOnly(2025, 3, 31), RequestedOn = DateTime.Now, DaysToRepeatOn = DaysOfWeek.Monday, CarId = cars[2].Id,  Car = cars[2] },
                new Booking { Id = Guid.NewGuid(), BookingDate = new DateOnly(2025, 3, 1), StartTime = new TimeSpan(11, 0, 0), EndTime = new TimeSpan(13, 0, 0), RepeatOption = RepeatOption.DoesNotRepeat, RequestedOn = DateTime.Now, CarId = cars[0].Id, Car = cars[0] },
                new Booking { Id = Guid.NewGuid(), BookingDate = new DateOnly(2025, 3, 7), StartTime = new TimeSpan(8, 0, 0), EndTime = new TimeSpan(10, 0, 0), RepeatOption = RepeatOption.Weekly, EndRepeatDate = new DateOnly(2025, 3, 28), RequestedOn = DateTime.Now, DaysToRepeatOn = DaysOfWeek.Friday, CarId = cars[1].Id, Car = cars[1] },
                new Booking { Id = Guid.NewGuid(), BookingDate = new DateOnly(2025, 3, 15), StartTime = new TimeSpan(15, 0, 0), EndTime = new TimeSpan(17, 0, 0), RepeatOption = RepeatOption.Daily, EndRepeatDate = new DateOnly(2025, 3, 20), RequestedOn = DateTime.Now, CarId = cars[2].Id,  Car = cars[2] }
            };

            return bookings;
        }

            #endregion

        }
}
