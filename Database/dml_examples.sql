-- ============================================================
--  dml_examples.sql — примеры изменяющих запросов.
--  Приложением не выполняется: откройте lab6.db в DB Browser for SQLite
--  и запускайте блоки по очереди (не забудьте PRAGMA foreign_keys = ON).
-- ============================================================
PRAGMA foreign_keys = ON;

-- INSERT: новая квартира
INSERT INTO Apartment (ApartmentNumber, Description) VALUES (401, 'Новая квартира');

-- Транзакция: комната создаётся в двух таблицах — либо обе вставки, либо ни одной
BEGIN TRANSACTION;
INSERT INTO Rooms (ApartmentId, Name) VALUES ((SELECT ID FROM Apartment WHERE ApartmentNumber = 401), 'Гостиная');
INSERT INTO RoomInfo (RoomId, Area, Temperature, IsLightOn) VALUES (last_insert_rowid(), 17.5, 22, 0);
COMMIT;

-- UPDATE: изменить температуру (сработает триггер и появится запись в RoomLog)
UPDATE RoomInfo SET Temperature = 24 WHERE RoomId = 4;

-- UPDATE с подзапросом: выключить свет во всех комнатах квартиры 101
UPDATE RoomInfo
SET IsLightOn = 0
WHERE RoomId IN (SELECT r.ID FROM Rooms r JOIN Apartment a ON a.ID = r.ApartmentId WHERE a.ApartmentNumber = 101);

-- DELETE: удаляем квартиру; комнаты, RoomInfo и RoomLog удалятся каскадно
DELETE FROM Apartment WHERE ApartmentNumber = 401;

-- Примеры нарушений ограничений (каждый запрос должен завершиться ошибкой):
-- INSERT INTO RoomInfo (RoomId, Area, Temperature, IsLightOn) VALUES (1, 10, 40, 0);   -- CHECK: температура 15..25
-- INSERT INTO Apartment (ApartmentNumber, Description) VALUES (101, 'дубль');           -- UNIQUE: номер уже есть
-- INSERT INTO Rooms (ApartmentId, Name) VALUES (999, 'Призрак');                        -- FOREIGN KEY: нет такой квартиры
