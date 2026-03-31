import api from '../api';

const routeService = {
    getRoutes: async (params) => {
        const response = await api.get('/Admin/routes', { params });
        return response.data;
    },

    getRouteById: async (routeId) => {
        const response = await api.get(`/Admin/routes/${routeId}`);
        return response.data;
    }
};

export default routeService;
