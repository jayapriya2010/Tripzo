import React from 'react';
import { BrowserRouter as Router, Routes, Route, Navigate } from 'react-router-dom';
import 'bootstrap/dist/css/bootstrap.min.css';
import 'bootstrap/dist/js/bootstrap.bundle.min.js';
import './styles/theme.css';

// Landing Page
import LandingPage from './pages/LandingPage';

// Auth Pages
import Login from './pages/Auth/Login';
import Signup from './pages/Auth/Signup';
import ForgotPassword from './pages/Auth/ForgotPassword';

// Admin Pages (these self-wrap with AdminLayout)
import Dashboard from './pages/Admin/Dashboard';
import Users from './pages/Admin/Users';
import RoutesPage from './pages/Admin/Routes';
import Bookings from './pages/Admin/Bookings';
import Cancellations from './pages/Admin/Cancellations';
import Amenities from './pages/Admin/Amenities';

// Operator Pages
import OperatorDashboard from './pages/Operator/OperatorDashboard';
import ManageBuses from './pages/Operator/ManageBuses';
import BusConfig from './pages/Operator/BusConfig';
import ManageRoutes from './pages/Operator/ManageRoutes';
import AddRoute from './pages/Operator/AddRoute';
import RouteDetails from './pages/Operator/RouteDetails';
import Schedule from './pages/Operator/Schedule';
import Refunds from './pages/Operator/Refunds';
import OperatorBookings from './pages/Operator/OperatorBookings';
import OperatorFeedbacks from './pages/Operator/OperatorFeedbacks';
import ReassignBus from './pages/Operator/ReassignBus';

// Passenger Pages
import PassengerDashboard from './pages/Passenger/Dashboard';
import SearchResults from './pages/Passenger/SearchResults';
import SeatSelection from './pages/Passenger/SeatSelection';
import BookingReview from './pages/Passenger/BookingReview';
import Payment from './pages/Passenger/Payment';
import BookingSuccess from './pages/Passenger/BookingSuccess';
import MyBookings from './pages/Passenger/MyBookings';
import Profile from './pages/Passenger/Profile';
import Feedback from './pages/Passenger/Feedback';

// Layouts
import OperatorLayout from './layouts/OperatorLayout';

// Auth Guard
const ProtectedRoute = ({ children, role }) => {
  const token = localStorage.getItem('token');
  const user = JSON.parse(localStorage.getItem('user'));

  if (!token) {
    return <Navigate to="/login" replace />;
  }

  const userRole = user?.role || user?.Role;
  if (role && userRole !== role) {
    console.warn(`Access Denied: Required Role ${role}, User Role ${userRole}`);
    return <Navigate to="/" replace />;
  }

  return children;
};

function App() {
  return (
    <Router>
      <Routes>
        {/* Public Routes */}
        <Route path="/" element={<LandingPage />} />
        <Route path="/login" element={<Login />} />
        <Route path="/signup" element={<Signup />} />
        <Route path="/forgot-password" element={<ForgotPassword />} />

        {/* Admin Routes (pages self-wrap with AdminLayout) */}
        <Route path="/admin/dashboard" element={<ProtectedRoute role="Admin"><Dashboard /></ProtectedRoute>} />
        <Route path="/admin/users" element={<ProtectedRoute role="Admin"><Users /></ProtectedRoute>} />
        <Route path="/admin/routes" element={<ProtectedRoute role="Admin"><RoutesPage /></ProtectedRoute>} />
        <Route path="/admin/bookings" element={<ProtectedRoute role="Admin"><Bookings /></ProtectedRoute>} />
        <Route path="/admin/cancellations" element={<ProtectedRoute role="Admin"><Cancellations /></ProtectedRoute>} />
        <Route path="/admin/amenities" element={<ProtectedRoute role="Admin"><Amenities /></ProtectedRoute>} />

        {/* Operator Routes (wrapped in OperatorLayout at router level) */}
        <Route path="/operator/dashboard" element={<ProtectedRoute role="Operator"><OperatorLayout><OperatorDashboard /></OperatorLayout></ProtectedRoute>} />
        <Route path="/operator/buses" element={<ProtectedRoute role="Operator"><OperatorLayout><ManageBuses /></OperatorLayout></ProtectedRoute>} />
        <Route path="/operator/buses/:busId/config" element={<ProtectedRoute role="Operator"><OperatorLayout><BusConfig /></OperatorLayout></ProtectedRoute>} />
        <Route path="/operator/routes" element={<ProtectedRoute role="Operator"><OperatorLayout><ManageRoutes /></OperatorLayout></ProtectedRoute>} />
        <Route path="/operator/routes/add" element={<ProtectedRoute role="Operator"><OperatorLayout><AddRoute /></OperatorLayout></ProtectedRoute>} />
        <Route path="/operator/routes/:routeId" element={<ProtectedRoute role="Operator"><OperatorLayout><RouteDetails /></OperatorLayout></ProtectedRoute>} />
        <Route path="/operator/schedule" element={<ProtectedRoute role="Operator"><OperatorLayout><Schedule /></OperatorLayout></ProtectedRoute>} />
        <Route path="/operator/schedule/reassign" element={<ProtectedRoute role="Operator"><OperatorLayout><ReassignBus /></OperatorLayout></ProtectedRoute>} />
        <Route path="/operator/bookings" element={<ProtectedRoute role="Operator"><OperatorLayout><OperatorBookings /></OperatorLayout></ProtectedRoute>} />
        <Route path="/operator/feedbacks" element={<ProtectedRoute role="Operator"><OperatorLayout><OperatorFeedbacks /></OperatorLayout></ProtectedRoute>} />
        <Route path="/operator/refunds" element={<ProtectedRoute role="Operator"><OperatorLayout><Refunds /></OperatorLayout></ProtectedRoute>} />

        {/* Passenger Routes — all protected with PassengerLayout built-in */}
        <Route path="/passenger/dashboard" element={<ProtectedRoute role="Passenger"><PassengerDashboard /></ProtectedRoute>} />
        <Route path="/passenger/search" element={<ProtectedRoute role="Passenger"><SearchResults /></ProtectedRoute>} />
        <Route path="/passenger/seats" element={<ProtectedRoute role="Passenger"><SeatSelection /></ProtectedRoute>} />
        <Route path="/passenger/review" element={<ProtectedRoute role="Passenger"><BookingReview /></ProtectedRoute>} />
        <Route path="/passenger/payment" element={<ProtectedRoute role="Passenger"><Payment /></ProtectedRoute>} />
        <Route path="/passenger/success" element={<ProtectedRoute role="Passenger"><BookingSuccess /></ProtectedRoute>} />
        <Route path="/passenger/bookings" element={<ProtectedRoute role="Passenger"><MyBookings /></ProtectedRoute>} />
        <Route path="/passenger/profile" element={<ProtectedRoute role="Passenger"><Profile /></ProtectedRoute>} />
        <Route path="/passenger/feedback" element={<ProtectedRoute role="Passenger"><Feedback /></ProtectedRoute>} />

        {/* Catch all */}
        <Route path="*" element={<Navigate to="/login" replace />} />
      </Routes>
    </Router>
  );
}

export default App;
