import api from './api';

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
  cancelBooking: (bookingId, userId, reason) =>
    api.post('/Passenger/cancel', { bookingId, userId, reason }),

  // Submit feedback
  submitFeedback: (payload) => api.post('/Passenger/feedback', payload),

  // Get user feedbacks
  getUserFeedbacks: (userId) => api.get(`/Passenger/feedback/${userId}`),

  // Get profile (uses user data stored in localStorage)
  getProfile: () => {
    const user = JSON.parse(localStorage.getItem('user'));
    return user;
  },

  // Update profile
  updateProfile: (userId, payload) => api.put(`/User/${userId}`, payload),
};

export default passengerService;
