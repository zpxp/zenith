using System;
using Microsoft.Extensions.DependencyInjection;
using Xunit;
using Zenith;

namespace ZenithTest
{
	
	public class UnitTestDI : IClassFixture<TestsFixture>
	{
		private readonly TestsFixture data;
		public UnitTestDI(TestsFixture data)
		{
			this.data = data;
		}

		[Fact]
		public void DI1()
		{
			using (var processContainer = data.services.BuildServiceProvider())
			{
				var factory = processContainer.GetRequiredService<Func<string, IUnitOfWork>>();
				var unitOfWork1 = processContainer.GetRequiredService<IUnitOfWork>();
				var unitOfWork2 = processContainer.GetRequiredService<IUnitOfWork>();

				Assert.Same(unitOfWork1, unitOfWork2);

				unitOfWork2 = factory("profile 2");
				Assert.NotSame(unitOfWork1, unitOfWork2);

				unitOfWork1 = factory("profile 2");
				Assert.Same(unitOfWork1, unitOfWork2);
			}
		}

	}
}
