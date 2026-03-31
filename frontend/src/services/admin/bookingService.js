import api from '../api';

const bookingService = {
    getAllBookings: async () => {
        const response = await api.get('/Admin/bookings');
        return response.data;
    },

    approveCancellation: async (bookingId) => {
        const response = await api.put(`/Admin/approve-cancellation/${bookingId}`);
        return response.data;
    }
};

export default bookingService;
