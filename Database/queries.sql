 -- соединение трёх таблиц:  
SELECT a.ApartmentNumber AS Apartment, r.Name AS Room, ri.Area, ri.Temperature, ri.IsLightOn
FROM Rooms r
JOIN Apartment a  ON a.ID = r.ApartmentId
JOIN RoomInfo  ri ON ri.RoomId = r.ID
ORDER BY a.ApartmentNumber, r.ID;

-- теплые комнаты  
SELECT * FROM v_room_details
WHERE Temperature >= 22
ORDER BY Temperature DESC, ApartmentNumber;
 
-- квартиры без комнат
SELECT a.ApartmentNumber AS Apartment,
       COUNT(r.ID)                     AS RoomsCount,
       COALESCE(ROUND(SUM(ri.Area), 1), 0) AS TotalArea,
       COALESCE(ROUND(AVG(ri.Temperature), 1), 0) AS AvgTemp
FROM Apartment a
LEFT JOIN Rooms r      ON r.ApartmentId = a.ID
LEFT JOIN RoomInfo ri  ON ri.RoomId = r.ID
GROUP BY a.ID
ORDER BY TotalArea DESC;

 
SELECT a.ApartmentNumber AS Apartment, COUNT(*) AS RoomsCount
FROM Apartment a
JOIN Rooms r ON r.ApartmentId = a.ID
GROUP BY a.ID
HAVING COUNT(*) > 3;

 
SELECT r.Name AS Room, ri.Area,
       ROUND((SELECT AVG(Area) FROM RoomInfo), 2) AS AvgArea
FROM Rooms r
JOIN RoomInfo ri ON ri.RoomId = r.ID
WHERE ri.Area > (SELECT AVG(Area) FROM RoomInfo)
ORDER BY ri.Area DESC;
 
SELECT ApartmentNumber AS Apartment, RoomName AS Room, Area
FROM v_room_details
WHERE IsLightOn = 1
ORDER BY ApartmentNumber;

 
SELECT a.ApartmentNumber AS Apartment, a.Description
FROM Apartment a
LEFT JOIN Rooms r ON r.ApartmentId = a.ID
WHERE r.ID IS NULL;

 
SELECT a.ApartmentNumber AS Apartment, r.Name AS Room, ri.Area
FROM Rooms r
JOIN Apartment a  ON a.ID = r.ApartmentId
JOIN RoomInfo  ri ON ri.RoomId = r.ID
WHERE ri.Area = (SELECT MAX(ri2.Area)
                 FROM Rooms r2
                 JOIN RoomInfo ri2 ON ri2.RoomId = r2.ID
                 WHERE r2.ApartmentId = r.ApartmentId)
ORDER BY a.ApartmentNumber;

 
SELECT r.Name AS RoomType,
       COUNT(*) AS Total,
       ROUND(AVG(ri.Area), 1) AS AvgArea,
       ROUND(AVG(ri.Temperature), 1) AS AvgTemp
FROM Rooms r
JOIN RoomInfo ri ON ri.RoomId = r.ID
GROUP BY r.Name
ORDER BY Total DESC, RoomType;

 
SELECT ApartmentNumber AS Apartment, RoomName AS Room, Area,
       ROW_NUMBER() OVER (PARTITION BY ApartmentNumber ORDER BY Area DESC) AS PlaceInApartment
FROM v_room_details
ORDER BY ApartmentNumber, PlaceInApartment;
 
WITH totals AS (
    SELECT a.ApartmentNumber AS Apartment, SUM(ri.Area) AS TotalArea
    FROM Apartment a
    JOIN Rooms r     ON r.ApartmentId = a.ID
    JOIN RoomInfo ri ON ri.RoomId = r.ID
    GROUP BY a.ID
)
SELECT Apartment, ROUND(TotalArea, 1) AS TotalArea
FROM totals
WHERE TotalArea > (SELECT AVG(TotalArea) FROM totals)
ORDER BY TotalArea DESC;

 
SELECT ApartmentNumber AS Apartment, RoomName AS Room, Area
FROM v_room_details
WHERE RoomName LIKE '%Спальня%' OR RoomName LIKE '%Детская%';
 
SELECT l.ChangedAt, a.ApartmentNumber AS Apartment, r.Name AS Room,
       l.OldTemperature AS OldTemp, l.NewTemperature AS NewTemp
FROM RoomLog l
JOIN Rooms r     ON r.ID = l.RoomId
JOIN Apartment a ON a.ID = r.ApartmentId
ORDER BY l.ID DESC;

 
SELECT COUNT(DISTINCT r.ApartmentId)            AS ApartmentsWithRooms,
       COUNT(*)                                 AS Rooms,
       ROUND(SUM(ri.Area), 1)                   AS TotalArea,
       SUM(CASE WHEN ri.IsLightOn = 1 THEN 1 ELSE 0 END) AS LightsOn,
       SUM(CASE WHEN ri.Temperature < 20 THEN 1 ELSE 0 END) AS ColdRooms
FROM Rooms r
JOIN RoomInfo ri ON ri.RoomId = r.ID;
