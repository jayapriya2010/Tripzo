import api from './api';

const bookingService = {
  getBookings: async () => {
    const response = await api.get('/Admin/bookings');
    return response.data;
  },

  getDashboardStats: async () => {
    const response = await api.get('/Admin/dashboard');
    return response.data;
  },
};

export default bookingService;
