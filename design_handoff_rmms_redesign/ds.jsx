/* ds.jsx — RMMS design system: icons + shared UI primitives */
const { useState, useEffect, useRef } = React;

/* ───────────────────────── Icons (stroke, rounded) ───────────────────────── */
function Icon({ name, size = 24, color = 'currentColor', stroke = 2, style }) {
  const p = { fill: 'none', stroke: color, strokeWidth: stroke, strokeLinecap: 'round', strokeLinejoin: 'round' };
  const paths = {
    home: <><path {...p} d="M3 10.5 12 3l9 7.5"/><path {...p} d="M5 9.5V20h14V9.5"/><path {...p} d="M9.5 20v-5h5v5"/></>,
    calendar: <><rect {...p} x="3" y="4.5" width="18" height="16.5" rx="3.5"/><path {...p} d="M3 9h18M8 2.5v4M16 2.5v4"/></>,
    check: <path {...p} d="M4 12.5 9.5 18 20 6.5"/>,
    checkCircle: <><circle {...p} cx="12" cy="12" r="9"/><path {...p} d="M8 12.2 11 15l5-6"/></>,
    clock: <><circle {...p} cx="12" cy="12" r="9"/><path {...p} d="M12 7.5V12l3 2"/></>,
    history: <><path {...p} d="M3.2 12a8.8 8.8 0 1 0 2.6-6.3"/><path {...p} d="M5 3v4h4"/><path {...p} d="M12 7.5V12l3 2"/></>,
    face: <><circle {...p} cx="12" cy="12" r="9"/><circle cx="9" cy="10.5" r="1.1" fill={color}/><circle cx="15" cy="10.5" r="1.1" fill={color}/><path {...p} d="M8.5 14.5c1 1.3 5 1.3 7 0"/></>,
    doc: <><path {...p} d="M6 3h8l4 4v14H6z"/><path {...p} d="M14 3v4h4M9 13h6M9 16.5h6"/></>,
    store: <><path {...p} d="M4 9.5 5 4h14l1 5.5"/><path {...p} d="M4 9.5h16v0a3 3 0 0 1-6 0 3 3 0 0 1-6 0 3 3 0 0 1-4 0Z"/><path {...p} d="M5 12v8h14v-8"/><path {...p} d="M10 20v-5h4v5"/></>,
    users: <><circle {...p} cx="9" cy="8" r="3.2"/><path {...p} d="M3.5 19c.6-3 2.9-4.6 5.5-4.6S14 16 14.5 19"/><path {...p} d="M16 5.2a3.2 3.2 0 0 1 0 6M18 14.6c2 .5 3.3 1.9 3.6 4.2"/></>,
    user: <><circle {...p} cx="12" cy="8" r="3.6"/><path {...p} d="M4.5 20c.7-3.6 3.6-5.6 7.5-5.6s6.8 2 7.5 5.6"/></>,
    logout: <><path {...p} d="M14 4h4a1 1 0 0 1 1 1v14a1 1 0 0 1-1 1h-4"/><path {...p} d="M9 8l-4 4 4 4M5 12h10"/></>,
    chevR: <path {...p} d="M9 5l7 7-7 7"/>,
    chevL: <path {...p} d="M15 5l-7 7 7 7"/>,
    chevDown: <path {...p} d="M5 9l7 7 7-7"/>,
    plus: <path {...p} d="M12 5v14M5 12h14"/>,
    edit: <><path {...p} d="M4 20h4L18.5 9.5a2 2 0 0 0-3-3L5 17v3z"/><path {...p} d="M13.5 7 16 9.5"/></>,
    send: <path {...p} d="M4 12 20 4l-6 16-3.5-6.5L4 12z"/>,
    undo: <><path {...p} d="M9 7 5 11l4 4"/><path {...p} d="M5 11h9a5 5 0 0 1 0 10h-3"/></>,
    umbrella: <><path {...p} d="M12 3v2M3.5 11a8.5 8.5 0 0 1 17 0Z"/><path {...p} d="M12 11v7a2.5 2.5 0 0 1-5 0"/></>,
    otClock: <><circle {...p} cx="12" cy="13" r="8"/><path {...p} d="M12 9.5V13l2.5 1.5M9 3h6M12 3v2.5"/></>,
    bell: <><path {...p} d="M6 9a6 6 0 0 1 12 0c0 5 2 6 2 6H4s2-1 2-6Z"/><path {...p} d="M10 20a2 2 0 0 0 4 0"/></>,
    mail: <><rect {...p} x="3" y="5" width="18" height="14" rx="3"/><path {...p} d="m4 7 8 6 8-6"/></>,
    lock: <><rect {...p} x="4.5" y="10" width="15" height="10.5" rx="3"/><path {...p} d="M8 10V7a4 4 0 0 1 8 0v3"/></>,
    eye: <><path {...p} d="M2.5 12S6 5.5 12 5.5 21.5 12 21.5 12 18 18.5 12 18.5 2.5 12 2.5 12Z"/><circle {...p} cx="12" cy="12" r="2.8"/></>,
    pin: <><path {...p} d="M12 21s7-6.3 7-11a7 7 0 1 0-14 0c0 4.7 7 11 7 11Z"/><circle {...p} cx="12" cy="10" r="2.5"/></>,
    sparkle: <path {...p} d="M12 3l1.8 5.2L19 10l-5.2 1.8L12 17l-1.8-5.2L5 10l5.2-1.8L12 3z"/>,
    flame: <path {...p} d="M12 3s5 4 5 9a5 5 0 0 1-10 0c0-2 1-3 1-3s.5 2 2 2c1 0 1.5-1 1-3-.5-2 1-5 1-5Z"/>,
    arrowUpR: <path {...p} d="M7 17 17 7M9 7h8v8"/>,
  };
  return (
    <svg width={size} height={size} viewBox="0 0 24 24" style={style} aria-hidden="true">
      {paths[name] || null}
    </svg>
  );
}

/* ───────────────────────── Status bar (app, dark glyphs) ───────────────────────── */
function StatusBar({ light = false }) {
  const c = light ? '#fff' : '#161528';
  return (
    <div style={{
      height: 54, flexShrink: 0, display: 'flex', alignItems: 'flex-end', justifyContent: 'space-between',
      padding: '0 30px 8px', position: 'relative', zIndex: 30,
    }}>
      <span style={{ fontWeight: 700, fontSize: 16, color: c, letterSpacing: 0.2 }}>9:41</span>
      <div style={{ display: 'flex', gap: 7, alignItems: 'center' }}>
        <svg width="18" height="12" viewBox="0 0 18 12"><g fill={c}>
          <rect x="0" y="7" width="3" height="5" rx="0.6"/><rect x="4.5" y="4.5" width="3" height="7.5" rx="0.6"/>
          <rect x="9" y="2" width="3" height="10" rx="0.6"/><rect x="13.5" y="0" width="3" height="12" rx="0.6"/>
        </g></svg>
        <svg width="16" height="12" viewBox="0 0 17 12" fill={c}><path d="M8.5 3.2C10.8 3.2 12.9 4.1 14.4 5.6L15.5 4.5C13.7 2.7 11.2 1.5 8.5 1.5 5.8 1.5 3.3 2.7 1.5 4.5L2.6 5.6C4.1 4.1 6.2 3.2 8.5 3.2Z"/><path d="M8.5 6.8C9.9 6.8 11.1 7.3 12 8.2L13.1 7.1C11.8 5.9 10.2 5.1 8.5 5.1 6.8 5.1 5.2 5.9 3.9 7.1L5 8.2C5.9 7.3 7.1 6.8 8.5 6.8Z"/><circle cx="8.5" cy="10.5" r="1.5"/></svg>
        <svg width="25" height="12" viewBox="0 0 25 12"><rect x="0.5" y="0.5" width="21" height="11" rx="3" stroke={c} strokeOpacity="0.35" fill="none"/><rect x="2" y="2" width="18" height="8" rx="1.8" fill={c}/><path d="M23 4v4c.8-.3 1.3-1 1.3-2S23.8 4.3 23 4Z" fill={c} fillOpacity="0.5"/></svg>
      </div>
    </div>
  );
}

/* ───────────────────────── Top bar (sub-screens) ───────────────────────── */
function TopBar({ title, onBack, light = false, trailing, large = false }) {
  const c = light ? '#fff' : 'var(--ink)';
  return (
    <div style={{ flexShrink: 0, padding: light ? '0 16px 4px' : '0 16px 8px', position: 'relative', zIndex: 20 }}>
      <div style={{ display: 'flex', alignItems: 'center', gap: 8, minHeight: 44 }}>
        {onBack && (
          <button onClick={onBack} className="press" aria-label="Quay lại" style={{
            width: 40, height: 40, borderRadius: 13, border: 'none', flexShrink: 0,
            background: light ? 'rgba(255,255,255,0.16)' : 'var(--surface)',
            boxShadow: light ? 'none' : 'var(--shadow-sm)',
            display: 'grid', placeItems: 'center', color: c,
          }}>
            <Icon name="chevL" size={22} stroke={2.4} />
          </button>
        )}
        {!large && <div style={{ fontSize: 19, fontWeight: 700, color: c, letterSpacing: -0.3, flex: 1 }}>{title}</div>}
        <div style={{ marginLeft: 'auto', display: 'flex', gap: 8 }}>{trailing}</div>
      </div>
      {large && <div style={{ fontSize: 30, fontWeight: 800, color: c, letterSpacing: -0.8, marginTop: 6 }} className="display">{title}</div>}
    </div>
  );
}

/* ───────────────────────── Chip / status pill ───────────────────────── */
function Chip({ children, tone = 'neutral', icon, solid = false }) {
  const tones = {
    neutral: ['#F1F1F8', '#6E6D87'],
    indigo: ['#EEEEFF', '#4338CA'],
    emerald: ['#E7FBF2', '#059669'],
    amber: ['#FFF6E5', '#B45309'],
    rose: ['#FFEEF1', '#E11D48'],
    sky: ['#E6F5FE', '#0284C7'],
  };
  const [bg, fg] = tones[tone] || tones.neutral;
  return (
    <span style={{
      display: 'inline-flex', alignItems: 'center', gap: 5, height: 28, padding: '0 11px',
      borderRadius: 999, fontSize: 12.5, fontWeight: 700, letterSpacing: 0.1,
      background: solid ? fg : bg, color: solid ? '#fff' : fg, whiteSpace: 'nowrap',
    }}>
      {icon && <Icon name={icon} size={14} stroke={2.4} />}
      {children}
    </span>
  );
}

/* ───────────────────────── Card ───────────────────────── */
function Card({ children, style, onClick, pad = 18, className = '' }) {
  return (
    <div onClick={onClick} className={(onClick ? 'press ' : '') + className} style={{
      background: 'var(--surface)', borderRadius: 'var(--r-lg)', boxShadow: 'var(--shadow-sm)',
      padding: pad, ...style,
    }}>{children}</div>
  );
}

/* ───────────────────────── Soft icon tile ───────────────────────── */
function IconTile({ name, tone = 'indigo', size = 46, r = 15 }) {
  const tones = {
    indigo: ['#EEEEFF', '#5B5BF0'],
    violet: ['#F3ECFF', '#8B5CF6'],
    emerald: ['#E7FBF2', '#059669'],
    amber: ['#FFF4E0', '#EA9009'],
    sky: ['#E4F4FE', '#0EA5E9'],
    rose: ['#FFECF0', '#F43F5E'],
  };
  const [bg, fg] = tones[tone] || tones.indigo;
  return (
    <div style={{ width: size, height: size, borderRadius: r, background: bg, display: 'grid', placeItems: 'center', flexShrink: 0 }}>
      <Icon name={name} size={size * 0.5} color={fg} stroke={2.1} />
    </div>
  );
}

/* ───────────────────────── Primary button ───────────────────────── */
function Button({ children, onClick, variant = 'primary', icon, full = false, style, disabled }) {
  const base = {
    height: 54, borderRadius: 17, border: 'none', fontFamily: 'inherit', fontSize: 16, fontWeight: 700,
    display: 'inline-flex', alignItems: 'center', justifyContent: 'center', gap: 9, whiteSpace: 'nowrap',
    width: full ? '100%' : undefined, letterSpacing: 0.1, padding: '0 22px',
    opacity: disabled ? 0.5 : 1, pointerEvents: disabled ? 'none' : 'auto',
  };
  const variants = {
    primary: { background: 'var(--grad-brand)', color: '#fff', boxShadow: 'var(--shadow-brand)' },
    emerald: { background: 'var(--grad-emerald)', color: '#fff', boxShadow: '0 10px 28px rgba(16,185,129,0.38)' },
    soft: { background: 'var(--surface-2)', color: 'var(--indigo-deep)', boxShadow: 'none' },
    ghost: { background: 'transparent', color: 'var(--muted)', boxShadow: 'none' },
  };
  return (
    <button onClick={onClick} className="press" disabled={disabled} style={{ ...base, ...variants[variant], ...style }}>
      {icon && <Icon name={icon} size={20} stroke={2.3} />}
      {children}
    </button>
  );
}

/* ───────────────────────── Bottom sheet ───────────────────────── */
function Sheet({ open, onClose, children, title }) {
  if (!open) return null;
  return (
    <div onClick={onClose} style={{
      position: 'absolute', inset: 0, zIndex: 80, background: 'rgba(20,19,40,0.4)',
      backdropFilter: 'blur(2px)', display: 'flex', flexDirection: 'column', justifyContent: 'flex-end',
      animation: 'fadeIn .2s ease',
    }}>
      <div onClick={e => e.stopPropagation()} style={{
        background: 'var(--surface)', borderRadius: '30px 30px 0 0', padding: '10px 16px 30px',
        animation: 'sheetUp .34s cubic-bezier(.2,.85,.25,1)', boxShadow: '0 -10px 40px rgba(0,0,0,0.16)',
      }}>
        <div style={{ width: 40, height: 5, borderRadius: 99, background: 'var(--line)', margin: '0 auto 16px' }} />
        {title && <div style={{ fontSize: 18, fontWeight: 800, padding: '0 4px 12px', letterSpacing: -0.3 }}>{title}</div>}
        {children}
      </div>
    </div>
  );
}

/* ───────────────────────── Toast ───────────────────────── */
function useToast() {
  const [toast, setToast] = useState(null);
  const show = (msg, tone = 'emerald') => { setToast({ msg, tone }); setTimeout(() => setToast(null), 1900); };
  const node = toast ? (
    <div style={{
      position: 'absolute', left: 16, right: 16, bottom: 104, zIndex: 95,
      background: 'var(--ink)', color: '#fff', borderRadius: 16, padding: '14px 18px',
      display: 'flex', alignItems: 'center', gap: 10, fontSize: 14.5, fontWeight: 600,
      boxShadow: '0 14px 40px rgba(0,0,0,0.3)', animation: 'fadeUp .3s cubic-bezier(.2,.8,.2,1)',
    }}>
      <Icon name={toast.tone === 'emerald' ? 'checkCircle' : 'bell'} size={20} color={toast.tone === 'emerald' ? '#34D399' : '#FBBF24'} />
      {toast.msg}
    </div>
  ) : null;
  return { show, node };
}

Object.assign(window, { Icon, StatusBar, TopBar, Chip, Card, IconTile, Button, Sheet, useToast });
