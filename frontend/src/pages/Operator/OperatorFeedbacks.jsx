import React, { useState, useEffect } from 'react';
import { MdChat, MdStar, MdPerson, MdDirectionsBus, MdReply, MdCheckCircle } from 'react-icons/md';
import operatorService from '../../services/operator/operatorService';
import authService from '../../services/auth/authService';

const OperatorFeedbacks = () => {
  const user = authService.getCurrentUser();
  const operatorId = user?.userId || user?.UserId;

  const [feedbacks, setFeedbacks] = useState([]);
  const [loading, setLoading] = useState(true);
  const [error, setError] = useState('');
  const [success, setSuccess] = useState('');
  const [replying, setReplying] = useState(null);
  const [responseText, setResponseText] = useState('');
  const [submittingResponse, setSubmittingResponse] = useState(false);

  useEffect(() => {
    fetchFeedbacks();
  }, [operatorId]);

  const fetchFeedbacks = async () => {
    try {
      setLoading(true);
      const res = await operatorService.getFeedbacks(operatorId);
      setFeedbacks(res.data);
    } catch {
      setFeedbacks([]);
    } finally {
      setLoading(false);
    }
  };

  const handleRespond = async (e) => {
    e.preventDefault();
    if (!responseText.trim()) return;

    setSubmittingResponse(true);
    setError('');
    setSuccess('');
    try {
      await operatorService.respondToFeedback({
        feedbackId: replying.feedbackId,
        response: responseText
      });
      setSuccess('Response submitted successfully!');
      setReplying(null);
      setResponseText('');
      fetchFeedbacks();
    } catch (err) {
      setError(err.response?.data?.message || 'Failed to submit response.');
    } finally {
      setSubmittingResponse(false);
    }
  };

  const renderStars = (rating) => {
    return [...Array(5)].map((_, i) => (
      <MdStar key={i} color={i < rating ? '#F59E0B' : '#E2E8F0'} size={18} />
    ));
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
        <h4 className="fw-bold m-0 text-dark">Passenger Feedbacks</h4>
        <p className="text-muted m-0 small">Listen to your passengers and respond to their concerns</p>
      </div>

      {error && <div className="alert alert-danger alert-dismissible fade show shadow-sm">{error}<button className="btn-close" onClick={() => setError('')}></button></div>}
      {success && <div className="alert alert-success alert-dismissible fade show shadow-sm">{success}<button className="btn-close" onClick={() => setSuccess('')}></button></div>}

      <div className="row g-4">
        {feedbacks.length === 0 ? (
          <div className="col-12">
            <div className="tripzo-card text-center py-5">
              <MdChat size={64} className="text-muted mb-3 opacity-25" />
              <h5 className="text-muted">No feedbacks received yet</h5>
              <p className="text-muted small">When passengers rate your service, their comments will appear here.</p>
            </div>
          </div>
        ) : (
          feedbacks.map(fb => (
            <div className="col-lg-6" key={fb.feedbackId}>
              <div className="tripzo-card border-top border-4 border-primary h-100 shadow-sm">
                <div className="d-flex justify-content-between align-items-start mb-3">
                  <div className="d-flex align-items-center gap-2">
                    <div className="bg-light text-primary rounded-circle d-flex align-items-center justify-content-center" style={{ width: 40, height: 40 }}>
                      <MdPerson size={24} />
                    </div>
                    <div className="d-flex flex-column">
                      <h6 className="fw-bold m-0">{fb.passengerName}</h6>
                      <p className="text-muted x-small m-0">{new Date(fb.createdAt).toLocaleDateString()}</p>
                    </div>
                  </div>
                  <div className="d-flex flex-column align-items-end gap-1">
                    <div>{renderStars(fb.rating)}</div>
                    {!fb.operatorResponse && (
                      <span className="badge bg-warning bg-opacity-10 text-warning px-2 py-1" style={{ fontSize: '0.65rem', border: '1px solid rgba(245, 158, 11, 0.2)' }}>
                        PENDING RESPONSE
                      </span>
                    )}
                  </div>
                </div>

                <div className="bg-light rounded-3 p-3 mb-3 border border-1">
                  <p className="m-0 text-dark fst-italic">"{fb.comment}"</p>
                </div>

                <div className="d-flex align-items-center gap-3 mb-3">
                  <div className="d-flex align-items-center gap-1 small text-muted">
                    <MdDirectionsBus size={14} className="text-primary" /> {fb.busName} ({fb.busNumber})
                  </div>
                  <div className="d-flex align-items-center gap-1 small text-muted">
                    <MdChat size={14} className="text-primary" /> {fb.routeName}
                  </div>
                </div>

                {fb.operatorResponse ? (
                  <div className="bg-primary bg-opacity-10 rounded-3 p-3 border border-primary border-opacity-25 mt-auto">
                    <p className="small fw-bold text-primary mb-1 d-flex align-items-center gap-1">
                      <MdReply /> Your Response:
                    </p>
                    <p className="small m-0 text-dark">{fb.operatorResponse}</p>
                  </div>
                ) : (
                  <div className="mt-auto pt-2">
                    {replying?.feedbackId === fb.feedbackId ? (
                      <form onSubmit={handleRespond}>
                        <textarea
                          className="form-control border-primary mb-2 small shadow-sm"
                          rows="3"
                          placeholder="Type your response here..."
                          value={responseText}
                          onChange={e => setResponseText(e.target.value)}
                          required
                        ></textarea>
                        <div className="d-flex gap-2 justify-content-end">
                          <button type="button" className="btn btn-sm btn-light rounded-pill px-3" onClick={() => setReplying(null)}>Cancel</button>
                          <button type="submit" className="btn btn-sm btn-primary rounded-pill px-4" disabled={submittingResponse}>
                            {submittingResponse ? 'Submitting...' : 'Send Response'}
                          </button>
                        </div>
                      </form>
                    ) : (
                      <button className="btn btn-sm btn-outline-primary rounded-pill px-4 w-100" onClick={() => setReplying(fb)}>
                        <MdReply className="me-1" /> Respond
                      </button>
                    )}
                  </div>
                )}
              </div>
            </div>
          ))
        )}
      </div>
    </div>
  );
};

export default OperatorFeedbacks;
