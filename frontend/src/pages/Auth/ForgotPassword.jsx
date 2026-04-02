import React, { useState, useEffect } from 'react';
import { useNavigate, Link } from 'react-router-dom';
import { MdEmail, MdLock, MdVpnKey, MdArrowBack, MdCheckCircle } from 'react-icons/md';
import authService from '../../services/auth/authService';

const ForgotPassword = () => {
  const [step, setStep] = useState(1); // 1: Email, 2: OTP, 3: Reset
  const [loading, setLoading] = useState(false);
  const [error, setError] = useState('');
  const [message, setMessage] = useState('');
  const [formData, setFormData] = useState({
    email: '',
    otp: '',
    newPassword: '',
    confirmPassword: ''
  });

  // Auto-dismiss alerts after 5 seconds
  useEffect(() => {
    if (error || message) {
      const timer = setTimeout(() => {
        setError('');
        setMessage('');
      }, 5000);
      return () => clearTimeout(timer);
    }
  }, [error, message]);

  const navigate = useNavigate();

  const handleChange = (e) => {
    setFormData({ ...formData, [e.target.name]: e.target.value });
  };

  const handleSendOtp = async (e) => {
    e.preventDefault();
    setError('');
    setLoading(true);
    try {
      await authService.forgotPassword(formData.email);
      setStep(2);
      setMessage('Verification code sent to your email.');
    } catch (err) {
      setError(err.response?.data?.message || 'Failed to send verification code.');
    } finally {
      setLoading(false);
    }
  };

  const handleVerifyOtp = async (e) => {
    e.preventDefault();
    if (formData.otp.length !== 6) {
      setError('Please enter a 6-digit code.');
      return;
    }
    setError('');
    setLoading(true);
    try {
      await authService.verifyOtp(formData.email, formData.otp);
      setStep(3);
      setMessage('');
    } catch (err) {
      setError(err.response?.data || err.response?.data?.message || 'Invalid or expired verification code.');
    } finally {
      setLoading(false);
    }
  };

  const handleResetPassword = async (e) => {
    e.preventDefault();
    setError('');
    
    if (formData.newPassword !== formData.confirmPassword) {
      setError('Passwords do not match.');
      return;
    }

    if (formData.newPassword.length < 6) {
      setError('Password must be at least 6 characters.');
      return;
    }

    setLoading(true);
    try {
      await authService.resetPassword({
        email: formData.email,
        otp: formData.otp,
        newPassword: formData.newPassword
      });
      setStep(4); // Success state
    } catch (err) {
      setError(err.response?.data?.message || 'Failed to reset password. The code might have expired.');
    } finally {
      setLoading(false);
    }
  };

  return (
    <div className="container-fluid bg-light min-vh-100 d-flex align-items-center justify-content-center">
      <div className="card border-0 shadow-lg" style={{ width: '450px', borderRadius: '20px', overflow: 'hidden' }}>
        <div className="p-5 bg-white">
          <div className="text-center mb-4">
            <h3 className="fw-bold">Reset Password</h3>
            <p className="text-muted">
              {step === 1 && "Enter your email to receive an OTP"}
              {step === 2 && "Enter the 6-digit code sent to your mail"}
              {step === 3 && "Create a secure new password"}
              {step === 4 && "Password changed successfully"}
            </p>
          </div>

          {error && (
            <div className="alert alert-danger py-2 small alert-dismissible fade show" role="alert">
              {error}
              <button type="button" className="btn-close py-2" onClick={() => setError('')} aria-label="Close"></button>
            </div>
          )}
          {message && step !== 4 && (
            <div className="alert alert-success py-2 small alert-dismissible fade show" role="alert">
              {message}
              <button type="button" className="btn-close py-2" onClick={() => setMessage('')} aria-label="Close"></button>
            </div>
          )}

          {step === 1 && (
            <form onSubmit={handleSendOtp}>
              <div className="mb-4">
                <label className="form-label text-muted fw-bold small">EMAIL ADDRESS</label>
                <div className="input-group bg-light rounded-3 p-1">
                  <span className="input-group-text bg-transparent border-0"><MdEmail className="text-primary" /></span>
                  <input
                    type="email"
                    name="email"
                    className="form-control bg-transparent border-0 shadow-none"
                    placeholder="Enter registered email"
                    value={formData.email}
                    onChange={handleChange}
                    required
                  />
                </div>
              </div>
              <button type="submit" className="btn btn-primary w-100 py-3 rounded-pill fw-bold shadow" disabled={loading}>
                {loading ? <span className="spinner-border spinner-border-sm me-2"></span> : 'SEND CODE'}
              </button>
            </form>
          )}

          {step === 2 && (
            <form onSubmit={handleVerifyOtp}>
              <div className="mb-4">
                <label className="form-label text-muted fw-bold small">VERIFICATION CODE</label>
                <div className="input-group bg-light rounded-3 p-1">
                  <span className="input-group-text bg-transparent border-0"><MdVpnKey className="text-primary" /></span>
                  <input
                    type="text"
                    name="otp"
                    className="form-control bg-transparent border-0 shadow-none"
                    placeholder="6-digit code"
                    maxLength={6}
                    value={formData.otp}
                    onChange={handleChange}
                    required
                  />
                </div>
              </div>
              <button type="submit" className="btn btn-primary w-100 py-3 rounded-pill fw-bold shadow" disabled={loading}>
                {loading ? <span className="spinner-border spinner-border-sm me-2"></span> : 'VERIFY CODE'}
              </button>
              <button type="button" className="btn btn-link w-100 mt-2 text-decoration-none small" onClick={() => setStep(1)}>
                Back to Email
              </button>
            </form>
          )}

          {step === 3 && (
            <form onSubmit={handleResetPassword}>
              <div className="mb-3">
                <label className="form-label text-muted fw-bold small">NEW PASSWORD</label>
                <div className="input-group bg-light rounded-3 p-1">
                  <span className="input-group-text bg-transparent border-0"><MdLock className="text-primary" /></span>
                  <input
                    type="password"
                    name="newPassword"
                    className="form-control bg-transparent border-0 shadow-none"
                    placeholder="Enter new password"
                    value={formData.newPassword}
                    onChange={handleChange}
                    required
                  />
                </div>
              </div>
              <div className="mb-4">
                <label className="form-label text-muted fw-bold small">CONFIRM PASSWORD</label>
                <div className="input-group bg-light rounded-3 p-1">
                  <span className="input-group-text bg-transparent border-0"><MdLock className="text-primary" /></span>
                  <input
                    type="password"
                    name="confirmPassword"
                    className="form-control bg-transparent border-0 shadow-none"
                    placeholder="Re-enter new password"
                    value={formData.confirmPassword}
                    onChange={handleChange}
                    required
                  />
                </div>
              </div>
              <button type="submit" className="btn btn-primary w-100 py-3 rounded-pill fw-bold shadow" disabled={loading}>
                {loading ? <span className="spinner-border spinner-border-sm me-2"></span> : 'RESET PASSWORD'}
              </button>
            </form>
          )}

          {step === 4 && (
            <div className="text-center">
              <MdCheckCircle size={64} className="text-success mb-3" />
              <p>Your password has been reset successfully. You can now log in with your new credentials.</p>
              <Link to="/login" className="btn btn-primary w-100 py-3 rounded-pill fw-bold shadow mt-3 text-decoration-none">
                LOGIN NOW
              </Link>
            </div>
          )}

          {step !== 4 && (
            <div className="text-center mt-4 border-top pt-3">
              <Link to="/login" className="text-muted small text-decoration-none d-flex align-items-center justify-content-center gap-1">
                <MdArrowBack /> Back to Login
              </Link>
            </div>
          )}
        </div>
      </div>
    </div>
  );
};

export default ForgotPassword;
