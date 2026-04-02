import React, { useState, useEffect } from 'react';
import { useNavigate } from 'react-router-dom';
import { MdAdd, MdVisibility, MdRoute } from 'react-icons/md';
import operatorService from '../../services/operator/operatorService';
import authService from '../../services/auth/authService';

const ManageRoutes = () => {
  const [buses, setBuses] = useState([]);
  const [loading, setLoading] = useState(true);
  const navigate = useNavigate();
  const user = authService.getCurrentUser();
  const operatorId = user?.userId || user?.UserId;

  useEffect(() => { fetchRoutes(); }, []);

  const fetchRoutes = async () => {
    try {
      setLoading(true);
      const res = await operatorService.getAllBusesWithRoutes(operatorId);
      setBuses(res.data);
    } catch {
      setBuses([]);
    } finally {
      setLoading(false);
    }
  };

  // Flatten all routes from all buses
  const allRoutes = buses.flatMap(bus =>
    (bus.routes || []).map(route => ({
      ...route,
      routeId: route.routeId || route.RouteId,
      busName: bus.busName,
      busNumber: bus.busNumber,
      busId: bus.busId
    }))
  );

  if (loading) {
    return (
      <div className="d-flex justify-content-center align-items-center" style={{ minHeight: '60vh' }}>
        <div className="spinner-border text-success" role="status"><span className="visually-hidden">Loading...</span></div>
      </div>
    );
  }

  return (
    <div>
      <div className="d-flex justify-content-between align-items-center mb-4">
        <div>
          <h4 className="fw-bold m-0">Manage Routes</h4>
          <p className="text-muted m-0">Create and view your bus routes</p>
        </div>
        <button className="btn btn-success rounded-pill px-4 d-flex align-items-center gap-2" onClick={() => navigate('/operator/routes/add')}>
          <MdAdd size={20} /> Add Route
        </button>
      </div>

      {/* Stats */}
      <div className="row g-3 mb-4">
        <div className="col-md-4">
          <div className="tripzo-card text-center">
            <h3 className="fw-bold text-success">{allRoutes.length}</h3>
            <p className="text-muted m-0 small">Total Routes</p>
          </div>
        </div>
        <div className="col-md-4">
          <div className="tripzo-card text-center">
            <h3 className="fw-bold text-primary">{buses.length}</h3>
            <p className="text-muted m-0 small">Buses with Routes</p>
          </div>
        </div>
        <div className="col-md-4">
          <div className="tripzo-card text-center">
            <h3 className="fw-bold text-warning">
              {new Set(allRoutes.flatMap(r => [r.sourceCity, r.destCity])).size}
            </h3>
            <p className="text-muted m-0 small">Unique Cities</p>
          </div>
        </div>
      </div>

      {/* Routes Table */}
      <div className="tripzo-card border-top border-4 border-primary shadow-sm">
        {allRoutes.length === 0 ? (
          <div className="text-center py-5">
            <MdRoute size={64} className="text-muted mb-3" style={{ opacity: 0.2 }} />
            <p className="text-muted">No routes found. Create your first route!</p>
          </div>
        ) : (
          <div className="table-responsive">
            <table className="table table-hover mb-0">
              <thead className="bg-light">
                <tr>
                  <th className="border-0">Source → Destination</th>
                  <th className="border-0">Base Fare</th>
                  <th className="border-0">Assigned Bus</th>
                  <th className="border-0">Stops</th>
                  <th className="border-0">Actions</th>
                </tr>
              </thead>
              <tbody>
                {allRoutes.map(route => (
                  <tr key={route.routeId}>
                    <td className="fw-bold text-dark">{route.sourceCity} → {route.destCity}</td>
                    <td className="fw-semibold text-primary">₹{route.baseFare}</td>
                    <td>
                      <div className="d-flex flex-column">
                        <span className="fw-semibold small">{route.busName}</span>
                        <code className="text-muted x-small" style={{ fontSize: '0.7rem' }}>{route.busNumber}</code>
                      </div>
                    </td>
                    <td><span className="badge bg-primary bg-opacity-10 text-primary rounded-pill px-3">{route.totalStops || 0} Stops</span></td>
                    <td>
                      <button
                        className="btn btn-sm btn-primary rounded-pill px-3 shadow-sm"
                        onClick={() => navigate(`/operator/routes/${route.routeId}`)}
                      >
                        <MdVisibility size={16} className="me-1" /> View Details
                      </button>
                    </td>
                  </tr>
                ))}
              </tbody>
            </table>
          </div>
        )}
      </div>
    </div>
  );
};

export default ManageRoutes;
