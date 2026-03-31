import React, { useState, useEffect } from 'react';
import AdminLayout from '../../layouts/AdminLayout';
import userService from '../../services/admin/userService';
import { MdSearch } from 'react-icons/md';

const Users = () => {
    const [users, setUsers] = useState([]);
    const [loading, setLoading] = useState(true);
    const [searchTerm, setSearchTerm] = useState('');
    const [roleFilter, setRoleFilter] = useState('');
    const [togglingId, setTogglingId] = useState(null);

    const fetchUsers = async () => {
        setLoading(true);
        try {
            const data = await userService.getUsers({
                searchTerm,
                role: roleFilter,
                pageNumber: 1,
                pageSize: 50
            });
            setUsers(data.items || []);
        } catch (error) {
            console.error('Error fetching users:', error);
        } finally {
            setLoading(false);
        }
    };

    useEffect(() => {
        const delayDebounceFn = setTimeout(() => {
            fetchUsers();
        }, 500);
        return () => clearTimeout(delayDebounceFn);
        // eslint-disable-next-line react-hooks/exhaustive-deps
    }, [searchTerm, roleFilter]);

    const handleToggleStatus = async (user) => {
        setTogglingId(user.userId);
        try {
            if (user.isActive) {
                await userService.deactivateUser(user.userId);
            } else {
                await userService.activateUser(user.userId);
            }
            await fetchUsers();
        } catch (error) {
            alert(error.response?.data?.message || 'Error updating user status');
        } finally {
            setTogglingId(null);
        }
    };

    return (
        <AdminLayout>
            <div className="d-flex align-items-center justify-content-between mb-4">
                <div>
                    <h4 className="fw-bold m-0">Manage Users</h4>
                    <p className="text-muted small">View and manage all system users.</p>
                </div>
            </div>

            {/* Filters */}
            <div className="tripzo-card mb-4 bg-white border-0 shadow-sm">
                <div className="row g-3">
                    <div className="col-md-6">
                        <div className="input-group">
                            <span className="input-group-text bg-light border-0"><MdSearch /></span>
                            <input
                                type="text"
                                className="form-control bg-light border-0 shadow-none"
                                placeholder="Search by name or email..."
                                value={searchTerm}
                                onChange={(e) => setSearchTerm(e.target.value)}
                            />
                        </div>
                    </div>
                    <div className="col-md-3">
                        <select
                            className="form-select bg-light border-0 shadow-none"
                            value={roleFilter}
                            onChange={(e) => setRoleFilter(e.target.value)}
                        >
                            <option value="">All Roles</option>
                            <option value="Passenger">Passenger</option>
                            <option value="Operator">Operator</option>
                        </select>
                    </div>
                </div>
            </div>

            {/* Table */}
            {loading ? (
                <div className="text-center py-5">
                    <div className="spinner-border text-primary" role="status">
                        <span className="visually-hidden">Loading...</span>
                    </div>
                </div>
            ) : users.length === 0 ? (
                <div className="text-center py-5 text-muted">No users found.</div>
            ) : (
                <div className="table-responsive table-container">
                    <table className="table table-hover mb-0">
                        <thead>
                            <tr>
                                <th>NAME</th>
                                <th>EMAIL</th>
                                <th>PHONE</th>
                                <th>ROLE</th>
                                <th>STATUS</th>
                                <th>ACTIONS</th>
                            </tr>
                        </thead>
                        <tbody>
                            {users.map((user) => (
                                <tr key={user.userId}>
                                    <td>{user.fullName}</td>
                                    <td>{user.email}</td>
                                    <td>{user.phoneNumber}</td>
                                    <td>{user.role}</td>
                                    <td>
                                        <span className={`badge-status ${user.isActive ? 'badge-active' : 'badge-inactive'}`}>
                                            {user.isActive ? 'Active' : 'Inactive'}
                                        </span>
                                    </td>
                                    <td>
                                        <div className="form-check form-switch">
                                            <input
                                                className="form-check-input"
                                                type="checkbox"
                                                role="switch"
                                                id={`toggle-${user.userId}`}
                                                checked={user.isActive}
                                                onChange={() => handleToggleStatus(user)}
                                                disabled={togglingId === user.userId}
                                                style={{ cursor: 'pointer', width: '3rem', height: '1.5rem' }}
                                            />
                                            <label
                                                className="form-check-label small text-muted ms-2"
                                                htmlFor={`toggle-${user.userId}`}
                                                style={{ cursor: 'pointer', lineHeight: '1.5rem' }}
                                            >
                                                {togglingId === user.userId ? 'Updating...' : (user.isActive ? 'Deactivate' : 'Activate')}
                                            </label>
                                        </div>
                                    </td>
                                </tr>
                            ))}
                        </tbody>
                    </table>
                </div>
            )}
        </AdminLayout>
    );
};

export default Users;
