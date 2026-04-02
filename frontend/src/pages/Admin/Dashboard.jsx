import React, { useState, useEffect } from 'react';
import { useNavigate } from 'react-router-dom';
import AdminLayout from '../../layouts/AdminLayout';
import StatsCard from '../../components/Admin/StatsCard';
import Table from '../../components/Admin/Table';
import bookingService from '../../services/admin/bookingService';
import userService from '../../services/admin/userService';
import { MdPeople, MdBook, MdAttachMoney, MdRoute } from 'react-icons/md';

const Dashboard = () => {
    const navigate = useNavigate();
    const [stats, setStats] = useState({
        totalPassengers: 0,
        activeOperators: 0,
        totalBuses: 0,
        totalRevenue: 0,
        todaysBookings: 0
    });
    const [recentUsers, setRecentUsers] = useState([]);
    const [loading, setLoading] = useState(true);

    useEffect(() => {
        const fetchDashboardData = async () => {
            try {
                const [statsRes, usersRes] = await Promise.all([
                    bookingService.getDashboardStats(),
                    userService.getUsers({ pageSize: 5 })
                ]);
                setStats(statsRes);
                setRecentUsers(usersRes.items);
            } catch (error) {
                console.error('Error fetching dashboard data:', error);
            } finally {
                setLoading(false);
            }
        };
        fetchDashboardData();
    }, []);

    const userColumns = [
        { label: 'NAME', key: 'fullName' },
        { label: 'EMAIL', key: 'email' },
        { label: 'ROLE', key: 'role' },
        { 
            label: 'STATUS', 
            key: 'isActive', 
            render: (row) => (
                <span className={`badge-status ${row.isActive ? 'badge-active' : 'badge-inactive'}`}>
                    {row.isActive ? 'Active' : 'Deactivated'}
                </span>
            )
        }
    ];

    return (
        <AdminLayout>
            <div className="mb-4">
                <h4 className="fw-bold">Admin Dashboard</h4>
                <p className="text-muted small">Welcome back! Here's what's happening today.</p>
            </div>

            <div className="row g-4 mb-5">
                <div className="col-md-3">
                    <StatsCard 
                        title="Total Users" 
                        value={stats.totalPassengers + stats.activeOperators} 
                        icon={<MdPeople size={28} />} 
                        color="#1E63FF" 
                        trend={{ isUp: true, value: 12 }} 
                    />
                </div>
                <div className="col-md-3">
                    <StatsCard 
                        title="Today's Bookings" 
                        value={stats.todaysBookings} 
                        icon={<MdBook size={28} />} 
                        color="#22C55E" 
                        trend={{ isUp: true, value: 8 }} 
                    />
                </div>
                <div className="col-md-3">
                    <StatsCard 
                        title="Total Revenue" 
                        value={`₹${stats.totalRevenue.toLocaleString()}`} 
                        icon={<MdAttachMoney size={28} />} 
                        color="#F59E0B" 
                        trend={{ isUp: false, value: 3 }} 
                    />
                </div>
                <div className="col-md-3">
                    <StatsCard 
                        title="Active Routes" 
                        value={stats.totalBuses} 
                        icon={<MdRoute size={28} />} 
                        color="#EF4444" 
                        trend={{ isUp: true, value: 5 }} 
                    />
                </div>
            </div>

            <div className="row">
                <div className="col-md-12">
                    <div className="d-flex align-items-center justify-content-between mb-3">
                        <h5 className="fw-bold m-0">Recent Users</h5>
                        <button className="btn btn-sm btn-link text-primary text-decoration-none fw-bold" onClick={() => navigate('/admin/users')}>View All</button>
                    </div>
                    <Table columns={userColumns} data={recentUsers} isLoading={loading} />
                </div>
            </div>
        </AdminLayout>
    );
};

export default Dashboard;
