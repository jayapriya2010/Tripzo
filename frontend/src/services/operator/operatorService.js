import api from '../api';

const operatorService = {
  // Dashboard
  getDashboard: (operatorId) => api.get(`/Operator/dashboard/${operatorId}`),

  // Fleet Management
  getFleet: (operatorId) => api.get(`/Operator/fleet/${operatorId}`),
  addBus: (data) => api.post('/Operator/buses', data),
  toggleBusStatus: (busId, isActive) => api.patch(`/Operator/buses/${busId}/status?isActive=${isActive}`),
  getBusDetail: (busId, operatorId) => api.get(`/Operator/bus/${busId}?operatorId=${operatorId}`),
  getAllBusesWithRoutes: (operatorId) => api.get(`/Operator/allBuses/${operatorId}`),

  // Seat Configuration
  configureSeats: (busId, seats) => api.post(`/Operator/buses/${busId}/seats`, seats),

  // Amenities
  getAllAmenities: () => api.get('/Operator/amenities'),
  getBusAmenities: (busId) => api.get(`/Operator/buses/${busId}/amenities`),
  addAmenitiesToBus: (busId, amenityIds) => api.post(`/Operator/buses/${busId}/amenities`, amenityIds),
  removeAmenitiesFromBus: (busId, amenityIds) => api.delete(`/Operator/buses/${busId}/amenities`, { data: amenityIds }),

  // Routes
  createRoute: (data) => api.post('/Operator/routes', data),
  getRouteDetails: (routeId) => api.get(`/Operator/route-detail/${routeId}`),

  // Schedules
  createSchedule: (data) => api.post('/Operator/schedule', data),
  getSchedules: (operatorId) => api.get(`/Operator/schedules?operatorId=${operatorId}`),
  getSchedulesByBus: (busId, operatorId) => api.get(`/Operator/schedules/${busId}?operatorId=${operatorId}`),
  deleteSchedule: (scheduleId) => api.delete(`/Operator/schedule/${scheduleId}`),
  reactivateSchedule: (scheduleId) => api.post(`/Operator/schedule/reactivate/${scheduleId}`),

  // Refunds
  getApprovedCancellations: (operatorId) => api.get(`/Operator/approved-cancellations/${operatorId}`),
  processRefund: (data) => api.post('/Operator/refund', data),

  // New Management Features
  getBusBookingStatus: (busId, operatorId) => api.get(`/Operator/buses/${busId}/bookings?operatorId=${operatorId}`),
  // Feedbacks
  getFeedbacks: (operatorId) => api.get(`/Operator/feedbacks/${operatorId}`),
  getFeedbackSummary: (operatorId) => api.get(`/Operator/feedbacks/${operatorId}/summary`),
  respondToFeedback: (data) => api.post('/Operator/feedbacks/respond', data),
  
  reassignBus: (data) => api.put('/Operator/schedule/reassign', data),
};

export default operatorService;
