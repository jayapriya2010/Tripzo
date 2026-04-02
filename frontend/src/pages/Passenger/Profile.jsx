import React, { useState } from 'react';
import { MdAccountCircle, MdEdit, MdSave, MdClose } from 'react-icons/md';
import PassengerLayout from '../../layouts/PassengerLayout';
import authService from '../../services/auth/authService';
import passengerService from '../../services/passenger/passengerService';

const Profile = () => {
  const user = authService.getCurrentUser();
  const [editing, setEditing] = useState(false);
  const [loading, setLoading] = useState(false);
  const [toast, setToast] = useState(null);

  const [form, setForm] = useState({
    fullName: user?.fullName || '',
    email: user?.email || '',
    phoneNumber: user?.phoneNumber || '',
    gender: user?.gender || '',
  });

  const showToast = (msg, type) => {
    setToast({ msg, type });
    setTimeout(() => setToast(null), 4000);
  };

  const handleSave = async () => {
    if (!form.fullName || !form.email || !form.phoneNumber) {
      showToast('Please fill in all required fields.', 'danger');
      return;
    }
    setLoading(true);
    try {
      await passengerService.updateProfile(user?.userId, form);
      // Update localStorage
      const updated = { ...user, ...form };
      localStorage.setItem('user', JSON.stringify(updated));
      showToast('Profile updated successfully!', 'success');
      setEditing(false);
    } catch (err) {
      showToast(err.response?.data?.message || 'Failed to update profile.', 'danger');
    } finally {
      setLoading(false);
    }
  };

  const handleCancel = () => {
    setForm({
      fullName: user?.fullName || '',
      email: user?.email || '',
      phoneNumber: user?.phoneNumber || '',
      gender: user?.gender || '',
    });
    setEditing(false);
  };

  const initials = (user?.fullName || 'P').split(' ').map(n => n[0]).join('').slice(0, 2).toUpperCase();

  const fields = [
    { key: 'fullName', label: 'Full Name', type: 'text', required: true },
    { key: 'email', label: 'Email Address', type: 'email', required: true },
    { key: 'phoneNumber', label: 'Phone Number', type: 'tel', required: true },
  ];

  return (
    <PassengerLayout>
      {toast && (
        <div className={`alert alert-${toast.type} position-fixed top-0 end-0 m-3 shadow`}
          style={{ zIndex: 9999, minWidth: 300 }}>
          {toast.msg}
        </div>
      )}

      <h4 className="fw-bold mb-4">My Profile</h4>

      <div className="row g-4">
        {/* Profile Card */}
        <div className="col-md-4">
          <div className="tripzo-card text-center">
            {/* Avatar */}
            <div className="d-inline-flex align-items-center justify-content-center rounded-circle mb-3"
              style={{
                width: 96, height: 96,
                background: 'linear-gradient(135deg, #1E63FF, #0F3D91)',
                color: 'white', fontSize: '2rem', fontWeight: 700,
                boxShadow: '0 8px 24px rgba(30,99,255,0.3)'
              }}>
              {initials}
            </div>
            <h5 className="fw-bold mb-1">{user?.fullName}</h5>
            <p className="text-muted small mb-2">{user?.email}</p>
            <span className="badge rounded-pill px-3 py-2"
              style={{ background: '#E8F0FF', color: '#1E63FF', fontWeight: 600 }}>
              🎫 Passenger
            </span>

            <hr className="my-3" />

            <div className="text-start">
              {[
                { label: 'Phone', value: user?.phoneNumber || '—' },
                { label: 'Gender', value: user?.gender || '—' },
                { label: 'Role', value: user?.role || 'Passenger' },
              ].map(row => (
                <div key={row.label} className="d-flex justify-content-between py-2 border-bottom">
                  <span className="text-muted small">{row.label}</span>
                  <span className="fw-semibold small">{row.value}</span>
                </div>
              ))}
            </div>
          </div>
        </div>

        {/* Edit Form */}
        <div className="col-md-8">
          <div className="tripzo-card">
            <div className="d-flex align-items-center justify-content-between mb-4">
              <h6 className="fw-bold mb-0 d-flex align-items-center gap-2">
                <MdAccountCircle color="var(--primary-blue)" /> Account Details
              </h6>
              {!editing ? (
                <button className="btn btn-outline-primary btn-sm rounded-3" onClick={() => setEditing(true)}>
                  <MdEdit className="me-1" /> Edit Profile
                </button>
              ) : (
                <div className="d-flex gap-2">
                  <button className="btn btn-outline-secondary btn-sm rounded-3" onClick={handleCancel}>
                    <MdClose className="me-1" /> Cancel
                  </button>
                  <button className="btn btn-primary btn-sm rounded-3" onClick={handleSave} disabled={loading}>
                    {loading ? <span className="spinner-border spinner-border-sm" /> : <><MdSave className="me-1" /> Save</>}
                  </button>
                </div>
              )}
            </div>

            <div className="row g-3">
              {fields.map(field => (
                <div className="col-md-6" key={field.key}>
                  <label className="form-label small fw-semibold text-muted">
                    {field.label.toUpperCase()} {field.required && <span className="text-danger">*</span>}
                  </label>
                  {editing ? (
                    <input
                      type={field.type}
                      className="form-control rounded-3"
                      value={form[field.key]}
                      onChange={e => setForm({ ...form, [field.key]: e.target.value })}
                    />
                  ) : (
                    <p className="fw-semibold mb-0 py-2">{user?.[field.key] || '—'}</p>
                  )}
                </div>
              ))}
              <div className="col-md-6">
                <label className="form-label small fw-semibold text-muted">GENDER</label>
                {editing ? (
                  <select className="form-select rounded-3"
                    value={form.gender} onChange={e => setForm({ ...form, gender: e.target.value })}>
                    <option value="">Select Gender</option>
                    <option value="Male">Male</option>
                    <option value="Female">Female</option>
                    <option value="Other">Other</option>
                  </select>
                ) : (
                  <p className="fw-semibold mb-0 py-2">{user?.gender || '—'}</p>
                )}
              </div>
            </div>

            {!editing && (
              <div className="mt-4 p-3 rounded-3" style={{ background: '#E8F0FF' }}>
                <p className="small text-muted mb-0">
                  <strong style={{ color: '#1E63FF' }}>ℹ️ Note:</strong> Your email address is used for login and ticket confirmations.
                  Contact support if you need to change it.
                </p>
              </div>
            )}
          </div>
        </div>
      </div>
    </PassengerLayout>
  );
};

export default Profile;
