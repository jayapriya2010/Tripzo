# 🚌 Tripzo API Documentation

**Online Bus Ticket Booking System**
Base URL: `https://localhost:7xxx/api`
Authentication: JWT Bearer Token
Framework: ASP.NET Core Web API (.NET 10)
Payment Gateway: Razorpay (Test Mode)

---

## Table of Contents

- [Authentication](#1-authentication)
- [User Registration](#2-user-registration)
- [Passenger APIs](#3-passenger-apis)
- [Admin APIs](#4-admin-apis)
- [Operator APIs](#5-operator-apis)
- [Payment Flow](#6-payment-flow-razorpay)
- [Cancellation & Refund Flow](#7-cancellation--refund-flow)
- [Error Handling](#8-error-handling)
- [Status Codes](#9-status-codes)

---

## 1. Authentication

### `POST /api/Auth/login`
**Access:** Public (No token required)

Authenticates a user and returns a JWT token.

**Request Body:**
```json
{
  "email": "passenger@example.com",
  "password": "SecurePassword123"
}
```

**Success Response (200):**
```json
{
  "token": "eyJhbGciOiJIUzI1NiIsInR5cCI6IkpXVCJ9...",
  "email": "passenger@example.com",
  "role": "Passenger",
  "fullName": "John Doe"
}
```

**Error Responses:**
| Status | Message |
|--------|---------|
| 400 | Invalid login request |
| 401 | Invalid email or password |
| 401 | Your account has been deactivated |

---

## 2. User Registration

### `POST /api/User/register`
**Access:** Public (No token required)

Registers a new Passenger or Operator account.

**Request Body:**
```json
{
  "fullName": "John Doe",
  "email": "john@example.com",
  "password": "SecurePassword123",
  "phoneNumber": "9876543210",
  "role": "Passenger",
  "gender": "Male"
}
```

| Field | Type | Required | Notes |
|-------|------|----------|-------|
| fullName | string | ✅ | Full name of the user |
| email | string | ✅ | Must be unique |
| password | string | ✅ | Will be hashed before storage |
| phoneNumber | string | ✅ | 10-digit phone number |
| role | string | ✅ | `Passenger` or `Operator` only |
| gender | string | ✅ | `Male`, `Female`, or `Other` |

**Success Response (200):**
```json
{
  "message": "Passenger registered successfully!"
}
```

---

## 3. Passenger APIs

> **Authorization:** `Bearer <token>` — Role: `Passenger`

---

### 3.1 `GET /api/Passenger/search`

Search for available buses between two cities on a specific date.

**Query Parameters:**
| Parameter | Type | Required | Description |
|-----------|------|----------|-------------|
| fromCity | string | ✅ | Source city |
| toCity | string | ✅ | Destination city |
| travelDate | date | ✅ | Format: `YYYY-MM-DD` (must be today or future) |
| busType | string | ❌ | Filter: `AC`, `Non-AC`, `Sleeper`, etc. |
| minFare | decimal | ❌ | Minimum fare filter |
| maxFare | decimal | ❌ | Maximum fare filter |
| amenities | string | ❌ | Comma-separated: `WiFi,AC,Charging` |
| pageNumber | int | ❌ | Default: 1 |
| pageSize | int | ❌ | Default: 10 (max: 50) |

**Example:** `GET /api/Passenger/search?fromCity=Chennai&toCity=Bangalore&travelDate=2026-04-18`

**Success Response (200):**
```json
{
  "items": [
    {
      "routeId": 5,
      "busId": 3,
      "busName": "Royal Travels",
      "busType": "AC Sleeper",
      "departureTime": "22:00:00",
      "fare": 550.00,
      "amenities": ["WiFi", "Charging Port", "Blanket"],
      "availableSeats": 12,
      "averageRating": 4.3,
      "totalReviews": 28
    }
  ],
  "meta": {
    "totalItems": 3,
    "pageNumber": 1,
    "pageSize": 10,
    "totalPages": 1
  }
}
```

---

### 3.2 `GET /api/Passenger/seats`

Get the seat layout with availability for a specific bus, route, and date.

**Query Parameters:**
| Parameter | Type | Required | Description |
|-----------|------|----------|-------------|
| busId | int | ✅ | Bus ID from search results |
| routeId | int | ✅ | Route ID from search results |
| travelDate | date | ✅ | Journey date |

**Example:** `GET /api/Passenger/seats?busId=3&routeId=5&travelDate=2026-04-18`

**Success Response (200):**
```json
[
  {
    "seatId": 26,
    "seatNumber": "1A",
    "seatType": "Sleeper",
    "isAvailable": true,
    "finalPrice": 600.00
  },
  {
    "seatId": 27,
    "seatNumber": "1B",
    "seatType": "Sleeper",
    "isAvailable": false,
    "finalPrice": 600.00
  },
  {
    "seatId": 28,
    "seatNumber": "2A",
    "seatType": "Seater",
    "isAvailable": true,
    "finalPrice": 500.00
  }
]
```

> **Note:** `finalPrice = BaseFare + AddonFare` (calculated server-side per seat)

---

### 3.3 `POST /api/Passenger/create-order` ⚡ Payment Step 1

Calculate total fare server-side and create a Razorpay payment order.

**Request Body:**
```json
{
  "routeId": 5,
  "busId": 3,
  "selectedSeatIds": [26, 28],
  "journeyDate": "2026-04-18",
  "boardingStopId": 17,
  "droppingStopId": 19
}
```

| Field | Type | Required | Description |
|-------|------|----------|-------------|
| routeId | int | ✅ | Route from search |
| busId | int | ✅ | Bus from search (scheduled bus) |
| selectedSeatIds | int[] | ✅ | Array of seat IDs to book |
| journeyDate | date | ✅ | Must be today or future |
| boardingStopId | int | ✅ | Must be a "Boarding" type stop |
| droppingStopId | int | ✅ | Must be a "Dropping" type stop |

**Success Response (200):**
```json
{
  "orderId": "order_PxQ1r2s3t4u5v6",
  "amount": 1100.00,
  "currency": "INR",
  "razorpayKeyId": "rzp_test_SWWgsvGf2ZuUCW"
}
```

> **Important:** The `amount` is calculated server-side (`BaseFare + AddonFare` per seat). The client does NOT send the amount.

---

### 3.4 `POST /api/Passenger/verify-payment` ⚡ Payment Step 2

Verify the Razorpay payment signature and confirm the booking.

**Request Body:**
```json
{
  "razorpayOrderId": "order_PxQ1r2s3t4u5v6",
  "razorpayPaymentId": "pay_AbC1d2e3f4g5h6",
  "razorpaySignature": "a1b2c3d4e5f6...hmac_sha256_signature",
  "routeId": 5,
  "busId": 3,
  "userId": 1,
  "selectedSeatIds": [26, 28],
  "journeyDate": "2026-04-18",
  "boardingStopId": 17,
  "droppingStopId": 19
}
```

| Field | Type | Required | Description |
|-------|------|----------|-------------|
| razorpayOrderId | string | ✅ | From Step 1 response |
| razorpayPaymentId | string | ✅ | From Razorpay checkout callback |
| razorpaySignature | string | ✅ | HMAC-SHA256 signature for verification |
| routeId | int | ✅ | Same as Step 1 |
| busId | int | ✅ | Same as Step 1 |
| userId | int | ✅ | Passenger's user ID |
| selectedSeatIds | int[] | ✅ | Same as Step 1 |
| journeyDate | date | ✅ | Same as Step 1 |
| boardingStopId | int | ✅ | Same as Step 1 |
| droppingStopId | int | ✅ | Same as Step 1 |

**Success Response (200):**
```json
{
  "bookingId": 10,
  "pnr": "TRPZ1035292898",
  "status": "Confirmed",
  "totalAmount": 1100.00
}
```

**Side Effects:**
- ✅ Booking record created in database
- ✅ Payment record stored with Razorpay order/payment IDs
- ✅ Seats linked to booking
- 📧 Confirmation email sent with PDF ticket attachment

---

### 3.5 `GET /api/Passenger/history/{userId}`

Retrieve booking history for a specific passenger.

**Example:** `GET /api/Passenger/history/1`

**Success Response (200):**
```json
[
  {
    "bookingId": 10,
    "routeName": "Chennai to Bangalore",
    "busNumber": "TN01AB1234",
    "journeyDate": "2026-04-18T00:00:00",
    "status": "Confirmed",
    "amount": 1100.00
  }
]
```

---

### 3.6 `POST /api/Passenger/cancel`

Request cancellation of a confirmed booking. Requires admin approval before refund.

**Request Body:**
```json
{
  "bookingId": 10,
  "userId": 1
}
```

**Success Response (200):**
```json
{
  "message": "Cancellation request submitted. Awaiting admin approval."
}
```

**Side Effects:**
- Booking status changes to `Cancelled`
- 📧 Cancellation request email sent to passenger

---

### 3.7 `POST /api/Passenger/feedback`

Submit feedback for a completed journey.

**Request Body:**
```json
{
  "bookingId": 10,
  "rating": 4,
  "comment": "Great journey! Bus was clean and on time."
}
```

| Field | Type | Required | Validation |
|-------|------|----------|------------|
| bookingId | int | ✅ | Must belong to the logged-in user |
| rating | int | ✅ | 1 to 5 |
| comment | string | ❌ | Max 500 characters |

**Prerequisites:**
- Journey date must have passed
- Booking must not be cancelled
- No prior feedback for this booking

**Success Response (200):**
```json
{
  "feedbackId": 5,
  "bookingId": 10,
  "routeName": "Chennai to Bangalore",
  "busName": "Royal Travels",
  "journeyDate": "2026-04-18T00:00:00",
  "rating": 4,
  "comment": "Great journey! Bus was clean and on time.",
  "createdAt": "2026-04-20T10:30:00Z"
}
```

---

### 3.8 `GET /api/Passenger/feedback/{userId}`

Get all feedbacks submitted by a specific user.

---

### 3.9 `GET /api/Passenger/feedback/bus/{busId}`

Get feedback summary and all reviews for a specific bus.

**Success Response (200):**
```json
{
  "busId": 3,
  "busName": "Royal Travels",
  "averageRating": 4.3,
  "totalReviews": 28,
  "feedbacks": [
    {
      "rating": 5,
      "comment": "Excellent service!",
      "passengerName": "John",
      "journeyDate": "2026-04-18"
    }
  ]
}
```

---

## 4. Admin APIs

> **Authorization:** `Bearer <token>` — Role: `Admin`

---

### 4.1 `GET /api/Admin/dashboard`

Admin overview with system-wide statistics.

**Success Response (200):**
```json
{
  "totalPassengers": 150,
  "activeOperators": 12,
  "totalBuses": 45,
  "totalRevenue": 125000.00,
  "todaysBookings": 23,
  "systemErrorsLast24Hours": 0
}
```

---

### 4.2 `GET /api/Admin/users`

Get all users with pagination and filters.

**Query Parameters:**
| Parameter | Type | Required | Description |
|-----------|------|----------|-------------|
| pageNumber | int | ❌ | Default: 1 |
| pageSize | int | ❌ | Default: 10 |
| role | string | ❌ | `Passenger` or `Operator` |
| isActive | bool | ❌ | Filter by account status |
| gender | string | ❌ | `Male`, `Female`, `Other` |
| searchTerm | string | ❌ | Search by name or email |
| sortBy | string | ❌ | `email`, `role`, `isactive`, `userid` |
| sortDescending | bool | ❌ | Default: false |

**Success Response (200):**
```json
{
  "items": [
    {
      "userId": 1,
      "fullName": "John Doe",
      "email": "john@example.com",
      "phoneNumber": "9876543210",
      "role": "Passenger",
      "gender": "Male",
      "isActive": true
    }
  ],
  "totalCount": 50,
  "pageNumber": 1,
  "pageSize": 10
}
```

---

### 4.3 `GET /api/Admin/users/{userId}`

Get detailed user information (uses Stored Procedure).

---

### 4.4 `PUT /api/Admin/deactivate-user/{userId}`

Deactivate a user account (soft delete). Admin accounts cannot be deactivated.

---

### 4.5 `PUT /api/Admin/activate-user/{userId}`

Re-activate a previously deactivated user account.

---

### 4.6 `GET /api/Admin/pending-cancellations`

View all booking cancellation requests awaiting admin approval.

**Success Response (200):**
```json
[
  {
    "bookingId": 10,
    "passengerName": "John Doe",
    "passengerEmail": "john@example.com",
    "routeName": "Chennai to Bangalore",
    "busNumber": "TN01AB1234",
    "journeyDate": "2026-04-18T00:00:00",
    "totalAmount": 1100.00,
    "cancellationDate": "2026-04-15T10:00:00",
    "status": "Cancelled"
  }
]
```

---

### 4.7 `PUT /api/Admin/approve-cancellation/{bookingId}`

Approve a cancellation request. Changes status to `CancellationApproved`.

**Side Effects:**
- Booking status → `CancellationApproved`
- 📧 Approval email sent to passenger
- Operator can now process the refund

---

### 4.8 `PUT /api/Admin/reject-cancellation/{bookingId}`

Reject a cancellation request. Restores booking to `Confirmed`.

**Side Effects:**
- Booking status → `Confirmed` (restored)
- 📧 Rejection email sent to passenger

---

### 4.9 `GET /api/Admin/routes`

Get all routes with pagination and filters.

**Query Parameters:**
| Parameter | Type | Required | Description |
|-----------|------|----------|-------------|
| sourceCity | string | ❌ | Filter by source |
| destCity | string | ❌ | Filter by destination |
| busName | string | ❌ | Filter by bus name |
| busNumber | string | ❌ | Filter by bus number |
| minFare | decimal | ❌ | Minimum fare |
| maxFare | decimal | ❌ | Maximum fare |
| searchTerm | string | ❌ | Global search |
| pageNumber | int | ❌ | Default: 1 |
| pageSize | int | ❌ | Default: 10 |

---

### 4.10 `GET /api/Admin/routes/{routeId}`

Get detailed route information including stops (uses Stored Procedure).

---

### 4.11 `GET /api/Admin/bookings`

Global booking audit list — view all bookings in the system.

---

### 4.12 `POST /api/Admin/amenities`

Add a new amenity to the master list.

**Request Body:**
```json
{
  "amenityName": "WiFi"
}
```

---

## 5. Operator APIs

> **Authorization:** `Bearer <token>` — Role: `Operator`

---

### 5.1 `GET /api/Operator/dashboard/{operatorId}`

Operator dashboard with performance metrics.

**Success Response (200):**
```json
{
  "totalBuses": 5,
  "totalActiveRoutes": 8,
  "bookingsToday": 14,
  "revenueThisMonth": 45000.00
}
```

---

### 5.2 `POST /api/Operator/buses`

Register a new bus.

**Request Body:**
```json
{
  "busName": "Royal Travels",
  "busNumber": "TN01AB1234",
  "busType": "AC Sleeper",
  "capacity": 40,
  "operatorId": 2
}
```

---

### 5.3 `POST /api/Operator/buses/{busId}/seats`

Define the seat layout for a bus.

**Request Body:**
```json
[
  {
    "seatNumber": "1A",
    "seatType": "Sleeper",
    "addonFare": 50.00
  },
  {
    "seatNumber": "2A",
    "seatType": "Seater",
    "addonFare": 0.00
  }
]
```

> **Note:** `FinalPrice = Route.BaseFare + Seat.AddonFare`

---

### 5.4 `GET /api/Operator/amenities`

List all available amenities in the system.

---

### 5.5 `GET /api/Operator/buses/{busId}/amenities`

Get amenities assigned to a specific bus.

---

### 5.6 `POST /api/Operator/buses/{busId}/amenities`

Add amenities to a bus.

**Request Body:**
```json
[1, 3, 5]
```

---

### 5.7 `DELETE /api/Operator/buses/{busId}/amenities`

Remove amenities from a bus.

**Request Body:**
```json
[3, 5]
```

---

### 5.8 `POST /api/Operator/routes`

Create a route with stops.

**Request Body:**
```json
{
  "busId": 3,
  "sourceCity": "Chennai",
  "destCity": "Bangalore",
  "baseFare": 550.00,
  "stops": [
    {
      "cityName": "Chennai",
      "locationName": "CMBT Bus Stand",
      "stopType": "Boarding",
      "stopOrder": 1,
      "arrivalTime": "22:00:00"
    },
    {
      "cityName": "Vellore",
      "locationName": "Vellore Bus Stop",
      "stopType": "Boarding",
      "stopOrder": 2,
      "arrivalTime": "00:30:00"
    },
    {
      "cityName": "Bangalore",
      "locationName": "Majestic Bus Stand",
      "stopType": "Dropping",
      "stopOrder": 3,
      "arrivalTime": "05:00:00"
    }
  ]
}
```

---

### 5.9 `POST /api/Operator/schedule`

Schedule a bus on specific dates for a route.

**Request Body:**
```json
{
  "routeId": 5,
  "busId": 3,
  "scheduledDates": [
    "2026-04-18",
    "2026-04-19",
    "2026-04-20"
  ]
}
```

**Success Response (200):**
```json
[
  {
    "scheduleId": 10,
    "routeName": "Chennai to Bangalore",
    "busName": "Royal Travels",
    "scheduledDate": "2026-04-18T00:00:00",
    "isActive": true
  }
]
```

---

### 5.10 `GET /api/Operator/schedules?operatorId={id}`

Get all active schedules for an operator.

---

### 5.11 `GET /api/Operator/schedules/{busId}?operatorId={id}`

Get schedules for a specific bus.

---

### 5.12 `DELETE /api/Operator/schedule/{scheduleId}`

Deactivate a schedule. Checks for active bookings first.

**Conflict Response (409) — If bookings exist:**
```json
{
  "success": false,
  "message": "Cannot deactivate. 3 active booking(s) exist.",
  "hasActiveBookings": true,
  "activeBookingsCount": 3,
  "scheduleId": 10,
  "busId": 3,
  "busName": "Royal Travels"
}
```

---

### 5.13 `PUT /api/Operator/schedule/reassign`

Reassign a different bus to a schedule. Transfers all existing bookings.

**Request Body:**
```json
{
  "scheduleId": 10,
  "newBusId": 5
}
```

**Validations:**
- Both buses must belong to the same operator
- New bus must have matching seat numbers for all booked seats
- New bus must not already be scheduled for the same route/date

---

### 5.14 `PATCH /api/Operator/buses/{busId}/status?isActive={true|false}`

Activate or deactivate a bus (soft delete).

---

### 5.15 `GET /api/Operator/fleet/{operatorId}`

List all buses belonging to the operator.

---

### 5.16 `GET /api/Operator/allBuses/{operatorId}`

Get all buses with their routes and amenities.

---

### 5.17 `GET /api/Operator/bus/{busId}?operatorId={id}`

Get detailed bus information including seats, routes, and occupancy.

---

### 5.18 `GET /api/Operator/buses/{busId}/bookings?operatorId={id}`

Get booking status and revenue for a specific bus across all schedules.

---

### 5.19 `GET /api/Operator/approved-cancellations/{operatorId}`

View cancellations approved by admin, ready for refund processing.

---

### 5.20 `POST /api/Operator/refund` 💰

Process refund for an admin-approved cancellation. Triggers Razorpay refund automatically.

**Request Body:**
```json
{
  "bookingId": 10,
  "refundAmount": 1100.00
}
```

**Success Response (200):**
```json
{
  "message": "Refund processed successfully."
}
```

**Side Effects:**
- Booking status → `Refunded`
- Payment status → `Refunded`
- 💰 Razorpay Refund API triggered (money returned to customer's original payment method)
- Razorpay refund ID stored in database
- 📧 Refund confirmation email sent to passenger

---

### 5.21 `GET /api/Operator/feedbacks/{operatorId}`

Get all feedbacks for the operator's buses.

---

### 5.22 `GET /api/Operator/feedbacks/{operatorId}/summary`

Get feedback summary with rating breakdown.

**Success Response (200):**
```json
{
  "totalFeedbacks": 28,
  "pendingResponses": 5,
  "averageRating": 4.3,
  "fiveStarCount": 12,
  "fourStarCount": 8,
  "threeStarCount": 5,
  "twoStarCount": 2,
  "oneStarCount": 1
}
```

---

### 5.23 `POST /api/Operator/feedbacks/respond`

Respond to a passenger's feedback.

**Request Body:**
```json
{
  "feedbackId": 5,
  "response": "Thank you for your feedback! We're glad you enjoyed the journey."
}
```

---

## 6. Payment Flow (Razorpay)

```
┌─────────┐     Step 1: create-order      ┌──────────┐     Create Order      ┌──────────┐
│  Client  │ ──────────────────────────▶   │  Server  │ ───────────────────▶  │ Razorpay │
│ (React)  │                               │ (API)    │ ◀───────────────────  │   API    │
│          │ ◀─────────────────────────    │          │     order_id          │          │
│          │   { orderId, amount, keyId }   │          │                       │          │
│          │                               │          │                       │          │
│          │     Open Checkout Popup        │          │                       │          │
│          │ ─────────────────────────────────────────────────────────────▶   │          │
│          │ ◀─────────────────────────────────────────────────────────────   │          │
│          │   { paymentId, signature }     │          │                       │          │
│          │                               │          │                       │          │
│          │  Step 2: verify-payment        │          │                       │          │
│          │ ──────────────────────────▶   │          │                       │          │
│          │                               │ Verify   │                       │          │
│          │                               │ HMAC-256 │                       │          │
│          │                               │ Create   │                       │          │
│          │ ◀─────────────────────────    │ Booking  │                       │          │
│          │   { bookingId, PNR }           │          │                       │          │
└─────────┘                               └──────────┘                       └──────────┘
```

### Security Features:
- ✅ Amount is **never** sent by the client — always calculated server-side
- ✅ Payment signature verified using HMAC-SHA256 before creating booking
- ✅ Amount recalculated during verification to prevent race conditions
- ✅ All operations run inside database transactions

### Test Mode Credentials:
| Parameter | Value |
|-----------|-------|
| Test Card | `4111 1111 1111 1111` |
| Expiry | Any future date |
| CVV | Any 3 digits |
| OTP | `1234` |

---

## 7. Cancellation & Refund Flow

```
 Passenger                Admin                  Operator               Razorpay
    │                       │                       │                       │
    │  POST /cancel         │                       │                       │
    │──────────────▶        │                       │                       │
    │  Status: "Cancelled"  │                       │                       │
    │  📧 Request email     │                       │                       │
    │                       │                       │                       │
    │                       │  GET /pending-         │                       │
    │                       │  cancellations         │                       │
    │                       │                       │                       │
    │                       │  PUT /approve-         │                       │
    │                       │  cancellation/{id}     │                       │
    │                       │──────────────▶        │                       │
    │  📧 Approval email    │  Status:              │                       │
    │◀──────────────────────│  "CancellationApproved"│                       │
    │                       │                       │                       │
    │                       │                       │  GET /approved-        │
    │                       │                       │  cancellations/{id}    │
    │                       │                       │                       │
    │                       │                       │  POST /refund          │
    │                       │                       │──────────────────────▶│
    │                       │                       │  Refund API            │
    │                       │                       │◀──────────────────────│
    │  📧 Refund email      │                       │  refund_id             │
    │◀──────────────────────────────────────────────│                       │
    │  Status: "Refunded"   │                       │  💰 Money returned     │
    │                       │                       │                       │
```

### Booking Status Lifecycle:
```
Confirmed → Cancelled → CancellationApproved → Refunded
                ↓
          (Rejected → Confirmed)
```

---

## 8. Error Handling

All error responses follow a consistent format:

```json
{
  "message": "Human-readable error description."
}
```

### Common Validations:
| Validation | Error Message |
|------------|--------------|
| Past journey date | "Journey date cannot be in the past." |
| Same boarding/dropping | "Boarding and dropping stops cannot be the same." |
| Invalid seat IDs | "Seats [26, 27] do not belong to bus Royal Travels" |
| Seats already booked | "Seats [26] are already booked" |
| Invalid stop type | "Stop 'XYZ' is not a boarding point." |
| Payment verification fail | "Payment verification failed. Invalid signature." |
| Refund exceeds amount | "Refund amount cannot exceed the original booking amount." |

---

## 9. Status Codes

| Code | Meaning | Usage |
|------|---------|-------|
| 200 | OK | Successful request |
| 400 | Bad Request | Validation errors, business rule violations |
| 401 | Unauthorized | Missing or invalid JWT token |
| 403 | Forbidden | Token valid but wrong role |
| 404 | Not Found | Resource doesn't exist |
| 409 | Conflict | Schedule has active bookings |

---

## 📧 Email Notifications

| Event | Recipient | Content |
|-------|-----------|---------|
| Booking Confirmed | Passenger | PDF ticket attachment |
| Cancellation Requested | Passenger | Under review notice |
| Cancellation Approved | Passenger | Refund pending notice |
| Cancellation Rejected | Passenger | Booking restored notice |
| Refund Processed | Passenger | Refund amount & timeline |

---

## 🔐 Authentication Header

All protected endpoints require the JWT token in the Authorization header:

```
Authorization: Bearer eyJhbGciOiJIUzI1NiIsInR5cCI6IkpXVCJ9...
```

---

*Generated for Tripzo v1.0 — Last updated: March 2026*
