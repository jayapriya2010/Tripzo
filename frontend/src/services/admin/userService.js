import api from '../api';

const userService = {
    getUsers: async (params) => {
        const response = await api.get('/Admin/users', { params });
        return response.data;
    },
    
    getUserById: async (userId) => {
        const response = await api.get(`/Admin/users/${userId}`);
        return response.data;
    },
    
    activateUser: async (userId) => {
        const response = await api.put(`/Admin/activate-user/${userId}`);
        return response.data;
    },
    
    deactivateUser: async (userId) => {
        const response = await api.put(`/Admin/deactivate-user/${userId}`);
        return response.data;
    },

    deleteUser: async (userId) => {
        const response = await api.delete(`/Admin/users/${userId}`);
        return response.data;
    }
};

export default userService;
