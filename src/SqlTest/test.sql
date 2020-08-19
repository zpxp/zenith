
-- some test tables sql. new db is created on every test run
CREATE TABLE [Contact](
	[ContactId] int CONSTRAINT [PK_Contact] PRIMARY KEY NOT NULL,
	[Name] [nvarchar](1000) NOT NULL,
	[Number] [nvarchar](1000) NOT NULL	
);


CREATE TABLE [Building](
	[BuildingId] int CONSTRAINT [PK_Building] PRIMARY KEY NOT NULL,
	[Name] [nvarchar](1000) NOT NULL	
);

CREATE TABLE [Bossman](
	[BossmanId] int CONSTRAINT [PK_Bossman] PRIMARY KEY NOT NULL,
	[ContactId] [int] constraint FK_Bossman_Contact references [Contact]([ContactId]) NOT NULL,
	[Name] [nvarchar](1000) NOT NULL	
);


CREATE TABLE [BossmanBuilding](
	[BossmanBuildingId] int CONSTRAINT [PK_BossmanBuilding] PRIMARY KEY NOT NULL,
	[BuildingId] [int] constraint FK_BuildingBossman_Building references [Building]([BuildingId]) NOT NULL,
	[BossmanId] [int] constraint FK_BossmanBossman_Bossman references [Bossman]([BossmanId]) NOT NULL
);


CREATE TABLE [Worker](
	[WorkerId] int CONSTRAINT [PK_Worker] PRIMARY KEY NOT NULL,
	[ContactId] [int] constraint FK_Worker_Contact references [Contact]([ContactId]) NULL,
	[Name] [nvarchar](1000) NOT NULL	,
	[UpdatedOn] [DATETIME] NULL
);




CREATE TABLE [WorkerBossman](
	[WorkerBossmanId] int CONSTRAINT [PK_WorkerBossman] PRIMARY KEY NOT NULL,
	[WorkerId] [int] constraint FK_WorkerBossman_Worker references [Worker]([WorkerId]) NOT NULL,
	[BossmanId] [int] constraint FK_SudentBossman_Bossman references [Bossman]([BossmanId]) NOT NULL
);

CREATE TABLE [WorkerCar](
	[WorkerCarId] int CONSTRAINT [PK_WorkerCar] PRIMARY KEY NOT NULL,
	[WorkerId] [int] constraint FK_WorkerCar_Worker references [Worker]([WorkerId]) NOT NULL,
	[Type] [nvarchar](1000) NOT NULL	
);

--------------

insert into [Contact] Values
(1, 'Bossman 1', '123'),
(2, 'Bossman 2', '1234'),
(3, 'Worker 1', '1235'),
(4, 'Worker 3', '126'),
(5, 'Worker 4', '1237');

insert into [Building] Values
(1, 'Room 1'),
(2, 'Room 2');

insert into [Bossman] Values
(1, 1, 'Bossman 1'),
(2, 2, 'Bossman 2');

insert into [Worker] 
([WorkerId], [ContactId], [Name])
Values
(1, 3, 'Worker 1'),
(2, null, 'Worker 2'),
(3, 4, 'Worker 3'),
(4, 5, 'Worker 4');

insert into [BossmanBuilding] Values
(1, 1, 1),
(2, 1, 2),
(3, 2, 1);

insert into [WorkerBossman] Values
(1, 1, 1),
(2, 2, 1),
(3, 3, 2),
(4, 4, 2);

insert into [WorkerCar] Values
(1, 1, 'Worker 1 Car'),
(2, 2, 'Worker 2 Car'),
(3, 3, 'Worker 3 Car'),
(4, 4, 'Worker 4 Car'),
(5, 1, 'Worker 1 Car2');