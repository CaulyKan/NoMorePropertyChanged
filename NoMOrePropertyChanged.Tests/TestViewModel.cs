using NoMorePropertyChanged;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace NoMorePropertyChanged.Tests
{
    public class TestViewModel : NoMorePropertyChanged
    {
        public dynamic Test1Binding { get; set; }
        public TestDTO Test1
        {
            get { return (TestDTO)Test1Binding; }
            set { SetBinding(nameof(Test1Binding), value); }
        }

        [DependsOn("Test1Binding.TestString")]
        public string Test2
        {
            get { return Test1Binding.TestString; }
        }

        [DependsOnCollection(nameof(Test1Binding), nameof(TestDTO.TestList))]
        public string Test3
        {
            get { return Test1 == null? "": string.Join(",", Test1.TestList); }
        }

        [DependsOn(nameof(Test3))]
        public string Test4
        {
            get { return Test3; }
        }

        public dynamic Test5Binding { get; set; }
        public List<string> Test5
        {
            get { return (List<string>) Test5Binding; }
            set { SetBinding(nameof(Test5Binding), value); }
        }
    }
}
