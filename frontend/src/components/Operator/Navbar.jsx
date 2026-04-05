import React, { useState } from 'react';
import { MdNotifications, MdAccountCircle, MdLogout } from 'react-icons/md';
import authService from '../../services/auth/authService';
import Logo from '../common/Logo';

const OperatorNavbar = () => {
  const user = authService.getCurrentUser();
  const [showDropdown, setShowDropdown] = useState(false);

  const handleLogout = () => {
    authService.logout();
  };

  return (
    <div className="navbar-top">
      <div className="d-flex align-items-center gap-2">
        <Logo size={28} />
        <span className="fw-bold text-muted" style={{ fontSize: '0.85rem', letterSpacing: '1px' }}>
          OPERATOR PANEL
        </span>
      </div>

      <div className="ms-auto d-flex align-items-center gap-4">
        <div className="position-relative" style={{ cursor: 'pointer' }} title="Notifications">
          <MdNotifications size={24} color="var(--primary-blue)" />
          <span
            className="position-absolute translate-middle badge rounded-pill bg-danger"
            style={{ fontSize: '0.55rem', top: '2px', left: '20px' }}
          >
            0
          </span>
        </div>

        <div className="position-relative">
          <div
            className="d-flex align-items-center gap-2"
            style={{ cursor: 'pointer' }}
            onClick={() => setShowDropdown(!showDropdown)}
          >
            <MdAccountCircle size={32} color="var(--primary-blue)" />
            <div>
              <p className="m-0 fw-semibold" style={{ fontSize: '0.9rem' }}>{user?.fullName || 'Operator'}</p>
              <p className="m-0 text-muted" style={{ fontSize: '0.75rem' }}>{user?.role || 'Operator'}</p>
            </div>
          </div>

          {showDropdown && (
            <>
              <div
                className="position-fixed top-0 start-0 w-100 h-100"
                style={{ zIndex: 999 }}
                onClick={() => setShowDropdown(false)}
              ></div>
              <div
                className="position-absolute end-0 bg-white shadow-lg border-0 rounded-3 py-2"
                style={{ zIndex: 1000, minWidth: '180px', top: '100%', marginTop: '8px' }}
              >
                <div className="px-3 py-2 border-bottom">
                  <p className="m-0 fw-semibold" style={{ fontSize: '0.85rem' }}>{user?.fullName || 'Operator'}</p>
                  <p className="m-0 text-muted" style={{ fontSize: '0.75rem' }}>{user?.email || ''}</p>
                </div>
                <button
                  className="dropdown-item py-2 px-3 text-danger d-flex align-items-center gap-2"
                  onClick={handleLogout}
                  style={{ fontSize: '0.85rem' }}
                >
                  <MdLogout /> Logout
                </button>
              </div>
            </>
          )}
        </div>
      </div>
    </div>
  );
};

export default OperatorNavbar;
