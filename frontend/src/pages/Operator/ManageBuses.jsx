import React, { useState, useEffect } from 'react';
import { useNavigate } from 'react-router-dom';
import { MdAdd, MdDirectionsBus, MdSettings, MdVisibility } from 'react-icons/md';
import operatorService from '../../services/operator/operatorService';
import authService from '../../services/authService';

const ManageBuses = () => {
  const [buses, setBuses] = useState([]);
  const [loading, setLoading] = useState(true);
  const [error, setError] = useState('');
  const [showModal, setShowModal] = useState(false);
  const [submitting, setSubmitting] = useState(false);
  const [toggling, setToggling] = useState(null);
  const [formData, setFormData] = useState({ busName: '', busNumber: '', busCategory: 'AC', busStyle: 'Seater', capacity: '' });
  const [summary, setSummary] = useState(null);
  const navigate = useNavigate();
  const user = authService.getCurrentUser();
  const operatorId = user?.userId || user?.UserId;

  useEffect(() => { 
    fetchBuses(); 
    fetchSummary();
  }, []);

  const fetchBuses = async () => {
    try {
      setLoading(true);
      const res = await operatorService.getAllBusesWithRoutes(operatorId);
      setBuses(res.data);
    } catch {
      setBuses([]);
    } finally {
      setLoading(false);
    }
  };

  const fetchSummary = async () => {
    try {
      const res = await operatorService.getFeedbackSummary(operatorId);
      setSummary(res.data);
    } catch {
      setSummary(null);
    }
  };

  const handleAddBus = async (e) => {
    e.preventDefault();
    setSubmitting(true);
    setError('');
    try {
      const combinedType = `${formData.busCategory} ${formData.busStyle}`;
      await operatorService.addBus({ 
        busName: formData.busName,
        busNumber: formData.busNumber,
        busType: combinedType,
        capacity: parseInt(formData.capacity), 
        operatorId 
      });
      setShowModal(false);
      setFormData({ busName: '', busNumber: '', busCategory: 'AC', busStyle: 'Seater', capacity: '' });
      fetchBuses();
    } catch (err) {
      setError(err.response?.data?.message || 'Failed to add bus.');
    } finally {
      setSubmitting(false);
    }
  };

  const handleToggle = async (busId, currentStatus) => {
    setToggling(busId);
    try {
      await operatorService.toggleBusStatus(busId, !currentStatus);
      setBuses(prev => prev.map(b => b.busId === busId ? { ...b, isActive: !currentStatus } : b));
    } catch (err) {
      setError(err.response?.data?.message || 'Failed to update status.');
    } finally {
      setToggling(null);
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
      <div className="d-flex justify-content-between align-items-center mb-4">
        <div>
          <h4 className="fw-bold m-0 text-dark">Manage Buses</h4>
          <p className="text-muted m-0">Add, configure, and manage your fleet</p>
        </div>
        <button className="btn btn-primary rounded-pill px-4 d-flex align-items-center gap-2" onClick={() => setShowModal(true)}>
          <MdAdd size={20} /> Add Bus
        </button>
      </div>

      {error && <div className="alert alert-danger alert-dismissible fade show">{error}<button type="button" className="btn-close" onClick={() => setError('')}></button></div>}

      {/* Stats */}
      <div className="row g-3 mb-4">
        <div className="col-md-3">
          <div className="tripzo-card text-center border-top border-4 border-primary shadow-sm">
            <h3 className="fw-bold text-primary m-0">{buses.length}</h3>
            <p className="text-muted m-0 small uppercase fw-bold">Total Buses</p>
          </div>
        </div>
        <div className="col-md-3">
          <div className="tripzo-card text-center border-top border-4 border-success shadow-sm">
            <h3 className="fw-bold text-success m-0">{buses.filter(b => b.isActive).length}</h3>
            <p className="text-muted m-0 small uppercase fw-bold">Active</p>
          </div>
        </div>
        <div className="col-md-3">
          <div className="tripzo-card text-center border-top border-4 border-warning shadow-sm">
            <h3 className="fw-bold text-warning m-0 text-dark d-flex align-items-center justify-content-center gap-2">
               {summary?.averageRating || 0} <span style={{ fontSize: '1rem', opacity: 0.5 }}>/ 5</span>
            </h3>
            <p className="text-muted m-0 small uppercase fw-bold">Avg Rating</p>
          </div>
        </div>
        <div className="col-md-3">
          <div className="tripzo-card text-center border-top border-4 border-info shadow-sm">
            <h3 className="fw-bold text-info m-0">{summary?.totalFeedbacks || 0}</h3>
            <p className="text-muted m-0 small uppercase fw-bold">Total Feedbacks</p>
          </div>
        </div>
      </div>

      {/* Bus Table */}
      <div className="tripzo-card shadow-sm">
        {buses.length === 0 ? (
          <div className="text-center py-5">
            <MdDirectionsBus size={64} className="text-muted mb-3" style={{ opacity: 0.2 }} />
            <p className="text-muted">No buses found. Add your first bus to get started!</p>
          </div>
        ) : (
          <div className="table-responsive">
            <table className="table table-hover align-middle mb-0">
              <thead className="bg-light">
                <tr>
                  <th className="border-0">Bus Details</th>
                  <th className="border-0">Type</th>
                  <th className="border-0 text-center">Capacity</th>
                  <th className="border-0 text-center">Rating</th>
                  <th className="border-0 text-center">Feedbacks</th>
                  <th className="border-0">Status</th>
                  <th className="border-0">Actions</th>
                </tr>
              </thead>
              <tbody>
                {buses.map(bus => (
                  <tr key={bus.busId}>
                    <td>
                      <div className="d-flex flex-column">
                        <span className="fw-bold text-dark">{bus.busName}</span>
                        <code className="text-primary small" style={{ letterSpacing: '0.5px' }}>{bus.busNumber}</code>
                      </div>
                    </td>
                    <td><span className="badge bg-light text-dark border-0 p-2 px-3 rounded-pill bg-opacity-75">{bus.busType}</span></td>
                    <td className="text-center fw-semibold">{bus.capacity}</td>
                    <td className="text-center">
                      <div className="d-flex align-items-center justify-content-center gap-1">
                        <span className="fw-bold text-warning">{bus.averageRating || 0}</span>
                        <span className="text-muted" style={{ fontSize: '0.9rem' }}>★</span>
                      </div>
                    </td>
                    <td className="text-center">
                       <span className={`badge ${bus.feedbackCount > 0 ? 'bg-primary text-white' : 'bg-light text-muted'}`}>
                          {bus.feedbackCount}
                       </span>
                    </td>                    <td>
                      <div className="form-check form-switch">
                        <input
                          className="form-check-input"
                          type="checkbox"
                          checked={bus.isActive}
                          onChange={() => handleToggle(bus.busId, bus.isActive)}
                          disabled={toggling === bus.busId}
                          style={{ cursor: 'pointer', width: '2.5em', height: '1.3em' }}
                        />
                        <label className="form-check-label small px-2">
                          {toggling === bus.busId ? 'Processing...' : (bus.isActive ? 'Active' : 'Inactive')}
                        </label>
                      </div>
                    </td>
                    <td>
                      <div className="d-flex gap-1">
                        <button
                          className="btn btn-sm btn-outline-primary rounded-pill px-3"
                          onClick={() => navigate(`/operator/buses/${bus.busId}/config`)}
                          title="Configure Seats & Amenities"
                        >
                          <MdSettings size={16} className="me-1" /> Configure
                        </button>
                      </div>
                    </td>
                  </tr>
                ))}
              </tbody>
            </table>
          </div>
        )}
      </div>

      {/* Add Bus Modal */}
      {showModal && (
        <div className="modal d-block" style={{ backgroundColor: 'rgba(15, 61, 145, 0.4)' }}>
          <div className="modal-dialog modal-dialog-centered">
            <div className="modal-content border-0 rounded-4 shadow-lg">
              <div className="modal-header border-0 pb-0 px-4 pt-4">
                <h5 className="modal-title fw-bold">Add New Bus</h5>
                <button type="button" className="btn-close" onClick={() => setShowModal(false)}></button>
              </div>
              <form onSubmit={handleAddBus}>
                <div className="modal-body p-4">
                  <div className="mb-3">
                    <label className="form-label fw-bold small text-muted">BUS NAME</label>
                    <input type="text" className="form-control bg-light border-0 rounded-3 p-3" placeholder="e.g. Tripzo Express" value={formData.busName} onChange={e => setFormData({ ...formData, busName: e.target.value })} required />
                  </div>
                  <div className="mb-3">
                    <label className="form-label fw-bold small text-muted">BUS NUMBER</label>
                    <input type="text" className="form-control bg-light border-0 rounded-3 p-3" placeholder="e.g. TN-01-AB-1234" value={formData.busNumber} onChange={e => setFormData({ ...formData, busNumber: e.target.value })} required />
                  </div>
                  <div className="row">
                    <div className="col-6 mb-3">
                      <label className="form-label fw-bold small text-muted">CLASS</label>
                      <select className="form-select bg-light border-0 rounded-3 p-3" value={formData.busCategory} onChange={e => setFormData({ ...formData, busCategory: e.target.value })}>
                        <option value="AC">AC</option>
                        <option value="Non-AC">Non-AC</option>
                      </select>
                    </div>
                    <div className="col-6 mb-3">
                      <label className="form-label fw-bold small text-muted">TYPE</label>
                      <select className="form-select bg-light border-0 rounded-3 p-3" value={formData.busStyle} onChange={e => setFormData({ ...formData, busStyle: e.target.value })}>
                        <option value="Seater">Seater</option>
                        <option value="Sleeper">Sleeper</option>
                        <option value="Semi-Sleeper">Semi-Sleeper</option>
                      </select>
                    </div>
                  </div>
                  <div className="mb-0">
                    <label className="form-label fw-bold small text-muted">CAPACITY</label>
                    <input type="number" className="form-control bg-light border-0 rounded-3 p-3" placeholder="e.g. 40" value={formData.capacity} onChange={e => setFormData({ ...formData, capacity: e.target.value })} required min="1" max="100" />
                  </div>
                </div>
                <div className="modal-footer border-0 p-4 pt-0">
                  <button type="button" className="btn btn-light rounded-pill px-4" onClick={() => setShowModal(false)}>Cancel</button>
                  <button type="submit" className="btn btn-primary rounded-pill px-4 shadow" disabled={submitting}>
                    {submitting ? <span className="spinner-border spinner-border-sm me-2"></span> : null}
                    Add Bus
                  </button>
                </div>
              </form>
            </div>
          </div>
        </div>
      )}
    </div>
  );
};

export default ManageBuses;
