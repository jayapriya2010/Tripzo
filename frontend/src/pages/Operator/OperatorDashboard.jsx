import React, { useState, useEffect } from 'react';
import { useNavigate } from 'react-router-dom';
import { MdDirectionsBus, MdRoute, MdBookOnline, MdTrendingUp, MdEventSeat, MdWarning } from 'react-icons/md';
import operatorService from '../../services/operator/operatorService';
import authService from '../../services/auth/authService';

const OperatorDashboard = () => {
  const [stats, setStats] = useState(null);
  const [fleet, setFleet] = useState([]);
  const [loading, setLoading] = useState(true);
  const [error, setError] = useState('');
  const navigate = useNavigate();
  const user = authService.getCurrentUser();
  const operatorId = user?.userId || user?.UserId;

  useEffect(() => {
    fetchData();
  }, []);

  const fetchData = async () => {
    try {
      setLoading(true);
      const [dashRes, fleetRes] = await Promise.allSettled([
        operatorService.getDashboard(operatorId),
        operatorService.getFleet(operatorId)
      ]);
      if (dashRes.status === 'fulfilled') setStats(dashRes.value.data);
      if (fleetRes.status === 'fulfilled') setFleet(fleetRes.value.data);
    } catch (err) {
      setError('Failed to load dashboard data.');
    } finally {
      setLoading(false);
    }
  };

  if (loading) {
    return (
      <div className="d-flex justify-content-center align-items-center" style={{ minHeight: '60vh' }}>
        <div className="spinner-border text-success" role="status">
          <span className="visually-hidden">Loading...</span>
        </div>
      </div>
    );
  }

  const statCards = [
    { label: 'Total Buses', value: stats?.totalBuses ?? 0, icon: <MdDirectionsBus size={28} />, color: '#1E63FF', bg: '#E8F0FF' },
    { label: 'Active Routes', value: stats?.totalActiveRoutes ?? 0, icon: <MdRoute size={28} />, color: '#22C55E', bg: '#DEF7EC' },
    { label: "Today's Bookings", value: stats?.bookingsToday ?? 0, icon: <MdBookOnline size={28} />, color: '#F59E0B', bg: '#FEF3C7' },
    { label: 'Monthly Revenue', value: `₹${(stats?.revenueThisMonth ?? 0).toLocaleString()}`, icon: <MdTrendingUp size={28} />, color: '#22C55E', bg: '#DEF7EC' },
    { label: 'Avg. Occupancy', value: `${(stats?.averageOccupancyRate ?? 0).toFixed(1)}%`, icon: <MdEventSeat size={28} />, color: '#8B5CF6', bg: '#EDE9FE' },
  ];

  return (
    <div>
      <div className="d-flex justify-content-between align-items-center mb-4">
        <div>
          <h4 className="fw-bold m-0">Welcome back, {user?.fullName}!</h4>
          <p className="text-muted m-0">Here's your fleet overview for today.</p>
        </div>
      </div>

      {error && <div className="alert alert-warning">{error}</div>}

      {/* Stats Cards */}
      <div className="row g-3 mb-4">
        {statCards.map((card, i) => (
          <div className="col-md-4 col-lg" key={i}>
            <div className="tripzo-card d-flex align-items-center gap-3">
              <div className="rounded-3 d-flex align-items-center justify-content-center" style={{ width: 50, height: 50, backgroundColor: card.bg, color: card.color }}>
                {card.icon}
              </div>
              <div>
                <p className="text-muted m-0" style={{ fontSize: '0.75rem', textTransform: 'uppercase', letterSpacing: '0.5px' }}>{card.label}</p>
                <h5 className="fw-bold m-0">{card.value}</h5>
              </div>
            </div>
          </div>
        ))}
      </div>

      {/* Fleet Overview */}
      <div className="row g-4">
        <div className="col-lg-12">
          <div className="tripzo-card">
            <div className="d-flex justify-content-between align-items-center mb-3">
              <h6 className="fw-bold m-0">Fleet Overview</h6>
              <button className="btn btn-sm btn-outline-primary rounded-pill px-3" onClick={() => navigate('/operator/buses')}>
                View All
              </button>
            </div>
            {fleet.length === 0 ? (
              <p className="text-muted text-center py-4">No buses registered yet. Start by adding your first bus!</p>
            ) : (
              <div className="table-responsive">
                <table className="table table-hover mb-0">
                  <thead>
                    <tr>
                      <th>Bus Name</th>
                      <th>Number</th>
                      <th>Type</th>
                      <th>Capacity</th>
                      <th>Status</th>
                    </tr>
                  </thead>
                  <tbody>
                    {fleet.slice(0, 10).map(bus => (
                      <tr key={bus.busId}>
                        <td className="fw-semibold">{bus.busName}</td>
                        <td><code>{bus.busNumber}</code></td>
                        <td>{bus.busType}</td>
                        <td>{bus.capacity}</td>
                        <td>
                          <span className={`badge-status ${bus.isActive ? 'badge-active' : 'badge-inactive'}`}>
                            {bus.isActive ? 'Active' : 'Inactive'}
                          </span>
                        </td>
                      </tr>
                    ))}
                  </tbody>
                </table>
              </div>
            )}
          </div>
        </div>
      </div>
    </div>
  );
};

export default OperatorDashboard;
