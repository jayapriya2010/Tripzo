import React, { useState } from 'react';
import { useNavigate, Link } from 'react-router-dom';
import authService from '../../services/auth/authService';
import { MdDirectionsBus, MdEmail, MdLock } from 'react-icons/md';

const Login = () => {
  const [formData, setFormData] = useState({
    email: '',
    password: ''
  });
  const [error, setError] = useState('');
  const [loading, setLoading] = useState(false);
  const navigate = useNavigate();

  const handleChange = (e) => {
    setFormData({ ...formData, [e.target.name]: e.target.value });
  };

  const handleSubmit = async (e) => {
    e.preventDefault();
    setError('');
    setLoading(true);

    try {
      const data = await authService.login(formData);
      if (data.role === 'Admin') {
        navigate('/admin/dashboard');
      } else if (data.role === 'Passenger') {
        navigate('/passenger/dashboard');
      } else if (data.role === 'Operator') {
        navigate('/operator/dashboard');
      } else {
        setError('Unauthorized access.');
      }
    } catch (err) {
      setError(err.response?.data || err.response?.data?.message || 'Invalid email or password');
    } finally {
      setLoading(false);
    }
  };

  return (
    <div className="container-fluid bg-light min-vh-100 d-flex align-items-center justify-content-center">
      <div className="card border-0 shadow-lg" style={{ width: '450px', borderRadius: '20px', overflow: 'hidden' }}>
        <div className="row g-0">
          <div className="col-12 p-5 bg-white">
            <div className="text-center mb-4">
              <MdDirectionsBus size={48} className="text-primary mb-2" />
              <h3 className="fw-bold">Welcome Back</h3>
              <p className="text-muted">Sign in to your account</p>
            </div>

            {error && <div className="alert alert-danger py-2 text-sm">{error}</div>}

            <form onSubmit={handleSubmit}>

              <div className="mb-3">
                <label className="form-label text-muted fw-bold small">EMAIL ADDRESS</label>
                <div className="input-group bg-light rounded-3 p-1">
                  <span className="input-group-text bg-transparent border-0"><MdEmail className="text-primary" /></span>
                  <input
                    type="email"
                    name="email"
                    className="form-control bg-transparent border-0 shadow-none"
                    placeholder="Enter email"
                    value={formData.email}
                    onChange={handleChange}
                    required
                  />
                </div>
              </div>

              <div className="mb-4">
                <label className="form-label text-muted fw-bold small">PASSWORD</label>
                <div className="input-group bg-light rounded-3 p-1">
                  <span className="input-group-text bg-transparent border-0"><MdLock className="text-primary" /></span>
                  <input
                    type="password"
                    name="password"
                    className="form-control bg-transparent border-0 shadow-none"
                    placeholder="Enter password"
                    value={formData.password}
                    onChange={handleChange}
                    required
                  />
                </div>
              </div>

              <button
                type="submit"
                className="btn btn-primary w-100 py-3 rounded-pill fw-bold shadow mt-2"
                disabled={loading}
              >
                {loading ? <span className="spinner-border spinner-border-sm me-2"></span> : 'SIGN IN'}
              </button>
            </form>

            <div className="text-center mt-4">
              <p className="text-muted small">Don't have an account? <Link to="/signup" className="text-primary fw-bold text-decoration-none">Sign up</Link></p>
            </div>
          </div>
        </div>
      </div>
    </div>
  );
};

export default Login;
