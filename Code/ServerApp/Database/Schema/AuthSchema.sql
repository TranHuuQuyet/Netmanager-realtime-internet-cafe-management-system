PRAGMA foreign_keys = ON;

CREATE TABLE IF NOT EXISTS AuthUsers
(
    Id TEXT PRIMARY KEY,
    Username TEXT NOT NULL UNIQUE,
    PasswordSaltBase64 TEXT NOT NULL,
    PasswordHashBase64 TEXT NOT NULL,
    Role INTEGER NOT NULL,
    MachineId TEXT,
    IsActive INTEGER NOT NULL DEFAULT 1,
    LastLoginAtUtc TEXT
);

CREATE TABLE IF NOT EXISTS AuthSessions
(
    Id TEXT PRIMARY KEY,
    UserId TEXT NOT NULL,
    Username TEXT NOT NULL,
    Role INTEGER NOT NULL,
    MachineId TEXT,
    State INTEGER NOT NULL,
    StartedAtUtc TEXT NOT NULL,
    EndedAtUtc TEXT
);

CREATE INDEX IF NOT EXISTS IX_AuthUsers_Username ON AuthUsers (Username);
CREATE INDEX IF NOT EXISTS IX_AuthSessions_UserId_State ON AuthSessions (UserId, State);
