import React from 'react';

/**
 * Tripzo Logo — pure inline SVG, fully transparent, no PNG background.
 * Works on any background colour. No container, no box, no wrapper.
 *
 * Props:
 *  size  - width in px (height scales proportionally at 1.2×)
 *  style - extra inline styles
 */
const Logo = ({ size = 40, style = {}, className = '' }) => (
  <svg
    xmlns="http://www.w3.org/2000/svg"
    viewBox="0 0 100 125"
    width={size}
    height={size * 1.25}
    style={{ display: 'block', flexShrink: 0, ...style }}
    className={className}
    aria-label="Tripzo"
    role="img"
  >
    <defs>
      {/* Globe body gradient — deep blue edge, bright centre */}
      <radialGradient id="tg-globe" cx="40%" cy="35%" r="65%" fx="38%" fy="30%">
        <stop offset="0%" stopColor="#64B8FF" />
        <stop offset="40%" stopColor="#1E72E8" />
        <stop offset="80%" stopColor="#0D41A0" />
        <stop offset="100%" stopColor="#082880" />
      </radialGradient>

      {/* Pin stroke gradient */}
      <linearGradient id="tg-stroke" x1="0" y1="0" x2="1" y2="1">
        <stop offset="0%" stopColor="#4FC3F7" />
        <stop offset="100%" stopColor="#0A2A7A" />
      </linearGradient>

      {/* Drop shadow */}
      <filter id="tg-shadow" x="-15%" y="-10%" width="130%" height="135%">
        <feDropShadow dx="0" dy="4" stdDeviation="4" floodColor="#0A2A7A" floodOpacity="0.35" />
      </filter>
    </defs>

    {/* ── Map-pin body ── */}
    <path
      d="M50 3
         C27 3 9 21 9 44
         C9 67 30 83 50 108
         C70 83 91 67 91 44
         C91 21 73 3 50 3Z"
      fill="url(#tg-globe)"
      stroke="url(#tg-stroke)"
      strokeWidth="1.2"
      filter="url(#tg-shadow)"
    />

    {/* ── Earth grid lines (latitude / longitude) ── */}
    <g
      stroke="rgba(255,255,255,0.17)"
      strokeWidth="1"
      fill="none"
      clipPath="url(#tg-pin-clip)"
    >
      {/* Equator */}
      <ellipse cx="50" cy="44" rx="39" ry="11" />
      {/* Upper/lower lat */}
      <ellipse cx="50" cy="44" rx="36" ry="24" />
      {/* Central meridian */}
      <ellipse cx="50" cy="44" rx="11" ry="39" />
      {/* Angled meridian */}
      <ellipse cx="50" cy="44" rx="24" ry="39" transform="rotate(35 50 44)" />
    </g>

    {/* ── Bus silhouette (white, front-facing) ── */}
    {/* Body */}
    <rect x="25" y="27" width="50" height="30" rx="6" fill="white" />
    {/* Roof ridge */}
    <rect x="27" y="24" width="46" height="6" rx="3" fill="white" />
    {/* Windshield windows */}
    <rect x="59" y="32" width="11" height="11" rx="2" fill="rgba(100,180,255,0.85)" />
    {/* Passenger windows row */}
    <rect x="28" y="32" width="9" height="9" rx="2" fill="rgba(100,180,255,0.85)" />
    <rect x="40" y="32" width="9" height="9" rx="2" fill="rgba(100,180,255,0.85)" />
    <rect x="52" y="32" width="5" height="9" rx="2" fill="rgba(100,180,255,0.85)" />
    {/* Front bumper line */}
    <rect x="25" y="54" width="50" height="3" rx="1.5" fill="rgba(255,255,255,0.5)" />
    {/* Wheels */}
    <circle cx="35" cy="61" r="5.5" fill="rgba(30,50,120,0.7)" />
    <circle cx="35" cy="61" r="2.5" fill="white" />
    <circle cx="65" cy="61" r="5.5" fill="rgba(30,50,120,0.7)" />
    <circle cx="65" cy="61" r="2.5" fill="white" />

    {/* ── WiFi / signal arcs  (top-right, outside the pin) ── */}
    <g stroke="#29B6F6" strokeWidth="2.5" fill="none" strokeLinecap="round">
      <path d="M80 12 Q88  8 90 16" />
      <path d="M83  7 Q94  2 97 14" />
    </g>
  </svg>
);

export default Logo;
