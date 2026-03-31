import React, { useState, useEffect } from 'react';
import { MdFeedback, MdStar, MdStarOutline, MdSend, MdHistory } from 'react-icons/md';
import PassengerLayout from '../../layouts/PassengerLayout';
import authService from '../../services/authService';
import passengerService from '../../services/passengerService';

const StarRating = ({ rating, onRate }) => (
  <div className="d-flex gap-1">
    {[1, 2, 3, 4, 5].map(n => (
      <button
        key={n}
        type="button"
        onClick={() => onRate(n)}
        style={{ background: 'none', border: 'none', padding: 4, cursor: 'pointer', transition: 'transform 0.15s' }}
        onMouseEnter={e => e.currentTarget.style.transform = 'scale(1.2)'}
        onMouseLeave={e => e.currentTarget.style.transform = 'scale(1)'}
      >
        {n <= rating
          ? <MdStar size={32} color="#F59E0B" />
          : <MdStarOutline size={32} color="#D1D5DB" />}
      </button>
    ))}
  </div>
);

const Feedback = () => {
  const user = authService.getCurrentUser();
  const [bookings, setBookings] = useState([]);
  const [pastFeedbacks, setPastFeedbacks] = useState([]);
  const [form, setForm] = useState({ bookingId: '', rating: 0, comment: '' });
  const [loading, setLoading] = useState(false);
  const [loadingData, setLoadingData] = useState(true);
  const [toast, setToast] = useState(null);

  useEffect(() => {
    if (!user?.userId) { setLoadingData(false); return; }
    Promise.all([
      passengerService.getHistory(user.userId).then(r => r.data || []).catch(() => []),
      passengerService.getUserFeedbacks(user.userId).then(r => r.data || []).catch(() => []),
    ]).then(([history, feedbacks]) => {
      // Only completed (not cancelled) bookings whose journey date has passed
      const today = new Date();
      const eligible = history.filter(b =>
        b.status === 'Confirmed' &&
        new Date(b.journeyDate) < today &&
        !feedbacks.some(f => f.bookingId === b.bookingId)
      );
      setBookings(eligible);
      setPastFeedbacks(feedbacks);
    }).finally(() => setLoadingData(false));
  }, []);

  const showToast = (msg, type) => {
    setToast({ msg, type });
    setTimeout(() => setToast(null), 4000);
  };

  const handleSubmit = async (e) => {
    e.preventDefault();
    if (!form.bookingId) { showToast('Please select a booking.', 'danger'); return; }
    if (form.rating === 0) { showToast('Please select a rating.', 'danger'); return; }
    setLoading(true);
    try {
      await passengerService.submitFeedback({
        bookingId: parseInt(form.bookingId),
        rating: form.rating,
        comment: form.comment,
      });
      showToast('Thank you for your feedback! 🎉', 'success');
      setForm({ bookingId: '', rating: 0, comment: '' });
      // Refresh
      const [history, feedbacks] = await Promise.all([
        passengerService.getHistory(user.userId).then(r => r.data || []),
        passengerService.getUserFeedbacks(user.userId).then(r => r.data || []),
      ]);
      const today = new Date();
      const eligible = history.filter(b =>
        b.status === 'Confirmed' &&
        new Date(b.journeyDate) < today &&
        !feedbacks.some(f => f.bookingId === b.bookingId)
      );
      setBookings(eligible);
      setPastFeedbacks(feedbacks);
    } catch (err) {
      showToast(err.response?.data?.message || 'Failed to submit feedback.', 'danger');
    } finally {
      setLoading(false);
    }
  };

  const ratingLabel = ['', 'Poor', 'Fair', 'Good', 'Very Good', 'Excellent'];

  return (
    <PassengerLayout>
      {toast && (
        <div className={`alert alert-${toast.type} position-fixed top-0 end-0 m-3 shadow`}
          style={{ zIndex: 9999, minWidth: 300 }}>
          {toast.msg}
        </div>
      )}

      <h4 className="fw-bold mb-4 d-flex align-items-center gap-2">
        <MdFeedback color="var(--primary-blue)" /> Feedback
      </h4>

      <div className="row g-4">
        {/* Submit Feedback Form */}
        <div className="col-md-6">
          <div className="tripzo-card">
            <h6 className="fw-bold mb-4">Submit Feedback</h6>

            {loadingData ? (
              <div className="text-center py-4"><div className="spinner-border text-primary" /></div>
            ) : (
              <form onSubmit={handleSubmit}>
                <div className="mb-4">
                  <label className="form-label fw-semibold small text-muted">SELECT BOOKING</label>
                  {bookings.length === 0 ? (
                    <div className="p-3 rounded-3 text-center" style={{ background: '#F8FAFC', border: '1px dashed #CBD5E1' }}>
                      <p className="text-muted small mb-0">
                        No completed trips available for feedback.<br />
                        Feedback can be submitted after your journey date.
                      </p>
                    </div>
                  ) : (
                    <select className="form-select rounded-3" value={form.bookingId}
                      onChange={e => setForm({ ...form, bookingId: e.target.value })} required>
                      <option value="">Choose a trip...</option>
                      {bookings.map(b => (
                        <option key={b.bookingId} value={b.bookingId}>
                          {b.routeName} — {new Date(b.journeyDate).toLocaleDateString('en-IN')}
                        </option>
                      ))}
                    </select>
                  )}
                </div>

                <div className="mb-4">
                  <label className="form-label fw-semibold small text-muted">YOUR RATING</label>
                  <StarRating rating={form.rating} onRate={r => setForm({ ...form, rating: r })} />
                  {form.rating > 0 && (
                    <p className="mt-1 small fw-semibold" style={{ color: '#F59E0B' }}>
                      {ratingLabel[form.rating]}
                    </p>
                  )}
                </div>

                <div className="mb-4">
                  <label className="form-label fw-semibold small text-muted">
                    COMMENT <span className="text-muted fw-normal">(Optional, max 500 chars)</span>
                  </label>
                  <textarea
                    className="form-control rounded-3"
                    rows={4}
                    placeholder="Share your experience with us..."
                    value={form.comment}
                    maxLength={500}
                    onChange={e => setForm({ ...form, comment: e.target.value })}
                  />
                  <div className="text-end small text-muted mt-1">{form.comment.length}/500</div>
                </div>

                <button
                  type="submit"
                  className="btn btn-primary w-100 rounded-3 py-2 fw-semibold"
                  disabled={loading || bookings.length === 0}>
                  {loading
                    ? <><span className="spinner-border spinner-border-sm me-2" />Submitting...</>
                    : <><MdSend className="me-2" />Submit Feedback</>}
                </button>
              </form>
            )}
          </div>
        </div>

        {/* Past Feedbacks */}
        <div className="col-md-6">
          <div className="tripzo-card">
            <h6 className="fw-bold mb-3 d-flex align-items-center gap-2">
              <MdHistory color="var(--primary-blue)" /> Past Feedbacks
            </h6>
            {pastFeedbacks.length === 0 ? (
              <div className="text-center py-4">
                <MdFeedback size={48} color="#CBD5E1" />
                <p className="text-muted small mt-2 mb-0">No feedbacks submitted yet.</p>
              </div>
            ) : (
              <div style={{ maxHeight: 480, overflowY: 'auto' }}>
                {pastFeedbacks.map(fb => (
                  <div key={fb.feedbackId} className="p-3 mb-3 rounded-3" style={{ background: '#F8FAFC', border: '1px solid #E2E8F0' }}>
                    <div className="d-flex justify-content-between align-items-start mb-1">
                      <span className="fw-semibold small">{fb.routeName}</span>
                      <div className="d-flex">
                        {[1, 2, 3, 4, 5].map(n => (
                          <MdStar key={n} size={14} color={n <= fb.rating ? '#F59E0B' : '#D1D5DB'} />
                        ))}
                      </div>
                    </div>
                    <p className="text-muted small mb-1">🚌 {fb.busName}</p>
                    {fb.comment && <p className="small mb-1" style={{ color: '#374151' }}>"{fb.comment}"</p>}
                    <p className="text-muted" style={{ fontSize: '0.7rem' }}>
                      {new Date(fb.createdAt).toLocaleDateString('en-IN', { day: '2-digit', month: 'short', year: 'numeric' })}
                    </p>
                  </div>
                ))}
              </div>
            )}
          </div>
        </div>
      </div>
    </PassengerLayout>
  );
};

export default Feedback;
