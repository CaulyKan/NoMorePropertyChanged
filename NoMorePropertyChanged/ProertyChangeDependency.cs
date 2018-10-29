using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows;
using System.Data;
using System.ComponentModel;
using System.Windows.Data;
using System.Collections.Specialized;

namespace NoMorePropertyChanged
{
    public static class PropertyChangeDependency
    {
        public static void Install(IOnPropertyChanged obj)
        {
            var type = obj.GetType();
            foreach(var prop in type.GetProperties())
            {
                var attribs = prop.GetCustomAttributes(typeof(DependsOnAttribute), true).Cast<DependsOnAttribute>().ToList();
                attribs.ForEach(attr => 
                {
                    CreateBinding(obj, attr.BindingPath, prop.Name);
                });
                var attribs2 = prop.GetCustomAttributes(typeof(DependsOnCollectionAttribute), true).Cast<DependsOnCollectionAttribute>().ToList();
                attribs2.ForEach(attr => 
                {
                    CreateCollectionBinding(obj, attr.BindingPath, prop.Name);
                });
            }
        }

        public static void MonitorPropertyChanged(IOnPropertyChanged obj, string prop, Action callback)
        {
            var pcdo = new PropertyChangeDependencyObject();
            var binding = new Binding(prop) {Source = obj};
            BindingOperations.SetBinding(pcdo, PropertyChangeDependencyObject.PropertyChangeDependencyProperty, binding);
            pcdo.DependentPropertyChanged += () => callback();
            pcdos.Add(pcdo);
        }

        public static void MonitorCollectionChanged(IOnPropertyChanged obj, string prop, Action callback)
        {
            var pcdo = new PropertyChangeDependencyObject();
            var binding = new Binding(prop) {Source = obj};
            BindingOperations.SetBinding(pcdo, PropertyChangeDependencyObject.CollectionChangeDependencyProperty, binding);
            pcdo.DependentCollectionChanged += () => callback();
            pcdos.Add(pcdo);
        }

        private static void CreateBinding(IOnPropertyChanged sourceObj, string path, string propToNotify)
        {
            var pcdo = new PropertyChangeDependencyObject();
            var binding = new Binding(path) {Source = sourceObj};
            BindingOperations.SetBinding(pcdo, PropertyChangeDependencyObject.PropertyChangeDependencyProperty, binding);
            pcdo.DependentPropertyChanged += () => sourceObj.OnPropertyChanged(propToNotify);
            pcdos.Add(pcdo);
        }

        private static void CreateCollectionBinding(IOnPropertyChanged sourceObj, string path, string propToNotify)
        {
            var pcdo = new PropertyChangeDependencyObject();
            var binding = new Binding(path) {Source = sourceObj};
            BindingOperations.SetBinding(pcdo, PropertyChangeDependencyObject.PropertyChangeDependencyProperty, binding);
            BindingOperations.SetBinding(pcdo, PropertyChangeDependencyObject.CollectionChangeDependencyProperty, binding);
            pcdo.DependentPropertyChanged += () => sourceObj.OnPropertyChanged(propToNotify);
            pcdo.DependentCollectionChanged += () => sourceObj.OnPropertyChanged(propToNotify);
            pcdos.Add(pcdo);
        }

        private static List<PropertyChangeDependencyObject> pcdos {get;set;} = new List<PropertyChangeDependencyObject>();

        private class PropertyChangeDependencyObject : UIElement
        {
            public static DependencyProperty PropertyChangeDependencyProperty = DependencyProperty.Register(
                nameof(PropertyChangeDependencyProperty), typeof(object), typeof(PropertyChangeDependencyObject), 
                new PropertyMetadata(null, (o, e) => {
                    (o as PropertyChangeDependencyObject).OnDependentPropertyChanged();
                })
            );

            public static DependencyProperty CollectionChangeDependencyProperty = DependencyProperty.Register(
                nameof(CollectionChangeDependencyProperty), typeof(object), typeof(PropertyChangeDependencyObject), 
                new PropertyMetadata(null, (o, e) => {
                    (o as PropertyChangeDependencyObject).OnDependentCollectionChanged(e.NewValue as INotifyCollectionChanged);
                })
            );

            public event Action DependentPropertyChanged;
            public event Action DependentCollectionChanged;

            public void OnDependentPropertyChanged() 
            {
                this.DependentPropertyChanged?.Invoke();
            } 

            public void OnDependentCollectionChanged(INotifyCollectionChanged col)
            {
                if (col != null)
                    col.CollectionChanged += (o, e) => this.DependentCollectionChanged?.Invoke();
            }
        }
    }

    public class DependsOnAttribute : Attribute 
    {
        public string BindingPath{get;set;}
        public DependsOnAttribute(params string[] BindingPath)
        {
            this.BindingPath = string.Join(".", BindingPath);
        }
    }

    public class DependsOnCollectionAttribute : Attribute 
    {
        public string BindingPath{get;set;}
        public DependsOnCollectionAttribute(params string[] BindingPath)
        {
            this.BindingPath = string.Join(".", BindingPath);
        }
    }
}