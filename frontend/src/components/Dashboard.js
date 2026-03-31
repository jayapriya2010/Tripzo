import React from 'react';

import DashboardSummary from './DashboardSummary';
import BusFleetTable from './BusFleetTable';
import RecentAlerts from './RecentAlerts';
import './Dashboard.css';

const summary = [
  { label: 'Total Buses', value: 45, change: '+2%', changeType: 'positive', icon: 'directions_bus' },
  { label: 'Active Routes', value: 12, change: 'Steady', changeType: 'neutral', icon: 'route' },
  { label: 'Monthly Bookings', value: '1,284', change: '-5%', changeType: 'negative', icon: 'confirmation_number' },
];

const buses = [
  { number: 'TR-4592', type: 'Luxury Sleeper', seats: 32, status: 'On Route', statusColor: 'green', route: 'NYC - Washington' },
  { number: 'TR-8812', type: 'AC Seater', seats: 45, status: 'In Yard', statusColor: 'blue', route: '-' },
  { number: 'TR-2104', type: 'Semi-Sleeper', seats: 36, status: 'Maintenance', statusColor: 'yellow', route: '-' },
  { number: 'TR-5523', type: 'Luxury Sleeper', seats: 32, status: 'On Route', statusColor: 'green', route: 'Chicago - Detroit' },
  { number: 'TR-9901', type: 'Electric AC', seats: 40, status: 'On Route', statusColor: 'green', route: 'Seattle - Portland' },
];

const alerts = [
  { type: 'Pending ticket cancellation', desc: 'Ticket #TK-9921 requires immediate approval.', time: 'JUST NOW', color: 'gold' },
  { type: 'Refund initiation in progress', desc: 'Processing refund for 12 cancelled bookings on NYC route.', time: '15 MINUTES AGO', color: 'blue' },
  { type: 'New bus assigned to TR-8812', desc: 'Replacement vehicle allocated for evening shift.', time: '1 HOUR AGO', color: 'blue' },
];


const Dashboard = () => (
  <div className="dashboard-wrapper">
    {/* Sticky Header */}
    <header className="dashboard-header">
      <div className="dashboard-header-left">
        <h2 className="dashboard-title">Dashboard Overview</h2>
        <p className="dashboard-subtitle">Monitor and manage your fleet operations</p>
      </div>
      <div className="dashboard-header-right">
        <div className="dashboard-search-wrapper">
          <span className="material-symbols-outlined dashboard-search-icon">search</span>
          <input
            className="dashboard-search-input"
            type="text"
            placeholder="Search buses, routes..."
          />
        </div>
        <button className="dashboard-notification-btn">
          <span className="material-symbols-outlined">notifications</span>
          <span className="dashboard-notification-dot"></span>
        </button>
        <button className="dashboard-add-bus-btn">
          <span className="material-symbols-outlined" style={{ fontSize: '18px' }}>add</span>
          <span>Add Bus</span>
        </button>
      </div>
    </header>

    {/* Content */}
    <div className="dashboard-content">
      <DashboardSummary summary={summary} />
      <BusFleetTable buses={buses} />
      <RecentAlerts alerts={alerts} />
    </div>
  </div>
);

export default Dashboard;
