import React, { useState, useEffect } from 'react';
import { MdBook, MdDirectionsBus, MdPerson, MdEventSeat, MdPayment, MdInfo, MdPhone, MdLocationOn, MdWc, MdEmail, MdSearch, MdFilterList, MdChevronLeft, MdChevronRight } from 'react-icons/md';
import operatorService from '../../services/operator/operatorService';
import authService from '../../services/auth/authService';

const OperatorBookings = () => {
  const user = authService.getCurrentUser();
  const operatorId = user?.userId || user?.UserId;

  const [fleet, setFleet] = useState([]);
  const [selectedBus, setSelectedBus] = useState('');
  const [bookingData, setBookingData] = useState(null);
  const [loading, setLoading] = useState(true);
  const [fetchingBookings, setFetchingBookings] = useState(false);
  const [error, setError] = useState('');
  const [selectedPassenger, setSelectedPassenger] = useState(null);
  const [showModal, setShowModal] = useState(false);
  
  // Fleet Sidebar Pagination & Search
  const [busSearchTerm, setBusSearchTerm] = useState('');
  const [fleetCurrentPage, setFleetCurrentPage] = useState(1);
  const [fleetTotalCount, setFleetTotalCount] = useState(0);
  const fleetPageSize = 10;

  // Bookings Pagination & Date Filter
  const [currentPage, setCurrentPage] = useState(1);
  const [totalCount, setTotalCount] = useState(0);
  const [pageSize, setPageSize] = useState(5);
  const [filterDate, setFilterDate] = useState('');

  useEffect(() => {
    fetchFleet();
  }, [operatorId, fleetCurrentPage]);

  // Debounced bus search
  useEffect(() => {
      const timer = setTimeout(() => {
          if (fleetCurrentPage !== 1) setFleetCurrentPage(1);
          else fetchFleet();
      }, 500);
      return () => clearTimeout(timer);
  }, [busSearchTerm]);

  useEffect(() => {
    if (selectedBus) {
        handleBusChange(selectedBus);
    }
  }, [currentPage, filterDate, pageSize]);

  const fetchFleet = async () => {
    try {
      setLoading(true);
      const res = await operatorService.getFleet(operatorId, { 
          pageNumber: fleetCurrentPage, 
          pageSize: fleetPageSize,
          searchTerm: busSearchTerm 
      });
      // Handle both old array response and new PagedResultDTO
      const items = res.data.items || (Array.isArray(res.data) ? res.data : []);
      setFleet(items);
      setFleetTotalCount(res.data.totalCount || items.length);
    } catch { setFleet([]); setFleetTotalCount(0); }
    finally { setLoading(false); }
  };

  const handleBusChange = async (busId) => {
    if (busId !== selectedBus) {
        setSelectedBus(busId);
        setCurrentPage(1);
        setFilterDate('');
        // Return here, the useEffect will trigger the fetch
        return;
    }
    
    setFetchingBookings(true);
    setError('');
    try {
      const res = await operatorService.getBusBookingStatus(busId, operatorId, {
          pageNumber: currentPage,
          pageSize: pageSize,
          filterDate: filterDate || null
      });
      setBookingData(res.data);
      setTotalCount(res.data.totalSchedulesCount || 0);
    } catch (err) {
      setError('Failed to fetch bookings for this bus.');
      setBookingData(null);
      setTotalCount(0);
    } finally {
      setFetchingBookings(false);
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
        <h4 className="fw-bold m-0 text-dark">Bus Bookings</h4>
        <p className="text-muted m-0 small">Monitor real-time booking status for your fleet</p>
      </div>

      <div className="row g-4">
        {/* Selection Sidebar */}
        <div className="col-lg-4">
          <div className="tripzo-card border-top border-4 border-primary">
            <div className="d-flex justify-content-between align-items-center mb-3">
              <h6 className="fw-bold m-0 d-flex align-items-center gap-2 text-primary">
                <MdDirectionsBus /> Select Bus
              </h6>
            </div>
            <div className="position-relative mb-3">
                <MdSearch className="position-absolute top-50 translate-middle-y ms-3 text-muted" />
                <input 
                    type="text" 
                    className="form-control rounded-pill ps-5 border-0 bg-light small" 
                    placeholder="Search bus name..." 
                    value={busSearchTerm}
                    onChange={(e) => setBusSearchTerm(e.target.value)}
                />
            </div>
            <div className="list-group list-group-flush">
              {fleet.map(bus => (
                <button
                  key={bus.busId}
                  className={`list-group-item list-group-item-action border-0 rounded-3 mb-2 d-flex justify-content-between align-items-center p-3 ${selectedBus == bus.busId ? 'bg-primary text-white shadow-sm' : 'bg-light'}`}
                  onClick={() => handleBusChange(bus.busId)}
                >
                  <div className="d-flex flex-column align-items-start">
                    <span className="fw-bold">{bus.busName}</span>
                    <code className={`small ${selectedBus == bus.busId ? 'text-white-50' : 'text-muted'}`}>{bus.busNumber}</code>
                  </div>
                  <span className={`badge rounded-pill ${selectedBus == bus.busId ? 'bg-white text-primary' : 'bg-primary text-white'}`}>
                    {bus.busType.split(' ')[0]}
                  </span>
                </button>
              ))}
              {fleet.length === 0 && <p className="text-muted small text-center py-3">No buses found.</p>}
            </div>

            {fleetTotalCount > fleetPageSize && (
                <div className="d-flex justify-content-center align-items-center gap-3 mt-3 pt-3 border-top">
                    <button 
                        className="btn btn-sm btn-white border shadow-sm rounded-circle p-1" 
                        disabled={fleetCurrentPage === 1}
                        onClick={() => setFleetCurrentPage(prev => prev - 1)}
                    >
                        <MdChevronLeft size={20} />
                    </button>
                    <small className="text-muted fw-bold">Page {fleetCurrentPage}</small>
                    <button 
                        className="btn btn-sm btn-white border shadow-sm rounded-circle p-1" 
                        disabled={fleetCurrentPage >= Math.ceil(fleetTotalCount / fleetPageSize)}
                        onClick={() => setFleetCurrentPage(prev => prev + 1)}
                    >
                        <MdChevronRight size={20} />
                    </button>
                </div>
            )}
          </div>
        </div>

        {/* Bookings Display */}
        <div className="col-lg-8">
          {!selectedBus ? (
            <div className="tripzo-card text-center py-5">
              <MdBook size={64} className="text-muted mb-3 opacity-25" />
              <h5 className="text-muted">Select a bus to view manifest</h5>
              <p className="text-muted small">Choose a vehicle from the left panel to see its upcoming runs and passenger lists.</p>
            </div>
          ) : fetchingBookings ? (
            <div className="tripzo-card text-center py-5">
              <div className="spinner-border text-primary mb-3"></div>
              <p className="text-muted">Fetching manifest data...</p>
            </div>
          ) : error ? (
            <div className="alert alert-danger">{error}</div>
          ) : (
            <>
              <div className="tripzo-card mb-4 p-3 shadow-sm d-flex justify-content-between align-items-center">
                  <h6 className="fw-bold m-0 text-primary d-flex align-items-center gap-2">
                      <MdBook size={20} /> Scheduled Runs & Manifests
                  </h6>
                  <div className="d-flex align-items-center gap-2">
                      <MdFilterList className="text-muted" />
                      <input 
                          type="date" 
                          className="form-control form-control-sm rounded-pill border-0 bg-light" 
                          value={filterDate}
                          onChange={(e) => setFilterDate(e.target.value)}
                      />
                      {filterDate && (
                          <button className="btn btn-sm btn-link text-decoration-none text-danger p-0" onClick={() => setFilterDate('')}>Clear</button>
                      )}
                  </div>
              </div>

              {(!bookingData?.scheduleBookings || bookingData.scheduleBookings.length === 0) ? (
                <div className="tripzo-card text-center py-5">
                  <p className="text-muted">No active schedules found for this bus.</p>
                </div>
              ) : (
                bookingData.scheduleBookings.map(sched => (
                  <div key={sched.scheduleId} className="tripzo-card mb-4 shadow-sm border-0">
                    <div className="d-flex justify-content-between align-items-center mb-4">
                      <div>
                        <h6 className="fw-bold text-dark m-0">{sched.routeName}</h6>
                        <small className="text-muted">{new Date(sched.scheduledDate).toLocaleDateString('en-IN', { day: 'numeric', month: 'long', year: 'numeric' })}</small>
                      </div>
                      <div className="d-flex gap-2">
                        <span className="badge bg-primary bg-opacity-10 text-primary">Booked: {sched.bookedSeats}/{sched.totalSeats}</span>
                        <span className="badge bg-success bg-opacity-10 text-success">Revenue: ₹{sched.totalRevenue.toLocaleString()}</span>
                      </div>
                    </div>

                    <h6 className="fw-bold mb-3 d-flex align-items-center gap-2 small text-muted">
                      <MdPerson /> Passenger Manifest
                    </h6>
                    
                    {(!sched.passengerDetails || sched.passengerDetails.length === 0) ? (
                      <div className="bg-light rounded-3 p-4 text-center">
                        <p className="text-muted small m-0">No passengers booked for this run yet.</p>
                      </div>
                    ) : (
                      <div className="table-responsive">
                        <table className="table table-hover align-middle mb-0">
                          <thead className="bg-light">
                            <tr>
                              <th className="border-0 x-small" style={{ fontSize: '0.75rem' }}>PASSENGER</th>
                              <th className="border-0 x-small text-center" style={{ fontSize: '0.75rem' }}>SEAT</th>
                              <th className="border-0 x-small text-end" style={{ fontSize: '0.75rem' }}>AMOUNT</th>
                              <th className="border-0 x-small text-end" style={{ fontSize: '0.75rem' }}>ACTION</th>
                            </tr>
                          </thead>
                          <tbody>
                            {sched.passengerDetails.map(p => (
                              <tr key={`${p.bookingId}-${p.seatNumber}`}>
                                <td>
                                  <div className="d-flex align-items-center gap-2">
                                    <div className="bg-primary bg-opacity-10 text-primary rounded-circle d-flex align-items-center justify-content-center" style={{ width: 28, height: 28 }}>
                                      <MdPerson size={14} />
                                    </div>
                                    <div className="d-flex flex-column">
                                      <span className="fw-bold small">{p.passengerName}</span>
                                      <span className="text-muted x-small" style={{ fontSize: '0.65rem' }}>{p.passengerEmail}</span>
                                    </div>
                                  </div>
                                </td>
                                <td className="text-center">
                                  <span className="badge bg-light text-dark border small">
                                    <MdEventSeat size={12} className="me-1 text-primary" /> {p.seatNumber}
                                  </span>
                                </td>
                                <td className="text-end fw-bold small">₹{p.amount.toLocaleString()}</td>
                                <td className="text-end">
                                  <button 
                                    className="btn btn-outline-primary btn-sm rounded-circle p-1" 
                                    style={{ width: 28, height: 28, display: 'inline-flex', alignItems: 'center', justifyContent: 'center' }}
                                    onClick={() => {
                                      setSelectedPassenger(p);
                                      setShowModal(true);
                                    }}
                                    title="View Details"
                                  >
                                    <MdInfo size={16} />
                                  </button>
                                </td>
                              </tr>
                            ))}
                          </tbody>
                        </table>
                      </div>
                    )}
                  </div>
                ))
              )}

              {/* Schedules Pagination */}
              {totalCount > 0 && (
                  <div className="d-flex justify-content-between align-items-center mt-2 px-2 bg-white rounded-3 p-3 shadow-sm mb-4 border-top">
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
                        <small className="text-muted fw-bold" style={{ fontSize: '0.75rem' }}>
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
            </>
          )}
        </div>
      </div>

      {/* Passenger Details Modal */}
      {showModal && selectedPassenger && (
        <div className="modal show d-block" style={{ backgroundColor: 'rgba(0,0,0,0.5)', backdropFilter: 'blur(4px)' }}>
          <div className="modal-dialog modal-dialog-centered">
            <div className="modal-content border-0 shadow-lg" style={{ borderRadius: '1.25rem' }}>
              <div className="modal-header border-0 pb-0 pt-4 px-4 position-relative">
                <div className="position-absolute top-0 start-0 w-100" style={{ height: 4, background: 'var(--primary-blue)' }} />
                <h5 className="modal-title fw-bold d-flex align-items-center gap-2">
                  <MdPerson className="text-primary" /> Passenger Details
                </h5>
                <button type="button" className="btn-close" onClick={() => setShowModal(false)}></button>
              </div>
              <div className="modal-body p-4">
                <div className="mb-4 text-center">
                  <div className="bg-primary bg-opacity-10 text-primary rounded-circle d-flex align-items-center justify-content-center mx-auto mb-2" style={{ width: 64, height: 64 }}>
                    <MdPerson size={32} />
                  </div>
                  <h5 className="fw-bold m-0">{selectedPassenger.passengerName}</h5>
                  <span className="badge bg-light text-dark border small mt-1">Seat: {selectedPassenger.seatNumber}</span>
                </div>

                <div className="row g-4">
                  <div className="col-12">
                    <div className="p-2 border rounded-3 bg-light">
                      <label className="text-muted small fw-bold d-block mb-1"><MdEmail className="me-1 text-primary" /> EMAIL</label>
                      <p className="small text-dark mb-0 fw-semibold" style={{ wordBreak: 'break-all' }}>{selectedPassenger.passengerEmail}</p>
                    </div>
                  </div>
                  <div className="col-6">
                    <label className="text-muted small fw-bold d-block mb-1"><MdPhone className="me-1" /> PHONE</label>
                    <p className="small text-dark mb-0">{selectedPassenger.phoneNumber || 'N/A'}</p>
                  </div>
                  <div className="col-6 text-end">
                    <label className="text-muted small fw-bold d-block mb-1 text-end"><MdWc className="me-1" /> GENDER</label>
                    <p className="small text-dark mb-0">{selectedPassenger.gender || 'N/A'}</p>
                  </div>
                  <div className="col-12">
                    <div className="d-flex justify-content-between align-items-center p-2 rounded-3" style={{ background: 'var(--primary-blue)', color: 'white' }}>
                      <span className="small fw-bold"><MdPayment className="me-1" /> AMOUNT PAID</span>
                      <span className="fw-bold">₹{selectedPassenger.amount.toLocaleString()}</span>
                    </div>
                  </div>
                  <div className="col-12">
                    <hr className="my-2 opacity-50" />
                  </div>
                  <div className="col-12">
                    <div className="d-flex align-items-center gap-3">
                      <div className="flex-grow-1">
                        <label className="text-muted small fw-bold d-block mb-1"><MdLocationOn className="me-1 text-success" /> BOARDING</label>
                        <p className="small text-dark mb-0 fw-semibold">{selectedPassenger.boardingStop}</p>
                      </div>
                      <div className="text-muted opacity-25">→</div>
                      <div className="flex-grow-1 text-end">
                        <label className="text-muted small fw-bold d-block mb-1"><MdLocationOn className="me-1 text-danger" /> DROPPING</label>
                        <p className="small text-dark mb-0 fw-semibold">{selectedPassenger.droppingStop}</p>
                      </div>
                    </div>
                  </div>
                </div>
              </div>
              <div className="modal-footer border-0 p-4 pt-0">
                <button type="button" className="btn btn-light w-100 rounded-3 fw-bold" onClick={() => setShowModal(false)}>Close Details</button>
              </div>
            </div>
          </div>
        </div>
      )}
    </div>
  );
};

export default OperatorBookings;
