import api from './api';

const cancellationService = {
  getPendingCancellations: async () => {
    const response = await api.get('/Admin/pending-cancellations');
    return response.data;
  },

  approveCancellation: async (bookingId) => {
    const response = await api.put(`/Admin/approve-cancellation/${bookingId}`);
    return response.data;
  },

  rejectCancellation: async (bookingId) => {
    const response = await api.put(`/Admin/reject-cancellation/${bookingId}`);
    return response.data;
  },
};

export default cancellationService;
