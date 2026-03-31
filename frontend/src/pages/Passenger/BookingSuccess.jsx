import React from 'react';
import { useNavigate, useLocation } from 'react-router-dom';
import { MdCheckCircle, MdDownload, MdDashboard, MdDirectionsBus } from 'react-icons/md';
import PassengerLayout from '../../layouts/PassengerLayout';

const BookingSuccess = () => {
  const navigate = useNavigate();
  const location = useLocation();
  const { booking, fromCity, toCity, bus } = location.state || {};

  if (!booking) {
    return (
      <PassengerLayout>
        <div className="text-center py-5">
          <p className="text-muted">No booking data found.</p>
          <button className="btn btn-primary" onClick={() => navigate('/passenger/dashboard')}>
            Go to Dashboard
          </button>
        </div>
      </PassengerLayout>
    );
  }

  const handleDownloadTicket = () => {
    const ticketContent = `
TRIPZO - BUS TICKET
====================
PNR: ${booking.pnr}
Booking ID: ${booking.bookingId}
Status: ${booking.status}

Route: ${fromCity} → ${toCity}
Bus: ${bus?.busName || '—'}
Amount Paid: ₹${booking.totalAmount}
====================
Thank you for travelling with Tripzo!
    `.trim();
    const blob = new Blob([ticketContent], { type: 'text/plain' });
    const url = URL.createObjectURL(blob);
    const a = document.createElement('a');
    a.href = url;
    a.download = `Tripzo_Ticket_${booking.pnr}.txt`;
    a.click();
    URL.revokeObjectURL(url);
  };

  return (
    <PassengerLayout>
      <div className="row justify-content-center">
        <div className="col-md-7 col-lg-6">
          {/* Success Animation */}
          <div className="text-center mb-4">
            <div
              className="rounded-circle d-inline-flex align-items-center justify-content-center mb-3"
              style={{
                width: 96, height: 96,
                background: 'linear-gradient(135deg, #22C55E, #16A34A)',
                boxShadow: '0 0 32px rgba(34,197,94,0.3)',
              }}>
              <MdCheckCircle size={56} color="white" />
            </div>
            <h3 className="fw-bold text-success mb-1">Booking Confirmed!</h3>
            <p className="text-muted">Your ticket has been booked successfully. A confirmation email has been sent.</p>
          </div>

          {/* Ticket Card */}
          <div className="mb-4 position-relative" style={{
            background: 'white',
            borderRadius: 16,
            boxShadow: '0 8px 32px rgba(0,0,0,0.12)',
            overflow: 'hidden',
          }}>
            {/* Top Banner */}
            <div style={{ background: 'linear-gradient(135deg, #1E63FF 0%, #0F3D91 100%)', padding: '24px 28px', color: 'white' }}>
              <div className="d-flex align-items-center justify-content-between">
                <div className="d-flex align-items-center gap-2">
                  <MdDirectionsBus size={28} />
                  <span className="fw-bold fs-5">Tripzo</span>
                </div>
                <span className="badge rounded-pill bg-white bg-opacity-25 px-3 py-2">
                  {booking.status}
                </span>
              </div>
              <div className="d-flex align-items-center justify-content-center gap-4 mt-3">
                <div className="text-center">
                  <div className="fw-bold fs-4">{fromCity}</div>
                  <div className="opacity-75 small">{bus?.departureTime?.slice(0, 5) || 'Dep.'}</div>
                </div>
                <div className="text-center flex-grow-1">
                  <div style={{ borderTop: '2px dashed rgba(255,255,255,0.4)', position: 'relative' }}>
                    <MdDirectionsBus size={20} className="position-absolute" style={{ top: -11, left: '50%', transform: 'translateX(-50%)' }} />
                  </div>
                </div>
                <div className="text-center">
                  <div className="fw-bold fs-4">{toCity}</div>
                  <div className="opacity-75 small">Arrival</div>
                </div>
              </div>
            </div>

            {/* Ticket Notch */}
            <div className="d-flex align-items-center" style={{ margin: '0 -1px' }}>
              <div style={{ width: 24, height: 24, borderRadius: '50%', background: '#F8FAFC', border: '2px solid #E2E8F0', flexShrink: 0 }} />
              <div style={{ flex: 1, borderTop: '2px dashed #E2E8F0' }} />
              <div style={{ width: 24, height: 24, borderRadius: '50%', background: '#F8FAFC', border: '2px solid #E2E8F0', flexShrink: 0 }} />
            </div>

            {/* Ticket Details */}
            <div className="p-4">
              <div className="row g-3">
                <div className="col-6">
                  <p className="text-muted small mb-0">PNR Number</p>
                  <p className="fw-bold mb-0" style={{ color: 'var(--primary-blue)', fontSize: '1rem', letterSpacing: '1px' }}>
                    {booking.pnr}
                  </p>
                </div>
                <div className="col-6 text-end">
                  <p className="text-muted small mb-0">Amount Paid</p>
                  <p className="fw-bold mb-0 fs-5" style={{ color: '#22C55E' }}>₹{booking.totalAmount}</p>
                </div>
                <div className="col-6">
                  <p className="text-muted small mb-0">Bus Name</p>
                  <p className="fw-semibold mb-0">{bus?.busName || '—'}</p>
                </div>
                <div className="col-6 text-end">
                  <p className="text-muted small mb-0">Bus Type</p>
                  <p className="fw-semibold mb-0">{bus?.busType || '—'}</p>
                </div>
              </div>

              {/* Barcode (decorative) */}
              <div className="text-center mt-3 pt-3" style={{ borderTop: '1px solid #E2E8F0' }}>
                <div style={{ display: 'flex', gap: 2, justifyContent: 'center', height: 40 }}>
                  {Array.from({ length: 30 }, (_, i) => (
                    <div key={i} style={{
                      width: i % 3 === 0 ? 3 : 1.5,
                      height: '100%',
                      background: '#1a202c',
                      opacity: Math.random() > 0.4 ? 1 : 0.4,
                    }} />
                  ))}
                </div>
                <p className="small text-muted mt-1 mb-0" style={{ letterSpacing: '2px' }}>{booking.pnr}</p>
              </div>
            </div>
          </div>

          {/* Action Buttons */}
          <div className="d-flex gap-3">
            <button
              className="btn btn-outline-primary rounded-3 py-2 flex-grow-1 fw-semibold"
              onClick={handleDownloadTicket}>
              <MdDownload className="me-2" /> Download Ticket
            </button>
            <button
              className="btn btn-primary rounded-3 py-2 flex-grow-1 fw-semibold"
              onClick={() => navigate('/passenger/dashboard')}>
              <MdDashboard className="me-2" /> Back to Dashboard
            </button>
          </div>
        </div>
      </div>
    </PassengerLayout>
  );
};

export default BookingSuccess;
