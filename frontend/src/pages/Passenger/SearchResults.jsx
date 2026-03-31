import React, { useState, useEffect } from 'react';
import { useNavigate, useLocation } from 'react-router-dom';
import {
  MdSearch, MdDirectionsBus, MdStar, MdWifi, MdAcUnit,
  MdChair, MdFilterList, MdArrowForward, MdAccessTime
} from 'react-icons/md';
import PassengerLayout from '../../layouts/PassengerLayout';
import passengerService from '../../services/passengerService';

const SearchResults = () => {
  const navigate = useNavigate();
  const location = useLocation();
  const params = location.state || {};

  const [form, setForm] = useState({
    fromCity: params.fromCity || '',
    toCity: params.toCity || '',
    travelDate: params.travelDate || new Date().toISOString().split('T')[0],
  });
  const [buses, setBuses] = useState([]);
  const [filteredBuses, setFilteredBuses] = useState([]);
  const [loading, setLoading] = useState(false);
  const [searched, setSearched] = useState(false);
  const [meta, setMeta] = useState({});
  const [filters, setFilters] = useState({
    busType: '',
    minFare: '',
    maxFare: '',
    rating: '',
  });

  useEffect(() => {
    if (params.fromCity && params.toCity && params.travelDate) {
      doSearch(form);
    }
  }, []);

  useEffect(() => {
    applyFilters();
  }, [filters, buses]);

  const doSearch = async (searchParams) => {
    setLoading(true);
    setSearched(true);
    try {
      const res = await passengerService.searchBuses({
        fromCity: searchParams.fromCity,
        toCity: searchParams.toCity,
        travelDate: searchParams.travelDate,
        busType: filters.busType || undefined,
        minFare: filters.minFare || undefined,
        maxFare: filters.maxFare || undefined,
      });
      const items = res.data?.items || [];
      setBuses(items);
      setMeta(res.data?.meta || {});
    } catch {
      setBuses([]);
    } finally {
      setLoading(false);
    }
  };

  const applyFilters = () => {
    let result = [...buses];
    if (filters.busType) result = result.filter(b => b.busType?.toLowerCase().includes(filters.busType.toLowerCase()));
    if (filters.minFare) result = result.filter(b => b.fare >= parseFloat(filters.minFare));
    if (filters.maxFare) result = result.filter(b => b.fare <= parseFloat(filters.maxFare));
    if (filters.rating) result = result.filter(b => (b.averageRating || 0) >= parseFloat(filters.rating));
    setFilteredBuses(result);
  };

  const handleSearch = (e) => {
    e.preventDefault();
    doSearch(form);
  };

  const handleSelectSeat = (bus) => {
    navigate('/passenger/seats', {
      state: {
        busId: bus.busId,
        routeId: bus.routeId,
        travelDate: form.travelDate,
        bus,
        fromCity: form.fromCity,
        toCity: form.toCity,
      }
    });
  };

  const busTypeColors = {
    'AC Sleeper': '#1E63FF',
    'AC Seater': '#0F3D91',
    'Non-AC': '#F59E0B',
    'Sleeper': '#22C55E',
  };

  return (
    <PassengerLayout>
      {/* Search Bar */}
      <div className="tripzo-card mb-4">
        <h5 className="fw-bold mb-3 d-flex align-items-center gap-2">
          <MdSearch color="var(--primary-blue)" /> Search Buses
        </h5>
        <form onSubmit={handleSearch}>
          <div className="row g-3 align-items-end">
            <div className="col-md-3">
              <label className="form-label fw-semibold small text-muted">FROM</label>
              <input type="text" className="form-control rounded-3" placeholder="e.g., Chennai"
                value={form.fromCity} onChange={e => setForm({ ...form, fromCity: e.target.value })} required />
            </div>
            <div className="col-md-3">
              <label className="form-label fw-semibold small text-muted">TO</label>
              <input type="text" className="form-control rounded-3" placeholder="e.g., Bangalore"
                value={form.toCity} onChange={e => setForm({ ...form, toCity: e.target.value })} required />
            </div>
            <div className="col-md-3">
              <label className="form-label fw-semibold small text-muted">DATE</label>
              <input type="date" className="form-control rounded-3"
                min={new Date().toISOString().split('T')[0]}
                value={form.travelDate} onChange={e => setForm({ ...form, travelDate: e.target.value })} required />
            </div>
            <div className="col-md-3">
              <button type="submit" className="btn btn-primary w-100 rounded-3 py-2 fw-semibold">
                <MdSearch className="me-1" /> Search
              </button>
            </div>
          </div>
        </form>
      </div>

      <div className="row g-4">
        {/* Filters Sidebar */}
        <div className="col-md-3">
          <div className="tripzo-card">
            <h6 className="fw-bold mb-3 d-flex align-items-center gap-2">
              <MdFilterList /> Filters
            </h6>
            <div className="mb-3">
              <label className="form-label small fw-semibold text-muted">BUS TYPE</label>
              <select className="form-select form-select-sm rounded-3"
                value={filters.busType} onChange={e => setFilters({ ...filters, busType: e.target.value })}>
                <option value="">All Types</option>
                <option value="AC Sleeper">AC Sleeper</option>
                <option value="AC Seater">AC Seater</option>
                <option value="Non-AC">Non-AC</option>
                <option value="Sleeper">Sleeper</option>
              </select>
            </div>
            <div className="mb-3">
              <label className="form-label small fw-semibold text-muted">MIN FARE (₹)</label>
              <input type="number" className="form-control form-control-sm rounded-3"
                placeholder="0" value={filters.minFare}
                onChange={e => setFilters({ ...filters, minFare: e.target.value })} />
            </div>
            <div className="mb-3">
              <label className="form-label small fw-semibold text-muted">MAX FARE (₹)</label>
              <input type="number" className="form-control form-control-sm rounded-3"
                placeholder="5000" value={filters.maxFare}
                onChange={e => setFilters({ ...filters, maxFare: e.target.value })} />
            </div>
            <div className="mb-3">
              <label className="form-label small fw-semibold text-muted">MIN RATING</label>
              <select className="form-select form-select-sm rounded-3"
                value={filters.rating} onChange={e => setFilters({ ...filters, rating: e.target.value })}>
                <option value="">Any</option>
                {[4, 3, 2, 1].map(r => <option key={r} value={r}>{r}+ Stars</option>)}
              </select>
            </div>
            <button className="btn btn-sm btn-outline-secondary w-100 rounded-3"
              onClick={() => setFilters({ busType: '', minFare: '', maxFare: '', rating: '' })}>
              Clear Filters
            </button>
          </div>
        </div>

        {/* Results */}
        <div className="col-md-9">
          {!searched ? (
            <div className="text-center py-5 tripzo-card">
              <MdDirectionsBus size={64} color="#CBD5E1" />
              <p className="text-muted mt-2">Enter your source, destination and date to search.</p>
            </div>
          ) : loading ? (
            <div className="text-center py-5">
              <div className="spinner-border text-primary" role="status" />
              <p className="text-muted mt-3">Finding best buses for you...</p>
            </div>
          ) : filteredBuses.length === 0 ? (
            <div className="text-center py-5 tripzo-card">
              <MdDirectionsBus size={64} color="#CBD5E1" />
              <p className="text-muted mt-2">No buses found. Try different filters or dates.</p>
            </div>
          ) : (
            <>
              <div className="d-flex justify-content-between align-items-center mb-3">
                <p className="text-muted mb-0 small">
                  Showing <strong>{filteredBuses.length}</strong> buses for{' '}
                  <strong>{form.fromCity} → {form.toCity}</strong> on{' '}
                  <strong>{new Date(form.travelDate).toLocaleDateString('en-IN', { day: '2-digit', month: 'short', year: 'numeric' })}</strong>
                </p>
              </div>
              {filteredBuses.map(bus => (
                <div className="tripzo-card mb-3" key={bus.busId + '-' + bus.routeId}>
                  <div className="row align-items-center">
                    <div className="col-md-7">
                      <div className="d-flex align-items-start gap-3">
                        <div className="rounded-3 d-flex align-items-center justify-content-center"
                          style={{ width: 48, height: 48, background: '#E8F0FF', color: '#1E63FF', flexShrink: 0 }}>
                          <MdDirectionsBus size={26} />
                        </div>
                        <div>
                          <h6 className="fw-bold mb-1">{bus.busName}</h6>
                          <span className="badge rounded-pill text-white small"
                            style={{ background: busTypeColors[bus.busType] || '#6B7280' }}>
                            {bus.busType}
                          </span>
                          <div className="d-flex align-items-center gap-2 mt-2">
                            <MdAccessTime size={14} className="text-muted" />
                            <span className="text-muted small">{bus.departureTime?.slice(0, 5)}</span>
                            <MdArrowForward size={14} className="text-muted" />
                            <span className="text-muted small">{form.fromCity} → {form.toCity}</span>
                          </div>
                          {bus.amenities?.length > 0 && (
                            <div className="d-flex flex-wrap gap-1 mt-2">
                              {bus.amenities.slice(0, 4).map(a => (
                                <span key={a} className="badge rounded-pill small"
                                  style={{ background: '#E8F0FF', color: '#1E63FF', fontWeight: 500 }}>
                                  {a}
                                </span>
                              ))}
                              {bus.amenities.length > 4 && (
                                <span className="badge rounded-pill small"
                                  style={{ background: '#F3F4F6', color: '#6B7280' }}>
                                  +{bus.amenities.length - 4} more
                                </span>
                              )}
                            </div>
                          )}
                        </div>
                      </div>
                    </div>
                    <div className="col-md-3 text-center">
                      <div className="d-flex align-items-center justify-content-center gap-1 mb-1">
                        <MdStar color="#F59E0B" />
                        <span className="fw-bold">{bus.averageRating?.toFixed(1) || 'N/A'}</span>
                        <span className="text-muted small">({bus.totalReviews || 0})</span>
                      </div>
                      <p className="text-muted small mb-0">
                        <span className="fw-semibold" style={{ color: bus.availableSeats > 10 ? '#22C55E' : '#EF4444' }}>
                          {bus.availableSeats}
                        </span> seats left
                      </p>
                    </div>
                    <div className="col-md-2 text-end">
                      <p className="fw-bold mb-1" style={{ fontSize: '1.25rem', color: 'var(--primary-blue)' }}>
                        ₹{bus.fare}
                      </p>
                      <button
                        className="btn btn-primary btn-sm rounded-3 fw-semibold"
                        onClick={() => handleSelectSeat(bus)}
                        disabled={bus.availableSeats === 0}
                      >
                        {bus.availableSeats === 0 ? 'Full' : 'Select Seat'}
                      </button>
                    </div>
                  </div>
                </div>
              ))}
            </>
          )}
        </div>
      </div>
    </PassengerLayout>
  );
};

export default SearchResults;
