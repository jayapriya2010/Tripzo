import React, { useState, useEffect } from 'react';
import { Link } from 'react-router-dom';
import Logo from '../components/common/Logo';

/* ─────────────────────────────────────────────────────────────
   Inline styles — no extra CSS file dependency needed for
   the landing page shell. Theme vars from theme.css are reused.
───────────────────────────────────────────────────────────────*/

const LandingPage = () => {
  const [scrolled, setScrolled] = useState(false);
  const [mobileMenuOpen, setMobileMenuOpen] = useState(false);

  useEffect(() => {
    const onScroll = () => setScrolled(window.scrollY > 60);
    window.addEventListener('scroll', onScroll);
    return () => window.removeEventListener('scroll', onScroll);
  }, []);

  /* ── stats counter animation ── */
  const [counts, setCounts] = useState({ routes: 0, passengers: 0, cities: 0, rating: 0 });
  useEffect(() => {
    const targets = { routes: 500, passengers: 50000, cities: 120, rating: 48 };
    const duration = 1800;
    const steps = 60;
    const interval = duration / steps;
    let step = 0;
    const timer = setInterval(() => {
      step++;
      const progress = step / steps;
      setCounts({
        routes: Math.round(targets.routes * progress),
        passengers: Math.round(targets.passengers * progress),
        cities: Math.round(targets.cities * progress),
        rating: Math.round(targets.rating * progress),
      });
      if (step >= steps) clearInterval(timer);
    }, interval);
    return () => clearInterval(timer);
  }, []);

  return (
    <div className="lp-root">

      {/* ══════════════════════════════
            NAVBAR
         ══════════════════════════════ */}
      <nav className={`lp-nav${scrolled ? ' lp-nav--scrolled' : ''}`}>
        <div className="lp-nav-inner">
          {/* Brand */}
          <Link to="/" className="lp-brand">
            <Logo size={42} />
            <span className="lp-brand-text">Tripzo</span>
          </Link>

          {/* Desktop links */}
          <div className="lp-nav-links">
            <a href="#features" className="lp-nav-link">Features</a>
            <a href="#how-it-works" className="lp-nav-link">How it works</a>
            <a href="#stats" className="lp-nav-link">About</a>
          </div>

          {/* Auth buttons */}
          <div className="lp-nav-auth">
            <Link to="/login" className="lp-btn-ghost">Sign In</Link>
            <Link to="/signup" className="lp-btn-solid">Get Started</Link>
          </div>

          {/* Hamburger */}
          <button className="lp-hamburger" onClick={() => setMobileMenuOpen(o => !o)} aria-label="Menu">
            <span className={`lp-ham-line${mobileMenuOpen ? ' open' : ''}`} />
            <span className={`lp-ham-line${mobileMenuOpen ? ' open' : ''}`} />
            <span className={`lp-ham-line${mobileMenuOpen ? ' open' : ''}`} />
          </button>
        </div>

        {/* Mobile dropdown */}
        {mobileMenuOpen && (
          <div className="lp-mobile-menu">
            <a href="#features" className="lp-mobile-link" onClick={() => setMobileMenuOpen(false)}>Features</a>
            <a href="#how-it-works" className="lp-mobile-link" onClick={() => setMobileMenuOpen(false)}>How it works</a>
            <a href="#stats" className="lp-mobile-link" onClick={() => setMobileMenuOpen(false)}>About</a>
            <div className="lp-mobile-auth">
              <Link to="/login" className="lp-btn-ghost w-100 text-center d-block mb-2">Sign In</Link>
              <Link to="/signup" className="lp-btn-solid w-100 text-center d-block">Get Started</Link>
            </div>
          </div>
        )}
      </nav>

      {/* ══════════════════════════════
            HERO
         ══════════════════════════════ */}
      <section className="lp-hero">
        {/* Floating blobs */}
        <div className="lp-blob lp-blob-1" />
        <div className="lp-blob lp-blob-2" />
        <div className="lp-blob lp-blob-3" />

        <div className="lp-hero-content">
          <div className="lp-hero-badge">
            <span className="lp-badge-dot" />
            India's Smartest Bus Booking Platform
          </div>

          <h1 className="lp-hero-title">
            Travel Smart,<br />
            <span className="lp-gradient-text">Book in Seconds</span>
          </h1>

          <p className="lp-hero-sub">
            Search hundreds of bus routes, pick your perfect seat, and book
            your journey — all in one seamless experience.
          </p>

          {/* Hero search teaser */}
          <div className="lp-hero-search">
            <div className="lp-search-field">
              <span className="lp-search-icon">📍</span>
              <span className="lp-search-placeholder">From City</span>
            </div>
            <div className="lp-search-divider">→</div>
            <div className="lp-search-field">
              <span className="lp-search-icon">📍</span>
              <span className="lp-search-placeholder">To City</span>
            </div>
            <Link to="/signup" className="lp-search-btn">Search Buses</Link>
          </div>

          <div className="lp-hero-ctas">
            <Link to="/signup" className="lp-cta-primary">
              Start Your Journey →
            </Link>
            <Link to="/login" className="lp-cta-secondary">
              Already have an account? Sign In
            </Link>
          </div>

          {/* Trust pills */}
          <div className="lp-trust-pills">
            {['✅ Instant Confirmation', '📱 Digital Tickets', '💳 Secure Payments', '🔁 Easy Cancellations'].map(p => (
              <span key={p} className="lp-trust-pill">{p}</span>
            ))}
          </div>
        </div>

        {/* Hero visual */}
        <div className="lp-hero-visual">
          <div className="lp-ticket-card lp-ticket-card--1">
            <div className="lp-tc-header">
              <Logo size={24} />
              <span style={{ fontWeight: 700, color: '#1E63FF', fontSize: '0.9rem' }}>Tripzo</span>
              <span className="lp-tc-badge">Confirmed ✓</span>
            </div>
            <div className="lp-tc-route">
              <span className="lp-tc-city">Chennai</span>
              <span className="lp-tc-arrow">──────→</span>
              <span className="lp-tc-city">Bangalore</span>
            </div>
            <div className="lp-tc-meta">
              <span>🗓 15 Apr 2026</span>
              <span>🚌 Seat 12A</span>
              <span style={{ fontWeight: 700, color: '#1E63FF' }}>₹580</span>
            </div>
          </div>

          <div className="lp-ticket-card lp-ticket-card--2">
            <div className="lp-tc-header">
              <Logo size={24} />
              <span style={{ fontWeight: 700, color: '#1E63FF', fontSize: '0.9rem' }}>Tripzo</span>
              <span className="lp-tc-badge lp-tc-badge--green">Active ●</span>
            </div>
            <div className="lp-tc-route">
              <span className="lp-tc-city">Mumbai</span>
              <span className="lp-tc-arrow">──────→</span>
              <span className="lp-tc-city">Pune</span>
            </div>
            <div className="lp-tc-meta">
              <span>🗓 20 Apr 2026</span>
              <span>🚌 Seat 5B</span>
              <span style={{ fontWeight: 700, color: '#1E63FF' }}>₹320</span>
            </div>
          </div>

          {/* Floating decorative elements */}
          <div className="lp-float-badge lp-float-badge--top">
            ⭐ 4.8 Rating
          </div>
          <div className="lp-float-badge lp-float-badge--bottom">
            🚌 50,000+ Happy Travellers
          </div>
        </div>
      </section>

      {/* ══════════════════════════════
            FEATURES
         ══════════════════════════════ */}
      <section className="lp-section lp-features" id="features">
        <div className="lp-section-header">
          <span className="lp-section-label">Why Tripzo?</span>
          <h2 className="lp-section-title">Everything you need for<br />a perfect journey</h2>
        </div>

        <div className="lp-features-grid">
          {[
            {
              icon: '🔍',
              color: '#E8F0FF',
              accent: '#1E63FF',
              title: 'Smart Search',
              desc: 'Find the best bus routes instantly across 120+ cities with real-time availability.',
            },
            {
              icon: '💺',
              color: '#DCFCE7',
              accent: '#22C55E',
              title: 'Choose Your Seat',
              desc: 'Interactive seat map lets you pick window, aisle, sleeper or seater — your call.',
            },
            {
              icon: '💳',
              color: '#FEF3C7',
              accent: '#F59E0B',
              title: 'Secure Payments',
              desc: 'Multiple payment options with bank-grade encryption to keep your money safe.',
            },
            {
              icon: '📲',
              color: '#FCE7F3',
              accent: '#EC4899',
              title: 'Digital Tickets',
              desc: 'Get your e-ticket instantly. No printing needed — show your phone and board.',
            },
            {
              icon: '🔔',
              color: '#EDE9FE',
              accent: '#7C3AED',
              title: 'Live Updates',
              desc: 'Real-time notifications for departure alerts, booking confirmations and more.',
            },
            {
              icon: '↩️',
              color: '#FEE2E2',
              accent: '#EF4444',
              title: 'Easy Cancellations',
              desc: 'Cancel with a tap and get your refund processed quickly — no hassle.',
            },
          ].map(f => (
            <div key={f.title} className="lp-feature-card">
              <div className="lp-feature-icon" style={{ background: f.color }}>
                <span style={{ fontSize: '1.8rem' }}>{f.icon}</span>
              </div>
              <h3 className="lp-feature-title" style={{ color: f.accent }}>{f.title}</h3>
              <p className="lp-feature-desc">{f.desc}</p>
            </div>
          ))}
        </div>
      </section>

      {/* ══════════════════════════════
            HOW IT WORKS
         ══════════════════════════════ */}
      <section className="lp-section lp-how" id="how-it-works">
        <div className="lp-how-inner">
          <div className="lp-section-header" style={{ textAlign: 'left' }}>
            <span className="lp-section-label">Simple Process</span>
            <h2 className="lp-section-title">Book your ticket in<br />3 easy steps</h2>
            <p className="lp-section-sub">No complicated forms, no long queues. Just you, your destination, and a few taps.</p>
            <Link to="/signup" className="lp-cta-primary" style={{ marginTop: '1.5rem', display: 'inline-flex' }}>
              Book Now →
            </Link>
          </div>

          <div className="lp-steps">
            {[
              { num: '01', icon: '🔍', title: 'Search Routes', desc: 'Enter your origin, destination and travel date to see all available buses.' },
              { num: '02', icon: '💺', title: 'Select Your Seat', desc: 'Browse the live seat map and choose exactly where you want to sit.' },
              { num: '03', icon: '✅', title: 'Pay & Confirm', desc: 'Complete secure payment and receive your digital ticket instantly.' },
            ].map((s, i) => (
              <div key={s.num} className="lp-step">
                <div className="lp-step-num">{s.num}</div>
                <div className="lp-step-icon">{s.icon}</div>
                <h3 className="lp-step-title">{s.title}</h3>
                <p className="lp-step-desc">{s.desc}</p>
                {i < 2 && <div className="lp-step-connector" />}
              </div>
            ))}
          </div>
        </div>
      </section>

      {/* ══════════════════════════════
            STATS STRIP
         ══════════════════════════════ */}
      <section className="lp-stats" id="stats">
        <div className="lp-stats-inner">
          {[
            { value: counts.routes, suffix: '+', label: 'Active Routes' },
            { value: counts.passengers.toLocaleString(), suffix: '+', label: 'Happy Travellers' },
            { value: counts.cities, suffix: '+', label: 'Cities Covered' },
            { value: (counts.rating / 10).toFixed(1), suffix: '★', label: 'Average Rating' },
          ].map(s => (
            <div key={s.label} className="lp-stat-item">
              <div className="lp-stat-value">{s.value}{s.suffix}</div>
              <div className="lp-stat-label">{s.label}</div>
            </div>
          ))}
        </div>
      </section>

      {/* ══════════════════════════════
            CTA BANNER
         ══════════════════════════════ */}
      <section className="lp-cta-banner">
        <div className="lp-blob lp-blob-cta-1" />
        <div className="lp-blob lp-blob-cta-2" />
        <div className="lp-cta-banner-inner">
          <h2 className="lp-cta-banner-title">Ready to hit the road?</h2>
          <p className="lp-cta-banner-sub">Join thousands of travellers who book smarter with Tripzo.</p>
          <div className="lp-cta-banner-btns">
            <Link to="/signup" className="lp-btn-white">Create Free Account</Link>
            <Link to="/login" className="lp-btn-outline-white">Sign In</Link>
          </div>
        </div>
      </section>

      {/* ══════════════════════════════
            FOOTER
         ══════════════════════════════ */}
      <footer className="lp-footer">
        <div className="lp-footer-inner">
          <div className="lp-footer-brand">
            <div className="lp-footer-logo">
              <Logo size={36} />
              <span className="lp-brand-text lp-brand-text--footer">Tripzo</span>
            </div>
            <p className="lp-footer-tagline">India's smartest bus booking platform. Travel smarter, not harder.</p>
          </div>

          <div className="lp-footer-links-group">
            <div className="lp-footer-col">
              <h4 className="lp-footer-col-title">Platform</h4>
              <Link to="/signup" className="lp-footer-link">Get Started</Link>
              <Link to="/login" className="lp-footer-link">Sign In</Link>
              <a href="#features" className="lp-footer-link">Features</a>
              <a href="#how-it-works" className="lp-footer-link">How it Works</a>
            </div>
            <div className="lp-footer-col">
              <h4 className="lp-footer-col-title">Company</h4>
              <a href="#stats" className="lp-footer-link">About</a>
              <span className="lp-footer-link" style={{ cursor: 'default' }}>Careers</span>
              <span className="lp-footer-link" style={{ cursor: 'default' }}>Blog</span>
              <span className="lp-footer-link" style={{ cursor: 'default' }}>Contact</span>
            </div>
            <div className="lp-footer-col">
              <h4 className="lp-footer-col-title">Support</h4>
              <span className="lp-footer-link" style={{ cursor: 'default' }}>Help Center</span>
              <span className="lp-footer-link" style={{ cursor: 'default' }}>Privacy Policy</span>
              <span className="lp-footer-link" style={{ cursor: 'default' }}>Terms of Service</span>
              <span className="lp-footer-link" style={{ cursor: 'default' }}>Refund Policy</span>
            </div>
          </div>
        </div>

        <div className="lp-footer-bottom">
          <span>© 2026 Tripzo. All rights reserved.</span>
          <span>Made with ❤️ for Indian travellers</span>
        </div>
      </footer>
    </div>
  );
};

export default LandingPage;
