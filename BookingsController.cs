using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Wafi.SampleTest.Dtos;
using Wafi.SampleTest.Entities;

namespace Wafi.SampleTest.Controllers
{
    [Route("api/Bookings")]
    [ApiController]
    public class BookingsController : ControllerBase
    {
        private readonly WafiDbContext _context;
        private readonly ILogger<BookingsController> _logger;

        public BookingsController(WafiDbContext context, 
            ILogger<BookingsController> logger)
        {
            _context = context;
            _logger = logger;
        }

        // GET: api/Bookings
        [HttpGet("Booking")]
        public async Task<IEnumerable<BookingCalendarDto>> GetCalendarBookings(BookingFilterDto input)
        {
            try
            {
                var bookings = await _context.Bookings
                    .Include(c => c.Car).Where(c => c.CarId == input.CarId &&
                                c.BookingDate >= input.StartBookingDate &&
                                c.BookingDate <= input.EndBookingDate).ToListAsync();

                return BookingUtility.GetCalendarBookings(bookings, input.StartBookingDate, input.EndBookingDate);

                //var calendar = new Dictionary<DateOnly, List<Booking>>();

                //foreach (var booking in bookings)
                //{
                //    var currentDate = booking.BookingDate;
                //    while (currentDate <= (booking.EndRepeatDate ?? booking.BookingDate))
                //    {
                //        if (currentDate < input.StartBookingDate || currentDate > input.EndBookingDate)
                //        {
                //            currentDate = currentDate.AddDays(1);
                //            continue;
                //        }

                //        if (!calendar.ContainsKey(currentDate))
                //            calendar[currentDate] = new List<Booking>();

                //        if (booking.RepeatOption == RepeatOption.Weekly && booking.DaysToRepeatOn.HasValue)
                //        {
                //            if (((DaysOfWeek)(1 << (int)currentDate.DayOfWeek) & booking.DaysToRepeatOn.Value) == 0)
                ////            {
                //                currentDate = currentDate.AddDays(1);
                //                continue;
                //            }
                //        }

                //        calendar[currentDate].Add(booking);

                //        currentDate = booking.RepeatOption switch
                //        {
                //            RepeatOption.Daily => currentDate.AddDays(1),
                //            RepeatOption.Weekly => currentDate.AddDays(7),
                //            _ => booking.EndRepeatDate.HasValue ? booking.EndRepeatDate.Value.AddDays(1) : currentDate.AddDays(1)
                //        };
                //    }
                //}

                //List<BookingCalendarDto> result = new List<BookingCalendarDto>();
                //foreach (var item in calendar)
                //{
                //    foreach (var booking in item.Value)
                //    {
                //        result.Add(new BookingCalendarDto
                //        {
                //            BookingDate = item.Key,
                //            CarModel = booking.Car.Model,
                //            StartTime = booking.StartTime,
                //            EndTime = booking.EndTime
                //        });
                //    }
                //}

                //return result;
            }
            catch (Exception ex)
            {
                _logger.LogInformation(ex, "Failed to GetCalendarBookings method...");
                throw new InvalidOperationException(ex.Message);
            }
        }


        //POST: api/Bookings
        [HttpPost("Booking")]
        public async Task<CreateUpdateBookingDto> PostBooking(CreateUpdateBookingDto booking)
        {
            var existingBookings = await _context.Bookings.Include(c => c.Car).ToListAsync();

            var recurringBookings = BookingUtility.recurringBookings(existingBookings, 
                    booking.BookingDate, booking.EndRepeatDate ?? booking.BookingDate);

            if (BookingUtility.checkBookingConflictOrNot(recurringBookings, booking))
            {
                throw new InvalidOperationException("Time conflicts with an existing booking.");
            }

            var newBooking = new Booking
            {
                Id = Guid.NewGuid(),
                BookingDate = booking.BookingDate,
                StartTime = booking.StartTime,
                EndTime = booking.EndTime,
                RepeatOption = booking.RepeatOption,
                EndRepeatDate = booking.EndRepeatDate,
                DaysToRepeatOn = booking.RepeatOption == RepeatOption.Weekly ? booking.DaysToRepeatOn : null,
                RequestedOn = DateTime.Now,
                CarId = booking.CarId
            };

            await _context.Bookings.AddAsync(newBooking);
            await _context.SaveChangesAsync();

            return booking;
        }



//        [HttpPost("Booking")]
//        public async Task<CreateUpdateBookingDto> PostBooking(CreateUpdateBookingDto booking)
//        {
//            if (!ModelState.IsValid)
//            {
//                throw new InvalidOperationException("Booking information invalid.");
//            }
//            try
//            {
//                var existingBookings = await _context.Bookings.Include(c => c.Car).ToListAsync();

//                var recurringBookingsList = recurringBookings(existingBookings, booking.BookingDate, booking.EndRepeatDate ?? booking.BookingDate);

//                if (recurringBookingsList.Any(c => bookingConflictOrNot(c, booking)))
//                {
//                    throw new InvalidOperationException("Booking time conflicts with an existing booking...");
//                }

//                var newBooking = new Booking
//        {
//            Id = Guid.NewGuid(),
//            BookingDate = booking.BookingDate,
//            StartTime = booking.StartTime,
//            EndTime = booking.EndTime,
//            RepeatOption = booking.RepeatOption,
//            EndRepeatDate = booking.EndRepeatDate,
//            DaysToRepeatOn = booking.DaysToRepeatOn,
//            RequestedOn = DateTime.UtcNow,
//            CarId = booking.CarId
//        };
//        await _context.Bookings.AddAsync(newBooking);
//        await _context.SaveChangesAsync();
//                return booking;
//    }
//            catch (ArgumentException ex)
//            {
//                _logger.LogInformation(ex, "Failed to PostBooking method...");
//                throw new InvalidOperationException(ex.Message);
//}
//        }
//        private List<BookingCalendarDto> recurringBookings(List<Booking> bookings, DateOnly StartTime, DateOnly EndTime)
//{
//    var calendar = new List<BookingCalendarDto>();

//    foreach (var booking in bookings)
//    {
//        var currentDate = booking.BookingDate;
//        while (currentDate <= (booking.EndRepeatDate ?? booking.BookingDate) && currentDate <= EndTime)
//        {
//                    if (currentDate >= StartTime)
//                    {
//                        if (booking.RepeatOption == RepeatOption.Weekly && booking.DaysToRepeatOn.HasValue)
//                        {
//                            if (((DaysOfWeek)(1 << (int) currentDate.DayOfWeek) & booking.DaysToRepeatOn.Value) == 0)
//                            {
//                                currentDate = currentDate.AddDays(1);
//                                continue;
//                            }
//}

//calendar.Add(new BookingCalendarDto
//{
//    BookingDate = currentDate,
//    CarModel = booking.Car.Model,
//    StartTime = booking.StartTime,
//    EndTime = booking.EndTime
//});
//    }

//    currentDate = booking.RepeatOption switch
//                    {
//                        RepeatOption.Daily => currentDate.AddDays(1),
//                        RepeatOption.Weekly => currentDate.AddDays(7),
//                        _ => booking.EndRepeatDate.HasValue? booking.EndRepeatDate.Value.AddDays(1) : currentDate.AddDays(1)
//                    };
//                }
//            }

//            return calendar;
//        }
//        private bool bookingConflictOrNot(BookingCalendarDto existingBooking, CreateUpdateBookingDto newBooking)
//{
//    return existingBooking.BookingDate == newBooking.BookingDate &&
//           !(newBooking.EndTime <= existingBooking.StartTime || newBooking.StartTime >= existingBooking.EndTime);
//}

// GET: api/SeedData
// For test purpose
[HttpGet("SeedData")]
        public async Task<IEnumerable<BookingCalendarDto>> GetSeedData()
        {
            try
            {
                var cars = await _context.Cars.ToListAsync();

                if (!cars.Any())
                {
                    cars = GetCars().ToList();
        await _context.Cars.AddRangeAsync(cars);
        await _context.SaveChangesAsync();
                }

                var bookings = await _context.Bookings.ToListAsync();

                if (!bookings.Any())
                {
                    bookings = GetBookings().ToList();

    await _context.Bookings.AddRangeAsync(bookings);
        await _context.SaveChangesAsync();
                }

                return BookingUtility.GetCalendarBookings(bookings);

            }
            catch(Exception ex)
            {
                _logger.LogInformation(ex,"Failed to GetSeedData method...");
                throw new InvalidOperationException(ex.Message);
            }
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
