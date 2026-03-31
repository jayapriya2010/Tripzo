import React from 'react';
import './Dashboard.css';

const RecentAlerts = ({ alerts }) => (
  <div className="dashboard-section dashboard-alerts-section">
    <h4 className="dashboard-alerts-title">Recent Alerts</h4>
    <div className="dashboard-alerts-grid">
      {alerts.map((alert, idx) => (
        <div key={idx} className="dashboard-alert-item">
          <div className={`dashboard-alert-dot dashboard-alert-dot-${alert.color}`}></div>
          <div className="dashboard-alert-content">
            <p className="dashboard-alert-type">{alert.type}</p>
            <p className="dashboard-alert-desc">{alert.desc}</p>
            <p className="dashboard-alert-time">{alert.time}</p>
          </div>
        </div>
      ))}
    </div>
    <button className="dashboard-alerts-viewall">View All Notifications</button>
  </div>
);

export default RecentAlerts;
