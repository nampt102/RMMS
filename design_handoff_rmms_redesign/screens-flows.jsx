/* screens-flows.jsx — Schedule, Register, Leave, Requests, Login, Face, History */
const { useState: useStateF } = React;

/* ════════════════════════ SCHEDULE LIST (Lịch làm việc) ════════════════════════ */
function ScheduleScreen({ nav, toast }) {
  const [shifts, setShifts] = useStateF([
    { id: 1, date: 'CN, 7 thg 6, 2026', time: '08:00 – 17:00', store: 'ST-001', status: 'pending' },
    { id: 2, date: 'T2, 8 thg 6, 2026', time: '09:00 – 18:00', store: 'ST-001', status: 'approved' },
    { id: 3, date: 'T4, 10 thg 6, 2026', time: '13:00 – 21:00', store: 'ST-001', status: 'draft' },
  ]);
  const meta = {
    pending: { tone: 'amber', icon: 'clock', label: 'Chờ duyệt' },
    approved: { tone: 'emerald', icon: 'checkCircle', label: 'Đã duyệt' },
    draft: { tone: 'neutral', icon: 'edit', label: 'Nháp' },
  };
  const submit = id => { setShifts(s => s.map(x => x.id === id ? { ...x, status: 'pending' } : x)); toast('Đã gửi duyệt ca làm'); };
  return (
    <React.Fragment>
      {/* header (tab-level, no back) */}
      <div style={{ padding: '6px 16px 0', display: 'flex', alignItems: 'center', justifyContent: 'space-between', gap: 10, flexShrink: 0 }}>
        <div style={{ fontSize: 25, fontWeight: 800, letterSpacing: -0.6, whiteSpace: 'nowrap' }} className="display">Lịch làm việc</div>
        <button className="press" onClick={() => nav('register')} aria-label="Đăng ký lịch" style={{ height: 42, padding: '0 15px', borderRadius: 14, border: 'none', background: 'var(--grad-brand)', color: '#fff', fontWeight: 700, fontSize: 13.5, display: 'inline-flex', alignItems: 'center', gap: 5, boxShadow: 'var(--shadow-brand)', flexShrink: 0, whiteSpace: 'nowrap' }}>
          <Icon name="plus" size={17} stroke={2.6} /> Đăng ký
        </button>
      </div>
      {/* week summary */}
      <div style={{ padding: '16px 16px 14px', flexShrink: 0 }}>
        <div style={{ display: 'flex', gap: 10 }}>
          {[['3', 'Ca tuần này', 'indigo'], ['24h', 'Tổng giờ', 'emerald'], ['1', 'Chờ duyệt', 'amber']].map(([n, l, t]) => (
            <Card key={l} pad={14} style={{ flex: 1, textAlign: 'center' }}>
              <div style={{ fontSize: 24, fontWeight: 800, color: `var(--${t === 'amber' ? 'amber' : t === 'emerald' ? 'emerald' : 'indigo'})`, fontFamily: 'Space Grotesk' }} className="tnum">{n}</div>
              <div style={{ fontSize: 11.5, color: 'var(--muted)', fontWeight: 600, marginTop: 2 }}>{l}</div>
            </Card>
          ))}
        </div>
      </div>
      <div className="scroll stagger" style={{ padding: '0 16px 120px', display: 'flex', flexDirection: 'column', gap: 12 }}>
        {shifts.map(s => {
          const m = meta[s.status];
          return (
            <Card key={s.id} pad={0} style={{ overflow: 'hidden' }}>
              <div style={{ display: 'flex' }}>
                <div style={{ width: 5, background: `var(--${m.tone === 'neutral' ? 'faint' : m.tone})` }} />
                <div style={{ flex: 1, padding: 16 }}>
                  <div style={{ display: 'flex', alignItems: 'center', justifyContent: 'space-between', gap: 8 }}>
                    <div style={{ fontSize: 14.5, fontWeight: 800, letterSpacing: -0.3, whiteSpace: 'nowrap' }}>{s.date}</div>
                    <Chip tone={m.tone} icon={m.icon}>{m.label}</Chip>
                  </div>
                  <div style={{ display: 'flex', gap: 8, marginTop: 12 }}>
                    <Chip tone="indigo" icon="clock">{s.time}</Chip>
                    <Chip tone="neutral" icon="store">{s.store}</Chip>
                  </div>
                  {s.status !== 'approved' && (
                    <div style={{ display: 'flex', gap: 8, marginTop: 14, borderTop: '1px solid var(--line)', paddingTop: 14 }}>
                      <button className="press" onClick={() => nav('register')} style={{ flex: 1, height: 42, borderRadius: 13, border: 'none', background: 'var(--surface-2)', color: 'var(--body)', fontWeight: 700, fontSize: 14, fontFamily: 'inherit', display: 'inline-flex', alignItems: 'center', justifyContent: 'center', gap: 6, whiteSpace: 'nowrap' }}><Icon name="edit" size={16} stroke={2.2} /> Sửa</button>
                      {s.status === 'draft'
                        ? <button className="press" onClick={() => submit(s.id)} style={{ flex: 1.4, height: 42, borderRadius: 13, border: 'none', background: 'var(--grad-brand)', color: '#fff', fontWeight: 700, fontSize: 14, fontFamily: 'inherit', display: 'inline-flex', alignItems: 'center', justifyContent: 'center', gap: 6, boxShadow: 'var(--shadow-brand)' }}><Icon name="send" size={16} stroke={2.2} /> Gửi duyệt</button>
                        : <button className="press" onClick={() => { setShifts(x => x.map(i => i.id === s.id ? { ...i, status: 'draft' } : i)); toast('Đã thu hồi', 'warn'); }} style={{ flex: 1, height: 42, borderRadius: 13, border: 'none', background: '#FFEEF1', color: 'var(--rose)', fontWeight: 700, fontSize: 14, fontFamily: 'inherit', display: 'inline-flex', alignItems: 'center', justifyContent: 'center', gap: 6, whiteSpace: 'nowrap' }}><Icon name="undo" size={16} stroke={2.2} /> Thu hồi</button>}
                    </div>
                  )}
                </div>
              </div>
            </Card>
          );
        })}
      </div>
    </React.Fragment>
  );
}

/* ════════════════════════ REGISTER (Đăng ký lịch) ════════════════════════ */
function RegisterScreen({ nav, toast }) {
  const [mode, setMode] = useStateF('day');
  const [shifts, setShifts] = useStateF([{ id: 1, start: '08:00', end: '17:00' }]);
  const modes = [['day', 'Ngày'], ['week', 'Tuần'], ['month', 'Tháng']];
  return (
    <div className="screen" style={{ background: 'var(--bg)' }}>
      <TopBar title="Đăng ký lịch" onBack={() => nav('back')} />
      <div className="scroll" style={{ padding: '8px 16px 130px' }}>
        {/* segmented */}
        <div style={{ display: 'flex', background: 'var(--surface-2)', borderRadius: 16, padding: 4, marginBottom: 18 }}>
          {modes.map(([k, l]) => (
            <button key={k} className="press" onClick={() => setMode(k)} style={{
              flex: 1, height: 42, borderRadius: 12, border: 'none', fontFamily: 'inherit', fontSize: 14.5, fontWeight: 700,
              background: mode === k ? 'var(--surface)' : 'transparent', color: mode === k ? 'var(--indigo-deep)' : 'var(--muted)',
              boxShadow: mode === k ? 'var(--shadow-sm)' : 'none', transition: 'all .2s',
            }}>{l}</button>
          ))}
        </div>

        <Card pad={16} style={{ marginBottom: 18, display: 'flex', alignItems: 'center', gap: 14 }} onClick={() => {}}>
          <IconTile name="calendar" tone="indigo" size={46} r={15} />
          <div style={{ flex: 1 }}>
            <div style={{ fontSize: 12.5, color: 'var(--muted)', fontWeight: 600 }}>Chọn ngày</div>
            <div style={{ fontSize: 16, fontWeight: 800, letterSpacing: -0.3 }}>CN, 7 thg 6, 2026</div>
            <div style={{ fontSize: 12, color: 'var(--emerald)', fontWeight: 700, marginTop: 2 }}>{mode === 'day' ? '1 ngày sẽ được tạo' : mode === 'week' ? '7 ngày sẽ được tạo' : '30 ngày sẽ được tạo'}</div>
          </div>
          <Icon name="chevR" size={20} color="var(--faint)" stroke={2.4} />
        </Card>

        <div style={{ fontSize: 12.5, fontWeight: 800, color: 'var(--muted)', letterSpacing: 1, padding: '0 6px 10px' }}>CA LÀM</div>
        <div style={{ display: 'flex', flexDirection: 'column', gap: 12 }}>
          {shifts.map((s, i) => (
            <Card key={s.id} pad={16} style={{ animation: 'popIn .3s ease' }}>
              <div className="press" style={{ display: 'flex', alignItems: 'center', gap: 12, height: 52, padding: '0 14px', borderRadius: 14, background: 'var(--surface-2)', marginBottom: 12 }}>
                <Icon name="store" size={20} color="var(--muted)" stroke={2.1} />
                <span style={{ flex: 1, color: 'var(--muted)', fontWeight: 600, fontSize: 15 }}>Điểm bán ST-001</span>
                <Icon name="chevDown" size={18} color="var(--faint)" stroke={2.4} />
              </div>
              <div style={{ display: 'flex', gap: 12 }}>
                {[['Bắt đầu', s.start], ['Kết thúc', s.end]].map(([l, v]) => (
                  <div key={l} className="press" style={{ flex: 1, padding: '12px 14px', borderRadius: 14, background: 'var(--surface-2)' }}>
                    <div style={{ display: 'flex', alignItems: 'center', gap: 6, color: 'var(--indigo)', fontSize: 12, fontWeight: 700 }}><Icon name="clock" size={14} stroke={2.4} /> {l}</div>
                    <div style={{ fontSize: 22, fontWeight: 800, marginTop: 4, fontFamily: 'Space Grotesk' }} className="tnum">{v}</div>
                  </div>
                ))}
              </div>
              {shifts.length > 1 && <button className="press" onClick={() => setShifts(x => x.filter(z => z.id !== s.id))} style={{ marginTop: 12, width: '100%', height: 40, borderRadius: 12, border: 'none', background: '#FFEEF1', color: 'var(--rose)', fontWeight: 700, fontFamily: 'inherit', fontSize: 13.5 }}>Xóa ca này</button>}
            </Card>
          ))}
          <button className="press" onClick={() => setShifts(x => [...x, { id: Date.now(), start: '18:00', end: '22:00' }])} style={{ height: 52, borderRadius: 16, border: '2px dashed var(--line)', background: 'transparent', color: 'var(--indigo)', fontWeight: 700, fontFamily: 'inherit', fontSize: 15, display: 'inline-flex', alignItems: 'center', justifyContent: 'center', gap: 8 }}>
            <Icon name="plus" size={20} stroke={2.6} /> Thêm ca
          </button>
        </div>
      </div>
      <div style={{ position: 'absolute', left: 0, right: 0, bottom: 0, padding: '14px 16px 30px', background: 'linear-gradient(to top, var(--bg) 70%, transparent)' }}>
        <Button full icon="check" onClick={() => { toast('Đã lưu lịch làm việc'); nav('back'); }}>Lưu lịch</Button>
      </div>
    </div>
  );
}

/* ════════════════════════ LEAVE (Xin nghỉ phép) ════════════════════════ */
function LeaveScreen({ nav, toast, type = 'leave' }) {
  const [reason, setReason] = useStateF('');
  const isOT = type === 'ot';
  return (
    <div className="screen" style={{ background: 'var(--bg)' }}>
      <TopBar title={isOT ? 'Đăng ký OT' : 'Xin nghỉ phép'} onBack={() => nav('back')} />
      <div className="scroll" style={{ padding: '8px 16px 130px' }}>
        <div style={{ display: 'flex', flexDirection: 'column', gap: 12, marginBottom: 18 }}>
          {[['Từ ngày', 'CN, 7 thg 6, 2026'], ['Đến ngày', 'CN, 7 thg 6, 2026']].map(([l, v], i) => (
            <Card key={l} pad={16} onClick={() => {}} style={{ display: 'flex', alignItems: 'center', gap: 14 }}>
              <IconTile name="calendar" tone={i === 0 ? 'indigo' : 'violet'} size={46} r={15} />
              <div style={{ flex: 1 }}>
                <div style={{ fontSize: 12.5, color: 'var(--muted)', fontWeight: 600 }}>{l}</div>
                <div style={{ fontSize: 16, fontWeight: 800, letterSpacing: -0.3, marginTop: 1 }}>{v}</div>
              </div>
              <Icon name="chevR" size={20} color="var(--faint)" stroke={2.4} />
            </Card>
          ))}
        </div>

        {isOT && (
          <Card pad={16} style={{ marginBottom: 18, display: 'flex', alignItems: 'center', gap: 14 }}>
            <IconTile name="otClock" tone="amber" size={46} r={15} />
            <div style={{ flex: 1 }}>
              <div style={{ fontSize: 12.5, color: 'var(--muted)', fontWeight: 600 }}>Số giờ OT</div>
              <div style={{ fontSize: 22, fontWeight: 800, fontFamily: 'Space Grotesk' }} className="tnum">2.0<span style={{ fontSize: 14, color: 'var(--muted)' }}> giờ</span></div>
            </div>
          </Card>
        )}

        <div style={{ fontSize: 12.5, fontWeight: 800, color: 'var(--muted)', letterSpacing: 1, padding: '0 6px 10px' }}>LÝ DO</div>
        <div style={{ background: 'var(--surface)', borderRadius: 20, padding: 16, boxShadow: 'var(--shadow-sm)' }}>
          <textarea value={reason} onChange={e => setReason(e.target.value.slice(0, 1000))} placeholder={isOT ? 'Mô tả công việc làm thêm…' : 'Nhập lý do xin nghỉ…'} style={{
            width: '100%', minHeight: 110, border: 'none', outline: 'none', resize: 'none', fontFamily: 'inherit',
            fontSize: 15.5, color: 'var(--ink)', background: 'transparent', lineHeight: 1.5,
          }} />
          <div style={{ textAlign: 'right', fontSize: 12, color: 'var(--faint)', fontWeight: 600 }}>{reason.length}/1000</div>
        </div>
      </div>
      <div style={{ position: 'absolute', left: 0, right: 0, bottom: 0, padding: '14px 16px 30px', background: 'linear-gradient(to top, var(--bg) 70%, transparent)' }}>
        <Button full icon="send" onClick={() => { toast(isOT ? 'Đã gửi đơn OT' : 'Đã gửi đơn nghỉ phép'); nav('requests'); }}>Gửi đơn</Button>
      </div>
    </div>
  );
}

/* ════════════════════════ REQUESTS (Đơn của tôi) ════════════════════════ */
function RequestsScreen({ nav, toast }) {
  const [tab, setTab] = useStateF('leave');
  const [sheet, setSheet] = useStateF(false);
  const data = {
    leave: [{ id: 1, date: 'CN, 7 thg 6, 2026', reason: 'Bận việc cá nhân', status: 'pending' }],
    ot: [{ id: 1, date: 'T7, 6 thg 6, 2026', reason: 'Kiểm kê cuối tháng · 2.0h', status: 'approved' }],
  };
  const meta = { pending: ['amber', 'clock', 'Chờ duyệt'], approved: ['emerald', 'checkCircle', 'Đã duyệt'], rejected: ['rose', 'undo', 'Từ chối'] };
  return (
    <div className="scroll" style={{ paddingBottom: 120 }}>
      <div style={{ padding: '6px 16px 0', display: 'flex', alignItems: 'center', justifyContent: 'space-between' }}>
        <div style={{ fontSize: 26, fontWeight: 800, letterSpacing: -0.6, whiteSpace: 'nowrap' }} className="display">Đơn của tôi</div>
        <button className="press" onClick={() => setSheet(true)} aria-label="Tạo đơn" style={{ width: 46, height: 46, borderRadius: 15, border: 'none', background: 'var(--grad-brand)', color: '#fff', display: 'grid', placeItems: 'center', boxShadow: 'var(--shadow-brand)' }}>
          <Icon name="plus" size={24} stroke={2.6} />
        </button>
      </div>

      {/* tabs */}
      <div style={{ display: 'flex', gap: 8, padding: '18px 16px 14px' }}>
        {[['leave', 'Nghỉ phép', 'umbrella'], ['ot', 'Làm thêm (OT)', 'otClock']].map(([k, l, ic]) => (
          <button key={k} className="press" onClick={() => setTab(k)} style={{
            flex: 1, height: 46, borderRadius: 15, border: 'none', fontFamily: 'inherit', fontSize: 14.5, fontWeight: 700,
            background: tab === k ? 'var(--ink)' : 'var(--surface)', color: tab === k ? '#fff' : 'var(--muted)',
            boxShadow: tab === k ? 'var(--shadow)' : 'var(--shadow-sm)', display: 'inline-flex', alignItems: 'center', justifyContent: 'center', gap: 7, transition: 'all .2s',
          }}><Icon name={ic} size={18} stroke={2.2} /> {l}</button>
        ))}
      </div>

      <div className="stagger" style={{ padding: '0 16px', display: 'flex', flexDirection: 'column', gap: 12 }}>
        {data[tab].map(d => {
          const [tone, icon, label] = meta[d.status];
          return (
            <Card key={d.id} pad={16}>
              <div style={{ display: 'flex', alignItems: 'center', justifyContent: 'space-between' }}>
                <Chip tone={tab === 'leave' ? 'indigo' : 'amber'} icon={tab === 'leave' ? 'umbrella' : 'otClock'}>{tab === 'leave' ? 'Nghỉ phép' : 'Làm thêm'}</Chip>
                <Chip tone={tone} icon={icon}>{label}</Chip>
              </div>
              <div style={{ fontSize: 16, fontWeight: 800, letterSpacing: -0.3, marginTop: 12 }}>{d.date}</div>
              <div style={{ fontSize: 13.5, color: 'var(--body)', fontWeight: 500, marginTop: 4 }}>{d.reason}</div>
              {d.status === 'pending' && (
                <div style={{ display: 'flex', justifyContent: 'flex-end', borderTop: '1px solid var(--line)', marginTop: 14, paddingTop: 12 }}>
                  <button className="press" onClick={() => toast('Đã thu hồi đơn', 'warn')} style={{ height: 38, padding: '0 16px', borderRadius: 12, border: 'none', background: '#FFEEF1', color: 'var(--rose)', fontWeight: 700, fontFamily: 'inherit', fontSize: 13.5, display: 'inline-flex', alignItems: 'center', gap: 6, whiteSpace: 'nowrap' }}><Icon name="undo" size={16} stroke={2.2} /> Thu hồi</button>
                </div>
              )}
            </Card>
          );
        })}
        <div style={{ textAlign: 'center', padding: '30px 0', color: 'var(--faint)', fontSize: 13, fontWeight: 600 }}>— Hết danh sách —</div>
      </div>

      <Sheet open={sheet} onClose={() => setSheet(false)} title="Tạo đơn mới">
        {[['Xin nghỉ phép', 'umbrella', 'indigo', () => nav('leave')], ['Đăng ký OT', 'otClock', 'amber', () => nav('ot')]].map(([t, ic, tone, go]) => (
          <div key={t} className="press" onClick={() => { setSheet(false); go(); }} style={{ display: 'flex', alignItems: 'center', gap: 14, padding: 14, borderRadius: 18, background: 'var(--surface-2)', marginBottom: 10 }}>
            <IconTile name={ic} tone={tone} size={48} r={16} />
            <div style={{ flex: 1, fontSize: 16, fontWeight: 800 }}>{t}</div>
            <Icon name="chevR" size={20} color="var(--faint)" stroke={2.4} />
          </div>
        ))}
      </Sheet>
    </div>
  );
}

/* ════════════════════════ FACE (Khuôn mặt) ════════════════════════ */
function FaceScreen({ nav, toast }) {
  const [done, setDone] = useStateF(true);
  return (
    <div className="screen" style={{ background: 'var(--bg)' }}>
      <TopBar title="Khuôn mặt" onBack={() => nav('back')} />
      <div className="scroll" style={{ padding: '8px 16px 40px', display: 'flex', flexDirection: 'column', alignItems: 'center' }}>
        <div style={{ position: 'relative', width: 200, height: 200, display: 'grid', placeItems: 'center', margin: '20px 0 24px' }}>
          <div style={{ position: 'absolute', inset: 0, borderRadius: '50%', background: 'var(--grad-mesh)', opacity: 0.14 }} />
          <div style={{ width: 150, height: 150, borderRadius: '50%', background: 'var(--grad-brand)', display: 'grid', placeItems: 'center', boxShadow: 'var(--shadow-lg)' }}>
            <Icon name="face" size={74} color="#fff" stroke={1.8} />
          </div>
          {done && <div style={{ position: 'absolute', bottom: 8, right: 28, width: 44, height: 44, borderRadius: '50%', background: 'var(--grad-emerald)', display: 'grid', placeItems: 'center', boxShadow: '0 6px 16px rgba(16,185,129,.4)', border: '3px solid var(--bg)' }}><Icon name="check" size={22} color="#fff" stroke={3} /></div>}
        </div>
        <div style={{ fontSize: 20, fontWeight: 800, letterSpacing: -0.4 }}>{done ? 'Khuôn mặt đã đăng ký' : 'Chưa có khuôn mặt'}</div>
        <div style={{ fontSize: 14, color: 'var(--muted)', fontWeight: 500, marginTop: 4, textAlign: 'center', maxWidth: 260 }}>Dùng để xác thực nhanh khi check-in / check-out tại điểm bán.</div>
        {done && <div style={{ display: 'flex', gap: 8, marginTop: 16 }}><Chip tone="emerald" icon="checkCircle">Đã xác thực</Chip><Chip tone="neutral" icon="clock">Cập nhật 06/06</Chip></div>}
        <div style={{ width: '100%', marginTop: 32, display: 'flex', flexDirection: 'column', gap: 10 }}>
          <Button full icon="face" onClick={() => { setDone(true); toast('Đã cập nhật khuôn mặt'); }}>{done ? 'Đăng ký lại' : 'Đăng ký khuôn mặt'}</Button>
          {done && <Button full variant="soft" icon="undo" onClick={() => { setDone(false); toast('Đã xóa khuôn mặt', 'warn'); }} style={{ color: 'var(--rose)', background: '#FFEEF1' }}>Xóa khuôn mặt</Button>}
        </div>
      </div>
    </div>
  );
}

/* ════════════════════════ HISTORY (Lịch sử chấm công) ════════════════════════ */
function HistoryScreen({ nav }) {
  const days = [
    { date: 'T6, 6 thg 6', in: '07:58', out: '17:03', h: '9h 05m', ok: true },
    { date: 'T5, 5 thg 6', in: '08:12', out: '17:00', h: '8h 48m', late: true },
    { date: 'T4, 4 thg 6', in: '07:55', out: '16:58', h: '9h 03m', ok: true },
    { date: 'T3, 3 thg 6', in: '08:00', out: '17:10', h: '9h 10m', ok: true },
  ];
  return (
    <div className="screen" style={{ background: 'var(--bg)' }}>
      <TopBar title="Lịch sử chấm công" onBack={() => nav('back')} />
      <div style={{ padding: '4px 16px 14px' }}>
        <div style={{ borderRadius: 24, background: 'var(--grad-mesh)', padding: 18, boxShadow: 'var(--shadow-lg)', display: 'flex', alignItems: 'center', gap: 16 }}>
          <div><div style={{ color: 'rgba(255,255,255,.8)', fontSize: 12.5, fontWeight: 600 }}>Tháng 6 · giờ công</div><div style={{ color: '#fff', fontSize: 30, fontWeight: 800, fontFamily: 'Space Grotesk' }} className="tnum">142<span style={{ fontSize: 16 }}>h</span></div></div>
          <div style={{ marginLeft: 'auto', textAlign: 'right' }}><div style={{ color: 'rgba(255,255,255,.8)', fontSize: 12.5, fontWeight: 600 }}>Đúng giờ</div><div style={{ color: '#fff', fontSize: 30, fontWeight: 800, fontFamily: 'Space Grotesk' }} className="tnum">96<span style={{ fontSize: 16 }}>%</span></div></div>
        </div>
      </div>
      <div className="scroll stagger" style={{ padding: '0 16px 40px', display: 'flex', flexDirection: 'column', gap: 10 }}>
        {days.map(d => (
          <Card key={d.date} pad={14} style={{ display: 'flex', alignItems: 'center', gap: 14 }}>
            <IconTile name={d.late ? 'clock' : 'checkCircle'} tone={d.late ? 'amber' : 'emerald'} size={44} r={14} />
            <div style={{ flex: 1 }}>
              <div style={{ fontSize: 15, fontWeight: 800, letterSpacing: -0.2 }}>{d.date}</div>
              <div style={{ fontSize: 12.5, color: 'var(--muted)', fontWeight: 600, marginTop: 2 }}>{d.in} → {d.out}</div>
            </div>
            <div style={{ textAlign: 'right' }}>
              <div style={{ fontSize: 15, fontWeight: 800, fontFamily: 'Space Grotesk' }} className="tnum">{d.h}</div>
              {d.late ? <span style={{ fontSize: 11.5, color: 'var(--amber)', fontWeight: 700 }}>Đi muộn</span> : <span style={{ fontSize: 11.5, color: 'var(--emerald)', fontWeight: 700 }}>Đúng giờ</span>}
            </div>
          </Card>
        ))}
      </div>
    </div>
  );
}

/* ════════════════════════ LOGIN (Đăng nhập) ════════════════════════ */
function LoginScreen({ nav }) {
  const [email, setEmail] = useStateF('pg01@rmms.local');
  const [pw, setPw] = useStateF('••••••••');
  const [show, setShow] = useStateF(false);
  const [busy, setBusy] = useStateF(false);
  const go = () => { setBusy(true); setTimeout(() => { setBusy(false); nav('home'); }, 1100); };
  const field = (icon, val, set, ph, pwd) => (
    <div style={{ display: 'flex', alignItems: 'center', gap: 12, height: 58, padding: '0 16px', borderRadius: 17, background: 'var(--surface)', boxShadow: 'var(--shadow-sm)' }}>
      <Icon name={icon} size={20} color="var(--muted)" stroke={2.1} />
      <input value={val} onChange={e => set(e.target.value)} placeholder={ph} type={pwd && !show ? 'password' : 'text'} style={{ flex: 1, border: 'none', outline: 'none', background: 'transparent', fontFamily: 'inherit', fontSize: 16, fontWeight: 600, color: 'var(--ink)' }} />
      {pwd && <button onClick={() => setShow(s => !s)} className="press" style={{ border: 'none', background: 'transparent', color: 'var(--faint)', padding: 0 }}><Icon name="eye" size={20} stroke={2.1} /></button>}
    </div>
  );
  return (
    <div className="screen" style={{ background: 'var(--bg)' }}>
      {/* brand hero */}
      <div style={{ padding: '20px 24px 0' }}>
        <div style={{ width: 64, height: 64, borderRadius: 22, background: 'var(--grad-brand)', display: 'grid', placeItems: 'center', boxShadow: 'var(--shadow-brand)', marginBottom: 22 }}>
          <Icon name="store" size={32} color="#fff" stroke={2} />
        </div>
        <div style={{ fontSize: 34, fontWeight: 800, letterSpacing: -1, lineHeight: 1.05 }} className="display">Chào mừng<br/>trở lại 👋</div>
        <div style={{ fontSize: 15, color: 'var(--muted)', fontWeight: 500, marginTop: 8 }}>Đăng nhập để bắt đầu ca làm việc của bạn.</div>
      </div>
      <div className="scroll" style={{ padding: '28px 24px 40px' }}>
        <div style={{ display: 'flex', flexDirection: 'column', gap: 12 }}>
          {field('mail', email, setEmail, 'Email')}
          {field('lock', pw, setPw, 'Mật khẩu', true)}
        </div>
        <div style={{ textAlign: 'right', margin: '14px 4px 22px' }}>
          <span style={{ fontSize: 14, fontWeight: 700, color: 'var(--indigo)' }}>Quên mật khẩu?</span>
        </div>
        <Button full onClick={go} disabled={busy} icon={busy ? null : 'arrowUpR'}>
          {busy ? <span style={{ width: 22, height: 22, borderRadius: '50%', border: '2.5px solid rgba(255,255,255,.4)', borderTopColor: '#fff', animation: 'spin .7s linear infinite' }} /> : 'Đăng nhập'}
        </Button>
        <div style={{ textAlign: 'center', marginTop: 24, fontSize: 14.5, color: 'var(--muted)', fontWeight: 500 }}>
          Chưa có tài khoản? <span style={{ color: 'var(--indigo)', fontWeight: 800 }}>Đăng ký</span>
        </div>
      </div>
    </div>
  );
}

Object.assign(window, { ScheduleScreen, RegisterScreen, LeaveScreen, RequestsScreen, FaceScreen, HistoryScreen, LoginScreen });
