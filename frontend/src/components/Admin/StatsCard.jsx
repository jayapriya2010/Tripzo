import React from 'react';

const StatsCard = ({ title, value, icon, color, trend }) => {
  return (
    <div className="tripzo-card d-flex align-items-center gap-3">
      <div 
        className="d-flex align-items-center justify-content-center rounded-circle" 
        style={{ width: '56px', height: '56px', backgroundColor: `${color}15`, color: color }}
      >
        {icon}
      </div>
      <div>
        <p className="text-muted m-0" style={{ fontSize: '0.85rem' }}>{title}</p>
        <h3 className="fw-bold m-0">{value}</h3>
        {trend && (
          <p className={`p-0 m-0 ${trend.isUp ? 'text-success' : 'text-danger'}`} style={{ fontSize: '0.75rem' }}>
            {trend.isUp ? '↑' : '↓'} {trend.value}% 
            <span className="text-muted ms-1">vs last month</span>
          </p>
        )}
      </div>
    </div>
  );
};

export default StatsCard;
