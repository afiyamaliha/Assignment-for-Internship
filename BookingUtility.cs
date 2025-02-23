using Wafi.SampleTest.Dtos;
using Wafi.SampleTest.Entities;

namespace Wafi.SampleTest
{
    public static class BookingUtility
    {
        public static List<BookingCalendarDto> GetCalendarBookings(IEnumerable<Booking> bookings, DateOnly? BookingDate = null, DateOnly? EndRepeatDate = null)
        {
            var calendar = recurringBookings(bookings, BookingDate ?? DateOnly.MinValue, EndRepeatDate ?? DateOnly.MaxValue);
            return calendar;
        }

        //public static List<BookingCalendarDto> recurringBookings(List<Booking> bookings, DateOnly BookingDate, DateOnly EndRepeatDate)
        //{
        //    var calendar = new List<BookingCalendarDto>();

        //    foreach (var booking in bookings)
        //    {
        //        var currentDate = booking.BookingDate;
        //        while (currentDate <= (booking.EndRepeatDate ?? booking.BookingDate) && currentDate <= EndRepeatDate)
        //        {
        //            if (currentDate >= BookingDate)
        //            {
        //                if (booking.RepeatOption == RepeatOption.Weekly && booking.DaysToRepeatOn.HasValue)
        //                {
        //                    // Loop through each day of the week and check if it matches
        //                    if (!IsDayOfWeekMatch(booking.DaysToRepeatOn.Value, currentDate.DayOfWeek))
        //                    {
        //                        currentDate = currentDate.AddDays(1);
        //                        continue;
        //                    }
        //                }

        //                calendar.Add(new BookingCalendarDto
        //                {
        //                    BookingDate = currentDate,
        //                    StartTime = booking.StartTime,
        //                    EndTime = booking.EndTime,
        //                    CarModel = booking.Car.Model
        //                });
        //            }

        //            currentDate = booking.RepeatOption switch
        //            {
        //                RepeatOption.Daily => currentDate.AddDays(1),
        //                RepeatOption.Weekly => currentDate.AddDays(1),
        //                _ => booking.EndRepeatDate.HasValue ? booking.EndRepeatDate.Value.AddDays(1) : currentDate.AddDays(1)
        //            };
        //        }
        //    }

        //    return calendar;
        //}

        public static List<BookingCalendarDto> recurringBookings(IEnumerable<Booking> bookings, DateOnly BookingDate, DateOnly EndRepeatDate)
        {
            var calendar = new List<BookingCalendarDto>();

            foreach (var booking in bookings)
            {
                var currentDate = booking.BookingDate;
                while (currentDate <= (booking.EndRepeatDate ?? booking.BookingDate) && currentDate <= EndRepeatDate)
                {
                    if (currentDate >= BookingDate)
                    {
                        if (booking.RepeatOption == RepeatOption.Weekly && booking.DaysToRepeatOn.HasValue)
                        {
                            if (!IsDayOfWeekMatch(booking.DaysToRepeatOn.Value, currentDate.DayOfWeek))
                            {
                                currentDate = currentDate.AddDays(1);
                                continue;
                            }
                        }

                        calendar.Add(new BookingCalendarDto
                        {
                            BookingDate = currentDate,
                            CarModel = booking.Car.Model,
                            StartTime = booking.StartTime,
                            EndTime = booking.EndTime

                        });
                    }

                    currentDate = booking.RepeatOption switch
                    {
                        RepeatOption.Daily => currentDate.AddDays(1),
                        RepeatOption.Weekly => currentDate.AddDays(1),
                        _ => booking.EndRepeatDate.HasValue ? booking.EndRepeatDate.Value.AddDays(1) : currentDate.AddDays(1)
                    };
                }
            }

            return calendar;
        }
        public static bool checkBookingConflictOrNot(IEnumerable<BookingCalendarDto> existingBookings, CreateUpdateBookingDto newBooking)
        {
            return existingBookings.Any(c =>
                c.BookingDate == newBooking.BookingDate &&
                !(newBooking.EndTime <= c.StartTime || newBooking.StartTime >= c.EndTime));

        }

        public static bool IsDayOfWeekMatch(DaysOfWeek repeatDays, DayOfWeek day)
        {
            return ((DaysOfWeek)(1 << (int)day) & repeatDays) != 0;
        }

    }
}
