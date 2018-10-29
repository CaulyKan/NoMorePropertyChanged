# NoMorePropertyChanged

This project aims to elimate the annoying PropertyChanged stuff from your ViewModel code.

## What's wrong with PropertyChanged

There are two main issues with PropertyChanged system.

1. A class must notify its own properties.

Suppose you have a data class from ORM or a dto from wcf. These classes just won't support INotifyPropertyChanged. The recommanded MVVM way to show these data in your view is to define corresponding properties in your viewmodel. However, this is not only boring, but also hard to maintain. Using technologies like Castle DynamicProxy or PropertyChanged.Fody can cut off most of these code, but still you have to maintain its state yourself.

1. The NotifyPropertyChanged logic flow is reversed against common sense.

Suppose you have two properties `string A {get;set;}` and `string B { get { return A;} }`. The correct flow is B depends on A, not A notifies B, because only B knows what it's looking for. This design will make your viewmodel unmaintainable when viewmodel is very complex.

## How NoMorePropertyChanged ease your pain

NoMorePropertyChanged utilizes two independent parts to above two problems(Though it's recommanded to use them together).

1. The NotifyProxy

The NotifyProxy is a dynamic object that proxies your data object. Define your property like:
```
        public dynamic Test1Binding { get; set; }
        public TestDTO Test1
        {
            get { return (TestDTO)Test1Binding; }
            set { SetBinding(nameof(Test1Binding), value); }
        }
```
Then, make all your binding in view and data manupation in your VM goes to `Test1Binding`, which will auto notify through INotifyPropertyChanged or INotifyCollectionChanged. Your can access your data with `Test1` anytime because `Test1Binding` is just a proxy.

2. The PropertyChangedDependency

The PropertyChangedDependency automatically triggers PropertyChanged event when the dependency changed. Define your property like:
```
        [DependsOn("Test1Binding.TestString")]
        public string Test2
        {
            get { return Test1Binding.TestString; }
        }
```
Then, `Test2` will be notified when `Test1Binding.TestString` is notified.

## A complete example

Suppose you have a model/data definition:
```
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
```

And your viewmodel:
```
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
            get { return string.Join(",", Test1.TestList); }
        }

        [DependsOn(nameof(Test3))]
        public string Test4
        {
            get { return Test3; }
        }
    }
```

Just bind or modify `Test1Binding`. Everything will notifies as excepted.

Hint: No intellisense for `Test1Binding`? Just write your code with `Test1` to access intellisense, and remember to modify it to `Test1Binding` afterwards.

Hint2: Have your own base view model class? No problem, just implement `INoMorePropertyChanged`, and add these to your base vm(if missing):
```
        public $your_constructor$()
        {
            PropertyChangeDependency.Install(this);
        }

        public event PropertyChangedEventHandler PropertyChanged;

        public void SetBinding(string bindingProp, object value)
        {
            var prop = this.GetType().GetProperty(bindingProp);
            prop.SetValue(this, NotifyProxy.CreateBinding(value));
            this.OnPropertyChanged(bindingProp);
        }

        public void OnPropertyChanged(string prop)
        {
            this.PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(prop));
        }
```

## Capacities and limitations
Here's a table of what this thing can and can not: (in above example)
 
ViewModel | Binding | Remarks
--------- | ------- | -------
Test1Binding.TestString = "" | {Binding Test1Binding.TestString} | NotifyPropertyChanged
Test1.TestString = "" | {Binding Test1Binding.TestString} | WON'T Notify
Test1Binding.SubTestData.SubTestString = "" | {Binding Test1Binding.SubTestData.SubTestString} | NotifyPropertyChanged
Test1Binding.SubTestData = new TestSubDTO { SubTestString = "" } | {Binding Test1Binding.SubTestData.SubTestString} | NotifyPropertyChanged
Test1Binding.SubTestData = new TestSubDTO { SubTestString = "" } | {Binding Test1Binding.SubTestData} | NotifyPropertyChanged
Test1Binding.TestList.Add("") | {Binding Test1Binding.TestList} | NotifyCollectionChanged
Test1Binding.TestList = new List<string> { "" } | {Binding Test1Binding.TestList} | NotifyPropertyChanged
Test1.TestList.Add("") | {Binding Test1Binding.TestList}  | WON'T Notify
Test1Binding.TestDataList.Add(new TestSubDTO { SubTestString = "" }) | {Binding Test1Binding.TestDataList} | NotifyCollectionChanged
Test1Binding.TestDataList[0].SubTestString = "" | {Binding Test1Binding.TestDataList[0].SubTestString} | NotifyPropertyChanged
Test1Binding.TestString = "" | {Binding Test2} | NotifyPropertyChanged
Test1Binding.TestList = new List<string> { "" } | {Binding Test3} | NotifyPropertyChanged
Test1Binding.TestList.Add("") | {Binding Test3} | NotifyPropertyChanged
Test1Binding.TestList.Add("") | {Binding Test4} | NotifyPropertyChanged

For more infomation, checkout the test cases.
