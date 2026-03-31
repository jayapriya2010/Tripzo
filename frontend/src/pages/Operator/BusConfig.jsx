import React, { useState, useEffect } from 'react';
import { useParams, useNavigate } from 'react-router-dom';
import { MdArrowBack, MdEventSeat, MdCheckCircle, MdAdd, MdDelete } from 'react-icons/md';
import operatorService from '../../services/operator/operatorService';
import authService from '../../services/authService';

const BusConfig = () => {
  const { busId } = useParams();
  const navigate = useNavigate();
  const user = authService.getCurrentUser();
  const operatorId = user?.userId || user?.UserId;

  const [busDetail, setBusDetail] = useState(null);
  const [loading, setLoading] = useState(true);
  const [error, setError] = useState('');
  const [success, setSuccess] = useState('');

  // Seat config
  const [seatForm, setSeatForm] = useState([]);
  const [submittingSeats, setSubmittingSeats] = useState(false);

  // Amenities
  const [allAmenities, setAllAmenities] = useState([]);
  const [busAmenityIds, setBusAmenityIds] = useState([]);
  const [submittingAmenities, setSubmittingAmenities] = useState(false);

  // Selected seat for tooltip
  const [selectedSeat, setSelectedSeat] = useState(null);

  useEffect(() => { fetchData(); }, [busId]);

  const fetchData = async () => {
    if (!busId || busId === 'undefined') {
      setError('Invalid Bus ID. Please return to the buses page.');
      setLoading(false);
      return;
    }
    try {
      setLoading(true);
      const [detailRes, amenitiesRes] = await Promise.allSettled([
        operatorService.getBusDetail(busId, operatorId),
        operatorService.getAllAmenities()
      ]);
      if (detailRes.status === 'fulfilled') {
        setBusDetail(detailRes.value.data);
        setBusAmenityIds((detailRes.value.data.amenities || []).map((_, i) => i));
      }
      if (amenitiesRes.status === 'fulfilled') setAllAmenities(amenitiesRes.value.data);

      // Try to load bus amenity IDs
      try {
        const busAmRes = await operatorService.getBusAmenities(busId);
        setBusAmenityIds(busAmRes.data.map(a => a.amenityId));
      } catch { setBusAmenityIds([]); }
    } catch {
      setError('Failed to load bus details.');
    } finally {
      setLoading(false);
    }
  };

  const addSeatRow = () => {
    setSeatForm([...seatForm, { seatNumber: '', seatType: 'Window', addonFare: 0 }]);
  };

  const removeSeatRow = (index) => {
    setSeatForm(seatForm.filter((_, i) => i !== index));
  };

  const updateSeatRow = (index, field, value) => {
    const updated = [...seatForm];
    updated[index][field] = value;
    setSeatForm(updated);
  };

  const handleSubmitSeats = async (e) => {
    e.preventDefault();
    if (seatForm.length === 0) { setError('Add at least one seat.'); return; }
    if (!busId || busId === 'undefined') { setError('Cannot save: Invalid Bus ID.'); return; }
    setSubmittingSeats(true);
    setError('');
    setSuccess('');
    try {
      await operatorService.configureSeats(busId, seatForm.map(s => ({
        seatNumber: s.seatNumber,
        seatType: s.seatType,
        addonFare: parseFloat(s.addonFare) || 0
      })));
      setSuccess('Seats configured successfully!');
      setSeatForm([]);
      fetchData();
    } catch (err) {
      setError(err.response?.data?.message || 'Failed to configure seats.');
    } finally {
      setSubmittingSeats(false);
    }
  };

  const handleToggleAmenity = async (amenityId) => {
    setSubmittingAmenities(true);
    setError('');
    try {
      if (busAmenityIds.includes(amenityId)) {
        await operatorService.removeAmenitiesFromBus(busId, [amenityId]);
        setBusAmenityIds(prev => prev.filter(id => id !== amenityId));
      } else {
        await operatorService.addAmenitiesToBus(busId, [amenityId]);
        setBusAmenityIds(prev => [...prev, amenityId]);
      }
    } catch (err) {
      setError(err.response?.data?.message || 'Failed to update amenities.');
    } finally {
      setSubmittingAmenities(false);
    }
  };

  // Build seat grid from existing seats
  const buildSeatGrid = (seats) => {
    if (!seats || seats.length === 0) return null;
    // Sort seats by seat number
    const sorted = [...seats].sort((a, b) => a.seatNumber.localeCompare(b.seatNumber, undefined, { numeric: true }));
    // Group into rows of 4 (standard bus layout: 2 + aisle + 2)
    const cols = 4;
    const rows = [];
    for (let i = 0; i < sorted.length; i += cols) {
      rows.push(sorted.slice(i, i + cols));
    }
    return rows;
  };

  if (loading) {
    return (
      <div className="d-flex justify-content-center align-items-center" style={{ minHeight: '60vh' }}>
        <div className="spinner-border text-success" role="status"><span className="visually-hidden">Loading...</span></div>
      </div>
    );
  }

  const seatRows = busDetail?.seats ? buildSeatGrid(busDetail.seats) : null;

  return (
    <div>
      <button className="btn btn-link text-decoration-none p-0 mb-3 d-flex align-items-center gap-1 text-muted" onClick={() => navigate('/operator/buses')}>
        <MdArrowBack /> Back to Buses
      </button>

      <div className="d-flex justify-content-between align-items-center mb-4">
        <div>
          <h4 className="fw-bold m-0">Configure: {busDetail?.busName || 'Loading...'}</h4>
          <p className="text-muted m-0">{busDetail?.busNumber} • {busDetail?.busType} • {busDetail?.capacity} seats</p>
        </div>
      </div>

      {error && <div className="alert alert-danger alert-dismissible">{error}<button className="btn-close" onClick={() => setError('')}></button></div>}
      {success && <div className="alert alert-success alert-dismissible">{success}<button className="btn-close" onClick={() => setSuccess('')}></button></div>}

      <div className="row g-4">
        {/* Pictorial Seat Layout */}
        <div className="col-lg-7">
          <div className="tripzo-card">
            <h6 className="fw-bold mb-3 d-flex align-items-center gap-2"><MdEventSeat /> Seat Layout</h6>

            {/* Legend */}
            <div className="d-flex gap-3 mb-3 flex-wrap">
              <span className="d-flex align-items-center gap-1 small"><span className="seat-box seat-available"></span> Available</span>
              <span className="d-flex align-items-center gap-1 small"><span className="seat-box seat-selected"></span> Selected</span>
              <span className="d-flex align-items-center gap-1 small"><span className="seat-box seat-window"></span> Window</span>
              <span className="d-flex align-items-center gap-1 small"><span className="seat-box seat-premium"></span> Premium</span>
            </div>

            {!seatRows || seatRows.length === 0 ? (
              <div className="text-center py-5 bg-light rounded-3">
                <MdEventSeat size={48} className="text-muted mb-2" style={{ opacity: 0.3 }} />
                <p className="text-muted m-0">No seats configured yet. Use the form below to add seats.</p>
              </div>
            ) : (
              <div className="seat-grid-container p-3 bg-light rounded-3">
                {/* Driver section */}
                <div className="text-center mb-3 pb-2 border-bottom">
                  <small className="text-muted fw-bold" style={{ letterSpacing: '1px' }}>🚌 DRIVER</small>
                </div>
                {seatRows.map((row, ri) => (
                  <div className="seat-row d-flex justify-content-center gap-2 mb-2" key={ri}>
                    {row.map((seat, ci) => {
                      const isSelected = selectedSeat?.seatId === seat.seatId;
                      const seatClass = seat.seatType === 'Window' ? 'seat-window'
                        : seat.addonFare > 0 ? 'seat-premium'
                        : 'seat-available';
                      return (
                        <React.Fragment key={seat.seatId}>
                          {ci === 2 && <div className="seat-aisle"></div>}
                          <div
                            className={`seat-cell ${seatClass} ${isSelected ? 'seat-selected' : ''}`}
                            onClick={() => setSelectedSeat(isSelected ? null : seat)}
                            title={`${seat.seatNumber} - ₹${seat.addonFare} (${seat.seatType})`}
                          >
                            <span className="seat-number">{seat.seatNumber}</span>
                          </div>
                        </React.Fragment>
                      );
                    })}
                  </div>
                ))}
                {/* Selected seat tooltip */}
                {selectedSeat && (
                  <div className="mt-3 p-3 bg-white rounded-3 shadow-sm">
                    <div className="d-flex justify-content-between">
                      <span className="fw-bold">{selectedSeat.seatNumber}</span>
                      <span className="badge bg-primary">{selectedSeat.seatType}</span>
                    </div>
                    <p className="text-muted m-0 mt-1 small">Addon Fare: <strong className="text-success">₹{selectedSeat.addonFare}</strong></p>
                  </div>
                )}
              </div>
            )}
          </div>
        </div>

        {/* Config Forms */}
        <div className="col-lg-5">
          {/* Add Seats Form */}
          <div className="tripzo-card">
            <h6 className="fw-bold mb-3">Add Seats</h6>
            <form onSubmit={handleSubmitSeats}>
              {seatForm.map((seat, i) => (
                <div className="row g-2 mb-2 align-items-end" key={i}>
                  <div className="col-4">
                    <label className="form-label small text-muted">Seat #</label>
                    <input type="text" className="form-control form-control-sm bg-light border-0" placeholder="1A" value={seat.seatNumber} onChange={e => updateSeatRow(i, 'seatNumber', e.target.value)} required />
                  </div>
                  <div className="col-3">
                    <label className="form-label small text-muted">Type</label>
                    <select className="form-select form-select-sm bg-light border-0" value={seat.seatType} onChange={e => updateSeatRow(i, 'seatType', e.target.value)}>
                      <option value="Window">Window</option>
                      <option value="Aisle">Aisle</option>
                      <option value="Lower">Lower</option>
                      <option value="Upper">Upper</option>
                    </select>
                  </div>
                  <div className="col-3">
                    <label className="form-label small text-muted">₹ Addon</label>
                    <input type="number" className="form-control form-control-sm bg-light border-0" value={seat.addonFare} onChange={e => updateSeatRow(i, 'addonFare', e.target.value)} min="0" />
                  </div>
                  <div className="col-2">
                    <button type="button" className="btn btn-sm btn-outline-danger w-100" onClick={() => removeSeatRow(i)}><MdDelete /></button>
                  </div>
                </div>
              ))}
              <button type="button" className="btn btn-sm btn-outline-success rounded-pill w-100 mt-2 mb-3" onClick={addSeatRow}>
                <MdAdd size={16} /> Add Seat Row
              </button>
              {seatForm.length > 0 && (
                <button type="submit" className="btn btn-success w-100 rounded-pill" disabled={submittingSeats}>
                  {submittingSeats ? <span className="spinner-border spinner-border-sm me-2"></span> : <MdCheckCircle className="me-2" />}
                  Save Seat Configuration
                </button>
              )}
            </form>
          </div>

          {/* Amenities */}
          <div className="tripzo-card mt-3">
            <h6 className="fw-bold mb-3">Bus Amenities</h6>
            {allAmenities.length === 0 ? (
              <p className="text-muted small">No amenities available in the system.</p>
            ) : (
              <div className="d-flex flex-wrap gap-2">
                {allAmenities.map(amenity => {
                  const isChecked = busAmenityIds.includes(amenity.amenityId);
                  return (
                    <button
                      key={amenity.amenityId}
                      className={`btn btn-sm rounded-pill px-3 ${isChecked ? 'btn-success' : 'btn-outline-secondary'}`}
                      onClick={() => handleToggleAmenity(amenity.amenityId)}
                      disabled={submittingAmenities}
                    >
                      {isChecked ? <MdCheckCircle className="me-1" /> : null}
                      {amenity.amenityName}
                    </button>
                  );
                })}
              </div>
            )}
          </div>
        </div>
      </div>
    </div>
  );
};

export default BusConfig;
