import React, { useState, useEffect } from 'react';
import { MdBook, MdDirectionsBus, MdPerson, MdEventSeat, MdPayment, MdInfo, MdPhone, MdLocationOn, MdWc, MdEmail } from 'react-icons/md';
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

  useEffect(() => {
    const fetchFleet = async () => {
      try {
        const res = await operatorService.getFleet(operatorId);
        setFleet(res.data);
      } catch { setFleet([]); }
      finally { setLoading(false); }
    };
    fetchFleet();
  }, [operatorId]);

  const handleBusChange = async (busId) => {
    setSelectedBus(busId);
    if (!busId) { setBookingData(null); return; }
    
    setFetchingBookings(true);
    setError('');
    try {
      const res = await operatorService.getBusBookingStatus(busId, operatorId);
      setBookingData(res.data);
    } catch (err) {
      setError('Failed to fetch bookings for this bus.');
      setBookingData(null);
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
            <h6 className="fw-bold mb-3 d-flex align-items-center gap-2 text-primary">
              <MdDirectionsBus /> Select Bus
            </h6>
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
