import React from 'react';
import './Dashboard.css';

const DashboardSummary = ({ summary }) => (
  <div className="dashboard-summary-row">
    {summary.map((item, idx) => (
      <div className="dashboard-summary-card" key={idx}>
        <div className="dashboard-summary-top">
          <span className="material-symbols-outlined dashboard-summary-icon">{item.icon}</span>
          <span className={`dashboard-summary-change dashboard-summary-change-${item.changeType}`}>
            {item.change}
          </span>
        </div>
        <div className="dashboard-summary-label">{item.label}</div>
        <div className="dashboard-summary-value">{item.value}</div>
      </div>
    ))}
  </div>
);

export default DashboardSummary;
