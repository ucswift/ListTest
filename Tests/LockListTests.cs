using FluentAssertions;
using NUnit.Framework;

namespace ListTest.Tests
{
	[TestFixture]
	public class LockListTests
	{
		[Test]
		public void ListInstantiation()
		{
			var list = new LockList<bool>();

			list.Should().NotBeNull();
		}

		[Test]
		public void AddSingleItemToList()
		{
			var list = new LockList<int>();
			list.Add(150);

			list.Should().NotBeNull();
			list.Count.Should().Be(1);
		}

		[Test]
		public void AddMultileItemsToList()
		{
			var list = new LockList<int>();
			list.Add(150);
			list.Add(7);
			list.Add(978);
			list.Add(35);
			list.Add(42);

			list.Should().NotBeNull();
			list.Count.Should().Be(5);
		}
	}
}
