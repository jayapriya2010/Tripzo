import api from '../api';

const authService = {
  login: async (credentials) => {
    const response = await api.post('/Auth/login', credentials);
    if (response.data.token) {
      localStorage.setItem('token', response.data.token);
      localStorage.setItem('user', JSON.stringify(response.data));
    }
    return response.data;
  },
  
  register: async (userData) => {
    const response = await api.post('/User/register', userData);
    return response.data;
  },
  
  logout: () => {
    localStorage.removeItem('token');
    localStorage.removeItem('user');
    window.location.href = '/login';
  },
  
  getCurrentUser: () => {
    const user = localStorage.getItem('user');
    return user ? JSON.parse(user) : null;
  },
  
  isAdmin: () => {
    const user = JSON.parse(localStorage.getItem('user'));
    return (user?.role || user?.Role) === 'Admin';
  },

  forgotPassword: (email) => {
    return api.post('/Auth/forgot-password', { email });
  },

  verifyOtp: (email, otp) => {
    return api.post('/Auth/verify-otp', { email, otp });
  },

  resetPassword: (payload) => {
    return api.post('/Auth/reset-password', payload);
  }
};

export default authService;
