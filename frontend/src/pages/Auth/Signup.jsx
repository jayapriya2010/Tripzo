import React, { useState } from 'react';
import { useNavigate, Link } from 'react-router-dom';
import authService from '../../services/auth/authService';
import { MdDirectionsBus, MdEmail, MdLock, MdPerson, MdPhone } from 'react-icons/md';

const Signup = () => {
    const [formData, setFormData] = useState({
        fullName: '',
        email: '',
        phoneNumber: '',
        password: '',
        confirmPassword: '',
        role: 'Passenger',
        gender: 'Male'
    });
    const [error, setError] = useState('');
    const [loading, setLoading] = useState(false);
    const navigate = useNavigate();

    const handleChange = (e) => {
        setFormData({ ...formData, [e.target.name]: e.target.value });
    };

    const validatePassword = (password) => {
        const rules = {
            length: password.length >= 8,
            number: /[0-9]/.test(password),
            upper: /[A-Z]/.test(password),
            special: /[!@#$%^&*]/.test(password)
        };
        return rules;
    };

    const passwordRules = validatePassword(formData.password);

    const handleSubmit = async (e) => {
        e.preventDefault();
        setError('');
        
        if (formData.password !== formData.confirmPassword) {
            setError('Passwords do not match');
            return;
        }

        if (!Object.values(passwordRules).every(Boolean)) {
            setError('Password does not meet requirements');
            return;
        }

        setLoading(true);

        try {
            await authService.register(formData);
            navigate('/login');
        } catch (err) {
            setError(err.response?.data?.message || 'Registrations failed');
        } finally {
            setLoading(false);
        }
    };

    const ValidationRule = ({ label, passed }) => (
        <div className={`text-sm mb-1 ${passed ? 'text-success' : 'text-muted'}`} style={{ fontSize: '0.75rem' }}>
            {passed ? '✓' : '○'} {label}
        </div>
    );

    return (
        <div className="container-fluid bg-light min-vh-100 d-flex align-items-center justify-content-center">
            <div className="card border-0 shadow-lg" style={{ width: '500px', borderRadius: '20px', overflow: 'hidden' }}>
                <div className="p-5 bg-white">
                    <div className="text-center mb-4">
                        <MdDirectionsBus size={48} className="text-primary mb-2" />
                        <h3 className="fw-bold">Create Account</h3>
                        <p className="text-muted">Join Tripzo - Tap. Book. Go.</p>
                    </div>

                    {error && <div className="alert alert-danger py-2 text-sm">{error}</div>}

                    <form onSubmit={handleSubmit}>
                        <div className="row">
                            <div className="col-md-12 mb-3">
                                <label className="form-label text-muted fw-bold small">ROLE</label>
                                <select 
                                    className="form-select bg-light border-0 shadow-none p-2" 
                                    name="role" 
                                    value={formData.role} 
                                    onChange={handleChange}
                                >
                                    <option value="Passenger">Passenger</option>
                                    <option value="Operator">Operator</option>
                                </select>
                            </div>
                            <div className="col-md-12 mb-3">
                                <label className="form-label text-muted fw-bold small">FULL NAME</label>
                                <div className="input-group bg-light rounded-3 p-1">
                                    <span className="input-group-text bg-transparent border-0"><MdPerson className="text-primary" /></span>
                                    <input type="text" name="fullName" className="form-control bg-transparent border-0 shadow-none" placeholder="Enter Full Name" value={formData.fullName} onChange={handleChange} required />
                                </div>
                            </div>
                            <div className="col-md-6 mb-3">
                                <label className="form-label text-muted fw-bold small">EMAIL ADDRESS</label>
                                <div className="input-group bg-light rounded-3 p-1">
                                    <span className="input-group-text bg-transparent border-0"><MdEmail className="text-primary" /></span>
                                    <input type="email" name="email" className="form-control bg-transparent border-0 shadow-none" placeholder="Email" value={formData.email} onChange={handleChange} required />
                                </div>
                            </div>
                            <div className="col-md-6 mb-3">
                                <label className="form-label text-muted fw-bold small">PHONE NUMBER</label>
                                <div className="input-group bg-light rounded-3 p-1">
                                    <span className="input-group-text bg-transparent border-0"><MdPhone className="text-primary" /></span>
                                    <input type="text" name="phoneNumber" className="form-control bg-transparent border-0 shadow-none" placeholder="Phone" value={formData.phoneNumber} onChange={handleChange} required />
                                </div>
                            </div>
                            <div className="col-md-12 mb-3">
                                <label className="form-label text-muted fw-bold small">GENDER</label>
                                <select
                                    className="form-select bg-light border-0 shadow-none p-2"
                                    name="gender"
                                    value={formData.gender}
                                    onChange={handleChange}
                                >
                                    <option value="Male">Male</option>
                                    <option value="Female">Female</option>
                                    <option value="Other">Other</option>
                                </select>
                            </div>
                            <div className="col-md-6 mb-3">
                                <label className="form-label text-muted fw-bold small">PASSWORD</label>
                                <div className="input-group bg-light rounded-3 p-1">
                                    <span className="input-group-text bg-transparent border-0"><MdLock className="text-primary" /></span>
                                    <input type="password" name="password" className="form-control bg-transparent border-0 shadow-none" placeholder="Password" value={formData.password} onChange={handleChange} required />
                                </div>
                                <div className="mt-2 p-2 bg-light rounded shadow-sm">
                                    <ValidationRule label="8+ characters" passed={passwordRules.length} />
                                    <ValidationRule label="At least one number" passed={passwordRules.number} />
                                    <ValidationRule label="At least one uppercase" passed={passwordRules.upper} />
                                    <ValidationRule label="Special character" passed={passwordRules.special} />
                                </div>
                            </div>
                            <div className="col-md-6 mb-4">
                                <label className="form-label text-muted fw-bold small">CONFIRM PASSWORD</label>
                                <div className="input-group bg-light rounded-3 p-1">
                                    <span className="input-group-text bg-transparent border-0"><MdLock className="text-primary" /></span>
                                    <input type="password" name="confirmPassword" className="form-control bg-transparent border-0 shadow-none" placeholder="Confirm" value={formData.confirmPassword} onChange={handleChange} required />
                                </div>
                            </div>
                        </div>

                        <button type="submit" className="btn btn-primary w-100 py-3 rounded-pill fw-bold shadow" disabled={loading}>
                            {loading ? <span className="spinner-border spinner-border-sm me-2"></span> : 'SIGN UP'}
                        </button>
                    </form>

                    <div className="text-center mt-4">
                        <p className="text-muted small">Already have an account? <Link to="/login" className="text-primary fw-bold text-decoration-none">Sign in</Link></p>
                    </div>
                </div>
            </div>
        </div>
    );
};

export default Signup;
