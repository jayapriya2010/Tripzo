import React, { useState, useEffect } from 'react';
import AdminLayout from '../../layouts/AdminLayout';
import Table from '../../components/Admin/Table';
import routeService from '../../services/admin/routeService';
import StatsCard from '../../components/Admin/StatsCard';
import { MdRoute, MdLocationOn, MdAccessTime, MdVisibility } from 'react-icons/md';

const Routes = () => {
    const [routes, setRoutes] = useState([]);
    const [loading, setLoading] = useState(true);
    const [selectedRoute, setSelectedRoute] = useState(null);
    const [detailLoading, setDetailLoading] = useState(false);
    const [stats, setStats] = useState({
        totalRoutes: 0,
        uniqueCities: 0,
    });

    useEffect(() => {
        const fetchData = async () => {
            setLoading(true);
            try {
                const data = await routeService.getRoutes();
                const items = data.items || [];
                setRoutes(items);

                // Compute unique cities from source + destination
                const citySet = new Set();
                items.forEach(r => {
                    if (r.sourceCity) citySet.add(r.sourceCity);
                    if (r.destCity) citySet.add(r.destCity);
                });

                setStats({
                    totalRoutes: data.totalCount || items.length,
                    uniqueCities: citySet.size,
                });
            } catch (error) {
                console.error('Error fetching routes:', error);
            } finally {
                setLoading(false);
            }
        };
        fetchData();
    }, []);

    const handleViewDetails = async (route) => {
        setDetailLoading(true);
        try {
            const details = await routeService.getRouteById(route.routeId);
            setSelectedRoute(details);
        } catch (error) {
            alert(error.response?.data?.message || 'Error fetching route details');
        } finally {
            setDetailLoading(false);
        }
    };

    const columns = [
        {
            label: 'ORIGIN',
            key: 'sourceCity',
            render: (row) => (
                <div className="d-flex align-items-center gap-2">
                    <MdLocationOn className="text-primary" />
                    <span>{row.sourceCity}</span>
                </div>
            )
        },
        {
            label: 'DESTINATION',
            key: 'destCity',
            render: (row) => (
                <div className="d-flex align-items-center gap-2">
                    <MdLocationOn className="text-danger" />
                    <span>{row.destCity}</span>
                </div>
            )
        },
        { label: 'BUS NAME', key: 'busName' },
        { label: 'BUS NUMBER', key: 'busNumber' },
        { label: 'BASE FARE', key: 'baseFare', render: (row) => `₹${row.baseFare}` },
    ];

    const actions = [
        {
            label: 'View Details',
            icon: <MdVisibility />,
            onClick: handleViewDetails
        }
    ];

    return (
        <AdminLayout>
            <div className="mb-4">
                <h4 className="fw-bold m-0">Manage Routes</h4>
                <p className="text-muted small">View bus routes and stopping points.</p>
            </div>

            <div className="row g-4 mb-5">
                <div className="col-md-6">
                    <StatsCard title="Total Routes" value={stats.totalRoutes} icon={<MdRoute size={28} />} color="#1E63FF" />
                </div>
                <div className="col-md-6">
                    <StatsCard title="Unique Cities" value={stats.uniqueCities} icon={<MdLocationOn size={28} />} color="#22C55E" />
                </div>
            </div>

            <Table columns={columns} data={routes} actions={actions} isLoading={loading} />

            {/* Route Details Modal */}
            {(selectedRoute || detailLoading) && (
                <div className="modal d-block" style={{ backgroundColor: 'rgba(0,0,0,0.5)' }} onClick={() => !detailLoading && setSelectedRoute(null)}>
                    <div className="modal-dialog modal-lg modal-dialog-centered" onClick={(e) => e.stopPropagation()}>
                        <div className="modal-content border-0 shadow-lg" style={{ borderRadius: '16px' }}>
                            <div className="modal-header border-0 pb-0">
                                <h5 className="modal-title fw-bold">
                                    {detailLoading ? 'Loading...' : `${selectedRoute.sourceCity} → ${selectedRoute.destCity}`}
                                </h5>
                                <button className="btn-close" onClick={() => setSelectedRoute(null)} disabled={detailLoading}></button>
                            </div>
                            <div className="modal-body">
                                {detailLoading ? (
                                    <div className="text-center py-4">
                                        <div className="spinner-border text-primary" role="status"></div>
                                    </div>
                                ) : selectedRoute && (
                                    <>
                                        <div className="row g-3 mb-4">
                                            <div className="col-md-4">
                                                <p className="text-muted small mb-1">Origin</p>
                                                <p className="fw-bold">{selectedRoute.sourceCity}</p>
                                            </div>
                                            <div className="col-md-4">
                                                <p className="text-muted small mb-1">Destination</p>
                                                <p className="fw-bold">{selectedRoute.destCity}</p>
                                            </div>
                                            <div className="col-md-4">
                                                <p className="text-muted small mb-1">Base Fare</p>
                                                <p className="fw-bold">₹{selectedRoute.baseFare}</p>
                                            </div>
                                            <div className="col-md-4">
                                                <p className="text-muted small mb-1">Bus Name</p>
                                                <p className="fw-bold">{selectedRoute.busName}</p>
                                            </div>
                                            <div className="col-md-4">
                                                <p className="text-muted small mb-1">Bus Number</p>
                                                <p className="fw-bold">{selectedRoute.busNumber}</p>
                                            </div>
                                        </div>

                                        {/* Stops */}
                                        {selectedRoute.stops && selectedRoute.stops.length > 0 && (
                                            <>
                                                <h6 className="fw-bold mb-3">Route Stops</h6>
                                                <div className="table-responsive">
                                                    <table className="table table-sm">
                                                        <thead>
                                                            <tr>
                                                                <th>#</th>
                                                                <th>City</th>
                                                                <th>Location</th>
                                                                <th>Type</th>
                                                                <th>Arrival</th>
                                                            </tr>
                                                        </thead>
                                                        <tbody>
                                                            {selectedRoute.stops.map((stop, idx) => (
                                                                <tr key={stop.stopId || idx}>
                                                                    <td>{stop.stopOrder}</td>
                                                                    <td>{stop.cityName}</td>
                                                                    <td>{stop.locationName}</td>
                                                                    <td>
                                                                        <span className={`badge-status ${stop.stopType === 'Boarding' ? 'badge-success' : 'badge-pending'}`}>
                                                                            {stop.stopType}
                                                                        </span>
                                                                    </td>
                                                                    <td>{stop.arrivalTime}</td>
                                                                </tr>
                                                            ))}
                                                        </tbody>
                                                    </table>
                                                </div>
                                            </>
                                        )}
                                    </>
                                )}
                            </div>
                        </div>
                    </div>
                </div>
            )}
        </AdminLayout>
    );
};

export default Routes;
