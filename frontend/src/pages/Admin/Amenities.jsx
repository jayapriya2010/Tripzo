import React, { useState, useEffect } from 'react';
import AdminLayout from '../../layouts/AdminLayout';
import amenityService from '../../services/admin/amenityService';
import { MdAdd, MdCheckCircle, MdWifi, MdList, MdRefresh } from 'react-icons/md';

// Amenity icon mapping for visual variety
const amenityIcons = {
    'WiFi': '📶', 'AC': '❄️', 'Charging Port': '🔌', 'Blanket': '🛏️',
    'Water Bottle': '💧', 'Entertainment': '🎬', 'Reading Light': '💡',
    'Reclining Seats': '💺', 'Snacks': '🍿', 'GPS Tracking': '📍',
    'Pillow': '🌙', 'Sanitizer': '🧴', 'Toilet': '🚻', 'First Aid': '🩺',
};

const ALL_SUGGESTIONS = ['WiFi', 'AC', 'Charging Port', 'Blanket', 'Water Bottle', 'Entertainment', 'Reading Light', 'Reclining Seats', 'Snacks', 'GPS Tracking', 'Pillow', 'Sanitizer', 'Toilet', 'First Aid'];

const Amenities = () => {
    const [amenityName, setAmenityName] = useState('');
    const [loading, setLoading] = useState(false);
    const [fetching, setFetching] = useState(true);
    const [success, setSuccess] = useState('');
    const [error, setError] = useState('');
    const [amenities, setAmenities] = useState([]);

    const fetchAmenities = async () => {
        setFetching(true);
        try {
            const data = await amenityService.getAmenities();
            setAmenities(data || []);
        } catch {
            setAmenities([]);
        } finally {
            setFetching(false);
        }
    };

    useEffect(() => { fetchAmenities(); }, []);

    // Filter out already-added amenities from suggestions
    const existingNames = amenities.map(a => a.amenityName?.toLowerCase());
    const availableSuggestions = ALL_SUGGESTIONS.filter(s => !existingNames.includes(s.toLowerCase()));

    const handleSubmit = async (e) => {
        e.preventDefault();
        setSuccess('');
        setError('');

        if (!amenityName.trim()) {
            setError('Amenity name is required.');
            return;
        }

        if (existingNames.includes(amenityName.trim().toLowerCase())) {
            setError(`"${amenityName.trim()}" already exists in the master list.`);
            return;
        }

        setLoading(true);
        try {
            const data = await amenityService.createAmenity(amenityName.trim());
            setSuccess(data.message || 'Amenity added successfully!');
            setAmenityName('');
            await fetchAmenities(); // Refresh the list after adding
        } catch (err) {
            setError(err.response?.data?.message || err.response?.data || 'Failed to add amenity.');
        } finally {
            setLoading(false);
        }
    };

    return (
        <AdminLayout>
            <div className="d-flex align-items-center justify-content-between mb-4">
                <div>
                    <h4 className="fw-bold m-0">Manage Amenities</h4>
                    <p className="text-muted small m-0">Add new amenities to the master list (e.g., WiFi, AC, Charging Port).</p>
                </div>
            </div>

            <div className="row g-4">
                {/* Left Column: Add Form + Quick Suggestions */}
                <div className="col-md-5">
                    {/* Add Form */}
                    <div className="tripzo-card bg-white border-0 shadow-sm mb-4">
                        <h6 className="fw-bold mb-3 d-flex align-items-center gap-2">
                            <MdWifi className="text-primary" /> Add New Amenity
                        </h6>

                        {success && (
                            <div className="alert alert-success d-flex align-items-center gap-2 py-2" role="alert">
                                <MdCheckCircle /> {success}
                            </div>
                        )}
                        {error && (
                            <div className="alert alert-danger py-2" role="alert">
                                {error}
                            </div>
                        )}

                        <form onSubmit={handleSubmit}>
                            <div className="mb-3">
                                <label className="form-label text-muted fw-bold small">AMENITY NAME</label>
                                <input
                                    type="text"
                                    className="form-control bg-light border-0 shadow-none p-3"
                                    placeholder="e.g., WiFi, Blanket, USB Charging"
                                    value={amenityName}
                                    onChange={(e) => setAmenityName(e.target.value)}
                                    disabled={loading}
                                />
                            </div>
                            <button
                                type="submit"
                                className="btn btn-primary d-flex align-items-center gap-2"
                                disabled={loading}
                            >
                                {loading ? (
                                    <span className="spinner-border spinner-border-sm"></span>
                                ) : (
                                    <MdAdd size={20} />
                                )}
                                Add Amenity
                            </button>
                        </form>
                    </div>

                    {/* Quick Suggestions (only shows amenities NOT already added) */}
                    <div className="tripzo-card bg-white border-0 shadow-sm">
                        <h6 className="fw-bold mb-1">Quick Add Suggestions</h6>
                        <p className="text-muted small mb-3">Click to pre-fill the form. Already-added amenities are hidden.</p>
                        {availableSuggestions.length === 0 ? (
                            <div className="text-center py-3">
                                <MdCheckCircle size={36} className="text-success mb-2" />
                                <p className="text-muted small fw-bold m-0">All common amenities have been added!</p>
                            </div>
                        ) : (
                            <div className="d-flex flex-wrap gap-2">
                                {availableSuggestions.map((name) => (
                                    <button
                                        key={name}
                                        className="btn btn-sm btn-outline-primary rounded-pill"
                                        onClick={() => setAmenityName(name)}
                                        disabled={loading}
                                    >
                                        {amenityIcons[name] || '✨'} {name}
                                    </button>
                                ))}
                            </div>
                        )}
                    </div>
                </div>

                {/* Right Column: All Amenities in DB */}
                <div className="col-md-7">
                    <div className="tripzo-card bg-white border-0 shadow-sm h-100">
                        <div className="d-flex align-items-center justify-content-between mb-3">
                            <h6 className="fw-bold m-0 d-flex align-items-center gap-2">
                                <MdList className="text-primary" />
                                Amenity Master List
                                <span className="badge bg-primary rounded-pill ms-1">{amenities.length}</span>
                            </h6>
                            <button
                                className="btn btn-sm btn-outline-secondary rounded-pill d-flex align-items-center gap-1"
                                onClick={fetchAmenities}
                                disabled={fetching}
                            >
                                <MdRefresh size={14} className={fetching ? 'spin' : ''} /> Refresh
                            </button>
                        </div>

                        {fetching ? (
                            <div className="text-center py-5">
                                <div className="spinner-border text-primary" role="status">
                                    <span className="visually-hidden">Loading...</span>
                                </div>
                            </div>
                        ) : amenities.length === 0 ? (
                            <div className="text-center py-5 text-muted">
                                <MdList size={48} style={{ opacity: 0.15 }} className="mb-2" />
                                <p className="small fw-bold m-0">No amenities added yet.</p>
                                <p className="small m-0">Use the form to add your first amenity.</p>
                            </div>
                        ) : (
                            <div className="d-flex flex-wrap gap-2">
                                {amenities.map((a) => (
                                    <div
                                        key={a.amenityId}
                                        className="d-flex align-items-center gap-2 px-3 py-2 rounded-pill border border-2"
                                        style={{
                                            borderColor: '#E8F0FF',
                                            background: '#F5F8FF',
                                            fontSize: '0.85rem',
                                            fontWeight: 600,
                                            color: '#1E63FF'
                                        }}
                                    >
                                        <span>{amenityIcons[a.amenityName] || '✨'}</span>
                                        {a.amenityName}
                                    </div>
                                ))}
                            </div>
                        )}
                    </div>
                </div>
            </div>
        </AdminLayout>
    );
};

export default Amenities;
