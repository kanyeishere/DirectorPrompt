UPDATE scenes
SET status = 'Active'
WHERE status = '0';

UPDATE scenes
SET status = 'Completed'
WHERE status = '1';

UPDATE scenes
SET status = 'Archived'
WHERE status = '2';
