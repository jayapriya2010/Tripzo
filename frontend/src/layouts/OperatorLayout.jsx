import React from 'react';
import OperatorSidebar from '../components/Operator/Sidebar';
import OperatorNavbar from '../components/Operator/Navbar';

const OperatorLayout = ({ children }) => {
  return (
    <div className="d-flex">
      <OperatorSidebar />
      <div className="main-content w-100">
        <OperatorNavbar />
        <div className="p-4">
          {children}
        </div>
      </div>
    </div>
  );
};

export default OperatorLayout;
