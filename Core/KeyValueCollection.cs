using System;
using System.Collections.Generic;
using System.Runtime.Serialization;


namespace MOUSE.Core
{
    public interface IKeyValueCollection
    {
        object GetByIndex(int index);
        void SetByIndex(int index, object element);

        void Add(object element);

        void OnDeserializedMethod();

        int Count { get; }
    }

    [DataContract]
    public class KeyValue<KEY, VALUE>
    {
        [DataMember]
        public readonly KEY Key;
        [DataMember]
        public readonly VALUE Value;

        public KeyValue()
        {
            Key = default(KEY);
            Value = default(VALUE);
        }

        public KeyValue(KEY key, VALUE value)
        {
            Key = key;
            Value = value;
        }
    }

    [DataContract]
    public class KeyValueCollection<KEY, VALUE> : IKeyValueCollection
    {
        private List<KEY> _keys;
        private List<VALUE> _values;

        [DataMember]
        private List<KeyValue<KEY, VALUE>> _array;

        private Dictionary<KEY, VALUE> _dictionary;

        public KeyValueCollection(int capacity)
        {
            _keys = new List<KEY>(capacity);
            _values = new List<VALUE>(capacity);

            _array = new List<KeyValue<KEY, VALUE>>(capacity);
            _dictionary = new Dictionary<KEY, VALUE>(capacity);
        }

        public KeyValueCollection()
            : this(0)
        {
        }

        public KeyValueCollection(KeyValueCollection<KEY, VALUE> collection)
            : this(collection.Count)
        {
            foreach (KeyValue<KEY, VALUE> element in collection)
                Add(element.Key, element.Value);
        }

        public KeyValueCollection(IDictionary<KEY, VALUE> collection)
            : this(collection.Count)
        {
            foreach (KeyValuePair<KEY, VALUE> element in collection)
                Add(element.Key, element.Value);
        }

        #region IDictionary<KEY,VALUE> Members

        public void Add(KEY key, VALUE value)
        {
            if (ContainsKey(key))
                throw new ArgumentException(String.Format("An element with the key {0} already exists", key));

            Add(new KeyValue<KEY, VALUE>(key, value));
        }

        public bool ContainsKey(KEY key)
        {
            return _dictionary.ContainsKey(key);
        }

        public ICollection<KEY> Keys
        {
            get { return _dictionary.Keys; }
        }

        public bool Remove(KEY key)
        {
            int index = _array.FindIndex(element => element.Key.Equals(key));
            if (index == -1)
                return false;


            _dictionary.Remove(_keys[index]);

            _keys.RemoveAt(index);
            _values.RemoveAt(index);
            _array.RemoveAt(index);
            return true;
        }

        public bool TryGetValue(KEY key, out VALUE value)
        {
            return _dictionary.TryGetValue(key, out value);
        }

        public ICollection<VALUE> Values
        {
            get { return _dictionary.Values; }
        }

        public KeyValue<KEY, VALUE> GetByIndex(int index)
        {
            return _array[index];
        }

        public void SetByIndex(int index, KeyValue<KEY, VALUE> element)
        {
            _dictionary[element.Key] = element.Value;

            _keys[index] = element.Key;
            _values[index] = element.Value;

            _array[index] = element;
        }

        public VALUE this[KEY key]
        {
            get
            {
                return _dictionary[key];
            }
            set
            {
                int index = _keys.FindIndex(k => k.Equals(key));
                if (index != -1)
                {
                    _values[index] = value;
                    _array[index] = new KeyValue<KEY, VALUE>(key, value);
                    _dictionary[key] = value;
                }
                else
                    Add(key, value);
            }
        }

        #endregion

        #region ICollection<KeyValue<KEY,VALUE>> Members

        public void Add(KeyValue<KEY, VALUE> item)
        {
            _keys.Add(item.Key);
            _values.Add(item.Value);

            _array.Add(item);

            _dictionary.Add(item.Key, item.Value);
        }

        public void Clear()
        {
            _keys.Clear();
            _values.Clear();
            _array.Clear();
            _dictionary.Clear();
        }

        public bool Contains(KeyValue<KEY, VALUE> item)
        {
            for (int i = 0, count = _array.Count; i < count; i++)
                if (_array[i].Key.Equals(item.Key) && _array[i].Value.Equals(item.Value))
                    return true;

            return false;
        }

        public void CopyTo(KeyValue<KEY, VALUE>[] array, int arrayIndex)
        {
            for (int i = 0, count = _array.Count; i < count; i++)
                array[arrayIndex + i] = _array[i];
        }

        public int Count
        {
            get { return _array.Count; }
        }

        public bool IsReadOnly
        {
            get { return false; }
        }

        public bool Remove(KeyValuePair<KEY, VALUE> item)
        {
            return Remove(item.Key);
        }

        #endregion

        #region IEnumerable<KeyValue<KEY,VALUE>> Members

        public IEnumerator<KeyValue<KEY, VALUE>> GetEnumerator()
        {
            return _array.GetEnumerator();
        }

        #endregion

        #region IKeyValueCollection Members

        object IKeyValueCollection.GetByIndex(int index)
        {
            return GetByIndex(index);
        }

        public void SetByIndex(int index, object element)
        {
            SetByIndex(index, (KeyValue<KEY, VALUE>)element);
        }

        public void Add(object element)
        {
            Add((KeyValue<KEY, VALUE>)element);
        }

        #endregion

        public void OnDeserializedMethod()
        {
            _keys = new List<KEY>(_array.Count);
            _values = new List<VALUE>(_array.Count);

            _dictionary = new Dictionary<KEY, VALUE>(_array.Count);

            for (int i = 0, count = _array.Count; i < count; i++)
            {
                KeyValue<KEY, VALUE> element = _array[i];

                _keys.Add(element.Key);
                _values.Add(element.Value);

                _dictionary.Add(element.Key, element.Value);
            }
        }

        [OnDeserialized]
        public void OnDeserializedMethod(StreamingContext context)
        {
            OnDeserializedMethod();
        }
    }
}
