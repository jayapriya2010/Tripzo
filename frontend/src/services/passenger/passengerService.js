import api from '../api';

const passengerService = {
  // Search buses
  searchBuses: (params) => api.get('/Passenger/search', { params }),

  // Get seat layout
  getSeats: (busId, routeId, travelDate) =>
    api.get('/Passenger/seats', { params: { busId, routeId, travelDate } }),

  // Booking history
  getHistory: (userId) => api.get(`/Passenger/history/${userId}`),

  // Create Razorpay order
  createOrder: (payload) => api.post('/Passenger/create-order', payload),

  // Verify payment
  verifyPayment: (payload) => api.post('/Passenger/verify-payment', payload),

  // Cancel booking
  cancelBooking: (bookingId, userId, reason, selectedSeatIds = null) =>
    api.post('/Passenger/cancel', { bookingId, userId, reason, selectedSeatIds }),

  // Submit feedback
  submitFeedback: (payload) => api.post('/Passenger/feedback', payload),

  // Get user feedbacks
  getUserFeedbacks: (userId) => api.get(`/Passenger/feedback/${userId}`),

  // Get bus feedbacks
  getBusFeedbacks: (busId) => api.get(`/Passenger/feedback/bus/${busId}`),

  // Get profile (uses user data stored in localStorage)
  getProfile: () => {
    const user = JSON.parse(localStorage.getItem('user'));
    return user;
  },

  // Update profile
  updateProfile: (userId, payload) => api.put(`/User/${userId}`, payload),

  // Get full ticket details
  getTicketDetails: (bookingId) => api.get(`/Passenger/ticket/${bookingId}`),
};

export default passengerService;
