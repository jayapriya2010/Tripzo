import React from 'react';
import Sidebar from '../components/Admin/Sidebar';
import Navbar from '../components/Admin/Navbar';

const AdminLayout = ({ children }) => {
  return (
    <div className="d-flex">
      <Sidebar />
      <div className="main-content w-100">
        <Navbar />
        <div className="p-4">
          {children}
        </div>
      </div>
    </div>
  );
};

export default AdminLayout;
