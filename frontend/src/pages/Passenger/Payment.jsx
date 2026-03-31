import React, { useState, useEffect } from 'react';
import { useNavigate, useLocation } from 'react-router-dom';
import { MdLock, MdCreditCard, MdCheckCircle, MdArrowBack } from 'react-icons/md';
import PassengerLayout from '../../layouts/PassengerLayout';
import authService from '../../services/authService';
import passengerService from '../../services/passengerService';

const Payment = () => {
  const navigate = useNavigate();
  const location = useLocation();
  const {
    orderData, busId, routeId, travelDate,
    selectedSeats, boardingStop, droppingStop,
    fromCity, toCity, bus, grandTotal,
  } = location.state || {};

  const user = authService.getCurrentUser();
  const [loading, setLoading] = useState(false);
  const [error, setError] = useState('');
  const [razorpayReady, setRazorpayReady] = useState(false);

  // Load Razorpay script dynamically
  useEffect(() => {
    const script = document.createElement('script');
    script.src = 'https://checkout.razorpay.com/v1/checkout.js';
    script.onload = () => setRazorpayReady(true);
    script.onerror = () => setError('Failed to load payment gateway. Please refresh.');
    document.body.appendChild(script);
    return () => { document.body.removeChild(script); };
  }, []);

  const handleRazorpayPayment = () => {
    if (!razorpayReady) {
      setError('Payment gateway is loading. Please wait...');
      return;
    }
    if (!orderData?.orderId) {
      setError('Payment order not found. Please go back and try again.');
      return;
    }

    const options = {
      key: orderData.razorpayKeyId,
      amount: Math.round((orderData.amount || grandTotal) * 100), // paise
      currency: orderData.currency || 'INR',
      name: 'Tripzo',
      description: `Bus Booking: ${fromCity} → ${toCity}`,
      order_id: orderData.orderId,
      handler: async function (response) {
        setLoading(true);
        setError('');
        try {
          const verifyPayload = {
            razorpayOrderId: response.razorpay_order_id,
            razorpayPaymentId: response.razorpay_payment_id,
            razorpaySignature: response.razorpay_signature,
            routeId,
            busId,
            userId: user?.userId,
            selectedSeatIds: selectedSeats.map(s => s.seatId),
            journeyDate: travelDate,
            boardingStopId: parseInt(boardingStop),
            droppingStopId: parseInt(droppingStop),
          };
          const res = await passengerService.verifyPayment(verifyPayload);
          navigate('/passenger/success', { state: { booking: res.data, fromCity, toCity, bus } });
        } catch (err) {
          setError(err.response?.data?.message || 'Payment verification failed. Contact support.');
        } finally {
          setLoading(false);
        }
      },
      prefill: {
        name: user?.fullName,
        email: user?.email,
        contact: user?.phoneNumber,
      },
      theme: { color: '#1E63FF' },
      modal: {
        ondismiss: () => setError('Payment was cancelled. Please try again.'),
      },
    };

    const rzp = new window.Razorpay(options);
    rzp.open();
  };

  return (
    <PassengerLayout>
      <div className="row justify-content-center">
        <div className="col-md-7 col-lg-6 position-relative">
          <button className="btn btn-link text-decoration-none p-0 d-flex align-items-center gap-2 text-dark fw-semibold mb-3"
            onClick={() => navigate(-1)}>
            <MdArrowBack size={20} /> Back
          </button>
          
          <h4 className="fw-bold mb-4 text-center">Complete Payment</h4>

          {/* Order Summary */}
          <div className="tripzo-card mb-4">
            <h6 className="fw-bold mb-3">Order Summary</h6>
            <div className="d-flex justify-content-between mb-2">
              <span className="text-muted">Route</span>
              <span className="fw-semibold">{fromCity} → {toCity}</span>
            </div>
            <div className="d-flex justify-content-between mb-2">
              <span className="text-muted">Bus</span>
              <span className="fw-semibold">{bus?.busName}</span>
            </div>
            <div className="d-flex justify-content-between mb-2">
              <span className="text-muted">Seats</span>
              <span className="fw-semibold">{selectedSeats?.map(s => s.seatNumber).join(', ')}</span>
            </div>
            <div className="d-flex justify-content-between mb-2">
              <span className="text-muted">Date</span>
              <span className="fw-semibold">
                {travelDate ? new Date(travelDate).toLocaleDateString('en-IN', { day: '2-digit', month: 'short', year: 'numeric' }) : '—'}
              </span>
            </div>
            <div className="border-top mt-3 pt-3 d-flex justify-content-between">
              <span className="fw-bold fs-5">Total</span>
              <span className="fw-bold fs-5" style={{ color: 'var(--primary-blue)' }}>
                ₹{(orderData?.amount || grandTotal || 0).toFixed(2)}
              </span>
            </div>
          </div>

          {/* Payment Card */}
          <div className="tripzo-card mb-4">
            <div className="text-center mb-4">
              <div className="rounded-circle d-inline-flex align-items-center justify-content-center mb-3"
                style={{ width: 64, height: 64, background: '#E8F0FF' }}>
                <MdCreditCard size={32} color="#1E63FF" />
              </div>
              <h6 className="fw-bold">Secure Payment via Razorpay</h6>
              <p className="text-muted small mb-0">UPI · Cards · Net Banking · Wallets</p>
            </div>

            <div className="p-3 rounded-3 mb-3" style={{ background: '#E8F0FF' }}>
              <div className="d-flex justify-content-between small mb-1">
                <span className="text-muted">Order ID</span>
                <span className="fw-semibold text-truncate ms-2" style={{ maxWidth: 180 }}>
                  {orderData?.orderId || 'Pending'}
                </span>
              </div>
              <div className="d-flex justify-content-between small">
                <span className="text-muted">Amount</span>
                <span className="fw-bold" style={{ color: 'var(--primary-blue)' }}>
                  ₹{(orderData?.amount || grandTotal || 0).toFixed(2)}
                </span>
              </div>
            </div>

            <div className="d-flex align-items-center gap-2 small text-muted mb-4">
              <MdLock color="#22C55E" size={18} />
              <span>256-bit SSL encrypted. Your payment info is safe.</span>
            </div>

            {error && <div className="alert alert-danger small py-2">{error}</div>}

            <button
              className="btn btn-primary w-100 py-2 fw-bold rounded-3"
              style={{ fontSize: '1rem' }}
              onClick={handleRazorpayPayment}
              disabled={loading || !razorpayReady}>
              {loading ? (
                <><span className="spinner-border spinner-border-sm me-2" />Verifying Payment...</>
              ) : !razorpayReady ? (
                'Loading Gateway...'
              ) : (
                <><MdLock className="me-2" />Pay ₹{(orderData?.amount || grandTotal || 0).toFixed(2)}</>
              )}
            </button>
          </div>

          <div className="text-center">
            <img
              src="https://razorpay.com/assets/razorpay-powered.svg"
              alt="Powered by Razorpay"
              height={30}
              onError={e => e.target.style.display = 'none'}
            />
          </div>
        </div>
      </div>
    </PassengerLayout>
  );
};

export default Payment;
