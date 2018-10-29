using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace NoMorePropertyChanged.Tests
{
    public class TestDTO
    {
        public string TestString { get; set; }
        public int TestInt { get; set; }
        public TestSubDTO SubTestData { get; set; } = new TestSubDTO();
        public List<string> TestList { get; set; } = new List<string>();
        public List<TestSubDTO> TestDataList { get; set; } = new List<TestSubDTO>();
    }

    public class TestSubDTO
    {
        public string SubTestString { get; set; }
        public Dictionary<string, string> TestDictionary { get; set; } = new Dictionary<string, string>();
    }
}
