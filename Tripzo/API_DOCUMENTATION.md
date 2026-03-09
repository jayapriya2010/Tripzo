# Tripzo Bus Ticket Booking System - API Documentation

## ?? Table of Contents
1. [Overview](#overview)
2. [Authentication](#authentication)
3. [API Endpoints](#api-endpoints)
   - [Authentication Endpoints](#1-authentication-endpoints)
   - [User Endpoints](#2-user-endpoints)
   - [Admin Endpoints](#3-admin-endpoints)
   - [Operator Endpoints](#4-operator-endpoints)
   - [Passenger Endpoints](#5-passenger-endpoints)
4. [Status Codes](#status-codes)
5. [Booking Status Flow](#booking-status-flow)
6. [Amenity Management Flow](#amenity-management-flow)
7. [Testing with Swagger](#testing-with-swagger)

---

## Overview

**Base URL:** `https://localhost:{port}/api`

**Technology Stack:**
- ASP.NET Core Web API (.NET 10)
- Entity Framework Core
- JWT Authentication
- SQL Server Database

**Authentication:** JWT Bearer Token

---

## Authentication

All protected endpoints require a valid JWT token in the Authorization header:

```
Authorization: Bearer <your_jwt_token>
```

### Roles:
| Role | Description |
|------|-------------|
| `Admin` | System administrator - manages users, approves cancellations, creates amenities, views system logs |
| `Operator` | Bus operator - manages buses, routes, schedules, amenities mapping, and processes refunds |
| `Passenger` | End user - searches buses (with amenities), books tickets, cancels bookings |

---

## API Endpoints

---

### 1. Authentication Endpoints

#### ?? Login
Authenticates a user and returns a JWT token.

| | |
|---|---|
| **URL** | `POST /api/Auth/login` |
| **Auth Required** | No |
| **Roles Allowed** | Anonymous |

**Request Body:**
```json
{
  "email": "passenger1@gmail.com",
  "password": "Password123"
}
```

**Success Response (200 OK):**
```json
{
  "token": "eyJhbGciOiJIUzI1NiIsInR5cCI6IkpXVCJ9...",
  "email": "passenger1@gmail.com",
  "role": "Passenger",
  "fullName": "John Doe"
}
```

**Error Responses:**
| Status | Description |
|--------|-------------|
| 400 | Invalid login request |
| 401 | Invalid email or password |
| 401 | Account deactivated |

---

### 2. User Endpoints

#### ?? Register User
Registers a new Passenger or Operator account.

| | |
|---|---|
| **URL** | `POST /api/User/register` |
| **Auth Required** | No |
| **Roles Allowed** | Anonymous |

**Request Body:**
```json
{
  "fullName": "John Doe",
  "email": "john.doe@example.com",
  "password": "SecurePass123",
  "role": "Passenger",
  "gender": "Male"
}
```

**Validation Rules:**
| Field | Rules |
|-------|-------|
| `fullName` | Required, Max 150 characters |
| `email` | Required, Valid email format, Max 255 characters |
| `password` | Required, 6-100 characters |
| `role` | Required, Must be `Passenger` or `Operator` (Admin not allowed) |
| `gender` | Optional, Must be `Male`, `Female`, `Other`, or `PreferNotToSay` |

**Success Response (200 OK):**
```json
{
  "message": "Passenger registered successfully!"
}
```

**Error Responses:**
| Status | Description |
|--------|-------------|
| 400 | Invalid request payload |
| 400 | Invalid role (only Passenger/Operator allowed) |
| 400 | User with this email already exists |

---

### 3. Admin Endpoints

> ?? **All Admin endpoints require `[Authorize(Roles = "Admin")]`**

---

#### ?? Get Dashboard Statistics
Returns system-wide statistics for the admin dashboard.

| | |
|---|---|
| **URL** | `GET /api/Admin/dashboard` |
| **Auth Required** | Yes |
| **Roles Allowed** | Admin |

**Success Response (200 OK):**
```json
{
  "totalPassengers": 150,
  "activeOperators": 12,
  "totalBuses": 45,
  "totalRevenue": 125000.00,
  "todaysBookings": 28,
  "systemErrorsLast24Hours": 3
}
```

---

#### ?? Get All Users
Returns all Operators and Passengers (excludes Admin users).

| | |
|---|---|
| **URL** | `GET /api/Admin/users` |
| **Auth Required** | Yes |
| **Roles Allowed** | Admin |

**Success Response (200 OK):**
```json
[
  {
    "userId": 2,
    "fullName": "John Doe",
    "email": "john.doe@example.com",
    "role": "Passenger",
    "gender": "Male",
    "isActive": true
  },
  {
    "userId": 3,
    "fullName": "Bus Operator 1",
    "email": "operator1@gmail.com",
    "role": "Operator",
    "gender": "Male",
    "isActive": true
  }
]
```

---

#### ?? Deactivate User Account
Soft deletes a user account (Operator or Passenger only).

| | |
|---|---|
| **URL** | `PUT /api/Admin/deactivate-user/{userId}` |
| **Auth Required** | Yes |
| **Roles Allowed** | Admin |

**Path Parameters:**
| Parameter | Type | Description |
|-----------|------|-------------|
| `userId` | int | The ID of the user to deactivate |

**Success Response (200 OK):**
```json
{
  "message": "User account deactivated successfully."
}
```

**Error Response (404 Not Found):**
```json
"User not found or cannot be deactivated."
```

---

#### ? Activate User Account
Reactivates a previously deactivated user account.

| | |
|---|---|
| **URL** | `PUT /api/Admin/activate-user/{userId}` |
| **Auth Required** | Yes |
| **Roles Allowed** | Admin |

**Path Parameters:**
| Parameter | Type | Description |
|-----------|------|-------------|
| `userId` | int | The ID of the user to activate |

**Success Response (200 OK):**
```json
{
  "message": "User account activated successfully."
}
```

---

#### ?? Get Pending Cancellations
Returns all bookings with "Cancelled" status awaiting admin approval.

| | |
|---|---|
| **URL** | `GET /api/Admin/pending-cancellations` |
| **Auth Required** | Yes |
| **Roles Allowed** | Admin |

**Success Response (200 OK):**
```json
[
  {
    "bookingId": 1,
    "passengerName": "John Doe",
    "passengerEmail": "john.doe@example.com",
    "routeName": "Mumbai to Pune",
    "busNumber": "MH01AB1234",
    "journeyDate": "2026-03-15T00:00:00",
    "totalAmount": 450.00,
    "cancellationDate": "2026-03-10T14:30:00",
    "status": "Cancelled"
  }
]
```

---

#### ? Approve Cancellation
Approves a cancellation request, allowing the operator to process refund.

| | |
|---|---|
| **URL** | `PUT /api/Admin/approve-cancellation/{bookingId}` |
| **Auth Required** | Yes |
| **Roles Allowed** | Admin |

**Path Parameters:**
| Parameter | Type | Description |
|-----------|------|-------------|
| `bookingId` | int | The ID of the booking to approve |

**Success Response (200 OK):**
```json
{
  "message": "Cancellation approved. Operator can now process the refund."
}
```

---

#### ? Reject Cancellation
Rejects a cancellation request, reverting booking to "Confirmed" status.

| | |
|---|---|
| **URL** | `PUT /api/Admin/reject-cancellation/{bookingId}` |
| **Auth Required** | Yes |
| **Roles Allowed** | Admin |

**Path Parameters:**
| Parameter | Type | Description |
|-----------|------|-------------|
| `bookingId` | int | The ID of the booking to reject |

**Success Response (200 OK):**
```json
{
  "message": "Cancellation rejected. Booking reverted to Confirmed status."
}
```

---

#### ??? Get All Routes
Returns all routes in the system with their stops.

| | |
|---|---|
| **URL** | `GET /api/Admin/routes` |
| **Auth Required** | Yes |
| **Roles Allowed** | Admin |

**Success Response (200 OK):**
```json
[
  {
    "routeId": 1,
    "busName": "Luxury Express",
    "busNumber": "MH01AB1234",
    "sourceCity": "Mumbai",
    "destCity": "Pune",
    "baseFare": 450.00,
    "stops": [
      {
        "stopId": 1,
        "cityName": "Mumbai",
        "locationName": "Dadar Bus Terminal",
        "stopType": "Boarding",
        "stopOrder": 1,
        "arrivalTime": "06:00:00"
      },
      {
        "stopId": 2,
        "cityName": "Lonavala",
        "locationName": "Lonavala Bus Stand",
        "stopType": "Both",
        "stopOrder": 2,
        "arrivalTime": "07:30:00"
      },
      {
        "stopId": 3,
        "cityName": "Pune",
        "locationName": "Shivaji Nagar",
        "stopType": "Dropping",
        "stopOrder": 3,
        "arrivalTime": "09:00:00"
      }
    ]
  }
]
```

---

#### ??? Get Route Details by ID
Returns detailed information for a specific route.

| | |
|---|---|
| **URL** | `GET /api/Admin/routes/{routeId}` |
| **Auth Required** | Yes |
| **Roles Allowed** | Admin |

**Path Parameters:**
| Parameter | Type | Description |
|-----------|------|-------------|
| `routeId` | int | The ID of the route |

**Success Response (200 OK):**
```json
{
  "routeId": 1,
  "busName": "Luxury Express",
  "busNumber": "MH01AB1234",
  "sourceCity": "Mumbai",
  "destCity": "Pune",
  "baseFare": 450.00,
  "stops": [...]
}
```

---

#### ?? Get Global Bookings
Returns all bookings in the system for auditing purposes.

| | |
|---|---|
| **URL** | `GET /api/Admin/bookings` |
| **Auth Required** | Yes |
| **Roles Allowed** | Admin |

**Success Response (200 OK):**
```json
[
  {
    "bookingId": 1,
    "passengerName": "John Doe",
    "routeName": "Mumbai to Pune",
    "journeyDate": "2026-03-15T00:00:00",
    "totalAmount": 450.00,
    "status": "Confirmed"
  }
]
```

---

#### ? Create Amenity
Adds a new amenity to the master amenities list. Operators can then assign these amenities to their buses.

| | |
|---|---|
| **URL** | `POST /api/Admin/amenities` |
| **Auth Required** | Yes |
| **Roles Allowed** | Admin |

**Request Body:**
```json
{
  "amenityName": "WiFi"
}
```

**Success Response (200 OK):**
```json
{
  "message": "Amenity added to master list."
}
```

> ?? **Tip:** Common amenities to create: WiFi, AC, Charging Point, Blanket, Water Bottle, Entertainment System, Toilet, Reading Light

---

#### ?? Get System Error Logs
Returns system error logs for monitoring and debugging.

| | |
|---|---|
| **URL** | `GET /api/Admin/logs` |
| **Auth Required** | Yes |
| **Roles Allowed** | Admin |

**Success Response (200 OK):**
```json
[
  {
    "logId": 1,
    "message": "Database connection timeout",
    "source": "BookingRepository",
    "timestamp": "2026-03-10T14:30:00"
  }
]
```

---

#### ??? Clear Old Logs
Deletes error logs older than the specified date.

| | |
|---|---|
| **URL** | `DELETE /api/Admin/logs/clear` |
| **Auth Required** | Yes |
| **Roles Allowed** | Admin |

**Query Parameters:**
| Parameter | Type | Description |
|-----------|------|-------------|
| `beforeDate` | DateTime | Delete logs before this date |

**Example:** `DELETE /api/Admin/logs/clear?beforeDate=2026-01-01`

**Success Response (200 OK):**
```json
{
  "message": "Old logs cleared successfully."
}
```

---

### 4. Operator Endpoints

> ?? **All Operator endpoints require `[Authorize(Roles = "Operator")]`**

---

#### ?? Get Operator Dashboard
Returns performance metrics for the operator's dashboard.

| | |
|---|---|
| **URL** | `GET /api/Operator/dashboard/{operatorId}` |
| **Auth Required** | Yes |
| **Roles Allowed** | Operator |

**Path Parameters:**
| Parameter | Type | Description |
|-----------|------|-------------|
| `operatorId` | int | The operator's user ID |

**Success Response (200 OK):**
```json
{
  "totalBuses": 5,
  "totalActiveRoutes": 8,
  "bookingsToday": 12,
  "revenueThisMonth": 45000.00
}
```

---

#### ?? Add Bus
Registers a new bus to the operator's fleet.

| | |
|---|---|
| **URL** | `POST /api/Operator/buses` |
| **Auth Required** | Yes |
| **Roles Allowed** | Operator |

**Request Body:**
```json
{
  "busName": "Luxury Express",
  "busNumber": "MH01AB1234",
  "busType": "AC Sleeper",
  "capacity": 40,
  "operatorId": 3
}
```

**Success Response (200 OK):**
```json
{
  "message": "Bus registered successfully."
}
```

---

#### ?? Configure Seats
Defines the seat layout for a bus.

| | |
|---|---|
| **URL** | `POST /api/Operator/buses/{busId}/seats` |
| **Auth Required** | Yes |
| **Roles Allowed** | Operator |

**Path Parameters:**
| Parameter | Type | Description |
|-----------|------|-------------|
| `busId` | int | The ID of the bus |

**Request Body:**
```json
[
  {
    "seatNumber": "1A",
    "seatType": "Sleeper",
    "addonFare": 100.00
  },
  {
    "seatNumber": "1B",
    "seatType": "Sleeper",
    "addonFare": 100.00
  },
  {
    "seatNumber": "2A",
    "seatType": "Seater",
    "addonFare": 0.00
  }
]
```

**Success Response (200 OK):**
```json
{
  "message": "Seat layout configured successfully."
}
```

---

#### ?? Get All Available Amenities
Returns all amenities from the master list that can be assigned to buses.

| | |
|---|---|
| **URL** | `GET /api/Operator/amenities` |
| **Auth Required** | Yes |
| **Roles Allowed** | Operator |

**Success Response (200 OK):**
```json
[
  {
    "amenityId": 1,
    "amenityName": "WiFi"
  },
  {
    "amenityId": 2,
    "amenityName": "AC"
  },
  {
    "amenityId": 3,
    "amenityName": "Charging Point"
  },
  {
    "amenityId": 4,
    "amenityName": "Blanket"
  },
  {
    "amenityId": 5,
    "amenityName": "Water Bottle"
  }
]
```

---

#### ?? Get Bus Amenities
Returns all amenities currently assigned to a specific bus.

| | |
|---|---|
| **URL** | `GET /api/Operator/buses/{busId}/amenities` |
| **Auth Required** | Yes |
| **Roles Allowed** | Operator |

**Path Parameters:**
| Parameter | Type | Description |
|-----------|------|-------------|
| `busId` | int | The ID of the bus |

**Success Response (200 OK):**
```json
[
  {
    "amenityId": 1,
    "amenityName": "WiFi"
  },
  {
    "amenityId": 3,
    "amenityName": "Charging Point"
  }
]
```

---

#### ? Add Amenities to Bus
Assigns one or more amenities to a bus. These amenities will be visible to passengers when searching for buses.

| | |
|---|---|
| **URL** | `POST /api/Operator/buses/{busId}/amenities` |
| **Auth Required** | Yes |
| **Roles Allowed** | Operator |

**Path Parameters:**
| Parameter | Type | Description |
|-----------|------|-------------|
| `busId` | int | The ID of the bus |

**Request Body:**
```json
[1, 2, 3, 4, 5]
```
> Array of amenity IDs to add

**Success Response (200 OK):**
```json
{
  "message": "Amenities added to bus successfully."
}
```

**Error Response (400 Bad Request):**
```json
"Failed to add amenities to bus. Check if bus exists."
```

> ?? **Note:** If an amenity is already assigned to the bus, it will be skipped (no duplicates).

---

#### ? Remove Amenities from Bus
Removes one or more amenities from a bus.

| | |
|---|---|
| **URL** | `DELETE /api/Operator/buses/{busId}/amenities` |
| **Auth Required** | Yes |
| **Roles Allowed** | Operator |

**Path Parameters:**
| Parameter | Type | Description |
|-----------|------|-------------|
| `busId` | int | The ID of the bus |

**Request Body:**
```json
[1, 3]
```
> Array of amenity IDs to remove

**Success Response (200 OK):**
```json
{
  "message": "Amenities removed from bus successfully."
}
```

---

#### ??? Create Route
Creates a new route with stops.

| | |
|---|---|
| **URL** | `POST /api/Operator/routes` |
| **Auth Required** | Yes |
| **Roles Allowed** | Operator |

**Request Body:**
```json
{
  "busId": 1,
  "sourceCity": "Mumbai",
  "destCity": "Pune",
  "baseFare": 450.00,
  "stops": [
    {
      "cityName": "Mumbai",
      "locationName": "Dadar Bus Terminal",
      "stopType": "Boarding",
      "stopOrder": 1,
      "arrivalTime": "06:00:00"
    },
    {
      "cityName": "Pune",
      "locationName": "Shivaji Nagar",
      "stopType": "Dropping",
      "stopOrder": 2,
      "arrivalTime": "09:00:00"
    }
  ]
}
```

**Success Response (200 OK):**
```json
{
  "message": "Route and stops created successfully."
}
```

---

#### ?? Get Approved Cancellations
Returns cancellations approved by admin, ready for refund processing.

| | |
|---|---|
| **URL** | `GET /api/Operator/approved-cancellations/{operatorId}` |
| **Auth Required** | Yes |
| **Roles Allowed** | Operator |

**Path Parameters:**
| Parameter | Type | Description |
|-----------|------|-------------|
| `operatorId` | int | The operator's user ID |

**Success Response (200 OK):**
```json
[
  {
    "bookingId": 1,
    "passengerName": "John Doe",
    "passengerEmail": "john.doe@example.com",
    "routeName": "Mumbai to Pune",
    "journeyDate": "2026-03-15T00:00:00",
    "refundAmount": 450.00,
    "cancellationDate": "2026-03-10T14:30:00"
  }
]
```

---

#### ?? Process Refund
Processes a refund for an approved cancellation.

| | |
|---|---|
| **URL** | `POST /api/Operator/refund` |
| **Auth Required** | Yes |
| **Roles Allowed** | Operator |

**Request Body:**
```json
{
  "bookingId": 1,
  "refundAmount": 450.00,
  "refundReason": "Customer requested cancellation",
  "refundProcessedDate": "2026-03-11T10:00:00"
}
```

**Success Response (200 OK):**
```json
{
  "message": "Refund processed successfully."
}
```

**Error Response (400 Bad Request):**
```json
"Refund processing failed. Check booking status or admin approval."
```

> ?? **Note:** Refund can only be processed after admin approval (status = "CancellationApproved")

---

#### ?? Get Fleet
Returns all buses belonging to the operator (including their amenities).

| | |
|---|---|
| **URL** | `GET /api/Operator/fleet/{operatorId}` |
| **Auth Required** | Yes |
| **Roles Allowed** | Operator |

**Path Parameters:**
| Parameter | Type | Description |
|-----------|------|-------------|
| `operatorId` | int | The operator's user ID |

**Success Response (200 OK):**
```json
[
  {
    "busId": 1,
    "busName": "Luxury Express",
    "busNumber": "MH01AB1234",
    "busType": "AC Sleeper",
    "capacity": 40,
    "isActive": true
  }
]
```

---

#### ?? Toggle Bus Status
Activates or deactivates a bus (soft delete).

| | |
|---|---|
| **URL** | `PATCH /api/Operator/buses/{busId}/status` |
| **Auth Required** | Yes |
| **Roles Allowed** | Operator |

**Path Parameters:**
| Parameter | Type | Description |
|-----------|------|-------------|
| `busId` | int | The ID of the bus |

**Query Parameters:**
| Parameter | Type | Description |
|-----------|------|-------------|
| `isActive` | bool | true to activate, false to deactivate |

**Example:** `PATCH /api/Operator/buses/1/status?isActive=false`

**Success Response (200 OK):**
```json
{
  "message": "Bus visibility updated to Inactive."
}
```

---

#### ?? Create Bus Schedule
Creates schedules for a bus on specific dates.

| | |
|---|---|
| **URL** | `POST /api/Operator/schedule` |
| **Auth Required** | Yes |
| **Roles Allowed** | Operator |

**Request Body:**
```json
{
  "routeId": 1,
  "busId": 1,
  "scheduledDates": [
    "2026-03-15",
    "2026-03-16",
    "2026-03-17"
  ]
}
```

**Success Response (200 OK):**
```json
[
  {
    "scheduleId": 1,
    "routeName": "Mumbai to Pune",
    "busName": "Luxury Express",
    "scheduledDate": "2026-03-15T00:00:00",
    "isActive": true
  }
]
```

---

#### ?? Get Schedules
Returns all schedules for the operator.

| | |
|---|---|
| **URL** | `GET /api/Operator/schedules` |
| **Auth Required** | Yes |
| **Roles Allowed** | Operator |

**Query Parameters:**
| Parameter | Type | Description |
|-----------|------|-------------|
| `operatorId` | int | The operator's user ID |

**Success Response (200 OK):**
```json
[
  {
    "scheduleId": 1,
    "routeName": "Mumbai to Pune",
    "busName": "Luxury Express",
    "scheduledDate": "2026-03-15T00:00:00",
    "isActive": true
  }
]
```

---

#### ??? Delete Schedule
Soft deletes a schedule.

| | |
|---|---|
| **URL** | `DELETE /api/Operator/schedule/{scheduleId}` |
| **Auth Required** | Yes |
| **Roles Allowed** | Operator |

**Path Parameters:**
| Parameter | Type | Description |
|-----------|------|-------------|
| `scheduleId` | int | The ID of the schedule |

**Success Response (200 OK):**
```json
"Schedule deleted successfully"
```

---

### 5. Passenger Endpoints

> ?? **All Passenger endpoints require `[Authorize(Roles = "Passenger")]`**

---

#### ?? Search Buses
Searches for available buses based on route and date. Returns bus amenities from the database.

| | |
|---|---|
| **URL** | `GET /api/Passenger/search` |
| **Auth Required** | Yes |
| **Roles Allowed** | Passenger |

**Query Parameters:**
| Parameter | Type | Description |
|-----------|------|-------------|
| `fromCity` | string | Departure city |
| `toCity` | string | Destination city |
| `travelDate` | DateTime | Date of travel (must be today or future) |

**Example:** `GET /api/Passenger/search?fromCity=Mumbai&toCity=Pune&travelDate=2026-03-15`

**Success Response (200 OK):**
```json
[
  {
    "routeId": 1,
    "busName": "Luxury Express",
    "busType": "AC Sleeper",
    "departureTime": "06:00:00",
    "fare": 450.00,
    "amenities": ["WiFi", "AC", "Charging Point", "Blanket", "Water Bottle"],
    "availableSeats": 25
  }
]
```

> ?? **Note:** The `amenities` array now displays the actual amenities assigned to the bus by the operator, not hardcoded values.

**Error Response (400 Bad Request):**
```json
"Travel date cannot be in the past."
```

---

#### ?? Get Seat Map
Returns the seat layout with availability status.

| | |
|---|---|
| **URL** | `GET /api/Passenger/seats` |
| **Auth Required** | Yes |
| **Roles Allowed** | Passenger |

**Query Parameters:**
| Parameter | Type | Description |
|-----------|------|-------------|
| `busId` | int | The ID of the bus |
| `routeId` | int | The ID of the route |
| `travelDate` | DateTime | Date of travel |

**Example:** `GET /api/Passenger/seats?busId=1&routeId=1&travelDate=2026-03-15`

**Success Response (200 OK):**
```json
[
  {
    "seatId": 1,
    "seatNumber": "1A",
    "seatType": "Sleeper",
    "isAvailable": true,
    "finalPrice": 550.00
  },
  {
    "seatId": 2,
    "seatNumber": "1B",
    "seatType": "Sleeper",
    "isAvailable": false,
    "finalPrice": 550.00
  }
]
```

---

#### ?? Book Ticket
Creates a new booking for selected seats.

| | |
|---|---|
| **URL** | `POST /api/Passenger/book` |
| **Auth Required** | Yes |
| **Roles Allowed** | Passenger |

**Request Body:**
```json
{
  "routeId": 1,
  "userId": 2,
  "selectedSeatIds": [1, 2, 3],
  "journeyDate": "2026-03-15",
  "boardingStopId": 1,
  "droppingStopId": 3,
  "totalPaid": 1350.00
}
```

**Success Response (200 OK):**
```json
{
  "bookingId": 1,
  "pnr": "TRPZ1638547632145",
  "status": "Confirmed",
  "totalAmount": 1350.00
}
```

**Error Response (400 Bad Request):**
```json
"Booking failed. Please check if seats are available and try again."
```

---

#### ?? Get Booking History
Returns the passenger's booking history.

| | |
|---|---|
| **URL** | `GET /api/Passenger/history/{userId}` |
| **Auth Required** | Yes |
| **Roles Allowed** | Passenger |

**Path Parameters:**
| Parameter | Type | Description |
|-----------|------|-------------|
| `userId` | int | The passenger's user ID |

**Success Response (200 OK):**
```json
[
  {
    "bookingId": 1,
    "routeName": "Mumbai to Pune",
    "busNumber": "MH01AB1234",
    "journeyDate": "2026-03-15T00:00:00",
    "status": "Confirmed",
    "amount": 450.00
  }
]
```

---

#### ? Cancel Ticket
Requests cancellation for a booking.

| | |
|---|---|
| **URL** | `POST /api/Passenger/cancel` |
| **Auth Required** | Yes |
| **Roles Allowed** | Passenger |

**Request Body:**
```json
{
  "bookingId": 1,
  "userId": 2,
  "reason": "Change of plans"
}
```

**Success Response (200 OK):**
```json
{
  "message": "Ticket cancelled successfully. Your refund request has been sent to the operator."
}
```

**Error Response (400 Bad Request):**
```json
"Unable to cancel ticket. It may already be cancelled or not found."
```

---

## Status Codes

| Code | Description |
|------|-------------|
| 200 | OK - Request successful |
| 400 | Bad Request - Invalid input or business rule violation |
| 401 | Unauthorized - Invalid or missing authentication |
| 403 | Forbidden - User lacks permission for this resource |
| 404 | Not Found - Resource does not exist |
| 500 | Internal Server Error - Unexpected server error |

---

## Booking Status Flow

```
???????????????     Passenger      ???????????????     Admin       ??????????????????????
?  Confirmed  ? ????? cancels ??????  Cancelled  ? ?? approves ????? CancellationApproved?
???????????????                    ???????????????                 ??????????????????????
                                         ?                                   ?
                                         ? Admin                             ? Operator
                                         ? rejects                           ? processes
                                         ?                                   ? refund
                                   ???????????????                           ?
                                   ?  Confirmed  ?                    ????????????
                                   ?  (reverted) ?                    ? Refunded ?
                                   ???????????????                    ????????????
```

### Status Values:
| Status | Description |
|--------|-------------|
| `Confirmed` | Booking is active and valid |
| `Cancelled` | Passenger cancelled, awaiting admin approval |
| `CancellationApproved` | Admin approved, operator can process refund |
| `Refunded` | Refund processed by operator |

---

## Amenity Management Flow

```
???????????????????     Admin creates    ???????????????????
?  AmenityMaster  ? ??????????????????????      Admin      ?
?    (WiFi, AC)   ?                      ?                 ?
???????????????????                      ???????????????????
         ?
         ? Operator assigns
         ?
???????????????????                      ???????????????????
?   BusAmenities  ? ??????????????????????    Passenger    ?
?  (Bus 1 + WiFi) ?     Visible in       ?  sees amenities ?
???????????????????     search results   ???????????????????
```

### Amenity Setup Steps:

1. **Admin** creates amenities in master list:
   ```
   POST /api/Admin/amenities
   { "amenityName": "WiFi" }
   ```

2. **Operator** views available amenities:
   ```
   GET /api/Operator/amenities
   ```

3. **Operator** assigns amenities to bus:
   ```
   POST /api/Operator/buses/1/amenities
   [1, 2, 3, 4, 5]
   ```

4. **Passenger** searches and sees amenities:
   ```
   GET /api/Passenger/search?fromCity=Mumbai&toCity=Pune&travelDate=2026-03-15
   Response: { "amenities": ["WiFi", "AC", "Charging Point"] }
   ```

---

## Testing with Swagger

### Step-by-Step Testing Flow:

1. **Register a User**
   ```
   POST /api/User/register
   ```
   Create a Passenger or Operator account.

2. **Login**
   ```
   POST /api/Auth/login
   ```
   Get the JWT token from the response.

3. **Authorize in Swagger**
   - Click the **"Authorize"** button (??) in Swagger UI
   - Enter: `Bearer <your_jwt_token>`
   - Click **"Authorize"**

4. **Access Protected APIs**
   - Now you can access role-specific endpoints
   - Admin endpoints: `/api/Admin/*`
   - Operator endpoints: `/api/Operator/*`
   - Passenger endpoints: `/api/Passenger/*`

### Example Login Response:
```json
{
  "token": "eyJhbGciOiJIUzI1NiIsInR5cCI6IkpXVCJ9.eyJodHRwOi8vc2NoZW1hcy54bWxzb2FwLm9yZy93cy8yMDA1LzA1L2lkZW50aXR5L2NsYWltcy9uYW1laWRlbnRpZmllciI6IjIiLCJodHRwOi8vc2NoZW1hcy54bWxzb2FwLm9yZy93cy8yMDA1LzA1L2lkZW50aXR5L2NsYWltcy9lbWFpbGFkZHJlc3MiOiJwYXNzZW5nZXIxQGdtYWlsLmNvbSIsImh0dHA6Ly9zY2hlbWFzLm1pY3Jvc29mdC5jb20vd3MvMjAwOC8wNi9pZGVudGl0eS9jbGFpbXMvcm9sZSI6IlBhc3NlbmdlciIsImV4cCI6MTcwOTk4NzY1NCwiaXNzIjoiVHJpcHpvQVBJIiwiYXVkIjoiVHJpcHpvQ2xpZW50In0.xxxxx",
  "email": "passenger1@gmail.com",
  "role": "Passenger",
  "fullName": "John Doe"
}
```

---

## Retrieving User Info from JWT Claims

In your controllers, you can access the authenticated user's information:

```csharp
// Get User ID
var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;

// Get Email
var email = User.FindFirst(ClaimTypes.Email)?.Value;

// Get Role
var role = User.FindFirst(ClaimTypes.Role)?.Value;
```

---

## API Summary Table

### Authentication (2 endpoints)
| Method | Endpoint | Auth | Role | Description |
|--------|----------|------|------|-------------|
| POST | `/api/Auth/login` | ? | Any | User login |
| POST | `/api/User/register` | ? | Any | User registration |

### Admin (13 endpoints)
| Method | Endpoint | Auth | Role | Description |
|--------|----------|------|------|-------------|
| GET | `/api/Admin/dashboard` | ? | Admin | Dashboard stats |
| GET | `/api/Admin/users` | ? | Admin | List all users (excludes Admin) |
| PUT | `/api/Admin/deactivate-user/{userId}` | ? | Admin | Deactivate user |
| PUT | `/api/Admin/activate-user/{userId}` | ? | Admin | Activate user |
| GET | `/api/Admin/pending-cancellations` | ? | Admin | View pending cancellations |
| PUT | `/api/Admin/approve-cancellation/{bookingId}` | ? | Admin | Approve cancellation |
| PUT | `/api/Admin/reject-cancellation/{bookingId}` | ? | Admin | Reject cancellation |
| GET | `/api/Admin/routes` | ? | Admin | List all routes |
| GET | `/api/Admin/routes/{routeId}` | ? | Admin | Get route details |
| GET | `/api/Admin/bookings` | ? | Admin | List all bookings |
| POST | `/api/Admin/amenities` | ? | Admin | Create amenity |
| GET | `/api/Admin/logs` | ? | Admin | View error logs |
| DELETE | `/api/Admin/logs/clear` | ? | Admin | Clear old logs |

### Operator (15 endpoints)
| Method | Endpoint | Auth | Role | Description |
|--------|----------|------|------|-------------|
| GET | `/api/Operator/dashboard/{operatorId}` | ? | Operator | Dashboard stats |
| POST | `/api/Operator/buses` | ? | Operator | Add bus |
| POST | `/api/Operator/buses/{busId}/seats` | ? | Operator | Configure seats |
| GET | `/api/Operator/amenities` | ? | Operator | Get all available amenities |
| GET | `/api/Operator/buses/{busId}/amenities` | ? | Operator | Get bus amenities |
| POST | `/api/Operator/buses/{busId}/amenities` | ? | Operator | Add amenities to bus |
| DELETE | `/api/Operator/buses/{busId}/amenities` | ? | Operator | Remove amenities from bus |
| POST | `/api/Operator/routes` | ? | Operator | Create route |
| GET | `/api/Operator/approved-cancellations/{operatorId}` | ? | Operator | View approved cancellations |
| POST | `/api/Operator/refund` | ? | Operator | Process refund |
| GET | `/api/Operator/fleet/{operatorId}` | ? | Operator | Get fleet |
| PATCH | `/api/Operator/buses/{busId}/status` | ? | Operator | Toggle bus status |
| POST | `/api/Operator/schedule` | ? | Operator | Create schedule |
| GET | `/api/Operator/schedules` | ? | Operator | Get schedules |
| DELETE | `/api/Operator/schedule/{scheduleId}` | ? | Operator | Delete schedule |

### Passenger (5 endpoints)
| Method | Endpoint | Auth | Role | Description |
|--------|----------|------|------|-------------|
| GET | `/api/Passenger/search` | ? | Passenger | Search buses (shows amenities) |
| GET | `/api/Passenger/seats` | ? | Passenger | Get seat map |
| POST | `/api/Passenger/book` | ? | Passenger | Book ticket |
| GET | `/api/Passenger/history/{userId}` | ? | Passenger | Booking history |
| POST | `/api/Passenger/cancel` | ? | Passenger | Cancel ticket |

---

**Total Endpoints: 35**

---

*Last Updated: March 2026*
*API Version: 1.1*
