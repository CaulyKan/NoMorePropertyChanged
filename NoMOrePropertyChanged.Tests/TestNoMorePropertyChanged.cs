using System;
using System.Collections.Generic;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace NoMorePropertyChanged.Tests
{
    [TestClass]
    public class TestNoMorePropertyChanged
    {
        private TestViewModel TestVM { get; set; }

        [TestInitialize]
        public void Initialize()
        {
            TestVM = new TestViewModel();
            TestVM.Test1 = new TestDTO();
            TestVM.Test5 = new List<string>();
        }

        [TestMethod]
        public void Test1_BasicString()
        {
            var notified = false;
            PropertyChangeDependency.MonitorPropertyChanged(TestVM.Test1Binding, "TestString", (Action)(() => notified = true));
            TestVM.Test1Binding.TestString = "Test1";
            Assert.IsTrue(notified);
            Assert.AreEqual(TestVM.Test1.TestString, "Test1");
        }

        [TestMethod]
        public void Test1_TestSubDataString()
        {
            var notified = false;
            PropertyChangeDependency.MonitorPropertyChanged(TestVM.Test1Binding.SubTestData, "SubTestString", (Action)(() => notified = true));
            TestVM.Test1Binding.SubTestData.SubTestString = "Test1";
            Assert.IsTrue(notified);
            Assert.AreEqual(TestVM.Test1.SubTestData.SubTestString, "Test1");
        }

        [TestMethod]
        public void Test1_testSubDataAssignment()
        {
            var notified = false;
            PropertyChangeDependency.MonitorPropertyChanged(TestVM.Test1Binding, "SubTestData", (Action)(() => notified = true));
            TestVM.Test1Binding.SubTestData = new TestSubDTO { SubTestString = "Test1" };
            Assert.IsTrue(notified);
            Assert.AreEqual(TestVM.Test1.SubTestData.SubTestString, "Test1");

            notified = false;
            PropertyChangeDependency.MonitorPropertyChanged(TestVM.Test1Binding, "SubTestData.SubTestString", (Action)(() => notified = true));
            TestVM.Test1Binding.SubTestData = new TestSubDTO { SubTestString = "NewTest1" };
            Assert.IsTrue(notified);
            Assert.AreEqual(TestVM.Test1.SubTestData.SubTestString, "NewTest1");
        }

        [TestMethod]
        public void Test1_TestList()
        {
            var notified = false;
            PropertyChangeDependency.MonitorCollectionChanged(TestVM.Test1Binding, "TestList", (Action)(() => notified = true));
            TestVM.Test1Binding.TestList.Add("Test1");
            Assert.IsTrue(notified);
            Assert.AreEqual(TestVM.Test1.TestList[0], "Test1");

            notified = false;
            PropertyChangeDependency.MonitorPropertyChanged(TestVM.Test1Binding, "TestList", (Action)(() => notified = true));
            TestVM.Test1Binding.TestList = new List<string> { "Test1" };
            Assert.IsTrue(notified);
            Assert.AreEqual(TestVM.Test1.TestList[0], "Test1");

            notified = false;
            TestVM.Test1Binding.TestList.Add("Test1");
            Assert.IsTrue(notified);
            Assert.AreEqual(TestVM.Test1.TestList[1], "Test1");
        }

        [TestMethod]
        public void Test1_TestDataList()
        {
            var notified = false;
            PropertyChangeDependency.MonitorCollectionChanged(TestVM.Test1Binding, "TestDataList", (Action)(() => notified = true));
            TestVM.Test1Binding.TestDataList.Add(new TestSubDTO { SubTestString = "Test1" });
            Assert.IsTrue(notified);
            Assert.AreEqual(TestVM.Test1.TestDataList[0].SubTestString, "Test1");

            notified = false;
            PropertyChangeDependency.MonitorPropertyChanged(TestVM.Test1Binding.TestDataList[0], "SubTestString", (Action)(() => notified = true));
            TestVM.Test1Binding.TestDataList[0].SubTestString = "NewTest1";
            Assert.IsTrue(notified);
            Assert.AreEqual(TestVM.Test1.TestDataList[0].SubTestString, "NewTest1");
        }

        [TestMethod]
        public void Test1_TestDictionary()
        {
            var notified = false;
            PropertyChangeDependency.MonitorCollectionChanged(TestVM.Test1Binding.SubTestData, "TestDictionary", (Action)(() => notified = true));
            TestVM.Test1Binding.SubTestData.TestDictionary.Add("Test1", "Test1");
            Assert.IsTrue(notified);
            Assert.AreEqual(TestVM.Test1.SubTestData.TestDictionary["Test1"], "Test1");
        }

        [TestMethod]
        public void Test1_TestSetValue()
        {
            TestVM.Test1Binding.TestInt = "1";
            Assert.AreEqual(TestVM.Test1.TestInt, 1);
        }

        [TestMethod]
        public void Test2_TestSimpleDependency()
        {
            var notified = false;
            PropertyChangeDependency.MonitorPropertyChanged(TestVM, "Test2", (Action)(() => notified = true));
            TestVM.Test1Binding.TestString = "Test2";
            Assert.IsTrue(notified);
            Assert.AreEqual(TestVM.Test1.TestString, "Test2");
            Assert.AreEqual(TestVM.Test2, "Test2");
        }

        [TestMethod]
        public void Test3_TestListDependency()
        {
            var notified = false;
            PropertyChangeDependency.MonitorPropertyChanged(TestVM, "Test3", (Action)(() => notified = true));
            TestVM.Test1Binding.TestList = new List<string> { "Test3" };
            Assert.IsTrue(notified);
            Assert.AreEqual(TestVM.Test3, "Test3");

            notified = false;
            TestVM.Test1Binding.TestList.Add("Test3");
            Assert.IsTrue(notified);
            Assert.AreEqual(TestVM.Test3, "Test3,Test3");

            notified = false;
            TestVM.Test1Binding.TestList.RemoveAt(0);
            Assert.IsTrue(notified);
            Assert.AreEqual(TestVM.Test3, "Test3");
        }

        [TestMethod]
        public void Test4_TestChainedDependency()
        {
            var notified = false;
            PropertyChangeDependency.MonitorPropertyChanged(TestVM, "Test4", (Action)(() => notified = true));
            TestVM.Test1Binding.TestList = new List<string> { "Test4" };
            Assert.IsTrue(notified);
            Assert.AreEqual(TestVM.Test4, "Test4");
        }

        [TestMethod]
        public void Test5_TestList()
        {
            var notified = false;
            PropertyChangeDependency.MonitorCollectionChanged(TestVM, "Test5Binding", (Action)(() => notified = true));
            TestVM.Test5Binding.Add("Test5");
            Assert.IsTrue(notified);
            Assert.AreEqual(TestVM.Test5[0], "Test5");

            notified = false;
            TestVM.Test5Binding[0] = "Test5_modify";
            Assert.IsTrue(notified);
            Assert.AreEqual(TestVM.Test5[0], "Test5_modify");

            notified = false;
            TestVM.Test5Binding[0] = 123;
            Assert.IsTrue(notified);
            Assert.AreEqual(TestVM.Test5[0], "123");
        }

    }
}
