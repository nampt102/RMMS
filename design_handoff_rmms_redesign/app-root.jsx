/* app.jsx — router, bottom nav, mount */
const { useState: useStateA } = React;

const TABS = [
  { k: 'home', icon: 'home', label: 'Trang chủ' },
  { k: 'schedule', icon: 'calendar', label: 'Lịch' },
  { k: 'attendance', icon: 'face', label: '', center: true },
  { k: 'requests', icon: 'doc', label: 'Đơn từ' },
  { k: 'profile', icon: 'user', label: 'Hồ sơ' },
];

function BottomNav({ active, onTab }) {
  return (
    <div style={{ position: 'absolute', left: 0, right: 0, bottom: 0, zIndex: 40, pointerEvents: 'none' }}>
      <div style={{
        margin: '0 14px 14px', height: 70, borderRadius: 26, pointerEvents: 'auto',
        background: 'rgba(255,255,255,0.82)', backdropFilter: 'blur(20px) saturate(180%)',
        WebkitBackdropFilter: 'blur(20px) saturate(180%)', boxShadow: '0 8px 30px rgba(22,21,40,0.14)',
        border: '1px solid rgba(255,255,255,0.7)', display: 'flex', alignItems: 'center', padding: '0 8px',
      }}>
        {TABS.map(t => {
          if (t.center) {
            return (
              <div key={t.k} style={{ flex: 1, display: 'grid', placeItems: 'center' }}>
                <button onClick={() => onTab(t.k)} className="press" aria-label="Chấm công" style={{
                  width: 58, height: 58, borderRadius: 20, marginTop: -26,
                  background: 'var(--grad-brand)', boxShadow: 'var(--shadow-brand)', display: 'grid', placeItems: 'center',
                  border: '4px solid var(--bg)',
                }}><Icon name="face" size={26} color="#fff" stroke={2.1} /></button>
              </div>
            );
          }
          const on = active === t.k;
          return (
            <button key={t.k} onClick={() => onTab(t.k)} className="press" style={{
              flex: 1, height: '100%', border: 'none', background: 'transparent', display: 'flex',
              flexDirection: 'column', alignItems: 'center', justifyContent: 'center', gap: 3, padding: 0,
            }}>
              <Icon name={t.icon} size={24} color={on ? 'var(--indigo)' : 'var(--faint)'} stroke={on ? 2.5 : 2.1} />
              <span style={{ fontSize: 10.5, fontWeight: 700, color: on ? 'var(--indigo)' : 'var(--faint)' }}>{t.label}</span>
            </button>
          );
        })}
      </div>
    </div>
  );
}

const TAB_KEYS = ['home', 'schedule', 'requests', 'profile'];

function App() {
  const [route, setRoute] = useStateA(() => localStorage.getItem('rmms_route') || 'login');
  const [tab, setTab] = useStateA(() => localStorage.getItem('rmms_tab') || 'home');
  const [stack, setStack] = useStateA([]);          // back stack for sub-screens
  const [checkedIn, setCheckedIn] = useStateA(false);
  const { show, node } = useToast();
  React.useEffect(() => { localStorage.setItem('rmms_route', route); localStorage.setItem('rmms_tab', tab); }, [route, tab]);

  const nav = (to) => {
    if (to === 'back') { setRoute(stack[stack.length - 1] || tab); setStack(s => s.slice(0, -1)); return; }
    if (to === 'login') { setRoute('login'); setStack([]); return; }
    if (to === 'home') { setRoute('home'); setTab('home'); setStack([]); return; }
    if (TAB_KEYS.includes(to)) { setTab(to); setRoute(to); setStack([]); return; }
    // sub-screen push
    setStack(s => [...s, route]);
    setRoute(to);
  };

  const onTab = (k) => {
    if (k === 'attendance') { setStack(s => [...s, tab]); setRoute('attendance'); return; }
    setTab(k); setRoute(k); setStack([]);
  };

  const isTabScreen = TAB_KEYS.includes(route);

  const renderScreen = () => {
    const p = { nav, toast: show, checkedIn, setCheckedIn };
    switch (route) {
      case 'login': return <LoginScreen nav={nav} />;
      case 'home': return <HomeScreen {...p} />;
      case 'schedule': return <ScheduleScreen nav={nav} toast={show} />;
      case 'requests': return <RequestsScreen nav={nav} toast={show} />;
      case 'profile': return <ProfileScreen nav={nav} />;
      case 'attendance': return <AttendanceScreen {...p} />;
      case 'register': return <RegisterScreen nav={nav} toast={show} />;
      case 'leave': return <LeaveScreen nav={nav} toast={show} type="leave" />;
      case 'ot': return <LeaveScreen nav={nav} toast={show} type="ot" />;
      case 'assign': return <AssignmentScreen nav={nav} />;
      case 'face': return <FaceScreen nav={nav} toast={show} />;
      case 'history': return <HistoryScreen nav={nav} />;
      default: return <HomeScreen {...p} />;
    }
  };

  const showNav = route !== 'login' && isTabScreen;

  return (
    <div className="stage-wrap">
      <div className="stage-caption">RMMS · Redesign 2026 — nhấn để trải nghiệm</div>
      <IOSDevice>
        <div className="app">
          <div style={{ height: 54, flexShrink: 0 }} />
          <div key={route} style={{ flex: 1, minHeight: 0, position: 'relative', overflow: 'hidden', display: 'flex', flexDirection: 'column' }}>
            {renderScreen()}
          </div>
          {node}
          {showNav && <BottomNav active={tab} onTab={onTab} />}
        </div>
      </IOSDevice>
    </div>
  );
}

ReactDOM.createRoot(document.getElementById('root')).render(<App />);
