import React from 'react';
import { NavLink } from 'react-router-dom';
import {
  MdDashboard,
  MdDirectionsBus,
  MdRoute,
  MdSchedule,
  MdMoneyOff,
  MdDirectionsBusFilled,
  MdBook,
  MdFeedback
} from 'react-icons/md';
import '../../styles/theme.css';

const OperatorSidebar = () => {
  const menuItems = [
    { name: 'Dashboard', icon: <MdDashboard />, path: '/operator/dashboard' },
    { name: 'Manage Buses', icon: <MdDirectionsBus />, path: '/operator/buses' },
    { name: 'Manage Routes', icon: <MdRoute />, path: '/operator/routes' },
    { name: 'Schedule', icon: <MdSchedule />, path: '/operator/schedule' },
    { name: 'Bookings', icon: <MdBook />, path: '/operator/bookings' },
    { name: 'Refunds', icon: <MdMoneyOff />, path: '/operator/refunds' },
  ];

  return (
    <div className="sidebar sidebar-operator">
      <div className="text-center mb-4 d-flex align-items-center justify-content-center gap-2">
        <MdDirectionsBusFilled size={32} color="var(--primary-blue)" />
        <h4 className="m-0 fw-bold" style={{ letterSpacing: '1px' }}>Tripzo</h4>
      </div>
      <p className="text-center m-0 mb-2" style={{ fontSize: '0.65rem', letterSpacing: '2px', opacity: 0.5 }}>OPERATOR PANEL</p>
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

export default OperatorSidebar;
