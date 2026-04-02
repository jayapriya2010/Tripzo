import React, { useState, useEffect } from 'react';
import { MdStar, MdPerson, MdClose, MdReply } from 'react-icons/md';
import passengerService from '../../services/passenger/passengerService';

const BusReviewsModal = ({ bus, onClose }) => {
  const [data, setData] = useState(null);
  const [loading, setLoading] = useState(true);

  useEffect(() => {
    if (bus?.busId) {
      fetchReviews();
    }
  }, [bus]);

  const fetchReviews = async () => {
    try {
      setLoading(true);
      const res = await passengerService.getBusFeedbacks(bus.busId);
      setData(res.data);
    } catch (err) {
      console.error('Failed to fetch bus reviews:', err);
    } finally {
      setLoading(false);
    }
  };

  if (!bus) return null;

  const renderStars = (rating, size = 16) => {
    return [...Array(5)].map((_, i) => (
      <MdStar key={i} color={i < rating ? '#F59E0B' : '#E2E8F0'} size={size} />
    ));
  };

  return (
    <div className="modal show d-block" style={{ backgroundColor: 'rgba(0,0,0,0.5)', backdropFilter: 'blur(4px)' }}>
      <div className="modal-dialog modal-dialog-centered modal-lg">
        <div className="modal-content border-0 shadow-lg" style={{ borderRadius: '1.25rem' }}>
          <div className="modal-header border-0 pb-0 pt-4 px-4 overflow-hidden position-relative">
            <div className="position-absolute top-0 start-0 w-100" style={{ height: 4, background: 'var(--primary-blue)' }} />
            <div className="d-flex align-items-center gap-3">
              <div className="bg-primary bg-opacity-10 text-primary rounded-circle d-flex align-items-center justify-content-center" style={{ width: 48, height: 48 }}>
                <MdStar size={24} />
              </div>
              <div>
                <h5 className="modal-title fw-bold">Reviews for {bus.busName}</h5>
                <p className="text-muted small m-0">{bus.busType} • {bus.busNumber || ''}</p>
              </div>
            </div>
            <button type="button" className="btn-close" onClick={onClose}></button>
          </div>

          <div className="modal-body p-4">
            {loading ? (
              <div className="text-center py-5">
                <div className="spinner-border text-primary" role="status" />
                <p className="text-muted mt-3">Loading reviews...</p>
              </div>
            ) : !data || data.reviews.length === 0 ? (
              <div className="text-center py-5 opacity-50">
                <MdStar size={64} color="#CBD5E1" />
                <p className="text-muted mt-3">No reviews yet for this bus.</p>
              </div>
            ) : (
              <div className="row g-4">
                {/* Summary Table */}
                <div className="col-md-4">
                  <div className="p-3 rounded-4 bg-light border border-1 h-100">
                    <div className="text-center mb-3">
                      <h1 className="display-4 fw-bold m-0" style={{ color: 'var(--primary-blue)' }}>{data.averageRating.toFixed(1)}</h1>
                      <div className="d-flex justify-content-center mb-1">{renderStars(Math.round(data.averageRating), 20)}</div>
                      <p className="text-muted small">Based on {data.totalReviews} reviews</p>
                    </div>

                    <div className="d-flex flex-column gap-2">
                      {[5, 4, 3, 2, 1].map(stars => {
                        const count = data[`${['', 'One', 'Two', 'Three', 'Four', 'Five'][stars]}StarCount`];
                        const percent = (count / data.totalReviews) * 100;
                        return (
                          <div key={stars} className="d-flex align-items-center gap-2">
                            <span className="small text-muted fw-bold" style={{ width: 12 }}>{stars}</span>
                            <div className="progress flex-grow-1" style={{ height: 6 }}>
                              <div className="progress-bar bg-warning" style={{ width: `${percent}%` }} />
                            </div>
                            <span className="small text-muted" style={{ width: 25, textAlign: 'right' }}>{count}</span>
                          </div>
                        );
                      })}
                    </div>
                  </div>
                </div>

                {/* Reviews List */}
                <div className="col-md-8">
                  <div style={{ maxHeight: '400px', overflowY: 'auto', paddingRight: '0.5rem' }} className="tripzo-scroll">
                    {data.reviews.map((rev, idx) => (
                      <div key={rev.feedbackId} className={`pb-4 ${idx !== data.reviews.length - 1 ? 'border-bottom mb-4' : ''}`}>
                        <div className="d-flex justify-content-between align-items-center mb-2">
                          <div className="d-flex align-items-center gap-2">
                            <div className="bg-secondary bg-opacity-10 rounded-circle d-flex align-items-center justify-content-center" style={{ width: 28, height: 28 }}>
                              <MdPerson size={16} />
                            </div>
                            <span className="fw-bold small">{rev.passengerName}</span>
                          </div>
                          <span className="text-muted x-small">{new Date(rev.createdAt).toLocaleDateString()}</span>
                        </div>
                        <div className="mb-2">{renderStars(rev.rating, 14)}</div>
                        <p className="small text-dark mb-0 fst-italic">"{rev.comment}"</p>

                        {rev.operatorResponse && (
                          <div className="mt-3 p-3 rounded-3" style={{ background: 'rgba(30, 99, 255, 0.05)', borderLeft: '3px solid var(--primary-blue)' }}>
                            <div className="d-flex align-items-center gap-1 text-primary fw-bold mb-1" style={{ fontSize: '0.7rem' }}>
                              <MdReply /> OPERATOR RESPONSE:
                            </div>
                            <p className="small m-0 text-dark" style={{ lineHeight: '1.4' }}>{rev.operatorResponse}</p>
                          </div>
                        )}
                      </div>
                    ))}
                  </div>
                </div>
              </div>
            )}
          </div>
        </div>
      </div>
    </div>
  );
};

export default BusReviewsModal;
