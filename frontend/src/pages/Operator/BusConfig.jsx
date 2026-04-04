import React, { useState, useEffect } from 'react';
import { useParams, useNavigate } from 'react-router-dom';
import { MdArrowBack, MdEventSeat, MdCheckCircle, MdAdd, MdDelete, MdHotel, MdChair } from 'react-icons/md';
import operatorService from '../../services/operator/operatorService';
import authService from '../../services/auth/authService';

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
    setSeatForm([...seatForm, { seatNumber: '', berth: 'Lower', position: 'Window', category: 'Seater', addonFare: 0 }]);
  };

  const removeSeatRow = (index) => {
    setSeatForm(seatForm.filter((_, i) => i !== index));
  };

  const updateSeatRow = (index, field, value) => {
    const updated = [...seatForm];
    updated[index][field] = value;
    
    // Requirement: Upper berth is "always sleeper"
    if (field === 'berth' && value === 'Upper') {
      updated[index].category = 'Sleeper';
    }
    
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
        seatType: `${s.berth}|${s.position}|${s.category}`,
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

  const buildSeatGrid = (seats) => {
    if (!seats || seats.length === 0) return { lower: null, upper: null };

    const parseType = (typeStr) => {
      if (typeStr.includes('|')) {
        const [berth, pos, cat] = typeStr.split('|');
        return { berth, pos, cat };
      }
      // Fallback for old data
      return { 
        berth: typeStr === 'Upper' ? 'Upper' : 'Lower', 
        pos: ['Window', 'Aisle'].includes(typeStr) ? typeStr : 'Window', 
        cat: typeStr === 'Upper' ? 'Sleeper' : 'Seater' 
      };
    };

    const sortedByNumber = [...seats].sort((a, b) => 
      a.seatNumber.localeCompare(b.seatNumber, undefined, { numeric: true })
    );

    const lower = sortedByNumber.filter(s => parseType(s.seatType).berth === 'Lower');
    const upper = sortedByNumber.filter(s => parseType(s.seatType).berth === 'Upper');

    const organizeIntoRows = (seatList) => {
      if (seatList.length === 0) return null;
      const rows = [];
      const cols = 4;
      for (let i = 0; i < seatList.length; i += cols) {
        rows.push(seatList.slice(i, i + cols));
      }
      return rows;
    };

    return { lower: organizeIntoRows(lower), upper: organizeIntoRows(upper) };
  };

  if (loading) {
    return (
      <div className="d-flex justify-content-center align-items-center" style={{ minHeight: '60vh' }}>
        <div className="spinner-border text-success" role="status"><span className="visually-hidden">Loading...</span></div>
      </div>
    );
  }

  const { lower: lowerRows, upper: upperRows } = buildSeatGrid(busDetail?.seats || []);

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
        <div className="col-lg-6">
          <div className="tripzo-card">
            <h6 className="fw-bold mb-3 d-flex align-items-center gap-2"><MdEventSeat /> Seat Layout</h6>

            {/* Legend */}
            <div className="d-flex gap-3 mb-3 flex-wrap">
              <span className="d-flex align-items-center gap-1 small"><span className="seat-box seat-available"></span> Available</span>
              <span className="d-flex align-items-center gap-1 small"><span className="seat-box seat-selected"></span> Selected</span>
              <span className="d-flex align-items-center gap-1 small"><span className="seat-box seat-window"></span> Window</span>
              <span className="d-flex align-items-center gap-1 small"><span className="seat-box seat-premium"></span> Premium</span>
            </div>

            {(lowerRows || upperRows) ? (
              <div className="row g-4">
                {/* Lower Berth Section */}
                <div className="col-md-6 border-end">
                  <div className="text-center mb-3">
                    <span className="badge bg-dark rounded-pill px-3 py-2 fw-bold" style={{ fontSize: '0.75rem', letterSpacing: '1px' }}>LOWER BERTH</span>
                  </div>
                  {lowerRows ? (
                    <div className="seat-grid-container p-3 bg-light rounded-3">
                      <div className="text-center mb-3 pb-2 border-bottom">
                        <small className="text-muted fw-bold" style={{ letterSpacing: '1px' }}>🚌 DRIVER</small>
                      </div>
                      {lowerRows.map((row, ri) => (
                        <div className="seat-row d-flex justify-content-center gap-2 mb-2" key={ri}>
                          {row.map((seat, ci) => {
                            const isSelected = selectedSeat?.seatId === seat.seatId;
                            const [berth, pos, cat] = seat.seatType.includes('|') ? seat.seatType.split('|') : ['Lower', 'Window', 'Seater'];
                            
                            const seatClass = pos === 'Window' ? 'seat-window'
                              : seat.addonFare > 0 ? 'seat-premium'
                              : 'seat-available';
                            
                            return (
                              <React.Fragment key={seat.seatId}>
                                {ci === 2 && <div className="seat-aisle"></div>}
                                <div
                                  className={`seat-cell ${seatClass} ${isSelected ? 'seat-selected' : ''} ${cat === 'Sleeper' ? 'seat-sleeper' : 'seat-seater'}`}
                                  onClick={() => setSelectedSeat(isSelected ? null : seat)}
                                  title={`${seat.seatNumber} - ₹${seat.addonFare} (${berth} | ${pos} | ${cat})`}
                                  style={{ height: cat === 'Sleeper' ? '65px' : '44px' }}
                                >
                                  <div className="d-flex flex-column align-items-center">
                                    {cat === 'Sleeper' ? <MdHotel size={18} className="mb-1" /> : <MdChair size={16} className="mb-1" />}
                                    <span className="seat-number" style={{ fontSize: '0.6rem' }}>{seat.seatNumber}</span>
                                  </div>
                                </div>
                              </React.Fragment>
                            );
                          })}
                        </div>
                      ))}
                    </div>
                  ) : (
                    <div className="text-center py-4 text-muted small">No lower berth seats</div>
                  )}
                </div>

                {/* Upper Berth Section */}
                <div className="col-md-6">
                  <div className="text-center mb-3">
                    <span className="badge bg-primary bg-opacity-10 text-primary border border-primary border-opacity-25 rounded-pill px-3 py-2 fw-bold" style={{ fontSize: '0.75rem', letterSpacing: '1px' }}>UPPER BERTH</span>
                  </div>
                  {upperRows ? (
                    <div className="seat-grid-container p-3 bg-light rounded-3 shadow-none border">
                      <div className="text-center mb-3 pb-2 border-bottom">
                         <small className="text-muted fw-bold invisible" style={{ letterSpacing: '1px' }}>SPACE</small>
                      </div>
                      {upperRows.map((row, ri) => (
                        <div className="seat-row d-flex justify-content-center gap-2 mb-2" key={ri}>
                          {row.map((seat, ci) => {
                            const isSelected = selectedSeat?.seatId === seat.seatId;
                            const [berth, pos, cat] = seat.seatType.includes('|') ? seat.seatType.split('|') : ['Upper', 'Window', 'Sleeper'];
                            
                            const seatClass = pos === 'Window' ? 'seat-window'
                              : seat.addonFare > 0 ? 'seat-premium'
                              : 'seat-available';
                            
                            return (
                              <React.Fragment key={seat.seatId}>
                                {ci === 2 && <div className="seat-aisle"></div>}
                                <div
                                  className={`seat-cell ${seatClass} ${isSelected ? 'seat-selected' : ''} seat-sleeper`}
                                  onClick={() => setSelectedSeat(isSelected ? null : seat)}
                                  title={`${seat.seatNumber} - ₹${seat.addonFare} (${berth} | ${pos} | ${cat})`}
                                  style={{ height: '65px' }}
                                >
                                  <div className="d-flex flex-column align-items-center">
                                    <MdHotel size={18} className="mb-1" />
                                    <span className="seat-number" style={{ fontSize: '0.6rem' }}>{seat.seatNumber}</span>
                                  </div>
                                </div>
                              </React.Fragment>
                            );
                          })}
                        </div>
                      ))}
                    </div>
                  ) : (
                    <div className="text-center py-4 text-muted small">No upper berth seats</div>
                  )}
                </div>

                {/* Selected seat tooltip */}
                {selectedSeat && (
                  <div className="col-12 mt-3">
                    <div className="p-3 bg-white rounded-3 shadow-sm border border-primary border-opacity-10">
                      <div className="d-flex justify-content-between align-items-center">
                        <span className="fw-bold fs-5 text-primary">{selectedSeat.seatNumber}</span>
                        <div className="d-flex gap-2">
                          {selectedSeat.seatType.split('|').map((tag, i) => (
                            <span key={i} className="badge bg-light text-dark border">{tag}</span>
                          ))}
                        </div>
                      </div>
                      <p className="text-muted m-0 mt-1 small">Addon Fare: <strong className="text-success">₹{selectedSeat.addonFare}</strong></p>
                    </div>
                  </div>
                )}
              </div>
            ) : (
              <div className="text-center py-5 bg-light rounded-3">
                <MdEventSeat size={48} className="text-muted mb-2" style={{ opacity: 0.3 }} />
                <p className="text-muted m-0">No seats configured yet. Use the form below to add seats.</p>
              </div>
            )}
          </div>
        </div>

        {/* Config Forms */}
        <div className="col-lg-6">
          {/* Add Seats Form */}
          <div className="tripzo-card">
            <h6 className="fw-bold mb-3">Add Seats</h6>
            <form onSubmit={handleSubmitSeats}>
              {seatForm.map((seat, i) => (
                <div className="row gx-1 mb-2 align-items-end" key={i}>
                  <div className="col-1">
                    <label className="form-label mb-1" style={{ fontSize: '0.65rem', fontWeight: 'bold', color: '#6c757d' }}>Seat</label>
                    <input type="text" className="form-control form-control-sm bg-light border-0 px-1" placeholder="1A" value={seat.seatNumber} onChange={e => updateSeatRow(i, 'seatNumber', e.target.value)} required />
                  </div>
                  <div className="col-3">
                    <label className="form-label mb-1" style={{ fontSize: '0.65rem', fontWeight: 'bold', color: '#6c757d' }}>Berth</label>
                    <select className="form-select form-select-sm bg-light border-0 px-2" style={{ paddingRight: '20px' }} value={seat.berth} onChange={e => updateSeatRow(i, 'berth', e.target.value)}>
                      <option value="Lower">Lower</option>
                      <option value="Upper">Upper</option>
                    </select>
                  </div>
                  <div className="col-3">
                    <label className="form-label mb-1" style={{ fontSize: '0.65rem', fontWeight: 'bold', color: '#6c757d' }}>Pos.</label>
                    <select className="form-select form-select-sm bg-light border-0 px-2" style={{ paddingRight: '20px' }} value={seat.position} onChange={e => updateSeatRow(i, 'position', e.target.value)}>
                      <option value="Window">Window</option>
                      <option value="Aisle">Aisle</option>
                    </select>
                  </div>
                  <div className="col-2">
                    <label className="form-label mb-1" style={{ fontSize: '0.65rem', fontWeight: 'bold', color: '#6c757d' }}>Type</label>
                    <select className="form-select form-select-sm bg-light border-0 px-2" style={{ paddingRight: '25px' }} value={seat.category} onChange={e => updateSeatRow(i, 'category', e.target.value)}>
                      <option value="Seater" disabled={seat.berth === 'Upper'}>Seater</option>
                      <option value="Sleeper">Sleeper</option>
                    </select>
                  </div>
                  <div className="col-2">
                    <label className="form-label mb-1" style={{ fontSize: '0.65rem', fontWeight: 'bold', color: '#6c757d' }}>Fare</label>
                    <input type="number" className="form-control form-control-sm bg-light border-0 px-1 text-center" value={seat.addonFare} onChange={e => updateSeatRow(i, 'addonFare', e.target.value)} min="0" />
                  </div>
                  <div className="col-1 text-center">
                    <button type="button" className="btn btn-sm btn-link text-danger p-0 mb-1" onClick={() => removeSeatRow(i)}><MdDelete size={18} /></button>
                  </div>
                </div>
              ))}
              <button type="button" className="btn btn-sm btn-outline-primary rounded-pill w-100 mt-2 mb-3" onClick={addSeatRow}>
                <MdAdd size={16} /> Add Seat Row
              </button>
              {seatForm.length > 0 && (
                <button type="submit" className="btn btn-primary w-100 rounded-pill shadow-sm" disabled={submittingSeats}>
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
                      className={`btn btn-sm rounded-pill px-3 ${isChecked ? 'btn-primary' : 'btn-outline-secondary'}`}
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
