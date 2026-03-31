import React, { useState, useEffect, useMemo } from 'react';
import AdminLayout from '../../layouts/AdminLayout';
import StatsCard from '../../components/Admin/StatsCard';
import bookingService from '../../services/admin/bookingService';
import { MdBook, MdSearch, MdFilterList, MdVisibility, MdCheckCircle, MdChevronLeft, MdChevronRight } from 'react-icons/md';

const PAGE_SIZE_OPTIONS = [10, 25, 50];

const Bookings = () => {
    const [allBookings, setAllBookings] = useState([]);
    const [loading, setLoading] = useState(true);
    const [searchTerm, setSearchTerm] = useState('');
    const [statusFilter, setStatusFilter] = useState('');
    const [currentPage, setCurrentPage] = useState(1);
    const [pageSize, setPageSize] = useState(10);
    const [selectedBooking, setSelectedBooking] = useState(null);
    const [actionLoading, setActionLoading] = useState(false);

    const fetchBookings = async () => {
        setLoading(true);
        try {
            const data = await bookingService.getAllBookings();
            setAllBookings(data || []);
        } catch (error) {
            console.error('Error fetching bookings:', error);
        } finally {
            setLoading(false);
        }
    };

    useEffect(() => {
        fetchBookings();
    }, []);

    // Client-side filtering
    const filteredBookings = useMemo(() => {
        let result = allBookings;

        if (searchTerm.trim()) {
            const term = searchTerm.toLowerCase();
            result = result.filter(b =>
                String(b.bookingId).includes(term) ||
                (b.passengerName && b.passengerName.toLowerCase().includes(term))
            );
        }

        if (statusFilter) {
            result = result.filter(b => b.status === statusFilter);
        }

        return result;
    }, [allBookings, searchTerm, statusFilter]);

    // Reset to page 1 when filters change
    useEffect(() => {
        setCurrentPage(1);
    }, [searchTerm, statusFilter, pageSize]);

    // Client-side pagination
    const totalPages = Math.ceil(filteredBookings.length / pageSize);
    const paginatedBookings = useMemo(() => {
        const start = (currentPage - 1) * pageSize;
        return filteredBookings.slice(start, start + pageSize);
    }, [filteredBookings, currentPage, pageSize]);

    // Stats from ALL bookings (not filtered)
    const confirmedCount = allBookings.filter(b => b.status === 'Confirmed').length;
    const cancelledCount = allBookings.filter(b => b.status === 'Cancelled').length;
    const approvedCount = allBookings.filter(b => b.status === 'CancellationApproved').length;

    const handleApproveCancellation = async (booking) => {
        if (!window.confirm(`Approve cancellation for this booking?`)) return;
        setActionLoading(true);
        try {
            await bookingService.approveCancellation(booking.bookingId);
            await fetchBookings();
        } catch (error) {
            alert(error.response?.data?.message || 'Error approving cancellation');
        } finally {
            setActionLoading(false);
        }
    };

    const getStatusBadge = (status) => {
        const map = {
            'Confirmed': 'badge-success',
            'Cancelled': 'badge-inactive',
            'CancellationApproved': 'badge-pending',
        };
        return map[status] || 'badge-pending';
    };

    return (
        <AdminLayout>
            <div className="d-flex align-items-center justify-content-between mb-4">
                <div>
                    <h4 className="fw-bold m-0">System Bookings</h4>
                    <p className="text-muted small">Global audit of all bus ticket bookings.</p>
                </div>
            </div>

            {/* Stats Cards */}
            <div className="row g-4 mb-4">
                <div className="col-md-4">
                    <StatsCard title="Confirmed" value={confirmedCount} icon={<MdBook size={28} />} color="#22C55E" />
                </div>
                <div className="col-md-4">
                    <StatsCard title="Cancelled" value={cancelledCount} icon={<MdBook size={28} />} color="#EF4444" />
                </div>
                <div className="col-md-4">
                    <StatsCard title="Cancellation Approved" value={approvedCount} icon={<MdBook size={28} />} color="#F59E0B" />
                </div>
            </div>

            {/* Filters */}
            <div className="tripzo-card mb-4 bg-white border-0 shadow-sm">
                <div className="row g-3 align-items-center">
                    <div className="col-md-5">
                        <div className="input-group">
                            <span className="input-group-text bg-light border-0"><MdSearch /></span>
                            <input
                                type="text"
                                className="form-control bg-light border-0 shadow-none"
                                placeholder="Search by Passenger Name..."
                                value={searchTerm}
                                onChange={(e) => setSearchTerm(e.target.value)}
                            />
                        </div>
                    </div>
                    <div className="col-md-3">
                        <div className="d-flex align-items-center gap-2">
                            <MdFilterList />
                            <select
                                className="form-select bg-light border-0 shadow-none"
                                value={statusFilter}
                                onChange={(e) => setStatusFilter(e.target.value)}
                            >
                                <option value="">All Status</option>
                                <option value="Confirmed">Confirmed</option>
                                <option value="Cancelled">Cancelled</option>
                                <option value="CancellationApproved">Cancellation Approved</option>
                            </select>
                        </div>
                    </div>
                    <div className="col-md-2">
                        <select
                            className="form-select bg-light border-0 shadow-none"
                            value={pageSize}
                            onChange={(e) => setPageSize(Number(e.target.value))}
                        >
                            {PAGE_SIZE_OPTIONS.map(s => (
                                <option key={s} value={s}>{s} per page</option>
                            ))}
                        </select>
                    </div>
                    <div className="col-md-2 text-end">
                        <span className="text-muted small">{filteredBookings.length} results</span>
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
            ) : paginatedBookings.length === 0 ? (
                <div className="text-center py-5 text-muted">No bookings found.</div>
            ) : (
                <div className="table-responsive table-container">
                    <table className="table table-hover mb-0">
                        <thead>
                            <tr>
                                <th>PASSENGER</th>
                                <th>ROUTE</th>
                                <th>JOURNEY DATE</th>
                                <th>AMOUNT</th>
                                <th>STATUS</th>
                                <th>ACTIONS</th>
                            </tr>
                        </thead>
                        <tbody>
                            {paginatedBookings.map((booking) => (
                                <tr key={booking.bookingId}>
                                    <td>{booking.passengerName}</td>
                                    <td>{booking.routeName}</td>
                                    <td>{new Date(booking.journeyDate).toLocaleDateString()}</td>
                                    <td>₹{booking.totalAmount}</td>
                                    <td>
                                        <span className={`badge-status ${getStatusBadge(booking.status)}`}>
                                            {booking.status}
                                        </span>
                                    </td>
                                    <td>
                                        <div className="d-flex gap-2">
                                            <button
                                                className="btn btn-sm btn-outline-primary"
                                                title="View Details"
                                                onClick={() => setSelectedBooking(booking)}
                                            >
                                                <MdVisibility />
                                            </button>
                                            {booking.status === 'Cancelled' && (
                                                <button
                                                    className="btn btn-sm btn-outline-success"
                                                    title="Approve Cancellation"
                                                    onClick={() => handleApproveCancellation(booking)}
                                                    disabled={actionLoading}
                                                >
                                                    <MdCheckCircle />
                                                </button>
                                            )}
                                        </div>
                                    </td>
                                </tr>
                            ))}
                        </tbody>
                    </table>
                </div>
            )}

            {/* Pagination */}
            {totalPages > 1 && (
                <div className="d-flex justify-content-between align-items-center mt-3">
                    <span className="text-muted small">
                        Showing {(currentPage - 1) * pageSize + 1}–{Math.min(currentPage * pageSize, filteredBookings.length)} of {filteredBookings.length}
                    </span>
                    <div className="d-flex gap-2">
                        <button
                            className="btn btn-sm btn-outline-primary"
                            disabled={currentPage === 1}
                            onClick={() => setCurrentPage(p => p - 1)}
                        >
                            <MdChevronLeft /> Prev
                        </button>
                        {Array.from({ length: Math.min(totalPages, 5) }, (_, i) => {
                            let page;
                            if (totalPages <= 5) {
                                page = i + 1;
                            } else if (currentPage <= 3) {
                                page = i + 1;
                            } else if (currentPage >= totalPages - 2) {
                                page = totalPages - 4 + i;
                            } else {
                                page = currentPage - 2 + i;
                            }
                            return (
                                <button
                                    key={page}
                                    className={`btn btn-sm ${currentPage === page ? 'btn-primary' : 'btn-outline-primary'}`}
                                    onClick={() => setCurrentPage(page)}
                                >
                                    {page}
                                </button>
                            );
                        })}
                        <button
                            className="btn btn-sm btn-outline-primary"
                            disabled={currentPage === totalPages}
                            onClick={() => setCurrentPage(p => p + 1)}
                        >
                            Next <MdChevronRight />
                        </button>
                    </div>
                </div>
            )}

            {/* View Details Modal */}
            {selectedBooking && (
                <div className="modal d-block" style={{ backgroundColor: 'rgba(0,0,0,0.5)' }} onClick={() => setSelectedBooking(null)}>
                    <div className="modal-dialog modal-dialog-centered" onClick={(e) => e.stopPropagation()}>
                        <div className="modal-content border-0 shadow-lg" style={{ borderRadius: '16px' }}>
                            <div className="modal-header border-0 pb-0">
                                <h5 className="modal-title fw-bold">Booking Details</h5>
                                <button className="btn-close" onClick={() => setSelectedBooking(null)}></button>
                            </div>
                            <div className="modal-body">
                                <div className="row g-3">
                                    <div className="col-6">
                                        <p className="text-muted small mb-1">Passenger</p>
                                        <p className="fw-bold">{selectedBooking.passengerName}</p>
                                    </div>
                                    <div className="col-6">
                                        <p className="text-muted small mb-1">Route</p>
                                        <p className="fw-bold">{selectedBooking.routeName}</p>
                                    </div>
                                    <div className="col-6">
                                        <p className="text-muted small mb-1">Journey Date</p>
                                        <p className="fw-bold">{new Date(selectedBooking.journeyDate).toLocaleDateString()}</p>
                                    </div>
                                    <div className="col-6">
                                        <p className="text-muted small mb-1">Amount</p>
                                        <p className="fw-bold">₹{selectedBooking.totalAmount}</p>
                                    </div>
                                    <div className="col-12">
                                        <p className="text-muted small mb-1">Status</p>
                                        <span className={`badge-status ${getStatusBadge(selectedBooking.status)}`}>
                                            {selectedBooking.status}
                                        </span>
                                    </div>
                                </div>
                            </div>
                        </div>
                    </div>
                </div>
            )}
        </AdminLayout>
    );
};

export default Bookings;
