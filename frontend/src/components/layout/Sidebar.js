import React from 'react';
import './Sidebar.css';


const Sidebar = ({ children }) => (
  <div className="sidebar-layout">
    <aside className="sidebar">
      <div>
        <div className="sidebar-logo-section">
          <div className="sidebar-logo-icon">
            <span className="material-symbols-outlined" style={{ color: '#fff', fontSize: '20px' }}>directions_bus</span>
          </div>
          <div>
            <div className="sidebar-logo-text">Tripzo</div>
            <div className="sidebar-operator">OPERATOR</div>
          </div>
        </div>
        <nav className="sidebar-nav sidebar-nav-top">
          <a href="#dashboard" className="active">
            <span className="material-symbols-outlined">dashboard</span>
            <span>Dashboard</span>
          </a>
          <a href="#buses">
            <span className="material-symbols-outlined">directions_bus</span>
            <span>Buses</span>
          </a>
          <a href="#routes">
            <span className="material-symbols-outlined">route</span>
            <span>Routes</span>
          </a>
          <a href="#schedules">
            <span className="material-symbols-outlined">calendar_today</span>
            <span>Schedules</span>
          </a>
          <a href="#refunds">
            <span className="material-symbols-outlined">receipt_long</span>
            <span>Refunds</span>
          </a>
        </nav>
      </div>
      <div className="sidebar-profile">
        <div className="profile-pic"></div>
        <div className="profile-info">
          <div className="profile-name">Alex Johnson</div>
          <div className="profile-role">Fleet Manager</div>
        </div>
        <span className="material-symbols-outlined sidebar-settings-icon">settings</span>
      </div>
    </aside>
    <main className="sidebar-content">{children}</main>
  </div>
);

export default Sidebar;
