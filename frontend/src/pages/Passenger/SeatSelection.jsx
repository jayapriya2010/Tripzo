import React, { useState, useEffect } from 'react';
import { useNavigate, useLocation } from 'react-router-dom';
import { MdChair, MdArrowForward, MdInfo, MdArrowBack, MdHotel } from 'react-icons/md';
import PassengerLayout from '../../layouts/PassengerLayout';
import passengerService from '../../services/passenger/passengerService';

const SeatSelection = () => {
  const navigate = useNavigate();
  const location = useLocation();
  const { busId, routeId, travelDate, bus, fromCity, toCity } = location.state || {};

  const [seats, setSeats] = useState([]);
  const [selectedSeats, setSelectedSeats] = useState([]);
  const [loading, setLoading] = useState(true);
  const [boardingStop, setBoardingStop] = useState('');
  const [droppingStop, setDroppingStop] = useState('');
  const [activeBerth, setActiveBerth] = useState('Lower');
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

  // Group seats by berth and then by rows
  const buildGrid = (berthType) => {
    const berthSeats = seats.filter(s => s.berth === berthType);
    const rowMap = {};
    
    berthSeats.forEach(seat => {
      // Extract row number (digits) from seat number
      const match = seat.seatNumber.match(/^(\d+)([A-Za-z]+)$/);
      const rowNum = match ? match[1] : '1';
      if (!rowMap[rowNum]) rowMap[rowNum] = [];
      rowMap[rowNum].push(seat);
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
    if (seat.position === 'Window') return 'seat-window';
    return 'seat-available';
  };

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

  const gridRows = buildGrid(activeBerth);
  const sortedRowKeys = Object.keys(gridRows).sort((a, b) => parseInt(a) - parseInt(b));
  
  const hasUpperBerth = seats.some(s => s.berth === 'Upper');

  return (
    <PassengerLayout>
      <div className="mb-3">
        <button className="btn btn-link text-decoration-none p-0 d-flex align-items-center gap-2 text-dark fw-semibold"
          onClick={() => navigate(-1)}>
          <MdArrowBack size={20} /> Back to Search
        </button>
      </div>

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
        <div className="col-md-7">
          <div className="tripzo-card">
            <h6 className="fw-bold mb-1">Select Your Seat(s)</h6>
            <p className="text-muted small mb-4">You can select multiple seats from both decks.</p>

            {/* Berth Switcher */}
            {hasUpperBerth && (
              <div className="d-flex p-1 bg-light rounded-pill mb-4" style={{ width: 'fit-content' }}>
                <button 
                  className={`btn btn-sm rounded-pill px-4 py-2 border-0 fw-bold transition-all ${activeBerth === 'Lower' ? 'bg-primary text-white shadow-sm' : 'text-muted'}`}
                  onClick={() => setActiveBerth('Lower')}
                >
                  Lower Deck
                </button>
                <button 
                  className={`btn btn-sm rounded-pill px-4 py-2 border-0 fw-bold transition-all ${activeBerth === 'Upper' ? 'bg-primary text-white shadow-sm' : 'text-muted'}`}
                  onClick={() => setActiveBerth('Upper')}
                >
                  Upper Deck
                </button>
              </div>
            )}

            {/* Legend */}
            <div className="d-flex gap-4 mb-4 flex-wrap">
              {[
                { label: 'Available', cls: 'seat-available', border: '#D1D5DB' },
                { label: 'Selected', cls: 'seat-selected', border: '#1E63FF', bg: '#1E63FF' },
                { label: 'Occupied', cls: 'seat-booked', border: '#D1D5DB', bg: '#F3F4F6' },
                { label: 'Window', cls: 'seat-window', border: '#E0E7FF', bg: '#EEF2FF', icon: true },
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
              <div className="position-relative mx-auto"
                style={{ 
                  maxWidth: 340, 
                  background: '#fcfcfc', 
                  borderRadius: 24, 
                  padding: '24px 20px', 
                  border: '2px solid #edeff2',
                  boxShadow: 'inset 0 0 40px rgba(0,0,0,0.02)'
                }}>
                
                {/* Steering Wheel / Front indicator */}
                <div className="d-flex justify-content-between align-items-center mb-4 pb-3"
                  style={{ borderBottom: '2px dashed #e2e8f0' }}>
                  <div style={{
                    width: 40, height: 40, borderRadius: '50%',
                    border: '3px solid #cbd5e1',
                    display: 'flex', alignItems: 'center', justifyContent: 'center'
                  }}>
                    <div style={{ width: 18, height: 18, borderRadius: '50%', background: '#cbd5e1' }} />
                  </div>
                  <span className="small text-muted fw-bold" style={{ letterSpacing: '2px' }}>FRONT</span>
                  <div style={{ width: 40 }} />
                </div>

                <div className="bus-grid">
                  {sortedRowKeys.map(rowKey => {
                    const rowSeats = gridRows[rowKey];
                    // Logic to split row seats into left and right (aisle in between)
                    // Assuming 4 columns max (2 left, 2 right)
                    const leftSeats = rowSeats.slice(0, 2);
                    const rightSeats = rowSeats.slice(2);
                    
                    return (
                      <div key={rowKey} className="d-flex align-items-center justify-content-center gap-2 mb-3">
                        <div className="d-flex gap-2">
                          {leftSeats.map(seat => (
                            <div
                              key={seat.seatId}
                              className={`seat-cell ${getSeatClass(seat)} ${seat.category === 'Sleeper' ? 'seat-sleeper' : 'seat-seater'}`}
                              onClick={() => toggleSeat(seat)}
                              title={`${seat.seatNumber} - ${seat.category} (${seat.position})`}
                              onMouseEnter={() => setTooltip(seat)}
                              onMouseLeave={() => setTooltip(null)}
                              style={{ height: seat.category === 'Sleeper' ? 65 : 44 }}
                            >
                              <div className="d-flex flex-column align-items-center justify-content-center h-100">
                                {seat.category === 'Sleeper' ? <MdHotel size={18} /> : <MdChair size={16} />}
                                <span className="seat-number mt-1">{seat.seatNumber}</span>
                              </div>
                            </div>
                          ))}
                        </div>
                        
                        <div className="aisle-space" style={{ width: 30 }} />
                        
                        <div className="d-flex gap-2">
                          {rightSeats.map(seat => (
                            <div
                              key={seat.seatId}
                              className={`seat-cell ${getSeatClass(seat)} ${seat.category === 'Sleeper' ? 'seat-sleeper' : 'seat-seater'}`}
                              onClick={() => toggleSeat(seat)}
                              title={`${seat.seatNumber} - ${seat.category} (${seat.position})`}
                              onMouseEnter={() => setTooltip(seat)}
                              onMouseLeave={() => setTooltip(null)}
                              style={{ height: seat.category === 'Sleeper' ? 65 : 44 }}
                            >
                              <div className="d-flex flex-column align-items-center justify-content-center h-100">
                                {seat.category === 'Sleeper' ? <MdHotel size={18} /> : <MdChair size={16} />}
                                <span className="seat-number mt-1">{seat.seatNumber}</span>
                              </div>
                            </div>
                          ))}
                        </div>
                      </div>
                    );
                  })}
                </div>

                {tooltip && (
                  <div className="position-absolute bg-dark text-white rounded-3 p-2 small shadow-lg"
                    style={{ zIndex: 9999, top: '10%', right: -160, minWidth: 150 }}>
                    <div className="d-flex justify-content-between mb-1 pb-1 border-bottom border-secondary">
                      <span className="fw-bold">{tooltip.seatNumber}</span>
                      <span className="opacity-75">{tooltip.berth}</span>
                    </div>
                    <div className="d-flex justify-content-between">
                      <span>Type:</span>
                      <span>{tooltip.category}</span>
                    </div>
                    <div className="d-flex justify-content-between">
                      <span>Fare:</span>
                      <span className="text-warning fw-bold">₹{tooltip.finalPrice}</span>
                    </div>
                    <div className="mt-1 pt-1 border-top border-secondary text-center font-monospace" 
                         style={{ color: tooltip.isAvailable ? '#4ADE80' : '#F87171' }}>
                      {tooltip.isAvailable ? 'AVAILABLE' : 'BOOKED'}
                    </div>
                  </div>
                )}
              </div>
            )}
          </div>
        </div>

        <div className="col-md-5">
          <div className="tripzo-card mb-3">
            <h6 className="fw-bold mb-3">Boarding & Dropping</h6>
            <div className="mb-3">
              <label className="form-label small fw-semibold text-muted">BOARDING POINT</label>
              <select className="form-select rounded-3 shadow-none bg-light border-0"
                value={boardingStop} onChange={e => setBoardingStop(e.target.value)}>
                <option value="">Select Boarding Point</option>
                {bus?.boardingStops?.map(s => (
                  <option key={s.stopId} value={s.stopId}>
                    {s.locationName} ({s.cityName}) - {s.arrivalTime.slice(0, 5)}
                  </option>
                ))}
              </select>
            </div>
            <div>
              <label className="form-label small fw-semibold text-muted">DROPPING POINT</label>
              <select className="form-select rounded-3 shadow-none bg-light border-0"
                value={droppingStop} onChange={e => setDroppingStop(e.target.value)}>
                <option value="">Select Dropping Point</option>
                {bus?.droppingStops?.map(s => (
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
              <div className="text-center py-4 bg-light rounded-3 border border-dashed">
                <MdChair size={30} className="text-muted opacity-25 mb-2" />
                <p className="text-muted small m-0">Select a seat to continue</p>
              </div>
            ) : (
              <>
                <div className="d-flex flex-wrap gap-2 mb-3">
                  {selectedSeats.map(s => (
                    <div key={s.seatId}
                      className="d-flex align-items-center gap-2 rounded-3 px-3 py-2 border shadow-xs"
                      style={{ background: '#fff', fontSize: '0.85rem', fontWeight: 600 }}>
                      <span className="text-primary">{s.category === 'Sleeper' ? <MdHotel /> : <MdChair />}</span>
                      <span>{s.seatNumber}</span>
                      <small className="text-muted border-start ps-2">{s.berth[0]}</small>
                      <button
                        onClick={() => toggleSeat(s)}
                        className="btn btn-link p-0 text-danger ms-2"
                        style={{ lineHeight: 1 }}>
                        ×
                      </button>
                    </div>
                  ))}
                </div>
                <div className="border-top pt-3">
                  <div className="d-flex justify-content-between small text-muted mb-2">
                    <span>Base Amount ({selectedSeats.length} seats)</span>
                    <span>₹{totalAmount.toFixed(2)}</span>
                  </div>
                  <div className="d-flex justify-content-between align-items-center mt-3 p-3 bg-primary rounded-3 border border-primary border-opacity-10 shadow-sm">
                    <div>
                      <div className="text-white text-opacity-75 small fw-semibold">TOTAL PAYABLE</div>
                      <div className="fs-4 fw-bold text-white">₹{totalAmount.toFixed(2)}</div>
                    </div>
                  </div>
                </div>
              </>
            )}
          </div>

          {error && <div className="alert alert-danger small py-3 rounded-3 border-0 shadow-sm d-flex align-items-center gap-2">
            <MdInfo /> {error}
          </div>}

          <button
            className="btn btn-primary w-100 rounded-pill py-3 fw-bold shadow-lg transition-all"
            onClick={handleProceed}
            disabled={selectedSeats.length === 0}
            style={{ letterSpacing: '1px' }}>
            PROCEED TO REVIEW →
          </button>
        </div>
      </div>
    </PassengerLayout>
  );
};

export default SeatSelection;
