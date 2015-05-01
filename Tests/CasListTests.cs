using System.Linq;
using FluentAssertions;
using NUnit.Framework;

namespace ListTest.Tests
{
	[TestFixture]
	public class CasListTests
	{
		[Test]
		public void ListInstantiation()
		{
			var list = new CasList<bool>();

			list.Should().NotBeNull();
		}

		[Test]
		public void AddSingleItemToList()
		{
			var list = new CasList<int>();
			list.Add(150);

			list.Should().NotBeNull();
			list.Count.Should().Be(1);
		}

		[Test]
		public void AddMultileItemsToList()
		{
			var list = new CasList<int>();
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
			var list = new CasList<int>();
			list.Add(150);
			list.Add(42);
			list.Add(978);

			list[1].Should().Be(42);
		}

		[Test]
		public void InsertWorks()
		{
			var list = new CasList<int>();
			list.Add(150);
			list.Add(42);
			list.Add(978);

			list.Insert(1, 99);
			list.Count.Should().Be(4);
			list[1].Should().Be(99);
			list[2].Should().Be(42);
		}

		[Test]
		public void ContainsWorksForList()
		{
			var list = new CasList<int>();
			list.Add(150);
			list.Add(42);
			list.Add(978);

			list.Contains(42).Should().BeTrue();
		}

		[Test]
		public void ContainsFalseWorksForList()
		{
			var list = new CasList<int>();
			list.Add(150);
			list.Add(42);
			list.Add(978);

			list.Contains(420).Should().BeFalse();
		}

		[Test]
		public void RemoveWorksForList()
		{
			var list = new CasList<int>();
			list.Add(150);
			list.Add(7);
			list.Add(978);
			list.Add(35);
			list.Add(42);

			list.Remove(978).Should().BeTrue();
			list.Count.Should().Be(4);
		}

		[Test]
		public void RemoveAtWorksForList()
		{
			var list = new CasList<int>();
			list.Add(150);
			list.Add(7);
			list.Add(978);
			list.Add(35);
			list.Add(42);

			list.RemoveAt(2);
			list.Count.Should().Be(4);
			list[2].Should().Be(35);
		}

		[Test]
		public void ListIndexOfTest()
		{
			var list = new CasList<int>();
			list.Add(150);
			list.Add(42);
			list.Add(978);

			list.IndexOf(42).Should().Be(1);
		}

		[Test]
		public void ClearRemovedAllItems()
		{
			var list = new CasList<int>();
			list.Add(150);
			list.Add(42);
			list.Add(978);
			list.Clear();

			list.Should().NotBeNull();
			list.Count.Should().Be(0);
		}

		[Test]
		public void CopyToWorks()
		{
			var list = new CasList<int>();
			list.Add(150);
			list.Add(42);
			list.Add(978);

			var list2 = new int[3];

			list.CopyTo(list2, 0);

			list2.Length.Should().Be(3);
			list2[0].Should().Be(150);
			list2[1].Should().Be(42);
			list2[2].Should().Be(978);
		}
	}
}
