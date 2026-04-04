import React, { useState, useEffect } from 'react';
import { MdClose, MdDownload, MdDirectionsBus, MdPrint } from 'react-icons/md';
import jsPDF from 'jspdf';
import passengerService from '../../services/passenger/passengerService';

const TicketModal = ({ bookingId, onClose }) => {
  const [ticket, setTicket] = useState(null);
  const [loading, setLoading] = useState(true);
  const [error, setError] = useState('');

  useEffect(() => {
    if (bookingId) {
      setLoading(true);
      passengerService.getTicketDetails(bookingId)
        .then(res => setTicket(res.data))
        .catch(err => setError(err.response?.data?.message || 'Failed to load ticket details.'))
        .finally(() => setLoading(false));
    }
  }, [bookingId]);

  const handleDownloadPDF = () => {
    if (!ticket) return;
    const doc = new jsPDF();
    const p = ticket.passengers || [];

    // Colors
    const primaryBlue = [30, 99, 255]; // #1E63FF
    const confirmedGreen = [34, 163, 74]; // Success Green
    const accentRed = [239, 68, 68]; // Error/Accent Red
    const textGray = [100, 116, 139]; // Slate 500

    // Header
    doc.setTextColor(...primaryBlue);
    doc.setFontSize(24);
    doc.setFont('helvetica', 'bold');
    doc.text('Tripzo Bus Ticket', 20, 25);

    doc.setTextColor(0, 0, 0);
    doc.setFontSize(16);
    doc.text(`#${ticket.bookingId}`, 190, 25, { align: 'right' });

    // Passenger & Journey Info
    let y = 40;
    doc.setFontSize(10);
    doc.setFont('helvetica', 'bold');
    doc.text('Passenger', 20, y);
    doc.text('Journey Date', 110, y);

    y += 7;
    doc.setFont('helvetica', 'normal');
    doc.text(ticket.passengerName, 20, y);
    doc.text(new Date(ticket.departureDateTime).toLocaleDateString('en-IN', { weekday: 'long', day: '2-digit', month: 'long', year: 'numeric' }), 110, y);
    
    y += 5;
    doc.setTextColor(...textGray);
    doc.text(ticket.passengerEmail, 20, y);

    // Route Section
    y += 10;
    doc.setDrawColor(226, 232, 240); // #E2E8F0
    doc.line(20, y, 190, y);
    
    y += 10;
    doc.setFontSize(10);
    doc.setTextColor(0, 0, 0);
    doc.setFont('helvetica', 'bold');
    doc.text('From', 20, y);
    doc.text('To', 120, y);

    // Arrow
    doc.setFontSize(14);
    doc.text('->', 103, y + 2, { align: 'center' });

    y += 8;
    doc.setFontSize(16);
    doc.setFont('helvetica', 'normal');
    doc.text(ticket.sourceCity, 20, y);
    doc.text(ticket.destCity, 120, y);

    y += 8;
    doc.line(20, y, 190, y);

    // Bus Details & Booked By
    y += 10;
    doc.setFontSize(10);
    doc.setFont('helvetica', 'bold');
    doc.text('Bus', 20, y);
    doc.text('Booked by', 190, y, { align: 'right' });

    y += 7;
    doc.setFont('helvetica', 'normal');
    const busInfo = `${ticket.busName} (${ticket.busType})${ticket.busNumber ? ` - ${ticket.busNumber}` : ''}`;
    doc.text(busInfo, 20, y);
    doc.text(ticket.passengerName, 190, y, { align: 'right' });

    // Traveler Information
    y += 15;
    doc.setFont('helvetica', 'bold');
    doc.setFontSize(12);
    doc.text('Traveler Information', 20, y);
    doc.setDrawColor(0, 0, 0);
    doc.line(20, y + 1, 60, y + 1); // Underline

    y += 10;
    doc.setFontSize(9);
    doc.setFont('helvetica', 'bold');
    doc.text('Seat', 20, y);
    doc.text('Name', 40, y);
    doc.text('Age', 160, y, { align: 'right' });
    doc.text('Gender', 190, y, { align: 'right' });
    
    doc.setDrawColor(0, 0, 0);
    doc.setLineWidth(0.5);
    doc.line(20, y + 2, 190, y + 2);

    y += 8;
    doc.setFont('helvetica', 'normal');
    p.forEach((pass, i) => {
      doc.text(ticket.seatNumbers[i] || '--', 20, y);
      doc.text(pass.name, 40, y);
      doc.text(pass.age.toString(), 160, y, { align: 'right' });
      doc.text(pass.gender, 190, y, { align: 'right' });
      y += 7;
    });

    y += 5;
    doc.line(20, y, 190, y);

    // Amount & Status
    y += 15;
    doc.setFont('helvetica', 'bold');
    doc.setFontSize(11);
    doc.text('Total Amount', 20, y);

    y += 10;
    doc.setTextColor(...primaryBlue);
    doc.setFontSize(20);
    doc.text(`Rs. ${ticket.totalAmount.toLocaleString('en-IN')}.00`, 20, y);

    doc.setTextColor(...confirmedGreen);
    doc.setFontSize(12);
    doc.text('Status: CONFIRMED', 190, y, { align: 'right' });

    // Footer
    doc.setTextColor(...accentRed);
    doc.setFontSize(10);
    doc.setFont('helvetica', 'bold');
    doc.text('Note: Please bring any govt. issued id for verification on the date of travel.', 105, 275, { align: 'center' });
    
    doc.setTextColor(0, 0, 0);
    const bookingDate = new Date(ticket.bookingDate).toLocaleDateString('en-IN', { day: '2-digit', month: 'short', year: 'numeric' });
    doc.text(`Booked on ${bookingDate} | Thank you for choosing Tripzo!`, 105, 282, { align: 'center' });

    // Save
    doc.save(`Tripzo_Ticket_${ticket.bookingId}.pdf`);
  };

  if (!bookingId) return null;

  return (
    <div className="modal fade show d-block" style={{ backgroundColor: 'rgba(0,0,0,0.6)', zIndex: 1050 }}>
      <div className="modal-dialog modal-dialog-centered">
        <div className="modal-content border-0 rounded-4 shadow-lg overflow-hidden" style={{ maxWidth: '480px', margin: '0 auto' }}>
          
          {/* Modal Header */}
          <div className="modal-header border-0 pb-0 position-absolute end-0 top-0" style={{ zIndex: 10 }}>
            <button type="button" className="btn-close bg-white rounded-circle p-2 shadow-sm" onClick={onClose} style={{ transform: 'scale(0.8)' }}></button>
          </div>

          <div className="modal-body p-0">
            {loading ? (
              <div className="text-center py-5">
                <div className="spinner-border text-primary" role="status"></div>
                <p className="mt-2 text-muted small">Generating Ticket...</p>
              </div>
            ) : error ? (
              <div className="p-4 text-center">
                <div className="alert alert-danger small mb-0">{error}</div>
              </div>
            ) : (
              <div className="ticket-container">
                {/* Visual Ticket Styling based on success page */}
                <div style={{ background: 'linear-gradient(135deg, #1E63FF 0%, #0F3D91 100%)', padding: '24px 24px 30px', color: 'white' }}>
                    <div className="d-flex align-items-center justify-content-between mb-3">
                        <div className="d-flex align-items-center gap-2">
                            <MdDirectionsBus size={24} />
                            <span className="fw-bold fs-5">Tripzo</span>
                        </div>
                        <span className="badge rounded-pill bg-white bg-opacity-25 px-2 py-1 small">
                            Confirmed
                        </span>
                    </div>
                    <div className="d-flex align-items-center justify-content-between gap-2 mt-2">
                        <div className="text-start">
                            <div className="fw-bold fs-4">{ticket.sourceCity}</div>
                            <div className="opacity-75 small">Departure</div>
                            <div className="fw-bold small mt-1">{new Date(ticket.departureDateTime).toLocaleDateString('en-IN', { day: '2-digit', month: 'short' })}</div>
                            <div className="small opacity-75">{new Date(ticket.departureDateTime).toLocaleTimeString('en-IN', { hour: '2-digit', minute: '2-digit', hour12: true })}</div>
                        </div>
                        <div className="flex-grow-1 px-2">
                            <div style={{ borderTop: '2px dashed rgba(255,255,255,0.3)', position: 'relative' }}>
                                <MdDirectionsBus size={18} className="position-absolute" style={{ top: -10, left: '50%', transform: 'translateX(-50%)' }} />
                            </div>
                        </div>
                        <div className="text-end">
                            <div className="fw-bold fs-4">{ticket.destCity}</div>
                            <div className="opacity-75 small">Arrival</div>
                            <div className="fw-bold small mt-1">{new Date(ticket.arrivalDateTime).toLocaleDateString('en-IN', { day: '2-digit', month: 'short' })}</div>
                            <div className="small opacity-75">{new Date(ticket.arrivalDateTime).toLocaleTimeString('en-IN', { hour: '2-digit', minute: '2-digit', hour12: true })}</div>
                        </div>
                    </div>
                </div>

                {/* Ticket Notch Line */}
                <div className="d-flex align-items-center" style={{ margin: '-12px -1px 0', position: 'relative', zIndex: 5 }}>
                    <div style={{ width: 24, height: 24, borderRadius: '50%', background: '#f8fafc', flexShrink: 0 }} />
                    <div style={{ flex: 1, borderTop: '2px dashed #E2E8F0' }} />
                    <div style={{ width: 24, height: 24, borderRadius: '50%', background: '#f8fafc', flexShrink: 0 }} />
                </div>

                <div className="bg-white p-4 pt-1">
                    <div className="row g-3 mb-4">
                        <div className="col-6">
                            <p className="text-muted small mb-0">PNR Number</p>
                            <p className="fw-bold mb-0 text-primary" style={{ letterSpacing: '1px' }}>
                                TRPZ{ticket.bookingId}1930599
                            </p>
                        </div>
                        <div className="col-6 text-end">
                            <p className="text-muted small mb-0">Amount Paid</p>
                            <p className="fw-bold mb-0 text-success fs-5">₹{ticket.totalAmount}</p>
                        </div>
                        <div className="col-6">
                            <p className="text-muted small mb-0">Bus Name</p>
                            <p className="fw-semibold mb-0 small">{ticket.busName} ({ticket.busType})</p>
                        </div>
                        <div className="col-6 text-end">
                            <p className="text-muted small mb-0">Bus Number</p>
                            <p className="fw-semibold mb-0 small">{ticket.busNumber}</p>
                        </div>
                    </div>

                    <div className="border-top pt-3 mb-3">
                        <p className="text-muted small fw-bold mb-2">TRAVELER DETAILS</p>
                        <div className="table-responsive">
                            <table className="table table-borderless table-sm mb-0">
                                <thead>
                                    <tr className="text-muted" style={{ fontSize: '0.65rem', borderBottom: '1px solid #f1f5f9' }}>
                                        <th>SEAT</th>
                                        <th>NAME</th>
                                        <th>AGE</th>
                                        <th>GENDER</th>
                                    </tr>
                                </thead>
                                <tbody>
                                    {ticket.passengers.map((p, idx) => (
                                        <tr key={idx} style={{ fontSize: '0.75rem' }}>
                                            <td className="fw-bold">{ticket.seatNumbers[idx] || '--'}</td>
                                            <td>{p.name}</td>
                                            <td>{p.age}</td>
                                            <td>{p.gender}</td>
                                        </tr>
                                    ))}
                                </tbody>
                            </table>
                        </div>
                    </div>

                    {/* Barcode */}
                    <div className="text-center mt-2 pt-3 border-top">
                        <div style={{ display: 'flex', gap: 2, justifyContent: 'center', height: 35 }}>
                            {Array.from({ length: 25 }, (_, i) => (
                                <div key={i} style={{
                                width: i % 4 === 0 ? 3 : 1,
                                height: '100%',
                                background: '#1a202c',
                                opacity: i % 7 === 0 ? 0.3 : 1
                                }} />
                            ))}
                        </div>
                        <p className="small text-muted mt-1 mb-0" style={{ fontSize: '0.65rem', letterSpacing: '2px' }}>
                            TRPZ{ticket.bookingId}1930599
                        </p>
                    </div>

                    <div className="mt-4 row g-2">
                        <div className="col-12 mb-2 text-center text-danger fw-bold" style={{ fontSize: '0.7rem' }}>
                            * Please bring any govt. issued id for verification on the date of travel.
                        </div>
                        <div className="col-12">
                            <button className="btn btn-primary w-100 rounded-3 py-2 fw-semibold d-flex align-items-center justify-content-center gap-2"
                                onClick={handleDownloadPDF}>
                                <MdDownload size={18} /> Download Ticket
                            </button>
                        </div>
                    </div>
                </div>
              </div>
            )}
          </div>
        </div>
      </div>
    </div>
  );
};

export default TicketModal;
