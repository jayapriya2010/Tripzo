import React from 'react';
import { NavLink } from 'react-router-dom';
import {
  MdDashboard,
  MdSearch,
  MdConfirmationNumber,
  MdAccountCircle,
  MdFeedback
} from 'react-icons/md';
import '../../styles/theme.css';
import Logo from '../common/Logo';

const PassengerSidebar = () => {
  const menuItems = [
    { name: 'Dashboard', icon: <MdDashboard />, path: '/passenger/dashboard' },
    { name: 'Search Buses', icon: <MdSearch />, path: '/passenger/search' },
    { name: 'My Bookings', icon: <MdConfirmationNumber />, path: '/passenger/bookings' },
    { name: 'Profile', icon: <MdAccountCircle />, path: '/passenger/profile' },
    { name: 'Feedback', icon: <MdFeedback />, path: '/passenger/feedback' },
  ];

  return (
    <div className="sidebar sidebar-operator">
      <div className="text-center mb-4 d-flex align-items-center justify-content-center gap-2">
        <Logo size={40} />
        <h4 className="m-0 fw-bold" style={{ letterSpacing: '1px' }}>Tripzo</h4>
      </div>
      <p className="text-center m-0 mb-2" style={{ fontSize: '0.65rem', letterSpacing: '2px', opacity: 0.5 }}>PASSENGER PANEL</p>
      <hr style={{ backgroundColor: 'rgba(255,255,255,0.1)' }} />
      <nav className="nav flex-column mt-3">
        {menuItems.map((item) => (
          <NavLink
            key={item.name}
            to={item.path}
            className={({ isActive }) =>
              `nav-link ${isActive ? 'active' : ''}`
            }
          >
            {item.icon}
            <span>{item.name}</span>
          </NavLink>
        ))}
      </nav>
    </div>
  );
};

export default PassengerSidebar;

