import React, { useState, useEffect } from 'react';
import { MdConfirmationNumber, MdCheckCircle, MdCancel, MdPending, MdSearch, MdVisibility } from 'react-icons/md';
import PassengerLayout from '../../layouts/PassengerLayout';
import TicketModal from '../../components/Passenger/TicketModal';
import authService from '../../services/auth/authService';
import passengerService from '../../services/passenger/passengerService';

const MyBookings = () => {
  const user = authService.getCurrentUser();
  const [bookings, setBookings] = useState([]);
  const [loading, setLoading] = useState(true);
  const [search, setSearch] = useState('');
  const [cancelling, setCancelling] = useState(null);
  const [toast, setToast] = useState(null);
  const [showCancelModal, setShowCancelModal] = useState(false);
  const [selectedBookingId, setSelectedBookingId] = useState(null);
  const [cancelReason, setCancelReason] = useState('');
  const [viewTicketId, setViewTicketId] = useState(null);

  useEffect(() => {
    fetchBookings();
  }, []);

  const fetchBookings = () => {
    if (!user?.userId) { setLoading(false); return; }
    setLoading(true);
    passengerService.getHistory(user.userId)
      .then(res => setBookings(res.data || []))
      .catch(() => setBookings([]))
      .finally(() => setLoading(false));
  };

  const openCancelModal = (bookingId) => {
    setSelectedBookingId(bookingId);
    setCancelReason('');
    setShowCancelModal(true);
  };

  const confirmCancel = async () => {
    if (!cancelReason.trim()) {
      showToast('Please provide a reason for cancellation.', 'warning');
      return;
    }
    
    setCancelling(selectedBookingId);
    setShowCancelModal(false);
    
    try {
      await passengerService.cancelBooking(selectedBookingId, user.userId, cancelReason);
      showToast('Cancellation request submitted. Awaiting admin approval.', 'success');
      fetchBookings();
    } catch (err) {
      showToast(err.response?.data?.message || 'Failed to cancel booking.', 'danger');
    } finally {
      setCancelling(null);
      setSelectedBookingId(null);
    }
  };

  const showToast = (msg, type) => {
    setToast({ msg, type });
    setTimeout(() => setToast(null), 4000);
  };

  const statusBadge = (status) => {
    const map = {
      Confirmed: { cls: 'badge-active', icon: <MdCheckCircle size={13} /> },
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

  const filtered = bookings.filter(b =>
    b.routeName?.toLowerCase().includes(search.toLowerCase()) ||
    b.busNumber?.toLowerCase().includes(search.toLowerCase()) ||
    b.status?.toLowerCase().includes(search.toLowerCase())
  );

  return (
    <PassengerLayout>
      {/* Toast */}
      {toast && (
        <div className={`alert alert-${toast.type} position-fixed top-0 end-0 m-3 shadow`}
          style={{ zIndex: 9999, minWidth: 300 }}>
          {toast.msg}
        </div>
      )}

      <div className="d-flex align-items-center justify-content-between mb-4 flex-wrap gap-3">
        <h4 className="fw-bold mb-0 d-flex align-items-center gap-2">
          <MdConfirmationNumber color="var(--primary-blue)" /> My Bookings
        </h4>
        <div className="position-relative" style={{ minWidth: 240 }}>
          <MdSearch
            className="position-absolute"
            style={{ top: '50%', left: 12, transform: 'translateY(-50%)', color: '#9CA3AF' }}
          />
          <input
            type="text"
            className="form-control rounded-3"
            placeholder="Search bookings..."
            style={{ paddingLeft: 36 }}
            value={search}
            onChange={e => setSearch(e.target.value)}
          />
        </div>
      </div>

      <div className="tripzo-card">
        {loading ? (
          <div className="text-center py-5"><div className="spinner-border text-primary" /></div>
        ) : filtered.length === 0 ? (
          <div className="text-center py-5">
            <MdConfirmationNumber size={56} color="#CBD5E1" />
            <p className="text-muted mt-2 mb-0">No bookings found.</p>
          </div>
        ) : (
          <div className="table-container">
            <table className="table table-hover mb-0">
              <thead>
                <tr>
                  <th>ROUTE</th>
                  <th>BUS NUMBER</th>
                  <th>DATE</th>
                  <th>AMOUNT</th>
                  <th>STATUS</th>
                  <th>ACTION</th>
                </tr>
              </thead>
              <tbody>
                {filtered.map(b => (
                  <tr key={b.bookingId}>
                    <td className="fw-semibold">{b.routeName}</td>
                    <td className="text-muted">{b.busNumber}</td>
                    <td>{new Date(b.journeyDate).toLocaleDateString('en-IN', { day: '2-digit', month: 'short', year: 'numeric' })}</td>
                    <td className="fw-bold" style={{ color: 'var(--primary-blue)' }}>₹{b.amount}</td>
                    <td>{statusBadge(b.status)}</td>
                    <td>
                      <div className="d-flex gap-2">
                        {b.status === 'Confirmed' && (
                          <button
                            className="btn btn-sm btn-outline-primary rounded-3 d-flex align-items-center gap-1"
                            onClick={() => setViewTicketId(b.bookingId)}>
                            <MdVisibility size={14} /> Ticket
                          </button>
                        )}
                        {b.status === 'Confirmed' ? (
                          <button
                            className="btn btn-sm btn-outline-danger rounded-3"
                            onClick={() => openCancelModal(b.bookingId)}
                            disabled={cancelling === b.bookingId}>
                            {cancelling === b.bookingId
                              ? <span className="spinner-border spinner-border-sm" />
                              : 'Cancel'}
                          </button>
                        ) : (
                          <span className="text-muted small">—</span>
                        )}
                      </div>
                    </td>
                  </tr>
                ))}
              </tbody>
            </table>
          </div>
        )}
      </div>

      {/* Cancellation Reason Modal */}
      {showCancelModal && (
        <div className="modal fade show d-block" style={{ backgroundColor: 'rgba(0,0,0,0.5)' }}>
          <div className="modal-dialog modal-dialog-centered">
            <div className="modal-content border-0 rounded-4 shadow">
              <div className="modal-header border-0 pb-0">
                <h5 className="fw-bold m-0 text-danger">Cancel Booking</h5>
                <button type="button" className="btn-close" onClick={() => setShowCancelModal(false)}></button>
              </div>
              <div className="modal-body py-4">
                <p className="text-muted mb-3">Please provide a reason for canceling this booking. This helps us improve our service.</p>
                <div className="form-group">
                  <label className="small fw-bold text-muted mb-2">REASON FOR CANCELLATION</label>
                  <textarea 
                    className="form-control rounded-3 border-2" 
                    rows="3" 
                    placeholder="e.g. Changed travel plans, Medical emergency..."
                    value={cancelReason}
                    onChange={(e) => setCancelReason(e.target.value)}
                  ></textarea>
                </div>
              </div>
              <div className="modal-footer border-0 pt-0">
                <button className="btn btn-light rounded-3 px-4 fw-semibold" onClick={() => setShowCancelModal(false)}>Back</button>
                <button className="btn btn-danger rounded-3 px-4 fw-bold" onClick={confirmCancel}>Confirm Cancellation</button>
              </div>
            </div>
          </div>
        </div>
      )}
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

export default MyBookings;
