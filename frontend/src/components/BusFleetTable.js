import React from 'react';
import './Dashboard.css';

const BusFleetTable = ({ buses }) => (
  <div className="dashboard-section">
    <div className="dashboard-section-header">
      <h4 className="dashboard-section-title">Bus Fleet Management</h4>
      <div className="dashboard-section-actions">
        <button className="dashboard-icon-btn">
          <span className="material-symbols-outlined">filter_list</span>
        </button>
        <button className="dashboard-icon-btn">
          <span className="material-symbols-outlined">download</span>
        </button>
      </div>
    </div>
    <div className="dashboard-table-wrapper">
      <table className="dashboard-table">
        <thead>
          <tr>
            <th>BUS NUMBER</th>
            <th>TYPE</th>
            <th>SEATS</th>
            <th>STATUS</th>
            <th>ROUTE</th>
            <th className="text-right">ACTIONS</th>
          </tr>
        </thead>
        <tbody>
          {buses.map((bus, idx) => (
            <tr key={idx}>
              <td className="dashboard-link">{bus.number}</td>
              <td>{bus.type}</td>
              <td>{bus.seats}</td>
              <td>
                <span className={`dashboard-status dashboard-status-${bus.statusColor}`}>
                  <span className={`dashboard-status-dot dashboard-status-dot-${bus.statusColor}`}></span>
                  {bus.status}
                </span>
              </td>
              <td className="dashboard-route-text">{bus.route}</td>
              <td className="text-right">
                <button className="dashboard-actions-btn">
                  <span className="material-symbols-outlined">more_vert</span>
                </button>
              </td>
            </tr>
          ))}
        </tbody>
      </table>
    </div>
    <div className="dashboard-table-footer">
      <span className="dashboard-table-info">Showing 1-5 of 45 buses</span>
      <div className="dashboard-pagination">
        <button className="dashboard-pagination-btn">Previous</button>
        <button className="dashboard-pagination-btn">Next</button>
      </div>
    </div>
  </div>
);

export default BusFleetTable;
