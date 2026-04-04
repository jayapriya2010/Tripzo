import React, { useState, useEffect } from 'react';
import { useNavigate } from 'react-router-dom';
import { MdSchedule, MdAdd, MdDelete, MdCalendarMonth, MdCheckCircle, MdSearch, MdFilterList, MdChevronLeft, MdChevronRight } from 'react-icons/md';
import operatorService from '../../services/operator/operatorService';
import authService from '../../services/auth/authService';

const Schedule = () => {
  const user = authService.getCurrentUser();
  const operatorId = user?.userId || user?.UserId;

  const [buses, setBuses] = useState([]); // buses with routes
  const [schedules, setSchedules] = useState([]);
  const [loading, setLoading] = useState(true);
  const [error, setError] = useState('');
  const [success, setSuccess] = useState('');
  const [submitting, setSubmitting] = useState(false);
  const [deleting, setDeleting] = useState(null);

  const [formData, setFormData] = useState({ busId: '', routeId: '' });
  const [selectedDates, setSelectedDates] = useState([]);
  const [dateInput, setDateInput] = useState('');
  const [availableRoutes, setAvailableRoutes] = useState([]);
  const [allOperatorRoutes, setAllOperatorRoutes] = useState([]);
  const [conflictData, setConflictData] = useState(null);
  const [showConflictModal, setShowConflictModal] = useState(false);
  const [showReactivateModal, setShowReactivateModal] = useState(false);
  const [selectedBusConfigured, setSelectedBusConfigured] = useState(true);
  
  // Pagination & Filtering
  const [currentPage, setCurrentPage] = useState(1);
  const [totalCount, setTotalCount] = useState(0);
  const [pageSize, setPageSize] = useState(10);
  const [searchTerm, setSearchTerm] = useState('');
  const [filterDate, setFilterDate] = useState('');

  const navigate = useNavigate();

  useEffect(() => { 
    fetchInitialData(); 
  }, []);

  useEffect(() => {
    fetchSchedules();
  }, [currentPage, filterDate, pageSize]);

  // Debounced search
  useEffect(() => {
    const timer = setTimeout(() => {
        if (currentPage !== 1) setCurrentPage(1);
        else fetchSchedules();
    }, 500);
    return () => clearTimeout(timer);
  }, [searchTerm]);

  const fetchInitialData = async () => {
    try {
      const busRes = await operatorService.getAllBusesWithRoutes(operatorId);
      // Requirement: Filter to show only active buses
      const activeBuses = busRes.data.items ? busRes.data.items.filter(b => b.isActive) : (Array.isArray(busRes.data) ? busRes.data.filter(b => b.isActive) : []);
      setBuses(activeBuses);
      
      // Extract all unique routes
      const routes = activeBuses.flatMap(bus => bus.routes || []);
      const uniqueRoutes = Array.from(new Map(routes.map(r => [r.routeId || r.RouteId, r])).values());
      setAllOperatorRoutes(uniqueRoutes);
    } catch { }
  };

  const fetchSchedules = async () => {
    try {
      setLoading(true);
      const res = await operatorService.getSchedules(operatorId, {
          pageNumber: currentPage,
          pageSize: pageSize,
          searchTerm: searchTerm,
          filterDate: filterDate || null
      });
      setSchedules(res.data.items || []);
      setTotalCount(res.data.totalCount || 0);
    } catch {
      setSchedules([]);
      setTotalCount(0);
    } finally {
      setLoading(false);
    }
  };

  const handleBusChange = async (busId) => {
    setFormData({ ...formData, busId, routeId: '' });
    // Show all operator routes instead of just the bus-specific ones
    setAvailableRoutes(allOperatorRoutes);
    
    if (!busId) {
      setSelectedBusConfigured(true);
      return;
    }
    
    try {
      const res = await operatorService.getBusDetail(busId, operatorId);
      const isConfigured = res.data.seats && res.data.seats.length > 0;
      setSelectedBusConfigured(isConfigured);
      if (!isConfigured) {
        setError('Selected bus has NO seat configuration. Please configure seats before scheduling.');
      } else {
        setError('');
      }
    } catch {
      setSelectedBusConfigured(false);
    }
  };

  const addDate = () => {
    if (!dateInput) return;
    const today = new Date().toISOString().split('T')[0];
    if (dateInput < today) { setError('Cannot schedule a past date.'); return; }
    if (selectedDates.includes(dateInput)) { setError('Date already selected.'); return; }

    // Maintenance rule: same bus, same route, no consecutive days
    const newDate = new Date(dateInput);
    const hasConsecutive = selectedDates.some(d => {
      const existingDate = new Date(d);
      const diffInDays = Math.round(Math.abs((newDate - existingDate) / (1000 * 60 * 60 * 24)));
      return diffInDays === 1;
    });

    if (hasConsecutive) {
      setError('Maintenance Rule: A bus cannot run the same route on consecutive days (needs a gap day).');
      return;
    }

    setSelectedDates([...selectedDates, dateInput]);
    setDateInput('');
    setError('');
  };

  const removeDate = (date) => {
    setSelectedDates(selectedDates.filter(d => d !== date));
  };

  const handleSubmit = async (e) => {
    e.preventDefault();
    if (selectedDates.length === 0) { setError('Select at least one date.'); return; }
    setSubmitting(true);
    setError('');
    setSuccess('');
    try {
      await operatorService.createSchedule({
        routeId: parseInt(formData.routeId),
        busId: parseInt(formData.busId),
        scheduledDates: selectedDates.map(d => d + 'T00:00:00')
      });
      setSuccess('Schedule created successfully!');
      setFormData({ busId: '', routeId: '' });
      setSelectedDates([]);
      setAvailableRoutes([]);
      fetchInitialData();
      fetchSchedules();
    } catch (err) {
      if (err.response?.status === 409 && err.response?.data?.isInactiveConflict) {
        setConflictData(err.response.data);
        setShowReactivateModal(true);
      } else {
        setError(err.response?.data?.message || 'Failed to create schedule.');
      }
    } finally {
      setSubmitting(false);
    }
  };

  const handleDeleteSchedule = async (scheduleId) => {
    setDeleting(scheduleId);
    setError('');
    try {
      await operatorService.deleteSchedule(scheduleId);
      setSchedules(prev => prev.filter(s => s.scheduleId !== scheduleId));
      setSuccess('Schedule deactivated successfully.');
    } catch (err) {
      if (err.response?.status === 409) {
        // Requirement: Conflict with active bookings
        setConflictData(err.response.data);
        setShowConflictModal(true);
      } else {
        setError(err.response?.data?.message || 'Failed to delete schedule.');
      }
    } finally {
      setDeleting(null);
    }
  };

  const handleReactivate = async () => {
    if (!conflictData?.conflictScheduleId) return;
    setSubmitting(true);
    try {
      await operatorService.reactivateSchedule(conflictData.conflictScheduleId);
      setSuccess('Schedule reactivated successfully!');
      setShowReactivateModal(false);
      setFormData({ busId: '', routeId: '' });
      setSelectedDates([]);
      fetchInitialData();
      fetchSchedules();
    } catch (err) {
      setError(err.response?.data?.message || 'Failed to reactivate schedule.');
      setShowReactivateModal(false);
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
      <div className="mb-4">
        <h4 className="fw-bold m-0 text-dark">Schedule Management</h4>
        <p className="text-muted m-0 small">Schedule active buses on predefined routes</p>
      </div>

      {error && <div className="alert alert-danger alert-dismissible fade show shadow-sm">{error}<button className="btn-close" onClick={() => setError('')}></button></div>}
      {success && <div className="alert alert-success alert-dismissible fade show shadow-sm">{success}<button className="btn-close" onClick={() => setSuccess('')}></button></div>}

      <div className="row g-4">
        {/* Create Schedule */}
        <div className="col-lg-5">
          <div className="tripzo-card border-top border-4 border-primary shadow-sm">
            <h6 className="fw-bold mb-3 d-flex align-items-center gap-2 text-primary"><MdCalendarMonth /> Create New Schedule</h6>
            <form onSubmit={handleSubmit}>
              <div className="mb-3">
                <label className="form-label fw-bold small text-muted">SELECT ACTIVE BUS</label>
                <select className="form-select bg-light border-0 rounded-3 p-3" value={formData.busId} onChange={e => handleBusChange(e.target.value)} required>
                  <option value="">Choose a bus...</option>
                  {buses.map(bus => (
                    <option key={bus.busId} value={bus.busId}>{bus.busName} ({bus.busNumber})</option>
                  ))}
                </select>
                {formData.busId && !selectedBusConfigured && (
                  <div className="alert alert-warning py-2 px-3 mt-2 small border-0 shadow-sm d-flex align-items-center gap-2">
                    <MdAdd size={18} className="text-warning" />
                    <span>This bus has no seats! <button type="button" className="btn btn-link btn-sm p-0 fw-bold" onClick={() => navigate(`/operator/buses/${formData.busId}/config`)}>Configure now</button></span>
                  </div>
                )}
                {buses.length === 0 && <small className="text-danger mt-1 d-block">No active buses found.</small>}
              </div>

              <div className="mb-3">
                <label className="form-label fw-bold small text-muted">SELECT ROUTE</label>
                <select className="form-select bg-light border-0 rounded-3 p-3" value={formData.routeId} onChange={e => setFormData({ ...formData, routeId: e.target.value })} required disabled={!formData.busId}>
                  <option value="">Choose a route...</option>
                  {availableRoutes.map(route => (
                    <option key={route.routeId} value={route.routeId}>{route.sourceCity} → {route.destCity} (₹{route.baseFare})</option>
                  ))}
                </select>
              </div>

              <div className="mb-3">
                <label className="form-label fw-bold small text-muted">ADD DATES</label>
                <div className="input-group">
                  <input type="date" className="form-control bg-light border-0 p-3" value={dateInput} onChange={e => setDateInput(e.target.value)} min={new Date().toISOString().split('T')[0]} />
                  <button type="button" className="btn btn-primary px-3" onClick={addDate}><MdAdd size={24} /></button>
                </div>
              </div>

              {selectedDates.length > 0 && (
                <div className="mb-3">
                  <div className="d-flex flex-wrap gap-2">
                    {selectedDates.sort().map(date => (
                      <span key={date} className="badge bg-primary bg-opacity-10 text-primary d-flex align-items-center gap-2 px-3 py-2 border border-primary border-opacity-25 rounded-pill">
                        {new Date(date).toLocaleDateString('en-IN', { day: 'numeric', month: 'short', year: 'numeric' })}
                        <MdDelete size={16} className="text-danger" style={{ cursor: 'pointer' }} onClick={() => removeDate(date)} />
                      </span>
                    ))}
                  </div>
                </div>
              )}

              <button type="submit" className="btn btn-primary w-100 rounded-pill py-3 mt-2 shadow" disabled={submitting || !selectedBusConfigured}>
                {submitting ? <span className="spinner-border spinner-border-sm me-2"></span> : <MdCheckCircle className="me-2" />}
                Create Schedule ({selectedDates.length} date{selectedDates.length !== 1 ? 's' : ''})
              </button>
            </form>
          </div>
        </div>

        {/* Scheduled Runs */}
        <div className="col-lg-7">
          <div className="tripzo-card border-top border-4 border-primary shadow-sm">
            <div className="d-flex justify-content-between align-items-center mb-3">
              <h6 className="fw-bold m-0 d-flex align-items-center gap-2 text-primary">
                <MdSchedule /> Scheduled Runs
              </h6>
              <div className="d-flex gap-2">
                  <div className="position-relative" style={{ width: '200px' }}>
                      <MdSearch className="position-absolute top-50 translate-middle-y ms-2 text-muted" size={14} />
                      <input 
                          type="text" 
                          className="form-control form-control-sm rounded-pill ps-4 border-0 bg-light" 
                          placeholder="Route/Bus..." 
                          value={searchTerm}
                          onChange={(e) => setSearchTerm(e.target.value)}
                      />
                  </div>
                  <input 
                      type="date" 
                      className="form-control form-control-sm rounded-pill border-0 bg-light"
                      value={filterDate}
                      onChange={(e) => setFilterDate(e.target.value)}
                  />
              </div>
            </div>

            {schedules.length === 0 ? (
              <div className="text-center py-5">
                <MdSchedule size={48} className="text-muted" style={{ opacity: 0.3 }} />
                <p className="text-muted m-0 mt-2">No schedules found matching criteria.</p>
              </div>
            ) : (
              <div className="table-responsive">
                <table className="table table-hover mb-0">
                  <thead className="bg-light">
                    <tr>
                      <th className="border-0">Route</th>
                      <th className="border-0">Bus</th>
                      <th className="border-0">Date</th>
                      <th className="border-0">Status</th>
                      <th className="border-0">Action</th>
                    </tr>
                  </thead>
                  <tbody>
                    {schedules.map(s => (
                      <tr key={s.scheduleId}>
                        <td className="fw-bold text-dark">{s.routeName}</td>
                        <td className="small">{s.busName}</td>
                        <td className="fw-semibold text-muted small">{new Date(s.scheduledDate).toLocaleDateString('en-IN', { day: 'numeric', month: 'short', year: 'numeric' })}</td>
                        <td>
                          <span className={`badge-status ${s.isActive ? 'badge-active' : 'badge-inactive'}`}>
                            {s.isActive ? 'Active' : 'Inactive'}
                          </span>
                        </td>
                        <td>
                          <button
                            className="btn btn-sm btn-outline-danger border-0 rounded-circle p-2"
                            onClick={() => handleDeleteSchedule(s.scheduleId)}
                            disabled={deleting === s.scheduleId}
                            title="Deactivate Schedule"
                          >
                            {deleting === s.scheduleId ? <span className="spinner-border spinner-border-sm"></span> : <MdDelete size={18} />}
                          </button>
                        </td>
                      </tr>
                    ))}
                  </tbody>
                </table>
              </div>
            )}

            {/* Pagination Controls */}
            {totalCount > 0 && (
                <div className="d-flex justify-content-between align-items-center mt-3 pt-3 border-top">
                    <div className="d-flex align-items-center gap-2">
                        <span className="text-muted small fw-bold text-uppercase" style={{ fontSize: '0.65rem' }}>Show:</span>
                        <select 
                            className="form-select form-select-sm bg-light border-0 shadow-none rounded-3" 
                            style={{ width: 'auto', fontSize: '0.75rem' }}
                            value={pageSize}
                            onChange={(e) => {
                                setPageSize(parseInt(e.target.value));
                                setCurrentPage(1);
                            }}
                        >
                            <option value="5">5</option>
                            <option value="10">10</option>
                            <option value="20">20</option>
                            <option value="50">50</option>
                        </select>
                        <small className="text-muted" style={{ fontSize: '0.75rem' }}>
                            {((currentPage - 1) * pageSize) + 1}–{Math.min(currentPage * pageSize, totalCount)} of {totalCount}
                        </small>
                    </div>
                    
                    <div className="d-flex gap-1">
                        <button 
                            className="btn btn-xs btn-outline-primary rounded-3 px-2 py-1" 
                            disabled={currentPage === 1}
                            onClick={() => setCurrentPage(prev => prev - 1)}
                            style={{ fontSize: '0.75rem' }}
                        >
                            <MdChevronLeft /> Prev
                        </button>
                        {(() => {
                            const totalPages = Math.ceil(totalCount / pageSize);
                            return Array.from({ length: Math.min(totalPages, 5) }, (_, i) => {
                                let page;
                                if (totalPages <= 5) page = i + 1;
                                else if (currentPage <= 3) page = i + 1;
                                else if (currentPage >= totalPages - 2) page = totalPages - 4 + i;
                                else page = currentPage - 2 + i;
                                
                                return (
                                    <button
                                        key={page}
                                        className={`btn btn-xs rounded-3 fw-bold px-2 py-1 ${currentPage === page ? 'btn-primary shadow-sm' : 'btn-outline-primary border-0'}`}
                                        onClick={() => setCurrentPage(page)}
                                        style={{ fontSize: '0.75rem', minWidth: '28px' }}
                                    >
                                        {page}
                                    </button>
                                );
                            });
                        })()}
                        <button 
                            className="btn btn-xs btn-outline-primary rounded-3 px-2 py-1" 
                            disabled={currentPage >= Math.ceil(totalCount / pageSize)}
                            onClick={() => setCurrentPage(prev => prev + 1)}
                            style={{ fontSize: '0.75rem' }}
                        >
                            Next <MdChevronRight />
                        </button>
                    </div>
                </div>
            )}
          </div>
        </div>
      </div>

      {/* Conflict Modal */}
      {showConflictModal && (
        <div className="modal d-block" style={{ backgroundColor: 'rgba(15, 61, 145, 0.4)' }}>
          <div className="modal-dialog modal-dialog-centered">
            <div className="modal-content border-0 rounded-4 shadow-lg">
              <div className="modal-header border-0 bg-danger text-white p-4">
                <h5 className="modal-title fw-bold">Active Bookings Detected</h5>
                <button type="button" className="btn-close btn-close-white" onClick={() => setShowConflictModal(false)}></button>
              </div>
              <div className="modal-body p-4 text-center">
                <MdDelete size={64} className="text-danger mb-3 opacity-25" />
                <p className="fw-bold text-dark fs-5 mb-2">Cannot deactivate this schedule.</p>
                <p className="text-muted mb-4">
                  There are <strong>{conflictData?.bookingCount || 'active'}</strong> bookings confirmed for this run.
                  Deactivating this would strand passengers.
                </p>
                
                <div className="d-grid gap-2">
                  <button 
                    className="btn btn-primary rounded-pill py-3 fw-bold shadow-sm"
                    onClick={() => navigate(`/operator/schedule/reassign`, { state: { scheduleId: conflictData.scheduleId, routeName: conflictData.routeName, date: conflictData.scheduledDate, currentBusId: conflictData.busId } })}
                  >
                    Reassign to Another Bus
                  </button>
                  <button className="btn btn-light rounded-pill py-2" onClick={() => setShowConflictModal(false)}>
                    Close
                  </button>
                </div>
              </div>
            </div>
          </div>
        </div>
      )}
      
      {/* Reactivate Modal */}
      {showReactivateModal && (
        <div className="modal d-block" style={{ backgroundColor: 'rgba(15, 61, 145, 0.4)' }}>
          <div className="modal-dialog modal-dialog-centered">
            <div className="modal-content border-0 rounded-4 shadow-lg overflow-hidden">
              <div className="modal-header border-0 bg-primary text-white p-4">
                <h5 className="modal-title fw-bold d-flex align-items-center gap-2">
                  <MdCalendarMonth /> Inactive Schedule Found
                </h5>
                <button type="button" className="btn-close btn-close-white" onClick={() => setShowReactivateModal(false)}></button>
              </div>
              <div className="modal-body p-4 text-center">
                <div className="mb-3 text-primary opacity-25">
                    <MdSchedule size={64} />
                </div>
                <p className="fw-bold text-dark fs-5 mb-2">Schedule Already Exists</p>
                <p className="text-muted mb-4">
                  An inactive schedule for this route already exists on 
                  <strong> {new Date(conflictData?.conflictDate).toLocaleDateString('en-IN', { day: 'numeric', month: 'short', year: 'numeric' })}</strong>.
                </p>
                
                <div className="d-grid gap-2">
                  <button 
                    className="btn btn-primary rounded-pill py-3 fw-bold shadow-sm"
                    onClick={handleReactivate}
                    disabled={submitting}
                  >
                    {submitting ? <span className="spinner-border spinner-border-sm me-2"></span> : null}
                    Yes, Activate Existing Run
                  </button>
                  <button 
                    className="btn btn-light rounded-pill py-2 text-muted fw-semibold" 
                    onClick={() => {
                      if (conflictData?.conflictDate) {
                        const dateStr = new Date(conflictData.conflictDate).toISOString().split('T')[0];
                        setSelectedDates(prev => prev.filter(d => d !== dateStr));
                      }
                      setShowReactivateModal(false);
                    }}
                  >
                    Try Another Date
                  </button>
                </div>
              </div>
            </div>
          </div>
        </div>
      )}
    </div>
  );
};

export default Schedule;
