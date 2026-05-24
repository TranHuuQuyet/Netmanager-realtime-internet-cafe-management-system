PRAGMA foreign_keys = ON;

CREATE TABLE IF NOT EXISTS Users (
    Id TEXT PRIMARY KEY,
    Username TEXT NOT NULL UNIQUE,
    Password TEXT NOT NULL,
    Role TEXT NOT NULL,
    MachineId TEXT NULL,
    IsActive INTEGER NOT NULL DEFAULT 1,
    LastLogin TEXT NULL,
    CreatedAt TEXT NOT NULL
);

CREATE TABLE IF NOT EXISTS Machines (
    Id TEXT PRIMARY KEY,
    MachineId TEXT NOT NULL UNIQUE,
    MachineName TEXT NOT NULL,
    IpAddress TEXT NULL,
    Status TEXT NOT NULL,
    LastSeen TEXT NULL,
    IsActive INTEGER NOT NULL DEFAULT 1
);

CREATE TABLE IF NOT EXISTS Sessions (
    Id TEXT PRIMARY KEY,
    UserId TEXT NOT NULL,
    MachineId TEXT NULL,
    Status TEXT NOT NULL,
    StartTime TEXT NOT NULL,
    EndTime TEXT NULL,
    LastSeen TEXT NULL,
    FOREIGN KEY (UserId) REFERENCES Users(Id)
);

CREATE INDEX IF NOT EXISTS IX_Users_Username ON Users (Username);
CREATE INDEX IF NOT EXISTS IX_Users_MachineId ON Users (MachineId);
CREATE INDEX IF NOT EXISTS IX_Sessions_MachineId_Status ON Sessions (MachineId, Status);
