#!/usr/bin/env bash
# ============================================================================
#  backup-restore.sh - RMMS Postgres + object-store backup & restore-verify
# ============================================================================
#
#  Runs on the Ubuntu VPS (where docker compose hosts the stack). Three modes:
#
#    backup           pg_dump the rmms DB + tar the bind-mounted data dirs
#                     (MinIO selfies, CompreFace pgdata) into ./backups/<stamp>/
#    verify <dump>    Restore <dump> into a THROWAWAY temp Postgres container
#                     (never touches prod), run sanity row counts, tear down.
#    all (default)    backup, then verify the dump that was just produced.
#
#  ⚠️  WHY VERIFY: a bind-mounted Postgres data dir (docker-compose maps
#      ${POSTGRES_DATA_DIR:-./data/postgres} -> /var/lib/postgresql/data) is
#      only initialised by the postgis entrypoint when the dir is EMPTY. If you
#      `docker compose down -v` / recreate the container against a populated
#      host dir it reuses existing data — but against an EMPTY dir it silently
#      re-inits a blank cluster. ALWAYS run `backup` before recreating the
#      Postgres container, and keep a verified dump off-box.
#
#  Usage:
#    ./scripts/backup-restore.sh                 # backup + verify
#    ./scripts/backup-restore.sh backup
#    ./scripts/backup-restore.sh verify ./backups/20260614-120000/rmms.dump
#
#  Env (falls back to docker-compose defaults; source your .env first):
#    POSTGRES_DB=rmms POSTGRES_USER=rmms POSTGRES_PASSWORD=...
#    PG_CONTAINER=rmms-postgres            # running prod container name
#    PG_IMAGE=postgis/postgis:16-3.4       # image used for the temp verify box
#    DATA_DIR=./data                       # bind-mount root (minio, compreface-pgdata)
#    BACKUP_ROOT=./backups
# ============================================================================
set -euo pipefail

# ---- config ----
POSTGRES_DB="${POSTGRES_DB:-rmms}"
POSTGRES_USER="${POSTGRES_USER:-rmms}"
POSTGRES_PASSWORD="${POSTGRES_PASSWORD:-rmms_dev_pw}"
PG_CONTAINER="${PG_CONTAINER:-rmms-postgres}"
PG_IMAGE="${PG_IMAGE:-postgis/postgis:16-3.4}"
DATA_DIR="${DATA_DIR:-./data}"
BACKUP_ROOT="${BACKUP_ROOT:-./backups}"

STAMP="$(date +%Y%m%d-%H%M%S)"
log()  { printf '\033[0;36m[%s]\033[0m %s\n' "$(date +%H:%M:%S)" "$*"; }
ok()   { printf '\033[0;32m  OK  \033[0m %s\n' "$*"; }
die()  { printf '\033[0;31mFATAL\033[0m %s\n' "$*" >&2; exit 1; }

require() { command -v "$1" >/dev/null 2>&1 || die "missing dependency: $1"; }
require docker

# ---------------------------------------------------------------------------
do_backup() {
    local outdir="$BACKUP_ROOT/$STAMP"
    mkdir -p "$outdir"
    log "Backup -> $outdir"

    docker ps --format '{{.Names}}' | grep -qx "$PG_CONTAINER" \
        || die "Postgres container '$PG_CONTAINER' is not running"

    # Custom-format dump (compressed, restorable with pg_restore).
    log "pg_dump $POSTGRES_DB (custom format) ..."
    docker exec -e PGPASSWORD="$POSTGRES_PASSWORD" "$PG_CONTAINER" \
        pg_dump -U "$POSTGRES_USER" -d "$POSTGRES_DB" -Fc > "$outdir/rmms.dump"
    local sz; sz="$(du -h "$outdir/rmms.dump" | cut -f1)"
    [ -s "$outdir/rmms.dump" ] || die "dump is empty"
    ok "DB dump written ($sz)"

    # Tar the bind-mounted object data (selfies + CompreFace embeddings DB).
    for d in minio compreface-pgdata; do
        if [ -d "$DATA_DIR/$d" ]; then
            log "tar $DATA_DIR/$d ..."
            tar -czf "$outdir/$d.tar.gz" -C "$DATA_DIR" "$d"
            ok "$d -> $(du -h "$outdir/$d.tar.gz" | cut -f1)"
        else
            printf '\033[0;33m  WARN\033[0m %s not found (skipped)\n' "$DATA_DIR/$d"
        fi
    done

    echo "$outdir/rmms.dump"   # last line = dump path (consumed by 'all')
}

# ---------------------------------------------------------------------------
do_verify() {
    local dump="$1"
    [ -f "$dump" ] || die "dump not found: $dump"
    local tmpname="rmms-verify-$STAMP"
    log "Restore-verify '$dump' into throwaway container '$tmpname'"

    # Throwaway box on an ephemeral port; tmpfs so nothing persists.
    docker run -d --rm --name "$tmpname" \
        -e POSTGRES_DB="$POSTGRES_DB" \
        -e POSTGRES_USER="$POSTGRES_USER" \
        -e POSTGRES_PASSWORD="$POSTGRES_PASSWORD" \
        --tmpfs /var/lib/postgresql/data \
        "$PG_IMAGE" >/dev/null
    # shellcheck disable=SC2064
    trap "docker stop '$tmpname' >/dev/null 2>&1 || true" EXIT

    log "Waiting for temp Postgres to accept connections ..."
    for _ in $(seq 1 30); do
        if docker exec "$tmpname" pg_isready -U "$POSTGRES_USER" -d "$POSTGRES_DB" >/dev/null 2>&1; then
            break
        fi
        sleep 1
    done
    docker exec "$tmpname" pg_isready -U "$POSTGRES_USER" -d "$POSTGRES_DB" >/dev/null 2>&1 \
        || die "temp Postgres never became ready"
    ok "temp Postgres ready"

    log "pg_restore ..."
    docker exec -i -e PGPASSWORD="$POSTGRES_PASSWORD" "$tmpname" \
        pg_restore -U "$POSTGRES_USER" -d "$POSTGRES_DB" --no-owner --no-privileges < "$dump"
    ok "restore completed"

    # Sanity: table count + a few key tables must exist and be queryable.
    log "Sanity counts ..."
    local q="
        SELECT 'tables', count(*) FROM information_schema.tables WHERE table_schema='public'
        UNION ALL SELECT 'users', count(*) FROM users
        UNION ALL SELECT 'attendance_records', count(*) FROM attendance_records
        UNION ALL SELECT 'audit_log', count(*) FROM audit_log;"
    docker exec -e PGPASSWORD="$POSTGRES_PASSWORD" "$tmpname" \
        psql -U "$POSTGRES_USER" -d "$POSTGRES_DB" -A -F': ' -t -c "$q" \
        | while IFS= read -r line; do ok "$line"; done

    local tcount
    tcount="$(docker exec -e PGPASSWORD="$POSTGRES_PASSWORD" "$tmpname" \
        psql -U "$POSTGRES_USER" -d "$POSTGRES_DB" -At \
        -c "SELECT count(*) FROM information_schema.tables WHERE table_schema='public';")"
    [ "${tcount:-0}" -ge 1 ] || die "restored DB has no public tables — dump is bad"
    ok "restore verified ($tcount public tables)"
}

# ---------------------------------------------------------------------------
MODE="${1:-all}"
case "$MODE" in
    backup) do_backup ;;
    verify) [ $# -ge 2 ] || die "usage: $0 verify <dumpfile>"; do_verify "$2" ;;
    all)
        DUMP="$(do_backup | tail -n1)"
        echo
        do_verify "$DUMP"
        echo
        ok "backup + verify complete: $DUMP"
        ;;
    *) die "unknown mode '$MODE' (use: backup | verify <dump> | all)" ;;
esac
