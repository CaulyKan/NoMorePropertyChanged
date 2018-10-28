using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.ComponentModel;
using System.Dynamic;
using System.Linq;

namespace NoMorePropertyChanged
{
    public interface IOnPropertyChanged : INotifyPropertyChanged
    {
        void OnPropertyChanged(string prop);
    }

    public interface INoMorePropertyChanged : IOnPropertyChanged
    {
        void SetBinding(string bindingProp, object value);
    }

    public class NoMorePropertyChanged : INoMorePropertyChanged
    {
        public NoMorePropertyChanged()
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
    }
    
    public class DynamicProxy: DynamicObject
    {
        private Type type;
        private object obj;

        public DynamicProxy(object obj)
        {
            SetObject(obj);
        }

        public virtual object GetObject()
        {
            return this.obj;
        }

        public virtual void SetObject(object obj)
        {
            this.obj = obj;
            this.type = this.obj.GetType();
        }

        public override bool Equals(object obj)
        {
            return this.obj.Equals(obj);
        }

        public override int GetHashCode()
        {
            return this.obj.GetHashCode();
        }

        public override string ToString()
        {
            return this.obj.ToString();
        }

        public override IEnumerable<string> GetDynamicMemberNames()
        {
            foreach (var member in this.type.GetMembers())
                yield return member.Name;
        }

        private object convertTo(object o, Type t)
        {
            if (t.IsAssignableFrom(o.GetType()))
                return o;

            var targetConverter = TypeDescriptior.GetConverter(t);
            if (targetConverter.CanConvertFrom(o.GetType()))
            {
                return targetConverter.ConvertFrom(o);
            }
            else if (targetConverter.CanConvertFrom(typeof(string)))
            {
                return targetConverter.ConvertFrom(o.ToString());
            }
            else 
            {
                throw new InvalidCastException($"Can't convert {o.GetType()} to {t}.");
            }
        }

        public override bool TryConvert(ConvertBinder binder, out object result)
        {
            if (this.obj == null)
                result = null;
            else if (binder.Type == this.type)
                result = this.obj;
            else 
                result = Convert.ChangeType(this.obj, binder.Type);
            return true;
        }

        public override bool TryGetIndex(GetIndexBinder binder, object[] indexes, out object result)
        {
            var prop = this.type.GetProperty("Item");
            if (prop == null || !prop.CanRead)
                throw new InvalidCastException();
            else if (this.obj == null)
                throw new NullReferenceException();
            else 
            {
                var indexTypes = prop.GetIndexParameters().Select(i => i.ParameterType).ToArray();
                if (indexes.Length != indexTypes.Length) return false;

                var convertedIndexes = new List<object>();
                for (int i = 0; i < indexes.Length; i++) 
                    convertedIndexes.Add(convertTo(indexes[i], indexTypes[i]));
                
                result = prop.GetValue(this.obj, convertedIndexes.ToArray());

                return true;
            }
        }

        public override bool TryGetMember(GetMemberBinder binder, out object result)
        {
            var prop = this.type.GetProperty(binder.Name);
            if (prop == null || !prop.CanRead)
                throw new InvalidCastException();
            else if (this.obj == null)
                throw new NullReferenceException();
            else 
            {
                result = prop.GetValue(this.obj);
                return true;
            }
        }

        public override bool TrySetMember(SetMemberBinder binder, object value)
        {
            var prop = this.type.GetProperty(binder.Name);
            if (prop == null || !prop.CanWrite)
                throw new InvalidCastException();
            else if (this.obj == null)
                throw new NullReferenceException();
            else 
            {
                result = prop.SetValue(this.obj, convertTo(value, prop.PropertyType));
                return true;
            }

        }

        public override bool TryInvokeMember(InvokeMemberBinder binder, object[] args, out object result)
        {
            var method = this.type.GetMethod(binder.Name);
            if (method == null)
                throw new InvalidCastException();
            else if (this.obj == null)
                throw new NullReferenceException();
            else 
            {
                result = method.Invoke(this.obj, args);
                return true;
            }

        }
    }

    public class NotifyProxy : DynamicProxy, IOnPropertyChanged
    {
        public NotifyProxy(object obj) : base(obj) {}

        public event PropertyChangedEventHandler PropertyChanged;

        public void OnPropertyChanged(string prop)
        {
            this.PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(prop));
        }   

        public static NotifyProxy CreateBinding(object obj) 
        {
            if (typeof(ICollection).IsAssignableFrom(obj.GetType())) return new NotifyList(obj);
            else return new NotifyProxy(obj);
        }

        public override bool TryGetIndex(GetIndexBinder binder, object[] indexes, out object result)
        {
            base.TryGetIndex(binder, indexes, out result);
            result = proxifyObject(result);
            return true;
        }

        public override bool TryGetMember(GetMemberBinder binder, out object result)
        {
            base.TryGetMember(binder, out result);
            result = unproxifyObject(result);
            return true;
        }

        public override bool TrySetMember(SetMemberBinder binder, object value)
        {
            var obj = proxifyObject(value);
            base.TrySetMember(binder, obj);
            OnPropertyChanged(binder.Name);
            return true;
        }

        private bool isSimpleType(object obj)
        {
            var type = obj.GetType();
            if (type.IsGenericType && type.GetGenericTypeDefinition() == typeof(Nullable<>))
                return isSimpleType(type.GetGenericArguments()[0]);
            return type.IsPrimitive || type.IsEnum || type == typeof(string) || type == typeof(decimal);
        }

        private Dictionary<WeakReference, NotifyProxy> propsDict = new Dictionary<WeakReference, NotifyProxy>();
        private bool searchPropsDict(object obj, ref object outObj)
        {
            foreach (var kvp in propsDict.ToList())
            {
                if (!kvp.Key.IsAlive)
                    propsDict.Remove(kvp.Key);
                else if (object.ReferenceEquals(obj, kvp.Key.Target))
                {
                    outObj = propsDict[kvp.Key];
                    return true;
                }
            }
            return false;
        }

        private object proxifyObject(object obj)
        {
            object result = null;
            if (obj == null) return null;
            else if (isSimpleType(obj)) return obj;
            else if (searchPropsDict(obj, ref result)) return result;
            else 
            {
                result = CreateBinding(obj);
                propsDict.Add(new WeakReference(obj), result as NotifyProxy);
                return result;
            }
        }
        
        private object unproxifyObject(object obj)
        {
            if (obj == null) return null;
            else if (typeof(NotifyProxy).IsAssignableFrom(obj.GetType())) return (obj as NotifyProxy).GetObject();
            else return obj;
        }
    }

    public class ProxyList : NotifyProxy, INotifyCollectionChanged, IEnumerable
    {
        public ProxyList(object obj) : base (obj) {}

        public override bool TryInvokeMember(InvokeMemberBinder binder, object[] args, out object result)
        {
            ArrayList cache = null;

            var name = binder.Name;
            if (name == nameof(List<object>.Clear) 
                || name == nameof(List<object>.RemoveAll)
                || name == nameof(List<object>.RemoveAt)
                || name == nameof(List<object>.RemoveRange)
                || name == nameof(List<object>.Sort))
                cache = this.toArrayList(this.GetObject() as IEnumerable);

            base.TryInvokeMember(binder, args, out result);

            if (name == nameof(List<object>.Add))
                this.OnCollectionChanged(NotifyCollectionChangedAction.Add, args[0]);
            else if (name == nameof(List<object>.AddRange))
                this.OnCollectionChanged(NotifyCollectionChangedAction.Add, toArrayList(args[0] as IEnumerable));
            else if (name == nameof(List<object>.Clear))
                this.OnCollectionChanged(NotifyCollectionChangedAction.Remove, cache);
            else if (name == nameof(List<object>.Insert))
                this.OnCollectionChanged(NotifyCollectionChangedAction.Add, args[1], (int)args[0]);
            else if (name == nameof(List<object>.InsertRange))
                this.OnCollectionChanged(NotifyCollectionChangedAction.Add, toArrayList(args[1] as IEnumerable), (int)args[0]);
            else if (name == nameof(List<object>.Remove))
                this.OnCollectionChanged(NotifyCollectionChangedAction.Remove, args[0]);
            else if (name == nameof(List<object>.RemoveAll))
            {
                this.OnCollectionChanged(NotifyCollectionChangedAction.Remove, cache);
                this.OnCollectionChanged(NotifyCollectionChangedAction.Add, toArrayList(this.GetObject() as IEnumerable));
            }
            else if (name == nameof(List<object>.RemoveAt))
            {
                if ((int)args[0]<cache.Count)
                    this.OnCollectionChanged(NotifyCollectionChangedAction.Remove, cache[(int)args[0]]);
            }
            else if (name == nameof(List<object>.RemoveRange))
            {
                for (int i = (int)args[0]; i < (int)args[0]+(int)args[1]; i++)
                    if (i < cache.Count)
                        this.OnCollectionChanged(NotifyCollectionChangedAction.Remove, cache[i]);
            }
            else if (name == nameof(List<object>.Sort))
            {
                this.OnCollectionChanged(NotifyCollectionChangedAction.Remove, cache);
                this.OnCollectionChanged(NotifyCollectionChangedAction.Add, toArrayList(this.GetObject() as IEnumerable));
            }

            return true;
        }

        public event NotifyCollectionChangedEventHandler CollectionChanged;

        public IEnumerator GetEnumerator()
        {
            if (this.GetObject() != null) return (this.GetObject() as IEnumerable).GetEnumerator();
            else return null;
        }

        private ArrayList toArrayList(IEnumerable items)
        {
            var result = new ArrayList();
            foreach(var i in items)
                result.Add(i);
        }

        #region OnCollectionChanged
        #endregion
    }
}