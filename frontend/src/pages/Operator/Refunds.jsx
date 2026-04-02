import React, { useState, useEffect } from 'react';
import { MdMoneyOff, MdCheckCircle, MdWarning, MdRefresh, MdLock, MdCreditCard, MdError } from 'react-icons/md';

import operatorService from '../../services/operator/operatorService';
import authService from '../../services/auth/authService';

const Refunds = () => {
  const user = authService.getCurrentUser();
  const operatorId = user?.userId || user?.UserId;

  const [cancellations, setCancellations] = useState([]);
  const [loading, setLoading] = useState(true);
  const [processing, setProcessing] = useState(null);
  const [showModal, setShowModal] = useState(false);
  const [selectedBooking, setSelectedBooking] = useState(null);
  const [refundAmount, setRefundAmount] = useState('');
  const [refundState, setRefundState] = useState('idle'); // idle | processing | success | error
  const [refundMessage, setRefundMessage] = useState('');
  const [toast, setToast] = useState(null);

  useEffect(() => { fetchCancellations(); }, []);

  const fetchCancellations = async () => {
    try {
      setLoading(true);
      const res = await operatorService.getApprovedCancellations(operatorId);
      setCancellations(res.data || []);
    } catch {
      setCancellations([]);
    } finally {
      setLoading(false);
    }
  };

  const showToast = (msg, type) => {
    setToast({ msg, type });
    setTimeout(() => setToast(null), 5000);
  };

  const openRefundModal = (booking) => {
    setSelectedBooking(booking);
    setRefundAmount(booking.refundAmount);
    setRefundState('idle');
    setRefundMessage('');
    setShowModal(true);
  };

  const closeModal = () => {
    if (refundState === 'processing') return; // prevent close during processing
    setShowModal(false);
    setSelectedBooking(null);
    setRefundState('idle');
  };

  const handleRefund = async () => {
    if (!selectedBooking) return;
    const amount = parseFloat(refundAmount);
    if (!amount || amount <= 0) {
      setRefundMessage('Please enter a valid refund amount.');
      return;
    }
    if (amount > selectedBooking.refundAmount) {
      setRefundMessage(`Amount cannot exceed original booking amount of ₹${selectedBooking.refundAmount}.`);
      return;
    }

    setRefundState('processing');
    setRefundMessage('');
    setProcessing(selectedBooking.bookingId);

    try {
      await operatorService.processRefund({
        bookingId: selectedBooking.bookingId,
        refundAmount: amount,
        refundReason: 'Admin approved cancellation refund',
        refundProcessedDate: new Date().toISOString(),
        selectedSeatIds: selectedBooking.bookedSeats?.map(s => s.bookedSeatId) || []
      });

      setRefundState('success');
      setRefundMessage(`Refund of ₹${amount.toFixed(2)} processed successfully via Razorpay. The passenger will receive the amount within 5-7 business days.`);
      setCancellations(prev => prev.filter(c => c.bookingId !== selectedBooking.bookingId));
      showToast(`Refund of ₹${amount.toFixed(2)} to ${selectedBooking.passengerName} processed!`, 'success');
    } catch (err) {
      setRefundState('error');
      setRefundMessage(err.response?.data?.message || 'Razorpay refund failed. Please try again or contact support.');
    } finally {
      setProcessing(null);
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
      {/* Toast */}
      {toast && (
        <div className={`alert alert-${toast.type} position-fixed top-0 end-0 m-3 shadow`}
          style={{ zIndex: 9999, minWidth: 300 }}>
          {toast.msg}
        </div>
      )}

      <div className="d-flex align-items-center justify-content-between mb-4 flex-wrap gap-2">
        <div>
          <h4 className="fw-bold m-0 text-dark">Refund Management</h4>
        </div>
        <button className="btn btn-outline-primary btn-sm rounded-pill d-flex align-items-center gap-1"
          onClick={fetchCancellations}>
          <MdRefresh size={16} /> Refresh
        </button>
      </div>

      {/* Stats */}
      <div className="row g-3 mb-4">
        <div className="col-md-6">
          <div className="tripzo-card text-center border-top border-4 border-warning">
            <h3 className="fw-bold text-warning">{cancellations.length}</h3>
            <p className="text-muted m-0 small fw-bold">PENDING REFUNDS</p>
          </div>
        </div>
        <div className="col-md-6">
          <div className="tripzo-card text-center border-top border-4 border-danger">
            <h3 className="fw-bold text-danger">
              ₹{cancellations.reduce((sum, c) => sum + c.refundAmount, 0).toLocaleString()}
            </h3>
            <p className="text-muted m-0 small fw-bold">TOTAL REFUND AMOUNT</p>
          </div>
        </div>
      </div>

      {/* Table */}
      <div className="tripzo-card border-top border-4 border-primary">
        {cancellations.length === 0 ? (
          <div className="text-center py-5">
            <MdCheckCircle size={64} className="mb-3" style={{ color: '#CBD5E1' }} />
            <p className="text-muted fw-bold mb-0">No pending refunds. All caught up!</p>
          </div>
        ) : (
          <div className="table-responsive">
            <table className="table table-hover align-middle mb-0">
              <thead className="bg-light">
                <tr>
                  <th className="border-0">Passenger</th>
                  <th className="border-0">Route</th>
                  <th className="border-0">Journey Date</th>
                  <th className="border-0">Refund Amount</th>
                  <th className="border-0 text-center">Reason</th>
                  <th className="border-0 text-center">Action</th>
                </tr>
              </thead>
              <tbody>
                {cancellations.map(c => (
                  <tr key={c.bookingId}>
                    <td>
                      <p className="fw-bold m-0 small">{c.passengerName}</p>
                      <p className="text-muted m-0" style={{ fontSize: '0.75rem' }}>{c.passengerEmail}</p>
                    </td>
                    <td className="small fw-semibold">{c.routeName}</td>
                    <td className="small">{new Date(c.journeyDate).toLocaleDateString('en-IN', { day: '2-digit', month: 'short', year: 'numeric' })}</td>
                    <td>
                      <span className="fw-bold text-danger">₹{c.refundAmount}</span>
                    </td>
                    <td className="text-center">
                      <span className="text-muted small text-truncate d-inline-block" style={{ maxWidth: 140 }} title={c.cancellationReason}>
                        {c.cancellationReason || '—'}
                      </span>
                    </td>
                    <td className="text-center">
                      <button
                        className="btn btn-sm btn-primary rounded-pill px-3 shadow-sm d-inline-flex align-items-center gap-1"
                        onClick={() => openRefundModal(c)}
                        disabled={processing === c.bookingId}
                      >
                        {processing === c.bookingId
                          ? <span className="spinner-border spinner-border-sm" />
                          : <><MdCreditCard size={14} /> Initiate Refund</>}
                      </button>
                    </td>
                  </tr>
                ))}
              </tbody>
            </table>
          </div>
        )}
      </div>

      {/* Razorpay Refund Modal */}
      {showModal && selectedBooking && (
        <div className="modal d-block" style={{ backgroundColor: 'rgba(15, 61, 145, 0.45)' }}>
          <div className="modal-dialog modal-dialog-centered">
            <div className="modal-content border-0 rounded-4 shadow-lg overflow-hidden">

              {/* Modal Header */}
              <div className="modal-header border-0 px-4 pt-4 pb-2">
                <div className="d-flex align-items-center gap-2">
                  <div className="rounded-circle d-flex align-items-center justify-content-center"
                    style={{ width: 40, height: 40, background: '#E8F0FF' }}>
                    <MdCreditCard size={22} color="#1E63FF" />
                  </div>
                  <div>
                    <h5 className="fw-bold m-0">Initiate Razorpay Refund</h5>
                    <small className="text-muted">Test Mode</small>
                  </div>
                </div>
                {refundState !== 'processing' && (
                  <button type="button" className="btn-close" onClick={closeModal}></button>
                )}
              </div>

              <div className="modal-body px-4 pb-0">

                {/* Success State */}
                {refundState === 'success' && (
                  <div className="text-center py-4">
                    <div className="rounded-circle d-inline-flex align-items-center justify-content-center mb-3"
                      style={{ width: 72, height: 72, background: '#DCFCE7' }}>
                      <MdCheckCircle size={40} color="#22C55E" />
                    </div>
                    <h6 className="fw-bold mb-2">Refund Initiated Successfully!</h6>
                    <p className="text-muted small mb-3">{refundMessage}</p>
                    <div className="p-2 rounded-3 mb-2" style={{ background: '#E8F0FF' }}>
                      <small className="text-primary fw-semibold">✅ Razorpay refund triggered · Status: Processed</small>
                    </div>
                  </div>
                )}

                {/* Error State */}
                {refundState === 'error' && (
                  <div className="alert alert-danger d-flex align-items-start gap-2 mb-3 rounded-3">
                    <MdError size={20} className="flex-shrink-0 mt-1" />
                    <small className="fw-semibold">{refundMessage}</small>
                  </div>
                )}

                {/* Normal / Processing State */}
                {(refundState === 'idle' || refundState === 'processing' || refundState === 'error') && (
                  <>
                    {/* Booking Summary */}
                    <div className="bg-light rounded-3 p-3 mb-3 border">
                      <div className="row g-2">
                        <div className="col-12 mb-2">
                          <p className="text-muted mb-1 fw-bold" style={{ fontSize: '0.7rem' }}>SEATS FOR REFUND</p>
                          <div className="d-flex flex-wrap gap-2 text-dark">
                            {selectedBooking.bookedSeats?.map(s => (
                              <span key={s.bookedSeatId} className="badge bg-white text-dark border fw-bold">
                                {s.seatNumber}
                              </span>
                            ))}
                          </div>
                        </div>
                        <div className="col-6">
                          <p className="text-muted mb-0 fw-bold" style={{ fontSize: '0.7rem' }}>PASSENGER</p>
                          <p className="fw-bold m-0 small">{selectedBooking.passengerName}</p>
                        </div>
                        <div className="col-6">
                          <p className="text-muted mb-0 fw-bold" style={{ fontSize: '0.7rem' }}>ROUTE</p>
                          <p className="fw-semibold m-0 small">{selectedBooking.routeName}</p>
                        </div>
                        <div className="col-6">
                          <p className="text-muted mb-0 fw-bold" style={{ fontSize: '0.7rem' }}>JOURNEY DATE</p>
                          <p className="fw-semibold m-0 small">{new Date(selectedBooking.journeyDate).toLocaleDateString('en-IN', { day: '2-digit', month: 'short', year: 'numeric' })}</p>
                        </div>
                        <div className="col-6">
                          <p className="text-muted mb-0 fw-bold" style={{ fontSize: '0.7rem' }}>EMAIL</p>
                          <p className="fw-semibold m-0 small">{selectedBooking.passengerEmail}</p>
                        </div>
                        {selectedBooking.cancellationReason && (
                          <div className="col-12">
                            <p className="text-muted mb-0 fw-bold" style={{ fontSize: '0.7rem' }}>CANCELLATION REASON</p>
                            <p className="m-0 small text-danger border-start border-3 border-danger ps-2 mt-1 fst-italic">
                              "{selectedBooking.cancellationReason}"
                            </p>
                          </div>
                        )}
                      </div>
                    </div>

                    {/* Refund Amount Input */}
                    <div className="mb-3">
                      <label className="form-label fw-bold small text-muted uppercase-text">REFUND AMOUNT (₹)</label>
                      <div className="input-group">
                        <span className="input-group-text fw-bold fs-5 bg-light border-2">₹</span>
                        <input
                          type="number"
                          className="form-control rounded-end-3 fw-bold fs-5 text-primary bg-light border-2 shadow-none"
                          value={refundAmount}
                          onChange={e => setRefundAmount(e.target.value)}
                          min="1"
                          max={selectedBooking.refundAmount}
                          disabled={refundState === 'processing'}
                        />
                      </div>
                      <small className="text-muted">Calculated refund for {selectedBooking.bookedSeats?.length || 0} seat(s): ₹{selectedBooking.refundAmount.toFixed(2)}</small>
                    </div>

                    {refundMessage && refundState !== 'error' && (
                      <p className="text-danger small">{refundMessage}</p>
                    )}

                    {/* Warning Banner */}
                    <div className="alert border-0 shadow-sm d-flex align-items-start gap-2 mb-3 rounded-4" style={{ background: '#FFF7ED' }}>
                      <MdWarning size={18} color="#F59E0B" className="flex-shrink-0 mt-1" />
                      <small className="fw-semibold" style={{ color: '#92400E' }}>
                        This will call the <strong>Razorpay Refund API</strong> (test mode). Action cannot be undone. Seats will be released.
                      </small>
                    </div>
                  </>
                )}
              </div>

              {/* Modal Footer */}
              <div className="modal-footer border-0 px-4 py-3">
                {refundState === 'success' ? (
                  <button className="btn btn-success rounded-pill px-4 w-100 fw-bold" onClick={closeModal}>
                    Done
                  </button>
                ) : (
                  <>
                    <button className="btn btn-light rounded-pill px-4" onClick={closeModal}
                      disabled={refundState === 'processing'}>
                      Cancel
                    </button>
                    <button
                      className="btn btn-primary rounded-pill px-4 shadow fw-bold d-flex align-items-center gap-2"
                      onClick={handleRefund}
                      disabled={refundState === 'processing'}
                    >
                      {refundState === 'processing' ? (
                        <><span className="spinner-border spinner-border-sm" /> Processing Refund...</>
                      ) : (
                        <><MdLock size={16} /> Confirm & Send Refund</>
                      )}
                    </button>
                  </>
                )}
              </div>
            </div>
          </div>
        </div>
      )}
    </div>
  );
};

export default Refunds;
