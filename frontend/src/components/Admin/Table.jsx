import React from 'react';

const Table = ({ columns, data, actions, isLoading }) => {
  if (isLoading) {
    return (
      <div className="text-center py-5">
        <div className="spinner-border text-primary" role="status">
          <span className="visually-hidden">Loading...</span>
        </div>
      </div>
    );
  }

  if (!data || data.length === 0) {
    return <div className="text-center py-5 text-muted">No data found.</div>;
  }

  return (
    <div className="table-responsive table-container">
      <table className="table table-hover mb-0">
        <thead>
          <tr>
            {columns.map((col) => (
              <th key={col.key || col.label}>{col.label}</th>
            ))}
            {actions && <th>Actions</th>}
          </tr>
        </thead>
        <tbody>
          {data.map((row, index) => (
            <tr key={row.id || index}>
              {columns.map((col) => (
                <td key={`${row.id}-${col.key}`}>
                  {col.render ? col.render(row) : row[col.key]}
                </td>
              ))}
              {actions && (
                <td>
                  <div className="d-flex gap-2">
                    {actions.map((action, actionIdx) => (
                      <button
                        key={actionIdx}
                        className={`btn btn-sm ${action.className || 'btn-outline-primary'}`}
                        onClick={() => action.onClick(row)}
                        title={action.label}
                      >
                        {action.icon || action.label}
                      </button>
                    ))}
                  </div>
                </td>
              )}
            </tr>
          ))}
        </tbody>
      </table>
    </div>
  );
};

export default Table;
