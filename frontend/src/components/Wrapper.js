import React from 'react';

// A generic wrapper component using composition
const Wrapper = ({ children, style }) => {
  return <div style={{ padding: 16, border: '1px solid #ccc', borderRadius: 8, ...style }}>{children}</div>;
};

export default Wrapper;
