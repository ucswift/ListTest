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

		[Test]
		public void ListIndexer()
		{
			var list = new LockList<int>();
			list.Add(150);
			list.Add(42);
			list.Add(978);

			list[1].Should().Be(42);
		}

		[Test]
		public void ContainsWorksForList()
		{
			var list = new LockList<int>();
			list.Add(150);
			list.Add(42);
			list.Add(978);

			list.Contains(42).Should().BeTrue();
		}

		[Test]
		public void ContainsFalseWorksForList()
		{
			var list = new LockList<int>();
			list.Add(150);
			list.Add(42);
			list.Add(978);

			list.Contains(420).Should().BeFalse();
		}

		[Test]
		public void RemoveWorksForList()
		{
			var list = new LockList<int>();
			list.Add(150);
			list.Add(7);
			list.Add(978);
			list.Add(35);
			list.Add(42);

			list.Remove(978).Should().BeTrue();
			list.Count.Should().Be(4);
		}

		[Test]
		public void ListIndexOfTest()
		{
			var list = new LockList<int>();
			list.Add(150);
			list.Add(42);
			list.Add(978);

			list.IndexOf(42).Should().Be(1);
		}
	}
}
