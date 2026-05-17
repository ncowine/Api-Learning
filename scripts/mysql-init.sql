-- Runs once on first MySQL container start (mounted via docker-entrypoint-initdb.d).
-- Creates the second database (the first, app_db, is created by MYSQL_DATABASE env var)
-- and grants the dev user access to both.

CREATE DATABASE IF NOT EXISTS testplan_db
    CHARACTER SET utf8mb4
    COLLATE utf8mb4_unicode_ci;

GRANT ALL PRIVILEGES ON app_db.* TO 'testers'@'%';
GRANT ALL PRIVILEGES ON testplan_db.* TO 'testers'@'%';

FLUSH PRIVILEGES;
