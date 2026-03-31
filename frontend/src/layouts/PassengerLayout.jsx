import React from 'react';
import PassengerSidebar from '../components/Passenger/Sidebar';
import PassengerNavbar from '../components/Passenger/Navbar';

const PassengerLayout = ({ children }) => {
  return (
    <div className="d-flex">
      <PassengerSidebar />
      <div className="main-content w-100">
        <PassengerNavbar />
        <div className="p-4">
          {children}
        </div>
      </div>
    </div>
  );
};

export default PassengerLayout;
