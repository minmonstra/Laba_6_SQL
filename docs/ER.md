# ER-диаграмма

```mermaid
erDiagram
    Apartment ||--o{ Rooms : "содержит"
    Rooms ||--|| RoomInfo : "состояние (1:1)"
    Rooms ||--o{ RoomLog : "журнал температуры"

    Apartment {
        INTEGER ID PK
        INTEGER ApartmentNumber "UNIQUE, > 0"
        TEXT Description
    }
    Rooms {
        INTEGER ID PK
        INTEGER ApartmentId FK "ON DELETE CASCADE"
        TEXT Name "не пустое"
    }
    RoomInfo {
        INTEGER RoomId PK, FK "ON DELETE CASCADE"
        REAL Area ">= 0"
        INTEGER Temperature "15..25"
        INTEGER IsLightOn "0 или 1"
    }
    RoomLog {
        INTEGER ID PK
        INTEGER RoomId FK "ON DELETE CASCADE"
        INTEGER OldTemperature
        INTEGER NewTemperature
        TEXT ChangedAt
    }
```

**Почему `RoomInfo` вынесена отдельно.** В `Rooms` лежат постоянные данные комнаты (к какой квартире относится, как называется),
а в `RoomInfo` — изменяемое состояние (площадь, температура, свет). Это связь «один к одному», при которой
первичный ключ `RoomInfo.RoomId` одновременно является внешним ключом на `Rooms.ID`.

**Объекты базы данных:**

| Объект | Назначение |
|---|---|
| `v_room_details` (VIEW) | «Плоская» таблица комнат с номером квартиры, используется в нескольких запросах |
| `trg_roominfo_temperature_log` (TRIGGER) | При смене температуры добавляет запись в `RoomLog` |
| `idx_rooms_apartment`, `idx_roomlog_room` (INDEX) | Ускоряют выборку комнат квартиры и журнала комнаты |
