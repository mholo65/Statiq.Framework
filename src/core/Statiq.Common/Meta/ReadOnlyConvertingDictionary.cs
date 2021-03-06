﻿using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using Statiq.Common;

namespace Statiq.Common
{
    /// <summary>
    /// A dictionary with metadata type conversion superpowers.
    /// </summary>
    /// <remarks>
    /// This class wraps an underlying <see cref="IReadOnlyDictionary{TKey, TValue}"/> but
    /// uses the provided <see cref="IExecutionContext"/> to perform type conversions
    /// when requesting values.
    /// </remarks>
    public class ReadOnlyConvertingDictionary : IMetadata
    {
        private readonly IReadOnlyDictionary<string, object> _dictionary;

        public ReadOnlyConvertingDictionary(IReadOnlyDictionary<string, object> dictionary)
        {
            _dictionary = dictionary ?? throw new ArgumentNullException(nameof(dictionary));
        }

        public ReadOnlyConvertingDictionary(IEnumerable<KeyValuePair<string, object>> items)
        {
            _ = items ?? throw new ArgumentNullException(nameof(items));
            Dictionary<string, object> dictionary = new Dictionary<string, object>(StringComparer.OrdinalIgnoreCase);

            // Copy over in case there are duplicate keys
            foreach (KeyValuePair<string, object> item in items)
            {
                dictionary[item.Key] = item.Value;
            }
            _dictionary = dictionary;
        }

        /// <inheritdoc />
        public int Count => _dictionary.Count;

        /// <inheritdoc />
        public IEnumerable<string> Keys => _dictionary.Keys;

        /// <inheritdoc />
        public IEnumerable<object> Values => _dictionary.Values.Select(GetValue);

        /// <inheritdoc />
        public bool ContainsKey(string key) => _dictionary.ContainsKey(key);

        /// <inheritdoc />
        public object this[string key]
        {
            get
            {
                if (key == null)
                {
                    throw new ArgumentNullException(nameof(key));
                }
                if (!TryGetValue(key, out object value))
                {
                    throw new KeyNotFoundException("The key " + key + " was not found in metadata, use Get() to provide a default value.");
                }
                return value;
            }
        }

        /// <inheritdoc />
        public IEnumerator<KeyValuePair<string, object>> GetEnumerator() => _dictionary.Select(GetItem).GetEnumerator();

        /// <inheritdoc />
        public bool TryGetRaw(string key, out object value) => _dictionary.TryGetValue(key, out value);

        /// <inheritdoc />
        IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();

        /// <inheritdoc />
        public IMetadata GetMetadata(params string[] keys) =>
            throw new NotSupportedException();

        /// <inheritdoc />
        public bool TryGetValue<TValue>(string key, out TValue value)
        {
            value = default;
            if (!_dictionary.TryGetValue(key, out object rawValue))
            {
                return false;
            }
            rawValue = GetValue(rawValue);
            return TypeHelper.TryConvert(rawValue, out value);
        }

        /// <inheritdoc />
        public bool TryGetValue(string key, out object value) => TryGetValue<object>(key, out value);

        /// <summary>
        /// This resolves the metadata value by recursively expanding IMetadataValue.
        /// </summary>
        private object GetValue(object originalValue) =>
            originalValue is IMetadataValue metadataValue ? GetValue(metadataValue.Get(this)) : originalValue;

        /// <summary>
        /// This resolves the metadata value by expanding IMetadataValue.
        /// </summary>
        private KeyValuePair<string, object> GetItem(KeyValuePair<string, object> item) =>
            item.Value is IMetadataValue metadataValue
                ? new KeyValuePair<string, object>(item.Key, GetValue(metadataValue.Get(this)))
                : item;
    }
}
