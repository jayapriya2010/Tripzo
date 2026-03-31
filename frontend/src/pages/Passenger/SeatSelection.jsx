import React, { useState, useEffect } from 'react';
import { useNavigate, useLocation } from 'react-router-dom';
import { MdChair, MdArrowForward, MdInfo, MdArrowBack } from 'react-icons/md';
import PassengerLayout from '../../layouts/PassengerLayout';
import passengerService from '../../services/passengerService';

const SeatSelection = () => {
  const navigate = useNavigate();
  const location = useLocation();
  const { busId, routeId, travelDate, bus, fromCity, toCity } = location.state || {};

  const [seats, setSeats] = useState([]);
  const [selectedSeats, setSelectedSeats] = useState([]);
  const [loading, setLoading] = useState(true);
  const [boardingStop, setBoardingStop] = useState('');
  const [droppingStop, setDroppingStop] = useState('');
  const [tooltip, setTooltip] = useState(null);
  const [error, setError] = useState('');

  useEffect(() => {
    if (!busId || !routeId || !travelDate) return;
    passengerService.getSeats(busId, routeId, travelDate)
      .then(res => setSeats(res.data || []))
      .catch(() => setError('Failed to load seats. Please try again.'))
      .finally(() => setLoading(false));
  }, []);

  const toggleSeat = (seat) => {
    if (!seat.isAvailable) return;
    setSelectedSeats(prev =>
      prev.find(s => s.seatId === seat.seatId)
        ? prev.filter(s => s.seatId !== seat.seatId)
        : [...prev, seat]
    );
  };

  const totalAmount = selectedSeats.reduce((sum, s) => sum + (s.finalPrice || 0), 0);

  // Build a grid: group seats by row number (e.g. "1A","1B" -> row 1)
  const buildGrid = () => {
    const rowMap = {};
    seats.forEach(seat => {
      const match = seat.seatNumber.match(/^(\d+)([A-Za-z]+)$/);
      if (match) {
        const rowNum = match[1];
        if (!rowMap[rowNum]) rowMap[rowNum] = [];
        rowMap[rowNum].push(seat);
      }
    });
    // Sort seats within each row by column letter
    Object.keys(rowMap).forEach(row => {
      rowMap[row].sort((a, b) => {
        const la = a.seatNumber.replace(/\d+/, '');
        const lb = b.seatNumber.replace(/\d+/, '');
        return la.localeCompare(lb);
      });
    });
    return rowMap;
  };

  const getSeatClass = (seat) => {
    if (!seat.isAvailable) return 'seat-booked';
    if (selectedSeats.find(s => s.seatId === seat.seatId)) return 'seat-selected';
    return 'seat-available';
  };

  const gridRows = buildGrid();
  const sortedRowKeys = Object.keys(gridRows).sort((a, b) => parseInt(a) - parseInt(b));

  const handleProceed = () => {
    if (selectedSeats.length === 0) {
      setError('Please select at least one seat.');
      return;
    }
    if (!boardingStop || !droppingStop) {
      setError('Please select boarding and dropping points.');
      return;
    }
    navigate('/passenger/review', {
      state: {
        busId, routeId, travelDate,
        bus, fromCity, toCity,
        selectedSeats,
        boardingStop,
        droppingStop,
        totalAmount,
      }
    });
  };

  const boardingStops = bus?.boardingStops || [];
  const droppingStops = bus?.droppingStops || [];

  return (
    <PassengerLayout>
      <div className="mb-3">
        <button className="btn btn-link text-decoration-none p-0 d-flex align-items-center gap-2 text-dark fw-semibold"
          onClick={() => navigate(-1)}>
          <MdArrowBack size={20} /> Back to Search
        </button>
      </div>
      {/* Route Header */}
      <div className="tripzo-card mb-4"
        style={{ background: 'linear-gradient(135deg, #1E63FF 0%, #0F3D91 100%)', color: 'white' }}>
        <div className="d-flex align-items-center justify-content-between flex-wrap gap-3">
          <div>
            <h5 className="fw-bold mb-1">{bus?.busName || 'Bus'}</h5>
            <span className="badge rounded-pill bg-white bg-opacity-25">{bus?.busType}</span>
          </div>
          <div className="d-flex align-items-center gap-3">
            <div className="text-center">
              <div className="fw-bold fs-5">{fromCity}</div>
              <div className="opacity-75 small">{bus?.departureTime?.slice(0, 5)}</div>
            </div>
            <MdArrowForward size={28} />
            <div className="text-center">
              <div className="fw-bold fs-5">{toCity}</div>
              <div className="opacity-75 small">Arrival</div>
            </div>
          </div>
          <div className="text-end">
            <div className="opacity-75 small">Journey Date</div>
            <div className="fw-bold">{new Date(travelDate).toLocaleDateString('en-IN', { day: '2-digit', month: 'short', year: 'numeric' })}</div>
          </div>
        </div>
      </div>

      <div className="row g-4">
        {/* Seat Map */}
        <div className="col-md-7">
          <div className="tripzo-card">
            <h6 className="fw-bold mb-1">Select Your Seat(s)</h6>
            <p className="text-muted small mb-3">Click on an available seat to select it. You can select multiple seats.</p>

            {/* Legend */}
            <div className="d-flex gap-4 mb-4 flex-wrap">
              {[
                { label: 'Available', cls: 'seat-available', border: '#D1D5DB' },
                { label: 'Selected', cls: 'seat-selected', border: '#1E63FF', bg: '#1E63FF' },
                { label: 'Occupied', cls: 'seat-booked', border: '#D1D5DB', bg: '#F3F4F6' },
              ].map(item => (
                <div key={item.label} className="d-flex align-items-center gap-2">
                  <div style={{
                    width: 20, height: 17,
                    borderRadius: '4px 4px 2px 2px',
                    border: `2px solid ${item.border}`,
                    background: item.bg || '#fff',
                    opacity: item.cls === 'seat-booked' ? 0.6 : 1,
                  }} />
                  <span className="small text-muted">{item.label}</span>
                </div>
              ))}
            </div>

            {loading ? (
              <div className="text-center py-5"><div className="spinner-border text-primary" /></div>
            ) : error ? (
              <div className="alert alert-danger">{error}</div>
            ) : (
              /* Bus Body */
              <div className="position-relative mx-auto"
                style={{ maxWidth: 340, background: '#F8FAFC', borderRadius: 16, padding: '20px 16px', border: '2px solid #E2E8F0' }}>
                {/* Steering Wheel indicator */}
                <div className="d-flex justify-content-between align-items-center mb-3 pb-2"
                  style={{ borderBottom: '2px dashed #E2E8F0' }}>
                  <div style={{
                    width: 36, height: 36, borderRadius: '50%',
                    border: '3px solid #CBD5E1',
                    display: 'flex', alignItems: 'center', justifyContent: 'center'
                  }}>
                    <div style={{ width: 16, height: 16, borderRadius: '50%', background: '#CBD5E1' }} />
                  </div>
                  <span className="small text-muted fw-semibold">FRONT</span>
                  <div style={{ width: 36 }} />
                </div>

                {/* Seat Rows */}
                {sortedRowKeys.map(rowKey => {
                  const rowSeats = gridRows[rowKey];
                  const leftSeats = rowSeats.slice(0, 2);
                  const rightSeats = rowSeats.slice(2);
                  return (
                    <div key={rowKey} className="d-flex align-items-center justify-content-center gap-2 mb-2">
                      {/* Left cluster */}
                      <div className="d-flex gap-1">
                        {leftSeats.map(seat => (
                          <div
                            key={seat.seatId}
                            className={`seat-cell ${getSeatClass(seat)}`}
                            onClick={() => toggleSeat(seat)}
                            title={`${seat.seatNumber} - ${seat.seatType} - ₹${seat.finalPrice} - ${seat.isAvailable ? 'Available' : 'Booked'}`}
                            onMouseEnter={() => setTooltip(seat)}
                            onMouseLeave={() => setTooltip(null)}
                          >
                            <span className="seat-number">{seat.seatNumber}</span>
                          </div>
                        ))}
                      </div>
                      {/* Aisle */}
                      <div className="seat-aisle" />
                      {/* Right cluster */}
                      <div className="d-flex gap-1">
                        {rightSeats.map(seat => (
                          <div
                            key={seat.seatId}
                            className={`seat-cell ${getSeatClass(seat)}`}
                            onClick={() => toggleSeat(seat)}
                            title={`${seat.seatNumber} - ${seat.seatType} - ₹${seat.finalPrice}`}
                            onMouseEnter={() => setTooltip(seat)}
                            onMouseLeave={() => setTooltip(null)}
                          >
                            <span className="seat-number">{seat.seatNumber}</span>
                          </div>
                        ))}
                      </div>
                    </div>
                  );
                })}

                {/* Tooltip */}
                {tooltip && (
                  <div className="position-fixed bg-dark text-white rounded-2 p-2 small"
                    style={{ zIndex: 9999, bottom: 20, right: 20, minWidth: 140, pointerEvents: 'none' }}>
                    <div className="fw-bold">{tooltip.seatNumber}</div>
                    <div>{tooltip.seatType}</div>
                    <div>₹{tooltip.finalPrice}</div>
                    <div style={{ color: tooltip.isAvailable ? '#86EFAC' : '#FCA5A5' }}>
                      {tooltip.isAvailable ? '✓ Available' : '✗ Booked'}
                    </div>
                  </div>
                )}
              </div>
            )}
          </div>
        </div>

        {/* Booking Panel */}
        <div className="col-md-5">
          <div className="tripzo-card mb-3">
            <h6 className="fw-bold mb-3">Boarding & Dropping</h6>
            <div className="mb-3">
              <label className="form-label small fw-semibold text-muted">BOARDING POINT</label>
              <select className="form-select rounded-3"
                value={boardingStop} onChange={e => setBoardingStop(e.target.value)}>
                <option value="">Select Boarding Point</option>
                {boardingStops.map(s => (
                  <option key={s.stopId} value={s.stopId}>
                    {s.locationName} ({s.cityName}) - {s.arrivalTime.slice(0, 5)}
                  </option>
                ))}
              </select>
            </div>
            <div>
              <label className="form-label small fw-semibold text-muted">DROPPING POINT</label>
              <select className="form-select rounded-3"
                value={droppingStop} onChange={e => setDroppingStop(e.target.value)}>
                <option value="">Select Dropping Point</option>
                {droppingStops.map(s => (
                  <option key={s.stopId} value={s.stopId}>
                    {s.locationName} ({s.cityName}) - {s.arrivalTime.slice(0, 5)}
                  </option>
                ))}
              </select>
            </div>
          </div>

          <div className="tripzo-card mb-3">
            <h6 className="fw-bold mb-3">Selected Seats</h6>
            {selectedSeats.length === 0 ? (
              <p className="text-muted small text-center py-2">No seats selected yet.</p>
            ) : (
              <>
                <div className="d-flex flex-wrap gap-2 mb-3">
                  {selectedSeats.map(s => (
                    <div key={s.seatId}
                      className="d-flex align-items-center gap-1 rounded-3 px-2 py-1"
                      style={{ background: '#E8F0FF', color: '#1E63FF', fontSize: '0.8rem', fontWeight: 600 }}>
                      <MdChair size={14} />
                      {s.seatNumber}
                      <button
                        onClick={() => toggleSeat(s)}
                        style={{ background: 'none', border: 'none', color: '#EF4444', padding: 0, lineHeight: 1 }}>
                        ×
                      </button>
                    </div>
                  ))}
                </div>
                <div className="border-top pt-3">
                  <div className="d-flex justify-content-between small text-muted mb-1">
                    <span>Seats ({selectedSeats.length})</span>
                    <span>₹{totalAmount.toFixed(2)}</span>
                  </div>
                  <div className="d-flex justify-content-between fw-bold mt-2">
                    <span>Total</span>
                    <span style={{ color: 'var(--primary-blue)', fontSize: '1.1rem' }}>₹{totalAmount.toFixed(2)}</span>
                  </div>
                </div>
              </>
            )}
          </div>

          {error && <div className="alert alert-danger small py-2">{error}</div>}

          <button
            className="btn btn-primary w-100 rounded-3 py-2 fw-bold"
            onClick={handleProceed}
            disabled={selectedSeats.length === 0}>
            Proceed to Review →
          </button>
        </div>
      </div>
    </PassengerLayout>
  );
};

export default SeatSelection;
