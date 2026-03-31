import React, { useState, useEffect } from 'react';
import { useParams, useNavigate } from 'react-router-dom';
import { MdArrowBack, MdLocationOn, MdAccessTime, MdMap, MdEventSeat } from 'react-icons/md';
import operatorService from '../../services/operator/operatorService';

const RouteDetails = () => {
  const { routeId } = useParams();
  const navigate = useNavigate();
  const [route, setRoute] = useState(null);
  const [loading, setLoading] = useState(true);
  const [error, setError] = useState('');

  useEffect(() => {
    const fetchDetails = async () => {
      try {
        const res = await operatorService.getRouteDetails(routeId);
        setRoute(res.data);
      } catch (err) {
        setError('Failed to load route details.');
      } finally {
        setLoading(false);
      }
    };
    fetchDetails();
  }, [routeId]);

  if (loading) {
    return (
      <div className="d-flex justify-content-center align-items-center" style={{ minHeight: '60vh' }}>
        <div className="spinner-border text-success" role="status"><span className="visually-hidden">Loading...</span></div>
      </div>
    );
  }

  if (error) {
    return <div className="alert alert-danger">{error}</div>;
  }

  return (
    <div>
      <button className="btn btn-link text-decoration-none p-0 mb-3 d-flex align-items-center gap-1 text-muted" onClick={() => navigate('/operator/routes')}>
        <MdArrowBack /> Back to Routes
      </button>

      <h4 className="fw-bold mb-4">Route Details</h4>

      <div className="row g-4">
        {/* Route Summary */}
        <div className="col-lg-5">
          <div className="tripzo-card">
            <h6 className="fw-bold mb-3">Route Summary</h6>
            <div className="mb-3">
              <p className="text-muted small m-0">JOURNEY</p>
              <p className="fw-bold text-primary">{(route?.sourceCity || route?.SourceCity)} → {(route?.destCity || route?.DestCity)}</p>
            </div>
            <div className="row">
              <div className="col-6 mb-3">
                <p className="text-muted small m-0">BUS</p>
                <p className="fw-semibold">{(route?.busName || route?.BusName)}</p>
              </div>
              <div className="col-6 mb-3">
                <p className="text-muted small m-0">BUS NUMBER</p>
                <p className="fw-semibold"><code className="text-primary">{(route?.busNumber || route?.BusNumber)}</code></p>
              </div>
            </div>
            <div className="mb-3">
              <p className="text-muted small m-0">BASE FARE</p>
              <h4 className="fw-bold text-primary">₹{(route?.baseFare || route?.BaseFare)}</h4>
            </div>

            <div className="pt-3 border-top mt-3">
              <div className="d-flex justify-content-between align-items-center">
                <div>
                  <p className="text-muted small m-0 uppercase fw-bold" style={{letterSpacing: '0.5px'}}>ACTIVE BOOKINGS</p>
                  <h3 className="fw-extrabold m-0 text-success">{(route?.activeBookingsCount || route?.ActiveBookingsCount || 0)}</h3>
                  <small className="text-muted">Total confirmed upcoming</small>
                </div>
                <div className="bg-success bg-opacity-10 p-3 rounded-circle text-success">
                  <MdEventSeat size={24} />
                </div>
              </div>
            </div>
          </div>

          {/* Map Placeholder */}
          <div className="tripzo-card mt-3">
            <h6 className="fw-bold mb-3 d-flex align-items-center gap-2"><MdMap /> Route Map</h6>
            <div className="bg-light rounded-3 d-flex align-items-center justify-content-center" style={{ height: 200 }}>
              <div className="text-center text-muted">
                <MdMap size={48} style={{ opacity: 0.3 }} />
                <p className="small m-0 mt-2">Map view coming soon</p>
              </div>
            </div>
          </div>
        </div>

        {/* Stops Timeline */}
        <div className="col-lg-7">
          <div className="tripzo-card">
            <h6 className="fw-bold mb-4">Stops & Timings</h6>
            {route?.stops && route.stops.length > 0 ? (
              <div className="route-timeline">
                {route.stops.map((stop, i) => (
                  <div className="timeline-item d-flex gap-3 mb-0 position-relative" key={stop.stopId || i}>
                    {/* Timeline line */}
                    <div className="d-flex flex-column align-items-center" style={{ width: 30 }}>
                      <div className={`rounded-circle d-flex align-items-center justify-content-center ${i === 0 ? 'bg-primary' : i === route.stops.length - 1 ? 'bg-danger' : 'bg-primary'}`}
                        style={{ width: 28, height: 28, color: 'white', fontSize: '0.7rem', fontWeight: 'bold' }}>
                        {stop.stopOrder}
                      </div>
                      {i < route.stops.length - 1 && (
                        <div style={{ width: 2, height: 40, backgroundColor: '#E2E8F0' }}></div>
                      )}
                    </div>
                    {/* Content */}
                    <div className="pb-3 flex-grow-1">
                      <div className="d-flex justify-content-between align-items-start">
                        <div>
                          <p className="fw-bold m-0">{stop.cityName || stop.CityName}</p>
                          <p className="text-muted small m-0 d-flex align-items-center gap-1">
                            <MdLocationOn size={14} className="text-primary" /> {stop.locationName || stop.LocationName}
                          </p>
                        </div>
                        <div className="text-end">
                          <span className={`badge ${(stop.stopType || stop.StopType) === 'Boarding' ? 'bg-primary' : 'bg-warning'} bg-opacity-10 ${(stop.stopType || stop.StopType) === 'Boarding' ? 'text-primary' : 'text-warning'}`}>
                            {stop.stopType || stop.StopType}
                          </span>
                          <p className="m-0 small text-muted mt-1 d-flex align-items-center justify-content-end gap-1">
                            <MdAccessTime size={14} /> {stop.arrivalTime || stop.ArrivalTime}
                          </p>
                        </div>
                      </div>
                    </div>
                  </div>
                ))}
              </div>
            ) : (
              <p className="text-muted text-center py-4">No stops configured for this route.</p>
            )}
          </div>
        </div>
      </div>
    </div>
  );
};

export default RouteDetails;
