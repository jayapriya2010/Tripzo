import React, { useState, useEffect } from 'react';
import { useNavigate } from 'react-router-dom';
import {
  MdSearch, MdDirectionsBus, MdHistory, MdConfirmationNumber,
  MdCheckCircle, MdCancel, MdPending, MdStar, MdVisibility
} from 'react-icons/md';
import PassengerLayout from '../../layouts/PassengerLayout';
import TicketModal from '../../components/Passenger/TicketModal';
import authService from '../../services/auth/authService';
import passengerService from '../../services/passenger/passengerService';

const PassengerDashboard = () => {
  const navigate = useNavigate();
  const user = authService.getCurrentUser();
  const [bookings, setBookings] = useState([]);
  const [loading, setLoading] = useState(true);
  const [searchForm, setSearchForm] = useState({ fromCity: '', toCity: '', travelDate: '' });
  const [viewTicketId, setViewTicketId] = useState(null);

  useEffect(() => {
    if (user?.userId) {
      passengerService.getHistory(user.userId, { pageSize: 10, pageNumber: 1 })
        .then(res => setBookings(res.data.items || []))
        .catch(() => setBookings([]))
        .finally(() => setLoading(false));
    } else {
      setLoading(false);
    }
  }, []);

  const handleSearch = (e) => {
    e.preventDefault();
    if (!searchForm.fromCity || !searchForm.toCity || !searchForm.travelDate) return;
    navigate('/passenger/search', { state: searchForm });
  };

  const statusBadge = (status) => {
    const map = {
      Confirmed: { cls: 'badge-active', icon: <MdCheckCircle size={13} /> },
      Completed: { cls: 'badge-active', icon: <MdCheckCircle size={13} /> },
      Cancelled: { cls: 'badge-inactive', icon: <MdCancel size={13} /> },
      CancellationApproved: { cls: 'badge-inactive', icon: <MdCancel size={13} /> },
    };
    const b = map[status] || { cls: 'badge-pending', icon: <MdPending size={13} /> };
    return (
      <span className={`badge-status ${b.cls} d-inline-flex align-items-center gap-1`}>
        {b.icon} {status}
      </span>
    );
  };

  const activeTickets = bookings.filter(b => b.status === 'Confirmed');
  const totalBookings = bookings.length;

  return (
    <PassengerLayout>
      {/* Welcome Banner */}
      <div
        className="rounded-4 p-4 mb-4 text-white position-relative overflow-hidden"
        style={{ background: 'linear-gradient(135deg, #1E63FF 0%, #0F3D91 100%)', minHeight: '140px' }}
      >
        <div className="position-relative" style={{ zIndex: 2 }}>
          <h4 className="fw-bold mb-1">Welcome back, {user?.fullName || 'Traveller'}! 👋</h4>
          <p className="mb-0 opacity-75">Where are you heading today?</p>
        </div>
        <MdDirectionsBus
          size={140}
          style={{ position: 'absolute', right: '-10px', bottom: '-20px', opacity: 0.08 }}
        />
      </div>

      {/* Quick Search Card */}
      <div className="tripzo-card mb-4">
        <h5 className="fw-bold mb-3 d-flex align-items-center gap-2">
          <MdSearch color="var(--primary-blue)" /> Quick Search
        </h5>
        <form onSubmit={handleSearch}>
          <div className="row g-3 align-items-end">
            <div className="col-md-3">
              <label className="form-label fw-semibold small text-muted">FROM</label>
              <input
                type="text"
                className="form-control rounded-3"
                placeholder="e.g., Chennai"
                value={searchForm.fromCity}
                onChange={e => setSearchForm({ ...searchForm, fromCity: e.target.value })}
              />
            </div>
            <div className="col-md-3">
              <label className="form-label fw-semibold small text-muted">TO</label>
              <input
                type="text"
                className="form-control rounded-3"
                placeholder="e.g., Bangalore"
                value={searchForm.toCity}
                onChange={e => setSearchForm({ ...searchForm, toCity: e.target.value })}
              />
            </div>
            <div className="col-md-3">
              <label className="form-label fw-semibold small text-muted">DATE</label>
              <input
                type="date"
                className="form-control rounded-3"
                min={new Date().toISOString().split('T')[0]}
                value={searchForm.travelDate}
                onChange={e => setSearchForm({ ...searchForm, travelDate: e.target.value })}
              />
            </div>
            <div className="col-md-3">
              <button type="submit" className="btn btn-primary w-100 rounded-3 py-2 fw-semibold">
                <MdSearch className="me-1" /> Search Buses
              </button>
            </div>
          </div>
        </form>
      </div>

      {/* Stats Row */}
      <div className="row g-3 mb-4">
        {[
          { label: 'Total Bookings', value: totalBookings, color: '#1E63FF', bg: '#E8F0FF', icon: <MdConfirmationNumber size={28} /> },
          { label: 'Active Tickets', value: activeTickets.length, color: '#22C55E', bg: '#DCFCE7', icon: <MdCheckCircle size={28} /> },
          { label: 'Cancelled', value: bookings.filter(b => b.status === 'Cancelled' || b.status === 'CancellationApproved').length, color: '#EF4444', bg: '#FEE2E2', icon: <MdCancel size={28} /> },
        ].map((stat) => (
          <div className="col-md-4" key={stat.label}>
            <div className="tripzo-card d-flex align-items-center gap-3 mb-0">
              <div className="rounded-3 d-flex align-items-center justify-content-center"
                style={{ width: 56, height: 56, background: stat.bg, color: stat.color, flexShrink: 0 }}>
                {stat.icon}
              </div>
              <div>
                <p className="text-muted small mb-0">{stat.label}</p>
                <h3 className="fw-bold mb-0" style={{ color: stat.color }}>{stat.value}</h3>
              </div>
            </div>
          </div>
        ))}
      </div>

      {/* Active Tickets */}
      {activeTickets.length > 0 && (
        <div className="mb-4">
          <h5 className="fw-bold mb-3 d-flex align-items-center gap-2">
            <MdConfirmationNumber color="var(--success-green)" /> Active Tickets
          </h5>
          <div className="row g-3">
            {activeTickets.slice(0, 3).map(ticket => (
              <div className="col-md-4" key={ticket.bookingId}>
                <div className="tripzo-card mb-0 border-start border-4" style={{ borderColor: 'var(--success-green) !important' }}>
                  <div className="d-flex justify-content-between align-items-start mb-2">
                    <span className="fw-bold small">{ticket.routeName}</span>
                    {statusBadge(ticket.status)}
                  </div>
                  <p className="text-muted small mb-1">
                    🚌 {ticket.busNumber}
                  </p>
                  <p className="text-muted small mb-2">
                    📅 {new Date(ticket.journeyDate).toLocaleDateString('en-IN', { day: '2-digit', month: 'short', year: 'numeric' })}
                  </p>
                  <div className="d-flex justify-content-between align-items-center mt-2 pt-2 border-top">
                    <span className="fw-bold" style={{ color: 'var(--primary-blue)' }}>₹{ticket.amount}</span>
                    <button className="btn btn-sm btn-link text-primary p-0 text-decoration-none fw-bold small d-flex align-items-center gap-1"
                      onClick={() => setViewTicketId(ticket.bookingId)}>
                      <MdVisibility size={14} /> View Ticket
                    </button>
                  </div>
                </div>
              </div>
            ))}
          </div>
        </div>
      )}

      {/* Booking History Table */}
      <div className="tripzo-card">
        <div className="d-flex align-items-center justify-content-between mb-3">
          <h5 className="fw-bold mb-0 d-flex align-items-center gap-2">
            <MdHistory color="var(--primary-blue)" /> Booking History
          </h5>
          <button className="btn btn-sm btn-outline-primary rounded-3" onClick={() => navigate('/passenger/bookings')}>
            View All
          </button>
        </div>

        {loading ? (
          <div className="text-center py-4">
            <div className="spinner-border text-primary" role="status" />
          </div>
        ) : bookings.length === 0 ? (
          <div className="text-center py-5">
            <MdDirectionsBus size={56} color="#CBD5E1" />
            <p className="text-muted mt-2">No bookings yet. Start your first journey!</p>
            <button className="btn btn-primary rounded-3" onClick={() => navigate('/passenger/search')}>
              Search Buses
            </button>
          </div>
        ) : (
          <div className="table-container">
            <table className="table table-hover mb-0">
              <thead>
                <tr>
                  <th>ROUTE</th>
                  <th>BUS</th>
                  <th>DATE</th>
                  <th>AMOUNT</th>
                  <th>STATUS</th>
                </tr>
              </thead>
              <tbody>
                {bookings.slice(0, 5).map(b => (
                  <tr key={b.bookingId}>
                    <td className="fw-semibold">{b.routeName}</td>
                    <td className="text-muted">{b.busNumber}</td>
                    <td>{new Date(b.journeyDate).toLocaleDateString('en-IN')}</td>
                    <td className="fw-bold" style={{ color: 'var(--primary-blue)' }}>₹{b.amount}</td>
                    <td>{statusBadge(b.status)}</td>
                  </tr>
                ))}
              </tbody>
            </table>
          </div>
        )}
      </div>
      {/* Ticket Modal */}
      {viewTicketId && (
        <TicketModal 
          bookingId={viewTicketId} 
          onClose={() => setViewTicketId(null)} 
        />
      )}
    </PassengerLayout>
  );
};

export default PassengerDashboard;
