import React, { useState, useEffect } from 'react';
import { useLocation, useNavigate } from 'react-router-dom';
import { MdArrowBack, MdSync, MdDirectionsBus, MdCheckCircle, MdWarning } from 'react-icons/md';
import operatorService from '../../services/operator/operatorService';
import authService from '../../services/authService';

const ReassignBus = () => {
  const location = useLocation();
  const navigate = useNavigate();
  const user = authService.getCurrentUser();
  const operatorId = user?.userId || user?.UserId;

  const { scheduleId, routeName, date } = location.state || {};
  const [fleet, setFleet] = useState([]);
  const [selectedBus, setSelectedBus] = useState('');
  const [loading, setLoading] = useState(true);
  const [submitting, setSubmitting] = useState(false);
  const [error, setError] = useState('');
  const [success, setSuccess] = useState('');

  useEffect(() => {
    if (!scheduleId) {
      navigate('/operator/schedule');
      return;
    }

    const fetchFleet = async () => {
      try {
        const res = await operatorService.getFleet(operatorId);
        // Filter: Must be active and different from current bus (though we don't strictly have current bus ID in state)
        // Also ensure bus is compatible with route capacity if needed, but for now just active fleet.
        setFleet(res.data.filter(b => b.isActive));
      } catch { 
        setFleet([]); 
      } finally { 
        setLoading(false); 
      }
    };
    fetchFleet();
  }, [scheduleId, operatorId]);

  const handleReassign = async (e) => {
    e.preventDefault();
    if (!selectedBus) return;

    setSubmitting(true);
    setError('');
    setSuccess('');
    try {
      await operatorService.reassignBus({
        scheduleId: parseInt(scheduleId),
        newBusId: parseInt(selectedBus)
      });
      setSuccess('Bus reassigned successfully! All bookings transferred.');
      setTimeout(() => navigate('/operator/schedule'), 1500);
    } catch (err) {
      setError(err.response?.data?.message || 'Failed to reassign bus.');
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
      <button className="btn btn-link text-decoration-none p-0 mb-3 d-flex align-items-center gap-1 text-muted fw-bold" onClick={() => navigate('/operator/schedule')}>
        <MdArrowBack /> Back to Schedules
      </button>

      <div className="mb-4">
        <h4 className="fw-bold m-0 p-0 text-dark">Reassign Bus for Schedule</h4>
        <p className="text-muted m-0 small">Transfer passengers to a new vehicle for a confirmed run</p>
      </div>

      <div className="row g-4">
        <div className="col-lg-6">
          <div className="tripzo-card border-top border-4 border-warning shadow-sm">
            <h6 className="fw-bold mb-3 d-flex align-items-center gap-2 text-warning"><MdWarning /> Schedule Context</h6>
            <div className="bg-light rounded-3 p-3 border border-1 border-warning border-opacity-25">
              <div className="mb-2">
                <p className="text-muted x-small m-0 fw-bold">ROUTE</p>
                <p className="fw-bold text-dark m-0">{routeName}</p>
              </div>
              <div className="mb-0">
                <p className="text-muted x-small m-0 fw-bold">DATE</p>
                <p className="fw-bold text-dark m-0">{new Date(date).toLocaleDateString('en-IN', { day: 'numeric', month: 'long', year: 'numeric' })}</p>
              </div>
            </div>
            <p className="mt-3 small text-muted">A conflict exists because passengers have already booked tickets for this schedule. You must assign a replacement bus to continue.</p>
          </div>
        </div>

        <div className="col-lg-6">
          <div className="tripzo-card border-top border-4 border-primary shadow-sm h-100">
            <h6 className="fw-bold mb-3 d-flex align-items-center gap-2 text-primary"><MdDirectionsBus /> Assign New Vehicle</h6>
            {error && <div className="alert alert-danger shadow-sm small">{error}</div>}
            {success && <div className="alert alert-success shadow-sm small">{success}</div>}
            
            <form onSubmit={handleReassign}>
              <div className="mb-4">
                <label className="form-label fw-bold small text-muted">SELECT REPLACEMENT BUS</label>
                <div className="list-group">
                  {fleet.length === 0 ? (
                    <p className="text-danger small p-3 bg-danger bg-opacity-10 rounded-3 border">No active buses available for reassignment. Please activate more buses first.</p>
                  ) : (
                    fleet.map(bus => (
                      <label key={bus.busId} className={`list-group-item d-flex justify-content-between align-items-center rounded-3 mb-2 p-3 border-0 ${selectedBus == bus.busId ? 'bg-primary text-white' : 'bg-light'}`} style={{ cursor: 'pointer' }}>
                        <div className="d-flex align-items-center gap-3">
                          <input 
                            className="form-check-input mt-0" 
                            type="radio" 
                            name="busSelection"
                            value={bus.busId}
                            checked={selectedBus == bus.busId}
                            onChange={(e) => setSelectedBus(e.target.value)}
                            required
                          />
                          <div className="d-flex flex-column align-items-start">
                            <span className="fw-bold">{bus.busName}</span>
                            <code className={`x-small ${selectedBus == bus.busId ? 'text-white-50' : 'text-primary'}`}>{bus.busNumber}</code>
                          </div>
                        </div>
                        <span className={`badge rounded-pill ${selectedBus == bus.busId ? 'bg-white text-primary' : 'bg-primary text-white'}`}>
                          {bus.capacity} seats
                        </span>
                      </label>
                    ))
                  )}
                </div>
              </div>

              <div className="d-grid gap-2">
                <button type="submit" className="btn btn-primary rounded-pill py-3 fw-bold shadow" disabled={submitting || !selectedBus}>
                  {submitting ? <span className="spinner-border spinner-border-sm me-2"></span> : <MdSync className="me-2" size={20} />}
                  Complete Reassignment
                </button>
                <button type="button" className="btn btn-light rounded-pill py-2" onClick={() => navigate('/operator/schedule')}>
                  Cancel & Go Back
                </button>
              </div>
            </form>
          </div>
        </div>
      </div>
    </div>
  );
};

export default ReassignBus;
