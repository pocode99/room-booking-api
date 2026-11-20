# Room Booking API

A REST API for booking meeting rooms inside a company.

## Who uses it
- **Employees (User)**: browse rooms, book a room for a time slot, cancel their own bookings.
- **Administrators (Admin)**: manage rooms (create, update, delete) and cancel any booking.

## Core features
- Register and log in (JWT authentication)
- Manage rooms (name, capacity)
- Book a room for a time range
- Search bookings by room and time range, with pagination
- Cancel a booking

## Business rules
1. A booking's start time must be before its end time.
2. A booking cannot start in the past.
3. A booking cannot be longer than 4 hours.
4. Two bookings for the same room cannot overlap. Back-to-back bookings
   (one ends at 10:00, the next starts at 10:00) are allowed.
5. Only the booking owner or an Admin can cancel a booking.
6. All times are stored in UTC.