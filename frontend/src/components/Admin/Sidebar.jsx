import React from 'react';
import { NavLink } from 'react-router-dom';
import { 
  MdDashboard, 
  MdPeople, 
  MdRoute, 
  MdBook, 
  MdCancel,
  MdWifi
} from 'react-icons/md';
import '../../styles/theme.css';
import Logo from '../common/Logo';

const Sidebar = () => {
  const menuItems = [
    { name: 'Dashboard', icon: <MdDashboard />, path: '/admin/dashboard' },
    { name: 'Manage Users', icon: <MdPeople />, path: '/admin/users' },
    { name: 'Manage Routes', icon: <MdRoute />, path: '/admin/routes' },
    { name: 'Bookings', icon: <MdBook />, path: '/admin/bookings' },
    { name: 'Cancellations', icon: <MdCancel />, path: '/admin/cancellations' },
    { name: 'Amenities', icon: <MdWifi />, path: '/admin/amenities' },
  ];

  return (
    <div className="sidebar">
      <div className="text-center mb-4 d-flex align-items-center justify-content-center gap-2">
        <Logo size={40} />
        <h4 className="m-0 fw-bold" style={{ letterSpacing: '1px' }}>Tripzo</h4>
      </div>
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

export default Sidebar;

