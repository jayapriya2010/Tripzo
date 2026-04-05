import React, { useState, useEffect } from 'react';
import { useNavigate } from 'react-router-dom';
import { MdArrowBack, MdAdd, MdDelete, MdCheckCircle } from 'react-icons/md';
import operatorService from '../../services/operator/operatorService';
import authService from '../../services/auth/authService';

const AddRoute = () => {
  const navigate = useNavigate();
  const user = authService.getCurrentUser();
  const operatorId = user?.userId || user?.UserId;

  const [fleet, setFleet] = useState([]);
  const [loading, setLoading] = useState(true);
  const [submitting, setSubmitting] = useState(false);
  const [error, setError] = useState('');
  const [success, setSuccess] = useState('');

  const [formData, setFormData] = useState({
    busId: '',
    sourceCity: '',
    destCity: '',
    baseFare: ''
  });
  const [selectedBusConfigured, setSelectedBusConfigured] = useState(true);

  const [stops, setStops] = useState([
    { cityName: '', locationName: '', stopType: 'Boarding', stopOrder: 1, arrivalTime: '06:00' },
    { cityName: '', locationName: '', stopType: 'Dropping', stopOrder: 2, arrivalTime: '12:00' }
  ]);

  useEffect(() => {
    const fetchFleet = async () => {
      try {
        const res = await operatorService.getFleet(operatorId);
        // Requirement: Filter to show only active buses
        const items = res.data.items || (Array.isArray(res.data) ? res.data : []);
        setFleet(items.filter(bus => bus.isActive));
      } catch { setFleet([]); }
      finally { setLoading(false); }
    };
    fetchFleet();
  }, [operatorId]);

  const handleBusChange = async (busId) => {
    setFormData({ ...formData, busId });
    if (!busId) {
      setSelectedBusConfigured(true);
      return;
    }
    
    try {
      // Check if bus has seats
      const res = await operatorService.getBusDetail(busId, operatorId);
      const isConfigured = res.data.seats && res.data.seats.length > 0;
      setSelectedBusConfigured(isConfigured);
      if (!isConfigured) {
        setError('Selected bus has NO seat configuration. Please configure seats under Manage Buses before creating a route.');
      } else {
        setError('');
      }
    } catch {
      setSelectedBusConfigured(false);
    }
  };

  const addStop = () => {
    setStops([...stops, {
      cityName: '', locationName: '', stopType: 'Boarding', stopOrder: stops.length + 1, arrivalTime: ''
    }]);
  };

  const removeStop = (index) => {
    if (stops.length <= 2) { setError('At least 2 stops (Source & Destination) are required.'); return; }
    const updated = stops.filter((_, i) => i !== index).map((s, i) => ({ ...s, stopOrder: i + 1 }));
    setStops(updated);
  };

  const updateStop = (index, field, value) => {
    const updated = [...stops];
    updated[index][field] = value;
    setStops(updated);
  };

  const handleSubmit = async (e) => {
    e.preventDefault();
    setError('');
    setSuccess('');
    setSubmitting(true);
    try {
      const payload = {
        busId: parseInt(formData.busId),
        sourceCity: formData.sourceCity,
        destCity: formData.destCity,
        baseFare: parseFloat(formData.baseFare),
        scheduleDate: stops[0].arrivalTime ? stops[0].arrivalTime.split('T')[0] : null,
        stops: stops.map((s, index) => {
          // Calculate DayOffset based on the date difference from the first stop
          let dayOffset = 0;
          if (index > 0 && stops[0].arrivalTime && s.arrivalTime) {
            const firstDate = new Date(stops[0].arrivalTime.split('T')[0]);
            const currentDate = new Date(s.arrivalTime.split('T')[0]);
            const diffTime = Math.abs(currentDate - firstDate);
            dayOffset = Math.ceil(diffTime / (1000 * 60 * 60 * 24));
            
            // Safety check: if dates are same, offset is 0
            if (currentDate.toDateString() === firstDate.toDateString()) dayOffset = 0;
          }
          
          let timeVal = s.arrivalTime;
          if (timeVal && timeVal.includes('T')) {
            timeVal = timeVal.split('T')[1];
          }
          if (timeVal && timeVal.length === 5) timeVal += ':00'; // Ensure HH:MM:SS
          
          return {
            ...s,
            arrivalTime: timeVal,
            dayOffset: dayOffset
          };
        })
      };
      await operatorService.createRoute(payload);
      setSuccess('Route created successfully!');
      setTimeout(() => navigate('/operator/routes'), 1500);
    } catch (err) {
      setError(err.response?.data?.message || 'Failed to create route.');
    } finally {
      setSubmitting(false);
    }
  };

  if (loading) {
    return (
      <div className="d-flex justify-content-center align-items-center" style={{ minHeight: '60vh' }}>
        <div className="spinner-border text-primary" role="status"><span className="visually-hidden">Loading...</span></div>
      </div>
    );
  }

  return (
    <div>
      <button className="btn btn-link text-decoration-none p-0 mb-3 d-flex align-items-center gap-1 text-muted fw-bold" onClick={() => navigate('/operator/routes')}>
        <MdArrowBack /> Back to Routes
      </button>

      <div className="mb-4">
        <h4 className="fw-bold m-0 p-0 text-dark">Add New Route</h4>
        <p className="text-muted m-0 small">Define cities, fare, and bus for the route template</p>
      </div>

      {error && <div className="alert alert-danger shadow-sm">{error}</div>}
      {success && <div className="alert alert-success shadow-sm">{success}</div>}

      <form onSubmit={handleSubmit}>
        <div className="row g-4">
          {/* Route Info */}
          <div className="col-lg-6">
            <div className="tripzo-card border-top border-4 border-primary">
              <h6 className="fw-bold mb-3">General Information</h6>
              <div className="mb-3">
                <label className="form-label fw-bold small text-muted">ASSIGN ACTIVE BUS</label>
                <select className="form-select bg-light border-0 rounded-3 p-3" value={formData.busId} onChange={e => handleBusChange(e.target.value)} required>
                  <option value="">Select a bus...</option>
                  {fleet.map(bus => (
                    <option key={bus.busId} value={bus.busId}>{bus.busName} ({bus.busNumber}) - {bus.busType}</option>
                  ))}
                </select>
                {formData.busId && !selectedBusConfigured && (
                  <div className="alert alert-warning py-2 px-3 mt-2 small border-0 shadow-sm d-flex align-items-center gap-2">
                    <MdAdd size={18} className="text-warning" />
                    <span>This bus has no seats! <button type="button" className="btn btn-link btn-sm p-0 fw-bold" onClick={() => navigate(`/operator/buses/${formData.busId}/config`)}>Configure now</button></span>
                  </div>
                )}
                {fleet.length === 0 && <small className="text-danger mt-1 d-block">No active buses available. Please activate a bus first.</small>}
              </div>
              <div className="row g-3">
                <div className="col-6">
                  <label className="form-label fw-bold small text-muted">SOURCE CITY</label>
                  <input type="text" className="form-control bg-light border-0 rounded-3 p-3" placeholder="e.g. Chennai" value={formData.sourceCity} onChange={e => setFormData({ ...formData, sourceCity: e.target.value })} required />
                </div>
                <div className="col-6">
                  <label className="form-label fw-bold small text-muted">DESTINATION CITY</label>
                  <input type="text" className="form-control bg-light border-0 rounded-3 p-3" placeholder="e.g. Bangalore" value={formData.destCity} onChange={e => setFormData({ ...formData, destCity: e.target.value })} required />
                </div>
              </div>
              <div className="mt-3">
                <label className="form-label fw-bold small text-muted">BASE FARE (₹)</label>
                <div className="input-group">
                  <span className="input-group-text bg-light border-0 rounded-start-3 text-muted">₹</span>
                  <input type="number" className="form-control bg-light border-0 rounded-end-3 p-3" placeholder="e.g. 500" value={formData.baseFare} onChange={e => setFormData({ ...formData, baseFare: e.target.value })} required min="1" />
                </div>
              </div>
            </div>
          </div>

          {/* Stops */}
          <div className="col-lg-6">
            <div className="tripzo-card border-top border-4 border-primary">
              <div className="d-flex justify-content-between align-items-center mb-3">
                <h6 className="fw-bold m-0">Route Stops & Timings</h6>
              </div>

              <div className="stops-scroll-area pe-1" style={{ maxHeight: '450px', overflowY: 'auto' }}>
                {stops.map((stop, i) => (
                  <div key={i} className="bg-light rounded-3 p-3 mb-3 position-relative border">
                    <div className="d-flex justify-content-between align-items-center mb-2">
                      <span className="badge bg-primary rounded-pill">Stop {stop.stopOrder}</span>
                      {stops.length > 2 && (
                        <button type="button" className="btn btn-sm btn-link text-danger p-0" onClick={() => removeStop(i)}>
                          <MdDelete size={18} />
                        </button>
                      )}
                    </div>
                    <div className="row g-2">
                      <div className="col-6">
                        <label className="small fw-bold text-muted mb-1">CITY</label>
                        <input type="text" className="form-control form-control-sm border-0" placeholder="e.g. Salem" value={stop.cityName} onChange={e => updateStop(i, 'cityName', e.target.value)} required />
                      </div>
                      <div className="col-6">
                        <label className="small fw-bold text-muted mb-1">LOCATION / STAND</label>
                        <input type="text" className="form-control form-control-sm border-0" placeholder="e.g. Central Stand" value={stop.locationName} onChange={e => updateStop(i, 'locationName', e.target.value)} required />
                      </div>
                      <div className="col-6">
                        <label className="small fw-bold text-muted mb-1">TYPE</label>
                        <select className="form-select form-select-sm border-0" value={stop.stopType} onChange={e => updateStop(i, 'stopType', e.target.value)}>
                          <option value="Boarding">Boarding</option>
                          <option value="Dropping">Dropping</option>
                          <option value="Both">Both</option>
                        </select>
                      </div>
                      <div className="col-6">
                        <label className="small fw-bold text-muted mb-1">TIME (OR DATE-TIME)</label>
                        {/* Requirement: Make it time with date context if desired, though backend stores TimeSpan */}
                        <input type="datetime-local" className="form-control form-control-sm border-0" value={stop.arrivalTime} onChange={e => updateStop(i, 'arrivalTime', e.target.value)} required />
                      </div>
                    </div>
                  </div>
                ))}
              </div>

              {/* Requirement: Add Stop at the bottom for easy usage */}
              <div className="text-center mt-2 pt-2 border-top">
                <button type="button" className="btn btn-sm btn-outline-primary rounded-pill px-4" onClick={addStop}>
                  <MdAdd size={20} className="me-1" /> Add Stop
                </button>
              </div>
            </div>
          </div>
        </div>

        <div className="mt-4 text-end">
          <button type="button" className="btn btn-light rounded-pill px-4 me-2 fw-bold" onClick={() => navigate('/operator/routes')}>Cancel</button>
          <button type="submit" className="btn btn-primary rounded-pill px-5 shadow" disabled={submitting || !selectedBusConfigured}>
            {submitting ? <span className="spinner-border spinner-border-sm me-2"></span> : <MdCheckCircle className="me-2" />}
            Create Route Template
          </button>
        </div>
      </form>
    </div>
  );
};

export default AddRoute;
