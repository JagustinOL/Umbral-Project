-- Backfill BaseScore for playable games created before the ranking fix.
-- Safe to run multiple times. Only updates Trivia / TreasureHunt nodes still at 0.
-- Stages keep base_score = 0.
--
-- IMPORTANT (RN-01): Prefer fixing missions while Draft via Admin UI (edit game → Puntaje base).
-- This script is for existing catalog rows that already have base_score = 0.
-- Sessions already created snapshot AllowedNode.BaseScore at creation time — recreate
-- those sessions after backfill if you need live ranking to reflect the new scores.
--
-- Usage (docker postgres):
--   docker exec -i umbral-db psql -U postgres -d umbral_db < scripts/backfill-playable-base-score.sql
-- Or:
--   psql -h localhost -U postgres -d umbral_db -f scripts/backfill-playable-base-score.sql

BEGIN;

UPDATE mission_nodes
SET base_score = 100
WHERE base_score = 0
  AND node_type IN ('Trivia', 'TreasureHunt');

-- Optional: give existing hints a default penalty so ScoringAudit can apply them.
UPDATE hints
SET penalty_points = 10
WHERE penalty_points = 0;

COMMIT;
