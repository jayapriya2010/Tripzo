import React, { useState, useEffect } from 'react';
import { useNavigate } from 'react-router-dom';
import { MdAdd, MdVisibility, MdRoute, MdSearch, MdChevronLeft, MdChevronRight } from 'react-icons/md';
import operatorService from '../../services/operator/operatorService';
import authService from '../../services/auth/authService';

const ManageRoutes = () => {
  const [routes, setRoutes] = useState([]);
  const [loading, setLoading] = useState(true);
  
  // Pagination & Filtering
  const [currentPage, setCurrentPage] = useState(1);
  const [totalCount, setTotalCount] = useState(0);
  const [pageSize, setPageSize] = useState(10);
  const [searchTerm, setSearchTerm] = useState('');

  const navigate = useNavigate();
  const user = authService.getCurrentUser();
  const operatorId = user?.userId || user?.UserId;

  useEffect(() => { 
    fetchRoutes(); 
  }, [currentPage, pageSize]);

  // Debounced search
  useEffect(() => {
    const timer = setTimeout(() => {
        if (currentPage !== 1) setCurrentPage(1);
        else fetchRoutes();
    }, 500);
    return () => clearTimeout(timer);
  }, [searchTerm]);

  const fetchRoutes = async () => {
    try {
      setLoading(true);
      const res = await operatorService.getOperatorRoutes(operatorId, {
          pageNumber: currentPage,
          pageSize: pageSize,
          searchTerm: searchTerm
      });
      setRoutes(res.data.items || []);
      setTotalCount(res.data.totalCount || 0);
    } catch {
      setRoutes([]);
      setTotalCount(0);
    } finally {
      setLoading(false);
    }
  };

  if (loading) {
    return (
      <div className="d-flex justify-content-center align-items-center" style={{ minHeight: '60vh' }}>
        <div className="spinner-border text-success" role="status"><span className="visually-hidden">Loading...</span></div>
      </div>
    );
  }

  return (
    <div>
      <div className="d-flex justify-content-between align-items-center mb-4">
        <div>
          <h4 className="fw-bold m-0">Manage Routes</h4>
          <p className="text-muted m-0">Create and view your bus routes</p>
        </div>
        <button className="btn btn-primary rounded-pill px-4 d-flex align-items-center gap-2" onClick={() => navigate('/operator/routes/add')}>
          <MdAdd size={20} /> Add Route
        </button>
      </div>

      {/* Stats */}
      <div className="row g-3 mb-4">
        <div className="col-md-6">
          <div className="tripzo-card text-center border-top border-4 border-success shadow-sm">
            <h3 className="fw-bold text-success m-0">{totalCount}</h3>
            <p className="text-muted m-0 small uppercase fw-bold">Total Active Routes</p>
          </div>
        </div>
        <div className="col-md-6">
          <div className="tripzo-card text-center border-top border-4 border-primary shadow-sm">
            <h3 className="fw-bold text-primary m-0">{Math.ceil(totalCount / pageSize)}</h3>
            <p className="text-muted m-0 small uppercase fw-bold">Total Pages</p>
          </div>
        </div>
      </div>

      {/* Search Bar */}
      <div className="tripzo-card mb-4 shadow-sm p-3">
        <div className="position-relative">
            <MdSearch className="position-absolute top-50 translate-middle-y ms-3 text-muted" size={20} />
            <input 
                type="text" 
                className="form-control rounded-pill ps-5 border-0 bg-light shadow-none" 
                placeholder="Search by city (source or destination)..." 
                value={searchTerm}
                onChange={(e) => setSearchTerm(e.target.value)}
            />
        </div>
      </div>

      {/* Routes Table */}
      <div className="tripzo-card border-top border-4 border-primary shadow-sm">
        {routes.length === 0 ? (
          <div className="text-center py-5">
            <MdRoute size={64} className="text-muted mb-3" style={{ opacity: 0.2 }} />
            <p className="text-muted">No routes found. Create your first route!</p>
          </div>
        ) : (
          <div className="table-responsive">
            <table className="table table-hover mb-0">
              <thead className="bg-light">
                <tr>
                  <th className="border-0">Source → Destination</th>
                  <th className="border-0">Base Fare</th>
                  <th className="border-0">Assigned Bus</th>
                  <th className="border-0">Stops</th>
                  <th className="border-0">Actions</th>
                </tr>
              </thead>
              <tbody>
                {routes.map(route => (
                  <tr key={route.routeId}>
                    <td className="fw-bold text-dark">{route.sourceCity} → {route.destCity}</td>
                    <td className="fw-semibold text-primary">₹{route.baseFare}</td>
                    <td>
                      <div className="d-flex flex-column">
                        <span className="fw-semibold small">{route.busName}</span>
                        <code className="text-muted x-small" style={{ fontSize: '0.7rem' }}>{route.busNumber}</code>
                      </div>
                    </td>
                    <td><span className="badge bg-primary bg-opacity-10 text-primary rounded-pill px-3">{route.stops?.length || 0} Stops</span></td>
                    <td>
                      <button
                        className="btn btn-sm btn-primary rounded-pill px-3 shadow-sm"
                        onClick={() => navigate(`/operator/routes/${route.routeId}`)}
                      >
                        <MdVisibility size={16} className="me-1" /> View Details
                      </button>
                    </td>
                  </tr>
                ))}
              </tbody>
            </table>
          </div>
        )}
      </div>

      {/* Pagination */}
      {totalCount > 0 && (
        <div className="d-flex justify-content-between align-items-center mt-4 px-2">
          <div className="d-flex align-items-center gap-3">
            <span className="text-muted small fw-bold text-uppercase" style={{ fontSize: '0.65rem' }}>Show:</span>
            <select 
                className="form-select form-select-sm bg-light border-0 shadow-none rounded-3" 
                style={{ width: 'auto' }}
                value={pageSize}
                onChange={(e) => {
                    setPageSize(parseInt(e.target.value));
                    setCurrentPage(1);
                }}
            >
                <option value="5">5 per page</option>
                <option value="10">10 per page</option>
                <option value="20">20 per page</option>
                <option value="50">50 per page</option>
            </select>
            <span className="text-muted small">
                Showing {((currentPage - 1) * pageSize) + 1}–{Math.min(currentPage * pageSize, totalCount)} of {totalCount}
            </span>
          </div>
          
          <div className="d-flex gap-2">
            <button
                className="btn btn-sm btn-outline-primary rounded-3 d-flex align-items-center gap-1"
                disabled={currentPage === 1}
                onClick={() => setCurrentPage(p => p - 1)}
            >
                <MdChevronLeft /> Prev
            </button>
            {(() => {
                const totalPages = Math.ceil(totalCount / pageSize);
                return Array.from({ length: Math.min(totalPages, 5) }, (_, i) => {
                    let page;
                    if (totalPages <= 5) page = i + 1;
                    else if (currentPage <= 3) page = i + 1;
                    else if (currentPage >= totalPages - 2) page = totalPages - 4 + i;
                    else page = currentPage - 2 + i;
                    
                    return (
                        <button
                            key={page}
                            className={`btn btn-sm rounded-3 fw-bold ${currentPage === page ? 'btn-primary shadow-sm' : 'btn-outline-primary border-0'}`}
                            onClick={() => setCurrentPage(page)}
                        >
                            {page}
                        </button>
                    );
                });
            })()}
            <button
                className="btn btn-sm btn-outline-primary rounded-3 d-flex align-items-center gap-1"
                disabled={currentPage >= Math.ceil(totalCount / pageSize)}
                onClick={() => setCurrentPage(p => p + 1)}
            >
                Next <MdChevronRight />
            </button>
          </div>
        </div>
      )}
    </div>
  );
};

export default ManageRoutes;
