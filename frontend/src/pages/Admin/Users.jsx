import React, { useState, useEffect } from 'react';
import AdminLayout from '../../layouts/AdminLayout';
import userService from '../../services/admin/userService';
import { MdSearch, MdChevronLeft, MdChevronRight } from 'react-icons/md';

const PAGE_SIZE_OPTIONS = [10, 25, 50];

const Users = () => {
    const [users, setUsers] = useState([]);
    const [loading, setLoading] = useState(true);
    const [searchTerm, setSearchTerm] = useState('');
    const [roleFilter, setRoleFilter] = useState('');
    const [togglingId, setTogglingId] = useState(null);
    const [currentPage, setCurrentPage] = useState(1);
    const [pageSize, setPageSize] = useState(10);
    const [totalPages, setTotalPages] = useState(1);
    const [totalCount, setTotalCount] = useState(0);

    const fetchUsers = async () => {
        setLoading(true);
        try {
            const data = await userService.getUsers({
                searchTerm,
                role: roleFilter,
                pageNumber: currentPage,
                pageSize: pageSize
            });
            setUsers(data.items || []);
            setTotalPages(data.totalPages || 1);
            setTotalCount(data.totalCount || 0);
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
    }, [searchTerm, roleFilter, currentPage, pageSize]);

    // Reset to page 1 when filters change
    useEffect(() => {
        setCurrentPage(1);
    }, [searchTerm, roleFilter, pageSize]);

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
                    <div className="col-md-3">
                        <div className="d-flex align-items-center gap-2">
                            <span className="small text-muted fw-bold text-uppercase" style={{ fontSize: '0.65rem', whiteSpace: 'nowrap' }}>Show:</span>
                            <select 
                                className="form-select bg-light border-0 shadow-none small" 
                                value={pageSize}
                                onChange={(e) => setPageSize(Number(e.target.value))}
                            >
                                {PAGE_SIZE_OPTIONS.map(opt => (
                                    <option key={opt} value={opt}>{opt} per page</option>
                                ))}
                            </select>
                        </div>
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
                <>
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

                    {/* Pagination */}
                    {totalPages > 1 && (
                        <div className="d-flex justify-content-between align-items-center mt-3">
                            <span className="text-muted small">
                                Showing {(currentPage - 1) * pageSize + 1}–{Math.min(currentPage * pageSize, totalCount)} of {totalCount}
                            </span>
                            <div className="d-flex gap-2">
                                <button
                                    className="btn btn-sm btn-outline-primary rounded-3"
                                    disabled={currentPage === 1}
                                    onClick={() => setCurrentPage(p => p - 1)}
                                >
                                    <MdChevronLeft /> Prev
                                </button>
                                {Array.from({ length: Math.min(totalPages, 5) }, (_, i) => {
                                    let page;
                                    if (totalPages <= 5) page = i + 1;
                                    else if (currentPage <= 3) page = i + 1;
                                    else if (currentPage >= totalPages - 2) page = totalPages - 4 + i;
                                    else page = currentPage - 2 + i;
                                    
                                    return (
                                        <button
                                            key={page}
                                            className={`btn btn-sm rounded-3 ${currentPage === page ? 'btn-primary shadow-sm' : 'btn-outline-primary border-0'}`}
                                            onClick={() => setCurrentPage(page)}
                                        >
                                            {page}
                                        </button>
                                    );
                                })}
                                <button
                                    className="btn btn-sm btn-outline-primary rounded-3"
                                    disabled={currentPage === totalPages}
                                    onClick={() => setCurrentPage(p => p + 1)}
                                >
                                    Next <MdChevronRight />
                                </button>
                            </div>
                        </div>
                    )}
                </>
            )}
        </AdminLayout>
    );
};

export default Users;
