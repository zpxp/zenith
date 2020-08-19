using System;
using System.Collections.Generic;
using Sql;

namespace SqlSharpTest
{

	[SqlMappable(nameof(ContactId), "Contact")]
	public class Contact
	{
		public int ContactId { get; set; }
		public string Name { get; set; }
		public string Number { get; set; }
	}


	[SqlMappable(nameof(BuildingId), "Building")]
	public class BaseBuilding
	{
		public int BuildingId { get; set; }
		public string Name { get; set; }
	}

	public class BuildingMegaJoins : BaseBuilding
	{
		[SqlJoin("Bossman", typeof(BossmanBuilding))]
		public List<BossmanMegaJoin> Bossmans { get; set; }

		[SqlJoin("Car", typeof(BossmanBuilding), typeof(BaseBossman), typeof(WorkerBossman), typeof(BaseWorker), typeof(WorkerCar), Condition = "[{0}].[WorkerId] = 1")]
		[SqlColumn("Type")]
		public List<string> Cars { get; set; }
	}

	public class BuildingMegaJoinsNoCon : BaseBuilding
	{
		[SqlJoin("Bossman", typeof(BossmanBuilding))]
		public List<BossmanMegaJoin> Bossmans { get; set; }

		[SqlJoin("Car", typeof(BossmanBuilding), typeof(BaseBossman), typeof(WorkerBossman), typeof(BaseWorker), typeof(WorkerCar))]
		[SqlColumn("Type")]
		public List<string> Cars { get; set; }
	}

	[SqlMappable(nameof(BossmanId), "Bossman")]
	public class BaseBossman
	{
		public int BossmanId { get; set; }
		public int ContactId { get; set; }
		public string Name { get; set; }
	}

	// a class with simple ez join
	public class BossmanEzJoin : BaseBossman
	{
		[SqlJoin("Building")]
		public List<BossmanBuilding> Buildings { get; set; }
	}

	public class BossmanMegaJoin : BaseBossman
	{
		[SqlJoin("Worker", typeof(WorkerBossman))]
		public List<Worker> Workers { get; set; }
	}



	[SqlMappable(nameof(BossmanBuildingId), "BossmanBuilding")]
	public class BossmanBuilding
	{
		public int BossmanBuildingId { get; set; }
		public int BuildingId { get; set; }
		public int BossmanId { get; set; }
	}


	[SqlMappable(nameof(BossmanBuildingId), "BossmanBuilding")]
	public class BossmanBuildingSubClass 
	{
		public int BossmanBuildingId { get; set; }
		public Sub Subprops { get; set; }

		public class Sub
		{
			public int BuildingId { get; set; }
			public int BossmanId { get; set; }
		}
	}


	[SqlMappable(nameof(WorkerId), "Worker")]
	public class BaseWorker : IUpdatable
	{
		public int WorkerId { get; set; }
		public int? ContactId { get; set; }
		[SqlColumn("Name")]
		public string WorkerName { get; set; }
		public DateTime? UpdatedOn { get; set; }
	}

	public class Worker : BaseWorker
	{
		[SqlJoin("Car")]
		public List<WorkerCar> Cars { get; set; }
	}

	public class WorkerInterface : BaseWorker
	{
		[SqlJoin("Car", typeof(WorkerCar))]
		public List<ICar> Cars { get; set; }
	}

	[SqlMappable(nameof(WorkerBossmanId), "WorkerBossman")]
	public class WorkerBossman
	{
		public int WorkerBossmanId { get; set; }
		public int WorkerId { get; set; }
		public int BossmanId { get; set; }
	}

	[SqlMappable(nameof(CarId), "WorkerCar")]
	public class WorkerCar : ICar
	{
		[SqlColumn("WorkerCarId")]
		public int CarId { get; set; }
		public int WorkerId { get; set; }
		public string Type { get; set; }
	}


	public interface ICar
	{
		string Type { get; set; }
	}


	public interface IUpdatable
	{
		DateTime? UpdatedOn { get; set; }
	}

}