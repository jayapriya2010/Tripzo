import React, { useState, useEffect } from 'react';
import AdminLayout from '../../layouts/AdminLayout';
import Table from '../../components/Admin/Table';
import cancellationService from '../../services/admin/cancellationService';
import StatsCard from '../../components/Admin/StatsCard';
import { MdCancel, MdCheck, MdClose, MdAttachMoney, MdAccessTime } from 'react-icons/md';

const Cancellations = () => {
    const [requests, setRequests] = useState([]);
    const [loading, setLoading] = useState(true);
    const [showSeatModal, setShowSeatModal] = useState(false);
    const [selectedBooking, setSelectedBooking] = useState(null);
    const [selectedSeatIds, setSelectedSeatIds] = useState([]);
    const [actionType, setActionType] = useState(''); // 'approve' or 'reject'

    const fetchRequests = async () => {
        setLoading(true);
        try {
            const data = await cancellationService.getPendingCancellations();
            setRequests(data || []);
        } catch (error) {
            console.error('Error fetching cancellations:', error);
        } finally {
            setLoading(false);
        }
    };

    useEffect(() => {
        fetchRequests();
    }, []);

    const openSeatModal = (booking, type) => {
        setSelectedBooking(booking);
        // Default select only seats that are currently 'CancellationPending'
        const pendingSeats = booking.bookedSeats
            ?.filter(s => s.status === 'CancellationPending')
            .map(s => s.bookedSeatId) || [];
        setSelectedSeatIds(pendingSeats);
        setActionType(type);
        setShowSeatModal(true);
    };

    const handleConfirmAction = async () => {
        if (selectedSeatIds.length === 0) {
            alert('Please select at least one seat.');
            return;
        }

        const confirmMsg = actionType === 'approve' 
            ? `Approve refund for ${selectedSeatIds.length} seat(s)?` 
            : `Reject cancellation for ${selectedSeatIds.length} seat(s)?`;

        if (window.confirm(confirmMsg)) {
            try {
                if (actionType === 'approve') {
                    await cancellationService.approveCancellation(selectedBooking.bookingId, selectedSeatIds);
                } else {
                    await cancellationService.rejectCancellation(selectedBooking.bookingId, selectedSeatIds);
                }
                setShowSeatModal(false);
                fetchRequests();
            } catch (error) {
                alert(`Error ${actionType === 'approve' ? 'approving' : 'rejecting'} cancellation`);
            }
        }
    };

    const toggleSeat = (seatId) => {
        setSelectedSeatIds(prev => 
            prev.includes(seatId) ? prev.filter(id => id !== seatId) : [...prev, seatId]
        );
    };

    const columns = [
        { label: 'PASSENGER', key: 'passengerName' },
        { label: 'BUS', key: 'busNumber' },
        { 
            label: 'SEATS', 
            key: 'bookedSeats',
            render: (row) => row.bookedSeats
                ?.filter(s => s.status === 'CancellationPending')
                .map(s => s.seatNumber)
                .join(', ') || '—'
        },
        { 
            label: 'JOURNEY', 
            key: 'journeyDate',
            render: (row) => new Date(row.journeyDate).toLocaleDateString()
        },
        { 
            label: 'TOTAL REFUND', 
            key: 'totalAmount',
            render: (row) => {
                const pendingCount = row.bookedSeats?.filter(s => s.status === 'CancellationPending').length || 1;
                const totalSeats = row.bookedSeats?.length || 1;
                const estimate = (row.totalAmount / totalSeats) * pendingCount;
                return `₹${estimate.toFixed(2)}`;
            }
        },
        {
            label: 'REASON',
            key: 'cancellationReason',
            render: (row) => row.cancellationReason || '—'
        },
        { 
            label: 'STATUS',
            key: 'status',
            render: (row) => (
                <span className={`badge-status ${row.status === 'PartiallyCancelled' ? 'badge-pending' : 'badge-inactive'}`}>
                    {row.status}
                </span>
            )
        }
    ];

    const actions = [
        { 
            label: 'Approve', 
            icon: <MdCheck />, 
            className: 'btn-outline-success',
            onClick: (row) => openSeatModal(row, 'approve') 
        },
        { 
            label: 'Reject', 
            icon: <MdClose />, 
            className: 'btn-outline-danger',
            onClick: (row) => openSeatModal(row, 'reject') 
        }
    ];

    return (
        <AdminLayout>
            <div className="d-flex align-items-center justify-content-between mb-4">
                <div>
                    <h4 className="fw-bold m-0 text-dark">Cancellation Review</h4>
                    <p className="text-muted small">Selectively approve or reject specific seat cancellation requests.</p>
                </div>
            </div>

            <Table columns={columns} data={requests} actions={actions} isLoading={loading} />

            {/* Seat Selection Modal */}
            {showSeatModal && (
                <div className="modal fade show d-block" style={{ backgroundColor: 'rgba(0,0,0,0.5)', zIndex: 1050 }}>
                    <div className="modal-dialog modal-dialog-centered">
                        <div className="modal-content border-0 rounded-4 shadow">
                            <div className="modal-header border-0 pb-0">
                                <h5 className={`fw-bold m-0 ${actionType === 'approve' ? 'text-primary' : 'text-danger'}`}>
                                    {actionType === 'approve' ? 'Approve Seats' : 'Reject Seats'}
                                </h5>
                                <button type="button" className="btn-close" onClick={() => setShowSeatModal(false)}></button>
                            </div>
                            <div className="modal-body py-4">
                                <p className="text-muted small mb-3">Booking ID: #{selectedBooking.bookingId} | Passenger: {selectedBooking.passengerName}</p>
                                <div className="list-group list-group-flush border rounded-3 overflow-hidden">
                                    {selectedBooking.bookedSeats
                                        ?.filter(s => s.status === 'CancellationPending')
                                        .map(seat => (
                                            <div key={seat.bookedSeatId} className="list-group-item d-flex align-items-center gap-3 py-3 border-light cursor-pointer hover-bg-light"
                                                onClick={() => toggleSeat(seat.bookedSeatId)}>
                                                <input 
                                                    type="checkbox" 
                                                    className="form-check-input m-0 shadow-none border-primary" 
                                                    checked={selectedSeatIds.includes(seat.bookedSeatId)}
                                                    onChange={() => {}}
                                                />
                                                <div className="flex-grow-1">
                                                    <div className="fw-bold text-dark">{seat.seatNumber} - {seat.passengerName}</div>
                                                    {seat.cancellationReason && <div className="small text-danger italic">"{seat.cancellationReason}"</div>}
                                                </div>
                                            </div>
                                        ))}
                                </div>
                            </div>
                            <div className="modal-footer border-0 pt-0">
                                <button className="btn btn-light rounded-3 px-4" onClick={() => setShowSeatModal(false)}>Cancel</button>
                                <button className={`btn ${actionType === 'approve' ? 'btn-primary' : 'btn-danger'} rounded-3 px-4 fw-bold shadow-sm`} 
                                    onClick={handleConfirmAction}>
                                    Confirm {actionType === 'approve' ? 'Approval' : 'Rejection'}
                                </button>
                            </div>
                        </div>
                    </div>
                </div>
            )}

            <div className="row g-4 mt-4">
                <div className="col-md-4">
                    <StatsCard title="Total Pending Requests" value={requests.length} icon={<MdCancel size={28} />} color="#F59E0B" />
                </div>
                <div className="col-md-4">
                    <StatsCard title="Avg Amount" value={`₹${(requests.reduce((sum, r) => sum + r.totalAmount, 0) / (requests.length || 1)).toFixed(0)}`} icon={<MdAttachMoney size={28} />} color="#1E63FF" />
                </div>
                <div className="col-md-4">
                    <StatsCard title="Priority" value="High" icon={<MdAccessTime size={28} />} color="#22C55E" />
                </div>
            </div>
        </AdminLayout>
    );
};

export default Cancellations;
