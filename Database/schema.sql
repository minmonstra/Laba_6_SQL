

PRAGMA foreign_keys = ON;   -- в SQLite внешние ключи включаются отдельно для каждого соединения

-- Квартиры
CREATE TABLE IF NOT EXISTS Apartment (
    ID              INTEGER PRIMARY KEY AUTOINCREMENT,
    ApartmentNumber INTEGER NOT NULL UNIQUE CHECK (ApartmentNumber > 0),
    Description     TEXT    NOT NULL DEFAULT 'Описание квартиры'
);

-- Комнаты  
CREATE TABLE IF NOT EXISTS Rooms (
    ID          INTEGER PRIMARY KEY AUTOINCREMENT,
    ApartmentId INTEGER NOT NULL,
    Name        TEXT    NOT NULL CHECK (length(trim(Name)) > 0),
    FOREIGN KEY (ApartmentId) REFERENCES Apartment(ID) ON DELETE CASCADE
);

 
CREATE TABLE IF NOT EXISTS RoomInfo (
    RoomId      INTEGER PRIMARY KEY,
    Area        REAL    NOT NULL CHECK (Area >= 0),
    Temperature INTEGER NOT NULL CHECK (Temperature BETWEEN 15 AND 25),
    IsLightOn   INTEGER NOT NULL DEFAULT 0 CHECK (IsLightOn IN (0, 1)),
    FOREIGN KEY (RoomId) REFERENCES Rooms(ID) ON DELETE CASCADE
);

-- Журнал изменений температуры  
CREATE TABLE IF NOT EXISTS RoomLog (
    ID             INTEGER PRIMARY KEY AUTOINCREMENT,
    RoomId         INTEGER NOT NULL,
    OldTemperature INTEGER NOT NULL,
    NewTemperature INTEGER NOT NULL,
    ChangedAt      TEXT    NOT NULL DEFAULT (datetime('now', 'localtime')),
    FOREIGN KEY (RoomId) REFERENCES Rooms(ID) ON DELETE CASCADE
);

 
CREATE INDEX IF NOT EXISTS idx_rooms_apartment ON Rooms(ApartmentId);
CREATE INDEX IF NOT EXISTS idx_roomlog_room    ON RoomLog(RoomId);
 
CREATE VIEW IF NOT EXISTS v_room_details AS
SELECT r.ID            AS RoomId,
       a.ApartmentNumber,
       r.Name          AS RoomName,
       ri.Area,
       ri.Temperature,
       ri.IsLightOn
FROM Rooms r
JOIN Apartment a  ON a.ID  = r.ApartmentId
JOIN RoomInfo  ri ON ri.RoomId = r.ID;

 
CREATE TRIGGER IF NOT EXISTS trg_roominfo_temperature_log
AFTER UPDATE OF Temperature ON RoomInfo
WHEN OLD.Temperature <> NEW.Temperature
BEGIN
    INSERT INTO RoomLog (RoomId, OldTemperature, NewTemperature)
    VALUES (OLD.RoomId, OLD.Temperature, NEW.Temperature);
END;
