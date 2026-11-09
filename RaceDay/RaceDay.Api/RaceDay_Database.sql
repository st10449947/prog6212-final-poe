-- RaceDay Database Script (Simple Version)
-- Run this in SSMS on a clean database

-- Drop tables if they already exist (so the script can be re-run)
create database RaceDay;
go

DROP TABLE IF EXISTS Results;
DROP TABLE IF EXISTS Enrolments;
DROP TABLE IF EXISTS Routes;
DROP TABLE IF EXISTS Categories;
DROP TABLE IF EXISTS Events;
DROP TABLE IF EXISTS Users;
GO

-- 1. Users (Organisers and Participants)
CREATE TABLE Users (
    UserID INT IDENTITY(1,1) PRIMARY KEY,
    FullName NVARCHAR(100) NOT NULL,
    Email NVARCHAR(150) NOT NULL UNIQUE,
    PasswordHash NVARCHAR(255) NOT NULL,
    Role NVARCHAR(20) NOT NULL,
    CreatedAt DATETIME DEFAULT GETDATE()
);
GO

-- 2. Events (created by an Organiser)
CREATE TABLE Events (
    EventID INT IDENTITY(1,1) PRIMARY KEY,
    UserID INT NOT NULL,
    EventName NVARCHAR(150) NOT NULL,
    Description NVARCHAR(MAX),
    EventDate DATE NOT NULL,
    Location NVARCHAR(150) NOT NULL,
    Distance DECIMAL(6,2) NOT NULL,
    EventType NVARCHAR(20) NOT NULL,
    FOREIGN KEY (UserID) REFERENCES Users(UserID)
);
GO

-- 3. Categories (belong to an Event)
CREATE TABLE Categories (
    CategoryID INT IDENTITY(1,1) PRIMARY KEY,
    EventID INT NOT NULL,
    CategoryName NVARCHAR(100) NOT NULL,
    DistanceKm DECIMAL(6,2) NOT NULL,
    MaxParticipants INT DEFAULT 100,
    FOREIGN KEY (EventID) REFERENCES Events(EventID)
);
GO

-- 4. Routes (one Route per Category)
CREATE TABLE Routes (
    RouteID INT IDENTITY(1,1) PRIMARY KEY,
    CategoryID INT NOT NULL UNIQUE,
    RouteName NVARCHAR(100) NOT NULL,
    StartPoint NVARCHAR(150) NOT NULL,
    EndPoint NVARCHAR(150) NOT NULL,
    ElevationGainM INT DEFAULT 0,
    MapURL NVARCHAR(255),
    FOREIGN KEY (CategoryID) REFERENCES Categories(CategoryID)
);
GO

-- 5. Enrolments (a Participant entering an Event under a Category)
CREATE TABLE Enrolments (
    EnrolmentID INT IDENTITY(1,1) PRIMARY KEY,
    UserID INT NOT NULL,
    EventID INT NOT NULL,
    CategoryID INT NOT NULL,
    EnrolmentDate DATETIME DEFAULT GETDATE(),
    Status NVARCHAR(20) DEFAULT 'Confirmed',
    FOREIGN KEY (UserID) REFERENCES Users(UserID),
    FOREIGN KEY (EventID) REFERENCES Events(EventID),
    FOREIGN KEY (CategoryID) REFERENCES Categories(CategoryID)
);
GO

-- 6. Results (one Result per Enrolment)
CREATE TABLE Results (
    ResultID INT IDENTITY(1,1) PRIMARY KEY,
    EnrolmentID INT NOT NULL UNIQUE,
    UserID INT NOT NULL,
    FinishTimeSeconds INT NOT NULL,
    FinishPosition INT NOT NULL,
    RecordedAt DATETIME DEFAULT GETDATE(),
    FOREIGN KEY (EnrolmentID) REFERENCES Enrolments(EnrolmentID),
    FOREIGN KEY (UserID) REFERENCES Users(UserID)
);
GO

-- ================= SEED DATA =================

-- 2 Organisers, 2 Participants
INSERT INTO Users (FullName, Email, PasswordHash, Role) VALUES
('Thabo Nkosi', 'thabo.nkosi@raceday.co.za', 'HASHED_PWD_1', 'Organiser'),
('Lindiwe Dlamini', 'lindiwe.dlamini@raceday.co.za', 'HASHED_PWD_2', 'Organiser'),
('Johan van der Merwe', 'johan.vdm@example.com', 'HASHED_PWD_3', 'Participant'),
('Aisha Patel', 'aisha.patel@example.com', 'HASHED_PWD_4', 'Participant');
GO

-- 3 Events
INSERT INTO Events (UserID, EventName, Description, EventDate, Location, Distance, EventType) VALUES
(1, 'Pretoria Sunrise Cycle Tour', 'A scenic cycling tour through Pretoria.', '2026-11-08', 'Pretoria, Gauteng', 94.70, 'Cycling'),
(1, 'Joburg City Fun Run', 'A community running event through Johannesburg CBD.', '2026-10-18', 'Johannesburg, Gauteng', 21.10, 'Running'),
(2, 'Durban Beachfront Walkathon', 'A family walking event along the Durban promenade.', '2026-09-27', 'Durban, KwaZulu-Natal', 10.00, 'Walking');
GO

-- Categories for each Event
INSERT INTO Categories (EventID, CategoryName, DistanceKm, MaxParticipants) VALUES
(1, '94km Individual', 94.70, 500),
(1, '47km Half Tour', 47.00, 300),
(2, '21.1km Half Marathon', 21.10, 800),
(2, '10km Fun Run', 10.00, 1000),
(3, '10km Walk', 10.00, 600),
(3, '5km Family Walk', 5.00, 600);
GO

-- Routes for each Category
INSERT INTO Routes (CategoryID, RouteName, StartPoint, EndPoint, ElevationGainM, MapURL) VALUES
(1, 'Pretoria 94km Route', 'Union Buildings', 'Loftus Versfeld Stadium', 1120, 'https://maps.raceday.co.za/pretoria-94km'),
(2, 'Pretoria 47km Route', 'Union Buildings', 'Menlyn Park', 560, 'https://maps.raceday.co.za/pretoria-47km'),
(3, 'Joburg Half Marathon Route', 'Sandton City', 'Zoo Lake', 340, 'https://maps.raceday.co.za/joburg-21km'),
(4, 'Joburg 10km Route', 'Sandton City', 'Melrose Arch', 180, 'https://maps.raceday.co.za/joburg-10km'),
(5, 'Durban 10km Walk Route', 'uShaka Marine World', 'Suncoast Casino', 20, 'https://maps.raceday.co.za/durban-10km'),
(6, 'Durban 5km Family Route', 'uShaka Marine World', 'Moses Mabhida Stadium', 15, 'https://maps.raceday.co.za/durban-5km');
GO

-- Sample Enrolments
INSERT INTO Enrolments (UserID, EventID, CategoryID, Status) VALUES
(3, 1, 2, 'Confirmed'),
(4, 1, 1, 'Confirmed'),
(3, 2, 3, 'Confirmed'),
(4, 3, 6, 'Confirmed');
GO

-- Sample Results
INSERT INTO Results (EnrolmentID, UserID, FinishTimeSeconds, FinishPosition) VALUES
(1, 1, 9840, 45),
(3, 1, 6300, 12);
GO