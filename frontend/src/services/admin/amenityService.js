import api from '../api';

const amenityService = {
    createAmenity: async (amenityName) => {
        const response = await api.post('/Admin/amenities', { amenityName });
        return response.data;
    },
    getAmenities: async () => {
        const response = await api.get('/Admin/amenities');
        return response.data;
    }
};

export default amenityService;
