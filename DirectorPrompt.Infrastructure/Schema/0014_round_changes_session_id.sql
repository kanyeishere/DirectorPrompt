-- round_changes: 添加 session_id 列, 修复 round_id 跨对话不唯一的问题

ALTER TABLE round_changes
    ADD COLUMN session_id INTEGER NOT NULL DEFAULT 0;

UPDATE round_changes
SET session_id = (SELECT pe.session_id
                  FROM playthrough_events pe
                  WHERE pe.round_id = round_changes.round_id
                  ORDER BY pe.id ASC
    LIMIT 1
    );

CREATE INDEX IF NOT EXISTS idx_round_changes_session_round ON round_changes(session_id, round_id);
