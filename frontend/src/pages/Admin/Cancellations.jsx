import React, { useState, useEffect } from 'react';
import AdminLayout from '../../layouts/AdminLayout';
import Table from '../../components/Admin/Table';
import cancellationService from '../../services/admin/cancellationService';
import StatsCard from '../../components/Admin/StatsCard';
import { MdCancel, MdCheck, MdClose, MdAttachMoney, MdAccessTime } from 'react-icons/md';

const Cancellations = () => {
    const [requests, setRequests] = useState([]);
    const [loading, setLoading] = useState(true);

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

    const handleApprove = async (id) => {
        if (window.confirm('Approve this refund request?')) {
            try {
                await cancellationService.approveCancellation(id);
                fetchRequests();
            } catch (error) {
                alert('Error approving cancellation');
            }
        }
    };

    const handleReject = async (id) => {
        if (window.confirm('Reject this refund request?')) {
            try {
                await cancellationService.rejectCancellation(id);
                fetchRequests();
            } catch (error) {
                alert('Error rejecting cancellation');
            }
        }
    };

    const columns = [
        { label: 'PASSENGER', key: 'passengerName' },
        { label: 'BUS DETAILS', key: 'busNumber' },
        { 
            label: 'JOURNEY DATE', 
            key: 'journeyDate',
            render: (row) => new Date(row.journeyDate).toLocaleDateString()
        },
        { 
            label: 'REFUND AMOUNT', 
            key: 'totalAmount',
            render: (row) => `₹${row.totalAmount}`
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
                <span className="badge-status badge-pending">
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
            onClick: (row) => handleApprove(row.bookingId) 
        },
        { 
            label: 'Reject', 
            icon: <MdClose />, 
            className: 'btn-outline-danger',
            onClick: (row) => handleReject(row.bookingId) 
        }
    ];

    return (
        <AdminLayout>
            <div className="d-flex align-items-center justify-content-between mb-4">
                <div>
                    <h4 className="fw-bold m-0">Cancellation Requests</h4>
                    <p className="text-muted small">Approve or reject passenger refund requests.</p>
                </div>
            </div>

            <Table columns={columns} data={requests} actions={actions} isLoading={loading} />

            <div className="row g-4 mt-4">
                <div className="col-md-4">
                    <StatsCard title="Total Pending" value={requests.length} icon={<MdCancel size={28} />} color="#F59E0B" />
                </div>
                <div className="col-md-4">
                    <StatsCard title="Refund Volume" value={`₹${requests.reduce((sum, r) => sum + r.totalAmount, 0).toLocaleString()}`} icon={<MdAttachMoney size={28} />} color="#1E63FF" />
                </div>
                <div className="col-md-4">
                    <StatsCard title="Avg Response Time" value="4.2 hrs" icon={<MdAccessTime size={28} />} color="#22C55E" />
                </div>
            </div>
        </AdminLayout>
    );
};

export default Cancellations;
