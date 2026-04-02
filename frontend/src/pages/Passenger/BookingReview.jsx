import React, { useState } from 'react';
import { useNavigate, useLocation } from 'react-router-dom';
import { MdDirectionsBus, MdChair, MdPerson, MdArrowForward, MdArrowBack } from 'react-icons/md';
import PassengerLayout from '../../layouts/PassengerLayout';
import authService from '../../services/auth/authService';
import passengerService from '../../services/passenger/passengerService';

const BookingReview = () => {
  const navigate = useNavigate();
  const location = useLocation();
  const {
    busId, routeId, travelDate, bus,
    fromCity, toCity,
    selectedSeats, boardingStop, droppingStop, totalAmount
  } = location.state || {};

  const user = authService.getCurrentUser();
  const [loading, setLoading] = useState(false);
  const [error, setError] = useState('');

  const [primaryEmail, setPrimaryEmail] = useState(user?.email || '');
  const [passengers, setPassengers] = useState(
    (selectedSeats || []).map((seat) => ({
      seatId: seat.seatId,
      seatNumber: seat.seatNumber,
      name: '',
      age: '',
      gender: '',
      phone: '',
    }))
  );

  const handlePassengerChange = (index, field, value) => {
    const updated = [...passengers];
    updated[index][field] = value;
    setPassengers(updated);
  };

  const baseFare = totalAmount || 0;
  const tax = parseFloat((baseFare * 0.05).toFixed(2));  // 5% GST
  const grandTotal = baseFare + tax;

  const handleProceedToPayment = async () => {
    if (!primaryEmail) {
      setError('Primary contact email is required.');
      return;
    }

    const incomplete = passengers.some(p => !p.name || !p.age || !p.gender || !p.phone);
    if (incomplete) {
      setError('Please fill in all traveler details.');
      return;
    }

    setLoading(true);
    setError('');
    try {
      const payload = {
        routeId,
        busId,
        passengers: passengers.map(p => ({
          seatId: p.seatId,
          name: p.name,
          age: parseInt(p.age),
          gender: p.gender,
          phone: p.phone,
        })),
        primaryEmail,
        journeyDate: travelDate,
        boardingStopId: parseInt(boardingStop),
        droppingStopId: parseInt(droppingStop),
      };
      
      const res = await passengerService.createOrder(payload);
      navigate('/passenger/payment', {
        state: {
          orderData: res.data,
          busId, routeId, travelDate,
          selectedSeats, boardingStop, droppingStop,
          fromCity, toCity, bus,
          passengers,
          primaryEmail,
          grandTotal,
        }
      });
    } catch (err) {
      setError(err.response?.data?.message || 'Failed to create order. Please try again.');
    } finally {
      setLoading(false);
    }
  };

  return (
    <PassengerLayout>
      <div className="d-flex align-items-center gap-2 mb-4">
        <button className="btn btn-outline-primary btn-sm rounded-circle p-1 d-flex align-items-center justify-content-center"
          onClick={() => navigate(-1)} style={{ width: 32, height: 32 }}>
          <MdArrowBack size={20} />
        </button>
        <h4 className="fw-bold mb-0">Review Your Booking</h4>
      </div>

      <div className="row g-4">
        {/* Left Column */}
        <div className="col-md-7">
          {/* Trip Summary */}
          <div className="tripzo-card mb-4">
            <h6 className="fw-bold mb-3 d-flex align-items-center gap-2">
              <MdDirectionsBus color="var(--primary-blue)" /> Trip Summary
            </h6>
            <div className="d-flex align-items-center gap-3 p-3 rounded-3" style={{ background: '#E8F0FF' }}>
              <div className="text-center">
                <div className="fw-bold fs-5">{fromCity}</div>
                <div className="text-muted small">{bus?.departureTime?.slice(0, 5)}</div>
              </div>
              <div className="flex-grow-1 text-center">
                <MdArrowForward size={24} style={{ color: 'var(--primary-blue)' }} />
                <div className="text-muted small">{bus?.busType}</div>
              </div>
              <div className="text-center">
                <div className="fw-bold fs-5">{toCity}</div>
                <div className="text-muted small">Arrival</div>
              </div>
            </div>
            <div className="row g-3 mt-2">
              <div className="col-6">
                <p className="small text-muted mb-0">Bus Name</p>
                <p className="fw-semibold mb-0">{bus?.busName}</p>
              </div>
              <div className="col-6">
                <p className="small text-muted mb-0">Journey Date</p>
                <p className="fw-semibold mb-0">
                  {travelDate ? new Date(travelDate).toLocaleDateString('en-IN', { day: '2-digit', month: 'short', year: 'numeric' }) : '—'}
                </p>
              </div>
              <div className="col-6">
                <p className="small text-muted mb-0">Boarding Point</p>
                <p className="fw-semibold mb-0">{boardingStop ? `Stop #${boardingStop}` : '—'}</p>
              </div>
              <div className="col-6">
                <p className="small text-muted mb-0">Dropping Point</p>
                <p className="fw-semibold mb-0">{droppingStop ? `Stop #${droppingStop}` : '—'}</p>
              </div>
            </div>
          </div>

          {/* Selected Seats */}
          <div className="tripzo-card mb-4">
            <h6 className="fw-bold mb-3 d-flex align-items-center gap-2">
              <MdChair color="var(--primary-blue)" /> Selected Seats
            </h6>
            <div className="d-flex flex-wrap gap-2">
              {(selectedSeats || []).map(seat => (
                <div key={seat.seatId}
                  className="rounded-3 px-3 py-2 d-flex align-items-center gap-2"
                  style={{ background: '#E8F0FF', color: '#1E63FF' }}>
                  <MdChair size={16} />
                  <span className="fw-bold small">{seat.seatNumber}</span>
                  <span className="text-muted small">({seat.seatType})</span>
                  <span className="fw-bold small ms-1">₹{seat.finalPrice}</span>
                </div>
              ))}
            </div>
          </div>

          {/* Primary Contact */}
          <div className="tripzo-card mb-4">
            <h6 className="fw-bold mb-3 d-flex align-items-center gap-2">
              <MdPerson color="var(--primary-blue)" /> Primary Contact (For Ticket)
            </h6>
            <div className="row">
              <div className="col-md-12">
                <label className="form-label small fw-semibold text-muted">PRIMARY EMAIL</label>
                <input type="email" className="form-control rounded-3"
                  placeholder="Enter email to receive ticket"
                  value={primaryEmail}
                  onChange={e => setPrimaryEmail(e.target.value)} />
              </div>
            </div>
          </div>

          {/* Individual Travelers */}
          <h6 className="fw-bold mb-3 mt-4">Traveler Details</h6>
          {passengers.map((p, idx) => (
            <div className="tripzo-card mb-3 border-start border-4" key={idx} style={{ borderLeftColor: 'var(--primary-blue) !important' }}>
              <div className="d-flex justify-content-between align-items-center mb-3">
                <h6 className="fw-bold mb-0 d-flex align-items-center gap-2">
                  <MdChair className="text-primary" /> Seat {p.seatNumber}
                </h6>
              </div>
              <div className="row g-3">
                <div className="col-md-6">
                  <label className="form-label small fw-semibold text-muted">FULL NAME</label>
                  <input type="text" className="form-control rounded-3"
                    placeholder="Enter traveler name"
                    value={p.name}
                    onChange={e => handlePassengerChange(idx, 'name', e.target.value)} />
                </div>
                <div className="col-md-6">
                  <label className="form-label small fw-semibold text-muted">PHONE NUMBER</label>
                  <input type="tel" className="form-control rounded-3"
                    placeholder="Enter phone number"
                    value={p.phone}
                    onChange={e => handlePassengerChange(idx, 'phone', e.target.value)} />
                </div>
                <div className="col-md-6">
                  <label className="form-label small fw-semibold text-muted">AGE</label>
                  <input type="number" className="form-control rounded-3"
                    placeholder="Enter age"
                    value={p.age}
                    onChange={e => handlePassengerChange(idx, 'age', e.target.value)} />
                </div>
                <div className="col-md-6">
                   <label className="form-label small fw-semibold text-muted">GENDER</label>
                   <select className="form-select rounded-3"
                     value={p.gender}
                     onChange={e => handlePassengerChange(idx, 'gender', e.target.value)}>
                     <option value="">Select</option>
                     <option value="Male">Male</option>
                     <option value="Female">Female</option>
                     <option value="Other">Other</option>
                   </select>
                </div>
              </div>
            </div>
          ))}
        </div>

        {/* Right Column — Fare Summary */}
        <div className="col-md-5">
          <div className="tripzo-card position-sticky" style={{ top: 80 }}>
            <h6 className="fw-bold mb-3">Fare Summary</h6>
            <div className="border-bottom pb-3 mb-3">
              {(selectedSeats || []).map(seat => (
                <div key={seat.seatId} className="d-flex justify-content-between small mb-1">
                  <span className="text-muted">Seat {seat.seatNumber}</span>
                  <span>₹{seat.finalPrice}</span>
                </div>
              ))}
            </div>
            <div className="d-flex justify-content-between small mb-1">
              <span className="text-muted">Base Fare ({selectedSeats?.length} seat{selectedSeats?.length !== 1 ? 's' : ''})</span>
              <span>₹{baseFare.toFixed(2)}</span>
            </div>
            <div className="d-flex justify-content-between small mb-2">
              <span className="text-muted">GST (5%)</span>
              <span>₹{tax.toFixed(2)}</span>
            </div>
            <div className="border-top pt-3">
              <div className="d-flex justify-content-between fw-bold">
                <span>Total Amount</span>
                <span style={{ color: 'var(--primary-blue)', fontSize: '1.2rem' }}>₹{grandTotal.toFixed(2)}</span>
              </div>
            </div>

            <div className="mt-3 p-2 rounded-3" style={{ background: '#DCFCE7' }}>
              <p className="small mb-0" style={{ color: '#166534' }}>
                ✅ Secure payment via Razorpay. Your booking will be confirmed instantly after payment.
              </p>
            </div>

            {error && <div className="alert alert-danger small py-2 mt-3">{error}</div>}

            <button
              className="btn btn-primary w-100 rounded-3 py-2 fw-bold mt-3"
              onClick={handleProceedToPayment}
              disabled={loading}>
              {loading ? (
                <><span className="spinner-border spinner-border-sm me-2" /> Processing...</>
              ) : (
                'Proceed to Payment →'
              )}
            </button>
          </div>
        </div>
      </div>
    </PassengerLayout>
  );
};

export default BookingReview;
