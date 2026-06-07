/* screens-main.jsx — Home, Profile, Assignment, Attendance */
const { useState: useStateM, useEffect: useEffectM } = React;

/* live HH:MM:SS clock */
function useClock() {
  const [t, setT] = useStateM(new Date());
  useEffectM(() => { const id = setInterval(() => setT(new Date()), 1000); return () => clearInterval(id); }, []);
  const pad = n => String(n).padStart(2, '0');
  return { hms: `${pad(t.getHours())}:${pad(t.getMinutes())}:${pad(t.getSeconds())}`, hm: `${pad(t.getHours())}:${pad(t.getMinutes())}` };
}

/* ════════════════════════ HOME ════════════════════════ */
function HomeScreen({ nav, toast, checkedIn, setCheckedIn }) {
  const { hms } = useClock();
  const quick = [
    { k: 'assign', icon: 'users', tone: 'indigo', title: 'Phân công', sub: 'Leader & điểm bán' },
    { k: 'history', icon: 'history', tone: 'emerald', title: 'Lịch sử', sub: 'Chấm công đã qua' },
    { k: 'face', icon: 'face', tone: 'amber', title: 'Khuôn mặt', sub: 'Đăng ký & quản lý' },
  ];
  return (
    <div className="scroll" style={{ paddingBottom: 120 }}>
      {/* hero */}
      <div style={{ padding: '4px 16px 0' }}>
        <div style={{
          position: 'relative', borderRadius: 30, overflow: 'hidden',
          background: 'var(--grad-mesh)', boxShadow: 'var(--shadow-lg)', padding: 22,
        }}>
          <div style={{ position: 'absolute', top: -40, right: -30, width: 160, height: 160, borderRadius: '50%', background: 'rgba(255,255,255,0.12)' }} />
          <div style={{ position: 'absolute', bottom: -50, left: -20, width: 120, height: 120, borderRadius: '50%', background: 'rgba(255,255,255,0.08)' }} />
          <div style={{ position: 'relative', display: 'flex', alignItems: 'center', gap: 14 }}>
            <div style={{
              width: 56, height: 56, borderRadius: 18, flexShrink: 0,
              background: 'rgba(255,255,255,0.22)', border: '1.5px solid rgba(255,255,255,0.4)',
              display: 'grid', placeItems: 'center', fontSize: 22, fontWeight: 800, color: '#fff',
            }}>P</div>
            <div style={{ flex: 1, minWidth: 0 }}>
              <div style={{ color: 'rgba(255,255,255,0.85)', fontSize: 13, fontWeight: 600 }}>Chào buổi sáng 👋</div>
              <div style={{ color: '#fff', fontSize: 22, fontWeight: 800, letterSpacing: -0.4, lineHeight: 1.1 }} className="display">PG 01</div>
            </div>
            <button onClick={() => nav('login')} className="press" aria-label="Đăng xuất" style={{
              width: 42, height: 42, borderRadius: 13, border: 'none', background: 'rgba(255,255,255,0.18)',
              display: 'grid', placeItems: 'center', color: '#fff',
            }}><Icon name="logout" size={20} stroke={2.2} /></button>
          </div>
          <div style={{ position: 'relative', display: 'flex', gap: 8, marginTop: 18 }}>
            <span style={{ display: 'inline-flex', alignItems: 'center', gap: 6, height: 30, padding: '0 12px', borderRadius: 999, background: 'rgba(255,255,255,0.2)', color: '#fff', fontSize: 12.5, fontWeight: 700 }}>
              <Icon name="store" size={14} stroke={2.4} /> Vai trò · PG
            </span>
            <span style={{ display: 'inline-flex', alignItems: 'center', gap: 6, height: 30, padding: '0 12px', borderRadius: 999, background: 'rgba(255,255,255,0.2)', color: '#fff', fontSize: 12.5, fontWeight: 700 }}>
              <Icon name="pin" size={14} stroke={2.4} /> ST-001
            </span>
          </div>
        </div>
      </div>

      {/* attendance hero card */}
      <div style={{ padding: '16px 16px 0' }}>
        <div className="press" onClick={() => nav('attendance')} style={{
          borderRadius: 26, padding: 18, background: 'var(--surface)', boxShadow: 'var(--shadow)',
          display: 'flex', alignItems: 'center', gap: 16,
        }}>
          <div style={{ position: 'relative', width: 62, height: 62, flexShrink: 0 }}>
            {!checkedIn && <div style={{ position: 'absolute', inset: 0, borderRadius: '50%', background: 'var(--indigo)', animation: 'pulseRing 2s ease-out infinite' }} />}
            <div style={{ position: 'relative', width: 62, height: 62, borderRadius: '50%', background: checkedIn ? 'var(--grad-emerald)' : 'var(--grad-brand)', display: 'grid', placeItems: 'center', boxShadow: checkedIn ? '0 8px 20px rgba(16,185,129,.4)' : 'var(--shadow-brand)' }}>
              <Icon name={checkedIn ? 'checkCircle' : 'face'} size={30} color="#fff" stroke={2.1} />
            </div>
          </div>
          <div style={{ flex: 1, minWidth: 0 }}>
            <div style={{ fontSize: 11.5, color: 'var(--muted)', fontWeight: 800, letterSpacing: 0.8 }}>CA HÔM NAY · 08:00–17:00</div>
            <div style={{ fontSize: 19, fontWeight: 800, letterSpacing: -0.4, marginTop: 2 }}>{checkedIn ? 'Đang trong ca' : 'Chưa chấm công'}</div>
            <div style={{ fontSize: 21, fontWeight: 700, color: 'var(--indigo-deep)', marginTop: 2, fontFamily: 'Space Grotesk' }} className="tnum">{hms}</div>
          </div>
          <div style={{ display: 'flex', flexDirection: 'column', alignItems: 'center', gap: 6 }}>
            <div style={{ width: 38, height: 38, borderRadius: 12, background: checkedIn ? 'var(--emerald-soft)' : 'var(--surface-2)', display: 'grid', placeItems: 'center', color: checkedIn ? 'var(--emerald)' : 'var(--indigo)' }}>
              <Icon name="chevR" size={20} stroke={2.6} />
            </div>
          </div>
        </div>
      </div>

      {/* quick access — chỉ các mục KHÔNG có trên thanh điều hướng */}
      <div style={{ padding: '24px 22px 10px', display: 'flex', alignItems: 'center', justifyContent: 'space-between' }}>
        <span style={{ fontSize: 12.5, fontWeight: 800, color: 'var(--muted)', letterSpacing: 1.2 }}>TRUY CẬP NHANH</span>
        <Icon name="sparkle" size={16} color="var(--violet)" />
      </div>
      <div className="stagger" style={{ padding: '0 16px', display: 'grid', gridTemplateColumns: '1fr 1fr 1fr', gap: 12 }}>
        {quick.map(q => (
          <div key={q.k} className="press" onClick={() => nav(q.k)} style={{
            background: 'var(--surface)', borderRadius: 22, padding: '18px 10px', boxShadow: 'var(--shadow-sm)',
            display: 'flex', flexDirection: 'column', alignItems: 'center', justifyContent: 'center', textAlign: 'center', gap: 10, minHeight: 110,
          }}>
            <IconTile name={q.icon} tone={q.tone} size={48} r={16} />
            <div style={{ fontSize: 13.5, fontWeight: 800, letterSpacing: -0.2, lineHeight: 1.15 }}>{q.title}</div>
          </div>
        ))}
      </div>
    </div>
  );
}

/* ════════════════════════ ATTENDANCE (chấm công) ════════════════════════ */
function AttendanceScreen({ nav, toast, checkedIn, setCheckedIn }) {
  const { hms } = useClock();
  const [busy, setBusy] = useStateM(false);
  const act = () => {
    setBusy(true);
    setTimeout(() => { setBusy(false); const next = !checkedIn; setCheckedIn(next); toast(next ? 'Đã check-in lúc ' + hms.slice(0,5) : 'Đã check-out. Chào ca mới!'); }, 1400);
  };
  return (
    <div className="screen" style={{ background: 'var(--bg)' }}>
      <TopBar title="Chấm công" onBack={() => nav('back')} />
      <div className="scroll" style={{ padding: '8px 16px 40px', display: 'flex', flexDirection: 'column', alignItems: 'center' }}>
        {/* store context */}
        <div style={{ display: 'flex', gap: 8, marginBottom: 20 }}>
          <Chip tone="indigo" icon="store">ST-001 · Quận 1</Chip>
          <Chip tone="emerald" icon="pin">Trong khu vực</Chip>
        </div>
        {/* face circle */}
        <div style={{ position: 'relative', width: 230, height: 230, display: 'grid', placeItems: 'center', margin: '14px 0 28px' }}>
          <div style={{ position: 'absolute', inset: 0, borderRadius: '50%', background: checkedIn ? 'var(--grad-emerald)' : 'var(--grad-mesh)', opacity: 0.16 }} />
          <div style={{ position: 'absolute', inset: 22, borderRadius: '50%', border: '2px dashed', borderColor: checkedIn ? 'var(--emerald)' : 'var(--indigo-bright)', animation: busy ? 'spin 3s linear infinite' : 'none' }} />
          <div style={{ width: 150, height: 150, borderRadius: '50%', background: checkedIn ? 'var(--grad-emerald)' : 'var(--grad-brand)', display: 'grid', placeItems: 'center', boxShadow: 'var(--shadow-lg)' }}>
            <Icon name={checkedIn ? 'checkCircle' : 'face'} size={70} color="#fff" stroke={1.8} />
          </div>
        </div>
        <div style={{ fontSize: 44, fontWeight: 700, letterSpacing: -1, fontFamily: 'Space Grotesk' }} className="tnum">{hms}</div>
        <div style={{ fontSize: 14, color: 'var(--muted)', fontWeight: 600, marginTop: 4 }}>CN, 7 tháng 6, 2026</div>

        {/* in/out timeline */}
        <div style={{ display: 'flex', gap: 12, width: '100%', marginTop: 26 }}>
          <Card pad={16} style={{ flex: 1, textAlign: 'center' }}>
            <Icon name="arrowUpR" size={20} color="var(--emerald)" style={{ transform: 'rotate(135deg)' }} />
            <div style={{ fontSize: 12, color: 'var(--muted)', fontWeight: 700, marginTop: 4 }}>VÀO CA</div>
            <div style={{ fontSize: 20, fontWeight: 800, marginTop: 2 }} className="tnum">{checkedIn ? hms.slice(0,5) : '--:--'}</div>
          </Card>
          <Card pad={16} style={{ flex: 1, textAlign: 'center' }}>
            <Icon name="arrowUpR" size={20} color="var(--rose)" style={{ transform: 'rotate(-45deg)' }} />
            <div style={{ fontSize: 12, color: 'var(--muted)', fontWeight: 700, marginTop: 4 }}>RA CA</div>
            <div style={{ fontSize: 20, fontWeight: 800, marginTop: 2 }} className="tnum">--:--</div>
          </Card>
        </div>

        <div style={{ width: '100%', marginTop: 28 }}>
          <Button full variant={checkedIn ? 'emerald' : 'primary'} icon={busy ? null : (checkedIn ? 'arrowUpR' : 'face')} onClick={act} disabled={busy}>
            {busy ? <span style={{ width: 22, height: 22, borderRadius: '50%', border: '2.5px solid rgba(255,255,255,.4)', borderTopColor: '#fff', animation: 'spin .7s linear infinite' }} /> : (checkedIn ? 'Check-out ngay' : 'Quét khuôn mặt & Check-in')}
          </Button>
        </div>
      </div>
    </div>
  );
}

/* ════════════════════════ PROFILE (Hồ sơ) ════════════════════════ */
function ProfileScreen({ nav }) {
  const rows = [
    { icon: 'bell', tone: 'violet', title: 'Thông báo', sub: 'Nhắc ca & duyệt đơn', go: null },
    { icon: 'lock', tone: 'indigo', title: 'Đổi mật khẩu', sub: 'Bảo mật tài khoản', go: null },
    { icon: 'doc', tone: 'sky', title: 'Trợ giúp & hỗ trợ', sub: 'Câu hỏi thường gặp', go: null },
  ];
  return (
    <div className="scroll" style={{ paddingBottom: 120 }}>
      <div style={{ padding: '6px 16px 0' }}>
        <div style={{ borderRadius: 30, background: 'var(--grad-mesh)', padding: '26px 20px', boxShadow: 'var(--shadow-lg)', textAlign: 'center', position: 'relative', overflow: 'hidden' }}>
          <div style={{ position: 'absolute', top: -30, right: -20, width: 130, height: 130, borderRadius: '50%', background: 'rgba(255,255,255,0.1)' }} />
          <div style={{ width: 78, height: 78, borderRadius: 26, margin: '0 auto', background: 'rgba(255,255,255,0.22)', border: '1.5px solid rgba(255,255,255,0.45)', display: 'grid', placeItems: 'center', fontSize: 32, fontWeight: 800, color: '#fff' }}>P</div>
          <div style={{ color: '#fff', fontSize: 22, fontWeight: 800, marginTop: 12, letterSpacing: -0.4 }} className="display">PG 01</div>
          <div style={{ color: 'rgba(255,255,255,0.8)', fontSize: 13.5, fontWeight: 600 }}>pg01@rmms.local</div>
          <div style={{ display: 'flex', gap: 8, justifyContent: 'center', marginTop: 14 }}>
            <span style={{ height: 30, padding: '0 12px', borderRadius: 999, background: 'rgba(255,255,255,0.2)', color: '#fff', fontSize: 12.5, fontWeight: 700, display: 'inline-flex', alignItems: 'center', gap: 6 }}><Icon name="user" size={14} stroke={2.4} /> Vai trò · PG</span>
          </div>
        </div>
      </div>

      <div style={{ padding: '20px 16px 0', display: 'flex', flexDirection: 'column', gap: 10 }} className="stagger">
        {rows.map(r => (
          <div key={r.title} className={r.go ? 'press' : ''} onClick={() => r.go && nav(r.go)} style={{ background: 'var(--surface)', borderRadius: 22, padding: 14, boxShadow: 'var(--shadow-sm)', display: 'flex', alignItems: 'center', gap: 14 }}>
            <IconTile name={r.icon} tone={r.tone} size={46} r={15} />
            <div style={{ flex: 1 }}>
              <div style={{ fontSize: 15.5, fontWeight: 700, letterSpacing: -0.2 }}>{r.title}</div>
              <div style={{ fontSize: 12.5, color: 'var(--muted)', fontWeight: 500 }}>{r.sub}</div>
            </div>
            <Icon name="chevR" size={20} color="var(--faint)" stroke={2.4} />
          </div>
        ))}
      </div>

      <div style={{ padding: '18px 16px 0' }}>
        <Button full variant="soft" icon="logout" onClick={() => nav('login')} style={{ color: 'var(--rose)', background: '#FFEEF1' }}>Đăng xuất</Button>
      </div>
    </div>
  );
}

/* ════════════════════════ ASSIGNMENT (Phân công) ════════════════════════ */
function AssignmentScreen({ nav }) {
  return (
    <div className="screen" style={{ background: 'var(--bg)' }}>
      <TopBar title="Phân công của tôi" onBack={() => nav('back')} />
      <div className="scroll" style={{ padding: '8px 16px 40px' }}>
        <div className="stagger" style={{ display: 'flex', flexDirection: 'column', gap: 22 }}>
          <div>
            <div style={{ fontSize: 12.5, fontWeight: 800, color: 'var(--muted)', letterSpacing: 1, padding: '0 6px 10px' }}>LEADER QUẢN LÝ</div>
            <Card pad={16} style={{ display: 'flex', alignItems: 'center', gap: 14 }}>
              <div style={{ width: 52, height: 52, borderRadius: 17, background: 'var(--grad-brand)', display: 'grid', placeItems: 'center', color: '#fff', fontSize: 18, fontWeight: 800, boxShadow: 'var(--shadow-brand)' }}>TL</div>
              <div style={{ flex: 1 }}>
                <div style={{ fontSize: 16.5, fontWeight: 800, letterSpacing: -0.3 }}>Team Lead 1</div>
                <div style={{ fontSize: 13, color: 'var(--muted)', fontWeight: 500, marginTop: 2 }}>leader1@rmms.local</div>
              </div>
              <button className="press" style={{ width: 42, height: 42, borderRadius: 13, border: 'none', background: 'var(--emerald-soft)', display: 'grid', placeItems: 'center', color: 'var(--emerald)' }}>
                <Icon name="bell" size={20} stroke={2.2} />
              </button>
            </Card>
            <div style={{ display: 'flex', gap: 8, marginTop: 10, padding: '0 4px' }}>
              <Chip tone="neutral" icon="clock">0900 000 011</Chip>
            </div>
          </div>

          <div>
            <div style={{ fontSize: 12.5, fontWeight: 800, color: 'var(--muted)', letterSpacing: 1, padding: '0 6px 10px' }}>ĐIỂM BÁN CỦA TÔI</div>
            <Card pad={0} style={{ overflow: 'hidden' }}>
              <div style={{ height: 110, background: 'var(--grad-mesh)', position: 'relative', display: 'grid', placeItems: 'center' }}>
                <div style={{ position: 'absolute', inset: 0, opacity: 0.25, background: 'repeating-linear-gradient(45deg, rgba(255,255,255,.15) 0 2px, transparent 2px 14px)' }} />
                <Icon name="store" size={44} color="rgba(255,255,255,0.9)" stroke={1.8} />
              </div>
              <div style={{ padding: 16, display: 'flex', alignItems: 'center', gap: 14 }}>
                <div style={{ flex: 1 }}>
                  <div style={{ fontSize: 16.5, fontWeight: 800, letterSpacing: -0.3 }}>Cửa hàng Quận 1</div>
                  <div style={{ fontSize: 13, color: 'var(--muted)', fontWeight: 600, marginTop: 2 }}>Mã ST-001</div>
                </div>
                <Chip tone="emerald" icon="pin">Đang hoạt động</Chip>
              </div>
            </Card>
          </div>
        </div>
      </div>
    </div>
  );
}

Object.assign(window, { HomeScreen, AttendanceScreen, ProfileScreen, AssignmentScreen, useClock });
